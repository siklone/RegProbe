#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import PROMOTION_GATES_PATH, load_json, load_json_if_exists, write_json  # noqa: E402

AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
JSON_OUTPUT = AUDIT_ROOT / "v36-clean-state-report.json"
MARKDOWN_OUTPUT = AUDIT_ROOT / "v36-clean-state-report.md"
FINAL_STATS_OUTPUT = REPO_ROOT / "research" / "research-v36-final-stats.json"

REJECTED_LEDGER_PATH = AUDIT_ROOT / "rejected-closure-ledger.json"
PROMOTION_REVIEW_PACK_PATH = AUDIT_ROOT / "promotion-eligible-review-pack.json"
BLOCKED_WORKLIST_PATH = AUDIT_ROOT / "blocked-worklist.json"
APP_READINESS_PATH = AUDIT_ROOT / "app-retest-readiness-latest.json"


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def as_int(value: Any) -> int:
    try:
        return int(value or 0)
    except (TypeError, ValueError):
        return 0


def as_dict(value: Any) -> dict[str, Any]:
    return value if isinstance(value, dict) else {}


def relative(path: Path) -> str:
    return str(path.relative_to(REPO_ROOT)).replace("\\", "/")


def load_inputs() -> dict[str, Any]:
    return {
        "promotion_gates": load_json(PROMOTION_GATES_PATH),
        "rejected_ledger": load_json_if_exists(REJECTED_LEDGER_PATH) or {},
        "promotion_review_pack": load_json_if_exists(PROMOTION_REVIEW_PACK_PATH) or {},
        "blocked_worklist": load_json_if_exists(BLOCKED_WORKLIST_PATH) or {},
        "app_readiness": load_json_if_exists(APP_READINESS_PATH) or {},
    }


