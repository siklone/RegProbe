#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from evidence_class_lib import (
    GHIDRA_EVIDENCE_KINDS,
    build_class_entry,
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
)
from research_path_lib import REPO_ROOT, RESEARCH_ROOT

RECORDS_DIR = RESEARCH_ROOT / "records"
PROVENANCE_PATH = REPO_ROOT / "Docs" / "tweaks" / "tweak-provenance.json"
OVERRIDES_PATH = RESEARCH_ROOT / "evidence-class-overrides.json"
INCIDENTS_PATH = RESEARCH_ROOT / "vm-incidents.json"
OUTPUT_PATH = RESEARCH_ROOT / "evidence-audit.json"
GHIDRA_PATH_RE = re.compile(r"research/evidence-files/ghidra/[^\s);,]+")


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
            candidate = REPO_ROOT / match
            evidence_path = candidate.parent / "evidence.json" if candidate.suffix else candidate / "evidence.json"
            candidate_files.add(evidence_path)

    for path in candidate_files:
        if not path.exists():
            continue
        payload = load_json(path)
        if payload.get("ghidra_no_function_fallback") is True:
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
                    "evidence_class": class_entry["evidence_class"],
                    "lane": lane,
                    "class_ready_basis": basis,
                    "official": has_official_evidence(record),
                    "procmon": has_procmon_evidence(record),
                    "ghidra": has_ghidra_evidence(record),
                    "ghidra_no_function_fallback": has_ghidra_no_function_fallback(record),
                    "wpr": has_wpr_evidence(record),
                    "benchmark": has_benchmark_evidence(record),
                    "reboot_tested": has_reboot_evidence(record),
                    "incident_seen": incident_seen,
                    "next_missing_layer": next_layer,
                    "app_mapping_status": extract_app_status(record),
                    "restore_story_known": restore_story_known(record),
                    "apply_allowed": (record.get("decision") or {}).get("apply_allowed"),
                    "confidence": (record.get("decision") or {}).get("confidence"),
                    "source_file": str(path.relative_to(REPO_ROOT)).replace("\\", "/"),
                    "incident_ids": [incident.get("incident_id") for incident in incidents if incident.get("incident_id")],
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
