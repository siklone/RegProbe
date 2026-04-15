#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from collections import defaultdict
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Any
from xml.etree import ElementTree as ET


REPO_ROOT = Path(__file__).resolve().parents[2]
EXCLUDED_DIR_NAMES = {
    ".git",
    ".github",
    ".codex",
    ".venv",
    "bin",
    "obj",
    "artifacts",
}
METHOD_PATTERN = re.compile(
    r"""
    ^\s*
    (?:(?:public|private|protected|internal|file|static|async|virtual|override|abstract|partial|sealed|unsafe|new|extern)\s+)*
    (?P<return>[A-Za-z_][A-Za-z0-9_<>,.\[\]?]*)
    \s+
    (?P<name>[A-Za-z_][A-Za-z0-9_]*)
    \s*
    \(
    """,
    re.VERBOSE,
)
METHOD_EXCLUSIONS = {"if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return"}
EVENT_HANDLER_PATTERN = re.compile(
    r"^\s*(?:private|protected|internal|public)\s+void\s+[A-Za-z_][A-Za-z0-9_]*\s*\(\s*object\b",
    re.MULTILINE,
)
MUTABLE_FIELD_PATTERN = re.compile(
    r"^\s*private\s+(?!readonly\b)(?!const\b)[A-Za-z_][A-Za-z0-9_<>,.\[\]?]*\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|;)",
    re.MULTILINE,
)
PACKAGE_REFERENCE_PATTERN = re.compile(r'<PackageReference\s+Include="([^"]+)"')
USING_PATTERN = re.compile(r"^\s*using\s+(?!var\b)([^=;]+);$", re.MULTILINE)
COMPLEXITY_PATTERNS = [
    re.compile(r"\bif\b"),
    re.compile(r"\bfor\b"),
    re.compile(r"\bforeach\b"),
    re.compile(r"\bwhile\b"),
    re.compile(r"\bcase\b"),
    re.compile(r"\bcatch\b"),
    re.compile(r"\bwhen\b"),
    re.compile(r"&&"),
    re.compile(r"\|\|"),
]
SCRIPT_RUNTIME_PATTERNS = [
    re.compile(r"\bNLua\b"),
    re.compile(r"\bPython\.Runtime\b"),
    re.compile(r"\bPythonEngine\b"),
    re.compile(r"\bPy\.[A-Za-z_]"),
    re.compile(r"\bnew\s+Lua\s*\("),
]


@dataclass
class MethodMetric:
    path: str
    member: str
    start_line: int
    end_line: int
    line_count: int
    cyclomatic_complexity: int


def github_request(url: str, token: str | None = None) -> Any:
    request = urllib.request.Request(
        url,
        headers={
            "Accept": "application/vnd.github+json",
            "User-Agent": "RegProbe-RepoMetrics/1.0",
            **({"Authorization": f"Bearer {token}"} if token else {}),
        },
    )
    with urllib.request.urlopen(request) as response:
        return json.load(response)


def fetch_latest_successful_run(owner: str, repo: str, workflow: str, branch: str, token: str | None) -> dict[str, Any]:
    url = (
        f"https://api.github.com/repos/{owner}/{repo}/actions/workflows/"
        f"{urllib.parse.quote(workflow)}/runs?branch={urllib.parse.quote(branch)}&status=success&per_page=1"
    )
    payload = github_request(url, token)
    runs = payload.get("workflow_runs") or []
    if not runs:
        raise RuntimeError(f"No successful workflow runs were found for {owner}/{repo} {workflow} on branch {branch}.")
    return runs[0]


def fetch_artifacts(owner: str, repo: str, run_id: int, token: str | None) -> list[dict[str, Any]]:
    url = f"https://api.github.com/repos/{owner}/{repo}/actions/runs/{run_id}/artifacts"
    payload = github_request(url, token)
    return payload.get("artifacts") or []


def is_zip_file(path: Path) -> bool:
    if not path.exists() or path.stat().st_size < 4:
        return False
    with path.open("rb") as handle:
        return handle.read(4) == b"PK\x03\x04"


