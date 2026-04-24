#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
LEDGER_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-decision-ledger.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-readiness-scoreboard.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-readiness-scoreboard.md"

REASON_PRIORITY = {
    "await-seeding-pivot": 1,
    "await-primary-doc": 2,
    "explicit-reopen-required": 3,
    "manual-review-required": 4,
}


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


def dominant_reason_code(reason_codes: list[str]) -> str:
    if not reason_codes:
        return "manual-review-required"
    return sorted(reason_codes, key=lambda code: (REASON_PRIORITY.get(code, 99), code))[0]


def next_unlock_prerequisite(entry: dict[str, Any], dominant_code: str) -> str | None:
    prerequisites = [str(value) for value in (entry.get("reopen_prerequisites") or [])]
    if not prerequisites:
        return None
    if dominant_code == "await-seeding-pivot":
        for prerequisite in prerequisites:
            if "boot/init reader" in prerequisite or "seeding caller" in prerequisite:
                return prerequisite
    if dominant_code == "await-primary-doc":
        for prerequisite in prerequisites:
            if "primary current-build Microsoft document" in prerequisite:
                return prerequisite
    if dominant_code == "explicit-reopen-required":
        for prerequisite in prerequisites:
            if "Explicitly reopen" in prerequisite:
                return prerequisite
    return prerequisites[0]


def unblocker_class(dominant_code: str) -> str:
    if dominant_code in {"await-seeding-pivot", "await-primary-doc"}:
        return "evidence-gap"
    if dominant_code == "explicit-reopen-required":
        return "operator-decision"
    return "manual-review"


def scoreboard_entry(entry: dict[str, Any]) -> dict[str, Any]:
    reason_codes = [str(code) for code in (entry.get("decision_reason_codes") or [])]
    dominant_code = dominant_reason_code(reason_codes)
    decision_state = str(entry.get("decision_state") or "defer")
    readiness_bucket = "ready" if decision_state == "review-ready" else "blocked"
    next_prerequisite = next_unlock_prerequisite(entry, dominant_code)
    return {
        "candidate_id": entry.get("candidate_id"),
        "feature_area": entry.get("feature_area"),
        "decision_state": decision_state,
        "readiness_bucket": readiness_bucket,
        "dominant_reason_code": dominant_code,
        "reason_code_priority": REASON_PRIORITY.get(dominant_code, 99),
        "unblocker_class": unblocker_class(dominant_code),
        "blocker_count": entry.get("blocker_count"),
        "prerequisite_count": entry.get("prerequisite_count"),
        "next_unlock_prerequisite": next_prerequisite,
        "next_review_trigger": entry.get("next_review_trigger"),
        "include_holds_plan_command": entry.get("include_holds_plan_command"),
        "include_holds_run_command": entry.get("include_holds_run_command"),
        "run_id": entry.get("run_id"),
        "host_etl_repo_path": entry.get("host_etl_repo_path"),
    }


def build_reopen_readiness_scoreboard(
    ledger_payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    entries = [
        scoreboard_entry(entry)
        for entry in sorted(
            list(ledger_payload.get("entries") or []),
            key=lambda item: (
                REASON_PRIORITY.get(dominant_reason_code(list(item.get("decision_reason_codes") or [])), 99),
                str(item.get("candidate_id") or ""),
            ),
        )
    ]
    ready_count = sum(1 for entry in entries if entry.get("readiness_bucket") == "ready")
    blocked_count = sum(1 for entry in entries if entry.get("readiness_bucket") == "blocked")
    reason_counts: dict[str, int] = {}
    for entry in entries:
        code = str(entry.get("dominant_reason_code") or "")
        reason_counts[code] = reason_counts.get(code, 0) + 1

    if not entries:
        scoreboard_status = "idle"
        next_action = "No reopen candidates are currently tracked."
    elif ready_count and blocked_count:
        scoreboard_status = "mixed"
        next_action = "Review the ready candidates first, then track the next unlock prerequisite for the blocked ones."
    elif ready_count:
        scoreboard_status = "review-ready"
        next_action = "Review the ready candidates before reopening the ETW lane."
    else:
        scoreboard_status = "blocked"
        next_action = "Track the next unlock prerequisite for the top blocked reopen candidate."

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_reopen_decision_ledger_path": portable_path(LEDGER_PATH),
        "ledger_status": ledger_payload.get("ledger_status"),
        "scoreboard_status": scoreboard_status,
        "operator": {
            "next_action": next_action,
            "intentional_reopen_required": bool(entries),
        },
        "counts": {
            "candidate_count": len(entries),
            "ready_count": ready_count,
            "blocked_count": blocked_count,
            "dominant_reason_counts": reason_counts,
        },
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# ETW Stackwalk Reopen Readiness Scoreboard",
        "",
        f"- Ledger status: `{payload.get('ledger_status')}`",
        f"- Scoreboard status: `{payload.get('scoreboard_status')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        f"- Ready count: `{counts.get('ready_count')}`",
        f"- Blocked count: `{counts.get('blocked_count')}`",
        f"- Dominant reason counts: `{counts.get('dominant_reason_counts')}`",
        f"- Next action: `{(payload.get('operator') or {}).get('next_action')}`",
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
                f"- Readiness bucket: `{entry.get('readiness_bucket')}`",
                f"- Dominant reason: `{entry.get('dominant_reason_code')}`",
                f"- Unblocker class: `{entry.get('unblocker_class')}`",
                f"- Next unlock prerequisite: `{entry.get('next_unlock_prerequisite')}`",
                f"- Next review trigger: `{entry.get('next_review_trigger')}`",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a compact ETW stackwalk reopen readiness scoreboard from the decision ledger.")
    parser.add_argument("--ledger", type=Path, default=LEDGER_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_reopen_readiness_scoreboard(load_json(args.ledger))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "scoreboard_status": payload.get("scoreboard_status"),
                "candidate_count": (payload.get("counts") or {}).get("candidate_count"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
