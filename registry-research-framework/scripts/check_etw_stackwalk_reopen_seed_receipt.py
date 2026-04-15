#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
CURRENT_DIR = Path(__file__).resolve().parent
DEFAULT_CURRENT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.json"
DEFAULT_PREVIOUS_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.previous.json"
DEFAULT_HISTORY_ARCHIVE_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive.json"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_reopen_seed_receipt import build_seed_receipt  # noqa: E402
from generate_etw_stackwalk_reopen_seed_receipt import load_json  # noqa: E402
from generate_etw_stackwalk_reopen_seed_receipt import load_json_if_exists  # noqa: E402
from generate_etw_stackwalk_reopen_seed_receipt import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def compare_seed_receipt(
    surface: dict[str, Any],
    expected: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []
    if surface.get("schema_version") != "1.0":
        errors.append("schema_version must be 1.0.")
    for key in (
        "source_current_snapshot_path",
        "source_previous_snapshot_path",
        "source_history_archive_summary_path",
        "receipt_status",
        "receipt_mode",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")
    for key in ("blocker", "next_action"):
        if (surface.get("operator") or {}).get(key) != (expected.get("operator") or {}).get(key):
            errors.append(f"operator.{key} mismatch.")
    for key in ("seed_previous_snapshot_command", "seed_previous_snapshot_markdown_command", "refresh_transition_summary_command"):
        if (surface.get("seed_commands") or {}).get(key) != (expected.get("seed_commands") or {}).get(key):
            errors.append(f"seed_commands.{key} mismatch.")
    for key in (
        "previous_snapshot_present",
        "previous_matches_current_snapshot",
        "previous_matches_retained_baseline",
    ):
        if (surface.get("verification") or {}).get(key) != (expected.get("verification") or {}).get(key):
            errors.append(f"verification.{key} mismatch.")
    for key in ("current_snapshot_id", "previous_snapshot_id", "retained_baseline_snapshot_id"):
        if (surface.get("focus") or {}).get(key) != (expected.get("focus") or {}).get(key):
            errors.append(f"focus.{key} mismatch.")
    for key in ("candidate_count", "verification_true_count"):
        if (surface.get("counts") or {}).get(key) != (expected.get("counts") or {}).get(key):
            errors.append(f"counts.{key} mismatch.")
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "receipt_status": surface.get("receipt_status"),
        "receipt_mode": surface.get("receipt_mode"),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Reopen Seed Receipt Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Receipt status: `{payload.get('receipt_status')}`",
        f"- Receipt mode: `{payload.get('receipt_mode')}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    else:
        for error in errors:
            lines.append(f"- {error}")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the ETW reopen seed receipt.")
    parser.add_argument("--current", type=Path, default=DEFAULT_CURRENT_PATH)
    parser.add_argument("--previous", type=Path, default=DEFAULT_PREVIOUS_PATH)
    parser.add_argument("--history-archive-summary", type=Path, default=DEFAULT_HISTORY_ARCHIVE_SUMMARY_PATH)
    parser.add_argument("--summary", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    expected = build_seed_receipt(
        load_json(args.current),
        load_json_if_exists(args.previous),
        load_json(args.history_archive_summary),
        current_snapshot_path=args.current,
        previous_snapshot_path=args.previous,
        history_archive_summary_path=args.history_archive_summary,
    )
    payload = compare_seed_receipt(load_json(args.summary), expected)
    payload["current_path"] = portable_path(args.current)
    payload["previous_path"] = portable_path(args.previous)
    payload["summary_path"] = portable_path(args.summary)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
