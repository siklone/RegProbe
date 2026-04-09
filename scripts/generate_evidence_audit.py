#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from evidence_class_lib import (
    boot_phase_relevant,
    GHIDRA_EVIDENCE_KINDS,
    build_class_entry,
    classification_layers,
    cross_layer_satisfied,
    determine_evidence_lane,
    evidence_items,
    evidence_kind,
    extract_app_status,
    has_benchmark_evidence,
    has_converged_vm_evidence,
    has_ghidra_evidence,
    has_official_evidence,
    has_procmon_evidence,
    has_reboot_evidence,
    has_wpr_evidence,
    load_json,
    load_overrides,
    load_provenance_map,
    next_missing_layer,
    restore_story_known,
    sanitize_value,
    suspected_layer,
)
from research_v36_lib import PROMOTION_GATES_PATH, build_sku_awareness, default_execution_context, load_json_if_exists
from research_path_lib import REPO_ROOT, RESEARCH_ROOT, V31_EVIDENCE_ROOT, is_github_release_url, normalize_reference

RECORDS_DIR = RESEARCH_ROOT / "records"
PROVENANCE_PATH = REPO_ROOT / "Docs" / "tweaks" / "tweak-provenance.json"
OVERRIDES_PATH = RESEARCH_ROOT / "evidence-class-overrides.json"
INCIDENTS_PATH = RESEARCH_ROOT / "vm-incidents.json"
OUTPUT_PATH = RESEARCH_ROOT / "evidence-audit.json"
GHIDRA_PATH_RE = re.compile(r"(?:research/evidence-files|evidence/files)/ghidra/[^\s);,]+")


def load_incident_map(path: Path) -> dict[str, list[dict[str, Any]]]:
    if not path.exists():
        return {}

    payload = load_json(path)
    incidents = payload.get("incidents") or []
    result: dict[str, list[dict[str, Any]]] = {}
    for incident in incidents:
        if not isinstance(incident, dict):
            continue
        for key_name in ("record_id", "tweak_id"):
            key = str(incident.get(key_name) or "").strip()
            if not key:
                continue
            result.setdefault(key, []).append(incident)
    return result


def load_promotion_gate_map(path: Path) -> dict[str, dict[str, Any]]:
    payload = load_json_if_exists(path)
    if not isinstance(payload, dict):
        return {}
    result: dict[str, dict[str, Any]] = {}
    for entry in payload.get("entries") or []:
        if not isinstance(entry, dict):
            continue
        for key_name in ("record_id", "tweak_id", "candidate_id"):
            key = str(entry.get(key_name) or "").strip()
            if key and key not in result:
                result[key] = entry
    return result


def has_ghidra_no_function_fallback(record: dict[str, Any]) -> bool:
    candidate_files: set[Path] = set()

    for item in evidence_items(record):
        if evidence_kind(item) not in GHIDRA_EVIDENCE_KINDS:
            continue

        location = str(item.get("location") or "")
        for match in GHIDRA_PATH_RE.findall(location):
            candidate = REPO_ROOT / normalize_reference(match)
            evidence_path = candidate.parent / "evidence.json" if candidate.suffix else candidate / "evidence.json"
            candidate_files.add(evidence_path)

    for path in candidate_files:
        if not path.exists():
            continue
        payload = load_json(path)
        if payload.get("ghidra_no_function_fallback") is True:
            return True

    return False


def v31_full_evidence_path(record_id: str) -> Path:
    return V31_EVIDENCE_ROOT / record_id / "full-evidence.json"


def load_v31_artifact_refs(record_id: str) -> list[dict[str, Any]]:
    path = v31_full_evidence_path(record_id)
    if not path.exists():
        return []

    try:
        payload = load_json(path)
    except Exception:
        return []
    artifact_refs = payload.get("artifact_refs") or []
    valid_refs: list[dict[str, Any]] = []
    for item in artifact_refs:
        if not isinstance(item, dict):
            continue
        storage_kind = str(item.get("storage_kind") or "").strip().lower()
        release_url = str(item.get("release_url") or "").strip()
        if storage_kind == "release" and release_url and not is_github_release_url(release_url):
            continue
        valid_refs.append(item)
    return sanitize_value(valid_refs)


def load_v31_full_evidence(record_id: str) -> dict[str, Any] | None:
    path = v31_full_evidence_path(record_id)
    if not path.exists():
        return None
    try:
        payload = load_json(path)
    except Exception:
        return None
    return payload if isinstance(payload, dict) else None


