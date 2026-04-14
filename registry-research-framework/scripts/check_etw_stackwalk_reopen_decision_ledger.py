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
DEFAULT_PACK_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-pack.json"
DEFAULT_LEDGER_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-decision-ledger.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-decision-ledger-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-decision-ledger-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_reopen_decision_ledger import build_reopen_decision_ledger  # noqa: E402
from generate_etw_stackwalk_reopen_decision_ledger import load_json  # noqa: E402
from generate_etw_stackwalk_reopen_decision_ledger import portable_path  # noqa: E402


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


def compare_reopen_decision_ledger(
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
        "source_hold_reopen_pack_path",
        "pack_status",
        "ledger_status",
        "reopen_candidate_count",
        "deferred_candidate_count",
        "review_ready_candidate_count",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")
    if (surface.get("operator") or {}).get("next_action") != (expected.get("operator") or {}).get("next_action"):
        errors.append("operator.next_action mismatch.")
    if (surface.get("operator") or {}).get("intentional_reopen_required") != (expected.get("operator") or {}).get("intentional_reopen_required"):
        errors.append("operator.intentional_reopen_required mismatch.")

    surface_entries = entry_map(surface)
    expected_entries = entry_map(expected)
    if sorted(surface_entries) != sorted(expected_entries):
        errors.append("candidate set does not match the current hold reopen pack.")
    for candidate_id, expected_entry in expected_entries.items():
        entry = surface_entries.get(candidate_id)
        if not entry:
            continue
        for key in (
            "feature_area",
            "next_missing_layer",
            "promotion_blockers",
            "blocker_count",
            "reopen_prerequisites",
            "prerequisite_count",
            "prerequisite_status",
            "decision_state",
            "decision_reason_codes",
            "decision_summary",
            "next_review_trigger",
            "include_holds_plan_command",
            "include_holds_run_command",
            "effective_config_command",
            "run_id",
            "host_etl_repo_path",
            "next_action_hint",
        ):
            if entry.get(key) != expected_entry.get(key):
                errors.append(f"{candidate_id}: {key} mismatch.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "ledger_status": surface.get("ledger_status"),
        "reopen_candidate_count": surface.get("reopen_candidate_count"),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Reopen Decision Ledger Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Ledger status: `{payload.get('ledger_status')}`",
        f"- Reopen candidates: `{payload.get('reopen_candidate_count')}`",
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
    parser = argparse.ArgumentParser(description="Validate the ETW stackwalk reopen decision ledger surface.")
    parser.add_argument("--pack", type=Path, default=DEFAULT_PACK_PATH)
    parser.add_argument("--ledger", type=Path, default=DEFAULT_LEDGER_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    pack_payload = load_json(args.pack)
    surface = load_json(args.ledger)
    expected = build_reopen_decision_ledger(
        pack_payload,
        generated_utc=str(surface.get("generated_utc") or now_utc()),
    )
    payload = compare_reopen_decision_ledger(surface, expected)
    payload["pack_path"] = portable_path(args.pack)
    payload["ledger_path"] = portable_path(args.ledger)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
