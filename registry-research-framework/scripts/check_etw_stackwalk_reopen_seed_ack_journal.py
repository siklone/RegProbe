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
DEFAULT_SEED_RECEIPT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt.json"
DEFAULT_ROTATION_LEDGER_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-rotation-ledger.json"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-ack-journal.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-ack-journal-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-ack-journal-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_reopen_seed_ack_journal import build_seed_ack_journal  # noqa: E402
from generate_etw_stackwalk_reopen_seed_ack_journal import load_json  # noqa: E402
from generate_etw_stackwalk_reopen_seed_ack_journal import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def compare_seed_ack_journal(
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
        "source_seed_receipt_path",
        "source_rotation_ledger_path",
        "ack_status",
        "ack_mode",
        "receipt_status",
        "rotation_status",
        "rotation_mode",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")
    for key in ("blocker", "next_action"):
        if (surface.get("operator") or {}).get(key) != (expected.get("operator") or {}).get(key):
            errors.append(f"operator.{key} mismatch.")
    for key in (
        "seed_previous_snapshot_command",
        "seed_previous_snapshot_markdown_command",
        "refresh_transition_summary_command",
        "regenerate_seed_receipt_command",
        "regenerate_rotation_ledger_command",
    ):
        if (surface.get("commands") or {}).get(key) != (expected.get("commands") or {}).get(key):
            errors.append(f"commands.{key} mismatch.")
    for key in (
        "previous_snapshot_present",
        "previous_matches_current_snapshot",
        "previous_matches_retained_baseline",
        "rotation_prerequisites_pending",
    ):
        if (surface.get("verification") or {}).get(key) != (expected.get("verification") or {}).get(key):
            errors.append(f"verification.{key} mismatch.")
    for key in ("current_snapshot_id", "previous_snapshot_id", "retained_baseline_snapshot_id", "top_rotation_candidate"):
        if (surface.get("focus") or {}).get(key) != (expected.get("focus") or {}).get(key):
            errors.append(f"focus.{key} mismatch.")
    for key in ("candidate_count", "ack_required_candidate_count", "rotation_candidate_count"):
        if (surface.get("counts") or {}).get(key) != (expected.get("counts") or {}).get(key):
            errors.append(f"counts.{key} mismatch.")
    surface_entries = surface.get("entries") or []
    expected_entries = expected.get("entries") or []
    if len(surface_entries) != len(expected_entries):
        errors.append(f"entries length mismatch: expected {len(expected_entries)}, saw {len(surface_entries)}.")
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "ack_status": surface.get("ack_status"),
        "ack_mode": surface.get("ack_mode"),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Reopen Seed Ack Journal Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Ack status: `{payload.get('ack_status')}`",
        f"- Ack mode: `{payload.get('ack_mode')}`",
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
    parser = argparse.ArgumentParser(description="Validate the ETW reopen seed acknowledgement journal.")
    parser.add_argument("--seed-receipt", type=Path, default=DEFAULT_SEED_RECEIPT_PATH)
    parser.add_argument("--rotation-ledger", type=Path, default=DEFAULT_ROTATION_LEDGER_PATH)
    parser.add_argument("--summary", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    expected = build_seed_ack_journal(
        load_json(args.seed_receipt),
        load_json(args.rotation_ledger),
        seed_receipt_path=args.seed_receipt,
        rotation_ledger_path=args.rotation_ledger,
    )
    payload = compare_seed_ack_journal(load_json(args.summary), expected)
    payload["seed_receipt_path"] = portable_path(args.seed_receipt)
    payload["rotation_ledger_path"] = portable_path(args.rotation_ledger)
    payload["summary_path"] = portable_path(args.summary)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
