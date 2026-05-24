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
GITHUB_ACTION_SHA_PATTERN = re.compile(r"^[0-9a-fA-F]{40}$")
REMOTE_GITHUB_ACTION_REF_PATTERN = re.compile(r"^\s*(?:-\s*)?uses:\s*([^#\s]+)")
COMPARATIVE_PROSE_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("useful", re.compile(r"\buseful\b", re.IGNORECASE)),
    ("strongest", re.compile(r"\bstrongest\b", re.IGNORECASE)),
    ("preferred", re.compile(r"\bpreferred\b", re.IGNORECASE)),
    ("recommended", re.compile(r"\brecommended\b", re.IGNORECASE)),
    ("better understood", re.compile(r"\bbetter understood\b", re.IGNORECASE)),
    ("good fit", re.compile(r"\bgood fit\b", re.IGNORECASE)),
    ("worth keeping", re.compile(r"\bworth keeping\b", re.IGNORECASE)),
    ("next useful", re.compile(r"\bnext useful\b", re.IGNORECASE)),
)
COMPARATIVE_PROSE_SCAN_FILES: tuple[str, ...] = (
    "README.md",
    "CONTRIBUTING.md",
    "SECURITY.md",
    "Docs/SETTINGS_EXPANSION_REPORT_2026-03-09.md",
    "Docs/UPSTREAM_CONFIGURATION_AUDIT_2026-03-09.md",
    "Docs/UPSTREAM_CONFIGURATION_SOURCES.md",
    "Docs/UPSTREAM_TRANCHE_EVALUATION_2026-03-09.md",
    "Docs/product/support-matrix.md",
    "Docs/research/how-to-read-a-record.md",
    "Docs/security/use-case-guide.md",
    "Docs/visibility/use-case-guide.md",
)


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


def iter_comparative_prose_scan_files(repo_root: Path) -> list[Path]:
    return [repo_root / relative_path for relative_path in COMPARATIVE_PROSE_SCAN_FILES if (repo_root / relative_path).exists()]


def find_comparative_prose_violations(paths: list[Path], repo_root: Path) -> list[dict[str, Any]]:
    violations: list[dict[str, Any]] = []
    for path in paths:
        in_fence = False
        for line_number, raw_line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            stripped = raw_line.strip()
            if stripped.startswith("```"):
                in_fence = not in_fence
                continue
            if in_fence or not stripped:
                continue
            scrubbed = re.sub(r"`[^`]*`", "", raw_line)
            for label, pattern in COMPARATIVE_PROSE_PATTERNS:
                if pattern.search(scrubbed):
                    violations.append(
                        {
                            "file": path.relative_to(repo_root).as_posix(),
                            "line": line_number,
                            "pattern": label,
                            "line_text": stripped,
                        }
                    )
                    break
    return violations


def parse_push_branches(workflow_text: str) -> list[str]:
    match = re.search(r"push:\s*\n(?:[ \t]+.*\n)*?[ \t]+branches:\s*\[([^\]]+)\]", workflow_text, re.MULTILINE)
    if not match:
        return []
    return [item.strip().strip("'\"") for item in match.group(1).split(",") if item.strip()]


def find_unpinned_workflow_actions(workflow_paths: list[Path], repo_root: Path) -> list[dict[str, Any]]:
    violations: list[dict[str, Any]] = []
    for path in workflow_paths:
        for line_number, raw_line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            match = REMOTE_GITHUB_ACTION_REF_PATTERN.search(raw_line)
            if not match:
                continue

            action_ref = match.group(1).strip().strip("'\"")
            if action_ref.startswith("./") or action_ref.startswith("docker://"):
                continue

            if "@" not in action_ref:
                violations.append(
                    {
                        "file": path.relative_to(repo_root).as_posix(),
                        "line": line_number,
                        "action": action_ref,
                        "reason": "missing_ref",
                    }
                )
                continue

            _, ref = action_ref.rsplit("@", 1)
            if not GITHUB_ACTION_SHA_PATTERN.fullmatch(ref):
                violations.append(
                    {
                        "file": path.relative_to(repo_root).as_posix(),
                        "line": line_number,
                        "action": action_ref,
                        "reason": "ref_is_not_full_commit_sha",
                    }
                )
    return violations


