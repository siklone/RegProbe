#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import (
    CURRENT_SCHEMA_VERSION,
    EVALUATOR_VERSION,
    PROMOTION_AUDIT_LOG_PATH,
    PROMOTION_GATES_PATH,
    append_jsonl,
    evaluate_candidate_gate,
    load_json,
    load_json_if_exists,
    load_records,
    load_url_validation_report,
    now_utc,
    validate_gate_result,
    write_json,
)


def load_audit_entries() -> dict[str, dict]:
    payload = load_json(Path("research/evidence-audit.json"))
    return {
        str(entry.get("record_id") or entry.get("tweak_id") or ""): entry
        for entry in payload.get("entries") or []
        if isinstance(entry, dict)
    }


def load_full_evidence(tweak_id: str) -> dict:
    path = REPO_ROOT / "evidence" / "records" / tweak_id / "full-evidence.json"
    payload = load_json_if_exists(path)
    return payload if isinstance(payload, dict) else {}


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate compact promotion gates from research records.")
    parser.add_argument("--emit-json", action="store_true", help="Print summary JSON.")
    args = parser.parse_args()

    audit_map = load_audit_entries()
    entries: list[dict] = []
    state_counts: Counter[str] = Counter()
    blocker_counts: Counter[str] = Counter()
    invalid_entries: list[dict] = []
    url_validation_map = load_url_validation_report()

    for record in load_records():
        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        audit = audit_map.get(record_id, {})
        gate = evaluate_candidate_gate(
            record,
            audit,
            load_full_evidence(record_id),
            url_validation_status=url_validation_map.get(record_id),
        )
        validation_errors = validate_gate_result(gate)
        if validation_errors:
            invalid_entries.append(
                {
                    "candidate_id": gate.get("candidate_id"),
                    "errors": validation_errors,
                }
            )
        state_counts[gate["promotion_state"]] += 1
        blocker_counts.update(str(item) for item in (gate.get("promotion_blockers") or []) if item)
        entries.append(gate)
        append_jsonl(
            PROMOTION_AUDIT_LOG_PATH,
            {
                "schema_version": CURRENT_SCHEMA_VERSION,
                "evaluator_version": EVALUATOR_VERSION,
                "timestamp_utc": now_utc(),
                "candidate_id": gate["candidate_id"],
                "promotion_state": gate["promotion_state"],
                "promotion_blockers": gate["promotion_blockers"],
                "schema_compatibility_mode": gate["schema_compatibility_mode"],
                "tweak_origin": gate["tweak_origin"],
                "score_breakdown": gate["score_breakdown"],
                "verification_context": gate.get("verification_context"),
            },
        )

    payload = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "evaluator_version": EVALUATOR_VERSION,
        "generated_utc": now_utc(),
        "summary": {
            "total_records": len(entries),
            "promotion_state_counts": dict(state_counts),
            "blocker_counts": dict(blocker_counts),
            "invalid_gate_entries": len(invalid_entries),
            "exit_criteria": {
                "has_promotion_eligible": state_counts.get("promotion-eligible", 0) > 0,
                "has_blocked": state_counts.get("blocked", 0) > 0,
                "has_revalidation_pending": state_counts.get("revalidation-pending", 0) > 0,
            },
        },
        "invalid_entries": invalid_entries,
        "entries": sorted(entries, key=lambda item: str(item.get("tweak_id") or item.get("candidate_id") or "")),
    }
    write_json(PROMOTION_GATES_PATH, payload)

    if args.emit_json:
        print(json.dumps(payload["summary"], ensure_ascii=False, indent=2))
    else:
        print(f"Wrote {PROMOTION_GATES_PATH}")
        print(json.dumps(payload["summary"], ensure_ascii=False, indent=2))
    exit_criteria = payload["summary"]["exit_criteria"]
    ok = (
        not invalid_entries
        and exit_criteria["has_promotion_eligible"]
        and exit_criteria["has_blocked"]
        and exit_criteria["has_revalidation_pending"]
    )
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
