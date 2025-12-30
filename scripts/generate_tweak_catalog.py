#!/usr/bin/env python3
from __future__ import annotations

import csv
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Tuple

REPO_ROOT = Path(__file__).resolve().parents[1]
PROVIDER_ROOT = REPO_ROOT / "WindowsOptimizer.App" / "Services" / "TweakProviders"
ENGINE_ROOT = REPO_ROOT / "WindowsOptimizer.Engine" / "Tweaks"
OUTPUT_MD = REPO_ROOT / "Docs" / "tweaks" / "tweak-catalog.md"
OUTPUT_CSV = REPO_ROOT / "Docs" / "tweaks" / "tweak-catalog.csv"
OUTPUT_TEST_TEMPLATE = REPO_ROOT / "Docs" / "tweaks" / "tweak-test-template.csv"

DOC_MAP = {
    "privacy": "Docs/privacy/privacy.md",
    "security": "Docs/security/security.md",
    "network": "Docs/network/network.md",
    "power": "Docs/power/power.md",
    "system": "Docs/system/system.md",
    "visibility": "Docs/visibility/visibility.md",
    "peripheral": "Docs/peripheral/peripheral.md",
    "audio": "Docs/peripheral/peripheral.md",
    "misc": "Docs/misc/misc.md",
    "cleanup": "Docs/cleanup/cleanup.md",
    "explorer": "Docs/visibility/visibility.md",
    "notifications": "Docs/notifications/notifications.md",
    "performance": "Docs/performance/performance.md",
}
DEFAULT_DOC = "Docs/tweaks/tweaks.md"

AREA_BY_CREATE_CALL = {
    "CreateRegistryTweak": "Registry",
    "CreateRegistryValueSetTweak": "Registry",
    "CreateRegistryValueBatchTweak": "Registry",
    "CreateCompositeTweak": "Composite",
    "CreateServiceStartModeBatchTweak": "Service",
    "CreateScheduledTaskBatchTweak": "Task",
    "CreateFileRenameTweak": "File",
}

CREATE_CALL_RE = re.compile(r"yield\s+return\s+(?P<call>Create[A-Za-z0-9_]+)\s*\(")
NEW_CALL_RE = re.compile(r"return\s+new\s+(?P<call>[A-Za-z0-9_]*Tweak)\s*\(")
BASE_CALL_RE = re.compile(r":\s*base\s*\(")


@dataclass(frozen=True)
class TweakEntry:
    tweak_id: str
    name: str
    category: str
    area: str
    source: str
    docs: str


def find_matching_paren(text: str, start: int) -> int:
    depth = 0
    in_string = False
    verbatim = False
    escape = False
    i = start
    while i < len(text):
        ch = text[i]
        if in_string:
            if verbatim:
                if ch == '"' and i + 1 < len(text) and text[i + 1] == '"':
                    i += 1
                elif ch == '"':
                    in_string = False
                    verbatim = False
            else:
                if escape:
                    escape = False
                elif ch == "\\":
                    escape = True
                elif ch == '"':
                    in_string = False
        else:
            if ch == '"':
                in_string = True
                verbatim = i > 0 and text[i - 1] == '@'
            elif ch == '(':
                depth += 1
            elif ch == ')':
                depth -= 1
                if depth == 0:
                    return i
        i += 1
    return -1


def extract_id_name(chunk: str) -> Tuple[Optional[str], Optional[str]]:
    id_match = re.search(r"\bid\s*:\s*@?\"([^\"]+)\"", chunk)
    name_match = re.search(r"\bname\s*:\s*@?\"([^\"]+)\"", chunk)
    if id_match and name_match:
        return id_match.group(1), name_match.group(1)

    strings = re.findall(r"@?\"([^\"]*)\"", chunk)
    if len(strings) >= 2:
        return strings[0], strings[1]
    return None, None


def infer_area(call_name: str, source_path: Path) -> str:
    if call_name in AREA_BY_CREATE_CALL:
        return AREA_BY_CREATE_CALL[call_name]

    normalized = call_name.lower()
    if "registry" in normalized:
        return "Registry"
    if "scheduledtask" in normalized or "task" in normalized:
        return "Task"
    if "service" in normalized:
        return "Service"
    if "filerename" in normalized or "filecleanup" in normalized:
        return "File"
    if "composite" in normalized:
        return "Composite"

    parts = {part.lower() for part in source_path.parts}
    if "commands" in parts:
        if "cleanup" in parts:
            return "Cleanup"
        return "Command"

    return "Other"


def category_for_id(tweak_id: str) -> str:
    if not tweak_id or "." not in tweak_id:
        return "Other"
    prefix = tweak_id.split(".", 1)[0]
    return prefix.capitalize()


def doc_for_id(tweak_id: str) -> str:
    if not tweak_id or "." not in tweak_id:
        return DEFAULT_DOC
    prefix = tweak_id.split(".", 1)[0].lower()
    return DOC_MAP.get(prefix, DEFAULT_DOC)


