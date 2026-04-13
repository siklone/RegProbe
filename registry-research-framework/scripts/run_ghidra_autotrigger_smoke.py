#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-job-queue.jsonl"
OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-smoke"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-smoke.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-smoke.md"


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


autotrigger = load_local_module("ghidra_smoke_autotrigger", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_autotrigger_seeds.py")
sync_lane_mod = load_local_module("ghidra_smoke_sync_lane", FRAMEWORK_ROOT / "scripts" / "sync_ghidra_autotrigger_lane.py")
transfer_pack_check_mod = load_local_module(
    "ghidra_smoke_transfer_pack_check",
    FRAMEWORK_ROOT / "scripts" / "check_ghidra_symbol_resolution_transfer_pack.py",
)
transfer_pack_import_mod = load_local_module(
    "ghidra_smoke_transfer_pack_import",
    FRAMEWORK_ROOT / "scripts" / "unpack_ghidra_symbol_resolution_transfer_pack.py",
)
execution_plan_mod = load_local_module(
    "ghidra_smoke_transfer_pack_execution_plan",
    FRAMEWORK_ROOT / "scripts" / "generate_ghidra_transfer_pack_execution_plan.py",
)
execution_run_mod = load_local_module(
    "ghidra_smoke_transfer_pack_execution_run",
    FRAMEWORK_ROOT / "scripts" / "run_ghidra_transfer_pack_execution_plan.py",
)
execution_run_check_mod = load_local_module(
    "ghidra_smoke_transfer_pack_execution_run_check",
    FRAMEWORK_ROOT / "scripts" / "check_ghidra_transfer_pack_execution_run.py",
)
smoke_check_mod = load_local_module(
    "ghidra_smoke_check",
    FRAMEWORK_ROOT / "scripts" / "check_ghidra_autotrigger_smoke.py",
)


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def slugify(value: Any) -> str:
    return "".join(ch if ch.isalnum() else "-" for ch in str(value or "").lower()).strip("-") or "unnamed"


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def select_queue_rows(queue_rows: list[dict[str, Any]], *, candidate_ids: list[str] | None = None, limit: int | None = None) -> list[dict[str, Any]]:
    wanted = {item.strip() for item in (candidate_ids or []) if item.strip()}
    rows = [
        row
        for row in queue_rows
        if str(row.get("candidate_id") or "").strip()
        and (not wanted or str(row.get("candidate_id") or "").strip() in wanted)
    ]
    rows.sort(
        key=lambda row: (
            int(row.get("priority_rank") or 999999),
            str(row.get("candidate_id") or ""),
        )
    )
    if limit is not None:
        rows = rows[:limit]
    return rows


def synthetic_unresolved_frame(index: int, row: dict[str, Any]) -> str:
    target_binary = autotrigger.infer_target_binary(row.get("key_path")) or "ntoskrnl.exe"
    rotation = (index - 1) % 4
    if rotation == 0:
        return f"{target_binary}+0x{0x1800 + (index * 0x120):X}"
    if rotation == 1:
        return f"0xFFFFF8051234{index:04X}"
    if rotation == 2:
        return f"Synthetic{slugify(row.get('candidate_id'))}Resolver"
    return f"{target_binary}+0x{0x2800 + (index * 0x120):X}"


def resolved_frame_for_row(row: dict[str, Any]) -> str:
    key_path = autotrigger.normalize_registry_path(row.get("key_path"))
    if "control\\power" in key_path or "session manager\\power" in key_path:
        return "nt!PopPowerRequestInitialize"
    if "session manager\\kernel" in key_path:
        return "nt!ExpInitializeExecutive"
    return "nt!SyntheticSmokeGate"


def event_value_name(row: dict[str, Any]) -> str | None:
    patterns = autotrigger.split_value_patterns(row.get("value_name"))
    if patterns:
        return patterns[0]
    cleaned = str(row.get("value_name") or "").strip()
    return cleaned or None


def build_synthetic_bundle(queue_rows: list[dict[str, Any]], *, generated_utc: str | None = None) -> tuple[dict[str, Any], dict[str, Any]]:
    generated_utc = generated_utc or now_utc()
    events: list[dict[str, Any]] = []
    candidate_ids: list[str] = []
    resolution_counts: Counter[str] = Counter()

    for index, row in enumerate(queue_rows, start=1):
        candidate_id = str(row.get("candidate_id") or "").strip()
        if not candidate_id:
            continue
        unresolved_frame = synthetic_unresolved_frame(index, row)
        resolution_counts[autotrigger.frame_resolution_kind(unresolved_frame)] += 1
        candidate_ids.append(candidate_id)
        events.append(
            {
                "key_path": row.get("key_path"),
                "value_name": event_value_name(row),
                "operation": "RegQueryValue",
                "caller_stack": [
                    resolved_frame_for_row(row),
                    unresolved_frame,
                ],
            }
        )

    bundle = {
        "schema_version": "1.0",
        "run_id": f"ghidra-autotrigger-smoke-{generated_utc.replace(':', '').replace('-', '')}",
        "source_tool": "synthetic-smoke",
        "capture_phase": "synthetic-smoke",
        "normalizer_name": "ghidra-autotrigger-smoke",
        "status": "completed",
        "generated_utc": generated_utc,
        "event_count": len(events),
        "stack_capture": {
            "source_fields": ["SyntheticCallerStack"],
            "captured_event_count": len(events),
        },
        "events": events,
    }
    metadata = {
        "selected_candidate_count": len(candidate_ids),
        "selected_candidate_ids": candidate_ids,
        "frame_resolution_counts": dict(sorted(resolution_counts.items())),
    }
    return bundle, metadata


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# Ghidra Autotrigger Smoke",
        "",
        f"- Smoke status: `{payload.get('smoke_status')}`",
        f"- Sync status: `{payload.get('sync_status')}`",
        f"- Selected candidates: `{payload.get('selected_candidate_count')}`",
        f"- Seed count: `{counts.get('seed_count')}`",
        f"- Symbol requests: `{counts.get('symbol_resolution_request_count')}`",
        f"- Symbol batch jobs: `{counts.get('symbol_resolution_batch_job_count')}`",
        f"- Dispatch jobs: `{counts.get('dispatch_job_count')}`",
        f"- Transfer pack jobs: `{counts.get('transfer_pack_selected_job_count')}`",
        f"- Transfer pack check: `{payload.get('transfer_pack_check_status')}`",
        f"- Transfer pack import: `{payload.get('transfer_pack_import_status')}`",
        f"- Execution plan: `{payload.get('execution_plan_status')}`",
        f"- Execution dry-run: `{payload.get('execution_run_status')}`",
        f"- Execution dry-run check: `{payload.get('execution_run_check_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        "",
        "## Candidates",
        "",
    ]
    for candidate_id in payload.get("selected_candidate_ids") or []:
        lines.append(f"- `{candidate_id}`")
    lines.extend(
        [
            "",
            "## Frame Mix",
            "",
        ]
    )
    frame_counts = payload.get("frame_resolution_counts") or {}
    if frame_counts:
        for key, value in sorted(frame_counts.items()):
            lines.append(f"- `{key}`: `{value}`")
    else:
        lines.append("- none")
    failed = payload.get("failed_assertions") or []
    lines.extend(
        [
            "",
            "## Assertions",
            "",
            f"- Failed assertions: `{len(failed)}`",
        ]
    )
    for item in failed:
        lines.append(f"- `{item}`")
    return "\n".join(lines) + "\n"


