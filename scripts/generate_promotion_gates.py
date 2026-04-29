#!/usr/bin/env python3
from __future__ import annotations

import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

from research_path_lib import RESEARCH_ROOT
from research_v36_lib import derive_promotion_state, load_audit_entries, load_records, validate_gate_result


OUTPUT_PATH = RESEARCH_ROOT / "promotion-gates.json"


def main() -> int:
    records = load_records()
    audit_map = load_audit_entries()

    entries: list[dict] = []
    invalid_entries: list[dict] = []
    promotion_state_counts: Counter[str] = Counter()
    blocker_counts: Counter[str] = Counter()

    for record in records:
        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        gate = derive_promotion_state(record, audit_map.get(record_id, {}))
        entries.append(gate)
        promotion_state_counts[str(gate.get("promotion_state") or "unknown")] += 1
        for blocker in gate.get("promotion_blockers") or []:
            blocker_counts[str(blocker)] += 1

        errors = validate_gate_result(gate)
        if errors:
            invalid_entries.append(
                {
                    "candidate_id": gate.get("candidate_id"),
                    "errors": errors,
                }
            )

    payload = {
        "schema_version": "1.0",
        "evaluator_version": "3.6.0",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "summary": {
            "total_records": len(entries),
            "promotion_state_counts": dict(promotion_state_counts),
            "blocker_counts": dict(blocker_counts),
            "invalid_gate_entries": len(invalid_entries),
            "exit_criteria": {
                "has_promotion_eligible": promotion_state_counts.get("promotion-eligible", 0) > 0,
                "has_blocked": promotion_state_counts.get("blocked", 0) > 0,
                "has_revalidation_pending": promotion_state_counts.get("revalidation-pending", 0) > 0,
            },
        },
        "invalid_entries": invalid_entries,
        "entries": entries,
    }

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    print(f"Wrote {OUTPUT_PATH}")
    print(f"Promotion states: {dict(promotion_state_counts)}")
    print(f"Invalid entries: {len(invalid_entries)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