def download_artifact(
    owner: str,
    repo: str,
    artifact: dict[str, Any],
    run_id: int,
    destination: Path,
    token: str | None,
) -> str:
    destination.parent.mkdir(parents=True, exist_ok=True)
    download_url = artifact["archive_download_url"]

    if token:
        request = urllib.request.Request(
            download_url,
            headers={
                "Accept": "application/vnd.github+json",
                "Authorization": f"Bearer {token}",
                "User-Agent": "RegProbe-RepoMetrics/1.0",
            },
        )
        try:
            with urllib.request.urlopen(request) as response, destination.open("wb") as handle:
                shutil.copyfileobj(response, handle)
            if is_zip_file(destination):
                return "github-api"
        except urllib.error.HTTPError:
            pass

    nightly_url = f"https://nightly.link/{owner}/{repo}/actions/runs/{run_id}/{artifact['name']}.zip"
    request = urllib.request.Request(nightly_url, headers={"User-Agent": "RegProbe-RepoMetrics/1.0"})
    with urllib.request.urlopen(request) as response, destination.open("wb") as handle:
        shutil.copyfileobj(response, handle)
    if not is_zip_file(destination):
        raise RuntimeError(f"Downloaded artifact is not a ZIP archive: {nightly_url}")
    return "nightly-link"


def extract_coverage_xml(artifact_zip: Path, scratch_dir: Path) -> tuple[Path, str]:
    with zipfile.ZipFile(artifact_zip) as archive:
        xml_members = [name for name in archive.namelist() if name.endswith("coverage.cobertura.xml")]
        if not xml_members:
            raise RuntimeError(f"No coverage.cobertura.xml file was found in {artifact_zip}.")
        xml_member = xml_members[0]
        archive.extract(xml_member, scratch_dir)
        return scratch_dir / xml_member, xml_member


def normalize_relative_path(path_value: str) -> str:
    return path_value.replace("\\", "/").lstrip("./")


def parse_condition_coverage(value: str | None) -> tuple[int, int]:
    if not value:
        return (0, 0)
    match = re.search(r"\((\d+)/(\d+)\)", value)
    if not match:
        return (0, 0)
    return (int(match.group(1)), int(match.group(2)))


def parse_cobertura_xml(xml_path: Path) -> dict[str, Any]:
    root = ET.fromstring(xml_path.read_bytes())
    totals = {
        "line_percent": round(float(root.attrib.get("line-rate", "0")) * 100, 2),
        "branch_percent": round(float(root.attrib.get("branch-rate", "0")) * 100, 2),
    }

    file_map: dict[str, dict[int, dict[str, int | bool]]] = defaultdict(dict)
    for class_element in root.findall(".//class"):
        file_name = normalize_relative_path(class_element.attrib.get("filename", "unknown"))
        for line in class_element.findall("./lines/line"):
            number = int(line.attrib.get("number", "0"))
            hits = int(line.attrib.get("hits", "0"))
            covered_branches, total_branches = parse_condition_coverage(line.attrib.get("condition-coverage"))
            is_branch = line.attrib.get("branch") == "true" or total_branches > 0
            existing = file_map[file_name].get(number)
            candidate = {
                "hits": hits,
                "is_branch": is_branch,
                "covered_branches": covered_branches,
                "total_branches": total_branches,
            }
            if existing is None:
                file_map[file_name][number] = candidate
                continue

            file_map[file_name][number] = {
                "hits": max(int(existing["hits"]), hits),
                "is_branch": bool(existing["is_branch"]) or is_branch,
                "covered_branches": max(int(existing["covered_branches"]), covered_branches),
                "total_branches": max(int(existing["total_branches"]), total_branches),
            }

    file_metrics = []
    for file_name, lines in file_map.items():
        total_lines = len(lines)
        covered_lines = sum(1 for line in lines.values() if int(line["hits"]) > 0)
        branch_lines = [line for line in lines.values() if bool(line["is_branch"]) or int(line["total_branches"]) > 0]
        covered_branches = sum(int(line["covered_branches"]) for line in branch_lines)
        total_branches = sum(int(line["total_branches"]) for line in branch_lines)
        file_metrics.append(
            {
                "path": file_name,
                "line_percent": round((covered_lines / total_lines) * 100, 2) if total_lines else 0.0,
                "branch_percent": round((covered_branches / total_branches) * 100, 2) if total_branches else 0.0,
                "line_hits": covered_lines,
                "line_total": total_lines,
                "branch_hits": covered_branches,
                "branch_total": total_branches,
            }
        )

    file_metrics.sort(key=lambda item: (item["line_percent"], item["branch_percent"], item["path"]))
    return {
        "totals": totals,
        "lowest_files": file_metrics[:5],
        "file_count": len(file_metrics),
    }


def iter_csharp_files(repo_root: Path) -> list[Path]:
    results: list[Path] = []
    for path in repo_root.rglob("*.cs"):
        if any(part in EXCLUDED_DIR_NAMES for part in path.parts):
            continue
        results.append(path)
    return results


