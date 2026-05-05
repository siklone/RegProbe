#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import re
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
DEFAULT_APP_EXE = r"C:\Tools\AppSmoke\RegProbe.App.exe"
DEFAULT_GUEST_OUTPUT_DIR = r"C:\Tools\ValidationController\smoke"
DEFAULT_GUEST_QA_SCRIPT = r".\scripts\vm\guest-app-tweak-qa.ps1"
DEFAULT_KVM_BATCH_SCRIPT = "scripts/vm-kvm/run-guest-app-tweak-qa-batch.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


single_tweak_check = load_module(
    "check_single_tweak_for_app_qa",
    FRAMEWORK_SCRIPTS / "check_single_tweak.py",
)


def normalize_text(value: Any) -> str:
    return str(value or "").strip()


def sanitize_file_token(value: str) -> str:
    sanitized = re.sub(r"[^A-Za-z0-9_.-]+", "-", normalize_text(value)).strip("-")
    return sanitized or "tweak"


def format_windows_output_path(output_dir: str, tweak_id: str) -> str:
    directory = normalize_text(output_dir).rstrip("\\/")
    if not directory:
        directory = DEFAULT_GUEST_OUTPUT_DIR
    return f"{directory}\\{sanitize_file_token(tweak_id)}.qa.json"


