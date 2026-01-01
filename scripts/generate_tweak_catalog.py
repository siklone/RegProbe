#!/usr/bin/env python3
from __future__ import annotations

import csv
import html
import os
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
OUTPUT_HTML = REPO_ROOT / "Docs" / "tweaks" / "tweak-catalog.html"
DOC_INDEX_START = "<!-- TWEAK INDEX START -->"
DOC_INDEX_END = "<!-- TWEAK INDEX END -->"

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
    description: str
    risk: str
    category: str
    area: str
    source: str
    docs: str


def shorten_description(text: str, limit: int = 140) -> str:
    cleaned = " ".join(text.split())
    if len(cleaned) <= limit:
        return cleaned
    return cleaned[: max(0, limit - 3)].rstrip() + "..."


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


def extract_metadata(chunk: str) -> Tuple[Optional[str], Optional[str], Optional[str], Optional[str]]:
    id_match = re.search(r"\bid\s*:\s*@?\"([^\"]+)\"", chunk)
    name_match = re.search(r"\bname\s*:\s*@?\"([^\"]+)\"", chunk)
    desc_match = re.search(r"\bdescription\s*:\s*@?\"([^\"]+)\"", chunk)
    risk_match = re.search(r"\brisk\s*:\s*TweakRiskLevel\.([A-Za-z]+)", chunk)
    if not risk_match:
        risk_match = re.search(r"\bTweakRiskLevel\.([A-Za-z]+)", chunk)

    strings = re.findall(r"@?\"([^\"]*)\"", chunk)
    tweak_id = id_match.group(1) if id_match else (strings[0] if len(strings) >= 1 else None)
    name = name_match.group(1) if name_match else (strings[1] if len(strings) >= 2 else None)
    description = desc_match.group(1) if desc_match else (strings[2] if len(strings) >= 3 else None)
    risk = risk_match.group(1) if risk_match else None
    return tweak_id, name, description, risk


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


def doc_href(doc_path: str, tweak_id: str) -> str:
    if not doc_path:
        return ""
    path = Path(doc_path)
    if not path.is_absolute():
        path = (REPO_ROOT / path).resolve()
    try:
        relative = path.relative_to(OUTPUT_HTML.parent)
    except ValueError:
        relative = Path(os.path.relpath(path, OUTPUT_HTML.parent))
    href = relative.as_posix()
    if tweak_id:
        href = f"{href}#{tweak_id}"
    return href


def extract_entries_from_file(path: Path, pattern: re.Pattern) -> Iterable[Tuple[str, str, str, str, str]]:
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
        tweak_id, name, description, risk = extract_metadata(chunk)
        if not tweak_id or not name:
            continue
        line = text.count("\n", 0, match.start()) + 1
        source = f"{path.relative_to(REPO_ROOT)}#L{line}"
        area = infer_area(call_name, path)
        yield tweak_id, name, description or "", risk or "Unknown", area, source


def extract_entries_from_base_call(path: Path) -> Iterable[Tuple[str, str, str, str, str]]:
    text = path.read_text(encoding="utf-8")
    for match in BASE_CALL_RE.finditer(text):
        open_paren = text.find("(", match.end() - 1)
        if open_paren == -1:
            continue
        close_paren = find_matching_paren(text, open_paren)
        if close_paren == -1:
            continue
        chunk = text[open_paren + 1 : close_paren]
        tweak_id, name, description, risk = extract_metadata(chunk)
        if not tweak_id or not name:
            continue
        line = text.count("\n", 0, match.start()) + 1
        source = f"{path.relative_to(REPO_ROOT)}#L{line}"
        area = infer_area("base", path)
        yield tweak_id, name, description or "", risk or "Unknown", area, source


def collect_entries() -> List[TweakEntry]:
    entries: Dict[str, TweakEntry] = {}

    provider_paths = sorted(PROVIDER_ROOT.glob("*.cs"))
    legacy_path = PROVIDER_ROOT / "LegacyTweakProvider.cs"

    for path in provider_paths:
        if path == legacy_path:
            continue
        for tweak_id, name, description, risk, area, source in extract_entries_from_file(path, CREATE_CALL_RE):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                description=description,
                risk=risk,
                category=category_for_id(tweak_id),
                area=area,
                source=source,
                docs=doc_for_id(tweak_id),
            )

    for path in sorted(ENGINE_ROOT.rglob("*.cs")):
        for tweak_id, name, description, risk, area, source in extract_entries_from_file(path, NEW_CALL_RE):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                description=description,
                risk=risk,
                category=category_for_id(tweak_id),
                area=area,
                source=source,
                docs=doc_for_id(tweak_id),
            )
        for tweak_id, name, description, risk, area, source in extract_entries_from_base_call(path):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                description=description,
                risk=risk,
                category=category_for_id(tweak_id),
                area=area,
                source=source,
                docs=doc_for_id(tweak_id),
            )

    if legacy_path.exists():
        for tweak_id, name, description, risk, area, source in extract_entries_from_file(legacy_path, CREATE_CALL_RE):
            key = tweak_id.lower()
            if key in entries:
                continue
            entries[key] = TweakEntry(
                tweak_id=tweak_id,
                name=name,
                description=description,
                risk=risk,
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
        "| ID | Name | Category | Area | Changes | Risk | Source | Docs |",
        "| --- | --- | --- | --- | --- | --- | --- | --- |",
    ]

    for entry in entries:
        source = f"`{entry.source}`"
        docs_link = doc_href(entry.docs, entry.tweak_id)
        docs = f"[{entry.docs}]({docs_link})" if docs_link else f"`{entry.docs}`"
        anchor = f"<a id=\"{html.escape(entry.tweak_id)}\"></a>"
        changes = shorten_description(entry.description)
        lines.append(
            f"| {anchor} `{entry.tweak_id}` | {entry.name} | {entry.category} | {entry.area} | {changes} | {entry.risk} | {source} | {docs} |"
        )

    OUTPUT_MD.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_csv(entries: List[TweakEntry]) -> None:
    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_CSV.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(["id", "name", "description", "risk", "category", "area", "source", "docs"])
        for entry in entries:
            writer.writerow([
                entry.tweak_id,
                entry.name,
                entry.description,
                entry.risk,
                entry.category,
                entry.area,
                entry.source,
                entry.docs,
            ])