def build_report(
    promotion_gates: dict[str, Any],
    rejected_ledger: dict[str, Any],
    promotion_review_pack: dict[str, Any],
    blocked_worklist: dict[str, Any],
    app_readiness: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or utc_now_iso()
    gate_summary = as_dict(promotion_gates.get("summary"))
    state_counts = as_dict(gate_summary.get("promotion_state_counts"))
    rejected_summary = as_dict(rejected_ledger.get("summary"))
    review_metadata = as_dict(promotion_review_pack.get("metadata"))
    readiness_summary = as_dict(app_readiness.get("summary"))
    readiness_reports = as_dict(app_readiness.get("reports"))
    evidence_surfaces = as_dict(readiness_reports.get("evidence_surfaces"))
    evidence_surface_summary = as_dict(evidence_surfaces.get("summary"))

    promoted = as_int(state_counts.get("promoted"))
    rejected = as_int(state_counts.get("rejected"))
    blocked = as_int(state_counts.get("blocked"))
    promotion_eligible = as_int(state_counts.get("promotion-eligible"))
    revalidation_pending = as_int(state_counts.get("revalidation-pending"))
    invalid_gate_entries = as_int(gate_summary.get("invalid_gate_entries"))
    unclassified_rejected = as_int(rejected_summary.get("unclassified_rejected"))
    promotion_review_records = as_int(review_metadata.get("total_records"))
    blocked_worklist_count = as_int(blocked_worklist.get("blocked_count"))
    app_readiness_status = str(app_readiness.get("check_status") or "UNKNOWN")
    all_records_classified = (
        as_int(gate_summary.get("total_records")) > 0
        and promoted + rejected == as_int(gate_summary.get("total_records"))
    )

    active_backlog = (
        blocked
        + promotion_eligible
        + revalidation_pending
        + invalid_gate_entries
        + unclassified_rejected
        + promotion_review_records
    )
    checks = {
        "no_blocked_gate_entries": blocked == 0,
        "no_revalidation_pending_gate_entries": revalidation_pending == 0,
        "no_promotion_eligible_gate_entries": promotion_eligible == 0,
        "no_invalid_gate_entries": invalid_gate_entries == 0,
        "no_unclassified_rejected_records": unclassified_rejected == 0,
        "no_promotion_review_records": promotion_review_records == 0,
        "blocked_worklist_empty": blocked_worklist_count == 0,
        "app_retest_readiness_pass": app_readiness_status == "PASS",
        "all_records_classified": all_records_classified,
    }
    status = "clean-state" if all(checks.values()) else "attention-needed"

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "campaign_id": "v36-repository-zero-pending",
        "status": status,
        "summary": {
            "total_records": as_int(gate_summary.get("total_records")),
            "promoted": promoted,
            "rejected": rejected,
            "blocked": blocked,
            "promotion_eligible": promotion_eligible,
            "revalidation_pending": revalidation_pending,
            "invalid_gate_entries": invalid_gate_entries,
            "unclassified_rejected": unclassified_rejected,
            "active_backlog": active_backlog,
            "limbo_count": active_backlog,
            "app_surface_entry_count": as_int(readiness_summary.get("app_surface_entry_count")),
            "apply_allowed_record_count": as_int(readiness_summary.get("apply_allowed_record_count")),
            "records_missing_validation_proof": as_int(evidence_surface_summary.get("records_missing_validation_proof")),
        },
        "checks": checks,
        "sources": {
            "promotion_gates": relative(PROMOTION_GATES_PATH),
            "rejected_closure_ledger": relative(REJECTED_LEDGER_PATH),
            "promotion_eligible_review_pack": relative(PROMOTION_REVIEW_PACK_PATH),
            "blocked_worklist": relative(BLOCKED_WORKLIST_PATH),
            "app_retest_readiness": relative(APP_READINESS_PATH),
        },
        "rejected_archive": {
            "total_rejected": as_int(rejected_summary.get("total_rejected")),
            "evidence_backed_rejected": as_int(rejected_summary.get("evidence_backed_rejected")),
            "deprecated_records": as_int(rejected_summary.get("deprecated_records")),
            "closure_status_counts": as_dict(rejected_summary.get("closure_status_counts")),
            "closure_kind_counts": as_dict(rejected_summary.get("closure_kind_counts")),
        },
        "verification": {
            "app_retest_readiness": app_readiness_status,
            "blocked_worklist_count": blocked_worklist_count,
            "kvm_app_smoke_status": readiness_summary.get("kvm_app_smoke_status"),
            "kvm_lane_health_status": readiness_summary.get("kvm_lane_health_status"),
            "missing_rollback_story_count": as_int(readiness_summary.get("missing_rollback_story_count")),
        },
        "next_phase_recommendations": [
            {
                "id": "qga-etw-ghidra-backfill",
                "priority": "optional",
                "summary": "Backfill deeper ETW/Ghidra bundles for promoted records if the VM transport lane needs more proof density.",
            },
            {
                "id": "v37-candidate-discovery",
                "priority": "optional",
                "summary": "Start a new candidate wave only after preserving this v36 zero-pending snapshot.",
            },
            {
                "id": "app-retest",
                "priority": "recommended",
                "summary": "Use the app retest readiness report before manual Windows validation of cards, evidence drawers, apply, verify, and rollback.",
            },
        ],
    }


def markdown_cell(value: Any) -> str:
    if value is None:
        return ""
    return str(value).replace("|", "\\|").replace("\n", " ").replace("`", "\\`")


def bool_label(value: bool) -> str:
    return "PASS" if value else "FAIL"


