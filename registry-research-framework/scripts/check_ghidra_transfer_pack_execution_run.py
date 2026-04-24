#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_RUN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run-check.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def resolve_path(path_value: str | None) -> Path | None:
    cleaned = str(path_value or "").strip()
    if not cleaned:
        return None
    path = Path(cleaned)
    return path if path.is_absolute() else (REPO_ROOT / path)


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def job_missing_inputs(job: dict[str, Any]) -> list[str]:
    missing = list(job.get("missing_inputs") or [])
    if not str(job.get("request_id") or "").strip():
        missing.append("request_id")
    if not job.get("argv"):
        missing.append("argv")
    if not str(job.get("command") or "").strip():
        missing.append("command")
    cwd = resolve_path(job.get("cwd"))
    if not cwd:
        missing.append("cwd")
    elif not cwd.exists():
        missing.append(f"cwd-missing:{portable_path(cwd)}")
    return missing


def validate_execution_run(payload: dict[str, Any], *, run_path: Path = DEFAULT_RUN_PATH, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []
    status = payload.get("execution_run_status")
    mode = payload.get("mode")
    counts = payload.get("counts") or {}
    jobs = list(payload.get("jobs") or [])
    blocked_jobs = list(payload.get("blocked_jobs") or [])
    job_blockers = [job_missing_inputs(job) for job in jobs]
    blocked_job_blockers = [job_missing_inputs(job) for job in blocked_jobs]
    jobs_with_blockers = sum(1 for blockers in job_blockers if blockers)
    blocked_jobs_with_blockers = sum(1 for blockers in blocked_job_blockers if blockers)

    planned_jobs = int(counts.get("planned_jobs") or 0)
    ready_jobs = int(counts.get("ready_jobs") or 0)
    blocked_job_count = int(counts.get("blocked_jobs") or 0)
    executed_jobs = int(counts.get("executed_jobs") or 0)

    if status not in {"ready", "executed", "blocked", "idle"}:
        errors.append(f"unknown execution_run_status: {status}")
    if mode not in {"dry-run", "execute"}:
        errors.append(f"unknown execution mode: {mode}")
    if planned_jobs != len(jobs) + len(blocked_jobs):
        errors.append(f"planned_jobs mismatch: counts={planned_jobs} actual={len(jobs) + len(blocked_jobs)}")
    if ready_jobs != sum(1 for job, blockers in zip(jobs, job_blockers) if job.get("ready") and not blockers):
        errors.append("ready_jobs does not match ready job list")
    if blocked_job_count != len(blocked_jobs) + jobs_with_blockers:
        errors.append("blocked_jobs does not match blocked job list")
    if executed_jobs != sum(1 for job in jobs if job.get("executed")):
        errors.append("executed_jobs does not match executed job list")
    if status == "ready" and (mode != "dry-run" or ready_jobs == 0 or blocked_job_count != 0 or executed_jobs != 0):
        errors.append("ready execution run must be dry-run with ready jobs, no blockers, and no executed jobs")
    if status == "executed" and (mode != "execute" or executed_jobs != ready_jobs or blocked_job_count != 0):
        errors.append("executed run must execute all ready jobs without blockers")
    if status == "blocked" and blocked_job_count == 0 and not (payload.get("errors") or []):
        errors.append("blocked run must include blocked jobs or errors")
    if status == "idle" and planned_jobs != 0:
        errors.append("idle run cannot contain planned jobs")

    for index, blockers in enumerate(job_blockers, start=1):
        for blocker in blockers:
            errors.append(f"job[{index}] {blocker}")
    for index, blockers in enumerate(blocked_job_blockers, start=1):
        if not blockers:
            errors.append(f"blocked_job[{index}] has no missing_inputs")
        for blocker in blockers:
            errors.append(f"blocked_job[{index}] {blocker}")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "run_path": portable_path(run_path),
        "execution_run_status": status,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "counts": {
            "planned_jobs": planned_jobs,
            "ready_jobs": ready_jobs,
            "blocked_jobs": blocked_job_count,
            "executed_jobs": executed_jobs,
            "jobs_with_blockers": jobs_with_blockers,
            "blocked_jobs_with_blockers": blocked_jobs_with_blockers,
        },
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# Ghidra Transfer Pack Execution Run Check",
        "",
        f"- Check status: `{payload.get('check_status')}`",
        f"- Execution run status: `{payload.get('execution_run_status')}`",
        f"- Planned jobs: `{counts.get('planned_jobs')}`",
        f"- Ready jobs: `{counts.get('ready_jobs')}`",
        f"- Blocked jobs: `{counts.get('blocked_jobs')}`",
        f"- Executed jobs: `{counts.get('executed_jobs')}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    for error in errors:
        lines.append(f"- `{error}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate a Ghidra transfer pack execution-run surface.")
    parser.add_argument("--run", type=Path, default=DEFAULT_RUN_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    payload = validate_execution_run(load_json(args.run), run_path=args.run)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
