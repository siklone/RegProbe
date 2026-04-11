from __future__ import annotations

import argparse
import codecs
import json
import re
import sys
from collections import Counter
from pathlib import Path


HEADER_PREFIXES = ("Windows Registry Editor Version", "REGEDIT4")
KEY_LINE_RE = re.compile(r"^\[(?P<path>-?.+)\]$")
VALUE_LINE_RE = re.compile(r'^(?P<name>@|"(?:[^"\\]|\\.)*")=(?P<data>.*)$')
DUMP_VALUE_RE = re.compile(r"^(?P<name>.+?)\s{2,}(?P<type>REG_[A-Z0-9_]+)\s{2,}(?P<data>.*)$")
NOISE_LINE_RE = re.compile(
    r"\b(NO MORE ENTRIES|NAME NOT FOUND|BUFFER OVERFLOW|KEY DELETED|PATH NOT FOUND|END OF FILE)\b",
    re.IGNORECASE,
)


def read_registry_text(path: Path) -> str:
    raw = path.read_bytes()
    if raw.startswith((codecs.BOM_UTF16_LE, codecs.BOM_UTF16_BE)):
        return raw.decode("utf-16")
    if raw.startswith(codecs.BOM_UTF8):
        return raw.decode("utf-8-sig")
    try:
        return raw.decode("utf-8")
    except UnicodeDecodeError:
        if b"\x00" in raw[:256]:
            return raw.decode("utf-16le")
        return raw.decode("latin-1")


def normalize_newlines(text: str) -> list[str]:
    if not text:
        return []
    return text.replace("\r\n", "\n").replace("\r", "\n").split("\n")


def iter_logical_lines(text: str):
    pending: str | None = None
    for raw_line in normalize_newlines(text):
        line = raw_line.lstrip("\ufeff")
        current = line if pending is None else pending + line.lstrip()
        stripped = current.rstrip()
        if stripped.endswith("\\") and not stripped.startswith("["):
            pending = stripped[:-1]
            continue
        yield current
        pending = None
    if pending is not None:
        yield pending


def test_is_registry_export_text(text: str) -> bool:
    return bool(
        re.search(r"Windows\s+Registry\s+Editor\s+Version", text)
        or re.search(r"(?m)^\s*\[(?:-)?HKEY_", text)
    )


def test_is_registry_dump_text(text: str) -> bool:
    return bool(re.search(r"(?m)^\s*HKEY_[A-Z_]+\\", text))


def test_is_noise_line(line: str) -> bool:
    return not line.strip() or bool(NOISE_LINE_RE.search(line))


def convert_registry_value_name(token: str) -> str:
    if token == "@":
        return "(Default)"
    if len(token) >= 2 and token[0] == '"' and token[-1] == '"':
        token = token[1:-1]
    return token.replace(r"\"", '"').replace(r"\\", "\\")


def normalize_value_data(raw_data: str) -> tuple[str, str]:
    data = raw_data.strip()
    if not data:
        return "", ""
    if data == "-":
        return "deleted", "(deleted)"
    if data.startswith('"') and data.endswith('"'):
        text = data[1:-1].replace(r"\"", '"').replace(r"\\", "\\").rstrip()
        return "string", text

    value_type, separator, payload = data.partition(":")
    if not separator:
        compact = re.sub(r"\s+", " ", data).strip()
        return "raw", compact

    normalized_type = value_type.strip().lower()
    normalized_payload = payload.strip()
    if normalized_type.startswith("hex"):
        pieces = [piece.strip().lower() for piece in normalized_payload.split(",") if piece.strip()]
        if normalized_type == "hex(2)":
            try:
                decoded = bytes.fromhex("".join(pieces)).decode("utf-16le", errors="ignore").rstrip("\x00").rstrip()
                return "expand_sz", decoded
            except ValueError:
                normalized_payload = ",".join(pieces)
        elif normalized_type == "hex(7)":
            try:
                decoded = bytes.fromhex("".join(pieces)).decode("utf-16le", errors="ignore").rstrip("\x00")
                return "multi_sz", ";".join(part for part in decoded.split("\x00") if part)
            except ValueError:
                normalized_payload = ",".join(pieces)
        else:
            normalized_type = "hex"
            normalized_payload = ",".join(pieces)
    elif normalized_type in {"dword", "qword"}:
        normalized_payload = normalized_payload.lower()
    else:
        normalized_payload = re.sub(r"\s+", " ", normalized_payload).strip()
    return normalized_type, normalized_payload