def extract_entries_from_file(path: Path, pattern: re.Pattern) -> Iterable[Tuple[str, str, str]]:
    text = path.read_text(encoding="utf-8")
    for match in pattern.finditer(text):
        call_name = match.group("call")
        open_paren = text.find("(", match.end() - 1)
        if open_paren == -1:
            continue
        close_paren = find_matching_paren(text, open_paren)
        if close_paren == -1:
            continue
        chunk = text[open_paren + 1 : close_paren]
        tweak_id, name = extract_id_name(chunk)
        if not tweak_id or not name:
            continue
        line = text.count("\n", 0, match.start()) + 1
        source = f"{path.relative_to(REPO_ROOT)}#L{line}"
        area = infer_area(call_name, path)
        yield tweak_id, name, area, source


def extract_entries_from_base_call(path: Path) -> Iterable[Tuple[str, str, str]]:
    text = path.read_text(encoding="utf-8")
    for match in BASE_CALL_RE.finditer(text):
        open_paren = text.find("(", match.end() - 1)
        if open_paren == -1:
            continue
        close_paren = find_matching_paren(text, open_paren)
        if close_paren == -1:
            continue
        chunk = text[open_paren + 1 : close_paren]
        tweak_id, name = extract_id_name(chunk)
        if not tweak_id or not name:
            continue
        line = text.count("\n", 0, match.start()) + 1
        source = f"{path.relative_to(REPO_ROOT)}#L{line}"
        area = infer_area("base", path)
        yield tweak_id, name, area, source


def collect_entries() -> List[TweakEntry]:
    entries: Dict[str, TweakEntry] = {}

    provider_paths = sorted(PROVIDER_ROOT.glob("*.cs"))
    legacy_path = PROVIDER_ROOT / "LegacyTweakProvider.cs"

    for path in provider_paths:
        if path == legacy_path:
            continue
        for tweak_id, name, area, source in extract_entries_from_file(path, CREATE_CALL_RE):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                category=category_for_id(tweak_id),
                area=area,
                source=source,
                docs=doc_for_id(tweak_id),
            )

    for path in sorted(ENGINE_ROOT.rglob("*.cs")):
        for tweak_id, name, area, source in extract_entries_from_file(path, NEW_CALL_RE):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                category=category_for_id(tweak_id),
                area=area,
                source=source,
                docs=doc_for_id(tweak_id),
            )
        for tweak_id, name, area, source in extract_entries_from_base_call(path):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                category=category_for_id(tweak_id),
                area=area,
                source=source,
                docs=doc_for_id(tweak_id),
            )

    if legacy_path.exists():
        for tweak_id, name, area, source in extract_entries_from_file(legacy_path, CREATE_CALL_RE):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                category=category_for_id(tweak_id),
                area=area,
                source=source,
                docs=doc_for_id(tweak_id),
            )

    return sorted(entries.values(), key=lambda entry: entry.tweak_id.lower())


def write_markdown(entries: List[TweakEntry]) -> None:
    lines = [
        "# Tweak Catalog (Generated)",
        "",
        "Generated by `scripts/generate_tweak_catalog.py`. Do not edit this file manually.",
        "",
        "Note: categories without a dedicated doc fall back to `Docs/tweaks/tweaks.md`.",
        "",
        "| ID | Name | Category | Area | Source | Docs |",
        "| --- | --- | --- | --- | --- | --- |",
    ]

    for entry in entries:
        source = f"`{entry.source}`"
        docs = f"`{entry.docs}`"
        lines.append(
            f"| `{entry.tweak_id}` | {entry.name} | {entry.category} | {entry.area} | {source} | {docs} |"
        )

    OUTPUT_MD.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_csv(entries: List[TweakEntry]) -> None:
    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_CSV.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(["id", "name", "category", "area", "source", "docs"])
        for entry in entries:
            writer.writerow([
                entry.tweak_id,
                entry.name,
                entry.category,
                entry.area,
                entry.source,
                entry.docs,
            ])


def write_test_template(entries: List[TweakEntry]) -> None:
    OUTPUT_TEST_TEMPLATE.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_TEST_TEMPLATE.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow([
            "id",
            "name",
            "category",
            "area",
            "docs",
            "source",
            "detect_status",
            "preview_status",
            "apply_status",
            "verify_status",
            "rollback_status",
            "notes",
            "tested_by",
            "tested_at",
        ])
        for entry in entries:
            writer.writerow([
                entry.tweak_id,
                entry.name,
                entry.category,
                entry.area,
                entry.docs,
                entry.source,
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
            ])


def main() -> int:
    if not PROVIDER_ROOT.exists() or not ENGINE_ROOT.exists():
        print("Could not locate tweak sources. Run from repo root.")
        return 1

    entries = collect_entries()
    if not entries:
        print("No tweaks found. Check parsing rules.")
        return 1

    write_markdown(entries)
    write_csv(entries)
    write_test_template(entries)
    print(f"Generated {len(entries)} tweaks -> {OUTPUT_MD}, {OUTPUT_CSV}, {OUTPUT_TEST_TEMPLATE}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
