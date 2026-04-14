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
MANIFEST_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest-check.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_execution_manifest import build_execution_manifest  # noqa: E402
from generate_etw_stackwalk_execution_manifest import load_json  # noqa: E402
from generate_etw_stackwalk_execution_manifest import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def entry_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(entry.get("candidate_id") or ""): entry
        for entry in payload.get("entries") or []
        if str(entry.get("candidate_id") or "").strip()
    }


def compare_execution_manifest(
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
        "status",
        "source_batch_path",
        "source_run_path",
        "source_hold_reopen_plan_path",
        "include_holds",
        "requested_candidate_ids",
        "missing_candidate_ids",
        "selected_count",
        "excluded_count",
        "default_selected_job_count",
        "default_skipped_hold_count",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")

    if (surface.get("operator") or {}).get("next_action") != (expected.get("operator") or {}).get("next_action"):
        errors.append("operator.next_action mismatch.")
    if (surface.get("operator") or {}).get("include_holds_required") != (expected.get("operator") or {}).get("include_holds_required"):
        errors.append("operator.include_holds_required mismatch.")

    surface_entries = entry_map(surface)
    expected_entries = entry_map(expected)
    if sorted(surface_entries) != sorted(expected_entries):
        errors.append("candidate set does not match current ETW execution inputs.")

    for candidate_id, expected_entry in expected_entries.items():
        entry = surface_entries.get(candidate_id)
        if not entry:
            continue
        for key in (
            "feature_area",
            "actionability",
            "selected",
            "selection_reason",
            "profile_id",
            "queue_state",
            "promotion_state",
            "next_missing_layer",
            "promotion_blockers",
            "registry_path",
            "value_name",
            "run_id",
            "host_etl_repo_path",
            "effective_config_command",
            "dispatch_command",
            "include_holds_plan_command",
            "include_holds_run_command",
            "next_action_hint",
            "reopen_prerequisites",
        ):
            if entry.get(key) != expected_entry.get(key):
                errors.append(f"{candidate_id}: {key} mismatch.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "status": surface.get("status"),
        "selected_count": surface.get("selected_count"),
        "excluded_count": surface.get("excluded_count"),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Execution Manifest Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Manifest status: `{payload.get('status')}`",
        f"- Selected entries: `{payload.get('selected_count')}`",
        f"- Excluded entries: `{payload.get('excluded_count')}`",
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
    parser = argparse.ArgumentParser(description="Validate the ETW stackwalk execution manifest surface.")
    parser.add_argument("--batch", type=Path, default=BATCH_PATH)
    parser.add_argument("--run", type=Path, default=RUN_PATH)
    parser.add_argument("--hold-reopen-plan", type=Path, default=HOLD_REOPEN_PLAN_PATH)
    parser.add_argument("--manifest", type=Path, default=MANIFEST_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    batch_payload = load_json(args.batch)
    run_payload = load_json(args.run)
    hold_reopen_payload = load_json(args.hold_reopen_plan)
    surface = load_json(args.manifest)
    expected = build_execution_manifest(
        batch_payload,
        run_payload,
        hold_reopen_payload,
        candidate_ids=set(surface.get("requested_candidate_ids") or []),
        include_holds=bool(surface.get("include_holds")),
        generated_utc=str(surface.get("generated_utc") or now_utc()),
    )
    payload = compare_execution_manifest(surface, expected)
    payload["manifest_path"] = portable_path(args.manifest)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
