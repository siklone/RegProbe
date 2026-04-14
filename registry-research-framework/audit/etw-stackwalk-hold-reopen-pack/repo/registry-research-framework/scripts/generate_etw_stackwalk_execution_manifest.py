#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
CURRENT_DIR = Path(__file__).resolve().parent
BATCH_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch.json"
RUN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-run.json"
HOLD_REOPEN_PLAN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-plan.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_hold_reopen_plan import load_json  # noqa: E402
from generate_etw_stackwalk_hold_reopen_plan import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def batch_item_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(item.get("candidate_id") or ""): item
        for item in payload.get("items") or []
        if str(item.get("candidate_id") or "").strip()
    }


def reopen_item_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(item.get("candidate_id") or ""): item
        for item in payload.get("items") or []
        if str(item.get("candidate_id") or "").strip()
    }


def manifest_entry(
    *,
    batch_item: dict[str, Any],
    reopen_item: dict[str, Any] | None,
    include_holds: bool,
) -> dict[str, Any]:
    capture_plan = batch_item.get("capture_plan") or {}
    run = capture_plan.get("run") or {}
    target = capture_plan.get("target") or {}
    blockers = [str(item) for item in (batch_item.get("promotion_blockers") or [])]
    hold_prereqs = list((reopen_item or {}).get("reopen_prerequisites") or [])
    selected = bool(batch_item.get("dispatch_recommended")) or (include_holds and batch_item.get("actionability") == "hold")
    selection_reason = "default-dispatch" if batch_item.get("dispatch_recommended") else "hold-reopen" if include_holds and batch_item.get("actionability") == "hold" else "excluded"
    return {
        "candidate_id": batch_item.get("candidate_id"),
        "feature_area": batch_item.get("feature_area"),
        "actionability": batch_item.get("actionability"),
        "selected": selected,
        "selection_reason": selection_reason,
        "profile_id": batch_item.get("profile_id"),
        "queue_state": batch_item.get("queue_state"),
        "promotion_state": batch_item.get("promotion_state"),
        "next_missing_layer": batch_item.get("next_missing_layer"),
        "promotion_blockers": blockers,
        "registry_path": target.get("registry_path"),
        "value_name": target.get("value_name"),
        "run_id": run.get("run_id"),
        "host_etl_repo_path": run.get("host_etl_repo_path"),
        "effective_config_command": batch_item.get("effective_config_command"),
        "dispatch_command": batch_item.get("dispatch_command"),
        "include_holds_plan_command": (reopen_item or {}).get("include_holds_plan_command"),
        "include_holds_run_command": (reopen_item or {}).get("include_holds_run_command"),
        "next_action_hint": batch_item.get("next_action_hint"),
        "reopen_prerequisites": hold_prereqs,
    }


