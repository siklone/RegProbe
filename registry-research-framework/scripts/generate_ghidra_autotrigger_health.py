#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
INPUTS_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-inputs.json"
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-job-queue.jsonl"
SEEDS_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-seeds.jsonl"
SYMBOL_QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-queue.json"
SYMBOL_BATCH_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-batch.json"
SYMBOL_RUN_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-run.json"
HANDOFF_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-handoff.json"
TRANSFER_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer.json"
TRANSFER_PACK_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.json"
BATCH_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-batch.json"
RUN_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-run.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-health.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-health.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8"))


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    if not path.exists():
        return []
    rows: list[dict[str, Any]] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line:
            rows.append(json.loads(line))
    return rows


def health_payload(
    input_manifest: dict[str, Any],
    queue_rows: list[dict[str, Any]],
    seed_rows: list[dict[str, Any]],
    symbol_queue: dict[str, Any],
    symbol_batch: dict[str, Any],
    symbol_run: dict[str, Any],
    handoff: dict[str, Any],
    transfer: dict[str, Any],
    transfer_pack: dict[str, Any],
    batch: dict[str, Any],
    run: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    input_entries = input_manifest.get("entries") or []
    queue_candidates = [str(row.get("candidate_id") or "") for row in queue_rows]
    seed_candidates = [str(row.get("candidate_id") or "") for row in seed_rows]
    symbol_requests = symbol_queue.get("requests") or []
    symbol_resolution_request_ids = [str(row.get("request_id") or "") for row in symbol_requests]
    symbol_resolution_lookup_keys = [str(row.get("lookup_key") or "") for row in symbol_requests]
    symbol_resolution_candidate_ids = sorted(
        {
            str(candidate_id or "")
            for row in symbol_requests
            for candidate_id in (row.get("candidate_ids") or [])
            if str(candidate_id or "")
        }
    )
    symbol_batch_jobs = symbol_batch.get("jobs") or []
    symbol_batch_request_ids = [str(job.get("request_id") or "") for job in symbol_batch_jobs]
    symbol_batch_diagnostics = symbol_batch.get("diagnostics") or {}
    handoff_selected_jobs = handoff.get("selected_jobs") or []
    handoff_blocked_jobs = handoff.get("blocked_jobs") or []
    handoff_request_ids = [
        str(job.get("request_id") or "")
        for job in [*handoff_selected_jobs, *handoff_blocked_jobs]
        if str(job.get("request_id") or "")
    ]
    transfer_jobs = transfer.get("jobs") or []
    transfer_request_ids = [str(job.get("request_id") or "") for job in transfer_jobs if str(job.get("request_id") or "")]
    transfer_pack_request_ids = [
        str(request_id or "")
        for request_id in (transfer_pack.get("request_ids") or [])
        if str(request_id or "")
    ]
    autotrigger_jobs = [job for job in (batch.get("jobs") or []) if int(job.get("autotrigger_seed_count") or 0) > 0]
    missing_input_jobs = [
        {
            "candidate_id": job.get("candidate_id"),
            "missing_inputs": job.get("missing_inputs") or [],
        }
        for job in (batch.get("jobs") or [])
        if job.get("missing_inputs")
    ]
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "inputs_path": portable_path(INPUTS_PATH),
        "queue_path": portable_path(QUEUE_PATH),
        "seeds_path": portable_path(SEEDS_PATH),
        "symbol_resolution_path": portable_path(SYMBOL_QUEUE_PATH),
        "symbol_batch_path": portable_path(SYMBOL_BATCH_PATH),
        "symbol_run_path": portable_path(SYMBOL_RUN_PATH),
        "symbol_handoff_path": portable_path(HANDOFF_PATH),
        "symbol_transfer_path": portable_path(TRANSFER_PATH),
        "symbol_transfer_pack_path": portable_path(TRANSFER_PACK_PATH),
        "batch_path": portable_path(BATCH_PATH),
        "run_path": portable_path(RUN_PATH),
        "counts": {
            "input_manifest_selected": len(input_entries),
            "queue_jobs": len(queue_rows),
            "autotrigger_seeds": len(seed_rows),
            "symbol_resolution_requests": len(symbol_requests),
            "symbol_resolution_batch_jobs": int(symbol_batch.get("job_count") or 0),
            "symbol_resolution_runnable_jobs": int(symbol_batch.get("runnable_job_count") or 0),
            "symbol_resolution_blocked_jobs": int(symbol_batch.get("blocked_job_count") or 0),
            "symbol_resolution_run_selected_jobs": int(symbol_run.get("selected_job_count") or 0),
            "symbol_resolution_run_blocked_jobs": int(symbol_run.get("blocked_job_count") or 0),
            "symbol_resolution_handoff_selected_jobs": int((handoff.get("counts") or {}).get("selected_jobs") or 0),
            "symbol_resolution_transfer_selected_jobs": int((transfer.get("counts") or {}).get("selected_jobs") or 0),
            "symbol_resolution_transfer_pack_selected_jobs": int((transfer_pack.get("counts") or {}).get("selected_jobs") or 0),
            "dispatch_jobs": int(batch.get("job_count") or 0),
            "autotrigger_dispatch_jobs": len(autotrigger_jobs),
            "run_selected_jobs": int(run.get("selected_job_count") or 0),
            "run_blocked_jobs": int(run.get("blocked_job_count") or 0),
        },
        "symbol_resolution_runner": {
            "available": bool(symbol_run.get("runner_available")),
            "mode": symbol_run.get("mode"),
            "error": symbol_run.get("error"),
        },
        "symbol_resolution_batch": {
            "missing_host_tools": symbol_batch.get("missing_host_tools") or [],
            "missing_input_counts": symbol_batch_diagnostics.get("missing_input_counts") or {},
            "resolution_kind_counts": symbol_batch_diagnostics.get("resolution_kind_counts") or {},
            "blocked_examples": symbol_batch_diagnostics.get("blocked_examples") or [],
        },
        "symbol_resolution_handoff": {
            "status": handoff.get("handoff_status"),
            "operator_blocker": (handoff.get("operator") or {}).get("blocker"),
            "selected_jobs": int((handoff.get("counts") or {}).get("selected_jobs") or 0),
            "blocked_jobs": int((handoff.get("counts") or {}).get("blocked_jobs") or 0),
        },
        "symbol_resolution_transfer": {
            "status": transfer.get("transfer_status"),
            "operator_blocker": (transfer.get("operator") or {}).get("blocker"),
            "selected_jobs": int((transfer.get("counts") or {}).get("selected_jobs") or 0),
            "repo_file_count": int((transfer.get("counts") or {}).get("repo_file_count") or 0),
            "missing_repo_file_count": int((transfer.get("counts") or {}).get("missing_repo_file_count") or 0),
        },
        "symbol_resolution_transfer_pack": {
            "status": transfer_pack.get("pack_status"),
            "operator_blocker": (transfer_pack.get("operator") or {}).get("blocker"),
            "selected_jobs": int((transfer_pack.get("counts") or {}).get("selected_jobs") or 0),
            "repo_files_copied": int((transfer_pack.get("counts") or {}).get("repo_files_copied") or 0),
            "command_files_written": int((transfer_pack.get("counts") or {}).get("command_files_written") or 0),
            "archive_path": transfer_pack.get("archive_path"),
        },
        "runner": {
            "available": bool(run.get("runner_available")),
            "mode": run.get("mode"),
            "error": run.get("error"),
        },
        "coverage": {
            "input_bundle_paths": [str(entry.get("path") or "") for entry in input_entries],
            "queued_candidate_ids": queue_candidates,
            "seed_candidate_ids": seed_candidates,
            "symbol_resolution_request_ids": symbol_resolution_request_ids,
            "symbol_resolution_lookup_keys": symbol_resolution_lookup_keys,
            "symbol_resolution_candidate_ids": symbol_resolution_candidate_ids,
            "symbol_resolution_batch_request_ids": symbol_batch_request_ids,
            "symbol_resolution_handoff_request_ids": handoff_request_ids,
            "symbol_resolution_transfer_request_ids": transfer_request_ids,
            "symbol_resolution_transfer_pack_request_ids": transfer_pack_request_ids,
            "autotrigger_dispatch_candidate_ids": [str(job.get("candidate_id") or "") for job in autotrigger_jobs],
        },
        "focus": {
            "top_input_bundle": str(input_entries[0].get("path") or "") if input_entries else None,
            "top_queue_candidate": queue_candidates[0] if queue_candidates else None,
            "top_autotrigger_candidate": str(autotrigger_jobs[0].get("candidate_id") or "") if autotrigger_jobs else None,
            "top_symbol_resolution_request": symbol_resolution_lookup_keys[0] if symbol_resolution_lookup_keys else None,
            "top_symbol_resolution_batch_request": symbol_batch_request_ids[0] if symbol_batch_request_ids else None,
            "top_symbol_resolution_handoff_request": handoff_request_ids[0] if handoff_request_ids else None,
            "top_symbol_resolution_transfer_request": transfer_request_ids[0] if transfer_request_ids else None,
            "top_symbol_resolution_transfer_pack_request": transfer_pack_request_ids[0] if transfer_pack_request_ids else None,
            "missing_input_jobs": missing_input_jobs,
        },
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    runner = payload.get("runner") or {}
    coverage = payload.get("coverage") or {}
    focus = payload.get("focus") or {}
    lines = [
        "# Ghidra Autotrigger Health",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Input bundles selected: `{counts.get('input_manifest_selected', 0)}`",
        f"- Queue jobs: `{counts.get('queue_jobs', 0)}`",
        f"- Autotrigger seeds: `{counts.get('autotrigger_seeds', 0)}`",
        f"- Symbol resolution requests: `{counts.get('symbol_resolution_requests', 0)}`",
        f"- Symbol resolution batch jobs: `{counts.get('symbol_resolution_batch_jobs', 0)}`",
        f"- Symbol resolution blocked jobs: `{counts.get('symbol_resolution_blocked_jobs', 0)}`",
        f"- Symbol resolution run selected jobs: `{counts.get('symbol_resolution_run_selected_jobs', 0)}`",
        f"- Symbol resolution handoff selected jobs: `{counts.get('symbol_resolution_handoff_selected_jobs', 0)}`",
        f"- Symbol resolution transfer selected jobs: `{counts.get('symbol_resolution_transfer_selected_jobs', 0)}`",
        f"- Symbol resolution transfer pack selected jobs: `{counts.get('symbol_resolution_transfer_pack_selected_jobs', 0)}`",
        f"- Dispatch jobs: `{counts.get('dispatch_jobs', 0)}`",
        f"- Autotrigger dispatch jobs: `{counts.get('autotrigger_dispatch_jobs', 0)}`",
        f"- Run selected jobs: `{counts.get('run_selected_jobs', 0)}`",
        f"- Symbol runner available: `{(payload.get('symbol_resolution_runner') or {}).get('available')}`",
        f"- Runner available: `{runner.get('available')}`",
        f"- Runner mode: `{runner.get('mode')}`",
        "",
        "## Focus",
        "",
        f"- Top input bundle: `{focus.get('top_input_bundle')}`",
        f"- Top queue candidate: `{focus.get('top_queue_candidate')}`",
        f"- Top autotrigger candidate: `{focus.get('top_autotrigger_candidate')}`",
        f"- Top symbol resolution request: `{focus.get('top_symbol_resolution_request')}`",
        f"- Top symbol resolution batch request: `{focus.get('top_symbol_resolution_batch_request')}`",
        f"- Top symbol resolution handoff request: `{focus.get('top_symbol_resolution_handoff_request')}`",
        f"- Top symbol resolution transfer request: `{focus.get('top_symbol_resolution_transfer_request')}`",
        f"- Top symbol resolution transfer pack request: `{focus.get('top_symbol_resolution_transfer_pack_request')}`",
        "",
        "## Coverage",
        "",
        f"- Input bundle paths: `{len(coverage.get('input_bundle_paths') or [])}`",
        f"- Queued candidate ids: `{len(coverage.get('queued_candidate_ids') or [])}`",
        f"- Seed candidate ids: `{len(coverage.get('seed_candidate_ids') or [])}`",
        f"- Symbol resolution requests: `{len(coverage.get('symbol_resolution_request_ids') or [])}`",
        f"- Symbol resolution batch request ids: `{len(coverage.get('symbol_resolution_batch_request_ids') or [])}`",
        f"- Symbol resolution handoff request ids: `{len(coverage.get('symbol_resolution_handoff_request_ids') or [])}`",
        f"- Symbol resolution transfer request ids: `{len(coverage.get('symbol_resolution_transfer_request_ids') or [])}`",
        f"- Symbol resolution transfer pack request ids: `{len(coverage.get('symbol_resolution_transfer_pack_request_ids') or [])}`",
        f"- Autotrigger dispatch candidate ids: `{len(coverage.get('autotrigger_dispatch_candidate_ids') or [])}`",
        "",
        "## Symbol Handoff",
        "",
        f"- Handoff status: `{(payload.get('symbol_resolution_handoff') or {}).get('status')}`",
        f"- Operator blocker: `{(payload.get('symbol_resolution_handoff') or {}).get('operator_blocker')}`",
        f"- Selected jobs: `{(payload.get('symbol_resolution_handoff') or {}).get('selected_jobs')}`",
        f"- Blocked jobs: `{(payload.get('symbol_resolution_handoff') or {}).get('blocked_jobs')}`",
        "",
        "## Symbol Transfer",
        "",
        f"- Transfer status: `{(payload.get('symbol_resolution_transfer') or {}).get('status')}`",
        f"- Operator blocker: `{(payload.get('symbol_resolution_transfer') or {}).get('operator_blocker')}`",
        f"- Selected jobs: `{(payload.get('symbol_resolution_transfer') or {}).get('selected_jobs')}`",
        f"- Missing repo files: `{(payload.get('symbol_resolution_transfer') or {}).get('missing_repo_file_count')}`",
        "",
        "## Transfer Pack",
        "",
        f"- Pack status: `{(payload.get('symbol_resolution_transfer_pack') or {}).get('status')}`",
        f"- Operator blocker: `{(payload.get('symbol_resolution_transfer_pack') or {}).get('operator_blocker')}`",
        f"- Selected jobs: `{(payload.get('symbol_resolution_transfer_pack') or {}).get('selected_jobs')}`",
        f"- Repo files copied: `{(payload.get('symbol_resolution_transfer_pack') or {}).get('repo_files_copied')}`",
        f"- Command files written: `{(payload.get('symbol_resolution_transfer_pack') or {}).get('command_files_written')}`",
        "",
        "## Symbol Batch Diagnostics",
        "",
        f"- Missing host tools: `{', '.join((payload.get('symbol_resolution_batch') or {}).get('missing_host_tools') or []) or 'none'}`",
        f"- Missing input counts: `{json.dumps((payload.get('symbol_resolution_batch') or {}).get('missing_input_counts') or {}, sort_keys=True)}`",
        f"- Resolution kind counts: `{json.dumps((payload.get('symbol_resolution_batch') or {}).get('resolution_kind_counts') or {}, sort_keys=True)}`",
    ]
    return "\n".join(lines) + "\n"


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def main() -> int:
    input_manifest = load_json(INPUTS_PATH)
    queue_rows = load_jsonl(QUEUE_PATH)
    seed_rows = load_jsonl(SEEDS_PATH)
    symbol_queue = load_json(SYMBOL_QUEUE_PATH)
    symbol_batch = load_json(SYMBOL_BATCH_PATH)
    symbol_run = load_json(SYMBOL_RUN_PATH)
    handoff = load_json(HANDOFF_PATH)
    transfer = load_json(TRANSFER_PATH)
    transfer_pack = load_json(TRANSFER_PACK_PATH)
    batch = load_json(BATCH_PATH)
    run = load_json(RUN_PATH)
    payload = health_payload(
        input_manifest,
        queue_rows,
        seed_rows,
        symbol_queue,
        symbol_batch,
        symbol_run,
        handoff,
        transfer,
        transfer_pack,
        batch,
        run,
    )
    write_json(OUTPUT_PATH, payload)
    write_text(MARKDOWN_PATH, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(OUTPUT_PATH),
                "queue_jobs": len(queue_rows),
                "autotrigger_seeds": len(seed_rows),
                "symbol_resolution_requests": int((symbol_queue or {}).get("request_count") or 0),
                "symbol_resolution_batch_jobs": int((symbol_batch or {}).get("job_count") or 0),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
