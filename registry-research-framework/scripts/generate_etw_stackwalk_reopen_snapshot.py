#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
JOURNAL_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-journal.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def snapshot_entry(journal_entry: dict[str, Any]) -> dict[str, Any]:
    return {
        "candidate_id": journal_entry.get("candidate_id"),
        "feature_area": journal_entry.get("feature_area"),
        "journal_state": journal_entry.get("journal_state"),
        "operator_blocker": journal_entry.get("operator_blocker"),
        "recommended_disposition": journal_entry.get("recommended_disposition"),
        "remaining_to_ready_count": journal_entry.get("remaining_to_ready_count"),
        "next_unlock_prerequisite": journal_entry.get("next_unlock_prerequisite"),
        "next_action": journal_entry.get("next_action"),
        "run_id": journal_entry.get("run_id"),
        "host_etl_repo_path": journal_entry.get("host_etl_repo_path"),
    }


def focus_summary(entries: list[dict[str, Any]]) -> dict[str, Any]:
    deferred = [entry for entry in entries if entry.get("journal_state") == "deferred"]
    review_pending = [entry for entry in entries if entry.get("journal_state") == "review-pending"]
    return {
        "top_deferred_candidate": deferred[0]["candidate_id"] if deferred else None,
        "top_review_pending_candidate": review_pending[0]["candidate_id"] if review_pending else None,
        "top_next_unlock_prerequisite": next(
            (
                entry.get("next_unlock_prerequisite")
                for entry in deferred
                if entry.get("next_unlock_prerequisite")
            ),
            None,
        ),
    }


def history_markers(entries: list[dict[str, Any]]) -> dict[str, list[str]]:
    return {
        "state_signature": [
            f"{entry.get('candidate_id')}:{entry.get('journal_state')}:{entry.get('remaining_to_ready_count')}"
            for entry in entries
        ],
        "blocker_signature": [
            f"{entry.get('candidate_id')}:{entry.get('operator_blocker')}"
            for entry in entries
        ],
        "run_id_signature": [
            str(entry.get("run_id") or "")
            for entry in entries
        ],
    }


def snapshot_id(entries: list[dict[str, Any]], journal_status: str | None) -> str:
    payload = {
        "journal_status": journal_status,
        "entries": entries,
    }
    digest = hashlib.sha1(json.dumps(payload, sort_keys=True).encode("utf-8")).hexdigest()
    return digest[:12]


def build_reopen_snapshot(
    journal_payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    entries = [
        snapshot_entry(entry)
        for entry in sorted(
            list(journal_payload.get("entries") or []),
            key=lambda item: (
                str(item.get("journal_state") or ""),
                -int(item.get("remaining_to_ready_count") or 0),
                str(item.get("candidate_id") or ""),
            ),
        )
    ]
    counts = {
        "candidate_count": len(entries),
        "deferred_count": sum(1 for entry in entries if entry.get("journal_state") == "deferred"),
        "review_pending_count": sum(1 for entry in entries if entry.get("journal_state") == "review-pending"),
        "ack_required_count": len(entries),
    }
    status = str(journal_payload.get("journal_status") or "idle")
    operator = journal_payload.get("operator") or {}
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_reopen_journal_path": portable_path(JOURNAL_PATH),
        "snapshot_scope": "current-reopen-state",
        "snapshot_status": status,
        "snapshot_id": snapshot_id(entries, status),
        "operator": {
            "blocker": operator.get("blocker"),
            "next_action": operator.get("next_action"),
        },
        "counts": counts,
        "focus": focus_summary(entries),
        "history_markers": history_markers(entries),
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    focus = payload.get("focus") or {}
    lines = [
        "# ETW Stackwalk Reopen Snapshot",
        "",
        f"- Snapshot status: `{payload.get('snapshot_status')}`",
        f"- Snapshot id: `{payload.get('snapshot_id')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        f"- Deferred count: `{counts.get('deferred_count')}`",
        f"- Review-pending count: `{counts.get('review_pending_count')}`",
        f"- Top deferred candidate: `{focus.get('top_deferred_candidate')}`",
        f"- Top review-pending candidate: `{focus.get('top_review_pending_candidate')}`",
        f"- Top next unlock prerequisite: `{focus.get('top_next_unlock_prerequisite')}`",
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
                f"- Journal state: `{entry.get('journal_state')}`",
                f"- Operator blocker: `{entry.get('operator_blocker')}`",
                f"- Remaining to ready: `{entry.get('remaining_to_ready_count')}`",
                f"- Next unlock prerequisite: `{entry.get('next_unlock_prerequisite')}`",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a deterministic ETW reopen snapshot from the reopen journal.")
    parser.add_argument("--journal", type=Path, default=JOURNAL_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_reopen_snapshot(load_json(args.journal))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "snapshot_status": payload.get("snapshot_status"),
                "snapshot_id": payload.get("snapshot_id"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