def render_markdown(report: dict[str, Any]) -> str:
    summary = as_dict(report.get("summary"))
    checks = as_dict(report.get("checks"))
    archive = as_dict(report.get("rejected_archive"))
    verification = as_dict(report.get("verification"))
    sources = as_dict(report.get("sources"))
    lines = [
        "# V36 Clean State Report",
        "",
        f"Generated: `{report.get('generated_utc')}`",
        f"Campaign: `{report.get('campaign_id')}`",
        f"Status: `{report.get('status')}`",
        "",
        "This report is the zero-pending snapshot for the v3.6 research surface. It combines promotion gates, rejected closure lanes, the promotion review pack, the blocked worklist, and app retest readiness into one audit contract.",
        "",
        "## Dashboard",
        "",
        "| Metric | Value |",
        "|---|---:|",
        f"| Total records | {as_int(summary.get('total_records'))} |",
        f"| Promoted | {as_int(summary.get('promoted'))} |",
        f"| Rejected | {as_int(summary.get('rejected'))} |",
        f"| Blocked | {as_int(summary.get('blocked'))} |",
        f"| Promotion eligible | {as_int(summary.get('promotion_eligible'))} |",
        f"| Revalidation pending | {as_int(summary.get('revalidation_pending'))} |",
        f"| Invalid gate entries | {as_int(summary.get('invalid_gate_entries'))} |",
        f"| Unclassified rejected | {as_int(summary.get('unclassified_rejected'))} |",
        f"| Active backlog | {as_int(summary.get('active_backlog'))} |",
        f"| Limbo count | {as_int(summary.get('limbo_count'))} |",
        f"| App surface entries | {as_int(summary.get('app_surface_entry_count'))} |",
        f"| Apply-allowed records | {as_int(summary.get('apply_allowed_record_count'))} |",
        f"| Records missing validation proof | {as_int(summary.get('records_missing_validation_proof'))} |",
        "",
        "## Clean-State Checks",
        "",
        "| Check | Result |",
        "|---|---:|",
    ]
    for key, value in checks.items():
        lines.append(f"| `{markdown_cell(key)}` | `{bool_label(bool(value))}` |")

    lines.extend(
        [
            "",
            "## Rejected Archive",
            "",
            "| Metric | Value |",
            "|---|---:|",
            f"| Total rejected | {as_int(archive.get('total_rejected'))} |",
            f"| Evidence-backed rejected | {as_int(archive.get('evidence_backed_rejected'))} |",
            f"| Deprecated records | {as_int(archive.get('deprecated_records'))} |",
            "",
            "### Closure Kind Counts",
            "",
            "| Closure kind | Count |",
            "|---|---:|",
        ]
    )
    for kind, count in as_dict(archive.get("closure_kind_counts")).items():
        lines.append(f"| `{markdown_cell(kind)}` | {as_int(count)} |")

    lines.extend(
        [
            "",
            "## Verification",
            "",
            "| Surface | Value |",
            "|---|---|",
        ]
    )
    for key, value in verification.items():
        lines.append(f"| `{markdown_cell(key)}` | `{markdown_cell(value)}` |")

    lines.extend(
        [
            "",
            "## Source Artifacts",
            "",
            "| Artifact | Path |",
            "|---|---|",
        ]
    )
    for key, value in sources.items():
        lines.append(f"| `{markdown_cell(key)}` | `{markdown_cell(value)}` |")

    lines.extend(
        [
            "",
            "## Next Phase",
            "",
        ]
    )
    for item in report.get("next_phase_recommendations") or []:
        lines.append(
            f"- `{markdown_cell(item.get('id'))}` ({markdown_cell(item.get('priority'))}): {markdown_cell(item.get('summary'))}"
        )
    lines.append("")
    return "\n".join(lines)


def write_outputs(report: dict[str, Any]) -> None:
    write_json(JSON_OUTPUT, report)
    write_json(FINAL_STATS_OUTPUT, report)
    MARKDOWN_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    MARKDOWN_OUTPUT.write_text(render_markdown(report), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the v36 zero-pending clean-state report.")
    parser.add_argument("--emit-json", action="store_true", help="Print summary JSON.")
    args = parser.parse_args()

    inputs = load_inputs()
    report = build_report(**inputs)
    write_outputs(report)

    if args.emit_json:
        print(json.dumps({"status": report["status"], **report["summary"]}, ensure_ascii=False, indent=2))
    else:
        print(f"Wrote {JSON_OUTPUT}")
        print(f"Wrote {MARKDOWN_OUTPUT}")
        print(f"Wrote {FINAL_STATS_OUTPUT}")
        print(json.dumps({"status": report["status"], **report["summary"]}, ensure_ascii=False, indent=2))
    return 0 if report["status"] == "clean-state" else 1


if __name__ == "__main__":
    raise SystemExit(main())
