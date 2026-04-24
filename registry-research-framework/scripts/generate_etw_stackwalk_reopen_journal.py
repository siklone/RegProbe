#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
BRIEF_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-operator-brief.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-journal.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-journal.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
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


def journal_entry(brief_entry: dict[str, Any]) -> dict[str, Any]:
    blocked = str(brief_entry.get("brief_status") or "blocked") == "blocked"
    journal_state = "deferred" if blocked else "review-pending"
    disposition = "keep-closed" if blocked else "explicit-review"
    next_action = (
        "Do not run the include-holds commands yet."
        if blocked
        else "Review the candidate, then run the include-holds commands intentionally."
    )
    return {
        "candidate_id": brief_entry.get("candidate_id"),
        "feature_area": brief_entry.get("feature_area"),
        "journal_state": journal_state,
        "recommended_disposition": disposition,
        "operator_ack_required": True,
        "operator_blocker": brief_entry.get("operator_blocker"),
        "operator_posture": brief_entry.get("operator_posture"),
        "remaining_to_ready_count": brief_entry.get("remaining_to_ready_count"),
        "outstanding_reason_codes": brief_entry.get("outstanding_reason_codes"),
        "next_unlock_prerequisite": brief_entry.get("next_unlock_prerequisite"),
        "next_review_trigger": brief_entry.get("next_review_trigger"),
        "next_action": next_action,
        "next_action_hint": brief_entry.get("next_action_hint"),
        "include_holds_plan_command": brief_entry.get("include_holds_plan_command"),
        "include_holds_run_command": brief_entry.get("include_holds_run_command"),
        "run_id": brief_entry.get("run_id"),
        "host_etl_repo_path": brief_entry.get("host_etl_repo_path"),
    }


def build_reopen_journal(
    brief_payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    entries = [
        journal_entry(entry)
        for entry in sorted(
            list(brief_payload.get("entries") or []),
            key=lambda item: (
                str(item.get("brief_status") or ""),
                -int(item.get("remaining_to_ready_count") or 0),
                str(item.get("candidate_id") or ""),
            ),
        )
    ]
    deferred_count = sum(1 for entry in entries if entry.get("journal_state") == "deferred")
    review_pending_count = sum(1 for entry in entries if entry.get("journal_state") == "review-pending")
    if not entries:
        journal_status = "idle"
        operator_blocker = "no-reopen-candidates"
        next_action = "No ETW reopen journal entries are currently tracked."
    elif deferred_count:
        journal_status = "deferred"
        operator_blocker = "acknowledge-deferred-holds"
        next_action = "Keep the blocked lanes deferred until their prerequisites land."
    else:
        journal_status = "review-pending"
        operator_blocker = "acknowledge-review-pending"
        next_action = "Review the reopen-ready lanes before dispatching include-holds capture."

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_reopen_operator_brief_path": portable_path(BRIEF_PATH),
        "brief_status": brief_payload.get("brief_status"),
        "journal_status": journal_status,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "counts": {
            "candidate_count": len(entries),
            "deferred_count": deferred_count,
            "review_pending_count": review_pending_count,
            "ack_required_count": len(entries),
        },
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# ETW Stackwalk Reopen Journal",
        "",
        f"- Brief status: `{payload.get('brief_status')}`",
        f"- Journal status: `{payload.get('journal_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        f"- Deferred count: `{counts.get('deferred_count')}`",
        f"- Review-pending count: `{counts.get('review_pending_count')}`",
        f"- Ack-required count: `{counts.get('ack_required_count')}`",
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
                f"- Recommended disposition: `{entry.get('recommended_disposition')}`",
                f"- Operator blocker: `{entry.get('operator_blocker')}`",
                f"- Next unlock prerequisite: `{entry.get('next_unlock_prerequisite')}`",
                f"- Next action hint: `{entry.get('next_action_hint')}`",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an operator-facing ETW reopen journal from the operator brief.")
    parser.add_argument("--brief", type=Path, default=BRIEF_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_reopen_journal(load_json(args.brief))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "journal_status": payload.get("journal_status"),
                "candidate_count": (payload.get("counts") or {}).get("candidate_count"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
