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

from research_v36_lib import (  # noqa: E402
    REGRESSION_PACKS_ROOT,
    build_regression_pack,
    candidate_regression_pack_dir,
    load_audit_entries,
    load_promotion_gate_map,
    load_records,
    record_map,
    write_regression_pack,
    load_full_evidence_bundle,
)


PROMOTABLE_STATES = {"promoted", "promotion-eligible", "revalidation-pending"}


def selected_candidates(
    gate_map: dict[str, dict],
    *,
    all_candidates: bool,
    candidate_id: str | None,
    states: set[str] | None,
    limit: int | None,
) -> list[str]:
    if candidate_id:
        return [candidate_id]

    if not all_candidates:
        return []

    chosen_states = states or PROMOTABLE_STATES
    ids = [
        str(entry.get("record_id") or entry.get("tweak_id") or entry.get("candidate_id") or "")
        for entry in gate_map.values()
        if str(entry.get("promotion_state") or "") in chosen_states
    ]
    ordered = sorted({item for item in ids if item})
    return ordered[:limit] if limit else ordered


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate regression packs for promotable RegProbe candidates.")
    parser.add_argument("candidate_id", nargs="?", help="Single candidate id.")
    parser.add_argument("--all", action="store_true", help="Generate packs for all promotable candidates.")
    parser.add_argument("--state", action="append", dest="states", help="Restrict --all to one or more promotion states.")
    parser.add_argument("--limit", type=int, help="Optional max candidate count for --all.")
    parser.add_argument("--output-root", help="Optional output root.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON summary.")
    args = parser.parse_args()

    gate_map = load_promotion_gate_map()
    audit_map = load_audit_entries()
    records = record_map(load_records())
    output_root = Path(args.output_root) if args.output_root else REGRESSION_PACKS_ROOT

    states = {item.strip() for item in (args.states or []) if item and item.strip()}
    candidate_ids = selected_candidates(
        gate_map,
        all_candidates=args.all,
        candidate_id=args.candidate_id,
        states=states or None,
        limit=args.limit,
    )

    results: list[dict[str, object]] = []
    for candidate_id in candidate_ids:
        record = records.get(candidate_id)
        if not record:
            results.append({"candidate_id": candidate_id, "status": "missing-record"})
            continue
        audit = audit_map.get(candidate_id, {})
        full_evidence = load_full_evidence_bundle(candidate_id)
        gate = gate_map.get(candidate_id, {})
        pack = build_regression_pack(record, audit, full_evidence, gate)
        output_dir = write_regression_pack(candidate_id, pack, output_root)
        results.append(
            {
                "candidate_id": candidate_id,
                "status": "generated",
                "output_dir": str(output_dir.relative_to(REPO_ROOT)),
                "files": sorted(pack.keys()),
            }
        )

    payload = {
        "output_root": str(output_root.relative_to(REPO_ROOT) if output_root.is_absolute() else output_root),
        "generated_count": sum(1 for item in results if item["status"] == "generated"),
        "missing_record_count": sum(1 for item in results if item["status"] == "missing-record"),
        "results": results,
    }
    if args.emit_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print(json.dumps({"generated_count": payload["generated_count"], "output_root": payload["output_root"]}, ensure_ascii=False, indent=2))
    return 0 if payload["generated_count"] > 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
