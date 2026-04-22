#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
INPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-batch.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-run.json"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def runnable_jobs(payload: dict[str, Any]) -> list[dict[str, Any]]:
    jobs = payload.get("jobs") or []
    runnable = [
        job
        for job in jobs
        if job.get("dispatch_status") == "prepared"
        and bool(job.get("can_run_headless"))
        and isinstance(job.get("command_argv"), list)
    ]
    return sorted(
        runnable,
        key=lambda job: (
            -int(job.get("autotrigger_seed_count") or 0),
            int(((job.get("source_job") or {}).get("priority_rank")) or 999999),
            str(job.get("candidate_id") or ""),
        ),
    )


def runner_available(executable: str = "pwsh") -> bool:
    return shutil.which(executable) is not None


def build_run_plan(payload: dict[str, Any], *, limit: int | None = None, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    jobs = runnable_jobs(payload)
    if limit is not None:
        jobs = jobs[:limit]
    blocked = [job for job in payload.get("jobs") or [] if job not in jobs]
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "mode": "dry-run",
        "source_batch": "registry-research-framework/queue/ghidra-dispatch-batch.json",
        "runner_available": runner_available(),
        "selected_job_count": len(jobs),
        "blocked_job_count": len(blocked),
        "jobs": [
            {
                "job_id": job.get("job_id"),
                "candidate_id": job.get("candidate_id"),
                "analysis_mode": job.get("analysis_mode"),
                "autotrigger_seed_count": int(job.get("autotrigger_seed_count") or 0),
                "command_argv": job.get("command_argv"),
                "suggested_command": job.get("suggested_command"),
                "output_dir": job.get("output_dir"),
            }
            for job in jobs
        ],
    }


def run_jobs(payload: dict[str, Any], *, limit: int | None = None, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    jobs = runnable_jobs(payload)
    if limit is not None:
        jobs = jobs[:limit]

    result_jobs: list[dict[str, Any]] = []
    available = runner_available()
    if not available:
        return {
            "schema_version": "1.0",
            "generated_utc": generated_utc,
            "mode": "run",
            "source_batch": "registry-research-framework/queue/ghidra-dispatch-batch.json",
            "runner_available": False,
            "selected_job_count": len(jobs),
            "executed_job_count": 0,
            "error": "pwsh-not-found",
            "jobs": result_jobs,
        }

    for job in jobs:
        argv = [str(part) for part in (job.get("command_argv") or [])]
        completed = subprocess.run(argv, cwd=REPO_ROOT, capture_output=True, text=True, check=False)
        result_jobs.append(
            {
                "job_id": job.get("job_id"),
                "candidate_id": job.get("candidate_id"),
                "analysis_mode": job.get("analysis_mode"),
                "autotrigger_seed_count": int(job.get("autotrigger_seed_count") or 0),
                "exit_code": completed.returncode,
                "stdout": completed.stdout,
                "stderr": completed.stderr,
            }
        )

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "mode": "run",
        "source_batch": "registry-research-framework/queue/ghidra-dispatch-batch.json",
        "runner_available": True,
        "selected_job_count": len(jobs),
        "executed_job_count": len(result_jobs),
        "jobs": result_jobs,
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Plan or run prepared Ghidra dispatch jobs.")
    parser.add_argument("--input", type=Path, default=INPUT_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--limit", type=int, default=None)
    parser.add_argument("--run", action="store_true", help="Execute jobs instead of writing a dry-run plan.")
    args = parser.parse_args()

    payload = load_json(args.input)
    result = run_jobs(payload, limit=args.limit) if args.run else build_run_plan(payload, limit=args.limit)
    write_json(args.output, result)
    print(
        json.dumps(
            {
                "output": args.output.relative_to(REPO_ROOT).as_posix(),
                "mode": result["mode"],
                "selected_job_count": result.get("selected_job_count"),
                "runner_available": result.get("runner_available"),
            },
            indent=2,
        )
    )
    return 0 if result.get("error") is None else 1


if __name__ == "__main__":
    raise SystemExit(main())
