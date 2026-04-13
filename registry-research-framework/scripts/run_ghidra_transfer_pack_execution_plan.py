#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_PLAN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-plan.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-run.md"


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
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def build_run_item(
    job: dict[str, Any],
    *,
    import_root: Path | None,
    execute: bool,
    timeout_seconds: int,
) -> dict[str, Any]:
    argv = [str(item) for item in (job.get("destination_argv") or [])]
    shell_command = str(job.get("destination_shell_command") or "").strip()
    missing_inputs = list(job.get("missing_inputs") or [])
    if not argv:
        missing_inputs.append("destination_argv")
    if not shell_command:
        missing_inputs.append("destination_shell_command")
    if not import_root:
        missing_inputs.append("import_root")

    item: dict[str, Any] = {
        "request_id": job.get("request_id"),
        "candidate_ids": job.get("candidate_ids") or [],
        "cwd": portable_path(import_root) if import_root else None,
        "argv": argv,
        "command": shell_command,
        "mode": "execute" if execute else "dry-run",
        "ready": not missing_inputs,
        "missing_inputs": missing_inputs,
        "executed": False,
        "returncode": None,
        "stdout_tail": None,
        "stderr_tail": None,
    }

    if execute and item["ready"] and import_root:
        completed = subprocess.run(
            argv,
            cwd=import_root,
            text=True,
            capture_output=True,
            timeout=timeout_seconds,
            check=False,
        )
        item["executed"] = True
        item["returncode"] = completed.returncode
        item["stdout_tail"] = completed.stdout[-4000:] if completed.stdout else ""
        item["stderr_tail"] = completed.stderr[-4000:] if completed.stderr else ""
        item["ready"] = completed.returncode == 0
        if completed.returncode != 0:
            item["missing_inputs"].append(f"execution-returncode:{completed.returncode}")

    return item


def execution_run_from_plan(
    plan_payload: dict[str, Any],
    *,
    plan_path: Path = DEFAULT_PLAN_PATH,
    execute: bool = False,
    timeout_seconds: int = 900,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    import_root = resolve_path(plan_payload.get("import_root"))
    errors = list(plan_payload.get("errors") or [])
    if plan_payload.get("execution_plan_status") != "ready":
        errors.append("execution_plan_status is not ready")
    if import_root and not import_root.exists():
        errors.append(f"import root missing: {portable_path(import_root)}")

    run_items = [
        build_run_item(job, import_root=import_root, execute=execute, timeout_seconds=timeout_seconds)
        for job in plan_payload.get("jobs") or []
    ]
    blocked_items = [
        build_run_item(job, import_root=import_root, execute=False, timeout_seconds=timeout_seconds)
        for job in plan_payload.get("blocked_jobs") or []
    ]
    failed_items = [item for item in run_items if item.get("missing_inputs")]
    ready_items = [item for item in run_items if item.get("ready")]

    if errors or failed_items or blocked_items:
        status = "blocked"
    elif ready_items:
        status = "executed" if execute else "ready"
    else:
        status = "idle"

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_plan_path": portable_path(plan_path),
        "execution_run_status": status,
        "mode": "execute" if execute else "dry-run",
        "operator": {
            "blocker": "execution-run-complete"
            if status == "executed"
            else "execution-run-ready"
            if status == "ready"
            else "execution-run-blocked"
            if status == "blocked"
            else "execution-run-idle",
            "next_action": "Review the dry-run commands, then rerun with --execute on a KVM-capable host."
            if status == "ready"
            else "Attach run outputs to the symbol resolution lane."
            if status == "executed"
            else "Resolve execution plan or command blockers before running destination jobs.",
        },
        "import_root": portable_path(import_root) if import_root else None,
        "counts": {
            "planned_jobs": len(run_items) + len(blocked_items),
            "ready_jobs": len(ready_items),
            "blocked_jobs": len(blocked_items) + len(failed_items),
            "executed_jobs": sum(1 for item in run_items if item.get("executed")),
        },
        "errors": errors,
        "jobs": run_items,
        "blocked_jobs": blocked_items,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# Ghidra Transfer Pack Execution Run",
        "",
        f"- Execution run status: `{payload.get('execution_run_status')}`",
        f"- Mode: `{payload.get('mode')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Import root: `{payload.get('import_root')}`",
        f"- Planned jobs: `{counts.get('planned_jobs')}`",
        f"- Ready jobs: `{counts.get('ready_jobs')}`",
        f"- Blocked jobs: `{counts.get('blocked_jobs')}`",
        f"- Executed jobs: `{counts.get('executed_jobs')}`",
        "",
        "## Jobs",
        "",
    ]
    jobs = payload.get("jobs") or []
    if not jobs:
        lines.append("- none")
    for job in jobs:
        lines.append(f"- `{job.get('request_id')}`")
        lines.append(f"  cwd: `{job.get('cwd')}`")
        lines.append(f"  command: `{job.get('command')}`")
    lines.extend(["", "## Blockers", ""])
    blockers = []
    for error in payload.get("errors") or []:
        blockers.append(str(error))
    for job in payload.get("blocked_jobs") or []:
        for item in job.get("missing_inputs") or []:
            blockers.append(f"{job.get('request_id')}: {item}")
    for job in payload.get("jobs") or []:
        for item in job.get("missing_inputs") or []:
            blockers.append(f"{job.get('request_id')}: {item}")
    if not blockers:
        lines.append("- none")
    for blocker in blockers:
        lines.append(f"- `{blocker}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Dry-run or execute a Ghidra transfer pack execution plan.")
    parser.add_argument("--plan", type=Path, default=DEFAULT_PLAN_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    parser.add_argument("--execute", action="store_true", help="Run destination argv entries. Default is dry-run only.")
    parser.add_argument("--timeout-seconds", type=int, default=900)
    args = parser.parse_args()

    plan_payload = load_json(args.plan)
    payload = execution_run_from_plan(
        plan_payload,
        plan_path=args.plan,
        execute=args.execute,
        timeout_seconds=args.timeout_seconds,
    )
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("execution_run_status") in {"ready", "executed", "idle"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