def normalize_dump_value_data(value_type: str, raw_data: str) -> tuple[str, str]:
    normalized_type = value_type.strip().upper()
    data = raw_data.strip()

    if normalized_type in {"REG_DWORD", "REG_DWORD_LITTLE_ENDIAN"}:
        token = data.split()[0] if data else "0"
        number = int(token, 16) if token.lower().startswith("0x") else int(token, 10)
        return "dword", f"{number:08x}"

    if normalized_type == "REG_QWORD":
        token = data.split()[0] if data else "0"
        number = int(token, 16) if token.lower().startswith("0x") else int(token, 10)
        return "qword", f"{number:016x}"

    if normalized_type == "REG_SZ":
        return "string", data.rstrip()

    if normalized_type == "REG_EXPAND_SZ":
        return "expand_sz", data.rstrip()

    if normalized_type == "REG_MULTI_SZ":
        return "multi_sz", re.sub(r"\s*;\s*", ";", data)

    if normalized_type in {"REG_BINARY", "REG_NONE"}:
        tokens = [piece.lower() for piece in re.split(r"[\s,]+", data) if piece]
        if len(tokens) == 1 and re.fullmatch(r"[0-9a-fA-F]+", tokens[0]) and len(tokens[0]) % 2 == 0:
            tokens = [tokens[0][i : i + 2] for i in range(0, len(tokens[0]), 2)]
        compact = ",".join(tokens)
        return "hex", compact

    return normalized_type.lower(), re.sub(r"\s+", " ", data).strip()


def get_value_entry_id(key_path: str, value_name: str) -> str:
    return f"{key_path}\n{value_name}"


def parse_registry_export(text: str) -> dict[str, dict[str, dict[str, str]]]:
    keys: dict[str, dict[str, str]] = {}
    values: dict[str, dict[str, str]] = {}
    current_key: str | None = None

    for line in iter_logical_lines(text):
        stripped = line.strip()
        if not stripped or stripped.startswith((";", "#")):
            continue
        if stripped.startswith(HEADER_PREFIXES):
            continue

        key_match = KEY_LINE_RE.match(stripped)
        if key_match:
            path = key_match.group("path").strip()
            is_deleted = path.startswith("-")
            if is_deleted:
                path = path[1:]
            current_key = path
            keys[path.casefold()] = {"KeyPath": path, "IsDeleted": "true" if is_deleted else "false"}
            continue

        if current_key is None:
            continue

        value_match = VALUE_LINE_RE.match(stripped)
        if not value_match:
            continue

        value_name = convert_registry_value_name(value_match.group("name"))
        value_type, data_text = normalize_value_data(value_match.group("data"))
        entry_id = get_value_entry_id(current_key, value_name)
        values[entry_id.casefold()] = {
            "KeyPath": current_key,
            "ValueName": value_name,
            "ValueType": value_type,
            "DataText": data_text,
        }

    return {"Keys": keys, "Values": values}