def audit_surface_from_gate(record: dict[str, Any], promotion_gate: dict[str, Any], full_evidence: dict[str, Any] | None) -> dict[str, Any]:
    full_evidence = full_evidence or {}
    freshness_status = promotion_gate.get("freshness_status") or {}
    rollback_status = promotion_gate.get("rollback_status") or {}
    bench_status = promotion_gate.get("bench_status") or {}
    negative_status = promotion_gate.get("negative_evidence_status") or {}
    url_validation = promotion_gate.get("url_validation_status") or {}
    verification_context = promotion_gate.get("verification_context") or {}
    build_sku = full_evidence.get("build_sku_awareness") or build_sku_awareness(record, default_execution_context())
    conflict_reason = negative_status.get("conflict_reason")
    if not conflict_reason and "conflicting-sources" in (promotion_gate.get("promotion_blockers") or []):
        conflict_reason = "conflicting-sources"
    return {
        "stale_reason": freshness_status.get("stale_reason"),
        "last_known_good_build": freshness_status.get("last_known_good_build") or verification_context.get("tested_build"),
        "revalidation_need": "required" if freshness_status.get("revalidation_needed") else "none",
        "rollback_verification_status": (
            "verified"
            if rollback_status.get("rollback_verified")
            else "failed"
            if rollback_status.get("rollback_failure_reason") == "rollback-state-mismatch"
            else "unverified"
        ),
        "bench_status": (
            "failed-safety"
            if "bench-failed-safety" in (negative_status.get("signals") or [])
            else "executed"
            if bench_status.get("executed")
            else "not-run"
        ),
        "conflict_reason": conflict_reason,
        "dead_link_count": url_validation.get("dead_link_count") or 0,
        "last_known_good_verification_context": freshness_status.get("last_known_good_verification_context") or verification_context,
        "os_build": build_sku.get("os_build"),
        "os_edition": build_sku.get("os_edition"),
        "architecture": build_sku.get("architecture"),
        "elevation_context": build_sku.get("elevation_context"),
        "machine_user_scope": build_sku.get("machine_user_scope"),
    }


def re_audit_completed(record_id: str, class_id: str, official: bool) -> bool:
    if official or class_id != "A":
        return False

    payload = load_v31_full_evidence(record_id)
    if not payload:
        return False

    re_audit = payload.get("re_audit") or {}
    classification = payload.get("classification") or {}
    runtime = payload.get("runtime") or {}
    etw = runtime.get("etw") or {}
    return (
        re_audit.get("is_re_audit") is True
        and str(re_audit.get("new_pipeline_version") or "") in {"v3.1", "v3.2"}
        and bool(re_audit.get("new_cross_layer"))
        and bool(re_audit.get("dead_flag_four_conditions_met"))
        and (bool(re_audit.get("new_tools_applied")) or bool(etw.get("executed")))
        and str(classification.get("class") or "") == "A"
    )


def is_final_decision_gate(record: dict[str, Any], class_id: str, incident_seen: bool) -> bool:
    if class_id != "B":
        return False

    decision = record.get("decision") or {}
    blocking_issues = decision.get("blocking_issues") or []
    if blocking_issues:
        return False

    return next_missing_layer(record, incident_seen=incident_seen) == "decision-gate"


def etw_executed(record_id: str, record: dict[str, Any]) -> bool:
    path = v31_full_evidence_path(record_id)
    if path.exists():
        payload = load_json(path)
        runtime = payload.get("runtime") or {}
        etw = runtime.get("etw") or {}
        if etw.get("executed") is True:
            return True
    return any(evidence_kind(item) == "etw-trace" for item in evidence_items(record))


def tools_used(record_id: str, record: dict[str, Any]) -> list[str]:
    tools: list[str] = []
    if has_official_evidence(record):
        tools.append("official-doc")
    if etw_executed(record_id, record):
        tools.append("etw")
    if has_procmon_evidence(record):
        tools.append("procmon")
    if has_ghidra_evidence(record):
        tools.append("ghidra")
    if has_ghidra_no_function_fallback(record):
        tools.append("ghidra_no_function_fallback")
    if has_wpr_evidence(record):
        tools.append("wpr")
    if has_benchmark_evidence(record):
        tools.append("benchmark")
    if has_reboot_evidence(record):
        tools.append("reboot")
    return tools


