#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
PACK_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-pack.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-decision-ledger.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-decision-ledger.md"


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


def decision_reason_codes(blockers: list[str]) -> list[str]:
    lowered = [str(blocker).lower() for blocker in blockers]
    codes: list[str] = []
    if any("no-current-build-registry-seeding-path" in blocker for blocker in lowered):
        codes.append("await-seeding-pivot")
    if any("no-primary-current-build-doc" in blocker for blocker in lowered):
        codes.append("await-primary-doc")
    if any(blocker == "intentional-hold" for blocker in lowered):
        codes.append("explicit-reopen-required")
    if not codes:
        codes.append("manual-review-required")
    return codes


def next_review_trigger(reason_codes: list[str]) -> str:
    if "await-seeding-pivot" in reason_codes and "await-primary-doc" in reason_codes:
        return "Revisit after a current-build seeding-path pivot and a primary Microsoft doc both land."
    if "await-seeding-pivot" in reason_codes:
        return "Revisit after a current-build boot/init reader or registry seeding caller pivot lands."
    if "await-primary-doc" in reason_codes:
        return "Revisit after a primary current-build Microsoft document lands for the exact value semantics."
    if "explicit-reopen-required" in reason_codes:
        return "Revisit only after we intentionally reopen this ETW lane."
    return "Manual review required before reopening this lane."


def decision_entry(item: dict[str, Any]) -> dict[str, Any]:
    blockers = [str(value) for value in (item.get("promotion_blockers") or [])]
    prerequisites = [str(value) for value in (item.get("reopen_prerequisites") or [])]
    reason_codes = decision_reason_codes(blockers)
    prerequisite_status = "satisfied" if not prerequisites else "unsatisfied"
    decision_state = "review-ready" if prerequisite_status == "satisfied" else "defer"
    decision_summary = (
        "Candidate is ready for an explicit reopen review."
        if decision_state == "review-ready"
        else "Keep the lane deferred until the listed prerequisites are satisfied."
    )
    return {
        "candidate_id": item.get("candidate_id"),
        "feature_area": item.get("feature_area"),
        "next_missing_layer": item.get("next_missing_layer"),
        "promotion_blockers": blockers,
        "blocker_count": len(blockers),
        "reopen_prerequisites": prerequisites,
        "prerequisite_count": len(prerequisites),
        "prerequisite_status": prerequisite_status,
        "decision_state": decision_state,
        "decision_reason_codes": reason_codes,
        "decision_summary": decision_summary,
        "next_review_trigger": next_review_trigger(reason_codes),
        "include_holds_plan_command": item.get("include_holds_plan_command"),
        "include_holds_run_command": item.get("include_holds_run_command"),
        "effective_config_command": item.get("effective_config_command"),
        "run_id": item.get("run_id"),
        "host_etl_repo_path": item.get("host_etl_repo_path"),
        "next_action_hint": item.get("next_action_hint"),
    }


def build_reopen_decision_ledger(
    pack_payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    entries = [
        decision_entry(item)
        for item in sorted(
            list(pack_payload.get("items") or []),
            key=lambda item: str(item.get("candidate_id") or ""),
        )
    ]
    deferred_count = sum(1 for entry in entries if entry.get("decision_state") == "defer")
    review_ready_count = sum(1 for entry in entries if entry.get("decision_state") == "review-ready")
    if not entries:
        ledger_status = "idle"
        next_action = "No hold reopen candidates are currently available."
    elif review_ready_count:
        ledger_status = "review-ready"
        next_action = "Review the candidates marked review-ready before reopening the ETW lane."
    else:
        ledger_status = "deferred"
        next_action = "Keep the ETW lane closed until one of the listed prerequisites lands."

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_hold_reopen_pack_path": portable_path(PACK_PATH),
        "pack_status": pack_payload.get("pack_status"),
        "ledger_status": ledger_status,
        "operator": {
            "next_action": next_action,
            "intentional_reopen_required": bool(entries),
        },
        "reopen_candidate_count": len(entries),
        "deferred_candidate_count": deferred_count,
        "review_ready_candidate_count": review_ready_count,
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Reopen Decision Ledger",
        "",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Ledger status: `{payload.get('ledger_status')}`",
        f"- Reopen candidates: `{payload.get('reopen_candidate_count')}`",
        f"- Deferred candidates: `{payload.get('deferred_candidate_count')}`",
        f"- Review-ready candidates: `{payload.get('review_ready_candidate_count')}`",
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
                f"- Decision state: `{entry.get('decision_state')}`",
                f"- Reason codes: `{entry.get('decision_reason_codes')}`",
                f"- Blockers: `{entry.get('promotion_blockers')}`",
                f"- Prerequisites: `{entry.get('reopen_prerequisites')}`",
                f"- Next review trigger: `{entry.get('next_review_trigger')}`",
                f"- Run id: `{entry.get('run_id')}`",
                f"- Host ETL path: `{entry.get('host_etl_repo_path')}`",
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
    parser = argparse.ArgumentParser(description="Generate a deterministic reopen decision ledger from the ETW hold reopen pack.")
    parser.add_argument("--pack", type=Path, default=PACK_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_reopen_decision_ledger(load_json(args.pack))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "ledger_status": payload.get("ledger_status"),
                "reopen_candidate_count": payload.get("reopen_candidate_count"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
