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
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-run-check.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-run-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from run_etw_stackwalk_dispatch_batch import build_run_plan  # noqa: E402
from run_etw_stackwalk_dispatch_batch import load_json  # noqa: E402
from run_etw_stackwalk_dispatch_batch import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def job_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(job.get("candidate_id") or ""): job
        for job in payload.get("jobs") or []
        if str(job.get("candidate_id") or "").strip()
    }


def compare_run_plan(
    surface: dict[str, Any],
    expected: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []

    if surface.get("schema_version") != "1.0":
        errors.append("schema_version must be 1.0.")
    if surface.get("mode") != "dry-run":
        errors.append("mode must be dry-run for the checked dispatch-run surface.")
    for key in ("source_batch", "include_holds", "runner_available", "selected_job_count", "skipped_hold_count"):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")

    surface_jobs = job_map(surface)
    expected_jobs = job_map(expected)
    if sorted(surface_jobs) != sorted(expected_jobs):
        errors.append("selected candidate set does not match current dispatch batch.")

    for candidate_id, expected_job in expected_jobs.items():
        job = surface_jobs.get(candidate_id)
        if not job:
            continue
        for key in ("actionability", "profile_id", "dispatch_command", "next_action_hint"):
            if job.get(key) != expected_job.get(key):
                errors.append(f"{candidate_id}: {key} mismatch.")
        if (job.get("dispatch_command_argv") or []) != (expected_job.get("dispatch_command_argv") or []):
            errors.append(f"{candidate_id}: dispatch_command_argv mismatch.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "mode": surface.get("mode"),
        "selected_job_count": surface.get("selected_job_count"),
        "skipped_hold_count": surface.get("skipped_hold_count"),
        "include_holds": surface.get("include_holds"),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Dispatch Run Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Mode: `{payload.get('mode')}`",
        f"- Include holds: `{payload.get('include_holds')}`",
        f"- Selected jobs: `{payload.get('selected_job_count')}`",
        f"- Skipped hold jobs: `{payload.get('skipped_hold_count')}`",
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
    parser = argparse.ArgumentParser(description="Validate the ETW stackwalk dispatch dry-run surface against the current batch.")
    parser.add_argument("--batch", type=Path, default=BATCH_PATH)
    parser.add_argument("--run", type=Path, default=RUN_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    batch = load_json(args.batch)
    surface = load_json(args.run)
    expected = build_run_plan(batch, generated_utc=str(surface.get("generated_utc") or now_utc()))
    payload = compare_run_plan(surface, expected)
    payload["run_path"] = portable_path(args.run)
    payload["batch_path"] = portable_path(args.batch)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