def quote_powershell(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def render_direct_app_command(app_exe: str, tweak_id: str, output_path: str, *, skip_rollback: bool) -> str:
    args = [
        "&",
        quote_powershell(app_exe),
        "--tweaks",
        "--qa-run-tweak",
        quote_powershell(tweak_id),
        "--qa-output",
        quote_powershell(output_path),
        "--qa-shutdown",
    ]
    if skip_rollback:
        args.append("--qa-skip-rollback")
    return " ".join(args)


def render_guest_vm_command(guest_script: str, tweak_id: str, output_path: str, *, skip_rollback: bool) -> str:
    args = [
        "powershell",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        quote_powershell(str(guest_script)),
        "-TweakId",
        quote_powershell(tweak_id),
        "-OutputPath",
        quote_powershell(output_path),
    ]
    if skip_rollback:
        args.append("-SkipRollback")
    return " ".join(args)


def render_kvm_batch_command(kvm_batch_script: str, tweak_id: str) -> str:
    return (
        "python3 "
        + quote_powershell(kvm_batch_script)
        + " --id "
        + quote_powershell(tweak_id)
    )


def build_expected_report_contract(match: dict[str, Any], *, skip_rollback: bool) -> dict[str, Any]:
    if not bool(match.get("apply_allowed")):
        return {
            "success": False,
            "status": "mutation-blocked",
            "rollback_requested": False,
            "required_stages": ["detect-before"],
            "required_stage_assertions": [
                "detect-before is present",
                "apply is not expected while mutation stays blocked",
            ],
        }

    stages = ["detect-before", "apply", "detect-after"]
    required_assertions = [
        "apply stage reports a successful apply story",
        "detect-after is present",
    ]
    if not skip_rollback:
        stages = ["detect-before", "apply", "rollback", "detect-after"]
        required_assertions.insert(1, "rollback stage reports a successful rollback story")

    return {
        "success": True,
        "status": "ok",
        "rollback_requested": not skip_rollback,
        "required_stages": stages,
        "required_stage_assertions": required_assertions,
    }


def build_expected_value_summary(match: dict[str, Any]) -> list[str]:
    summaries: list[str] = []
    for item in match.get("expected_value_checks") or []:
        expected_value = normalize_text(item.get("expected_value"))
        if not expected_value:
            continue
        summaries.append(
            f"{expected_value} -> {'matched' if item.get('found_any') else 'not found'}"
        )
    return summaries


def build_candidate_plan(
    match: dict[str, Any],
    *,
    app_exe: str,
    guest_output_dir: str,
    guest_user: str,
    guest_script: str,
    kvm_batch_script: str,
    query: str,
    expected_values: list[str],
) -> dict[str, Any]:
    tweak_id = normalize_text(match.get("tweak_id")) or normalize_text(match.get("candidate_id")) or normalize_text(match.get("record_id"))
    record_id = normalize_text(match.get("record_id"))
    output_path = format_windows_output_path(guest_output_dir, tweak_id)
    surface = match.get("app_surface_entry") or {}
    catalog = match.get("catalog_entry") or {}
    card_name = normalize_text(surface.get("name")) or normalize_text(catalog.get("name")) or tweak_id
    card_category = normalize_text(surface.get("category")) or normalize_text(catalog.get("category"))
    card_description = normalize_text(surface.get("description")) or normalize_text(catalog.get("description"))
    documentation = normalize_text(surface.get("documentation")) or normalize_text(catalog.get("docs")) or normalize_text(match.get("record_file"))
    evidence_locations = [
        normalize_text(item.get("location"))
        for item in (match.get("evidence") or [])
        if isinstance(item, dict) and normalize_text(item.get("location"))
    ]

    direct_app_command = render_direct_app_command(app_exe, tweak_id, output_path, skip_rollback=False)
    direct_app_command_skip_rollback = render_direct_app_command(app_exe, tweak_id, output_path, skip_rollback=True)
    guest_vm_command = render_guest_vm_command(guest_script, tweak_id, output_path, skip_rollback=False)
    guest_vm_command_skip_rollback = render_guest_vm_command(guest_script, tweak_id, output_path, skip_rollback=True)
    kvm_batch_command = render_kvm_batch_command(kvm_batch_script, tweak_id)

    inspect_command = [
        "dotnet",
        "run",
        "--project",
        "cli/cli.csproj",
        "--",
        "research",
        "inspect",
        query,
    ]
    for expected_value in expected_values:
        inspect_command.extend(["--expected-value", expected_value])

    plan = {
        "candidate_id": normalize_text(match.get("candidate_id")) or tweak_id,
        "record_id": record_id,
        "tweak_id": tweak_id,
        "promotion_state": normalize_text(match.get("promotion_state")),
        "record_status": normalize_text(match.get("record_status")),
        "apply_allowed": bool(match.get("apply_allowed")),
        "restore_default_supported": bool(match.get("restore_default_supported")),
        "restore_previous_supported": bool(match.get("restore_previous_supported")),
        "app_mapping_status": normalize_text(match.get("app_mapping_status")),
        "card_expectations": {
            "name": card_name,
            "category": card_category,
            "description": card_description,
            "documentation": documentation,
        },
        "rollback_expectations": {
            "restore_default_supported": bool(match.get("restore_default_supported")),
            "restore_previous_supported": bool(match.get("restore_previous_supported")),
        },
        "value_expectations": build_expected_value_summary(match),
        "evidence_expectations": {
            "linked_evidence_count": len(match.get("evidence") or []),
            "runtime_read_signal_count": len(match.get("runtime_read_signals") or []),
            "linked_evidence_locations": evidence_locations[:5],
        },
        "commands": {
            "inspect": " ".join(quote_powershell(arg) if " " in arg or "\\" in arg else arg for arg in inspect_command),
            "readiness": "dotnet run --project cli/cli.csproj -- research readiness",
            "direct_app": direct_app_command,
            "direct_app_skip_rollback": direct_app_command_skip_rollback,
            "guest_vm": guest_vm_command,
            "guest_vm_skip_rollback": guest_vm_command_skip_rollback,
            "kvm_batch": kvm_batch_command,
        },
        "qa_report_path": output_path,
        "guest_user": guest_user,
        "expected_report": build_expected_report_contract(match, skip_rollback=False),
        "expected_report_skip_rollback": build_expected_report_contract(match, skip_rollback=True),
        "operator_checklist": [
            "Run the inspect command and confirm the expected values and tracked registry targets still match the record.",
            "Run the readiness command before launching the desktop app so cards, evidence, rollback coverage, and KVM smoke status stay green.",
            f"Open the app card '{card_name}' and verify the title, category, and linked research record ({documentation}) match this plan.",
            "Run the direct app or guest VM QA command and keep the JSON report it writes.",
            "Check the report fields Success, Status, RollbackRequested, and the stage list before trusting the result.",
            "If the normal run fails only because you need to observe the post-apply state manually, rerun the skip-rollback variant and record that fact in your notes.",
        ],
    }
    return plan


def build_single_tweak_app_qa_report(
    query: str,
    *,
    expected_values: list[str] | None = None,
    exact: bool = False,
    limit: int = 3,
    app_exe: str = DEFAULT_APP_EXE,
    guest_output_dir: str = DEFAULT_GUEST_OUTPUT_DIR,
    guest_user: str = "rai",
    repo_root: Path | None = None,
) -> dict[str, Any]:
    root = repo_root or REPO_ROOT
    expected_values = [normalize_text(value) for value in (expected_values or []) if normalize_text(value)]
    inspect_limit = max(limit, 8)
    inspect_report = single_tweak_check.build_single_tweak_report(
        query,
        expected_values=expected_values,
        exact=exact,
        limit=inspect_limit,
        repo_root=root,
    )

    report: dict[str, Any] = {
        "query": normalize_text(query),
        "expected_values": expected_values,
        "exact": exact,
        "limit": limit,
        "status": "ok",
        "inspect_status": inspect_report.get("status"),
        "inspect_match_count": inspect_report.get("match_count"),
        "qa_candidate_count": 0,
        "candidates": [],
        "sources": inspect_report.get("sources") or {},
    }

    matches = inspect_report.get("matches") or []
    if inspect_report.get("status") == "error":
        report["status"] = "error"
        report["error"] = inspect_report.get("error") or "single tweak inspection failed"
        return report

    if not matches:
        report["status"] = "no-match"
        return report

    candidates: list[dict[str, Any]] = []
    seen_tweak_ids: set[str] = set()
    for match in matches:
        tweak_id = normalize_text(match.get("tweak_id")) or normalize_text(match.get("candidate_id")) or normalize_text(match.get("record_id"))
        if not tweak_id:
            continue
        normalized_tweak_id = tweak_id.lower()
        if normalized_tweak_id in seen_tweak_ids:
            continue
        surface = match.get("app_surface_entry") or {}
        catalog = match.get("catalog_entry") or {}
        if not surface.get("present") and not catalog:
            continue
        candidates.append(
            build_candidate_plan(
                match,
                app_exe=app_exe,
                guest_output_dir=guest_output_dir,
                guest_user=guest_user,
                guest_script=DEFAULT_GUEST_QA_SCRIPT,
                kvm_batch_script=DEFAULT_KVM_BATCH_SCRIPT,
                query=normalize_text(query),
                expected_values=expected_values,
            )
        )
        seen_tweak_ids.add(normalized_tweak_id)
        if len(candidates) >= max(1, limit):
            break

    if not candidates:
        report["status"] = "no-app-card-match"
        report["closest_matches"] = [
            {
                "candidate_id": normalize_text(match.get("candidate_id")),
                "record_id": normalize_text(match.get("record_id")),
                "tweak_id": normalize_text(match.get("tweak_id")),
                "promotion_state": normalize_text(match.get("promotion_state")),
                "app_surface_present": bool((match.get("app_surface_entry") or {}).get("present")),
            }
            for match in matches[: max(1, limit)]
        ]
        return report

    report["qa_candidate_count"] = len(candidates)
    report["candidates"] = candidates
    report["summary"] = {
        "apply_allowed_candidate_count": sum(1 for item in candidates if item.get("apply_allowed")),
        "blocked_candidate_count": sum(1 for item in candidates if not item.get("apply_allowed")),
        "candidate_ids": [item.get("candidate_id") for item in candidates],
    }
    return report


def render_single_tweak_app_qa_report(report: dict[str, Any]) -> str:
    lines = [
        f"Query: {report.get('query')}",
        f"Status: {report.get('status')}",
        f"Inspect status: {report.get('inspect_status')}",
        f"Inspect matches: {report.get('inspect_match_count')}",
        f"QA candidates: {report.get('qa_candidate_count')}",
    ]

    if report.get("expected_values"):
        lines.append("Expected values: " + ", ".join(report["expected_values"]))

    if report.get("status") == "error":
        lines.append(f"Error: {report.get('error')}")
        return "\n".join(lines)

    if report.get("status") == "no-match":
        lines.append("No matching tweak, record, value name, or path was found.")
        return "\n".join(lines)

    if report.get("status") == "no-app-card-match":
        lines.append("Matches were found, but none of them currently resolve to a shipped app card.")
        for item in report.get("closest_matches") or []:
            lines.append(
                "- "
                + f"{item.get('candidate_id')} | tweak_id={item.get('tweak_id')} | "
                + f"promotion={item.get('promotion_state')} | "
                + f"app_surface_present={str(bool(item.get('app_surface_present'))).lower()}"
            )
        return "\n".join(lines)

    for index, candidate in enumerate(report.get("candidates") or [], start=1):
        card = candidate.get("card_expectations") or {}
        expected_report = candidate.get("expected_report") or {}
        expected_report_skip = candidate.get("expected_report_skip_rollback") or {}
        lines.extend(
            [
                "",
                f"[{index}] {candidate.get('candidate_id')}",
                "  card: "
                + f"{card.get('name') or candidate.get('tweak_id')}"
                + (f" [{card.get('category')}]" if card.get("category") else ""),
                "  promotion: "
                + f"{candidate.get('promotion_state') or 'unknown'} | "
                + f"apply_allowed={str(bool(candidate.get('apply_allowed'))).lower()}",
                "  rollback: "
                + f"restore_default={str(bool(candidate.get('restore_default_supported'))).lower()} | "
                + f"restore_previous={str(bool(candidate.get('restore_previous_supported'))).lower()}",
                f"  research_doc: {card.get('documentation')}",
            ]
        )
        if card.get("description"):
            lines.append(f"  card_description: {card.get('description')}")
        if candidate.get("value_expectations"):
            lines.append("  expected_values:")
            for summary in candidate["value_expectations"]:
                lines.append(f"    - {summary}")
        lines.extend(
            [
                "  commands:",
                f"    - inspect: {candidate['commands']['inspect']}",
                f"    - readiness: {candidate['commands']['readiness']}",
                f"    - direct_app: {candidate['commands']['direct_app']}",
                f"    - direct_app_skip_rollback: {candidate['commands']['direct_app_skip_rollback']}",
                f"    - guest_vm: {candidate['commands']['guest_vm']}",
                f"    - guest_vm_skip_rollback: {candidate['commands']['guest_vm_skip_rollback']}",
                f"    - kvm_batch: {candidate['commands']['kvm_batch']}",
                f"  qa_report_path: {candidate.get('qa_report_path')}",
                "  expected_report:",
                "    - "
                + f"Success={str(bool(expected_report.get('success'))).lower()} | "
                + f"Status={expected_report.get('status')} | "
                + f"RollbackRequested={str(bool(expected_report.get('rollback_requested'))).lower()}",
                "    - required_stages: " + ", ".join(expected_report.get("required_stages") or []),
                "  expected_report_skip_rollback:",
                "    - "
                + f"Success={str(bool(expected_report_skip.get('success'))).lower()} | "
                + f"Status={expected_report_skip.get('status')} | "
                + f"RollbackRequested={str(bool(expected_report_skip.get('rollback_requested'))).lower()}",
                "    - required_stages: " + ", ".join(expected_report_skip.get("required_stages") or []),
            ]
        )
        evidence = candidate.get("evidence_expectations") or {}
        if evidence.get("linked_evidence_locations"):
            lines.append("  evidence_locations:")
            for location in evidence["linked_evidence_locations"]:
                lines.append(f"    - {location}")
        lines.append("  operator_checklist:")
        for item in candidate.get("operator_checklist") or []:
            lines.append(f"    - {item}")

    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a manual desktop-app QA plan for one tweak, record, registry value name, or registry path query."
    )
    parser.add_argument("query", help="Tweak id, record id, registry value name, or registry path fragment")
    parser.add_argument(
        "--expected-value",
        action="append",
        default=[],
        help="Optional value expectation to surface in the QA plan",
    )
    parser.add_argument("--exact", action="store_true", help="Require exact token matches instead of substring matches")
    parser.add_argument("--limit", type=int, default=3, help="Maximum number of QA candidates to emit")
    parser.add_argument("--json", action="store_true", help="Emit the QA plan as JSON")
    parser.add_argument("--app-exe", default=DEFAULT_APP_EXE, help="Windows app executable path to use in the direct-launch plan")
    parser.add_argument(
        "--guest-output-dir",
        default=DEFAULT_GUEST_OUTPUT_DIR,
        help="Windows directory where the QA JSON report should be written",
    )
    parser.add_argument("--guest-user", default="rai", help="Guest user name documented in the plan")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.limit <= 0:
        raise SystemExit("--limit must be greater than 0")

    report = build_single_tweak_app_qa_report(
        args.query,
        expected_values=args.expected_value,
        exact=args.exact,
        limit=args.limit,
        app_exe=args.app_exe,
        guest_output_dir=args.guest_output_dir,
        guest_user=args.guest_user,
    )

    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print(render_single_tweak_app_qa_report(report))

    return 0 if report.get("status") not in {"error"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
