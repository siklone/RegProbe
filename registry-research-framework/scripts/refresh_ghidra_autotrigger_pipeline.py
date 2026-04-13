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
BATCH_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-batch.json"
RUN_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-run.json"


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


autotrigger = load_local_module("refresh_pipeline_autotrigger", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_autotrigger_seeds.py")
dispatch_batch = load_local_module("refresh_pipeline_dispatch_batch", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_dispatch_batch.py")
dispatch_runner = load_local_module("refresh_pipeline_dispatch_runner", FRAMEWORK_ROOT / "scripts" / "run_ghidra_dispatch_batch.py")


def refresh_pipeline(
    bundle_path: Path,
    *,
    queue_path: Path = QUEUE_PATH,
    seeds_path: Path = SEEDS_PATH,
    batch_path: Path = BATCH_PATH,
    run_path: Path = RUN_PATH,
) -> dict[str, Any]:
    bundle = autotrigger.load_json(bundle_path)
    queue_rows = autotrigger.load_jsonl(queue_path)
    seeds = autotrigger.autotrigger_seeds_from_bundle(
        bundle,
        bundle_path=autotrigger.portable_path(bundle_path),
        queue_rows=queue_rows,
    )
    autotrigger.write_jsonl(seeds_path, seeds)

    batch = dispatch_batch.dispatch_batch_from_queue(queue_rows, autotrigger_rows=seeds)
    dispatch_batch.write_json(batch_path, batch)

    run_plan = dispatch_runner.build_run_plan(batch)
    dispatch_runner.write_json(run_path, run_plan)

    return {
        "bundle_path": autotrigger.portable_path(bundle_path),
        "seed_count": len(seeds),
        "dispatch_job_count": batch.get("job_count", 0),
        "dispatch_autotrigger_matched_job_count": batch.get("autotrigger_matched_job_count", 0),
        "run_plan_selected_job_count": run_plan.get("selected_job_count", 0),
        "runner_available": run_plan.get("runner_available"),
        "outputs": {
            "seeds_path": autotrigger.portable_path(seeds_path),
            "batch_path": autotrigger.portable_path(batch_path),
            "run_path": autotrigger.portable_path(run_path),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Refresh Ghidra auto-trigger surfaces from a fresh normalized registry bundle.")
    parser.add_argument("--bundle", type=Path, required=True)
    parser.add_argument("--queue", type=Path, default=QUEUE_PATH)
    parser.add_argument("--seeds-output", type=Path, default=SEEDS_PATH)
    parser.add_argument("--batch-output", type=Path, default=BATCH_PATH)
    parser.add_argument("--run-output", type=Path, default=RUN_PATH)
    args = parser.parse_args()

    payload = refresh_pipeline(
        args.bundle,
        queue_path=args.queue,
        seeds_path=args.seeds_output,
        batch_path=args.batch_output,
        run_path=args.run_output,
    )
    print(json.dumps(payload, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
