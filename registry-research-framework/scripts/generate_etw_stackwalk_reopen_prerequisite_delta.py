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
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-prerequisite-delta.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-prerequisite-delta.md"

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


def sort_reason_codes(reason_codes: list[str]) -> list[str]:
    return sorted(reason_codes, key=lambda code: (REASON_PRIORITY.get(code, 99), code))


def reason_class(reason_code: str) -> str:
    if reason_code in {"await-seeding-pivot", "await-primary-doc"}:
        return "evidence-gap"
    if reason_code == "explicit-reopen-required":
        return "operator-decision"
    return "manual-review"


def next_unlock_prerequisite(prerequisites: list[str], reason_codes: list[str]) -> str | None:
    if not prerequisites:
        return None
    if "await-seeding-pivot" in reason_codes:
        for prerequisite in prerequisites:
            if "boot/init reader" in prerequisite or "seeding caller" in prerequisite:
                return prerequisite
    if "await-primary-doc" in reason_codes:
        for prerequisite in prerequisites:
            if "primary current-build Microsoft document" in prerequisite:
                return prerequisite
    if "explicit-reopen-required" in reason_codes:
        for prerequisite in prerequisites:
            if "Explicitly reopen" in prerequisite:
                return prerequisite
    return prerequisites[0]


def delta_entry(entry: dict[str, Any]) -> dict[str, Any]:
    reason_codes = sort_reason_codes([str(code) for code in (entry.get("decision_reason_codes") or [])])
    prerequisites = [str(value) for value in (entry.get("reopen_prerequisites") or [])]
    outstanding = list(prerequisites)
    classes = sorted({reason_class(code) for code in reason_codes})
    delta_status = "clear" if not outstanding else "blocked"
    return {
        "candidate_id": entry.get("candidate_id"),
        "feature_area": entry.get("feature_area"),
        "decision_state": entry.get("decision_state"),
        "delta_status": delta_status,
        "remaining_to_ready_count": len(outstanding),
        "outstanding_reason_codes": reason_codes,
        "outstanding_reason_classes": classes,
        "outstanding_prerequisites": outstanding,
        "next_unlock_prerequisite": next_unlock_prerequisite(outstanding, reason_codes),
        "next_review_trigger": entry.get("next_review_trigger"),
        "run_id": entry.get("run_id"),
        "host_etl_repo_path": entry.get("host_etl_repo_path"),
    }


def build_reopen_prerequisite_delta(
    ledger_payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    entries = [
        delta_entry(entry)
        for entry in sorted(
            list(ledger_payload.get("entries") or []),
            key=lambda item: (
                len(list(item.get("reopen_prerequisites") or [])),
                str(item.get("candidate_id") or ""),
            ),
            reverse=True,
        )
    ]
    blocked_count = sum(1 for entry in entries if entry.get("delta_status") == "blocked")
    clear_count = sum(1 for entry in entries if entry.get("delta_status") == "clear")
    outstanding_reason_counts: dict[str, int] = {}
    unique_prerequisites: dict[str, list[str]] = {}
    for entry in entries:
        for code in entry.get("outstanding_reason_codes") or []:
            outstanding_reason_counts[code] = outstanding_reason_counts.get(code, 0) + 1
        for prerequisite in entry.get("outstanding_prerequisites") or []:
            unique_prerequisites.setdefault(prerequisite, []).append(str(entry.get("candidate_id") or ""))

    if not entries:
        delta_status = "idle"
        next_action = "No reopen prerequisite deltas are currently tracked."
    elif blocked_count:
        delta_status = "blocked"
        next_action = "Use the delta entries to land the next outstanding prerequisite before reopening the ETW lane."
    else:
        delta_status = "clear"
        next_action = "All tracked reopen candidates are clear for explicit review."

    unique_prerequisite_entries = [
        {
            "prerequisite": prerequisite,
            "candidate_ids": candidate_ids,
            "candidate_count": len(candidate_ids),
        }
        for prerequisite, candidate_ids in sorted(
            unique_prerequisites.items(),
            key=lambda item: (-len(item[1]), item[0]),
        )
    ]

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_reopen_decision_ledger_path": portable_path(LEDGER_PATH),
        "ledger_status": ledger_payload.get("ledger_status"),
        "delta_status": delta_status,
        "operator": {
            "next_action": next_action,
            "intentional_reopen_required": bool(entries),
        },
        "counts": {
            "candidate_count": len(entries),
            "blocked_candidate_count": blocked_count,
            "clear_candidate_count": clear_count,
            "outstanding_reason_counts": outstanding_reason_counts,
            "unique_prerequisite_count": len(unique_prerequisite_entries),
        },
        "unique_prerequisites": unique_prerequisite_entries,
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# ETW Stackwalk Reopen Prerequisite Delta",
        "",
        f"- Ledger status: `{payload.get('ledger_status')}`",
        f"- Delta status: `{payload.get('delta_status')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        f"- Blocked candidate count: `{counts.get('blocked_candidate_count')}`",
        f"- Clear candidate count: `{counts.get('clear_candidate_count')}`",
        f"- Outstanding reason counts: `{counts.get('outstanding_reason_counts')}`",
        f"- Unique prerequisite count: `{counts.get('unique_prerequisite_count')}`",
        f"- Next action: `{(payload.get('operator') or {}).get('next_action')}`",
        "",
        "## Unique Prerequisites",
        "",
    ]
    unique_prereqs = payload.get("unique_prerequisites") or []
    if not unique_prereqs:
        lines.append("- none")
    else:
        for item in unique_prereqs:
            lines.append(f"- `{item.get('prerequisite')}` -> `{item.get('candidate_ids')}`")

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
                f"- Delta status: `{entry.get('delta_status')}`",
                f"- Outstanding reason codes: `{entry.get('outstanding_reason_codes')}`",
                f"- Outstanding reason classes: `{entry.get('outstanding_reason_classes')}`",
                f"- Remaining to ready: `{entry.get('remaining_to_ready_count')}`",
                f"- Next unlock prerequisite: `{entry.get('next_unlock_prerequisite')}`",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a candidate-by-candidate ETW stackwalk reopen prerequisite delta surface.")
    parser.add_argument("--ledger", type=Path, default=LEDGER_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_reopen_prerequisite_delta(load_json(args.ledger))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "delta_status": payload.get("delta_status"),
                "candidate_count": (payload.get("counts") or {}).get("candidate_count"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
