#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from evidence_class_lib import build_class_entry, load_json, load_overrides, load_provenance_map
from research_path_lib import REPO_ROOT, RESEARCH_ROOT
from wave2_research_lib import evidence_freshness


RECORDS_DIR = RESEARCH_ROOT / "records"
PROVENANCE_PATH = REPO_ROOT / "Docs" / "tweaks" / "tweak-provenance.json"
OVERRIDES_PATH = RESEARCH_ROOT / "evidence-class-overrides.json"
OUTPUT_PATH = RESEARCH_ROOT / "regression-history.json"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def baseline_default(record: dict[str, Any]) -> Any:
    defaults = record.get("windows_defaults") or []
    if not defaults:
        return None
    states = (defaults[0] or {}).get("states") or []
    if not states:
        return None
    return states[0].get("value")


def major_build_token(build: str | None) -> str | None:
    if not build:
        return None
    return str(build).split(".", 1)[0]


def main() -> int:
    provenance_map = load_provenance_map(PROVENANCE_PATH)
    overrides = load_overrides(OVERRIDES_PATH)
    existing = load_json(OUTPUT_PATH) if OUTPUT_PATH.exists() else {}
    existing_entries = existing.get("entries") if isinstance(existing, dict) else []
    previous_map = {
        str(item.get("tweak_id") or ""): item
        for item in existing_entries or []
        if isinstance(item, dict) and item.get("tweak_id")
    }

    entries: list[dict[str, Any]] = []
    revalidation_queue: list[dict[str, Any]] = []

    for path in sorted(RECORDS_DIR.glob("*.json")):
        record = load_json(path)
        tweak_id = str(record.get("tweak_id") or "")
        class_entry = build_class_entry(
            record,
            provenance_entry=provenance_map.get(tweak_id),
            override=overrides.get(tweak_id),
        )
        freshness = evidence_freshness(record)
        current_snapshot = {
            "build": freshness.get("os_build"),
            "default": baseline_default(record),
            "class": class_entry.get("evidence_class"),
            "last_reviewed_utc": record.get("last_reviewed_utc"),
        }

        existing_history = []
        if isinstance(previous_map.get(tweak_id), dict):
            existing_history = previous_map[tweak_id].get("history") or []
        history = [item for item in existing_history if isinstance(item, dict)]
        if not history or history[-1] != current_snapshot:
            history.append(current_snapshot)

        previous_build = history[-2].get("build") if len(history) >= 2 and isinstance(history[-2], dict) else None
        current_build = current_snapshot.get("build")
        major_changed = major_build_token(previous_build) not in {None, major_build_token(current_build)} if current_build else False
        entry = {
            "tweak_id": tweak_id,
            "history": history,
            "revalidation_needed_on_major_update": True,
            "current_build": current_build,
            "current_class": class_entry.get("evidence_class"),
            "current_default": current_snapshot.get("default"),
            "major_build_changed": major_changed,
        }
        entries.append(entry)
        if major_changed:
            revalidation_queue.append(
                {
                    "tweak_id": tweak_id,
                    "from_build": previous_build,
                    "to_build": current_build,
                    "reason": "major-build-change",
                }
            )

    payload = {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "entries": entries,
        "revalidation_queue": revalidation_queue,
    }
    with OUTPUT_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    print(f"Wrote regression history to {OUTPUT_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
