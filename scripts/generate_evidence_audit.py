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

    payload = load_json(path)
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


def re_audit_reason(class_id: str, official: bool, record: dict[str, Any], record_id: str) -> str:
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


def re_audit_priority(class_id: str, official: bool, record: dict[str, Any], record_id: str) -> int:
    if class_id == "B":
        return 1
    if class_id != "A" or official:
        return 0
    if suspected_layer(record) in {"kernel", "boot", "driver"}:
        return 1
    if not etw_executed(record_id, record):
        return 2
    return 3


def re_audit_required(class_id: str, official: bool) -> bool:
    if class_id == "B":
        return True
    if class_id == "A" and not official:
        return True
    return False


def main() -> int:
    provenance_map = load_provenance_map(PROVENANCE_PATH)
    overrides = load_overrides(OVERRIDES_PATH)
    incident_map = load_incident_map(INCIDENTS_PATH)

    entries: list[dict[str, Any]] = []
    class_counts: Counter[str] = Counter()
    lane_counts: Counter[str] = Counter()
    missing_counts: Counter[str] = Counter()

    for path in sorted(RECORDS_DIR.glob("*.json")):
        record = load_json(path)
        if str(record.get("record_status") or "").strip().lower() == "deprecated":
            continue

        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        incidents = incident_map.get(record_id, [])
        incident_seen = bool(incidents)
        class_entry = build_class_entry(
            record,
            provenance_entry=provenance_map.get(record_id),
            override=overrides.get(record_id),
        )
        lane = determine_evidence_lane(record)
        next_layer = next_missing_layer(record, incident_seen=incident_seen)
        official = has_official_evidence(record)
        class_id = class_entry["evidence_class"]
        checks = dead_flag_checks(record_id, record)
        audit_required = re_audit_required(class_id, official)
        class_counts[class_entry["evidence_class"]] += 1
        lane_counts[lane] += 1
        missing_counts[next_layer] += 1

        if class_entry["evidence_class"] == "A":
            if has_official_evidence(record):
                basis = "official-doc"
            elif has_converged_vm_evidence(record):
                basis = "converged-vm"
            else:
                basis = "unknown"
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
                    "re_audit_priority": re_audit_priority(class_id, official, record, record_id),
                    "re_audit_reason": re_audit_reason(class_id, official, record, record_id),
                    "original_class": class_id if audit_required else None,
                    "original_pipeline_version": "pre-v3.1" if audit_required else None,
                    "new_pipeline_version": "v3.1",
                    "artifact_refs": load_v31_artifact_refs(record_id),
                    "app_mapping_status": extract_app_status(record),
                    "restore_story_known": restore_story_known(record),
                    "apply_allowed": (record.get("decision") or {}).get("apply_allowed"),
                    "confidence": (record.get("decision") or {}).get("confidence"),
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
