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
                "error": str(exc),
            }
            write_json(output_path, payload)
            return payload
        raise
    health_payload = health_check_mod.load_json(effective_health_path)
    errors = health_check_mod.validate_health(health_payload)
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
    }
    write_json(output_path, payload)
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
        output_path=args.output,
    )
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("sync_status") in {"ok", "idle"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
