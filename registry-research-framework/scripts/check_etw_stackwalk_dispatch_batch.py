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
PROFILE_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "etw-stackwalk-profiles.json"
RUNNER_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "tweak-vm-runners.json"
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "research-queue.json"
PROMOTION_GATES_PATH = REPO_ROOT / "research" / "promotion-gates.json"
BATCH_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch-check.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_dispatch_batch import build_dispatch_batch  # noqa: E402
from generate_etw_stackwalk_dispatch_batch import load_json  # noqa: E402
from generate_etw_stackwalk_dispatch_batch import portable_path  # noqa: E402
from generate_etw_stackwalk_capture_plan import load_config  # noqa: E402
from generate_etw_stackwalk_capture_plan import load_runner_config  # noqa: E402


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


def compare_batch(
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
        "batch_status",
        "mapped_candidate_count",
        "ready_capture_count",
        "dispatch_recommended_count",
        "active_candidate_count",
        "hold_candidate_count",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")

    if sorted(surface.get("profiles_used") or []) != sorted(expected.get("profiles_used") or []):
        errors.append("profiles_used does not match current runner/profile config.")

    surface_items = item_map(surface)
    expected_items = item_map(expected)
    if sorted(surface_items) != sorted(expected_items):
        errors.append("candidate set does not match current mapped ETW stackwalk runners.")

    for candidate_id, expected_item in expected_items.items():
        item = surface_items.get(candidate_id)
        if not item:
            continue
        for key in (
            "profile_id",
            "queue_state",
            "promotion_state",
            "next_missing_layer",
            "actionability",
            "capture_ready",
            "dispatch_recommended",
            "dispatch_command",
            "effective_config_command",
            "next_action_hint",
        ):
            if item.get(key) != expected_item.get(key):
                errors.append(f"{candidate_id}: {key} mismatch.")
        if (item.get("promotion_blockers") or []) != (expected_item.get("promotion_blockers") or []):
            errors.append(f"{candidate_id}: promotion_blockers mismatch.")
        item_run = ((item.get("capture_plan") or {}).get("run") or {})
        expected_run = ((expected_item.get("capture_plan") or {}).get("run") or {})
        if item_run.get("run_id") != expected_run.get("run_id"):
            errors.append(f"{candidate_id}: capture_plan.run.run_id mismatch.")
        if item_run.get("host_etl_repo_path") != expected_run.get("host_etl_repo_path"):
            errors.append(f"{candidate_id}: capture_plan.run.host_etl_repo_path mismatch.")
        stack_capture = ((item.get("capture_plan") or {}).get("stack_capture") or {})
        if stack_capture.get("expected") is not True:
            errors.append(f"{candidate_id}: stack_capture.expected must be true.")
        if "RegQueryValue" not in set(stack_capture.get("stackwalk_events") or []):
            errors.append(f"{candidate_id}: stackwalk_events must include RegQueryValue.")
        if "--candidate-id" not in str(item.get("dispatch_command") or ""):
            errors.append(f"{candidate_id}: dispatch_command must include --candidate-id.")
        if "--print-effective-config" not in str(item.get("effective_config_command") or ""):
            errors.append(f"{candidate_id}: effective_config_command must include --print-effective-config.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "batch_status": surface.get("batch_status"),
        "mapped_candidate_count": surface.get("mapped_candidate_count"),
        "dispatch_recommended_count": surface.get("dispatch_recommended_count"),
        "profiles_used": surface.get("profiles_used") or [],
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Dispatch Batch Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Batch status: `{payload.get('batch_status')}`",
        f"- Mapped candidates: `{payload.get('mapped_candidate_count')}`",
        f"- Dispatch recommended now: `{payload.get('dispatch_recommended_count')}`",
        f"- Profiles used: `{', '.join(payload.get('profiles_used') or [])}`",
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
    parser = argparse.ArgumentParser(description="Validate the ETW stackwalk dispatch batch against current repo inputs.")
    parser.add_argument("--batch", type=Path, default=BATCH_PATH)
    parser.add_argument("--profile-config", type=Path, default=PROFILE_CONFIG_PATH)
    parser.add_argument("--runner-config", type=Path, default=RUNNER_CONFIG_PATH)
    parser.add_argument("--queue", type=Path, default=QUEUE_PATH)
    parser.add_argument("--promotion-gates", type=Path, default=PROMOTION_GATES_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    surface = load_json(args.batch)
    expected = build_dispatch_batch(
        profile_config=load_config(args.profile_config),
        runner_config=load_runner_config(args.runner_config),
        queue_payload=load_json(args.queue),
        gates_payload=load_json(args.promotion_gates),
        generated_utc=str(surface.get("generated_utc") or now_utc()),
    )
    payload = compare_batch(surface, expected)
    payload["batch_path"] = portable_path(args.batch)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
