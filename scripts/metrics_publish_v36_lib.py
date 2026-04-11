from __future__ import annotations

import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from research_v36_lib import FRAMEWORK_ROOT, RESEARCH_ROOT, load_json_if_exists

METRICS_ROOT = FRAMEWORK_ROOT / "metrics"
OPERATIONAL_METRICS_PATH = METRICS_ROOT / "operational-metrics.json"
GATE_METRICS_PATH = METRICS_ROOT / "gate-metrics.json"
PUBLISH_METRICS_PATH = METRICS_ROOT / "publish-metrics.json"
README_PATH = FRAMEWORK_ROOT / "README.md"
README_BLOCK_START = "<!-- BEGIN:RESEARCH_HEALTH -->"
README_BLOCK_END = "<!-- END:RESEARCH_HEALTH -->"

DEFAULT_GATE_THRESHOLDS = {
    "max_stale_promoted": 0,
    "max_invalid_gate_entries": 0,
    "min_schema_complete_ratio": 0.95,
}

BENCH_PENDING_BLOCKERS = {"bench-not-run", "bench-bare-metal-pending"}


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def load_json_dict(path: Path) -> dict[str, Any]:
    payload = load_json_if_exists(path)
    return payload if isinstance(payload, dict) else {}


def normalize_blocker_name(blocker: Any) -> str:
    raw = str(blocker or "").strip()
    if not raw:
        return ""
    lowered = raw.lower()
    if raw == "documentation-first-review":
        return raw
    if raw == "stale-evidence":
        return raw
    if raw in {"rollback-unverified", "rollback-failed", "dead-link", "bench-not-run", "bench-failed-safety", "bench-bare-metal-pending", "functional-no-effect"}:
        return raw
    if raw == "runtime_no_read" or "no-runtime-proof" in lowered or "runtime no read" in lowered:
        return "no-runtime-proof"
    if raw == "conflicting-sources" or "conflict" in lowered:
        return "conflicting-sources"
    if raw == "schema-version-unsupported":
        return raw
    if raw == "path_context_unclear" or "ambiguous" in lowered:
        return "ambiguous-value-semantics"
    if raw == "privilege-dependent":
        return raw
    if raw == "build-specific-behavior":
        return raw
    if raw == "no-rollback-proof":
        return raw
    if len(raw) > 80:
        return "record-blocker"
    return raw


def normalized_blocker_breakdown(entries: list[dict[str, Any]]) -> dict[str, int]:
    counts: Counter[str] = Counter()
    for entry in entries:
        for blocker in entry.get("promotion_blockers") or []:
            normalized = normalize_blocker_name(blocker)
            if normalized:
                counts[normalized] += 1
    return dict(sorted(counts.items()))


def stale_records(entries: list[dict[str, Any]]) -> list[dict[str, Any]]:
    result = []
    for entry in entries:
        if entry.get("stale_reason") or entry.get("revalidation_need") == "required":
            result.append(
                {
                    "candidate_id": entry.get("record_id") or entry.get("tweak_id"),
                    "promotion_state": entry.get("promotion_state"),
                    "stale_reason": entry.get("stale_reason"),
                    "last_known_good_build": entry.get("last_known_good_build"),
                }
            )
    return result


def conflict_records(entries: list[dict[str, Any]]) -> list[dict[str, Any]]:
    result = []
    for entry in entries:
        if entry.get("conflict_reason"):
            result.append(
                {
                    "candidate_id": entry.get("record_id") or entry.get("tweak_id"),
                    "promotion_state": entry.get("promotion_state"),
                    "conflict_reason": entry.get("conflict_reason"),
                }
            )
    return result


def rollback_unverified_records(entries: list[dict[str, Any]]) -> list[dict[str, Any]]:
    result = []
    for entry in entries:
        status = str(entry.get("rollback_verification_status") or "")
        if status and status != "verified":
            result.append(
                {
                    "candidate_id": entry.get("record_id") or entry.get("tweak_id"),
                    "promotion_state": entry.get("promotion_state"),
                    "rollback_verification_status": status,
                }
            )
    return result


def bench_pending_records(entries: list[dict[str, Any]]) -> list[dict[str, Any]]:
    result = []
    for entry in entries:
        blockers = {normalize_blocker_name(item) for item in (entry.get("promotion_blockers") or [])}
        blockers.discard("")
        if blockers & BENCH_PENDING_BLOCKERS:
            result.append(
                {
                    "candidate_id": entry.get("record_id") or entry.get("tweak_id"),
                    "promotion_state": entry.get("promotion_state"),
                    "bench_status": entry.get("bench_status"),
                    "promotion_blockers": sorted(blockers & BENCH_PENDING_BLOCKERS),
                }
            )
    return result


def dead_link_count_from_entries(entries: list[dict[str, Any]]) -> int:
    return sum(int(entry.get("dead_link_count") or 0) for entry in entries)


