#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-job-queue.jsonl"
SEEDS_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-seeds.jsonl"
BATCH_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-batch.json"
RUN_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-run.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-health.json"


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
    queue_rows: list[dict[str, Any]],
    seed_rows: list[dict[str, Any]],
    batch: dict[str, Any],
    run: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    queue_candidates = [str(row.get("candidate_id") or "") for row in queue_rows]
    seed_candidates = [str(row.get("candidate_id") or "") for row in seed_rows]
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
        "queue_path": portable_path(QUEUE_PATH),
        "seeds_path": portable_path(SEEDS_PATH),
        "batch_path": portable_path(BATCH_PATH),
        "run_path": portable_path(RUN_PATH),
        "counts": {
            "queue_jobs": len(queue_rows),
            "autotrigger_seeds": len(seed_rows),
            "dispatch_jobs": int(batch.get("job_count") or 0),
            "autotrigger_dispatch_jobs": len(autotrigger_jobs),
            "run_selected_jobs": int(run.get("selected_job_count") or 0),
            "run_blocked_jobs": int(run.get("blocked_job_count") or 0),
        },
        "runner": {
            "available": bool(run.get("runner_available")),
            "mode": run.get("mode"),
            "error": run.get("error"),
        },
        "coverage": {
            "queued_candidate_ids": queue_candidates,
            "seed_candidate_ids": seed_candidates,
            "autotrigger_dispatch_candidate_ids": [str(job.get("candidate_id") or "") for job in autotrigger_jobs],
        },
        "focus": {
            "top_queue_candidate": queue_candidates[0] if queue_candidates else None,
            "top_autotrigger_candidate": str(autotrigger_jobs[0].get("candidate_id") or "") if autotrigger_jobs else None,
            "missing_input_jobs": missing_input_jobs,
        },
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    queue_rows = load_jsonl(QUEUE_PATH)
    seed_rows = load_jsonl(SEEDS_PATH)
    batch = load_json(BATCH_PATH)
    run = load_json(RUN_PATH)
    payload = health_payload(queue_rows, seed_rows, batch, run)
    write_json(OUTPUT_PATH, payload)
    print(json.dumps({"output": portable_path(OUTPUT_PATH), "queue_jobs": len(queue_rows), "autotrigger_seeds": len(seed_rows)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
