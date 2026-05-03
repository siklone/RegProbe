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
INPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-run.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-run.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def runner_available(executable: str = "python3") -> bool:
    return shutil.which(executable) is not None


def selected_items(
    payload: dict[str, Any],
    *,
    include_holds: bool = False,
    candidate_ids: set[str] | None = None,
) -> list[dict[str, Any]]:
    items = []
    for item in payload.get("items") or []:
        candidate_id = str(item.get("candidate_id") or "")
        if candidate_ids and candidate_id not in candidate_ids:
            continue
        if not item.get("capture_ready"):
            continue
        if not isinstance(item.get("dispatch_command_argv"), list):
            continue
        if not include_holds and not item.get("dispatch_recommended"):
            continue
        items.append(item)
    return sorted(items, key=lambda item: str(item.get("candidate_id") or ""))


def skipped_hold_count(
    payload: dict[str, Any],
    *,
    candidate_ids: set[str] | None = None,
) -> int:
    count = 0
    for item in payload.get("items") or []:
        candidate_id = str(item.get("candidate_id") or "")
        if candidate_ids and candidate_id not in candidate_ids:
            continue
        if item.get("capture_ready") and item.get("actionability") == "hold":
            count += 1
    return count


def build_run_plan(
    payload: dict[str, Any],
    *,
    include_holds: bool = False,
    candidate_ids: set[str] | None = None,
    limit: int | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    jobs = selected_items(payload, include_holds=include_holds, candidate_ids=candidate_ids)
    if limit is not None:
        jobs = jobs[:limit]
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "mode": "dry-run",
        "source_batch": portable_path(INPUT_PATH),
        "include_holds": include_holds,
        "runner_available": runner_available(),
        "selected_job_count": len(jobs),
        "skipped_hold_count": 0 if include_holds else skipped_hold_count(payload, candidate_ids=candidate_ids),
        "jobs": [
            {
                "candidate_id": item.get("candidate_id"),
                "actionability": item.get("actionability"),
                "profile_id": item.get("profile_id"),
                "dispatch_command_argv": item.get("dispatch_command_argv"),
                "dispatch_command": item.get("dispatch_command"),
                "next_action_hint": item.get("next_action_hint"),
            }
            for item in jobs
        ],
    }


def run_jobs(
    payload: dict[str, Any],
    *,
    include_holds: bool = False,
    candidate_ids: set[str] | None = None,
    limit: int | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    jobs = selected_items(payload, include_holds=include_holds, candidate_ids=candidate_ids)
    if limit is not None:
        jobs = jobs[:limit]
    if not runner_available():
        return {
            "schema_version": "1.0",
            "generated_utc": generated_utc,
            "mode": "run",
            "source_batch": portable_path(INPUT_PATH),
            "include_holds": include_holds,
            "runner_available": False,
            "selected_job_count": len(jobs),
            "executed_job_count": 0,
            "error": "python3-not-found",
            "jobs": [],
        }

    result_jobs = []
    for item in jobs:
        argv = [str(part) for part in (item.get("dispatch_command_argv") or [])]
        completed = subprocess.run(argv, cwd=REPO_ROOT, capture_output=True, text=True, check=False)
        result_jobs.append(
            {
                "candidate_id": item.get("candidate_id"),
                "actionability": item.get("actionability"),
                "profile_id": item.get("profile_id"),
                "exit_code": completed.returncode,
                "stdout": completed.stdout,
                "stderr": completed.stderr,
            }
        )

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "mode": "run",
        "source_batch": portable_path(INPUT_PATH),
        "include_holds": include_holds,
        "runner_available": True,
        "selected_job_count": len(jobs),
        "executed_job_count": len(result_jobs),
        "jobs": result_jobs,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Dispatch Run",
        "",
        f"- Mode: `{payload.get('mode')}`",
        f"- Include holds: `{payload.get('include_holds')}`",
        f"- Runner available: `{payload.get('runner_available')}`",
        f"- Selected jobs: `{payload.get('selected_job_count')}`",
    ]
    if payload.get("mode") == "dry-run":
        lines.append(f"- Skipped hold jobs: `{payload.get('skipped_hold_count')}`")
    if payload.get("mode") == "run":
        lines.append(f"- Executed jobs: `{payload.get('executed_job_count')}`")
    lines.extend(["", "## Jobs", ""])
    jobs = payload.get("jobs") or []
    if not jobs:
        lines.append("- none")
        return "\n".join(lines).rstrip() + "\n"
    for item in jobs:
        lines.extend(
            [
                f"### {item.get('candidate_id')}",
                "",
                f"- Actionability: `{item.get('actionability')}`",
                f"- Profile: `{item.get('profile_id')}`",
            ]
        )
        if payload.get("mode") == "run":
            lines.append(f"- Exit code: `{item.get('exit_code')}`")
        else:
            lines.append(f"- Next action hint: `{item.get('next_action_hint')}`")
        command = item.get("dispatch_command") or " ".join(str(part) for part in (item.get("dispatch_command_argv") or []))
        lines.extend(["", "```bash", str(command), "```", ""])
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Plan or run ETW stackwalk dispatch jobs from the queue-aware batch surface.")
    parser.add_argument("--input", type=Path, default=INPUT_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    parser.add_argument("--candidate-id", action="append", default=[], help="Limit the plan to one or more candidate ids.")
    parser.add_argument("--include-holds", action="store_true", help="Include intentional-hold items in the selected job set.")
    parser.add_argument("--limit", type=int, default=None)
    parser.add_argument("--run", action="store_true", help="Execute selected jobs instead of writing a dry-run plan.")
    args = parser.parse_args()

    candidate_ids = {item for item in args.candidate_id if str(item).strip()} or None
    payload = load_json(args.input)
    result = (
        run_jobs(payload, include_holds=args.include_holds, candidate_ids=candidate_ids, limit=args.limit)
        if args.run
        else build_run_plan(payload, include_holds=args.include_holds, candidate_ids=candidate_ids, limit=args.limit)
    )
    write_json(args.output, result)
    write_text(args.markdown_output, render_markdown(result))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "mode": result.get("mode"),
                "selected_job_count": result.get("selected_job_count"),
                "include_holds": result.get("include_holds"),
            },
            indent=2,
        )
    )
    return 0 if result.get("error") is None else 1


if __name__ == "__main__":
    raise SystemExit(main())
