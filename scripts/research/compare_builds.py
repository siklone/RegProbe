#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path


def parse_reg(path: Path) -> dict[str, list[str]]:
    current_key: str | None = None
    data: dict[str, list[str]] = {}

    for raw_line in path.read_text(encoding="utf-16", errors="replace").splitlines():
        line = raw_line.rstrip()
        if not line or line.startswith(";") or line.startswith("Windows Registry Editor"):
            continue
        if line.startswith("[") and line.endswith("]"):
            current_key = line[1:-1]
            data.setdefault(current_key, [])
            continue
        if current_key is not None:
            data[current_key].append(line)

    return data


def load_record_index(records_dir: Path) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for path in sorted(records_dir.glob("*.json")):
        try:
            payload = json.loads(path.read_text())
        except Exception:
            continue
        record_id = payload.get("record_id") or path.stem
        key_path = (payload.get("key_path") or payload.get("registry_path") or "").strip()
        if key_path:
            rows.append({"record_id": record_id, "key_path": key_path})
    return rows


def match_records(changed_keys: list[str], record_index: list[dict[str, str]]) -> list[dict[str, object]]:
    matches: list[dict[str, object]] = []
    lowered = [(item["record_id"], item["key_path"], item["key_path"].lower()) for item in record_index]
    for key in changed_keys:
        key_lower = key.lower()
        hit_ids = [
            record_id
            for record_id, key_path, path_lower in lowered
            if key_lower.startswith(path_lower) or path_lower.startswith(key_lower)
        ]
        if hit_ids:
            matches.append({"key": key, "record_ids": sorted(set(hit_ids))})
    return matches


def main() -> int:
    parser = argparse.ArgumentParser(description="Compare two registry baseline .reg exports.")
    parser.add_argument("old_build", type=Path)
    parser.add_argument("new_build", type=Path)
    parser.add_argument(
        "--records-dir",
        type=Path,
        default=Path("research/records"),
        help="Record directory used for cross-reference matching.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Optional explicit output path.",
    )
    args = parser.parse_args()

    old_data = parse_reg(args.old_build)
    new_data = parse_reg(args.new_build)

    old_keys = set(old_data)
    new_keys = set(new_data)
    added = sorted(new_keys - old_keys)
    removed = sorted(old_keys - new_keys)
    changed = sorted(key for key in old_keys & new_keys if old_data[key] != new_data[key])

    record_index = load_record_index(args.records_dir)

    output = args.output
    if output is None:
        output = Path("evidence/builds") / f"delta-{args.old_build.stem}-{args.new_build.stem}.json"
    output.parent.mkdir(parents=True, exist_ok=True)

    payload = {
        "old_build": args.old_build.as_posix(),
        "new_build": args.new_build.as_posix(),
        "added_keys": added,
        "removed_keys": removed,
        "changed_keys": changed,
        "record_matches": {
            "added": match_records(added, record_index),
            "removed": match_records(removed, record_index),
            "changed": match_records(changed, record_index),
        },
    }

    output.write_text(json.dumps(payload, indent=2) + "\n")
    print(json.dumps(payload, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