def build_public_repo_hygiene_report(repo_root: Path) -> dict[str, Any]:
    markdown_files = iter_public_markdown_files(repo_root)
    absolute_path_violations = find_absolute_local_path_violations(markdown_files, repo_root)
    comparative_prose_scan_files = iter_comparative_prose_scan_files(repo_root)
    comparative_prose_violations = find_comparative_prose_violations(comparative_prose_scan_files, repo_root)

    security_path = repo_root / "SECURITY.md"
    readme_path = repo_root / "README.md"
    contributing_path = repo_root / "CONTRIBUTING.md"
    workflow_path = repo_root / ".github" / "workflows" / "dotnet.yml"
    placeholder_test_path = repo_root / "tests" / "UnitTest1.cs"
    codeowners_path = repo_root / ".github" / "CODEOWNERS"
    pr_template_path = repo_root / ".github" / "PULL_REQUEST_TEMPLATE.md"
    cli_docs_path = repo_root / "Docs" / "product" / "cli.md"
    support_matrix_path = repo_root / "Docs" / "product" / "support-matrix.md"
    media_doc_path = repo_root / "Docs" / "product" / "media.md"
    issue_template_dir = repo_root / ".github" / "ISSUE_TEMPLATE"
    required_issue_templates = [
        issue_template_dir / "bug-report.yml",
        issue_template_dir / "feature-request.yml",
        issue_template_dir / "research-finding.yml",
    ]

    workflow_paths = sorted((repo_root / ".github" / "workflows").glob("*.yml")) + sorted(
        (repo_root / ".github" / "workflows").glob("*.yaml")
    )
    workflow_push_branches = parse_push_branches(workflow_path.read_text(encoding="utf-8")) if workflow_path.exists() else []
    unpinned_workflow_actions = find_unpinned_workflow_actions(workflow_paths, repo_root)

    readme_text = readme_path.read_text(encoding="utf-8")
    contributing_text = contributing_path.read_text(encoding="utf-8") if contributing_path.exists() else ""
    pr_template_text = pr_template_path.read_text(encoding="utf-8") if pr_template_path.exists() else ""
    user_guide_path = repo_root / "Docs" / "product" / "user-guide.md"
    user_guide_text = user_guide_path.read_text(encoding="utf-8") if user_guide_path.exists() else ""
    cli_docs_text = cli_docs_path.read_text(encoding="utf-8") if cli_docs_path.exists() else ""
    media_doc_text = media_doc_path.read_text(encoding="utf-8") if media_doc_path.exists() else ""
    bug_report_text = (issue_template_dir / "bug-report.yml").read_text(encoding="utf-8") if (issue_template_dir / "bug-report.yml").exists() else ""
    feature_request_text = (issue_template_dir / "feature-request.yml").read_text(encoding="utf-8") if (issue_template_dir / "feature-request.yml").exists() else ""

    checks = {
        "security_policy_present": security_path.exists(),
        "readme_has_product_entry": "## What RegProbe Does" in readme_text and "## Start Here" in readme_text,
        "workflow_push_main_only": workflow_push_branches == ["main"],
        "github_actions_pinned_to_shas": not unpinned_workflow_actions,
        "placeholder_unittest_removed": not placeholder_test_path.exists(),
        "absolute_local_paths_removed": not absolute_path_violations,
        "comparative_public_prose_removed": not comparative_prose_violations,
        "issue_templates_present": all(path.exists() for path in required_issue_templates),
        "pr_template_present": pr_template_path.exists(),
        "codeowners_present": codeowners_path.exists(),
        "cli_docs_present": cli_docs_path.exists(),
        "support_matrix_present": support_matrix_path.exists(),
        "media_doc_present": media_doc_path.exists(),
        "readme_surface_names_current": all(token in readme_text for token in ("`Tweaks`", "`Recovery`", "`Diagnostics`")) and "`Configuration` is the main workspace" not in readme_text,
        "user_guide_surface_names_current": all(token in user_guide_text for token in ("`Tweaks`", "`Recovery`", "`Diagnostics`")) and "`Configuration` is the main tweak workspace" not in user_guide_text,
        "readme_mentions_research_checks": "research inspect" in readme_text and "research readiness" in readme_text,
        "contributing_mentions_research_checks": "research inspect" in contributing_text and "research readiness" in contributing_text,
        "cli_docs_mentions_research_checks": "research inspect" in cli_docs_text and "research readiness" in cli_docs_text,
        "readme_mentions_app_qa_plan": "research qa-plan" in readme_text,
        "contributing_mentions_app_qa_plan": "research qa-plan" in contributing_text,
        "cli_docs_mentions_app_qa_plan": "research qa-plan" in cli_docs_text,
        "readme_mentions_app_qa_batch": "research qa-batch" in readme_text,
        "contributing_mentions_app_qa_batch": "research qa-batch" in contributing_text,
        "cli_docs_mentions_app_qa_batch": "research qa-batch" in cli_docs_text,
        "contributing_has_safe_flow_expectations": "Detect -> Apply -> Verify -> Rollback" in contributing_text and "integration coverage" in contributing_text,
        "contributing_has_media_lane_expectations": "Docs/product/media.md" in contributing_text,
        "contributing_has_cli_docs_expectations": "Docs/product/cli.md" in contributing_text,
        "contributing_has_release_doc_expectations": "Docs/product/support-matrix.md" in contributing_text,
        "pr_template_has_safe_flow_check": "SAFE Flow Impact" in pr_template_text and "integration coverage should be updated" in pr_template_text,
        "pr_template_has_media_and_release_checks": "Screenshot or media lane updated if UI changed" in pr_template_text and "Support matrix or release docs updated if package contract changed" in pr_template_text,
        "issue_templates_use_current_surface_names": all(token in bug_report_text for token in ("Tweaks", "Recovery", "Diagnostics")) and all(token in feature_request_text for token in ("Tweaks", "Recovery", "Diagnostics")),
        "media_doc_has_refresh_rules": "When To Refresh" in media_doc_text and "do not merge a UI rename" in media_doc_text,
    }

    errors: list[str] = []
    if not checks["security_policy_present"]:
        errors.append("SECURITY.md is missing.")
    if not checks["readme_has_product_entry"]:
        errors.append("README.md is missing the product-first entry sections ('## What RegProbe Does' and '## Start Here').")
    if not checks["workflow_push_main_only"]:
        errors.append(f"Workflow push branches drifted from main-only policy: {workflow_push_branches or 'missing'}")
    if unpinned_workflow_actions:
        errors.append(
            f"Found {len(unpinned_workflow_actions)} GitHub Action reference(s) not pinned to full commit SHAs."
        )
    if not checks["placeholder_unittest_removed"]:
        errors.append("tests/UnitTest1.cs is still present.")
    if absolute_path_violations:
        errors.append(
            f"Found {len(absolute_path_violations)} public markdown link(s) with workstation-specific absolute paths."
        )
    if comparative_prose_violations:
        errors.append(
            f"Found {len(comparative_prose_violations)} comparative repo-authored prose hit(s) in the guarded public-doc set."
        )
    if not checks["issue_templates_present"]:
        errors.append("Required issue templates are missing under .github/ISSUE_TEMPLATE/.")
    if not checks["pr_template_present"]:
        errors.append(".github/PULL_REQUEST_TEMPLATE.md is missing.")
    if not checks["codeowners_present"]:
        errors.append(".github/CODEOWNERS is missing.")
    if not checks["cli_docs_present"]:
        errors.append("Docs/product/cli.md is missing.")
    if not checks["support_matrix_present"]:
        errors.append("Docs/product/support-matrix.md is missing.")
    if not checks["media_doc_present"]:
        errors.append("Docs/product/media.md is missing.")
    if not checks["readme_surface_names_current"]:
        errors.append("README.md still drifts from the shipped Tweaks/Recovery/Diagnostics surface language.")
    if not checks["user_guide_surface_names_current"]:
        errors.append("Docs/product/user-guide.md still drifts from the shipped Tweaks/Recovery/Diagnostics surface language.")
    if not checks["readme_mentions_research_checks"]:
        errors.append("README.md is missing the research inspect or research readiness examples.")
    if not checks["contributing_mentions_research_checks"]:
        errors.append("CONTRIBUTING.md is missing the research inspect or research readiness workflow.")
    if not checks["cli_docs_mentions_research_checks"]:
        errors.append("Docs/product/cli.md is missing the research inspect or research readiness command coverage.")
    if not checks["readme_mentions_app_qa_plan"]:
        errors.append("README.md is missing the research qa-plan workflow.")
    if not checks["contributing_mentions_app_qa_plan"]:
        errors.append("CONTRIBUTING.md is missing the research qa-plan workflow.")
    if not checks["cli_docs_mentions_app_qa_plan"]:
        errors.append("Docs/product/cli.md is missing the research qa-plan command coverage.")
    if not checks["readme_mentions_app_qa_batch"]:
        errors.append("README.md is missing the research qa-batch workflow.")
    if not checks["contributing_mentions_app_qa_batch"]:
        errors.append("CONTRIBUTING.md is missing the research qa-batch workflow.")
    if not checks["cli_docs_mentions_app_qa_batch"]:
        errors.append("Docs/product/cli.md is missing the research qa-batch command coverage.")
    if not checks["contributing_has_safe_flow_expectations"]:
        errors.append("CONTRIBUTING.md no longer carries the SAFE flow integration expectation.")
    if not checks["contributing_has_media_lane_expectations"]:
        errors.append("CONTRIBUTING.md is missing the product media lane expectation.")
    if not checks["contributing_has_cli_docs_expectations"]:
        errors.append("CONTRIBUTING.md is missing the CLI docs update expectation.")
    if not checks["contributing_has_release_doc_expectations"]:
        errors.append("CONTRIBUTING.md is missing the release/support-matrix update expectation.")
    if not checks["pr_template_has_safe_flow_check"]:
        errors.append("PULL_REQUEST_TEMPLATE.md is missing the SAFE flow integration reminder.")
    if not checks["pr_template_has_media_and_release_checks"]:
        errors.append("PULL_REQUEST_TEMPLATE.md is missing the media or release contract checklist items.")
    if not checks["issue_templates_use_current_surface_names"]:
        errors.append("Issue templates drifted from the shipped Tweaks/Recovery/Diagnostics surface names.")
    if not checks["media_doc_has_refresh_rules"]:
        errors.append("Docs/product/media.md is missing the media refresh or rename-drift rules.")

    return {
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "check_status": "PASS" if not errors else "FAIL",
        "checks": checks,
        "workflow_push_branches": workflow_push_branches,
        "unpinned_workflow_actions": unpinned_workflow_actions,
        "required_issue_templates": [path.relative_to(repo_root).as_posix() for path in required_issue_templates],
        "public_markdown_files": [path.relative_to(repo_root).as_posix() for path in markdown_files],
        "comparative_prose_scan_files": [path.relative_to(repo_root).as_posix() for path in comparative_prose_scan_files],
        "comparative_prose_violations": comparative_prose_violations,
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
    if report["comparative_prose_violations"]:
        lines.extend(["", "## Comparative Prose Violations"])
        for item in report["comparative_prose_violations"]:
            lines.append(f"- `{item['file']}:{item['line']}` -> `{item['pattern']}` -> {item['line_text']}")
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
