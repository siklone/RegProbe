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
    DISCOVERY_ROOT,
    GAP_ANALYSIS_SUMMARY_PATH,
    QUEUE_ROOT,
    append_discovery_candidates,
    build_queue_entry,
    capability_status,
    default_execution_context,
    derive_promotion_state,
    discovery_seed_from_record,
    gap_analysis_candidates,
    load_backend_capabilities,
    load_json,
    load_records,
    score_candidate,
    summarize_gap_analysis,
    summarize_queue,
    triage_candidate,
    validate_discovery_candidate,
    validate_queue_entry,
    required_capabilities_for_runner_entry,
    write_json,
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


def append_discovery_events(candidates: list[dict]) -> None:
    append_discovery_candidates(candidates)


def queue_entries_from_records(records: list[dict], audit_map: dict[str, dict], runner_config: dict, backend_manifest: dict) -> list[dict]:
    entries: list[dict] = []
    for record in records:
        seed = discovery_seed_from_record(record)
        audit = audit_map.get(str(record.get("record_id") or record.get("tweak_id") or ""), {})
        gate = derive_promotion_state(record, audit)
        gate["score_breakdown"] = score_candidate(record, audit)
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

    queue_entries = validate_entries(
        queue_entries_from_records(records, audit_map, runner_config, backend_manifest)
        + queue_entries_from_gaps(records)
    )

    append_discovery_events(queue_entries)

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
