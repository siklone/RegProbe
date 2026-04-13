#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_BATCH_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-batch.json"
DEFAULT_RUN_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-run.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-handoff.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-handoff.md"


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


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def derive_operator_state(batch: dict[str, Any], run: dict[str, Any]) -> dict[str, Any]:
    prepared = int(batch.get("job_count") or 0)
    selected = int(run.get("selected_job_count") or 0)
    blocked = int(batch.get("blocked_job_count") or 0)
    runner_available = bool(run.get("runner_available"))

    if prepared <= 0:
        return {
            "status": "idle",
            "blocker": "no-symbol-resolution-jobs",
            "next_action": "Refresh the autotrigger lane from a stack-bearing normalized bundle before preparing a handoff.",
            "top_focus": None,
        }
    if selected > 0:
        return {
            "status": "ready",
            "blocker": "symbol-resolution-ready" if runner_available else "external-host-required",
            "next_action": (
                "Run the prepared symbol-resolution jobs locally."
                if runner_available
                else "Hand the prepared symbol-resolution jobs to a KVM-capable host with python3, curl, and virsh available."
            ),
            "top_focus": ((run.get("jobs") or [{}])[0].get("request_id")),
        }
    if blocked > 0:
        return {
            "status": "blocked",
            "blocker": "symbol-resolution-inputs-missing",
            "next_action": "Fill the missing inputs or host tools called out in the blocked jobs before attempting a handoff.",
            "top_focus": ((run.get("blocked_jobs") or [{}])[0].get("request_id")),
        }
    return {
        "status": "idle",
        "blocker": "no-runnable-symbol-resolution-jobs",
        "next_action": "Inspect the symbol batch and run plan; no runnable handoff jobs were selected.",
        "top_focus": None,
    }


def handoff_payload(
    batch: dict[str, Any],
    run: dict[str, Any],
    *,
    batch_path: Path = DEFAULT_BATCH_PATH,
    run_path: Path = DEFAULT_RUN_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    jobs = batch.get("jobs") or []
    run_jobs = run.get("jobs") or []
    run_job_ids = {str(job.get("job_id") or "") for job in run_jobs if str(job.get("job_id") or "")}
    prepared_jobs = [
        {
            "job_id": job.get("job_id"),
            "request_id": job.get("request_id"),
            "lookup_key": job.get("lookup_key"),
            "analysis_mode": job.get("analysis_mode"),
            "candidate_ids": job.get("candidate_ids") or [],
            "target_binary": job.get("target_binary"),
            "guest_binary_path": job.get("guest_binary_path"),
            "patterns": job.get("patterns") or [],
            "suggested_command": job.get("suggested_command"),
            "output_dir": job.get("output_dir"),
            "missing_inputs": job.get("missing_inputs") or [],
            "missing_host_tools": job.get("missing_host_tools") or [],
            "can_run_guest_orchestrator": bool(job.get("can_run_guest_orchestrator")),
        }
        for job in jobs
    ]
    selected_jobs = [job for job in prepared_jobs if str(job.get("job_id") or "") in run_job_ids]
    blocked_jobs = [job for job in prepared_jobs if str(job.get("job_id") or "") not in run_job_ids]
    candidate_ids = sorted(
        {
            str(candidate_id or "")
            for job in prepared_jobs
            for candidate_id in (job.get("candidate_ids") or [])
            if str(candidate_id or "")
        }
    )
    operator = derive_operator_state(batch, run)
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "batch_path": portable_path(batch_path),
        "run_path": portable_path(run_path),
        "handoff_status": operator.get("status"),
        "operator": operator,
        "counts": {
            "prepared_jobs": int(batch.get("job_count") or 0),
            "runnable_jobs": int(batch.get("runnable_job_count") or 0),
            "blocked_jobs": int(batch.get("blocked_job_count") or 0),
            "selected_jobs": int(run.get("selected_job_count") or 0),
            "run_blocked_jobs": int(run.get("blocked_job_count") or 0),
            "candidate_count": len(candidate_ids),
        },
        "required_host_tools": sorted(
            {
                str(tool)
                for job in prepared_jobs
                for tool in (job.get("missing_host_tools") or [])
                if str(tool)
            }
        ),
        "batch_missing_host_tools": batch.get("missing_host_tools") or [],
        "candidate_ids": candidate_ids,
        "selected_jobs": selected_jobs,
        "blocked_jobs": blocked_jobs[:10],
        "diagnostics": {
            "batch_resolution_kind_counts": ((batch.get("diagnostics") or {}).get("resolution_kind_counts") or {}),
            "batch_missing_input_counts": ((batch.get("diagnostics") or {}).get("missing_input_counts") or {}),
            "batch_blocked_examples": ((batch.get("diagnostics") or {}).get("blocked_examples") or []),
            "run_runner_available": bool(run.get("runner_available")),
            "run_mode": run.get("mode"),
        },
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# Ghidra Symbol Resolution Handoff",
        "",
        f"- Handoff status: `{payload.get('handoff_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Prepared jobs: `{counts.get('prepared_jobs')}`",
        f"- Runnable jobs: `{counts.get('runnable_jobs')}`",
        f"- Selected jobs: `{counts.get('selected_jobs')}`",
        f"- Blocked jobs: `{counts.get('blocked_jobs')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        "",
        "## Selected Jobs",
        "",
    ]
    selected = payload.get("selected_jobs") or []
    if not selected:
        lines.append("- none")
    for job in selected:
        lines.append(
            f"- `{job.get('request_id')}` -> `{job.get('target_binary')}` | candidates={len(job.get('candidate_ids') or [])} | patterns={len(job.get('patterns') or [])}"
        )
        lines.append(f"  command: `{job.get('suggested_command')}`")
    lines.extend(
        [
            "",
            "## Blocked Jobs",
            "",
        ]
    )
    blocked = payload.get("blocked_jobs") or []
    if not blocked:
        lines.append("- none")
    for job in blocked:
        lines.append(
            f"- `{job.get('request_id')}` missing_inputs={job.get('missing_inputs')} missing_host_tools={job.get('missing_host_tools')}"
        )
    lines.extend(
        [
            "",
            "## Diagnostics",
            "",
            f"- Resolution kind counts: `{json.dumps((payload.get('diagnostics') or {}).get('batch_resolution_kind_counts') or {}, sort_keys=True)}`",
            f"- Missing input counts: `{json.dumps((payload.get('diagnostics') or {}).get('batch_missing_input_counts') or {}, sort_keys=True)}`",
            f"- Runner available: `{(payload.get('diagnostics') or {}).get('run_runner_available')}`",
        ]
    )
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an operator-facing handoff summary for prepared Ghidra symbol-resolution jobs.")
    parser.add_argument("--batch", type=Path, default=DEFAULT_BATCH_PATH)
    parser.add_argument("--run", type=Path, default=DEFAULT_RUN_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    batch = load_json(args.batch)
    run = load_json(args.run)
    payload = handoff_payload(batch, run, batch_path=args.batch, run_path=args.run)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "handoff_status": payload.get("handoff_status"),
                "selected_jobs": payload.get("counts", {}).get("selected_jobs"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
