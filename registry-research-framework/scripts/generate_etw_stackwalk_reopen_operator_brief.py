#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DELTA_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-prerequisite-delta.json"
PACK_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-pack.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-operator-brief.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-operator-brief.md"


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


def pack_item_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(item.get("candidate_id") or ""): item
        for item in payload.get("items") or []
        if str(item.get("candidate_id") or "").strip()
    }


def brief_entry(delta_entry: dict[str, Any], pack_entry: dict[str, Any] | None) -> dict[str, Any]:
    blocked = str(delta_entry.get("delta_status") or "blocked") == "blocked"
    posture = "do-not-run" if blocked else "review-before-run"
    blocker = (
        "outstanding-prerequisites"
        if blocked
        else "explicit-review-required"
    )
    return {
        "candidate_id": delta_entry.get("candidate_id"),
        "feature_area": delta_entry.get("feature_area"),
        "brief_status": "blocked" if blocked else "review-ready",
        "operator_posture": posture,
        "operator_blocker": blocker,
        "remaining_to_ready_count": delta_entry.get("remaining_to_ready_count"),
        "outstanding_reason_codes": delta_entry.get("outstanding_reason_codes"),
        "next_unlock_prerequisite": delta_entry.get("next_unlock_prerequisite"),
        "next_review_trigger": delta_entry.get("next_review_trigger"),
        "promotion_blockers": (pack_entry or {}).get("promotion_blockers"),
        "next_action_hint": (pack_entry or {}).get("next_action_hint"),
        "effective_config_command": (pack_entry or {}).get("effective_config_command"),
        "dispatch_command": (pack_entry or {}).get("dispatch_command"),
        "include_holds_plan_command": (pack_entry or {}).get("include_holds_plan_command"),
        "include_holds_run_command": (pack_entry or {}).get("include_holds_run_command"),
        "run_id": delta_entry.get("run_id"),
        "host_etl_repo_path": delta_entry.get("host_etl_repo_path"),
    }


def build_reopen_operator_brief(
    delta_payload: dict[str, Any],
    pack_payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    pack_map = pack_item_map(pack_payload)
    entries = [
        brief_entry(delta_entry, pack_map.get(str(delta_entry.get("candidate_id") or "")))
        for delta_entry in sorted(
            list(delta_payload.get("entries") or []),
            key=lambda item: (
                str(item.get("delta_status") or ""),
                -int(item.get("remaining_to_ready_count") or 0),
                str(item.get("candidate_id") or ""),
            ),
        )
    ]
    blocked_count = sum(1 for entry in entries if entry.get("brief_status") == "blocked")
    review_ready_count = sum(1 for entry in entries if entry.get("brief_status") == "review-ready")
    if not entries:
        brief_status = "idle"
        operator_blocker = "no-reopen-candidates"
        next_action = "No ETW reopen candidates are currently tracked."
    elif blocked_count:
        brief_status = "blocked"
        operator_blocker = "reopen-prerequisites-blocked"
        next_action = "Do not run the include-holds commands yet; land the next unlock prerequisite first."
    else:
        brief_status = "review-ready"
        operator_blocker = "explicit-review-required"
        next_action = "Review the reopen-ready candidates, then run the include-holds commands intentionally."

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_reopen_prerequisite_delta_path": portable_path(DELTA_PATH),
        "source_hold_reopen_pack_path": portable_path(PACK_PATH),
        "delta_status": delta_payload.get("delta_status"),
        "pack_status": pack_payload.get("pack_status"),
        "brief_status": brief_status,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "counts": {
            "candidate_count": len(entries),
            "blocked_candidates": blocked_count,
            "review_ready_candidates": review_ready_count,
        },
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# ETW Stackwalk Reopen Operator Brief",
        "",
        f"- Delta status: `{payload.get('delta_status')}`",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Brief status: `{payload.get('brief_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        f"- Blocked candidates: `{counts.get('blocked_candidates')}`",
        f"- Review-ready candidates: `{counts.get('review_ready_candidates')}`",
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
                f"- Brief status: `{entry.get('brief_status')}`",
                f"- Operator posture: `{entry.get('operator_posture')}`",
                f"- Remaining to ready: `{entry.get('remaining_to_ready_count')}`",
                f"- Next unlock prerequisite: `{entry.get('next_unlock_prerequisite')}`",
                f"- Next action hint: `{entry.get('next_action_hint')}`",
                "",
                "```bash",
                str(entry.get("include_holds_plan_command") or ""),
                "```",
                "",
                "```bash",
                str(entry.get("include_holds_run_command") or ""),
                "```",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a concise operator brief for ETW stackwalk reopen candidates.")
    parser.add_argument("--delta", type=Path, default=DELTA_PATH)
    parser.add_argument("--pack", type=Path, default=PACK_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_reopen_operator_brief(load_json(args.delta), load_json(args.pack))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "brief_status": payload.get("brief_status"),
                "candidate_count": (payload.get("counts") or {}).get("candidate_count"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