def parse_registry_dump_text(text: str) -> dict[str, dict[str, dict[str, str]]]:
    keys: dict[str, dict[str, str]] = {}
    values: dict[str, dict[str, str]] = {}
    current_key: str | None = None

    for raw_line in normalize_newlines(text):
        line = raw_line.strip("\ufeff").rstrip()
        stripped = line.strip()
        if not stripped:
            continue
        if stripped.startswith("HKEY_"):
            current_key = stripped
            keys[stripped.casefold()] = {"KeyPath": stripped, "IsDeleted": "false"}
            continue
        if current_key is None:
            continue

        match = DUMP_VALUE_RE.match(stripped)
        if not match:
            continue

        value_name = match.group("name").strip()
        value_type, data_text = normalize_dump_value_data(match.group("type"), match.group("data"))
        entry_id = get_value_entry_id(current_key, value_name)
        values[entry_id.casefold()] = {
            "KeyPath": current_key,
            "ValueName": value_name,
            "ValueType": value_type,
            "DataText": data_text,
        }

    return {"Keys": keys, "Values": values}


def get_line_summary_diff(before_text: str, after_text: str) -> dict[str, object]:
    before_all = normalize_newlines(before_text)
    after_all = normalize_newlines(after_text)
    ignored_before = sum(1 for line in before_all if test_is_noise_line(line))
    ignored_after = sum(1 for line in after_all if test_is_noise_line(line))
    before_lines = [line for line in before_all if not test_is_noise_line(line)]
    after_lines = [line for line in after_all if not test_is_noise_line(line)]
    before_counts = Counter(before_lines)
    after_counts = Counter(after_lines)

    added: list[dict[str, object]] = []
    removed: list[dict[str, object]] = []
    for line in sorted(set(before_counts) | set(after_counts)):
        before_count = before_counts.get(line, 0)
        after_count = after_counts.get(line, 0)
        if after_count > before_count:
            added.append({"Line": line, "Count": after_count - before_count})
        if before_count > after_count:
            removed.append({"Line": line, "Count": before_count - after_count})

    return {
        "BeforeLineCount": len(before_lines),
        "AfterLineCount": len(after_lines),
        "IgnoredBeforeNoise": ignored_before,
        "IgnoredAfterNoise": ignored_after,
        "Added": added,
        "Removed": removed,
    }


def shorten_text(text: str | None, max_length: int = 120) -> str:
    if not text:
        return ""
    if len(text) <= max_length:
        return text
    return text[: max_length - 3] + "..."


def add_section(lines: list[str], title: str, items: list[dict[str, object]], formatter, limit: int) -> None:
    lines.extend(["", title])
    if not items:
        lines.append("- none")
        return
    for item in items[:limit]:
        lines.append(formatter(item))
    if len(items) > limit:
        lines.append(f"... {len(items) - limit} more omitted")


def format_value_entry(entry: dict[str, str]) -> str:
    return f'- [{entry["KeyPath"]}] {entry["ValueName"]} | {entry["ValueType"]} | {shorten_text(entry["DataText"])}'


def format_modified_value_entry(entry: dict[str, str]) -> str:
    return (
        f'- [{entry["KeyPath"]}] {entry["ValueName"]} | '
        f'{entry["BeforeType"]}:{shorten_text(entry["BeforeData"])} -> '
        f'{entry["AfterType"]}:{shorten_text(entry["AfterData"])}'
    )


