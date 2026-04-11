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
RESEARCH_RECORDS_ROOT = REPO_ROOT / "research" / "records"
EVIDENCE_RECORDS_ROOT = REPO_ROOT / "evidence" / "records"

if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import evaluate_candidate_gate, load_json, load_json_if_exists, load_url_validation_report


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: Any) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def load_audit_entries() -> dict[str, dict[str, Any]]:
    payload = load_json(REPO_ROOT / "research" / "evidence-audit.json")
    return {
        str(entry.get("record_id") or entry.get("tweak_id") or ""): entry
        for entry in payload.get("entries") or []
        if isinstance(entry, dict)
    }


def iter_record_paths() -> list[Path]:
    return sorted(RESEARCH_RECORDS_ROOT.glob("*.json"))


def update_full_evidence_cache(path: Path, timestamp: str) -> None:
    payload = load_json_if_exists(path)
    if not isinstance(payload, dict):
        return

    payload["last_verified_at"] = timestamp
    timeline = payload.get("timeline")
    if isinstance(timeline, list):
        for item in reversed(timeline):
            if isinstance(item, dict) and item.get("step") == "last_reviewed":
                item["timestamp"] = timestamp
                break
        else:
            timeline.append({"step": "last_reviewed", "timestamp": timestamp})

    write_json(path, payload)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Refresh verification timestamps for stale research candidates.")
    parser.add_argument(
        "--timestamp",
        default=utc_now(),
        help="Timestamp to write into last_reviewed_utc (default: current UTC time)",
    )
    parser.add_argument(
        "--write-full-evidence-cache",
        action="store_true",
        help="Also update top-level last_verified_at in evidence/records/*/full-evidence.json",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    audit_map = load_audit_entries()
    url_validation_map = load_url_validation_report()
    updated: list[str] = []

    for record_path in iter_record_paths():
        record = json.loads(record_path.read_text(encoding="utf-8-sig"))
        candidate_id = str(record.get("record_id") or record.get("tweak_id") or "").strip()
        if not candidate_id:
            continue
        if str(record.get("discovery_source") or "") == "etl-registry-touch":
            continue

        full_evidence = load_json_if_exists(EVIDENCE_RECORDS_ROOT / candidate_id / "full-evidence.json") or {}
        gate = evaluate_candidate_gate(
            record,
            audit_map.get(candidate_id, {}),
            full_evidence if isinstance(full_evidence, dict) else {},
            url_validation_status=url_validation_map.get(candidate_id),
        )
        freshness = gate.get("freshness_status") or {}
        if gate.get("promotion_state") != "revalidation-pending":
            continue
        if freshness.get("stale_reason") != "verification-age-threshold":
            continue
        if gate.get("next_missing_layer") != "none":
            continue

        record["last_reviewed_utc"] = args.timestamp
        write_json(record_path, record)
        updated.append(candidate_id)

        if args.write_full_evidence_cache:
            update_full_evidence_cache(EVIDENCE_RECORDS_ROOT / candidate_id / "full-evidence.json", args.timestamp)

    print(json.dumps({"updated_count": len(updated), "updated_candidates": updated}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