def calculate_complexity(lines: list[str]) -> int:
    body = "\n".join(lines)
    complexity = 1
    for pattern in COMPLEXITY_PATTERNS:
        complexity += len(pattern.findall(body))
    return complexity


def extract_method_metrics(path: Path, repo_root: Path) -> list[MethodMetric]:
    lines = path.read_text(encoding="utf-8").splitlines()
    metrics: list[MethodMetric] = []
    pending: dict[str, Any] | None = None
    current: dict[str, Any] | None = None

    for index, line in enumerate(lines, start=1):
        stripped = line.strip()
        if current is None and pending is None:
            match = METHOD_PATTERN.match(line)
            if not match:
                continue
            member = match.group("name")
            if match.group("return") in {"record", "class", "interface", "struct", "enum"}:
                continue
            if member in METHOD_EXCLUSIONS:
                continue
            pending = {"member": member, "start": index, "brace_depth": 0, "lines": [line]}
            if "{" in line:
                pending["brace_depth"] = line.count("{") - line.count("}")
                current = pending
                pending = None
                if current["brace_depth"] <= 0:
                    metrics.append(
                        MethodMetric(
                            path=path.relative_to(repo_root).as_posix(),
                            member=current["member"],
                            start_line=current["start"],
                            end_line=index,
                            line_count=index - current["start"] + 1,
                            cyclomatic_complexity=calculate_complexity(current["lines"]),
                        )
                    )
                    current = None
            continue

        if pending is not None:
            pending["lines"].append(line)
            if "{" in line:
                pending["brace_depth"] += line.count("{") - line.count("}")
                current = pending
                pending = None
                if current["brace_depth"] <= 0:
                    metrics.append(
                        MethodMetric(
                            path=path.relative_to(repo_root).as_posix(),
                            member=current["member"],
                            start_line=current["start"],
                            end_line=index,
                            line_count=index - current["start"] + 1,
                            cyclomatic_complexity=calculate_complexity(current["lines"]),
                        )
                    )
                    current = None
            continue

        current["lines"].append(line)
        current["brace_depth"] += line.count("{") - line.count("}")
        if current["brace_depth"] <= 0:
            metrics.append(
                MethodMetric(
                    path=path.relative_to(repo_root).as_posix(),
                    member=current["member"],
                    start_line=current["start"],
                    end_line=index,
                    line_count=index - current["start"] + 1,
                    cyclomatic_complexity=calculate_complexity(current["lines"]),
                )
            )
            current = None

    return metrics


def build_cli_program_metrics(repo_root: Path) -> dict[str, Any]:
    program_path = repo_root / "cli" / "Program.cs"
    program_methods = extract_method_metrics(program_path, repo_root)
    program_text = program_path.read_text(encoding="utf-8")
    direct_namespace_dependencies = [match.group(1) for match in USING_PATTERN.finditer(program_text)]

    command_hotspots: list[dict[str, Any]] = []
    for path in sorted((repo_root / "cli" / "Commands").glob("*.cs")):
        for metric in extract_method_metrics(path, repo_root):
            command_hotspots.append(
                {
                    "path": metric.path,
                    "member": metric.member,
                    "line_count": metric.line_count,
                    "cyclomatic_complexity": metric.cyclomatic_complexity,
                }
            )
    command_hotspots.sort(
        key=lambda item: (-item["cyclomatic_complexity"], -item["line_count"], item["path"], item["member"])
    )

    average_length = round(
        sum(metric.line_count for metric in program_methods) / len(program_methods),
        2,
    ) if program_methods else 0.0

    return {
        "path": "cli/Program.cs",
        "method_count": len(program_methods),
        "average_method_length": average_length,
        "direct_namespace_dependencies": direct_namespace_dependencies,
        "methods": [
            {
                "member": metric.member,
                "line_count": metric.line_count,
                "cyclomatic_complexity": metric.cyclomatic_complexity,
                "start_line": metric.start_line,
            }
            for metric in program_methods
        ],
        "command_hotspots": command_hotspots[:5],
    }


