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

from research_v36_lib import ENRICHMENT_CACHE_PATH, build_enrichment_cache_entries, load_records, load_enrichment_cache, write_enrichment_cache


def main() -> int:
    parser = argparse.ArgumentParser(description="Build the RegProbe 3.6 source enrichment cache.")
    parser.add_argument("--emit-json", action="store_true", help="Print summary JSON.")
    args = parser.parse_args()

    entries = []
    for record in load_records():
        entries.extend(build_enrichment_cache_entries(record))

    write_enrichment_cache(entries, ENRICHMENT_CACHE_PATH)
    cached = load_enrichment_cache(ENRICHMENT_CACHE_PATH)
    summary = {
        "cache_path": str(ENRICHMENT_CACHE_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "entry_count": len(cached),
        "record_count": len({str(item.get("record_id") or "") for item in cached if item.get("record_id")}),
        "sources": sorted({str(item.get("source") or "") for item in cached if item.get("source")}),
    }
    if args.emit_json:
        print(json.dumps(summary, ensure_ascii=False, indent=2))
    else:
        print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
