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
TRANSITION_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-transition-summary.json"
HISTORY_ARCHIVE_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive.json"
SEED_RECEIPT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-seed-receipt.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-rotation-ledger.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-rotation-ledger.md"


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


def build_rotation_ledger(
    current_snapshot: dict[str, Any],
    previous_snapshot: dict[str, Any] | None,
    transition_summary: dict[str, Any],
    history_archive_summary: dict[str, Any],
    seed_receipt: dict[str, Any] | None = None,
    *,
    current_snapshot_path: Path | None = None,
    previous_snapshot_path: Path | None = None,
    transition_summary_path: Path | None = None,
    history_archive_summary_path: Path | None = None,
    seed_receipt_path: Path | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    current_snapshot_id = str(current_snapshot.get("snapshot_id") or "")
    previous_snapshot_id = str((previous_snapshot or {}).get("snapshot_id") or "") or None
    transition_status = str(transition_summary.get("transition_status") or "unknown")
    history_status = str(history_archive_summary.get("history_status") or "unknown")
    seed_receipt_status = str((seed_receipt or {}).get("receipt_status") or "") or None
    changed_entries = [
        entry for entry in (transition_summary.get("entries") or [])
        if str(entry.get("transition_type") or "unchanged") != "unchanged"
    ]

    if previous_snapshot is None:
        rotation_status = "seed-pending"
        rotation_mode = "seed-from-baseline"
        operator_blocker = "seed-previous-snapshot-from-history-archive"
        next_action = "Seed snapshot.previous from the retained baseline snapshot before expecting rotation-aware reopen diffs."
        entry_disposition = "seed-baseline"
        prerequisite_codes = ["seed-previous-snapshot", "refresh-transition-summary"]
    elif previous_snapshot_id == current_snapshot_id:
        if seed_receipt_status in {"seeded-retained-baseline", "current-matches-previous"}:
            rotation_status = "seed-complete"
            rotation_mode = "receipt-confirmed-steady"
            operator_blocker = "await-new-reopen-snapshot"
            next_action = "Seed receipt confirms snapshot.previous is aligned; wait for a new current reopen snapshot before expecting rotation-aware diffs."
        else:
            rotation_status = "steady"
            rotation_mode = "no-rotation"
            operator_blocker = "await-new-reopen-snapshot"
            next_action = "Current and previous snapshot ids already match; wait for a new current reopen snapshot before rotating history."
        entry_disposition = "steady"
        prerequisite_codes = []
    else:
        rotation_status = "rotation-pending"
        rotation_mode = "advance-previous-snapshot"
        operator_blocker = "review-and-rotate-previous-snapshot"
        next_action = "Review the changed candidates, persist the current snapshot into history storage, then rotate snapshot.previous and refresh the transition summary."
        entry_disposition = "review-rotate"
        prerequisite_codes = [
            "review-transition-delta",
            "persist-current-snapshot-history",
            "rotate-previous-snapshot",
            "refresh-transition-summary",
        ]

    rotate_previous_snapshot_command = (
        "cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json "
        "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json"
    )
    rotate_previous_snapshot_markdown_command = (
        "cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.md "
        "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md"
    )
    entries = [
        {
            "candidate_id": entry.get("candidate_id"),
            "feature_area": entry.get("feature_area"),
            "transition_type": entry.get("transition_type"),
            "current_journal_state": entry.get("current_journal_state"),
            "previous_journal_state": entry.get("previous_journal_state"),
            "current_operator_blocker": entry.get("current_operator_blocker"),
            "previous_operator_blocker": entry.get("previous_operator_blocker"),
            "next_unlock_prerequisite": entry.get("next_unlock_prerequisite"),
            "requires_rotation_review": True,
            "rotation_disposition": entry_disposition,
        }
        for entry in changed_entries
    ]

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_current_snapshot_path": portable_path(current_snapshot_path),
        "source_previous_snapshot_path": portable_path(previous_snapshot_path) if previous_snapshot is not None else None,
        "source_transition_summary_path": portable_path(transition_summary_path),
        "source_history_archive_summary_path": portable_path(history_archive_summary_path),
        "source_history_archive_markdown_path": portable_path(
            history_archive_summary_path.with_suffix(".md") if history_archive_summary_path is not None else None
        ),
        "source_seed_receipt_path": portable_path(seed_receipt_path) if seed_receipt is not None else None,
        "rotation_status": rotation_status,
        "rotation_mode": rotation_mode,
        "history_status": history_status,
        "transition_status": transition_status,
        "seed_receipt_status": seed_receipt_status,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "seed_previous_snapshot_command": history_archive_summary.get("seed_previous_snapshot_command"),
        "seed_previous_snapshot_markdown_command": history_archive_summary.get("seed_previous_snapshot_markdown_command"),
        "persist_current_snapshot_history_command": history_archive_summary.get("persist_current_snapshot_history_command"),
        "rotate_previous_snapshot_command": rotate_previous_snapshot_command,
        "rotate_previous_snapshot_markdown_command": rotate_previous_snapshot_markdown_command,
        "refresh_transition_summary_command": history_archive_summary.get("refresh_transition_summary_command"),
        "prerequisite_codes": prerequisite_codes,
        "counts": {
            "current_candidate_count": int((current_snapshot.get("counts") or {}).get("candidate_count") or 0),
            "previous_candidate_count": int((previous_snapshot or {}).get("counts", {}).get("candidate_count") or 0),
            "changed_candidate_count": int((transition_summary.get("counts") or {}).get("changed_candidate_count") or 0),
            "rotation_candidate_count": len(entries),
            "prerequisite_count": len(prerequisite_codes),
        },
        "focus": {
            "current_snapshot_id": current_snapshot_id,
            "previous_snapshot_id": previous_snapshot_id,
            "retained_baseline_snapshot_id": history_archive_summary.get("retained_baseline_snapshot_id"),
            "top_changed_candidate": (transition_summary.get("focus") or {}).get("top_changed_candidate"),
            "top_rotation_candidate": entries[0]["candidate_id"] if entries else None,
        },
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    focus = payload.get("focus") or {}
    lines = [
        "# ETW Stackwalk Reopen Rotation Ledger",
        "",
        f"- Rotation status: `{payload.get('rotation_status')}`",
        f"- Rotation mode: `{payload.get('rotation_mode')}`",
        f"- History status: `{payload.get('history_status')}`",
        f"- Transition status: `{payload.get('transition_status')}`",
        f"- Seed receipt status: `{payload.get('seed_receipt_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Current snapshot id: `{focus.get('current_snapshot_id')}`",
        f"- Previous snapshot id: `{focus.get('previous_snapshot_id')}`",
        f"- Retained baseline snapshot id: `{focus.get('retained_baseline_snapshot_id')}`",
        f"- Rotation candidate count: `{counts.get('rotation_candidate_count')}`",
        f"- Prerequisite count: `{counts.get('prerequisite_count')}`",
        "",
        "## Prerequisites",
        "",
    ]
    prerequisites = payload.get("prerequisite_codes") or []
    if not prerequisites:
        lines.append("- none")
    else:
        for code in prerequisites:
            lines.append(f"- `{code}`")
    lines.extend(["", "## Entries", ""])
    entries = payload.get("entries") or []
    if not entries:
        lines.append("- none")
        return "\n".join(lines).rstrip() + "\n"
    for entry in entries:
        lines.extend(
            [
                f"### {entry.get('candidate_id')}",
                "",
                f"- Transition type: `{entry.get('transition_type')}`",
                f"- Rotation disposition: `{entry.get('rotation_disposition')}`",
                f"- Current journal state: `{entry.get('current_journal_state')}`",
                f"- Previous journal state: `{entry.get('previous_journal_state')}`",
                f"- Next unlock prerequisite: `{entry.get('next_unlock_prerequisite')}`",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an ETW reopen rotation ledger from current, previous, transition, and history surfaces.")
    parser.add_argument("--current", type=Path, default=CURRENT_SNAPSHOT_PATH)
    parser.add_argument("--previous", type=Path, default=PREVIOUS_SNAPSHOT_PATH)
    parser.add_argument("--transition", type=Path, default=TRANSITION_SUMMARY_PATH)
    parser.add_argument("--history-archive-summary", type=Path, default=HISTORY_ARCHIVE_SUMMARY_PATH)
    parser.add_argument("--seed-receipt", type=Path, default=SEED_RECEIPT_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_rotation_ledger(
        load_json(args.current),
        load_json_if_exists(args.previous),
        load_json(args.transition),
        load_json(args.history_archive_summary),
        load_json_if_exists(args.seed_receipt),
        current_snapshot_path=args.current,
        previous_snapshot_path=args.previous,
        transition_summary_path=args.transition,
        history_archive_summary_path=args.history_archive_summary,
        seed_receipt_path=args.seed_receipt,
    )
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "rotation_status": payload.get("rotation_status"),
                "rotation_candidate_count": (payload.get("counts") or {}).get("rotation_candidate_count"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
