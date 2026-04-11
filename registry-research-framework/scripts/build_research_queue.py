#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import (
    CURRENT_SCHEMA_VERSION,
    DEFAULT_BACKEND_ID,
    DISCOVERY_EVENTS_PATH,
    DISCOVERY_ROOT,
    ETL_REGISTRY_DISCOVERY_PATH,
    GAP_ANALYSIS_SUMMARY_PATH,
    QUEUE_ROOT,
    build_queue_entry,
    capability_status,
    default_execution_context,
    discovery_seed_from_record,
    evaluate_candidate_gate,
    gap_analysis_candidates,
    load_backend_capabilities,
    load_json,
    load_json_if_exists,
    load_records,
    load_url_validation_report,
    score_candidate,
    score_etl_candidate,
    summarize_gap_analysis,
    summarize_queue,
    triage_candidate,
    validate_discovery_candidate,
    validate_queue_entry,
    required_capabilities_for_runner_entry,
    write_json,
    sibling_expansion_candidates,
)


QUEUE_PATH = QUEUE_ROOT / "research-queue.json"


def load_audit_entries() -> dict[str, dict]:
    audit_path = Path("research/evidence-audit.json")
    payload = load_json(audit_path)
    return {
        str(entry.get("record_id") or entry.get("tweak_id") or ""): entry
        for entry in payload.get("entries") or []
        if isinstance(entry, dict)
    }


def load_runner_config() -> dict:
    return load_json(Path("registry-research-framework/config/tweak-vm-runners.json"))


def load_full_evidence(tweak_id: str) -> dict:
    path = REPO_ROOT / "evidence" / "records" / tweak_id / "full-evidence.json"
    payload = load_json_if_exists(path)
    return payload if isinstance(payload, dict) else {}


def load_runtime_discovery_candidates() -> list[dict]:
    results: list[dict] = []
    seen_ids: set[str] = set()
    etl_snapshot_present = False

    snapshot_payload = load_json_if_exists(ETL_REGISTRY_DISCOVERY_PATH)
    if isinstance(snapshot_payload, dict):
        for candidate in snapshot_payload.get("discovery_candidates") or []:
            if not isinstance(candidate, dict):
                continue
            source = str(candidate.get("discovery_source") or "")
            if source != "etl-registry-touch":
                continue
            candidate_id = str(candidate.get("candidate_id") or "").strip()
            if candidate_id and candidate_id in seen_ids:
                continue
            if candidate_id:
                seen_ids.add(candidate_id)
            results.append(candidate)
            etl_snapshot_present = True

    if not DISCOVERY_EVENTS_PATH.exists():
        return results
    for line in DISCOVERY_EVENTS_PATH.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        payload = json.loads(line)
        source = str(payload.get("discovery_source") or "")
        if source in {"existing-record", "ai-gap-analysis"}:
            continue
        if source == "etl-registry-touch" and etl_snapshot_present:
            continue
        candidate_id = str(payload.get("candidate_id") or "").strip()
        if candidate_id and candidate_id in seen_ids:
            continue
        if candidate_id:
            seen_ids.add(candidate_id)
        results.append(payload)
    return results


def queue_entries_from_records(
    records: list[dict],
    audit_map: dict[str, dict],
    runner_config: dict,
    backend_manifest: dict,
    url_validation_map: dict[str, dict],
) -> list[dict]:
    entries: list[dict] = []
    for record in records:
        seed = discovery_seed_from_record(record)
        audit = audit_map.get(str(record.get("record_id") or record.get("tweak_id") or ""), {})
        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        full_evidence = load_full_evidence(record_id)
        gate = evaluate_candidate_gate(record, audit, full_evidence, url_validation_status=url_validation_map.get(record_id))
        gate["score_breakdown"] = score_candidate(record, audit, full_evidence)
        runner_lane = "runtime"
        next_layer = gate.get("next_missing_layer") or "decision-gate"
        if next_layer == "procmon":
            runner_lane = "procmon"
        elif next_layer in {"wpr-or-benchmark", "runtime-benchmark"}:
            runner_lane = "behavior"
        required_capabilities = required_capabilities_for_runner_entry(runner_lane, str(record.get("tweak_id") or ""), runner_config)
        _available, missing = capability_status(required_capabilities, backend_manifest)
        state = gate["promotion_state"]
        if state == "promotion-eligible":
            queue_state = "promotion-eligible"
        elif state == "promoted":
            queue_state = "promoted"
        elif state == "revalidation-pending":
            queue_state = "revalidation-pending"
        elif state == "rejected":
            queue_state = "rejected"
        else:
            queue_state = "blocked"

        blockers = list(gate["promotion_blockers"])
        if missing:
            blockers = [*blockers, *(f"capability-excluded:{item}" for item in missing)]
        entry = build_queue_entry(
            seed,
            state=queue_state,
            blockers=blockers,
            required_capabilities=required_capabilities,
            next_lane=next_layer,
            linked_record_id=str(record.get("record_id") or record.get("tweak_id") or ""),
            gate_result=gate,
        )
        entries.append(entry)
    return entries