def build_ui_hotspots(repo_root: Path) -> dict[str, Any]:
    hotspot_paths = [
        "app/Views/TweaksWorkspaceView.xaml",
        "app/Resources/TweaksWorkspaceResources.xaml",
        "app/ViewModels/TweaksViewModel.cs",
        "app/ViewModels/TweakItemViewModel.cs",
        "app/MainWindow.xaml",
        "app/MainWindow.xaml.cs",
    ]
    hotspots = []
    for relative_path in hotspot_paths:
        path = repo_root / relative_path
        if not path.exists():
            continue
        hotspots.append(
            {
                "path": relative_path,
                "line_count": len(path.read_text(encoding="utf-8").splitlines()),
            }
        )

    main_window_codebehind = repo_root / "app" / "MainWindow.xaml.cs"
    codebehind_text = main_window_codebehind.read_text(encoding="utf-8") if main_window_codebehind.exists() else ""

    return {
        "hotspots": hotspots,
        "main_window": {
            "xaml_lines": next((item["line_count"] for item in hotspots if item["path"] == "app/MainWindow.xaml"), 0),
            "codebehind_lines": next(
                (item["line_count"] for item in hotspots if item["path"] == "app/MainWindow.xaml.cs"),
                0,
            ),
            "event_handler_count": len(EVENT_HANDLER_PATTERN.findall(codebehind_text)),
            "mutable_state_field_count": len(MUTABLE_FIELD_PATTERN.findall(codebehind_text)),
        },
    }


def build_complexity_hotspots(repo_root: Path) -> list[dict[str, Any]]:
    metrics: list[MethodMetric] = []
    for path in iter_csharp_files(repo_root):
        metrics.extend(extract_method_metrics(path, repo_root))
    metrics.sort(
        key=lambda item: (-item.cyclomatic_complexity, -item.line_count, item.path, item.member)
    )
    return [
        {
            "path": metric.path,
            "member": metric.member,
            "line_count": metric.line_count,
            "cyclomatic_complexity": metric.cyclomatic_complexity,
            "start_line": metric.start_line,
        }
        for metric in metrics[:5]
    ]


def analyze_core_scripting_dependencies(repo_root: Path) -> dict[str, Any]:
    core_csproj = repo_root / "core" / "core.csproj"
    package_references: list[str] = []
    if core_csproj.exists():
        package_references = PACKAGE_REFERENCE_PATTERN.findall(core_csproj.read_text(encoding="utf-8"))

    active_usages: list[dict[str, Any]] = []
    for path in list(iter_csharp_files(repo_root)) + [core_csproj]:
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        for pattern in SCRIPT_RUNTIME_PATTERNS:
            for match in pattern.finditer(text):
                active_usages.append(
                    {
                        "path": path.relative_to(repo_root).as_posix(),
                        "match": match.group(0),
                    }
                )

    return {
        "core_package_references": package_references,
        "core_contains_nlua_reference": any(ref == "NLua" for ref in package_references),
        "core_contains_pythonnet_reference": any(ref == "pythonnet" for ref in package_references),
        "active_usage_count": len(active_usages),
        "active_usages": active_usages[:10],
    }


def build_report(
    repo_root: Path,
    owner: str,
    repo: str,
    workflow: str,
    branch: str,
    artifact_name: str,
    token: str | None,
) -> dict[str, Any]:
    run = fetch_latest_successful_run(owner, repo, workflow, branch, token)
    artifacts = fetch_artifacts(owner, repo, int(run["id"]), token)
    artifact = next((item for item in artifacts if item.get("name") == artifact_name), None)
    if artifact is None:
        available = ", ".join(sorted(item.get("name", "<unnamed>") for item in artifacts))
        raise RuntimeError(f"Artifact '{artifact_name}' was not found. Available artifacts: {available}")

    with tempfile.TemporaryDirectory(prefix="regprobe-repometrics-") as temp_dir:
        scratch_dir = Path(temp_dir)
        artifact_zip = scratch_dir / f"{artifact_name}.zip"
        download_strategy = download_artifact(owner, repo, artifact, int(run["id"]), artifact_zip, token)
        coverage_xml_path, coverage_member = extract_coverage_xml(artifact_zip, scratch_dir)
        coverage = parse_cobertura_xml(coverage_xml_path)

    return {
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "source": {
            "repository": f"{owner}/{repo}",
            "workflow": workflow,
            "branch": branch,
            "workflow_run_id": run["id"],
            "workflow_run_created_at": run.get("created_at"),
            "workflow_run_head_sha": run.get("head_sha"),
            "artifact": {
                "id": artifact.get("id"),
                "name": artifact.get("name"),
                "digest": artifact.get("digest"),
                "download_strategy": download_strategy,
                "coverage_member_path": coverage_member,
            },
        },
        "coverage": coverage,
        "complexity_hotspots": build_complexity_hotspots(repo_root),
        "cli_program": build_cli_program_metrics(repo_root),
        "ui_hotspots": build_ui_hotspots(repo_root),
        "core_scripting_dependencies": analyze_core_scripting_dependencies(repo_root),
    }


