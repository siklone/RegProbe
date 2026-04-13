#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import sys
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_EVIDENCE_ROOT = REPO_ROOT / "evidence"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-sync.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-sync.md"


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


refresh_pipeline_mod = load_local_module("sync_lane_refresh", FRAMEWORK_ROOT / "scripts" / "refresh_ghidra_autotrigger_pipeline.py")
health_check_mod = load_local_module("sync_lane_health_check", FRAMEWORK_ROOT / "scripts" / "check_ghidra_autotrigger_health.py")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def derive_operator_state(*, sync_status: str, manifest_payload: dict[str, Any] | None, health_payload: dict[str, Any] | None) -> dict[str, Any]:
    manifest_payload = manifest_payload or {}
    health_payload = health_payload or {}
    diagnostics = manifest_payload.get("diagnostics") or {}
    counts = health_payload.get("counts") or {}
    focus = health_payload.get("focus") or {}

    if sync_status == "idle":
        scanned = int(diagnostics.get("scanned_bundle_count") or 0)
        no_stack = int(((diagnostics.get("skipped_reason_counts") or {}).get("no-caller-stack")) or 0)
        no_match = int(((diagnostics.get("skipped_reason_counts") or {}).get("no-queue-match")) or 0)
        if scanned == 0:
            next_action = "Produce at least one fresh normalized registry bundle with caller stacks, then rerun sync."
            blocker = "no-bundles-discovered"
        elif no_stack > 0:
            next_action = "Rerun the WPR or ETW capture with caller-stack collection enabled so the lane has stack-capable bundles."
            blocker = "bundles-missing-caller-stack"
        elif no_match > 0:
            next_action = "Refresh the blocked ghidra queue or widen candidate matching so discovered bundles map to queued records."
            blocker = "bundles-missing-queue-match"
        else:
            next_action = "Provide at least one matching bundle input and rerun sync."
            blocker = "bundle-selection-empty"
        return {
            "status": "idle",
            "blocker": blocker,
            "next_action": next_action,
            "top_focus": None,
        }

    symbol_batch_jobs = int(counts.get("symbol_resolution_batch_jobs") or 0)
    symbol_selected = int(counts.get("symbol_resolution_run_selected_jobs") or 0)
    dispatch_jobs = int(counts.get("dispatch_jobs") or 0)
    dispatch_selected = int(counts.get("run_selected_jobs") or 0)
    if symbol_batch_jobs > 0 and symbol_selected > 0:
        next_action = "Run the symbol-resolution batch to resolve caller-stack pivots before deeper Ghidra dispatch."
        blocker = "symbol-resolution-ready"
        top_focus = focus.get("top_symbol_resolution_batch_request")
    elif dispatch_jobs > 0 and dispatch_selected > 0:
        next_action = "Run the prepared Ghidra dispatch batch for the queued blocked candidates."
        blocker = "dispatch-ready"
        top_focus = focus.get("top_queue_candidate")
    else:
        next_action = "Inspect the refreshed health surfaces; the lane is synchronized but no runnable symbol or dispatch jobs were selected."
        blocker = "synced-no-runnable-jobs"
        top_focus = focus.get("top_queue_candidate")
    return {
        "status": "ready" if sync_status == "ok" else "error",
        "blocker": blocker,
        "next_action": next_action,
        "top_focus": top_focus,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    operator = payload.get("operator") or {}
    lines = [
        "# Ghidra Autotrigger Sync",
        "",
        f"- Sync status: `{payload.get('sync_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Top focus: `{operator.get('top_focus')}`",
    ]
    refresh = payload.get("refresh") or {}
    if refresh:
        lines.extend(
            [
                "",
                "## Refresh",
                "",
                f"- Bundle count: `{refresh.get('bundle_count')}`",
                f"- Seed count: `{refresh.get('seed_count')}`",
                f"- Symbol resolution request count: `{refresh.get('symbol_resolution_request_count')}`",
                f"- Symbol resolution batch job count: `{refresh.get('symbol_resolution_batch_job_count')}`",
                f"- Symbol resolution selected jobs: `{refresh.get('symbol_resolution_run_selected_job_count')}`",
                f"- Dispatch job count: `{refresh.get('dispatch_job_count')}`",
                f"- Dispatch selected jobs: `{refresh.get('run_plan_selected_job_count')}`",
            ]
        )
    bundle_manifest = payload.get("bundle_manifest") or {}
    diagnostics = bundle_manifest.get("diagnostics") or {}
    if bundle_manifest:
        lines.extend(
            [
                "",
                "## Bundle Manifest",
                "",
                f"- Selected count: `{bundle_manifest.get('selected_count')}`",
                f"- Scanned bundle count: `{diagnostics.get('scanned_bundle_count')}`",
                f"- Caller-stack capable bundle count: `{diagnostics.get('caller_stack_capable_bundle_count')}`",
                f"- Skip reasons: `{json.dumps(diagnostics.get('skipped_reason_counts') or {}, sort_keys=True)}`",
            ]
        )
    return "\n".join(lines) + "\n"


def sync_lane(
    *,
    discover_input_roots: list[Path] | None = None,
    input_manifest_limit: int | None = None,
    queue_path: Path | None = None,
    bundle_manifest_path: Path | None = None,
    seeds_path: Path | None = None,
    symbol_queue_path: Path | None = None,
    symbol_batch_path: Path | None = None,
    symbol_run_path: Path | None = None,
    batch_path: Path | None = None,
    run_path: Path | None = None,
    health_path: Path | None = None,
    markdown_path: Path = MARKDOWN_PATH,
    output_path: Path = OUTPUT_PATH,
) -> dict[str, Any]:
    roots = discover_input_roots or [DEFAULT_EVIDENCE_ROOT]
    effective_manifest_path = bundle_manifest_path or refresh_pipeline_mod.INPUTS_PATH
    effective_health_path = health_path or refresh_pipeline_mod.HEALTH_PATH
    try:
        refresh_payload = refresh_pipeline_mod.refresh_pipeline(
            refresh_bundle_manifest=True,
            input_roots=roots,
            input_manifest_limit=input_manifest_limit,
            queue_path=queue_path or refresh_pipeline_mod.QUEUE_PATH,
            bundle_manifest_path=effective_manifest_path,
            seeds_path=seeds_path or refresh_pipeline_mod.SEEDS_PATH,
            symbol_queue_path=symbol_queue_path or refresh_pipeline_mod.SYMBOL_QUEUE_PATH,
            symbol_batch_path=symbol_batch_path or refresh_pipeline_mod.SYMBOL_BATCH_PATH,
            symbol_run_path=symbol_run_path or refresh_pipeline_mod.SYMBOL_RUN_PATH,
            batch_path=batch_path or refresh_pipeline_mod.BATCH_PATH,
            run_path=run_path or refresh_pipeline_mod.RUN_PATH,
            health_path=effective_health_path,
        )
    except ValueError as exc:
        manifest_payload = refresh_pipeline_mod.autotrigger.load_json(effective_manifest_path) if effective_manifest_path.exists() else {}
        selected_count = int((manifest_payload or {}).get("selected_count") or 0)
        if selected_count == 0:
            operator = derive_operator_state(sync_status="idle", manifest_payload=manifest_payload, health_payload=None)
            payload = {
                "schema_version": "1.0",
                "sync_status": "idle",
                "reason": "no-discovered-bundles",
                "discover_input_roots": [portable_path(path) for path in roots],
                "input_manifest_limit": input_manifest_limit,
                "refresh": None,
                "health_check": None,
                "bundle_manifest": {
                    "path": portable_path(effective_manifest_path),
                    "selected_count": selected_count,
                    "diagnostics": (manifest_payload or {}).get("diagnostics") or {},
                },
                "operator": operator,
                "error": str(exc),
            }
            write_json(output_path, payload)
            write_text(markdown_path, render_markdown(payload))
            return payload
        raise
    health_payload = health_check_mod.load_json(effective_health_path)
    errors = health_check_mod.validate_health(health_payload)
    operator = derive_operator_state(
        sync_status="ok" if not errors else "error",
        manifest_payload=refresh_pipeline_mod.autotrigger.load_json(effective_manifest_path) if effective_manifest_path.exists() else {},
        health_payload=health_payload,
    )
    payload = {
        "schema_version": "1.0",
        "sync_status": "ok" if not errors else "error",
        "discover_input_roots": [portable_path(path) for path in roots],
        "input_manifest_limit": input_manifest_limit,
        "refresh": refresh_payload,
        "health_check": {
            "status": "ok" if not errors else "error",
            "errors": errors,
            "path": portable_path(effective_health_path),
        },
        "operator": operator,
    }
    write_json(output_path, payload)
    write_text(markdown_path, render_markdown(payload))
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Refresh and validate the Ghidra autotrigger lane in one command.")
    parser.add_argument("--discover-input-root", type=Path, action="append", default=[])
    parser.add_argument("--input-manifest-limit", type=int, default=None)
    parser.add_argument("--queue", type=Path, default=None)
    parser.add_argument("--bundle-manifest", type=Path, default=None)
    parser.add_argument("--seeds-output", type=Path, default=None)
    parser.add_argument("--symbol-queue-output", type=Path, default=None)
    parser.add_argument("--symbol-batch-output", type=Path, default=None)
    parser.add_argument("--symbol-run-output", type=Path, default=None)
    parser.add_argument("--batch-output", type=Path, default=None)
    parser.add_argument("--run-output", type=Path, default=None)
    parser.add_argument("--health-output", type=Path, default=None)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    args = parser.parse_args()

    payload = sync_lane(
        discover_input_roots=args.discover_input_root,
        input_manifest_limit=args.input_manifest_limit,
        queue_path=args.queue,
        bundle_manifest_path=args.bundle_manifest,
        seeds_path=args.seeds_output,
        symbol_queue_path=args.symbol_queue_output,
        symbol_batch_path=args.symbol_batch_output,
        symbol_run_path=args.symbol_run_output,
        batch_path=args.batch_output,
        run_path=args.run_output,
        health_path=args.health_output,
        markdown_path=args.markdown_output,
        output_path=args.output,
    )
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("sync_status") in {"ok", "idle"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
