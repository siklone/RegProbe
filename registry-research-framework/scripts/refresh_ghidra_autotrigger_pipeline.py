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
HANDOFF_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-handoff.json"
HANDOFF_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-handoff.md"
TRANSFER_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer.json"
TRANSFER_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer.md"
TRANSFER_PACK_OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack"
TRANSFER_PACK_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.json"
TRANSFER_PACK_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.md"
TRANSFER_PACK_ARCHIVE_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.zip"
TRANSFER_PACK_CHECK_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-check.json"
TRANSFER_PACK_CHECK_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-check.md"
TRANSFER_PACK_IMPORT_ROOT = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-import"
TRANSFER_PACK_IMPORT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-import.json"
TRANSFER_PACK_IMPORT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-import.md"
EXECUTION_PLAN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-plan.json"
EXECUTION_PLAN_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-plan.md"
EXECUTION_RUN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run.json"
EXECUTION_RUN_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run.md"
EXECUTION_RUN_CHECK_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run-check.json"
EXECUTION_RUN_CHECK_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run-check.md"
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
handoff_mod = load_local_module("refresh_pipeline_symbol_handoff", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_symbol_resolution_handoff.py")
transfer_mod = load_local_module("refresh_pipeline_symbol_transfer", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_symbol_resolution_transfer.py")
transfer_pack_mod = load_local_module(
    "refresh_pipeline_symbol_transfer_pack",
    FRAMEWORK_ROOT / "scripts" / "materialize_ghidra_symbol_resolution_transfer_pack.py",
)
transfer_pack_check_mod = load_local_module(
    "refresh_pipeline_symbol_transfer_pack_check",
    FRAMEWORK_ROOT / "scripts" / "check_ghidra_symbol_resolution_transfer_pack.py",
)
transfer_pack_import_mod = load_local_module(
    "refresh_pipeline_symbol_transfer_pack_import",
    FRAMEWORK_ROOT / "scripts" / "unpack_ghidra_symbol_resolution_transfer_pack.py",
)
execution_plan_mod = load_local_module(
    "refresh_pipeline_symbol_transfer_execution_plan",
    FRAMEWORK_ROOT / "scripts" / "generate_ghidra_transfer_pack_execution_plan.py",
)
execution_run_mod = load_local_module(
    "refresh_pipeline_symbol_transfer_execution_run",
    FRAMEWORK_ROOT / "scripts" / "run_ghidra_transfer_pack_execution_plan.py",
)
execution_run_check_mod = load_local_module(
    "refresh_pipeline_symbol_transfer_execution_run_check",
    FRAMEWORK_ROOT / "scripts" / "check_ghidra_transfer_pack_execution_run.py",
)


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
    handoff_path: Path = HANDOFF_PATH,
    handoff_markdown_path: Path = HANDOFF_MARKDOWN_PATH,
    transfer_path: Path = TRANSFER_PATH,
    transfer_markdown_path: Path = TRANSFER_MARKDOWN_PATH,
    transfer_pack_output_root: Path = TRANSFER_PACK_OUTPUT_ROOT,
    transfer_pack_summary_path: Path = TRANSFER_PACK_SUMMARY_PATH,
    transfer_pack_markdown_path: Path = TRANSFER_PACK_MARKDOWN_PATH,
    transfer_pack_archive_path: Path = TRANSFER_PACK_ARCHIVE_PATH,
    transfer_pack_check_path: Path = TRANSFER_PACK_CHECK_PATH,
    transfer_pack_check_markdown_path: Path = TRANSFER_PACK_CHECK_MARKDOWN_PATH,
    transfer_pack_import_root: Path | None = None,
    transfer_pack_import_path: Path | None = None,
    transfer_pack_import_markdown_path: Path | None = None,
    execution_plan_path: Path | None = None,
    execution_plan_markdown_path: Path | None = None,
    execution_run_path: Path | None = None,
    execution_run_markdown_path: Path | None = None,
    execution_run_check_path: Path | None = None,
    execution_run_check_markdown_path: Path | None = None,
) -> dict[str, Any]:
    transfer_pack_import_root = transfer_pack_import_root or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-import")
    transfer_pack_import_path = transfer_pack_import_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-import.json")
    transfer_pack_import_markdown_path = transfer_pack_import_markdown_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-import.md")
    execution_plan_path = execution_plan_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-execution-plan.json")
    execution_plan_markdown_path = execution_plan_markdown_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-execution-plan.md")
    execution_run_path = execution_run_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-execution-run.json")
    execution_run_markdown_path = execution_run_markdown_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-execution-run.md")
    execution_run_check_path = execution_run_check_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-execution-run-check.json")
    execution_run_check_markdown_path = execution_run_check_markdown_path or transfer_pack_summary_path.with_name("ghidra-symbol-resolution-transfer-pack-execution-run-check.md")
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
    handoff = handoff_mod.handoff_payload(
        symbol_batch,
        symbol_run,
        batch_path=symbol_batch_path,
        run_path=symbol_run_path,
    )
    handoff_mod.write_json(handoff_path, handoff)
    handoff_mod.write_text(handoff_markdown_path, handoff_mod.render_markdown(handoff))
    transfer = transfer_mod.transfer_payload(handoff, handoff_path=handoff_path)
    transfer_mod.write_json(transfer_path, transfer)
    transfer_mod.write_text(transfer_markdown_path, transfer_mod.render_markdown(transfer))
    transfer_pack = transfer_pack_mod.materialize_transfer_pack(
        transfer,
        transfer_path=transfer_path,
        output_root=transfer_pack_output_root,
        summary_path=transfer_pack_summary_path,
        markdown_path=transfer_pack_markdown_path,
        archive_path=transfer_pack_archive_path,
    )
    transfer_pack_check = transfer_pack_check_mod.validate_transfer_pack(
        transfer_pack,
        summary_path=transfer_pack_summary_path,
    )
    transfer_pack_check_mod.write_json(transfer_pack_check_path, transfer_pack_check)
    transfer_pack_check_mod.write_text(
        transfer_pack_check_markdown_path,
        transfer_pack_check_mod.render_markdown(transfer_pack_check),
    )
    transfer_pack_import = transfer_pack_import_mod.unpack_transfer_pack(
        transfer_pack,
        summary_path=transfer_pack_summary_path,
        output_root=transfer_pack_import_root,
        output_path=transfer_pack_import_path,
        markdown_path=transfer_pack_import_markdown_path,
    )
    execution_plan = execution_plan_mod.execution_plan_from_import(
        transfer_pack_import,
        import_path=transfer_pack_import_path,
    )
    execution_plan_mod.write_json(execution_plan_path, execution_plan)
    execution_plan_mod.write_text(execution_plan_markdown_path, execution_plan_mod.render_markdown(execution_plan))
    execution_run = execution_run_mod.execution_run_from_plan(
        execution_plan,
        plan_path=execution_plan_path,
        execute=False,
    )
    execution_run_mod.write_json(execution_run_path, execution_run)
    execution_run_mod.write_text(execution_run_markdown_path, execution_run_mod.render_markdown(execution_run))
    execution_run_check = execution_run_check_mod.validate_execution_run(
        execution_run,
        run_path=execution_run_path,
    )
    execution_run_check_mod.write_json(execution_run_check_path, execution_run_check)
    execution_run_check_mod.write_text(
        execution_run_check_markdown_path,
        execution_run_check_mod.render_markdown(execution_run_check),
    )

    input_manifest = autotrigger.load_json(effective_manifest_path) if effective_manifest_path.exists() else {"entries": []}
    health = autotrigger_health.health_payload(
        input_manifest,
        queue_rows,
        seeds,
        symbol_queue,
        symbol_batch,
        symbol_run,
        handoff,
        transfer,
        transfer_pack,
        transfer_pack_check,
        batch,
        run_plan,
        execution_run=execution_run,
        execution_run_check=execution_run_check,
    )
    autotrigger_health.write_json(health_path, health)

    return {
        "bundle_count": len(effective_bundle_paths),
        "bundle_paths": [autotrigger.portable_path(path) for path in effective_bundle_paths],
        "seed_count": len(seeds),
        "symbol_resolution_request_count": symbol_queue.get("request_count", 0),
        "symbol_resolution_batch_job_count": symbol_batch.get("job_count", 0),
        "symbol_resolution_run_selected_job_count": symbol_run.get("selected_job_count", 0),
        "symbol_resolution_handoff_status": handoff.get("handoff_status"),
        "symbol_resolution_handoff_selected_job_count": int((handoff.get("counts") or {}).get("selected_jobs") or 0),
        "symbol_resolution_transfer_status": transfer.get("transfer_status"),
        "symbol_resolution_transfer_selected_job_count": int((transfer.get("counts") or {}).get("selected_jobs") or 0),
        "symbol_resolution_transfer_pack_status": transfer_pack.get("pack_status"),
        "symbol_resolution_transfer_pack_selected_job_count": int((transfer_pack.get("counts") or {}).get("selected_jobs") or 0),
        "symbol_resolution_transfer_pack_check_status": transfer_pack_check.get("check_status"),
        "symbol_resolution_transfer_pack_check_error_count": len(transfer_pack_check.get("errors") or []),
        "symbol_resolution_transfer_pack_import_status": transfer_pack_import.get("import_status"),
        "symbol_resolution_execution_plan_status": execution_plan.get("execution_plan_status"),
        "symbol_resolution_execution_run_status": execution_run.get("execution_run_status"),
        "symbol_resolution_execution_run_check_status": execution_run_check.get("check_status"),
        "symbol_resolution_execution_run_check_error_count": len(execution_run_check.get("errors") or []),
        "symbol_resolution_execution_run_ready_job_count": int((execution_run.get("counts") or {}).get("ready_jobs") or 0),
        "symbol_resolution_execution_run_blocked_job_count": int((execution_run.get("counts") or {}).get("blocked_jobs") or 0),
        "dispatch_job_count": batch.get("job_count", 0),
        "dispatch_autotrigger_matched_job_count": batch.get("autotrigger_matched_job_count", 0),
        "run_plan_selected_job_count": run_plan.get("selected_job_count", 0),
        "runner_available": run_plan.get("runner_available"),
        "outputs": {
            "seeds_path": autotrigger.portable_path(seeds_path),
            "symbol_queue_path": autotrigger.portable_path(symbol_queue_path),
            "symbol_batch_path": autotrigger.portable_path(symbol_batch_path),
            "symbol_run_path": autotrigger.portable_path(symbol_run_path),
            "handoff_path": autotrigger.portable_path(handoff_path),
            "handoff_markdown_path": autotrigger.portable_path(handoff_markdown_path),
            "transfer_path": autotrigger.portable_path(transfer_path),
            "transfer_markdown_path": autotrigger.portable_path(transfer_markdown_path),
            "transfer_pack_output_root": autotrigger.portable_path(transfer_pack_output_root),
            "transfer_pack_summary_path": autotrigger.portable_path(transfer_pack_summary_path),
            "transfer_pack_markdown_path": autotrigger.portable_path(transfer_pack_markdown_path),
            "transfer_pack_archive_path": autotrigger.portable_path(transfer_pack_archive_path),
            "transfer_pack_check_path": autotrigger.portable_path(transfer_pack_check_path),
            "transfer_pack_check_markdown_path": autotrigger.portable_path(transfer_pack_check_markdown_path),
            "transfer_pack_import_root": autotrigger.portable_path(transfer_pack_import_root),
            "transfer_pack_import_path": autotrigger.portable_path(transfer_pack_import_path),
            "transfer_pack_import_markdown_path": autotrigger.portable_path(transfer_pack_import_markdown_path),
            "execution_plan_path": autotrigger.portable_path(execution_plan_path),
            "execution_plan_markdown_path": autotrigger.portable_path(execution_plan_markdown_path),
            "execution_run_path": autotrigger.portable_path(execution_run_path),
            "execution_run_markdown_path": autotrigger.portable_path(execution_run_markdown_path),
            "execution_run_check_path": autotrigger.portable_path(execution_run_check_path),
            "execution_run_check_markdown_path": autotrigger.portable_path(execution_run_check_markdown_path),
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
    parser.add_argument("--handoff-output", type=Path, default=HANDOFF_PATH)
    parser.add_argument("--handoff-markdown-output", type=Path, default=HANDOFF_MARKDOWN_PATH)
    parser.add_argument("--transfer-output", type=Path, default=TRANSFER_PATH)
    parser.add_argument("--transfer-markdown-output", type=Path, default=TRANSFER_MARKDOWN_PATH)
    parser.add_argument("--transfer-pack-output-root", type=Path, default=TRANSFER_PACK_OUTPUT_ROOT)
    parser.add_argument("--transfer-pack-summary-output", type=Path, default=TRANSFER_PACK_SUMMARY_PATH)
    parser.add_argument("--transfer-pack-markdown-output", type=Path, default=TRANSFER_PACK_MARKDOWN_PATH)
    parser.add_argument("--transfer-pack-archive-output", type=Path, default=TRANSFER_PACK_ARCHIVE_PATH)
    parser.add_argument("--transfer-pack-check-output", type=Path, default=TRANSFER_PACK_CHECK_PATH)
    parser.add_argument("--transfer-pack-check-markdown-output", type=Path, default=TRANSFER_PACK_CHECK_MARKDOWN_PATH)
    parser.add_argument("--transfer-pack-import-root", type=Path, default=TRANSFER_PACK_IMPORT_ROOT)
    parser.add_argument("--transfer-pack-import-output", type=Path, default=TRANSFER_PACK_IMPORT_PATH)
    parser.add_argument("--transfer-pack-import-markdown-output", type=Path, default=TRANSFER_PACK_IMPORT_MARKDOWN_PATH)
    parser.add_argument("--execution-plan-output", type=Path, default=EXECUTION_PLAN_PATH)
    parser.add_argument("--execution-plan-markdown-output", type=Path, default=EXECUTION_PLAN_MARKDOWN_PATH)
    parser.add_argument("--execution-run-output", type=Path, default=EXECUTION_RUN_PATH)
    parser.add_argument("--execution-run-markdown-output", type=Path, default=EXECUTION_RUN_MARKDOWN_PATH)
    parser.add_argument("--execution-run-check-output", type=Path, default=EXECUTION_RUN_CHECK_PATH)
    parser.add_argument("--execution-run-check-markdown-output", type=Path, default=EXECUTION_RUN_CHECK_MARKDOWN_PATH)
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
        handoff_path=args.handoff_output,
        handoff_markdown_path=args.handoff_markdown_output,
        transfer_path=args.transfer_output,
        transfer_markdown_path=args.transfer_markdown_output,
        transfer_pack_output_root=args.transfer_pack_output_root,
        transfer_pack_summary_path=args.transfer_pack_summary_output,
        transfer_pack_markdown_path=args.transfer_pack_markdown_output,
        transfer_pack_archive_path=args.transfer_pack_archive_output,
        transfer_pack_check_path=args.transfer_pack_check_output,
        transfer_pack_check_markdown_path=args.transfer_pack_check_markdown_output,
        transfer_pack_import_root=args.transfer_pack_import_root,
        transfer_pack_import_path=args.transfer_pack_import_output,
        transfer_pack_import_markdown_path=args.transfer_pack_import_markdown_output,
        execution_plan_path=args.execution_plan_output,
        execution_plan_markdown_path=args.execution_plan_markdown_output,
        execution_run_path=args.execution_run_output,
        execution_run_markdown_path=args.execution_run_markdown_output,
        execution_run_check_path=args.execution_run_check_output,
        execution_run_check_markdown_path=args.execution_run_check_markdown_output,
    )
    print(json.dumps(payload, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
