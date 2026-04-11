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
    URL_VALIDATION_REPORT_PATH,
    load_records,
    load_full_evidence_bundle,
    now_utc,
    validate_candidate_urls,
    write_json,
)


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate HTTP/HTTPS source enrichment URLs for RegProbe candidates.")
    parser.add_argument("--candidate-id", help="Optional single candidate id.")
    parser.add_argument("--timeout", type=float, default=5.0, help="Per-request timeout in seconds.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON payload.")
    args = parser.parse_args()

    entries: list[dict[str, object]] = []
    for record in load_records():
        candidate_id = str(record.get("record_id") or record.get("tweak_id") or "")
        if args.candidate_id and candidate_id != args.candidate_id:
            continue
        full_evidence = load_full_evidence_bundle(candidate_id)
        status = validate_candidate_urls(record, full_evidence, timeout=args.timeout)
        entries.append(
            {
                "candidate_id": candidate_id,
                "record_id": candidate_id,
                "tweak_id": str(record.get("tweak_id") or candidate_id),
                **status,
            }
        )

    payload = {
        "generated_utc": now_utc(),
        "entry_count": len(entries),
        "checked_url_count": sum(int(item.get("checked_url_count") or 0) for item in entries),
        "dead_link_count": sum(int(item.get("dead_link_count") or 0) for item in entries),
        "entries": entries,
    }
    write_json(URL_VALIDATION_REPORT_PATH, payload)

    if args.emit_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print(f"Wrote {URL_VALIDATION_REPORT_PATH}")
        print(json.dumps({"entry_count": payload["entry_count"], "dead_link_count": payload["dead_link_count"]}, ensure_ascii=False, indent=2))
    return 0 if payload["dead_link_count"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