def dead_flag_checks(record_id: str, record: dict[str, Any]) -> dict[str, bool]:
    layer = suspected_layer(record)
    used_tools = tools_used(record_id, record)
    boot_relevant = boot_phase_relevant(record)
    return {
        "etw_executed": etw_executed(record_id, record),
        "boot_phase_included": (not boot_relevant) or has_wpr_evidence(record) or has_reboot_evidence(record),
        "correct_tool_used": not (layer in {"kernel", "boot", "driver"} and "frida" in used_tools),
        "trigger_condition_tested": has_procmon_evidence(record) or has_reboot_evidence(record) or has_wpr_evidence(record) or has_benchmark_evidence(record),
    }


def re_audit_reason(class_id: str, official: bool, record: dict[str, Any], record_id: str, incident_seen: bool) -> str:
    if re_audit_completed(record_id, class_id, official):
        return ""
    if is_final_decision_gate(record, class_id, incident_seen):
        return ""
    reasons: list[str] = []
    if class_id == "B":
        reasons.append("current_blocker")
    if class_id == "A" and not official:
        reasons.append("non_official_v31_reaudit")
    if not cross_layer_satisfied(record):
        reasons.append("cross_layer_missing")
    if not etw_executed(record_id, record):
        reasons.append("etw_not_recorded")
    checks = dead_flag_checks(record_id, record)
    if not all(checks.values()):
        reasons.append("dead_flag_checks_incomplete")
    if boot_phase_relevant(record) and not checks["boot_phase_included"]:
        reasons.append("boot_trace_missing")
    return "; ".join(dict.fromkeys(reasons))


def re_audit_priority(class_id: str, official: bool, record: dict[str, Any], record_id: str, incident_seen: bool) -> int:
    if re_audit_completed(record_id, class_id, official):
        return 0
    if is_final_decision_gate(record, class_id, incident_seen):
        return 0
    if class_id == "B":
        return 1
    if class_id != "A" or official:
        return 0
    if suspected_layer(record) in {"kernel", "boot", "driver"}:
        return 1
    if not etw_executed(record_id, record):
        return 2
    return 3


def re_audit_required(class_id: str, official: bool, record_id: str, record: dict[str, Any], incident_seen: bool) -> bool:
    if re_audit_completed(record_id, class_id, official):
        return False
    if is_final_decision_gate(record, class_id, incident_seen):
        return False
    if class_id == "B":
        return True
    if class_id == "A" and not official:
        return True
    return False