def bench_required_and_executed(entries: list[dict[str, Any]]) -> tuple[int, int]:
    required = 0
    executed = 0
    for entry in entries:
        status = entry.get("bench_status")
        if isinstance(status, dict):
            if status.get("required"):
                required += 1
            if status.get("executed"):
                executed += 1
    return required, executed


def build_gate_metrics(
    gate_payload: dict[str, Any],
    audit_payload: dict[str, Any],
    validation_summary: dict[str, Any],
    generated_at: str | None = None,
    thresholds: dict[str, Any] | None = None,
) -> dict[str, Any]:
    entries = list(audit_payload.get("entries") or [])
    generated_at = generated_at or utc_now_iso()
    thresholds = dict(DEFAULT_GATE_THRESHOLDS | (thresholds or {}))

    total_active = max(len(entries), 1)
    invalid_gate_entries = int((gate_payload.get("summary") or {}).get("invalid_gate_entries") or 0)
    invalid_count = int(validation_summary.get("invalid_count") or 0)
    schema_complete_ratio = round((len(entries) - invalid_count) / total_active, 4)
    stale_promoted_count = sum(
        1
        for entry in entries
        if str(entry.get("promotion_state") or "") == "promoted"
        and (entry.get("stale_reason") or entry.get("revalidation_need") == "required")
    )
    rollback_unverified_count = len(rollback_unverified_records(entries))
    no_runtime_proof_count = sum(
        1
        for entry in entries
        if "no-runtime-proof" in {normalize_blocker_name(item) for item in (entry.get("promotion_blockers") or [])}
    )
    missing_rollback_count = sum(
        1
        for entry in gate_payload.get("entries") or []
        if str(entry.get("promotion_state") or "") != "rejected"
        and not bool((entry.get("rollback_status") or {}).get("rollback_value"))
        and "no-rollback-proof" not in {normalize_blocker_name(item) for item in (entry.get("promotion_blockers") or [])}
        and "rollback-unverified" not in {normalize_blocker_name(item) for item in (entry.get("promotion_blockers") or [])}
        and "rollback-failed" not in {normalize_blocker_name(item) for item in (entry.get("promotion_blockers") or [])}
    )
    bench_not_run_count = len(bench_pending_records(entries))
    dead_link_count = dead_link_count_from_entries(entries)

    threshold_violations: list[str] = []
    if stale_promoted_count > float(thresholds["max_stale_promoted"]):
        threshold_violations.append("stale_promoted")
    if invalid_gate_entries > float(thresholds["max_invalid_gate_entries"]):
        threshold_violations.append("invalid_gate_entries")
    if schema_complete_ratio < float(thresholds["min_schema_complete_ratio"]):
        threshold_violations.append("schema_complete_ratio")

    return {
        "schema_complete_ratio": schema_complete_ratio,
        "missing_rollback_count": missing_rollback_count,
        "rollback_unverified_count": rollback_unverified_count,
        "no_runtime_proof_count": no_runtime_proof_count,
        "stale_promoted_count": stale_promoted_count,
        "dead_link_count": dead_link_count,
        "bench_not_run_count": bench_not_run_count,
        "invalid_gate_entries": invalid_gate_entries,
        "thresholds": thresholds,
        "threshold_violations": threshold_violations,
        "generated_at": generated_at,
    }


def determine_gate_health(
    gate_metrics: dict[str, Any],
    validation_summary: dict[str, Any],
    bench_pending_count: int,
) -> str:
    if gate_metrics.get("threshold_violations") or int(gate_metrics.get("stale_promoted_count") or 0) > 0 or int(gate_metrics.get("invalid_gate_entries") or 0) > 0:
        return "red"
    if int(validation_summary.get("missing_docs_count") or 0) > 0 or bench_pending_count > 0:
        return "yellow"
    return "green"


def build_operational_metrics(
    queue_payload: dict[str, Any],
    gate_payload: dict[str, Any],
    audit_payload: dict[str, Any],
    validation_summary: dict[str, Any],
    gate_metrics: dict[str, Any],
    generated_at: str | None = None,
) -> dict[str, Any]:
    generated_at = generated_at or utc_now_iso()
    entries = list(audit_payload.get("entries") or [])
    queue_summary = queue_payload.get("summary") or {}
    state_counts = dict(queue_summary.get("state_counts") or {})
    total_discovered = int(queue_summary.get("total_entries") or len(queue_payload.get("entries") or []))
    total_discarded = int(state_counts.get("discarded") or 0)
    total_triaged = max(total_discovered - total_discarded, 0)
    required_bench_count, executed_bench_count = bench_required_and_executed(gate_payload.get("entries") or [])
    bench_completion_rate = 1.0 if required_bench_count == 0 else round(executed_bench_count / required_bench_count, 4)

    return {
        "total_discovered": total_discovered,
        "total_triaged": total_triaged,
        "total_discarded": total_discarded,
        "total_blocked": int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("blocked") or 0),
        "total_promoted": int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("promoted") or 0),
        "total_rejected": int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("rejected") or 0),
        "revalidation_pending": int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("revalidation-pending") or 0),
        "stale_count": len(stale_records(entries)),
        "blocker_breakdown": normalized_blocker_breakdown(entries),
        "bench_completion_rate": bench_completion_rate,
        "missing_docs_count": int(validation_summary.get("missing_docs_count") or 0),
        "generated_at": generated_at,
    }