def render_markdown(report: dict[str, Any]) -> str:
    coverage = report["coverage"]
    lines = [
        "# Baseline Metrics",
        "",
        f"- Generated UTC: `{report['generated_utc']}`",
        f"- Source workflow run: `{report['source']['workflow_run_id']}` on `{report['source']['branch']}`",
        f"- Source commit: `{report['source']['workflow_run_head_sha']}`",
        f"- Coverage artifact: `{report['source']['artifact']['name']}` (`{report['source']['artifact']['id']}`)",
        f"- Coverage download path: `{report['source']['artifact']['download_strategy']}`",
        "",
        "## Coverage",
        "",
        f"- Total line coverage: `{coverage['totals']['line_percent']:.2f}%`",
        f"- Total branch coverage: `{coverage['totals']['branch_percent']:.2f}%`",
        "",
        "### Lowest Coverage Files",
    ]
    for item in coverage["lowest_files"]:
        lines.append(
            f"- `{item['path']}` — line `{item['line_percent']:.2f}%`, branch `{item['branch_percent']:.2f}%` "
            f"({item['line_hits']}/{item['line_total']} lines)"
        )

    lines.extend(["", "## Highest Complexity Methods", ""])
    for item in report["complexity_hotspots"]:
        lines.append(
            f"- `{item['path']}:{item['start_line']}` `{item['member']}` — complexity `{item['cyclomatic_complexity']}`, lines `{item['line_count']}`"
        )

    cli_program = report["cli_program"]
    lines.extend(
        [
            "",
            "## CLI Program.cs",
            "",
            f"- Method count: `{cli_program['method_count']}`",
            f"- Average method length: `{cli_program['average_method_length']}` lines",
            f"- Direct namespace dependencies: `{', '.join(cli_program['direct_namespace_dependencies'])}`",
            "",
            "### Command Hotspots",
        ]
    )
    for item in cli_program["command_hotspots"]:
        lines.append(
            f"- `{item['path']}` `{item['member']}` — complexity `{item['cyclomatic_complexity']}`, lines `{item['line_count']}`"
        )

    ui_hotspots = report["ui_hotspots"]
    lines.extend(["", "## UI Hotspots", ""])
    for item in ui_hotspots["hotspots"]:
        lines.append(f"- `{item['path']}` — `{item['line_count']}` lines")

    main_window = ui_hotspots["main_window"]
    lines.extend(
        [
            "",
            "### MainWindow",
            "",
            f"- XAML lines: `{main_window['xaml_lines']}`",
            f"- Code-behind lines: `{main_window['codebehind_lines']}`",
            f"- Event handlers in code-behind: `{main_window['event_handler_count']}`",
            f"- Mutable state fields in code-behind: `{main_window['mutable_state_field_count']}`",
        ]
    )

    scripting = report["core_scripting_dependencies"]
    lines.extend(
        [
            "",
            "## Core Scripting Dependencies",
            "",
            f"- `core/core.csproj` package references: `{', '.join(scripting['core_package_references']) or 'none'}`",
            f"- `NLua` present in Core project: `{scripting['core_contains_nlua_reference']}`",
            f"- `pythonnet` present in Core project: `{scripting['core_contains_pythonnet_reference']}`",
            f"- Active runtime usage matches in repo scan: `{scripting['active_usage_count']}`",
        ]
    )
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate RegProbe repo modernization baseline metrics.")
    parser.add_argument("--owner", default="siklone")
    parser.add_argument("--repo", default="RegProbe")
    parser.add_argument("--workflow", default="dotnet.yml")
    parser.add_argument("--branch", default="main")
    parser.add_argument("--artifact-name", default="coverage-report")
    parser.add_argument("--output-json", type=Path, default=REPO_ROOT / "Docs" / "metrics" / "baseline.json")
    parser.add_argument("--output-md", type=Path, default=REPO_ROOT / "Docs" / "metrics" / "baseline.md")
    args = parser.parse_args()

    token = os.environ.get("GITHUB_TOKEN")
    report = build_report(REPO_ROOT, args.owner, args.repo, args.workflow, args.branch, args.artifact_name, token)

    args.output_json.parent.mkdir(parents=True, exist_ok=True)
    args.output_md.parent.mkdir(parents=True, exist_ok=True)
    args.output_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    args.output_md.write_text(render_markdown(report), encoding="utf-8")

    print(
        json.dumps(
            {
                "output_json": args.output_json.relative_to(REPO_ROOT).as_posix(),
                "output_md": args.output_md.relative_to(REPO_ROOT).as_posix(),
                "workflow_run_id": report["source"]["workflow_run_id"],
                "line_percent": report["coverage"]["totals"]["line_percent"],
                "branch_percent": report["coverage"]["totals"]["branch_percent"],
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