def main() -> int:
    provenance_map = load_provenance_map(PROVENANCE_PATH)
    overrides = load_overrides(OVERRIDES_PATH)
    incident_map = load_incident_map(INCIDENTS_PATH)
    promotion_gate_map = load_promotion_gate_map(PROMOTION_GATES_PATH)

    entries: list[dict[str, Any]] = []
    class_counts: Counter[str] = Counter()
    lane_counts: Counter[str] = Counter()
    missing_counts: Counter[str] = Counter()
    promotion_state_counts: Counter[str] = Counter()

    for path in sorted(RECORDS_DIR.glob("*.json")):
        record = load_json(path)
        if str(record.get("record_status") or "").strip().lower() == "deprecated":
            continue

        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        incidents = incident_map.get(record_id, [])
        incident_seen = bool(incidents)
        override = overrides.get(record_id)
        class_entry = build_class_entry(
            record,
            provenance_entry=provenance_map.get(record_id),
            override=override,
        )
        lane = determine_evidence_lane(record)
        next_layer = next_missing_layer(record, incident_seen=incident_seen)
        if override and override.get("next_missing_layer_override"):
            next_layer = str(override["next_missing_layer_override"])
        official = has_official_evidence(record)
        class_id = class_entry["evidence_class"]
        promotion_gate = promotion_gate_map.get(record_id) or {}
        full_evidence = load_v31_full_evidence(record_id)
        audit_surface = audit_surface_from_gate(record, promotion_gate, full_evidence)
        checks = dead_flag_checks(record_id, record)
        audit_required = re_audit_required(class_id, official, record_id, record, incident_seen)
        class_counts[class_entry["evidence_class"]] += 1
        lane_counts[lane] += 1
        missing_counts[next_layer] += 1
        promotion_state_counts[str(promotion_gate.get("promotion_state") or "unknown")] += 1

        if class_entry["evidence_class"] == "A":
            if has_official_evidence(record):
                basis = "official-doc"
            elif has_converged_vm_evidence(record):
                basis = "converged-vm"
            else:
                basis = "unknown"
        elif is_final_decision_gate(record, class_id, incident_seen):
            basis = "final-decision-gate"
        else:
            basis = "pending"

        entries.append(
            sanitize_value(
                {
                    "record_id": record.get("record_id"),
                    "tweak_id": record.get("tweak_id"),
                    "evidence_class": class_id,
                    "lane": lane,
                    "class_ready_basis": basis,
                    "official": official,
                    "official_doc_exists": official,
                    "procmon": has_procmon_evidence(record),
                    "ghidra": has_ghidra_evidence(record),
                    "ghidra_no_function_fallback": has_ghidra_no_function_fallback(record),
                    "wpr": has_wpr_evidence(record),
                    "benchmark": has_benchmark_evidence(record),
                    "reboot_tested": has_reboot_evidence(record),
                    "incident_seen": incident_seen,
                    "next_missing_layer": next_layer,
                    "cross_layer_satisfied": cross_layer_satisfied(record),
                    "layers_used": classification_layers(record),
                    "tools_used": tools_used(record_id, record),
                    "boot_phase_relevant": boot_phase_relevant(record),
                    "suspected_layer": suspected_layer(record),
                    "frida_kernel_guard_applied": suspected_layer(record) in {"kernel", "boot", "driver"},
                    "dead_flag_checks": checks,
                    "re_audit_required": audit_required,
                    "re_audit_priority": re_audit_priority(class_id, official, record, record_id, incident_seen),
                    "re_audit_reason": re_audit_reason(class_id, official, record, record_id, incident_seen),
                    "original_class": class_id if audit_required else None,
                    "original_pipeline_version": "pre-v3.1" if audit_required else None,
                    "new_pipeline_version": "v3.2",
                    "artifact_refs": load_v31_artifact_refs(record_id),
                    "app_mapping_status": extract_app_status(record),
                    "restore_story_known": restore_story_known(record),
                    "apply_allowed": (record.get("decision") or {}).get("apply_allowed"),
                    "confidence": (record.get("decision") or {}).get("confidence"),
                    "tweak_origin": promotion_gate.get("tweak_origin"),
                    "promotion_state": promotion_gate.get("promotion_state"),
                    "promotion_blockers": promotion_gate.get("promotion_blockers"),
                    "record_promotion_allowed": promotion_gate.get("record_promotion_allowed"),
                    "tweak_ingest_allowed": promotion_gate.get("tweak_ingest_allowed"),
                    "score_breakdown": promotion_gate.get("score_breakdown"),
                    "schema_compatibility_mode": promotion_gate.get("schema_compatibility_mode"),
                    "evaluator_version": promotion_gate.get("evaluator_version"),
                    "stale_reason": audit_surface.get("stale_reason"),
                    "last_known_good_build": audit_surface.get("last_known_good_build"),
                    "revalidation_need": audit_surface.get("revalidation_need"),
                    "rollback_verification_status": audit_surface.get("rollback_verification_status"),
                    "bench_status": audit_surface.get("bench_status"),
                    "conflict_reason": audit_surface.get("conflict_reason"),
                    "last_known_good_verification_context": audit_surface.get("last_known_good_verification_context"),
                    "os_build": audit_surface.get("os_build"),
                    "os_edition": audit_surface.get("os_edition"),
                    "architecture": audit_surface.get("architecture"),
                    "elevation_context": audit_surface.get("elevation_context"),
                    "machine_user_scope": audit_surface.get("machine_user_scope"),
                    "source_file": str(path.relative_to(REPO_ROOT)).replace("\\", "/"),
                    "incident_ids": list(
                        dict.fromkeys(
                            incident.get("incident_id")
                            for incident in incidents
                            if incident.get("incident_id")
                        )
                    ),
                }
            )
        )

    payload = {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "summary": {
            "total_active_records": len(entries),
            "class_counts": dict(class_counts),
            "lane_counts": dict(lane_counts),
            "next_missing_layer_counts": dict(missing_counts),
            "promotion_state_counts": dict(promotion_state_counts),
            "re_audit_required_count": sum(1 for entry in entries if entry.get("re_audit_required")),
        },
        "entries": entries,
    }

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    print(f"Wrote {OUTPUT_PATH}")
    print(f"Summary: {payload['summary']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
