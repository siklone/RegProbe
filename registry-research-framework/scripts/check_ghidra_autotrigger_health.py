#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
HEALTH_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-health.json"


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def validate_health(payload: dict[str, Any]) -> list[str]:
    errors: list[str] = []

    counts = payload.get("counts") or {}
    coverage = payload.get("coverage") or {}
    focus = payload.get("focus") or {}
    runner = payload.get("runner") or {}

    input_manifest_selected = int(counts.get("input_manifest_selected") or 0)
    queue_jobs = int(counts.get("queue_jobs") or 0)
    seeds = int(counts.get("autotrigger_seeds") or 0)
    symbol_resolution_requests = int(counts.get("symbol_resolution_requests") or 0)
    dispatch_jobs = int(counts.get("dispatch_jobs") or 0)
    autotrigger_dispatch_jobs = int(counts.get("autotrigger_dispatch_jobs") or 0)
    run_selected_jobs = int(counts.get("run_selected_jobs") or 0)
    run_blocked_jobs = int(counts.get("run_blocked_jobs") or 0)

    input_bundle_paths = coverage.get("input_bundle_paths") or []
    queued_candidate_ids = coverage.get("queued_candidate_ids") or []
    seed_candidate_ids = coverage.get("seed_candidate_ids") or []
    symbol_resolution_request_ids = coverage.get("symbol_resolution_request_ids") or []
    symbol_resolution_lookup_keys = coverage.get("symbol_resolution_lookup_keys") or []
    autotrigger_dispatch_candidate_ids = coverage.get("autotrigger_dispatch_candidate_ids") or []
    missing_input_jobs = focus.get("missing_input_jobs") or []

    if len(input_bundle_paths) != input_manifest_selected:
        errors.append(f"input_manifest_selected mismatch: counts={input_manifest_selected} coverage={len(input_bundle_paths)}")
    if len(queued_candidate_ids) != queue_jobs:
        errors.append(f"queue_jobs mismatch: counts={queue_jobs} coverage={len(queued_candidate_ids)}")
    if len(seed_candidate_ids) != seeds:
        errors.append(f"autotrigger_seeds mismatch: counts={seeds} coverage={len(seed_candidate_ids)}")
    if len(symbol_resolution_request_ids) != symbol_resolution_requests:
        errors.append(
            "symbol_resolution_requests mismatch: "
            f"counts={symbol_resolution_requests} coverage={len(symbol_resolution_request_ids)}"
        )
    if len(symbol_resolution_lookup_keys) != symbol_resolution_requests:
        errors.append(
            "symbol_resolution_lookup_keys mismatch: "
            f"counts={symbol_resolution_requests} coverage={len(symbol_resolution_lookup_keys)}"
        )
    if len(autotrigger_dispatch_candidate_ids) != autotrigger_dispatch_jobs:
        errors.append(
            "autotrigger_dispatch_jobs mismatch: "
            f"counts={autotrigger_dispatch_jobs} coverage={len(autotrigger_dispatch_candidate_ids)}"
        )
    if autotrigger_dispatch_jobs > dispatch_jobs:
        errors.append(f"autotrigger_dispatch_jobs exceeds dispatch_jobs: {autotrigger_dispatch_jobs}>{dispatch_jobs}")
    if run_selected_jobs > dispatch_jobs:
        errors.append(f"run_selected_jobs exceeds dispatch_jobs: {run_selected_jobs}>{dispatch_jobs}")
    if run_blocked_jobs > dispatch_jobs:
        errors.append(f"run_blocked_jobs exceeds dispatch_jobs: {run_blocked_jobs}>{dispatch_jobs}")
    if run_selected_jobs + run_blocked_jobs < dispatch_jobs:
        errors.append(
            f"selected+blocked does not cover dispatch jobs: {run_selected_jobs}+{run_blocked_jobs}<{dispatch_jobs}"
        )

    top_input_bundle = focus.get("top_input_bundle")
    if input_manifest_selected == 0 and top_input_bundle is not None:
        errors.append("top_input_bundle should be null when input manifest is empty")
    if input_manifest_selected > 0 and not input_bundle_paths and top_input_bundle is not None:
        errors.append("top_input_bundle does not match first discovered input bundle")
    if input_manifest_selected > 0 and input_bundle_paths and top_input_bundle != input_bundle_paths[0]:
        errors.append("top_input_bundle does not match first discovered input bundle")

    top_queue_candidate = focus.get("top_queue_candidate")
    if queue_jobs == 0 and top_queue_candidate is not None:
        errors.append("top_queue_candidate should be null when queue is empty")
    if queue_jobs > 0 and top_queue_candidate != queued_candidate_ids[0]:
        errors.append("top_queue_candidate does not match first queued candidate")

    top_autotrigger_candidate = focus.get("top_autotrigger_candidate")
    if autotrigger_dispatch_jobs == 0 and top_autotrigger_candidate is not None:
        errors.append("top_autotrigger_candidate should be null when no autotrigger dispatch jobs exist")
    if autotrigger_dispatch_jobs > 0 and top_autotrigger_candidate != autotrigger_dispatch_candidate_ids[0]:
        errors.append("top_autotrigger_candidate does not match first autotrigger dispatch candidate")

    top_symbol_resolution_request = focus.get("top_symbol_resolution_request")
    if symbol_resolution_requests == 0 and top_symbol_resolution_request is not None:
        errors.append("top_symbol_resolution_request should be null when no symbol resolution requests exist")
    if symbol_resolution_requests > 0 and not symbol_resolution_lookup_keys and top_symbol_resolution_request is not None:
        errors.append("top_symbol_resolution_request does not match first symbol resolution lookup key")
    if symbol_resolution_requests > 0 and symbol_resolution_lookup_keys and top_symbol_resolution_request != symbol_resolution_lookup_keys[0]:
        errors.append("top_symbol_resolution_request does not match first symbol resolution lookup key")

    if runner.get("available") and runner.get("error"):
        errors.append("runner cannot be available and errored at the same time")

    seen_missing_candidates: set[str] = set()
    for item in missing_input_jobs:
        candidate_id = str(item.get("candidate_id") or "")
        if not candidate_id:
            errors.append("missing_input_jobs entry is missing candidate_id")
            continue
        if candidate_id in seen_missing_candidates:
            errors.append(f"duplicate missing_input_jobs candidate: {candidate_id}")
        seen_missing_candidates.add(candidate_id)
        if not (item.get("missing_inputs") or []):
            errors.append(f"missing_input_jobs entry has no missing_inputs: {candidate_id}")

    return errors


def main() -> int:
    payload = load_json(HEALTH_PATH)
    errors = validate_health(payload)
    if errors:
        print(json.dumps({"status": "error", "errors": errors}, indent=2))
        return 1
    print(json.dumps({"status": "ok", "path": str(HEALTH_PATH.relative_to(REPO_ROOT))}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