def write_html(entries: List[TweakEntry]) -> None:
    OUTPUT_HTML.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "<!doctype html>",
        "<html lang=\"en\">",
        "<head>",
        "  <meta charset=\"utf-8\">",
        "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">",
        "  <title>Tweak Catalog</title>",
        "  <style>",
        "    body { font-family: Segoe UI, Arial, sans-serif; background: #f6f7fb; color: #1f2430; }",
        "    .container { max-width: 1200px; margin: 24px auto; padding: 0 16px; }",
        "    h1 { font-size: 22px; margin: 0 0 12px; }",
        "    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; }",
        "    thead { background: #2f3a4a; color: #fff; }",
        "    th, td { padding: 10px 12px; font-size: 13px; border-bottom: 1px solid #e5e8ef; vertical-align: top; }",
        "    td.changes { max-width: 360px; }",
        "    tbody tr:nth-child(even) { background: #f9fafc; }",
        "    code { font-family: Consolas, monospace; font-size: 12px; }",
        "  </style>",
        "</head>",
        "<body>",
        "  <div class=\"container\">",
        "    <h1>Tweak Catalog</h1>",
        "    <p>Generated by scripts/generate_tweak_catalog.py</p>",
        "    <table>",
        "      <thead>",
        "        <tr>",
        "          <th>ID</th>",
        "          <th>Name</th>",
        "          <th>Category</th>",
        "          <th>Area</th>",
        "          <th>Changes</th>",
        "          <th>Risk</th>",
        "          <th>Source</th>",
        "          <th>Docs</th>",
        "        </tr>",
        "      </thead>",
        "      <tbody>",
    ]

    for entry in entries:
        tweak_id = html.escape(entry.tweak_id)
        changes = html.escape(shorten_description(entry.description))
        docs_href = doc_href(entry.docs, entry.tweak_id)
        docs_label = html.escape(entry.docs)
        docs_cell = (
            f"<a href=\"{html.escape(docs_href)}\"><code>{docs_label}</code></a>"
            if docs_href
            else f"<code>{docs_label}</code>"
        )
        lines.append(
            "        <tr id=\"{tweak_id}\">"
            "<td><code>{tweak_id}</code></td>"
            "<td>{name}</td>"
            "<td>{category}</td>"
            "<td>{area}</td>"
            "<td class=\"changes\">{changes}</td>"
            "<td>{risk}</td>"
            "<td><code>{source}</code></td>"
            "<td>{docs}</td>"
            "</tr>".format(
                tweak_id=tweak_id,
                name=html.escape(entry.name),
                category=html.escape(entry.category),
                area=html.escape(entry.area),
                changes=changes,
                risk=html.escape(entry.risk),
                source=html.escape(entry.source),
                docs=docs_cell,
            )
        )

    lines.extend([
        "      </tbody>",
        "    </table>",
        "  </div>",
        "</body>",
        "</html>",
    ])

    OUTPUT_HTML.write_text("\n".join(lines) + "\n", encoding="utf-8")


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


def render_doc_index(entries: List[TweakEntry]) -> str:
    lines = [
        DOC_INDEX_START,
        "## Tweak Index (Generated)",
        "",
        "This section is generated from `Docs/tweaks/tweak-catalog.csv`.",
        "Do not edit manually.",
        "",
        "| ID | Name | Changes | Risk | Source |",
        "| --- | --- | --- | --- | --- |",
    ]

    for entry in entries:
        anchor = f"<a id=\"{html.escape(entry.tweak_id)}\"></a>"
        changes = shorten_description(entry.description)
        lines.append(
            f"| {anchor} `{entry.tweak_id}` | {entry.name} | {changes} | {entry.risk} | `{entry.source}` |"
        )

    lines.append(DOC_INDEX_END)
    return "\n".join(lines)


def write_doc_indexes(entries: List[TweakEntry]) -> None:
    entries_by_doc: Dict[str, List[TweakEntry]] = {}
    for entry in entries:
        entries_by_doc.setdefault(entry.docs, []).append(entry)

    for doc_path, doc_entries in entries_by_doc.items():
        full_path = REPO_ROOT / doc_path
        if not full_path.exists():
            continue

        doc_entries = sorted(doc_entries, key=lambda item: item.tweak_id.lower())
        generated = render_doc_index(doc_entries)
        text = full_path.read_text(encoding="utf-8")

        if DOC_INDEX_START in text and DOC_INDEX_END in text:
            start = text.index(DOC_INDEX_START)
            end = text.index(DOC_INDEX_END) + len(DOC_INDEX_END)
            updated = text[:start].rstrip() + "\n\n" + generated + "\n"
            remaining = text[end:].lstrip()
            if remaining:
                updated += "\n" + remaining
        else:
            updated = text.rstrip() + "\n\n" + generated + "\n"

        full_path.write_text(updated, encoding="utf-8")


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
    write_html(entries)
    write_test_template(entries)
    write_doc_indexes(entries)
    print(f"Generated {len(entries)} tweaks -> {OUTPUT_MD}, {OUTPUT_CSV}, {OUTPUT_HTML}, {OUTPUT_TEST_TEMPLATE}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
