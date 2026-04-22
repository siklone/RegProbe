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
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-transition-summary.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-transition-summary.md"


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


def entry_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(entry.get("candidate_id") or ""): entry
        for entry in payload.get("entries") or []
        if str(entry.get("candidate_id") or "").strip()
    }


def transition_entry(
    candidate_id: str,
    current_entry: dict[str, Any] | None,
    previous_entry: dict[str, Any] | None,
) -> dict[str, Any]:
    if current_entry and not previous_entry:
        transition_type = "added"
    elif previous_entry and not current_entry:
        transition_type = "removed"
    elif (current_entry or {}).get("journal_state") != (previous_entry or {}).get("journal_state"):
        transition_type = "state-changed"
    elif (current_entry or {}).get("remaining_to_ready_count") != (previous_entry or {}).get("remaining_to_ready_count"):
        transition_type = "progress-changed"
    elif (current_entry or {}).get("operator_blocker") != (previous_entry or {}).get("operator_blocker"):
        transition_type = "blocker-changed"
    else:
        transition_type = "unchanged"

    preferred = current_entry or previous_entry or {}
    return {
        "candidate_id": candidate_id,
        "feature_area": preferred.get("feature_area"),
        "transition_type": transition_type,
        "current_journal_state": (current_entry or {}).get("journal_state"),
        "previous_journal_state": (previous_entry or {}).get("journal_state"),
        "current_operator_blocker": (current_entry or {}).get("operator_blocker"),
        "previous_operator_blocker": (previous_entry or {}).get("operator_blocker"),
        "current_remaining_to_ready_count": (current_entry or {}).get("remaining_to_ready_count"),
        "previous_remaining_to_ready_count": (previous_entry or {}).get("remaining_to_ready_count"),
        "current_snapshot_id": (current_entry or {}).get("_snapshot_id"),
        "previous_snapshot_id": (previous_entry or {}).get("_snapshot_id"),
        "next_unlock_prerequisite": (current_entry or previous_entry or {}).get("next_unlock_prerequisite"),
    }


def build_reopen_transition_summary(
    current_snapshot: dict[str, Any],
    previous_snapshot: dict[str, Any] | None = None,
    *,
    current_snapshot_path: Path | None = None,
    previous_snapshot_path: Path | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    current_entries = entry_map(current_snapshot)
    previous_entries = entry_map(previous_snapshot or {})

    for entry in current_entries.values():
        entry["_snapshot_id"] = current_snapshot.get("snapshot_id")
    for entry in previous_entries.values():
        entry["_snapshot_id"] = (previous_snapshot or {}).get("snapshot_id")

    candidate_ids = sorted(set(current_entries) | set(previous_entries))
    transitions = [
        transition_entry(candidate_id, current_entries.get(candidate_id), previous_entries.get(candidate_id))
        for candidate_id in candidate_ids
    ]

    changed = [entry for entry in transitions if entry.get("transition_type") != "unchanged"]
    added = [entry for entry in transitions if entry.get("transition_type") == "added"]
    removed = [entry for entry in transitions if entry.get("transition_type") == "removed"]

    if previous_snapshot is None:
        transition_status = "baseline"
        operator_blocker = "no-previous-snapshot"
        next_action = "Treat the current reopen snapshot as the baseline until a previous snapshot is retained."
    elif current_snapshot.get("snapshot_id") == previous_snapshot.get("snapshot_id"):
        transition_status = "unchanged"
        operator_blocker = "no-transition-detected"
        next_action = "No reopen transition was detected; keep following the current snapshot guidance."
    elif changed:
        transition_status = "changed"
        operator_blocker = "review-transition-delta"
        next_action = "Review the changed candidates before updating reopen operator guidance."
    else:
        transition_status = "unchanged"
        operator_blocker = "no-transition-detected"
        next_action = "No reopen transition was detected; keep following the current snapshot guidance."

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_current_snapshot_path": portable_path(current_snapshot_path),
        "source_previous_snapshot_path": portable_path(previous_snapshot_path) if previous_snapshot is not None else None,
        "transition_status": transition_status,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "counts": {
            "current_candidate_count": len(current_entries),
            "previous_candidate_count": len(previous_entries),
            "changed_candidate_count": len(changed),
            "added_candidate_count": len(added),
            "removed_candidate_count": len(removed),
        },
        "focus": {
            "current_snapshot_id": current_snapshot.get("snapshot_id"),
            "previous_snapshot_id": (previous_snapshot or {}).get("snapshot_id"),
            "current_snapshot_status": current_snapshot.get("snapshot_status"),
            "previous_snapshot_status": (previous_snapshot or {}).get("snapshot_status"),
            "top_changed_candidate": changed[0]["candidate_id"] if changed else None,
        },
        "entries": transitions,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    focus = payload.get("focus") or {}
    lines = [
        "# ETW Stackwalk Reopen Transition Summary",
        "",
        f"- Transition status: `{payload.get('transition_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Current candidate count: `{counts.get('current_candidate_count')}`",
        f"- Previous candidate count: `{counts.get('previous_candidate_count')}`",
        f"- Changed candidate count: `{counts.get('changed_candidate_count')}`",
        f"- Added candidate count: `{counts.get('added_candidate_count')}`",
        f"- Removed candidate count: `{counts.get('removed_candidate_count')}`",
        f"- Current snapshot id: `{focus.get('current_snapshot_id')}`",
        f"- Previous snapshot id: `{focus.get('previous_snapshot_id')}`",
        f"- Top changed candidate: `{focus.get('top_changed_candidate')}`",
        "",
        "## Entries",
        "",
    ]
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
                f"- Current journal state: `{entry.get('current_journal_state')}`",
                f"- Previous journal state: `{entry.get('previous_journal_state')}`",
                f"- Current blocker: `{entry.get('current_operator_blocker')}`",
                f"- Previous blocker: `{entry.get('previous_operator_blocker')}`",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an ETW reopen transition summary from current and previous snapshots.")
    parser.add_argument("--current", type=Path, default=CURRENT_SNAPSHOT_PATH)
    parser.add_argument("--previous", type=Path, default=PREVIOUS_SNAPSHOT_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    current_snapshot = load_json(args.current)
    previous_snapshot = load_json_if_exists(args.previous)
    payload = build_reopen_transition_summary(
        current_snapshot,
        previous_snapshot,
        current_snapshot_path=args.current,
        previous_snapshot_path=args.previous,
    )
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "transition_status": payload.get("transition_status"),
                "changed_candidate_count": (payload.get("counts") or {}).get("changed_candidate_count"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
