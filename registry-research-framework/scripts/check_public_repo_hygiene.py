#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
REPORT_PATH = AUDIT_DIR / "public-repo-hygiene-check.json"
MARKDOWN_PATH = AUDIT_DIR / "public-repo-hygiene-check.md"
ABSOLUTE_LOCAL_LINK_PATTERN = re.compile(r"\]\(([A-Za-z]:[\\/][^)]+)\)")


def iter_public_markdown_files(repo_root: Path) -> list[Path]:
    docs = [repo_root / "README.md", repo_root / "CONTRIBUTING.md", repo_root / "SECURITY.md"]
    docs.extend(sorted((repo_root / "Docs").rglob("*.md")))
    return [path for path in docs if path.exists()]


def find_absolute_local_path_violations(paths: list[Path], repo_root: Path) -> list[dict[str, Any]]:
    violations: list[dict[str, Any]] = []
    for path in paths:
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            for match in ABSOLUTE_LOCAL_LINK_PATTERN.finditer(line):
                violations.append(
                    {
                        "file": path.relative_to(repo_root).as_posix(),
                        "line": line_number,
                        "match": match.group(1),
                        "line_text": line.strip(),
                    }
                )
    return violations


def parse_push_branches(workflow_text: str) -> list[str]:
    match = re.search(r"push:\s*\n(?:[ \t]+.*\n)*?[ \t]+branches:\s*\[([^\]]+)\]", workflow_text, re.MULTILINE)
    if not match:
        return []
    return [item.strip().strip("'\"") for item in match.group(1).split(",") if item.strip()]


def build_public_repo_hygiene_report(repo_root: Path) -> dict[str, Any]:
    markdown_files = iter_public_markdown_files(repo_root)
    absolute_path_violations = find_absolute_local_path_violations(markdown_files, repo_root)

    security_path = repo_root / "SECURITY.md"
    readme_path = repo_root / "README.md"
    workflow_path = repo_root / ".github" / "workflows" / "dotnet.yml"
    placeholder_test_path = repo_root / "tests" / "UnitTest1.cs"

    workflow_push_branches = parse_push_branches(workflow_path.read_text(encoding="utf-8")) if workflow_path.exists() else []

    readme_text = readme_path.read_text(encoding="utf-8")

    checks = {
        "security_policy_present": security_path.exists(),
        "readme_has_product_entry": "## What RegProbe Does" in readme_text and "## Start Here" in readme_text,
        "workflow_push_main_only": workflow_push_branches == ["main"],
        "placeholder_unittest_removed": not placeholder_test_path.exists(),
        "absolute_local_paths_removed": not absolute_path_violations,
    }

    errors: list[str] = []
    if not checks["security_policy_present"]:
        errors.append("SECURITY.md is missing.")
    if not checks["readme_has_product_entry"]:
        errors.append("README.md is missing the product-first entry sections ('## What RegProbe Does' and '## Start Here').")
    if not checks["workflow_push_main_only"]:
        errors.append(f"Workflow push branches drifted from main-only policy: {workflow_push_branches or 'missing'}")
    if not checks["placeholder_unittest_removed"]:
        errors.append("tests/UnitTest1.cs is still present.")
    if absolute_path_violations:
        errors.append(
            f"Found {len(absolute_path_violations)} public markdown link(s) with workstation-specific absolute paths."
        )

    return {
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "check_status": "PASS" if not errors else "FAIL",
        "checks": checks,
        "workflow_push_branches": workflow_push_branches,
        "public_markdown_files": [path.relative_to(repo_root).as_posix() for path in markdown_files],
        "absolute_local_path_violations": absolute_path_violations,
        "errors": errors,
    }


def render_markdown(report: dict[str, Any]) -> str:
    lines = [
        "# Public Repo Hygiene Check",
        "",
        f"- Status: **{report['check_status']}**",
        f"- Generated UTC: `{report['generated_utc']}`",
        "",
        "## Checks",
    ]
    for key, value in report["checks"].items():
        lines.append(f"- `{key}`: `{value}`")
    if report["absolute_local_path_violations"]:
        lines.extend(["", "## Absolute Local Path Violations"])
        for item in report["absolute_local_path_violations"]:
            lines.append(f"- `{item['file']}:{item['line']}` -> `{item['match']}`")
    if report["errors"]:
        lines.extend(["", "## Errors"])
        for item in report["errors"]:
            lines.append(f"- {item}")
    return "\n".join(lines) + "\n"


def main() -> int:
    report = build_public_repo_hygiene_report(REPO_ROOT)
    AUDIT_DIR.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    MARKDOWN_PATH.write_text(render_markdown(report), encoding="utf-8")
    print(json.dumps({"report": REPORT_PATH.as_posix(), "status": report["check_status"]}, ensure_ascii=False, indent=2))
    return 0 if report["check_status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