def build_diff_payload(before_path: Path, after_path: Path) -> dict[str, object]:
    before_text = read_registry_text(before_path)
    after_text = read_registry_text(after_path)

    before_is_export = test_is_registry_export_text(before_text)
    after_is_export = test_is_registry_export_text(after_text)
    before_is_dump = test_is_registry_dump_text(before_text)
    after_is_dump = test_is_registry_dump_text(after_text)
    before_is_semantic = before_is_export or before_is_dump
    after_is_semantic = after_is_export or after_is_dump

    if before_is_semantic and after_is_semantic:
        before_format = "registry-export" if before_is_export else "registry-dump-text"
        after_format = "registry-export" if after_is_export else "registry-dump-text"
        before = parse_registry_export(before_text) if before_is_export else parse_registry_dump_text(before_text)
        after = parse_registry_export(after_text) if after_is_export else parse_registry_dump_text(after_text)

        added_keys: list[dict[str, str]] = []
        removed_keys: list[dict[str, str]] = []
        added_values: list[dict[str, str]] = []
        removed_values: list[dict[str, str]] = []
        modified_values: list[dict[str, str]] = []

        all_keys = sorted(set(before["Keys"]) | set(after["Keys"]))
        all_value_ids = sorted(set(before["Values"]) | set(after["Values"]))

        for key_path in all_keys:
            in_before = key_path in before["Keys"]
            in_after = key_path in after["Keys"]
            if in_after and not in_before:
                added_keys.append(after["Keys"][key_path])
            elif in_before and not in_after:
                removed_keys.append(before["Keys"][key_path])

        for value_id in all_value_ids:
            in_before = value_id in before["Values"]
            in_after = value_id in after["Values"]
            if in_after and not in_before:
                added_values.append(after["Values"][value_id])
                continue
            if in_before and not in_after:
                removed_values.append(before["Values"][value_id])
                continue

            before_value = before["Values"][value_id]
            after_value = after["Values"][value_id]
            if (
                before_value["ValueType"] != after_value["ValueType"]
                or before_value["DataText"] != after_value["DataText"]
            ):
                modified_values.append(
                    {
                        "KeyPath": after_value["KeyPath"],
                        "ValueName": after_value["ValueName"],
                        "BeforeType": before_value["ValueType"],
                        "BeforeData": before_value["DataText"],
                        "AfterType": after_value["ValueType"],
                        "AfterData": after_value["DataText"],
                    }
                )

        unchanged_values = len(all_value_ids) - len(added_values) - len(removed_values) - len(modified_values)

        return {
            "title": "Registry sideeffect diff",
            "before_path": str(before_path),
            "after_path": str(after_path),
            "detected_format": "semantic-registry",
            "before_format": before_format,
            "after_format": after_format,
            "summary_counts": {
                "before_keys": len(before["Keys"]),
                "after_keys": len(after["Keys"]),
                "added_keys": len(added_keys),
                "removed_keys": len(removed_keys),
                "before_values": len(before["Values"]),
                "after_values": len(after["Values"]),
                "added_values": len(added_values),
                "removed_values": len(removed_values),
                "modified_values": len(modified_values),
                "unchanged_values": unchanged_values,
            },
            "sections": {
                "added_keys": sorted(added_keys, key=lambda item: item["KeyPath"]),
                "removed_keys": sorted(removed_keys, key=lambda item: item["KeyPath"]),
                "added_values": sorted(added_values, key=lambda item: (item["KeyPath"], item["ValueName"])),
                "removed_values": sorted(removed_values, key=lambda item: (item["KeyPath"], item["ValueName"])),
                "modified_values": sorted(modified_values, key=lambda item: (item["KeyPath"], item["ValueName"])),
            },
        }

    line_diff = get_line_summary_diff(before_text, after_text)
    added_line_count = sum(int(item["Count"]) for item in line_diff["Added"])
    removed_line_count = sum(int(item["Count"]) for item in line_diff["Removed"])

    return {
        "title": "Registry sideeffect diff",
        "before_path": str(before_path),
        "after_path": str(after_path),
        "detected_format": "generic-text",
        "before_format": "generic-text",
        "after_format": "generic-text",
        "summary_counts": {
            "before_lines": line_diff["BeforeLineCount"],
            "after_lines": line_diff["AfterLineCount"],
            "ignored_before_noise_lines": line_diff["IgnoredBeforeNoise"],
            "ignored_after_noise_lines": line_diff["IgnoredAfterNoise"],
            "added_lines": added_line_count,
            "removed_lines": removed_line_count,
        },
        "sections": {
            "added_line_samples": sorted(line_diff["Added"], key=lambda item: (-int(item["Count"]), str(item["Line"]))),
            "removed_line_samples": sorted(line_diff["Removed"], key=lambda item: (-int(item["Count"]), str(item["Line"]))),
        },
        "notes": [
            "semantic registry diff was skipped because one or both inputs do not look like supported registry exports or registry dump text.",
            "common registry noise lines are excluded from the generic text summary.",
        ],
    }


