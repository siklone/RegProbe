#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
SEED_RECEIPT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt.json"
ROTATION_LEDGER_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-rotation-ledger.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-ack-journal.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-ack-journal.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path | None) -> str | None:
    if path is None:
        return None
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def build_seed_ack_journal(
    seed_receipt: dict[str, Any],
    rotation_ledger: dict[str, Any],
    *,
    seed_receipt_path: Path | None = None,
    rotation_ledger_path: Path | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    receipt_status = str(seed_receipt.get("receipt_status") or "unknown")
    rotation_status = str(rotation_ledger.get("rotation_status") or "unknown")
    rotation_mode = str(rotation_ledger.get("rotation_mode") or "unknown")
    verification = seed_receipt.get("verification") or {}
    seed_commands = seed_receipt.get("seed_commands") or {}

    if receipt_status == "pending":
        ack_status = "awaiting-application"
        ack_mode = "apply-seed"
        operator_blocker = "seed-not-yet-applied"
        next_action = "Run the seed commands, then regenerate the seed receipt and rotation ledger."
    elif receipt_status == "seeded-retained-baseline":
        ack_status = "awaiting-refresh"
        ack_mode = "refresh-after-seed"
        operator_blocker = "refresh-transition-and-ledger"
        next_action = "Regenerate the transition summary and rotation ledger so the lane can leave seed-pending."
    elif receipt_status == "current-matches-previous" and rotation_status in {"seed-complete", "steady"}:
        ack_status = "complete"
        ack_mode = "steady"
        operator_blocker = "await-new-current-snapshot"
        next_action = "Seed alignment is complete; wait for a new current reopen snapshot."
    else:
        ack_status = "manual-review"
        ack_mode = "review-custom-previous"
        operator_blocker = "manual-previous-needs-review"
        next_action = "Review the retained previous snapshot before recording seed completion."

    journal_entries = [
        {
            "candidate_id": entry.get("candidate_id"),
            "transition_type": entry.get("transition_type"),
            "rotation_disposition": entry.get("rotation_disposition"),
            "current_journal_state": entry.get("current_journal_state"),
            "next_unlock_prerequisite": entry.get("next_unlock_prerequisite"),
            "ack_required": ack_status != "complete",
        }
        for entry in (rotation_ledger.get("entries") or [])
    ]

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_seed_receipt_path": portable_path(seed_receipt_path),
        "source_rotation_ledger_path": portable_path(rotation_ledger_path),
        "ack_status": ack_status,
        "ack_mode": ack_mode,
        "receipt_status": receipt_status,
        "rotation_status": rotation_status,
        "rotation_mode": rotation_mode,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "commands": {
            "seed_previous_snapshot_command": seed_commands.get("seed_previous_snapshot_command"),
            "seed_previous_snapshot_markdown_command": seed_commands.get("seed_previous_snapshot_markdown_command"),
            "refresh_transition_summary_command": seed_commands.get("refresh_transition_summary_command"),
            "regenerate_seed_receipt_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_seed_receipt.py",
            "regenerate_rotation_ledger_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_rotation_ledger.py",
        },
        "verification": {
            "previous_snapshot_present": verification.get("previous_snapshot_present"),
            "previous_matches_current_snapshot": verification.get("previous_matches_current_snapshot"),
            "previous_matches_retained_baseline": verification.get("previous_matches_retained_baseline"),
            "rotation_prerequisites_pending": int((rotation_ledger.get("counts") or {}).get("prerequisite_count") or 0) > 0,
        },
        "focus": {
            "current_snapshot_id": (seed_receipt.get("focus") or {}).get("current_snapshot_id"),
            "previous_snapshot_id": (seed_receipt.get("focus") or {}).get("previous_snapshot_id"),
            "retained_baseline_snapshot_id": (seed_receipt.get("focus") or {}).get("retained_baseline_snapshot_id"),
            "top_rotation_candidate": (rotation_ledger.get("focus") or {}).get("top_rotation_candidate"),
        },
        "counts": {
            "candidate_count": int((seed_receipt.get("counts") or {}).get("candidate_count") or 0),
            "ack_required_candidate_count": sum(1 for entry in journal_entries if entry.get("ack_required")),
            "rotation_candidate_count": len(journal_entries),
        },
        "entries": journal_entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    operator = payload.get("operator") or {}
    commands = payload.get("commands") or {}
    verification = payload.get("verification") or {}
    focus = payload.get("focus") or {}
    lines = [
        "# ETW Stackwalk Reopen Seed Ack Journal",
        "",
        f"- Ack status: `{payload.get('ack_status')}`",
        f"- Ack mode: `{payload.get('ack_mode')}`",
        f"- Receipt status: `{payload.get('receipt_status')}`",
        f"- Rotation status: `{payload.get('rotation_status')}`",
        f"- Rotation mode: `{payload.get('rotation_mode')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Top rotation candidate: `{focus.get('top_rotation_candidate')}`",
        "",
        "## Verification",
        "",
        f"- Previous snapshot present: `{verification.get('previous_snapshot_present')}`",
        f"- Previous matches current snapshot: `{verification.get('previous_matches_current_snapshot')}`",
        f"- Previous matches retained baseline: `{verification.get('previous_matches_retained_baseline')}`",
        f"- Rotation prerequisites pending: `{verification.get('rotation_prerequisites_pending')}`",
        "",
        "## Commands",
        "",
        f"- `{commands.get('seed_previous_snapshot_command')}`",
        f"- `{commands.get('seed_previous_snapshot_markdown_command')}`",
        f"- `{commands.get('refresh_transition_summary_command')}`",
        f"- `{commands.get('regenerate_seed_receipt_command')}`",
        f"- `{commands.get('regenerate_rotation_ledger_command')}`",
        "",
    ]
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an ETW reopen seed acknowledgement journal.")
    parser.add_argument("--seed-receipt", type=Path, default=SEED_RECEIPT_PATH)
    parser.add_argument("--rotation-ledger", type=Path, default=ROTATION_LEDGER_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_seed_ack_journal(
        load_json(args.seed_receipt),
        load_json(args.rotation_ledger),
        seed_receipt_path=args.seed_receipt,
        rotation_ledger_path=args.rotation_ledger,
    )
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "ack_status": payload.get("ack_status"),
                "ack_mode": payload.get("ack_mode"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