def queue_entries_from_gaps(records: list[dict]) -> list[dict]:
    entries: list[dict] = []
    for candidate in gap_analysis_candidates(records):
        accepted, reasons = triage_candidate(candidate)
        state = "triaged" if accepted else "discarded"
        blockers = [] if accepted else [f"triage:{reason}" for reason in reasons]
        entry = build_queue_entry(
            candidate,
            state=state,
            blockers=blockers,
            required_capabilities=[],
            next_lane="scoring" if accepted else "discarded",
            linked_record_id=None,
            gate_result=None,
        )
        if not accepted:
            entry["discard_reason"] = reasons
        entries.append(entry)
    return entries


def queue_entries_from_runtime_discovery(candidates: list[dict]) -> list[dict]:
    entries: list[dict] = []
    for candidate in candidates:
        accepted, reasons = triage_candidate(candidate)
        state = "triaged" if accepted else "discarded"
        blockers = [] if accepted else [f"triage:{reason}" for reason in reasons]
        entry = build_queue_entry(
            candidate,
            state=state,
            blockers=blockers,
            required_capabilities=[],
            next_lane="scoring" if accepted else "discarded",
            linked_record_id=None,
            gate_result=None,
        )
        if accepted and str(candidate.get("discovery_source") or "") == "etl-registry-touch":
            entry["score"] = score_etl_candidate(candidate)
        if not accepted:
            entry["discard_reason"] = reasons
        entries.append(entry)
    return entries


def queue_entries_from_sibling_discovery(records: list[dict], audit_map: dict[str, dict], gate_map: dict[str, dict]) -> list[dict]:
    entries: list[dict] = []
    for candidate in sibling_expansion_candidates(records, audit_map, gate_map):
        accepted, reasons = triage_candidate(candidate)
        state = "triaged" if accepted else "discarded"
        blockers = [] if accepted else [f"triage:{reason}" for reason in reasons]
        entry = build_queue_entry(
            candidate,
            state=state,
            blockers=blockers,
            required_capabilities=[],
            next_lane="triage" if accepted else "discarded",
            linked_record_id=None,
            gate_result=None,
        )
        if not accepted:
            entry["discard_reason"] = reasons
        entries.append(entry)
    return entries


def validate_entries(entries: list[dict]) -> list[dict]:
    valid: list[dict] = []
    for entry in entries:
        candidate_errors = validate_discovery_candidate(entry)
        queue_errors = validate_queue_entry(entry)
        if candidate_errors or queue_errors:
            entry = dict(entry)
            entry["state"] = "discarded"
            entry["discard_reason"] = sorted(set(candidate_errors + queue_errors))
        valid.append(entry)
    return valid


def main() -> int:
    parser = argparse.ArgumentParser(description="Build the RegProbe 3.6 research queue.")
    parser.add_argument("--emit-json", action="store_true", help="Print summary JSON.")
    args = parser.parse_args()

    records = load_records()
    audit_map = load_audit_entries()
    runner_config = load_runner_config()
    backend_manifest = load_backend_capabilities(DEFAULT_BACKEND_ID)
    url_validation_map = load_url_validation_report()
    runtime_discovery_candidates = load_runtime_discovery_candidates()

    record_entries = queue_entries_from_records(records, audit_map, runner_config, backend_manifest, url_validation_map)
    gate_map = {
        str(entry.get("linked_record_id") or entry.get("record_id") or entry.get("candidate_id") or ""): (entry.get("last_evaluator_result") or {})
        for entry in record_entries
        if str(entry.get("linked_record_id") or entry.get("record_id") or entry.get("candidate_id") or "")
    }

    queue_entries = validate_entries(
        record_entries
        + queue_entries_from_gaps(records)
        + queue_entries_from_sibling_discovery(records, audit_map, gate_map)
        + queue_entries_from_runtime_discovery(runtime_discovery_candidates)
    )

    payload = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "generated_utc": next((entry.get("updated_utc") for entry in queue_entries if entry.get("updated_utc")), None),
        "backend_id": DEFAULT_BACKEND_ID,
        "execution_context": default_execution_context(DEFAULT_BACKEND_ID),
        "summary": summarize_queue(queue_entries),
        "gap_analysis_summary": summarize_gap_analysis(queue_entries),
        "entries": sorted(queue_entries, key=lambda item: (str(item.get("state") or ""), str(item.get("candidate_id") or ""))),
    }
    write_json(QUEUE_PATH, payload)
    write_json(GAP_ANALYSIS_SUMMARY_PATH, payload["gap_analysis_summary"])

    if args.emit_json:
        print(json.dumps(payload["summary"], ensure_ascii=False, indent=2))
    else:
        print(f"Wrote {QUEUE_PATH}")
        print(json.dumps(payload["summary"], ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
