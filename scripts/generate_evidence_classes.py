#!/usr/bin/env python3
from __future__ import annotations

import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

from evidence_class_lib import (
    CLASS_DEFINITIONS,
    build_class_entry,
    load_json,
    load_overrides,
    load_provenance_map,
)
from research_path_lib import REPO_ROOT, RESEARCH_ROOT

RECORDS_DIR = RESEARCH_ROOT / "records"
PROVENANCE_PATH = REPO_ROOT / "Docs" / "tweaks" / "tweak-provenance.json"
OVERRIDES_PATH = RESEARCH_ROOT / "evidence-class-overrides.json"
OUTPUT_PATH = RESEARCH_ROOT / "evidence-classes.json"


def main() -> int:
    provenance_map = load_provenance_map(PROVENANCE_PATH)
    overrides = load_overrides(OVERRIDES_PATH)

    entries: list[dict] = []
    class_counts: Counter[str] = Counter()
    action_counts: Counter[str] = Counter()

    for path in sorted(RECORDS_DIR.glob("*.json")):
        record = load_json(path)
        key = str(record.get("record_id") or record.get("tweak_id") or "")
        entry = build_class_entry(
            record,
            provenance_entry=provenance_map.get(key),
            override=overrides.get(key),
        )
        class_counts[entry["evidence_class"]] += 1
        action_counts[entry["action_state"]] += 1
        entries.append(entry)

    payload = {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "summary": {
            "total_records": len(entries),
            "class_counts": {class_id: class_counts.get(class_id, 0) for class_id in CLASS_DEFINITIONS},
            "action_state_counts": dict(action_counts),
        },
        "classes": {
            class_id: {
                "label": definition["label"],
                "title": definition["title"],
                "description": definition["description"],
            }
            for class_id, definition in CLASS_DEFINITIONS.items()
        },
        "entries": entries,
    }

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    print(f"Wrote {OUTPUT_PATH}")
    print(f"Class counts: {payload['summary']['class_counts']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
