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
    symbol_resolution_batch_jobs = int(counts.get("symbol_resolution_batch_jobs") or 0)
    symbol_resolution_runnable_jobs = int(counts.get("symbol_resolution_runnable_jobs") or 0)
    symbol_resolution_blocked_jobs = int(counts.get("symbol_resolution_blocked_jobs") or 0)
    symbol_resolution_run_selected_jobs = int(counts.get("symbol_resolution_run_selected_jobs") or 0)
    symbol_resolution_run_blocked_jobs = int(counts.get("symbol_resolution_run_blocked_jobs") or 0)
    symbol_resolution_run_completed_jobs = int(counts.get("symbol_resolution_run_completed_jobs") or 0)
    symbol_resolution_handoff_selected_jobs = int(counts.get("symbol_resolution_handoff_selected_jobs") or 0)
    symbol_resolution_transfer_selected_jobs = int(counts.get("symbol_resolution_transfer_selected_jobs") or 0)
    symbol_resolution_transfer_pack_selected_jobs = int(counts.get("symbol_resolution_transfer_pack_selected_jobs") or 0)
    symbol_resolution_transfer_pack_check_errors = int(counts.get("symbol_resolution_transfer_pack_check_errors") or 0)
    symbol_resolution_execution_run_planned_jobs = int(counts.get("symbol_resolution_execution_run_planned_jobs") or 0)
    symbol_resolution_execution_run_ready_jobs = int(counts.get("symbol_resolution_execution_run_ready_jobs") or 0)
    symbol_resolution_execution_run_blocked_jobs = int(counts.get("symbol_resolution_execution_run_blocked_jobs") or 0)
    symbol_resolution_execution_run_check_errors = int(counts.get("symbol_resolution_execution_run_check_errors") or 0)
    etw_stackwalk_plan_errors = int(counts.get("etw_stackwalk_plan_errors") or 0)
    etw_stackwalk_plan_check_errors = int(counts.get("etw_stackwalk_plan_check_errors") or 0)
    dispatch_jobs = int(counts.get("dispatch_jobs") or 0)
    autotrigger_dispatch_jobs = int(counts.get("autotrigger_dispatch_jobs") or 0)
    run_selected_jobs = int(counts.get("run_selected_jobs") or 0)
    run_blocked_jobs = int(counts.get("run_blocked_jobs") or 0)

    input_bundle_paths = coverage.get("input_bundle_paths") or []
    queued_candidate_ids = coverage.get("queued_candidate_ids") or []
    seed_candidate_ids = coverage.get("seed_candidate_ids") or []
    symbol_resolution_request_ids = coverage.get("symbol_resolution_request_ids") or []
    symbol_resolution_lookup_keys = coverage.get("symbol_resolution_lookup_keys") or []
    symbol_resolution_batch_request_ids = coverage.get("symbol_resolution_batch_request_ids") or []
    symbol_resolution_handoff_request_ids = coverage.get("symbol_resolution_handoff_request_ids") or []
    symbol_resolution_transfer_request_ids = coverage.get("symbol_resolution_transfer_request_ids") or []
    symbol_resolution_transfer_pack_request_ids = coverage.get("symbol_resolution_transfer_pack_request_ids") or []
    symbol_resolution_execution_run_request_ids = coverage.get("symbol_resolution_execution_run_request_ids") or []
    autotrigger_dispatch_candidate_ids = coverage.get("autotrigger_dispatch_candidate_ids") or []
    missing_input_jobs = focus.get("missing_input_jobs") or []
    symbol_resolution_runner = payload.get("symbol_resolution_runner") or {}
    symbol_resolution_handoff = payload.get("symbol_resolution_handoff") or {}
    symbol_resolution_transfer = payload.get("symbol_resolution_transfer") or {}
    symbol_resolution_transfer_pack = payload.get("symbol_resolution_transfer_pack") or {}
    symbol_resolution_transfer_pack_check = payload.get("symbol_resolution_transfer_pack_check") or {}
    symbol_resolution_execution_run = payload.get("symbol_resolution_execution_run") or {}
    symbol_resolution_execution_run_check = payload.get("symbol_resolution_execution_run_check") or {}
    etw_stackwalk_capture = payload.get("etw_stackwalk_capture") or {}

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
    if len(symbol_resolution_batch_request_ids) != symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_batch_jobs mismatch: "
            f"counts={symbol_resolution_batch_jobs} coverage={len(symbol_resolution_batch_request_ids)}"
        )
    if symbol_resolution_handoff_selected_jobs > len(symbol_resolution_handoff_request_ids):
        errors.append(
            "symbol_resolution_handoff_selected_jobs exceeds handoff coverage: "
            f"{symbol_resolution_handoff_selected_jobs}>{len(symbol_resolution_handoff_request_ids)}"
        )
    if symbol_resolution_handoff_selected_jobs > symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_handoff_selected_jobs exceeds symbol_resolution_batch_jobs: "
            f"{symbol_resolution_handoff_selected_jobs}>{symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_transfer_selected_jobs > len(symbol_resolution_transfer_request_ids):
        errors.append(
            "symbol_resolution_transfer_selected_jobs exceeds transfer coverage: "
            f"{symbol_resolution_transfer_selected_jobs}>{len(symbol_resolution_transfer_request_ids)}"
        )
    if symbol_resolution_transfer_selected_jobs > symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_transfer_selected_jobs exceeds symbol_resolution_batch_jobs: "
            f"{symbol_resolution_transfer_selected_jobs}>{symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_transfer_pack_selected_jobs > len(symbol_resolution_transfer_pack_request_ids):
        errors.append(
            "symbol_resolution_transfer_pack_selected_jobs exceeds transfer pack coverage: "
            f"{symbol_resolution_transfer_pack_selected_jobs}>{len(symbol_resolution_transfer_pack_request_ids)}"
        )
    if symbol_resolution_transfer_pack_selected_jobs > symbol_resolution_transfer_selected_jobs:
        errors.append(
            "symbol_resolution_transfer_pack_selected_jobs exceeds symbol_resolution_transfer_selected_jobs: "
            f"{symbol_resolution_transfer_pack_selected_jobs}>{symbol_resolution_transfer_selected_jobs}"
        )
    if symbol_resolution_transfer_pack_selected_jobs > 0 and symbol_resolution_transfer_pack_check.get("status") != "ok":
        errors.append("symbol_resolution_transfer_pack_check must be ok when transfer pack has selected jobs")
    if int(symbol_resolution_transfer_pack_check.get("error_count") or 0) != symbol_resolution_transfer_pack_check_errors:
        errors.append("symbol_resolution_transfer_pack_check error_count does not match counts")
    if symbol_resolution_transfer_pack_check_errors > 0 and symbol_resolution_transfer_pack_check.get("status") == "ok":
        errors.append("symbol_resolution_transfer_pack_check cannot be ok when errors are present")
    if symbol_resolution_execution_run_planned_jobs > len(symbol_resolution_execution_run_request_ids):
        errors.append(
            "symbol_resolution_execution_run_planned_jobs exceeds execution run coverage: "
            f"{symbol_resolution_execution_run_planned_jobs}>{len(symbol_resolution_execution_run_request_ids)}"
        )
    if symbol_resolution_execution_run_planned_jobs > symbol_resolution_transfer_pack_selected_jobs:
        errors.append(
            "symbol_resolution_execution_run_planned_jobs exceeds symbol_resolution_transfer_pack_selected_jobs: "
            f"{symbol_resolution_execution_run_planned_jobs}>{symbol_resolution_transfer_pack_selected_jobs}"
        )
    if symbol_resolution_execution_run_ready_jobs + symbol_resolution_execution_run_blocked_jobs != symbol_resolution_execution_run_planned_jobs:
        errors.append(
            "symbol_resolution execution ready+blocked does not equal planned jobs: "
            f"{symbol_resolution_execution_run_ready_jobs}+{symbol_resolution_execution_run_blocked_jobs}!={symbol_resolution_execution_run_planned_jobs}"
        )
    if symbol_resolution_transfer_pack_selected_jobs > 0 and symbol_resolution_execution_run.get("status") != "ready":
        errors.append("symbol_resolution_execution_run must be ready when transfer pack has selected jobs")
    if symbol_resolution_execution_run_ready_jobs > 0 and symbol_resolution_execution_run_check.get("status") != "ok":
        errors.append("symbol_resolution_execution_run_check must be ok when execution run has ready jobs")
    if int(symbol_resolution_execution_run_check.get("error_count") or 0) != symbol_resolution_execution_run_check_errors:
        errors.append("symbol_resolution_execution_run_check error_count does not match counts")
    if symbol_resolution_execution_run_check_errors > 0 and symbol_resolution_execution_run_check.get("status") == "ok":
        errors.append("symbol_resolution_execution_run_check cannot be ok when errors are present")
    etw_stackwalk_active = bool(
        etw_stackwalk_capture.get("plan_status")
        or etw_stackwalk_capture.get("check_status")
        or etw_stackwalk_capture.get("plan_errors")
        or etw_stackwalk_capture.get("check_errors")
    )
    if etw_stackwalk_active:
        if len(etw_stackwalk_capture.get("plan_errors") or []) != etw_stackwalk_plan_errors:
            errors.append("etw_stackwalk plan error_count does not match counts")
        if len(etw_stackwalk_capture.get("check_errors") or []) != etw_stackwalk_plan_check_errors:
            errors.append("etw_stackwalk check error_count does not match counts")
        if etw_stackwalk_plan_errors > 0 and etw_stackwalk_capture.get("plan_status") == "ready":
            errors.append("etw_stackwalk plan cannot be ready when errors are present")
        if etw_stackwalk_plan_check_errors > 0 and etw_stackwalk_capture.get("check_status") == "ok":
            errors.append("etw_stackwalk check cannot be ok when errors are present")
        if etw_stackwalk_capture.get("plan_status") == "ready" and etw_stackwalk_capture.get("check_status") != "ok":
            errors.append("etw_stackwalk check must be ok when plan is ready")
        if etw_stackwalk_capture.get("stack_expected") is not True:
            errors.append("etw_stackwalk stack_expected must be true")
        if int(etw_stackwalk_capture.get("stackwalk_event_count") or 0) <= 0:
            errors.append("etw_stackwalk stackwalk_event_count must be positive")
    if len(autotrigger_dispatch_candidate_ids) != autotrigger_dispatch_jobs:
        errors.append(
            "autotrigger_dispatch_jobs mismatch: "
            f"counts={autotrigger_dispatch_jobs} coverage={len(autotrigger_dispatch_candidate_ids)}"
        )
    if symbol_resolution_runnable_jobs > symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_runnable_jobs exceeds symbol_resolution_batch_jobs: "
            f"{symbol_resolution_runnable_jobs}>{symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_blocked_jobs > symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_blocked_jobs exceeds symbol_resolution_batch_jobs: "
            f"{symbol_resolution_blocked_jobs}>{symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_runnable_jobs + symbol_resolution_blocked_jobs != symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution runnable+blocked does not equal batch jobs: "
            f"{symbol_resolution_runnable_jobs}+{symbol_resolution_blocked_jobs}!={symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_run_selected_jobs > symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_run_selected_jobs exceeds symbol_resolution_batch_jobs: "
            f"{symbol_resolution_run_selected_jobs}>{symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_run_blocked_jobs > symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_run_blocked_jobs exceeds symbol_resolution_batch_jobs: "
            f"{symbol_resolution_run_blocked_jobs}>{symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_run_completed_jobs > symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution_run_completed_jobs exceeds symbol_resolution_batch_jobs: "
            f"{symbol_resolution_run_completed_jobs}>{symbol_resolution_batch_jobs}"
        )
    if (
        symbol_resolution_run_completed_jobs == 0
        and symbol_resolution_run_selected_jobs + symbol_resolution_run_blocked_jobs < symbol_resolution_batch_jobs
    ):
        errors.append(
            "symbol_resolution selected+blocked does not cover batch jobs: "
            f"{symbol_resolution_run_selected_jobs}+{symbol_resolution_run_blocked_jobs}<{symbol_resolution_batch_jobs}"
        )
    if symbol_resolution_run_selected_jobs + symbol_resolution_run_blocked_jobs + symbol_resolution_run_completed_jobs < symbol_resolution_batch_jobs:
        errors.append(
            "symbol_resolution selected+blocked+completed does not cover batch jobs: "
            f"{symbol_resolution_run_selected_jobs}+{symbol_resolution_run_blocked_jobs}+{symbol_resolution_run_completed_jobs}<{symbol_resolution_batch_jobs}"
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

    top_symbol_resolution_batch_request = focus.get("top_symbol_resolution_batch_request")
    if symbol_resolution_batch_jobs == 0 and top_symbol_resolution_batch_request is not None:
        errors.append("top_symbol_resolution_batch_request should be null when no symbol resolution batch jobs exist")
    if symbol_resolution_batch_jobs > 0 and symbol_resolution_batch_request_ids and top_symbol_resolution_batch_request != symbol_resolution_batch_request_ids[0]:
        errors.append("top_symbol_resolution_batch_request does not match first symbol resolution batch request id")
    if symbol_resolution_batch_jobs > 0 and not symbol_resolution_batch_request_ids and top_symbol_resolution_batch_request is not None:
        errors.append("top_symbol_resolution_batch_request does not match first symbol resolution batch request id")

    top_symbol_resolution_handoff_request = focus.get("top_symbol_resolution_handoff_request")
    if not symbol_resolution_handoff_request_ids and top_symbol_resolution_handoff_request is not None:
        errors.append("top_symbol_resolution_handoff_request should be null when no symbol handoff requests exist")
    if symbol_resolution_handoff_request_ids and top_symbol_resolution_handoff_request != symbol_resolution_handoff_request_ids[0]:
        errors.append("top_symbol_resolution_handoff_request does not match first symbol handoff request id")

    top_symbol_resolution_transfer_request = focus.get("top_symbol_resolution_transfer_request")
    if not symbol_resolution_transfer_request_ids and top_symbol_resolution_transfer_request is not None:
        errors.append("top_symbol_resolution_transfer_request should be null when no symbol transfer requests exist")
    if symbol_resolution_transfer_request_ids and top_symbol_resolution_transfer_request != symbol_resolution_transfer_request_ids[0]:
        errors.append("top_symbol_resolution_transfer_request does not match first symbol transfer request id")

    top_symbol_resolution_transfer_pack_request = focus.get("top_symbol_resolution_transfer_pack_request")
    if not symbol_resolution_transfer_pack_request_ids and top_symbol_resolution_transfer_pack_request is not None:
        errors.append("top_symbol_resolution_transfer_pack_request should be null when no symbol transfer pack requests exist")
    if (
        symbol_resolution_transfer_pack_request_ids
        and top_symbol_resolution_transfer_pack_request != symbol_resolution_transfer_pack_request_ids[0]
    ):
        errors.append("top_symbol_resolution_transfer_pack_request does not match first symbol transfer pack request id")

    top_symbol_resolution_execution_run_request = focus.get("top_symbol_resolution_execution_run_request")
    if not symbol_resolution_execution_run_request_ids and top_symbol_resolution_execution_run_request is not None:
        errors.append("top_symbol_resolution_execution_run_request should be null when no execution run requests exist")
    if symbol_resolution_execution_run_request_ids and top_symbol_resolution_execution_run_request != symbol_resolution_execution_run_request_ids[0]:
        errors.append("top_symbol_resolution_execution_run_request does not match first symbol execution run request id")

    if symbol_resolution_runner.get("available") and symbol_resolution_runner.get("error"):
        errors.append("symbol_resolution_runner cannot be available and errored at the same time")
    if symbol_resolution_handoff.get("selected_jobs") != symbol_resolution_handoff_selected_jobs:
        errors.append("symbol_resolution_handoff selected_jobs does not match counts")
    if symbol_resolution_transfer.get("selected_jobs") != symbol_resolution_transfer_selected_jobs:
        errors.append("symbol_resolution_transfer selected_jobs does not match counts")
    if symbol_resolution_transfer_pack.get("selected_jobs") != symbol_resolution_transfer_pack_selected_jobs:
        errors.append("symbol_resolution_transfer_pack selected_jobs does not match counts")
    if symbol_resolution_execution_run:
        if symbol_resolution_execution_run.get("planned_jobs") != symbol_resolution_execution_run_planned_jobs:
            errors.append("symbol_resolution_execution_run planned_jobs does not match counts")
        if symbol_resolution_execution_run.get("ready_jobs") != symbol_resolution_execution_run_ready_jobs:
            errors.append("symbol_resolution_execution_run ready_jobs does not match counts")
        if symbol_resolution_execution_run.get("blocked_jobs") != symbol_resolution_execution_run_blocked_jobs:
            errors.append("symbol_resolution_execution_run blocked_jobs does not match counts")
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
