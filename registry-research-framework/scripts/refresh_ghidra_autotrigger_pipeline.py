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
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-job-queue.jsonl"
SEEDS_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-seeds.jsonl"
SYMBOL_QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-queue.json"
SYMBOL_BATCH_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-batch.json"
SYMBOL_RUN_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-run.json"
BATCH_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-batch.json"
RUN_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-run.json"
HEALTH_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-health.json"
INPUTS_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-inputs.json"


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


autotrigger = load_local_module("refresh_pipeline_autotrigger", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_autotrigger_seeds.py")
autotrigger_inputs = load_local_module("refresh_pipeline_autotrigger_inputs", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_autotrigger_inputs.py")
symbol_queue_mod = load_local_module("refresh_pipeline_symbol_queue", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_symbol_resolution_queue.py")
symbol_batch_mod = load_local_module("refresh_pipeline_symbol_batch", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_symbol_resolution_batch.py")
symbol_run_mod = load_local_module("refresh_pipeline_symbol_run", FRAMEWORK_ROOT / "scripts" / "run_ghidra_symbol_resolution_batch.py")
dispatch_batch = load_local_module("refresh_pipeline_dispatch_batch", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_dispatch_batch.py")
dispatch_runner = load_local_module("refresh_pipeline_dispatch_runner", FRAMEWORK_ROOT / "scripts" / "run_ghidra_dispatch_batch.py")
autotrigger_health = load_local_module("refresh_pipeline_autotrigger_health", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_autotrigger_health.py")


def resolve_path(path_value: str) -> Path:
    path = Path(path_value)
    return path if path.is_absolute() else (REPO_ROOT / path)


def bundle_paths_from_manifest(path: Path) -> list[Path]:
    payload = autotrigger.load_json(path)
    bundle_paths: list[Path] = []
    for entry in payload.get("entries") or []:
        path_value = str(entry.get("path") or "").strip()
        if path_value:
            bundle_paths.append(resolve_path(path_value).resolve())
    return bundle_paths


def refresh_pipeline(
    bundle_path: Path | None = None,
    *,
    bundle_paths: list[Path] | None = None,
    bundle_root: Path | None = None,
    bundle_manifest_path: Path | None = None,
    refresh_bundle_manifest: bool = False,
    input_roots: list[Path] | None = None,
    input_manifest_limit: int | None = None,
    queue_path: Path = QUEUE_PATH,
    seeds_path: Path = SEEDS_PATH,
    symbol_queue_path: Path = SYMBOL_QUEUE_PATH,
    symbol_batch_path: Path = SYMBOL_BATCH_PATH,
    symbol_run_path: Path = SYMBOL_RUN_PATH,
    batch_path: Path = BATCH_PATH,
    run_path: Path = RUN_PATH,
    health_path: Path = HEALTH_PATH,
) -> dict[str, Any]:
    queue_rows = autotrigger.load_jsonl(queue_path)
    bundle_manifest_payload: dict[str, Any] | None = None
    effective_manifest_path = bundle_manifest_path or INPUTS_PATH
    if refresh_bundle_manifest:
        roots = input_roots or [autotrigger_inputs.DEFAULT_SEARCH_ROOT]
        bundle_manifest_payload = autotrigger_inputs.input_manifest(
            roots,
            queue_rows=queue_rows,
            limit=input_manifest_limit,
            require_caller_stack=True,
            require_queue_match=True,
        )
        autotrigger_inputs.write_json(effective_manifest_path, bundle_manifest_payload)
    effective_bundle_paths = autotrigger.collect_bundle_paths(
        ([bundle_path] if bundle_path else []) + list(bundle_paths or []),
        bundle_root=bundle_root,
    )
    if effective_manifest_path and not effective_bundle_paths:
        effective_bundle_paths = bundle_paths_from_manifest(effective_manifest_path)
    if not effective_bundle_paths:
        raise ValueError("Provide bundle_path, bundle_paths, or bundle_root with at least one normalized bundle.")
    seeds = autotrigger.autotrigger_seeds_from_bundle_paths(
        effective_bundle_paths,
        queue_rows=queue_rows,
    )
    autotrigger.write_jsonl(seeds_path, seeds)

    symbol_queue = symbol_queue_mod.symbol_resolution_queue_from_seeds(seeds)
    symbol_queue_mod.write_json(symbol_queue_path, symbol_queue)
    symbol_batch = symbol_batch_mod.symbol_resolution_batch_from_queue(symbol_queue)
    symbol_batch_mod.write_json(symbol_batch_path, symbol_batch)
    symbol_run = symbol_run_mod.build_run_plan(symbol_batch)
    symbol_run_mod.write_json(symbol_run_path, symbol_run)

    batch = dispatch_batch.dispatch_batch_from_queue(queue_rows, autotrigger_rows=seeds)
    dispatch_batch.write_json(batch_path, batch)

    run_plan = dispatch_runner.build_run_plan(batch)
    dispatch_runner.write_json(run_path, run_plan)

    input_manifest = autotrigger.load_json(effective_manifest_path) if effective_manifest_path.exists() else {"entries": []}
    health = autotrigger_health.health_payload(
        input_manifest,
        queue_rows,
        seeds,
        symbol_queue,
        symbol_batch,
        symbol_run,
        batch,
        run_plan,
    )
    autotrigger_health.write_json(health_path, health)

    return {
        "bundle_count": len(effective_bundle_paths),
        "bundle_paths": [autotrigger.portable_path(path) for path in effective_bundle_paths],
        "seed_count": len(seeds),
        "symbol_resolution_request_count": symbol_queue.get("request_count", 0),
        "symbol_resolution_batch_job_count": symbol_batch.get("job_count", 0),
        "symbol_resolution_run_selected_job_count": symbol_run.get("selected_job_count", 0),
        "dispatch_job_count": batch.get("job_count", 0),
        "dispatch_autotrigger_matched_job_count": batch.get("autotrigger_matched_job_count", 0),
        "run_plan_selected_job_count": run_plan.get("selected_job_count", 0),
        "runner_available": run_plan.get("runner_available"),
        "outputs": {
            "seeds_path": autotrigger.portable_path(seeds_path),
            "symbol_queue_path": autotrigger.portable_path(symbol_queue_path),
            "symbol_batch_path": autotrigger.portable_path(symbol_batch_path),
            "symbol_run_path": autotrigger.portable_path(symbol_run_path),
            "batch_path": autotrigger.portable_path(batch_path),
            "run_path": autotrigger.portable_path(run_path),
            "health_path": autotrigger.portable_path(health_path),
            "bundle_manifest_path": autotrigger.portable_path(effective_manifest_path) if effective_manifest_path else None,
        },
        "bundle_manifest_refreshed": refresh_bundle_manifest,
        "bundle_manifest_selected_count": int((bundle_manifest_payload or {}).get("selected_count") or 0),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Refresh Ghidra auto-trigger surfaces from one or more fresh normalized registry bundles.")
    parser.add_argument("--bundle", type=Path, action="append", default=[])
    parser.add_argument("--bundle-root", type=Path, default=None)
    parser.add_argument("--bundle-manifest", type=Path, default=INPUTS_PATH)
    parser.add_argument("--refresh-bundle-manifest", action="store_true")
    parser.add_argument("--discover-input-root", type=Path, action="append", default=[])
    parser.add_argument("--input-manifest-limit", type=int, default=None)
    parser.add_argument("--queue", type=Path, default=QUEUE_PATH)
    parser.add_argument("--seeds-output", type=Path, default=SEEDS_PATH)
    parser.add_argument("--symbol-queue-output", type=Path, default=SYMBOL_QUEUE_PATH)
    parser.add_argument("--symbol-batch-output", type=Path, default=SYMBOL_BATCH_PATH)
    parser.add_argument("--symbol-run-output", type=Path, default=SYMBOL_RUN_PATH)
    parser.add_argument("--batch-output", type=Path, default=BATCH_PATH)
    parser.add_argument("--run-output", type=Path, default=RUN_PATH)
    parser.add_argument("--health-output", type=Path, default=HEALTH_PATH)
    args = parser.parse_args()

    payload = refresh_pipeline(
        bundle_paths=args.bundle,
        bundle_root=args.bundle_root,
        bundle_manifest_path=args.bundle_manifest,
        refresh_bundle_manifest=args.refresh_bundle_manifest,
        input_roots=args.discover_input_root,
        input_manifest_limit=args.input_manifest_limit,
        queue_path=args.queue,
        seeds_path=args.seeds_output,
        symbol_queue_path=args.symbol_queue_output,
        symbol_batch_path=args.symbol_batch_output,
        symbol_run_path=args.symbol_run_output,
        batch_path=args.batch_output,
        run_path=args.run_output,
        health_path=args.health_output,
    )
    print(json.dumps(payload, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