def build_publish_metrics(
    gate_payload: dict[str, Any],
    audit_payload: dict[str, Any],
    validation_summary: dict[str, Any],
    gate_metrics: dict[str, Any],
    generated_at: str | None = None,
) -> dict[str, Any]:
    generated_at = generated_at or utc_now_iso()
    entries = list(audit_payload.get("entries") or [])
    stale = stale_records(entries)
    rollback_unverified = rollback_unverified_records(entries)
    bench_pending = bench_pending_records(entries)
    health = determine_gate_health(gate_metrics, validation_summary, len(bench_pending))

    return {
        "promoted_candidate_count": int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("promoted") or 0),
        "blocked_candidate_count": int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("blocked") or 0),
        "revalidation_pending_count": int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("revalidation-pending") or 0),
        "stale_candidate_count": len(stale),
        "rollback_verification_health": f"{len(entries) - len(rollback_unverified)} verified-or-known, {len(rollback_unverified)} pending/failing",
        "bench_safety_summary": f"{len(bench_pending)} bench-pending, {int(gate_metrics.get('bench_not_run_count') or 0)} blocker-counted",
        "verification_health_summary": f"gate={health}, schema_complete_ratio={gate_metrics.get('schema_complete_ratio')}, missing_docs={int(validation_summary.get('missing_docs_count') or 0)}",
        "generated_at": generated_at,
    }


def build_final_audit_payload(
    base_audit: dict[str, Any],
    gate_metrics: dict[str, Any],
    validation_summary: dict[str, Any],
) -> dict[str, Any]:
    entries = list(base_audit.get("entries") or [])
    stale = stale_records(entries)
    conflicts = conflict_records(entries)
    rollback_unverified = rollback_unverified_records(entries)
    bench_pending = bench_pending_records(entries)
    missing_docs = [
        {
            "candidate_id": detail.get("candidate_id"),
            "promotion_state": detail.get("promotion_state"),
            "documentation_issues": detail.get("documentation_issues") or [],
        }
        for detail in validation_summary.get("details") or []
        if detail.get("missing_docs")
    ]
    summary = dict(base_audit.get("summary") or {})
    promotion_counts = dict(summary.get("promotion_state_counts") or {})
    health = determine_gate_health(gate_metrics, validation_summary, len(bench_pending))
    summary.update(
        {
            "total": len(entries),
            "promoted": int(promotion_counts.get("promoted") or 0),
            "blocked": int(promotion_counts.get("blocked") or 0),
            "revalidation_pending": int(promotion_counts.get("revalidation-pending") or 0),
            "stale": len(stale),
            "gate_health": health,
        }
    )
    payload = dict(base_audit)
    payload.update(
        {
            "summary": summary,
            "blocker_breakdown": normalized_blocker_breakdown(entries),
            "stale_records": stale,
            "conflict_records": conflicts,
            "missing_docs_records": missing_docs,
            "rollback_unverified_records": rollback_unverified,
            "bench_pending_records": bench_pending,
        }
    )
    return payload


def research_health_markdown(publish_metrics: dict[str, Any], gate_metrics: dict[str, Any], validation_summary: dict[str, Any], gate_health: str) -> str:
    icon = {"green": "🟢", "yellow": "🟡", "red": "🔴"}.get(gate_health, "⚪")
    schema_percent = round(float(gate_metrics.get("schema_complete_ratio") or 0) * 100)
    return "\n".join(
        [
            README_BLOCK_START,
            "## Research Health",
            "",
            "| Metric | Value |",
            "|--------|-------|",
            f"| Promoted | {publish_metrics.get('promoted_candidate_count', 0)} |",
            f"| Blocked | {publish_metrics.get('blocked_candidate_count', 0)} |",
            f"| Revalidation Pending | {publish_metrics.get('revalidation_pending_count', publish_metrics.get('stale_candidate_count', 0))} |",
            f"| Gate Health | {icon} {gate_health} |",
            f"| Schema Complete | {schema_percent}% |",
            f"| Missing Docs | {int(validation_summary.get('missing_docs_count') or 0)} |",
            README_BLOCK_END,
        ]
    )


def update_readme_summary_block(readme_path: Path, block_text: str) -> str:
    current = readme_path.read_text(encoding="utf-8") if readme_path.exists() else "# Registry Research Framework\n"
    if README_BLOCK_START in current and README_BLOCK_END in current:
        start = current.index(README_BLOCK_START)
        end = current.index(README_BLOCK_END) + len(README_BLOCK_END)
        updated = current[:start].rstrip() + "\n\n" + block_text + current[end:]
    else:
        updated = current.rstrip() + "\n\n" + block_text + "\n"
    readme_path.write_text(updated.rstrip() + "\n", encoding="utf-8", newline="\n")
    return updated


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
