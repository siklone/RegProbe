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
DEFAULT_BRIEF_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-operator-brief.json"
DEFAULT_JOURNAL_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-journal.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-journal-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-journal-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_reopen_journal import build_reopen_journal  # noqa: E402
from generate_etw_stackwalk_reopen_journal import load_json  # noqa: E402
from generate_etw_stackwalk_reopen_journal import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def entry_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(entry.get("candidate_id") or ""): entry
        for entry in payload.get("entries") or []
        if str(entry.get("candidate_id") or "").strip()
    }


def compare_reopen_journal(
    surface: dict[str, Any],
    expected: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []
    if surface.get("schema_version") != "1.0":
        errors.append("schema_version must be 1.0.")
    for key in ("source_reopen_operator_brief_path", "brief_status", "journal_status"):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")
    for key in ("blocker", "next_action"):
        if (surface.get("operator") or {}).get(key) != (expected.get("operator") or {}).get(key):
            errors.append(f"operator.{key} mismatch.")
    for key in ("candidate_count", "deferred_count", "review_pending_count", "ack_required_count"):
        if (surface.get("counts") or {}).get(key) != (expected.get("counts") or {}).get(key):
            errors.append(f"counts.{key} mismatch.")

    surface_entries = entry_map(surface)
    expected_entries = entry_map(expected)
    if sorted(surface_entries) != sorted(expected_entries):
        errors.append("candidate set does not match the current reopen operator brief.")
    for candidate_id, expected_entry in expected_entries.items():
        entry = surface_entries.get(candidate_id)
        if not entry:
            continue
        for key in (
            "feature_area",
            "journal_state",
            "recommended_disposition",
            "operator_ack_required",
            "operator_blocker",
            "operator_posture",
            "remaining_to_ready_count",
            "outstanding_reason_codes",
            "next_unlock_prerequisite",
            "next_review_trigger",
            "next_action",
            "next_action_hint",
            "include_holds_plan_command",
            "include_holds_run_command",
            "run_id",
            "host_etl_repo_path",
        ):
            if entry.get(key) != expected_entry.get(key):
                errors.append(f"{candidate_id}: {key} mismatch.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "journal_status": surface.get("journal_status"),
        "candidate_count": (surface.get("counts") or {}).get("candidate_count"),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Reopen Journal Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Journal status: `{payload.get('journal_status')}`",
        f"- Candidate count: `{payload.get('candidate_count')}`",
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
    parser = argparse.ArgumentParser(description="Validate the ETW reopen journal surface.")
    parser.add_argument("--brief", type=Path, default=DEFAULT_BRIEF_PATH)
    parser.add_argument("--journal", type=Path, default=DEFAULT_JOURNAL_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    brief_payload = load_json(args.brief)
    surface = load_json(args.journal)
    expected = build_reopen_journal(
        brief_payload,
        generated_utc=str(surface.get("generated_utc") or now_utc()),
    )
    payload = compare_reopen_journal(surface, expected)
    payload["brief_path"] = portable_path(args.brief)
    payload["journal_path"] = portable_path(args.journal)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
