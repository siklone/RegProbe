#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


COMMAND_RE = re.compile(
    r"""
    reg\s+add\s+"(?P<path>[^"]+)"
    \s+/v\s+"(?P<value_name>[^"]+)"
    \s+/?t\s+(?P<value_type>\S+)
    \s+/?d\s+"?(?P<value_data>[^"\s]+)"?
    \s+/?f\b
    """,
    re.IGNORECASE | re.VERBOSE,
)


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def slug(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9_.-]+", "-", value).strip("-").lower() or "item"


def normalize_reg_add_text(raw: str) -> str:
    text = raw.replace("\r\n", "\n").replace("\r", "\n")
    # Some pasted batches lose the newline between /f and the next command.
    text = re.sub(r"(?i)/f\s*(?=reg\s+add\s+\")", "/f\n", text)
    # The paste often drops the slash before t/d/f after a line break.
    text = re.sub(r"(?im)(\s)(t|d|f)(\s+REG_|\s+\"|\s*$)", r"\1/\2\3", text)
    return re.sub(r"\s+", " ", text).strip()


def parse_int(value: str) -> int | None:
    try:
        return int(value, 0)
    except ValueError:
        return None


def classify_risk(path: str, value_name: str, data: int | None) -> tuple[str, list[str]]:
    lower_path = path.lower()
    lower_name = value_name.lower()
    tags: list[str] = []

    if "session manager\\kernel" in lower_path:
        tags.extend(["kernel-session-manager", "boot-critical"])
    if "session manager\\executive" in lower_path:
        tags.extend(["nt-executive", "scheduler-critical"])
    if "control\\power" in lower_path or "session manager\\power" in lower_path:
        tags.extend(["power-manager", "reboot-sensitive"])
    if "policies\\system" in lower_path:
        tags.extend(["policy", "logon-sensitive"])
    if any(token in lower_name for token in ("exception", "controlflowguard", "cfg", "wer")):
        tags.append("security-sensitive")
    if any(token in lower_name for token in ("watchdog", "bugcheck", "crashdump")):
        tags.append("crash-path")
    if any(token in lower_name for token in ("workerthread", "workerthreads", "maximumkernelworker")):
        tags.append("threading-critical")
    if data is not None and data > 1024:
        tags.append("large-numeric-override")

    if {"boot-critical", "security-sensitive"} & set(tags):
        return "critical", sorted(set(tags))
    if {"scheduler-critical", "threading-critical", "crash-path"} & set(tags):
        return "high", sorted(set(tags))
    if {"reboot-sensitive", "power-manager", "logon-sensitive"} & set(tags):
        return "medium", sorted(set(tags))
    return "low", sorted(set(tags))


def candidate_values(value_name: str, requested: int | None) -> list[int]:
    values: list[int] = []
    for value in (requested, 0, 1):
        if value is not None and value not in values:
            values.append(value)
    if requested is not None and requested not in (0, 1):
        if requested > 1 and requested // 2 not in values:
            values.append(requested // 2)
        if requested < 64 and requested + 1 not in values:
            values.append(requested + 1)

    lower_name = value_name.lower()
    if "threshold" in lower_name or "timeout" in lower_name or "interval" in lower_name:
        for value in (10, 60, 300):
            if value not in values:
                values.append(value)
    if "maximum" in lower_name or "max" in lower_name:
        for value in (16, 64, 256):
            if value not in values:
                values.append(value)

    return values[:6]


def parse_batch(raw: str) -> dict[str, Any]:
    normalized = normalize_reg_add_text(raw)
    entries: list[dict[str, Any]] = []
    seen: set[tuple[str, str]] = set()

    for match in COMMAND_RE.finditer(normalized):
        path = match.group("path")
        value_name = match.group("value_name")
        value_type = match.group("value_type")
        value_data_text = match.group("value_data")
        value_data = parse_int(value_data_text)
        key = (path.lower(), value_name.lower())
        risk, tags = classify_risk(path, value_name, value_data)
        duplicate = key in seen
        seen.add(key)

        entries.append(
            {
                "id": f"{slug(value_name)}-{len(entries) + 1:03d}",
                "registry_path": path,
                "value_name": value_name,
                "value_type": value_type,
                "requested_data": value_data if value_data is not None else value_data_text,
                "requested_data_raw": value_data_text,
                "duplicate_in_batch": duplicate,
                "risk": risk,
                "tags": tags,
                "requires_snapshot_or_overlay": risk in {"critical", "high", "medium"},
                "candidate_values": candidate_values(value_name, value_data),
                "lanes": [
                    "qga-baseline-read",
                    "apply-smoke",
                    "reboot-smoke",
                    "rollback-smoke",
                    "etw-or-procmon-if-missing-or-opaque",
                ],
            }
        )

    return {
        "generated_utc": now_utc(),
        "status": "ok" if entries else "no-commands-found",
        "input_command_count": len(entries),
        "policy": {
            "do_not_batch_apply_raw": True,
            "require_snapshot_or_overlay_for_reboot_tests": True,
            "missing_value_lane": "If the value is absent or opaque, collect ETW/Procmon/static-string evidence instead of calling it nonexistent.",
            "smoke_contract": "baseline read -> apply -> app/process smoke -> reboot -> QGA health -> rollback -> reboot -> final smoke",
        },
        "entries": entries,
    }


def write_markdown(payload: dict[str, Any], path: Path) -> None:
    lines = [
        "# Registry Add Batch Experiment Plan",
        "",
        f"- Generated UTC: `{payload['generated_utc']}`",
        f"- Parsed commands: `{payload['input_command_count']}`",
        "- Policy: do not raw-apply the batch; run one value per snapshot/overlay lane.",
        "",
        "## Contract",
        "",
        "- Baseline read the exact key/value first.",
        "- If a value is missing, collect ETW, Procmon, or static-string evidence before closing it.",
        "- For present values, test sensible alternate values one at a time.",
        "- Smoke Start, shell, Store/UWP URI, x64 app launch, x86 app launch, reboot health, rollback, and final smoke.",
        "- Boot-critical lanes require snapshot or disposable overlay before apply.",
        "",
        "## Entries",
        "",
        "| # | Risk | Value | Requested | Candidate values | Tags |",
        "|---|------|-------|-----------|------------------|------|",
    ]
    for index, entry in enumerate(payload.get("entries", []), 1):
        tags = ", ".join(entry["tags"]) or "-"
        candidates = ", ".join(str(value) for value in entry["candidate_values"]) or "-"
        lines.append(
            f"| {index} | `{entry['risk']}` | `{entry['registry_path']}\\{entry['value_name']}` | "
            f"`{entry['requested_data_raw']}` | `{candidates}` | {tags} |"
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Parse pasted reg add commands into a one-value-at-a-time experiment plan.")
    parser.add_argument("--input", default="-", help="Input text file, or '-' for stdin.")
    parser.add_argument("--json-output", default="")
    parser.add_argument("--markdown-output", default="")
    args = parser.parse_args()

    raw = sys.stdin.read() if args.input == "-" else Path(args.input).read_text(encoding="utf-8")
    payload = parse_batch(raw)

    if args.json_output:
        Path(args.json_output).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json_output).write_text(json.dumps(payload, indent=2), encoding="utf-8")
    if args.markdown_output:
        Path(args.markdown_output).parent.mkdir(parents=True, exist_ok=True)
        write_markdown(payload, Path(args.markdown_output))

    print(json.dumps(payload, indent=2))
    return 0 if payload["status"] == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
