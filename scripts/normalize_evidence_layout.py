#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import shutil
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent
RESEARCH_ROOT = REPO_ROOT / "research"
RECORDS_DIR = RESEARCH_ROOT / "records"
NOTES_DIR = RESEARCH_ROOT / "notes"
EVIDENCE_ROOT = RESEARCH_ROOT / "evidence-files"

PATH_PATTERN = re.compile(
    r"(research/evidence-files/[A-Za-z0-9._/\-]+(?:/[A-Za-z0-9._/\-]+)*)"
)

GHIDRA_KINDS = {"ghidra-headless", "ghidra-trace"}
PROCMON_KINDS = {"procmon-trace"}


def normalize_slashes(text: str) -> str:
    return text.replace("\\", "/")


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: dict) -> None:
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def copy_path(source_rel: str, bucket: str, record_id: str) -> str | None:
    source_rel = normalize_slashes(source_rel)
    source_path = REPO_ROOT / Path(source_rel)
    if not source_path.exists():
        return None

    target_root = EVIDENCE_ROOT / bucket / record_id
    target_root.mkdir(parents=True, exist_ok=True)

    if source_path.is_dir():
        target_path = target_root / source_path.name
        if target_path.resolve() == source_path.resolve():
            return normalize_slashes(str(target_path.relative_to(REPO_ROOT)))
        if target_path.exists():
            shutil.rmtree(target_path)
        shutil.copytree(source_path, target_path)
    else:
        target_path = target_root / source_path.name
        if target_path.resolve() == source_path.resolve():
            return normalize_slashes(str(target_path.relative_to(REPO_ROOT)))
        shutil.copy2(source_path, target_path)

    return normalize_slashes(str(target_path.relative_to(REPO_ROOT)))


def rewrite_location(location: str, mapping: dict[str, str]) -> str:
    updated = normalize_slashes(location)
    for old, new in mapping.items():
        updated = updated.replace(old, new)
    return updated


def process_records() -> dict[str, str]:
    path_map: dict[str, str] = {}

    for record_path in sorted(RECORDS_DIR.glob("*.json")):
        record = load_json(record_path)
        record_id = str(record.get("record_id") or record.get("tweak_id") or record_path.stem)
        changed = False

        for evidence in record.get("evidence", []) or []:
            kind = str(evidence.get("kind") or "").strip()
            bucket = None
            if kind in GHIDRA_KINDS:
                bucket = "ghidra"
            elif kind in PROCMON_KINDS:
                bucket = "procmon"

            if not bucket:
                continue

            location = str(evidence.get("location") or "")
            matches = sorted(set(PATH_PATTERN.findall(normalize_slashes(location))))
            if not matches:
                continue

            local_map: dict[str, str] = {}
            for match in matches:
                target_rel = path_map.get(match)
                if not target_rel:
                    target_rel = copy_path(match, bucket, record_id)
                    if not target_rel:
                        continue
                    path_map[match] = target_rel
                local_map[match] = target_rel

            if local_map:
                new_location = rewrite_location(location, local_map)
                if new_location != location:
                    evidence["location"] = new_location
                    changed = True

        if changed:
            write_json(record_path, record)

    return path_map


def process_notes(path_map: dict[str, str]) -> int:
    changed_count = 0
    for note_path in sorted(NOTES_DIR.glob("*.md")):
        text = note_path.read_text(encoding="utf-8")
        updated = normalize_slashes(text)
        for old, new in path_map.items():
            updated = updated.replace(old, new)
        if updated != text:
            note_path.write_text(updated, encoding="utf-8", newline="\n")
            changed_count += 1
    return changed_count


def bucket_for_path(source_rel: str) -> str | None:
    lowered = normalize_slashes(source_rel).lower()
    name = Path(lowered).name
    if "ghidra" in lowered or name.endswith(".java"):
        return "ghidra"
    procmon_hints = (
        "procmon",
        ".pml",
        ".csv",
        "hits.csv",
        "result.txt",
        "_probe.txt",
        "regquery",
    )
    if any(hint in lowered for hint in procmon_hints):
        return "procmon"
    return None


def process_note_embedded_paths() -> int:
    changed_count = 0
    for note_path in sorted(NOTES_DIR.glob("*.md")):
        text = note_path.read_text(encoding="utf-8")
        matches = sorted(set(PATH_PATTERN.findall(normalize_slashes(text))))
        if not matches:
            continue

        local_map: dict[str, str] = {}
        note_group = note_path.stem
        for match in matches:
            bucket = bucket_for_path(match)
            if not bucket:
                continue
            target_rel = copy_path(match, bucket, note_group)
            if target_rel:
                local_map[match] = target_rel

        if not local_map:
            continue

        updated = normalize_slashes(text)
        for old, new in local_map.items():
            updated = updated.replace(old, new)
        if updated != text:
            note_path.write_text(updated, encoding="utf-8", newline="\n")
            changed_count += 1

    return changed_count


def main() -> int:
    path_map = process_records()
    note_changes = process_notes(path_map)
    embedded_note_changes = process_note_embedded_paths()
    print(f"Copied {len(path_map)} evidence path(s)")
    print(f"Updated {note_changes} note file(s)")
    print(f"Updated {embedded_note_changes} note embedded path file(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