def run_smoke(
    *,
    queue_path: Path = QUEUE_PATH,
    output_root: Path = OUTPUT_ROOT,
    output_path: Path = OUTPUT_PATH,
    markdown_path: Path = MARKDOWN_PATH,
    candidate_ids: list[str] | None = None,
    limit: int | None = None,
) -> dict[str, Any]:
    generated_utc = now_utc()
    queue_rows = autotrigger.load_jsonl(queue_path)
    selected_rows = select_queue_rows(queue_rows, candidate_ids=candidate_ids, limit=limit)
    if not selected_rows:
        raise ValueError("No queued ghidra candidates were available for the synthetic smoke run.")

    bundle, metadata = build_synthetic_bundle(selected_rows, generated_utc=generated_utc)
    smoke_input_root = output_root / "inputs"
    bundle_path = smoke_input_root / "synthetic" / "normalized-registry-bundle.json"
    write_json(bundle_path, bundle)

    manifest_path = output_root / "ghidra-autotrigger-inputs.json"
    seeds_path = output_root / "ghidra-autotrigger-seeds.jsonl"
    symbol_queue_path = output_root / "ghidra-symbol-resolution-queue.json"
    symbol_batch_path = output_root / "ghidra-symbol-resolution-batch.json"
    symbol_run_path = output_root / "ghidra-symbol-resolution-run.json"
    batch_path = output_root / "ghidra-dispatch-batch.json"
    run_path = output_root / "ghidra-dispatch-run.json"
    health_path = output_root / "ghidra-autotrigger-health.json"
    sync_path = output_root / "ghidra-autotrigger-sync.json"
    sync_markdown_path = output_root / "ghidra-autotrigger-sync.md"
    handoff_path = output_root / "ghidra-symbol-resolution-handoff.json"
    handoff_markdown_path = output_root / "ghidra-symbol-resolution-handoff.md"
    transfer_path = output_root / "ghidra-symbol-resolution-transfer.json"
    transfer_markdown_path = output_root / "ghidra-symbol-resolution-transfer.md"
    transfer_pack_output_root = output_root / "ghidra-symbol-resolution-transfer-pack"
    transfer_pack_summary_path = output_root / "ghidra-symbol-resolution-transfer-pack.json"
    transfer_pack_markdown_path = output_root / "ghidra-symbol-resolution-transfer-pack.md"
    transfer_pack_archive_path = output_root / "ghidra-symbol-resolution-transfer-pack.zip"
    transfer_pack_check_path = output_root / "ghidra-symbol-resolution-transfer-pack-check.json"
    transfer_pack_check_markdown_path = output_root / "ghidra-symbol-resolution-transfer-pack-check.md"
    transfer_pack_import_root = output_root / "ghidra-symbol-resolution-transfer-pack-import"
    transfer_pack_import_path = output_root / "ghidra-symbol-resolution-transfer-pack-import.json"
    transfer_pack_import_markdown_path = output_root / "ghidra-symbol-resolution-transfer-pack-import.md"
    execution_plan_path = output_root / "ghidra-symbol-resolution-transfer-pack-execution-plan.json"
    execution_plan_markdown_path = output_root / "ghidra-symbol-resolution-transfer-pack-execution-plan.md"
    execution_run_path = output_root / "ghidra-symbol-resolution-transfer-pack-execution-run.json"
    execution_run_markdown_path = output_root / "ghidra-symbol-resolution-transfer-pack-execution-run.md"
    execution_run_check_path = output_root / "ghidra-symbol-resolution-transfer-pack-execution-run-check.json"
    execution_run_check_markdown_path = output_root / "ghidra-symbol-resolution-transfer-pack-execution-run-check.md"

    sync_payload = sync_lane_mod.sync_lane(
        discover_input_roots=[smoke_input_root],
        queue_path=queue_path,
        bundle_manifest_path=manifest_path,
        seeds_path=seeds_path,
        symbol_queue_path=symbol_queue_path,
        symbol_batch_path=symbol_batch_path,
        symbol_run_path=symbol_run_path,
        batch_path=batch_path,
        run_path=run_path,
        health_path=health_path,
        handoff_path=handoff_path,
        handoff_markdown_path=handoff_markdown_path,
        transfer_path=transfer_path,
        transfer_markdown_path=transfer_markdown_path,
        transfer_pack_output_root=transfer_pack_output_root,
        transfer_pack_summary_path=transfer_pack_summary_path,
        transfer_pack_markdown_path=transfer_pack_markdown_path,
        transfer_pack_archive_path=transfer_pack_archive_path,
        transfer_pack_check_path=transfer_pack_check_path,
        transfer_pack_check_markdown_path=transfer_pack_check_markdown_path,
        markdown_path=sync_markdown_path,
        output_path=sync_path,
    )
    handoff_payload = load_json(handoff_path)
    transfer_payload = load_json(transfer_path)
    transfer_pack_payload = load_json(transfer_pack_summary_path)
    transfer_pack_check_payload = transfer_pack_check_mod.validate_transfer_pack(
        transfer_pack_payload,
        summary_path=transfer_pack_summary_path,
    )
    write_json(transfer_pack_check_path, transfer_pack_check_payload)
    write_text(transfer_pack_check_markdown_path, transfer_pack_check_mod.render_markdown(transfer_pack_check_payload))
    transfer_pack_import_payload = transfer_pack_import_mod.unpack_transfer_pack(
        transfer_pack_payload,
        summary_path=transfer_pack_summary_path,
        output_root=transfer_pack_import_root,
        output_path=transfer_pack_import_path,
        markdown_path=transfer_pack_import_markdown_path,
    )
    execution_plan_payload = execution_plan_mod.execution_plan_from_import(
        transfer_pack_import_payload,
        import_path=transfer_pack_import_path,
    )
    write_json(execution_plan_path, execution_plan_payload)
    write_text(execution_plan_markdown_path, execution_plan_mod.render_markdown(execution_plan_payload))
    execution_run_payload = execution_run_mod.execution_run_from_plan(
        execution_plan_payload,
        plan_path=execution_plan_path,
        execute=False,
    )
    write_json(execution_run_path, execution_run_payload)
    write_text(execution_run_markdown_path, execution_run_mod.render_markdown(execution_run_payload))
    execution_run_check_payload = execution_run_check_mod.validate_execution_run(
        execution_run_payload,
        run_path=execution_run_path,
    )
    write_json(execution_run_check_path, execution_run_check_payload)
    write_text(execution_run_check_markdown_path, execution_run_check_mod.render_markdown(execution_run_check_payload))

    refresh = sync_payload.get("refresh") or {}
    counts = {
        "manifest_selected_count": int(((sync_payload.get("bundle_manifest") or {}).get("selected_count")) or ((refresh.get("bundle_manifest_selected_count")) or 0)),
        "seed_count": int(refresh.get("seed_count") or 0),
        "symbol_resolution_request_count": int(refresh.get("symbol_resolution_request_count") or 0),
        "symbol_resolution_batch_job_count": int(refresh.get("symbol_resolution_batch_job_count") or 0),
        "symbol_resolution_run_selected_job_count": int(refresh.get("symbol_resolution_run_selected_job_count") or 0),
        "dispatch_job_count": int(refresh.get("dispatch_job_count") or 0),
        "dispatch_selected_job_count": int(refresh.get("run_plan_selected_job_count") or 0),
        "transfer_pack_selected_job_count": int(refresh.get("symbol_resolution_transfer_pack_selected_job_count") or 0),
    }
    failed_assertions = [
        name
        for name, passed in {
            "sync-status-ok": sync_payload.get("sync_status") == "ok",
            "operator-symbol-resolution-ready": (sync_payload.get("operator") or {}).get("blocker") == "symbol-resolution-ready",
            "manifest-selected-one-bundle": counts["manifest_selected_count"] == 1,
            "seed-count-matches-candidates": counts["seed_count"] == metadata["selected_candidate_count"],
            "symbol-request-count-positive": counts["symbol_resolution_request_count"] > 0,
            "symbol-batch-job-count-positive": counts["symbol_resolution_batch_job_count"] > 0,
            "transfer-pack-ready": transfer_pack_payload.get("pack_status") == "ready",
            "transfer-pack-check-ok": transfer_pack_check_payload.get("check_status") == "ok",
            "transfer-pack-import-ok": transfer_pack_import_payload.get("import_status") == "ok",
            "execution-plan-ready": execution_plan_payload.get("execution_plan_status") == "ready",
            "execution-run-ready": execution_run_payload.get("execution_run_status") == "ready",
            "execution-run-check-ok": execution_run_check_payload.get("check_status") == "ok",
        }.items()
        if not passed
    ]

    payload = {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "smoke_status": "ok" if not failed_assertions else "error",
        "queue_path": autotrigger.portable_path(queue_path),
        "output_root": autotrigger.portable_path(output_root),
        "synthetic_bundle_path": autotrigger.portable_path(bundle_path),
        "selected_candidate_count": metadata["selected_candidate_count"],
        "selected_candidate_ids": metadata["selected_candidate_ids"],
        "frame_resolution_counts": metadata["frame_resolution_counts"],
        "sync_status": sync_payload.get("sync_status"),
        "operator": sync_payload.get("operator"),
        "counts": counts,
        "failed_assertions": failed_assertions,
        "paths": {
            "manifest_path": autotrigger.portable_path(manifest_path),
            "seeds_path": autotrigger.portable_path(seeds_path),
            "symbol_queue_path": autotrigger.portable_path(symbol_queue_path),
            "symbol_batch_path": autotrigger.portable_path(symbol_batch_path),
            "symbol_run_path": autotrigger.portable_path(symbol_run_path),
            "dispatch_batch_path": autotrigger.portable_path(batch_path),
            "dispatch_run_path": autotrigger.portable_path(run_path),
            "health_path": autotrigger.portable_path(health_path),
            "sync_path": autotrigger.portable_path(sync_path),
            "sync_markdown_path": autotrigger.portable_path(sync_markdown_path),
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
        },
        "handoff_status": handoff_payload.get("handoff_status"),
        "transfer_status": transfer_payload.get("transfer_status"),
        "transfer_pack_status": transfer_pack_payload.get("pack_status"),
        "transfer_pack_check_status": transfer_pack_check_payload.get("check_status"),
        "transfer_pack_import_status": transfer_pack_import_payload.get("import_status"),
        "execution_plan_status": execution_plan_payload.get("execution_plan_status"),
        "execution_run_status": execution_run_payload.get("execution_run_status"),
        "execution_run_check_status": execution_run_check_payload.get("check_status"),
    }
    write_json(output_path, payload)
    write_text(markdown_path, render_markdown(payload))
    smoke_check_path = output_path.with_name(f"{output_path.stem}-check.json")
    smoke_check_markdown_path = markdown_path.with_name(f"{markdown_path.stem}-check.md")
    smoke_check_payload = smoke_check_mod.validate_smoke(payload, smoke_path=output_path)
    smoke_check_mod.write_json(smoke_check_path, smoke_check_payload)
    smoke_check_mod.write_text(smoke_check_markdown_path, smoke_check_mod.render_markdown(smoke_check_payload))
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a synthetic smoke test for the Ghidra autotrigger lane.")
    parser.add_argument("--queue", type=Path, default=QUEUE_PATH)
    parser.add_argument("--candidate-id", action="append", default=[])
    parser.add_argument("--limit", type=int, default=None)
    parser.add_argument("--output-root", type=Path, default=OUTPUT_ROOT)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = run_smoke(
        queue_path=args.queue,
        output_root=args.output_root,
        output_path=args.output,
        markdown_path=args.markdown_output,
        candidate_ids=args.candidate_id,
        limit=args.limit,
    )
    print(json.dumps(payload, indent=2))
    smoke_check_path = args.output.with_name(f"{args.output.stem}-check.json")
    smoke_check_payload = load_json(smoke_check_path) if smoke_check_path.exists() else {}
    return 0 if payload.get("smoke_status") == "ok" and smoke_check_payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