def build_diff_report(before_path: Path, after_path: Path, max_entries_per_section: int = 200) -> str:
    payload = build_diff_payload(before_path, after_path)

    lines = [
        str(payload["title"]),
        f"Before: {before_path}",
        f"After:  {after_path}",
        f"Max entries per section: {max_entries_per_section}",
    ]

    detected_format = str(payload["detected_format"])
    before_format = str(payload["before_format"])
    after_format = str(payload["after_format"])
    counts = dict(payload["summary_counts"])
    sections = dict(payload["sections"])

    if detected_format == "semantic-registry":
        lines.extend(
            [
                f"Detected format: semantic-registry ({before_format} -> {after_format})",
                "",
                "Summary counts",
                f"- before_keys: {counts['before_keys']}",
                f"- after_keys: {counts['after_keys']}",
                f"- added_keys: {counts['added_keys']}",
                f"- removed_keys: {counts['removed_keys']}",
                f"- before_values: {counts['before_values']}",
                f"- after_values: {counts['after_values']}",
                f"- added_values: {counts['added_values']}",
                f"- removed_values: {counts['removed_values']}",
                f"- modified_values: {counts['modified_values']}",
                f"- unchanged_values: {counts['unchanged_values']}",
            ]
        )

        add_section(
            lines,
            "Added keys",
            list(sections["added_keys"]),
            lambda item: f'- [{item["KeyPath"]}]',
            max_entries_per_section,
        )
        add_section(
            lines,
            "Removed keys",
            list(sections["removed_keys"]),
            lambda item: f'- [{item["KeyPath"]}]',
            max_entries_per_section,
        )
        add_section(
            lines,
            "Added values",
            list(sections["added_values"]),
            format_value_entry,
            max_entries_per_section,
        )
        add_section(
            lines,
            "Removed values",
            list(sections["removed_values"]),
            format_value_entry,
            max_entries_per_section,
        )
        add_section(
            lines,
            "Modified values",
            list(sections["modified_values"]),
            format_modified_value_entry,
            max_entries_per_section,
        )
    else:
        lines.extend(
            [
                "Detected format: generic-text",
                "",
                "Summary counts",
                f"- before_lines: {counts['before_lines']}",
                f"- after_lines: {counts['after_lines']}",
                f"- ignored_before_noise_lines: {counts['ignored_before_noise_lines']}",
                f"- ignored_after_noise_lines: {counts['ignored_after_noise_lines']}",
                f"- added_lines: {counts['added_lines']}",
                f"- removed_lines: {counts['removed_lines']}",
            ]
        )
        for note in payload.get("notes", []):
            lines.append(f"- note: {note}")

        add_section(
            lines,
            "Added line samples",
            list(sections["added_line_samples"]),
            lambda item: f"- ({item['Count']}x) {shorten_text(str(item['Line']))}",
            max_entries_per_section,
        )
        add_section(
            lines,
            "Removed line samples",
            list(sections["removed_line_samples"]),
            lambda item: f"- ({item['Count']}x) {shorten_text(str(item['Line']))}",
            max_entries_per_section,
        )

    return "\n".join(lines).rstrip() + "\n"


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate a semantic diff for registry sideeffect outputs.")
    parser.add_argument("--before", required=True, type=Path)
    parser.add_argument("--after", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--output-json", type=Path)
    parser.add_argument("--max-entries", type=int, default=200)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    report = build_diff_report(args.before, args.after, max_entries_per_section=args.max_entries)
    payload = build_diff_payload(args.before, args.after)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    if args.output_json is not None:
        args.output_json.parent.mkdir(parents=True, exist_ok=True)
        args.output_json.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
