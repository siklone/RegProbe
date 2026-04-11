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

from research_v36_lib import canonical_bundle_projection, load_json, load_records, validate_canonical_bundle, write_json


EVIDENCE_RECORDS_ROOT = REPO_ROOT / "evidence" / "records"
AUDIT_PATH = REPO_ROOT / "research" / "evidence-audit.json"


def load_audit_entries() -> dict[str, dict]:
    payload = load_json(AUDIT_PATH)
    return {
        str(entry.get("record_id") or entry.get("tweak_id") or ""): entry
        for entry in payload.get("entries") or []
        if isinstance(entry, dict)
    }


def record_map() -> dict[str, dict]:
    return {
        str(record.get("record_id") or record.get("tweak_id") or ""): record
        for record in load_records()
    }


def existing_full_evidence_paths(tweak_id: str | None = None) -> list[Path]:
    if tweak_id:
        candidate = EVIDENCE_RECORDS_ROOT / tweak_id / "full-evidence.json"
        return [candidate] if candidate.exists() else []
    return sorted(EVIDENCE_RECORDS_ROOT.glob("*/full-evidence.json"))


def adapt_full_evidence(path: Path, records: dict[str, dict], audits: dict[str, dict]) -> dict[str, object]:
    payload = load_json(path)
    tweak_id = str(payload.get("tweak_id") or path.parent.name)
    record = records.get(tweak_id)
    if not record:
        return {
            "tweak_id": tweak_id,
            "path": str(path.relative_to(REPO_ROOT)),
            "status": "missing-record",
        }
    audit = audits.get(tweak_id, {})
    projection = canonical_bundle_projection(record, audit, payload)
    errors = validate_canonical_bundle(projection)
    if errors:
        return {
            "tweak_id": tweak_id,
            "path": str(path.relative_to(REPO_ROOT)),
            "status": "invalid-projection",
            "errors": errors,
        }
    payload.update(projection)
    write_json(path, payload)
    return {
        "tweak_id": tweak_id,
        "path": str(path.relative_to(REPO_ROOT)),
        "status": "adapted",
        "promotion_state": projection.get("promotion_state"),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Adapt existing full-evidence bundles to the RegProbe 3.6 canonical bundle fields.")
    parser.add_argument("--tweak-id", help="Optional single tweak id to adapt.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON summary.")
    args = parser.parse_args()

    records = record_map()
    audits = load_audit_entries()
    results = [adapt_full_evidence(path, records, audits) for path in existing_full_evidence_paths(args.tweak_id)]
    summary = {
        "total_bundles": len(results),
        "adapted": sum(1 for item in results if item.get("status") == "adapted"),
        "invalid": sum(1 for item in results if item.get("status") == "invalid-projection"),
        "missing_record": sum(1 for item in results if item.get("status") == "missing-record"),
    }
    payload = {
        "summary": summary,
        "results": results,
    }
    if args.emit_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0 if summary["invalid"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
