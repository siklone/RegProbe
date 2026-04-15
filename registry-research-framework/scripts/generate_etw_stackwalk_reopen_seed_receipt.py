#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
CURRENT_SNAPSHOT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.json"
PREVIOUS_SNAPSHOT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.previous.json"
HISTORY_ARCHIVE_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt.md"


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
    return json.loads(path.read_text(encoding="utf-8"))


def load_json_if_exists(path: Path | None) -> dict[str, Any] | None:
    if path is None or not path.exists():
        return None
    return load_json(path)


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def build_seed_receipt(
    current_snapshot: dict[str, Any],
    previous_snapshot: dict[str, Any] | None,
    history_archive_summary: dict[str, Any],
    *,
    current_snapshot_path: Path | None = None,
    previous_snapshot_path: Path | None = None,
    history_archive_summary_path: Path | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    current_snapshot_id = str(current_snapshot.get("snapshot_id") or "")
    previous_snapshot_id = str((previous_snapshot or {}).get("snapshot_id") or "") or None
    retained_baseline_snapshot_id = str(history_archive_summary.get("retained_baseline_snapshot_id") or "") or None

    if previous_snapshot is None:
        receipt_status = "pending"
        receipt_mode = "await-seed"
        operator_blocker = "seed-not-applied"
        next_action = "Apply the retained baseline seed commands, then refresh the transition summary and rotation ledger."
    elif previous_snapshot_id == retained_baseline_snapshot_id and previous_snapshot_id == current_snapshot_id:
        receipt_status = "seeded-retained-baseline"
        receipt_mode = "baseline-seed-confirmed"
        operator_blocker = "refresh-transition-after-seed"
        next_action = "Seed receipt is confirmed; refresh the transition summary and rotation ledger so the lane leaves seed-pending."
    elif previous_snapshot_id == current_snapshot_id:
        receipt_status = "current-matches-previous"
        receipt_mode = "steady"
        operator_blocker = "await-new-current-snapshot"
        next_action = "Previous snapshot already matches current; wait for a new current reopen snapshot before expecting a diff."
    else:
        receipt_status = "custom-previous-present"
        receipt_mode = "manual-previous"
        operator_blocker = "review-manual-previous-snapshot"
        next_action = "Review the retained previous snapshot before trusting rotation-driven diffs."

    seed_commands = {
        "seed_previous_snapshot_command": history_archive_summary.get("seed_previous_snapshot_command"),
        "seed_previous_snapshot_markdown_command": history_archive_summary.get("seed_previous_snapshot_markdown_command"),
        "refresh_transition_summary_command": history_archive_summary.get("refresh_transition_summary_command"),
    }
    verification = {
        "previous_snapshot_present": previous_snapshot is not None,
        "previous_matches_current_snapshot": previous_snapshot_id == current_snapshot_id if previous_snapshot_id else False,
        "previous_matches_retained_baseline": previous_snapshot_id == retained_baseline_snapshot_id if previous_snapshot_id else False,
    }

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_current_snapshot_path": portable_path(current_snapshot_path),
        "source_previous_snapshot_path": portable_path(previous_snapshot_path),
        "source_history_archive_summary_path": portable_path(history_archive_summary_path),
        "receipt_status": receipt_status,
        "receipt_mode": receipt_mode,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "seed_commands": seed_commands,
        "verification": verification,
        "focus": {
            "current_snapshot_id": current_snapshot_id,
            "previous_snapshot_id": previous_snapshot_id,
            "retained_baseline_snapshot_id": retained_baseline_snapshot_id,
        },
        "counts": {
            "candidate_count": int((current_snapshot.get("counts") or {}).get("candidate_count") or 0),
            "verification_true_count": sum(1 for value in verification.values() if value is True),
        },
    }


def render_markdown(payload: dict[str, Any]) -> str:
    operator = payload.get("operator") or {}
    focus = payload.get("focus") or {}
    verification = payload.get("verification") or {}
    commands = payload.get("seed_commands") or {}
    lines = [
        "# ETW Stackwalk Reopen Seed Receipt",
        "",
        f"- Receipt status: `{payload.get('receipt_status')}`",
        f"- Receipt mode: `{payload.get('receipt_mode')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Current snapshot id: `{focus.get('current_snapshot_id')}`",
        f"- Previous snapshot id: `{focus.get('previous_snapshot_id')}`",
        f"- Retained baseline snapshot id: `{focus.get('retained_baseline_snapshot_id')}`",
        "",
        "## Verification",
        "",
        f"- Previous snapshot present: `{verification.get('previous_snapshot_present')}`",
        f"- Previous matches current snapshot: `{verification.get('previous_matches_current_snapshot')}`",
        f"- Previous matches retained baseline: `{verification.get('previous_matches_retained_baseline')}`",
        "",
        "## Commands",
        "",
        f"- `{commands.get('seed_previous_snapshot_command')}`",
        f"- `{commands.get('seed_previous_snapshot_markdown_command')}`",
        f"- `{commands.get('refresh_transition_summary_command')}`",
        "",
    ]
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a seed receipt for ETW reopen snapshot.previous seeding.")
    parser.add_argument("--current", type=Path, default=CURRENT_SNAPSHOT_PATH)
    parser.add_argument("--previous", type=Path, default=PREVIOUS_SNAPSHOT_PATH)
    parser.add_argument("--history-archive-summary", type=Path, default=HISTORY_ARCHIVE_SUMMARY_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_seed_receipt(
        load_json(args.current),
        load_json_if_exists(args.previous),
        load_json(args.history_archive_summary),
        current_snapshot_path=args.current,
        previous_snapshot_path=args.previous,
        history_archive_summary_path=args.history_archive_summary,
    )
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "receipt_status": payload.get("receipt_status"),
                "receipt_mode": payload.get("receipt_mode"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
