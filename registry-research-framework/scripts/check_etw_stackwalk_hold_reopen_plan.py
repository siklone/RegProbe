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
PLAN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-plan.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-plan-check.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-plan-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_hold_reopen_plan import build_hold_reopen_plan  # noqa: E402
from generate_etw_stackwalk_hold_reopen_plan import load_json  # noqa: E402
from generate_etw_stackwalk_hold_reopen_plan import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def item_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(item.get("candidate_id") or ""): item
        for item in payload.get("items") or []
        if str(item.get("candidate_id") or "").strip()
    }


def compare_hold_reopen_plan(
    surface: dict[str, Any],
    expected: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []

    if surface.get("schema_version") != "1.0":
        errors.append("schema_version must be 1.0.")
    for key in (
        "source_batch_path",
        "source_run_path",
        "default_run_mode",
        "default_selected_job_count",
        "default_skipped_hold_count",
        "reopen_candidate_count",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")

    surface_items = item_map(surface)
    expected_items = item_map(expected)
    if sorted(surface_items) != sorted(expected_items):
        errors.append("candidate set does not match current hold-capable ETW batch items.")

    for candidate_id, expected_item in expected_items.items():
        item = surface_items.get(candidate_id)
        if not item:
            continue
        for key in (
            "feature_area",
            "next_missing_layer",
            "promotion_blockers",
            "reopen_prerequisites",
            "default_dispatch_excluded",
            "effective_config_command",
            "dispatch_command",
            "include_holds_plan_command",
            "include_holds_run_command",
            "run_id",
            "host_etl_repo_path",
            "next_action_hint",
        ):
            if item.get(key) != expected_item.get(key):
                errors.append(f"{candidate_id}: {key} mismatch.")
        if "--include-holds" not in str(item.get("include_holds_plan_command") or ""):
            errors.append(f"{candidate_id}: include_holds_plan_command must include --include-holds.")
        if "--run" not in str(item.get("include_holds_run_command") or ""):
            errors.append(f"{candidate_id}: include_holds_run_command must include --run.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "reopen_candidate_count": surface.get("reopen_candidate_count"),
        "default_selected_job_count": surface.get("default_selected_job_count"),
        "default_skipped_hold_count": surface.get("default_skipped_hold_count"),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Hold Reopen Plan Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Reopen candidates: `{payload.get('reopen_candidate_count')}`",
        f"- Default selected jobs: `{payload.get('default_selected_job_count')}`",
        f"- Default skipped hold jobs: `{payload.get('default_skipped_hold_count')}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    else:
        for error in errors:
            lines.append(f"- {error}")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the ETW stackwalk hold reopen plan surface.")
    parser.add_argument("--batch", type=Path, default=BATCH_PATH)
    parser.add_argument("--run", type=Path, default=RUN_PATH)
    parser.add_argument("--plan", type=Path, default=PLAN_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    batch = load_json(args.batch)
    run_payload = load_json(args.run)
    surface = load_json(args.plan)
    expected = build_hold_reopen_plan(batch, run_payload, generated_utc=str(surface.get("generated_utc") or now_utc()))
    payload = compare_hold_reopen_plan(surface, expected)
    payload["plan_path"] = portable_path(args.plan)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
