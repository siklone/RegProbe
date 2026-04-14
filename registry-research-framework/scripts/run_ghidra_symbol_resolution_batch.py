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
INPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-batch.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-run.json"
REQUIRED_HOST_TOOLS = ("python3", "curl", "virsh")
DEFAULT_BRIDGE_DIR = Path("/tmp/regprobe-bridge")
BRIDGE_ARTIFACTS = (
    ("-evidence.json", "evidence.json"),
    ("-ghidra-matches.md", "ghidra-matches.md"),
    ("-summary.json", "run-summary.json"),
    ("-launcher-stage.json", "launcher-stage.json"),
    ("-symchk.txt", "symchk.txt"),
)


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def runner_available(required_tools: tuple[str, ...] = REQUIRED_HOST_TOOLS) -> bool:
    return all(shutil.which(tool) for tool in required_tools)


def resolve_output_dir(job: dict[str, Any]) -> Path | None:
    output_dir = str(job.get("output_dir") or "").strip()
    if not output_dir:
        return None
    path = Path(output_dir)
    if not path.is_absolute():
        path = REPO_ROOT / path
    return path


def materialize_bridge_artifacts(job: dict[str, Any], *, bridge_dir: Path = DEFAULT_BRIDGE_DIR) -> list[str]:
    output_dir = resolve_output_dir(job)
    if output_dir is None:
        return []
    output_name = output_dir.name
    materialized: list[str] = []
    output_dir.mkdir(parents=True, exist_ok=True)
    for bridge_suffix, target_name in BRIDGE_ARTIFACTS:
        bridge_path = bridge_dir / f"{output_name}{bridge_suffix}"
        if not bridge_path.exists():
            continue
        target_path = output_dir / target_name
        shutil.copy2(bridge_path, target_path)
        materialized.append(target_name)
    return materialized


def job_output_completed(job: dict[str, Any]) -> bool:
    path = resolve_output_dir(job)
    if path is None:
        return False
    summary_path = path / "run-summary.json"
    if not summary_path.exists():
        return False
    try:
        payload = load_json(summary_path)
    except json.JSONDecodeError:
        return False
    if str(payload.get("status") or "").strip().lower() == "ok":
        return True
    try:
        return int(payload.get("ghidra_exit_code")) == 0
    except (TypeError, ValueError):
        return False


def completed_jobs(payload: dict[str, Any]) -> list[dict[str, Any]]:
    return [
        job
        for job in (payload.get("jobs") or [])
        if isinstance(job, dict) and job_output_completed(job)
    ]


def runnable_jobs(payload: dict[str, Any]) -> list[dict[str, Any]]:
    completed_ids = {str(job.get("job_id") or "") for job in completed_jobs(payload)}
    jobs = payload.get("jobs") or []
    runnable = [
        job
        for job in jobs
        if str(job.get("job_id") or "") not in completed_ids
        if job.get("dispatch_status") == "prepared"
        and bool(job.get("can_run_guest_orchestrator"))
        and isinstance(job.get("command_argv"), list)
    ]
    return sorted(
        runnable,
        key=lambda job: (
            int(job.get("priority_rank") or 999999),
            -int(job.get("candidate_count") or 0),
            -int(job.get("occurrence_count") or 0),
            str(job.get("request_id") or ""),
        ),
    )


def build_run_plan(payload: dict[str, Any], *, limit: int | None = None, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    completed = completed_jobs(payload)
    completed_ids = {str(job.get("job_id") or "") for job in completed if str(job.get("job_id") or "")}
    jobs = runnable_jobs(payload)
    if limit is not None:
        jobs = jobs[:limit]
    blocked = [
        job for job in payload.get("jobs") or []
        if job not in jobs and str(job.get("job_id") or "") not in completed_ids
    ]
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "mode": "dry-run",
        "source_batch": "registry-research-framework/queue/ghidra-symbol-resolution-batch.json",
        "runner_available": runner_available(),
        "selected_job_count": len(jobs),
        "blocked_job_count": len(blocked),
        "completed_job_count": len(completed),
        "blocked_jobs": [
            {
                "job_id": job.get("job_id"),
                "request_id": job.get("request_id"),
                "missing_inputs": job.get("missing_inputs") or [],
                "missing_host_tools": job.get("missing_host_tools") or [],
            }
            for job in blocked[:10]
        ],
        "completed_jobs": [
            {
                "job_id": job.get("job_id"),
                "request_id": job.get("request_id"),
                "output_dir": job.get("output_dir"),
            }
            for job in completed[:10]
        ],
        "jobs": [
            {
                "job_id": job.get("job_id"),
                "request_id": job.get("request_id"),
                "analysis_mode": job.get("analysis_mode"),
                "command_argv": job.get("command_argv"),
                "suggested_command": job.get("suggested_command"),
                "output_dir": job.get("output_dir"),
            }
            for job in jobs
        ],
    }


def run_jobs(payload: dict[str, Any], *, limit: int | None = None, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    completed = completed_jobs(payload)
    jobs = runnable_jobs(payload)
    if limit is not None:
        jobs = jobs[:limit]

    if not runner_available():
        return {
            "schema_version": "1.0",
            "generated_utc": generated_utc,
        "mode": "run",
        "source_batch": "registry-research-framework/queue/ghidra-symbol-resolution-batch.json",
        "runner_available": False,
        "selected_job_count": len(jobs),
        "executed_job_count": 0,
        "completed_job_count": len(completed),
        "blocked_job_count": int(payload.get("blocked_job_count") or 0),
        "error": "host-kvm-tools-missing",
        "jobs": [],
    }

    result_jobs: list[dict[str, Any]] = []
    for job in jobs:
        argv = [str(part) for part in (job.get("command_argv") or [])]
        process = subprocess.run(argv, cwd=REPO_ROOT, capture_output=True, text=True, check=False)
        materialized_files = materialize_bridge_artifacts(job)
        result_jobs.append(
            {
                "job_id": job.get("job_id"),
                "request_id": job.get("request_id"),
                "analysis_mode": job.get("analysis_mode"),
                "exit_code": process.returncode,
                "stdout": process.stdout,
                "stderr": process.stderr,
                "materialized_files": materialized_files,
            }
        )

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "mode": "run",
        "source_batch": "registry-research-framework/queue/ghidra-symbol-resolution-batch.json",
        "runner_available": True,
        "selected_job_count": len(jobs),
        "executed_job_count": len(result_jobs),
        "completed_job_count": len(completed),
        "jobs": result_jobs,
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Plan or run prepared Ghidra symbol-resolution jobs.")
    parser.add_argument("--input", type=Path, default=INPUT_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--limit", type=int, default=None)
    parser.add_argument("--run", action="store_true")
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