def build_execution_manifest(
    batch_payload: dict[str, Any],
    run_payload: dict[str, Any],
    hold_reopen_payload: dict[str, Any],
    *,
    candidate_ids: set[str] | None = None,
    include_holds: bool = False,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    batch_map = batch_item_map(batch_payload)
    reopen_map = reopen_item_map(hold_reopen_payload)
    if candidate_ids is None:
        candidate_ids = set(batch_map)
    found_ids = sorted(candidate_id for candidate_id in candidate_ids if candidate_id in batch_map)
    missing_ids = sorted(candidate_id for candidate_id in candidate_ids if candidate_id not in batch_map)

    entries = [
        manifest_entry(
            batch_item=batch_map[candidate_id],
            reopen_item=reopen_map.get(candidate_id),
            include_holds=include_holds,
        )
        for candidate_id in found_ids
    ]
    selected_entries = [entry for entry in entries if entry.get("selected")]
    excluded_entries = [entry for entry in entries if not entry.get("selected")]
    status = "ready" if selected_entries else "idle"
    if missing_ids:
        status = "blocked"

    operator_next_action = (
        "Run the selected dispatch commands."
        if selected_entries
        else "Review excluded hold candidates and reopen intentionally if needed."
    )
    if missing_ids:
        operator_next_action = "Resolve missing candidate ids before using this execution manifest."

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "status": status,
        "source_batch_path": portable_path(BATCH_PATH),
        "source_run_path": portable_path(RUN_PATH),
        "source_hold_reopen_plan_path": portable_path(HOLD_REOPEN_PLAN_PATH),
        "include_holds": include_holds,
        "requested_candidate_ids": sorted(candidate_ids),
        "missing_candidate_ids": missing_ids,
        "selected_count": len(selected_entries),
        "excluded_count": len(excluded_entries),
        "default_selected_job_count": int(run_payload.get("selected_job_count") or 0),
        "default_skipped_hold_count": int(run_payload.get("skipped_hold_count") or 0),
        "operator": {
            "next_action": operator_next_action,
            "include_holds_required": bool(include_holds),
        },
        "entries": entries,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Execution Manifest",
        "",
        f"- Status: `{payload.get('status')}`",
        f"- Include holds: `{payload.get('include_holds')}`",
        f"- Requested candidates: `{', '.join(payload.get('requested_candidate_ids') or [])}`",
        f"- Missing candidates: `{', '.join(payload.get('missing_candidate_ids') or [])}`",
        f"- Selected entries: `{payload.get('selected_count')}`",
        f"- Excluded entries: `{payload.get('excluded_count')}`",
        f"- Default selected jobs: `{payload.get('default_selected_job_count')}`",
        f"- Default skipped hold jobs: `{payload.get('default_skipped_hold_count')}`",
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
                f"- Selected: `{entry.get('selected')}`",
                f"- Selection reason: `{entry.get('selection_reason')}`",
                f"- Actionability: `{entry.get('actionability')}`",
                f"- Blockers: `{entry.get('promotion_blockers')}`",
                f"- Registry target: `{entry.get('registry_path')}` / `{entry.get('value_name')}`",
                f"- Run id: `{entry.get('run_id')}`",
                f"- Host ETL path: `{entry.get('host_etl_repo_path')}`",
                f"- Next action hint: `{entry.get('next_action_hint')}`",
                "",
                "```bash",
                str(entry.get("effective_config_command") or ""),
                "```",
                "",
                "```bash",
                str(
                    entry.get("include_holds_run_command")
                    if entry.get("selection_reason") == "hold-reopen"
                    else entry.get("dispatch_command")
                )
                or "",
                "```",
                "",
            ]
        )
        prereqs = entry.get("reopen_prerequisites") or []
        if prereqs:
            lines.append("Prerequisites:")
            for prereq in prereqs:
                lines.append(f"- {prereq}")
            lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an operator-facing ETW stackwalk execution manifest for selected candidates.")
    parser.add_argument("--batch", type=Path, default=BATCH_PATH)
    parser.add_argument("--run", type=Path, default=RUN_PATH)
    parser.add_argument("--hold-reopen-plan", type=Path, default=HOLD_REOPEN_PLAN_PATH)
    parser.add_argument("--candidate-id", action="append", default=[], help="Limit the manifest to one or more candidate ids.")
    parser.add_argument("--include-holds", action="store_true", help="Allow intentional-hold candidates to be selected via their reopen commands.")
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    batch_payload = load_json(args.batch)
    run_payload = load_json(args.run)
    hold_reopen_payload = load_json(args.hold_reopen_plan)
    candidate_ids = {item for item in args.candidate_id if str(item).strip()} or None
    payload = build_execution_manifest(
        batch_payload,
        run_payload,
        hold_reopen_payload,
        candidate_ids=candidate_ids,
        include_holds=args.include_holds,
    )
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "status": payload.get("status"),
                "selected_count": payload.get("selected_count"),
            },
            indent=2,
        )
    )
    return 0 if payload.get("status") in {"ready", "idle"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
