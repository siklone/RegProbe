#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shlex
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_IMPORT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-import.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-plan.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-execution-plan.md"


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


def split_csv(value: str | None) -> list[str]:
    return [item.strip() for item in str(value or "").split(",") if item.strip()]


def shell_quote(value: str) -> str:
    if not value:
        return "''"
    safe = set("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_+-=.,/:")
    if all(ch in safe for ch in value):
        return value
    return "'" + value.replace("'", "'\"'\"'") + "'"


def command_argv(command: str) -> list[str]:
    if not command:
        return []
    return shlex.split(command, posix=False)


def parse_command_file(path: Path) -> dict[str, Any]:
    metadata: dict[str, str] = {}
    command_lines: list[str] = []
    in_command = False
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line:
            in_command = True
            continue
        if not in_command and ":" in line:
            key, value = line.split(":", 1)
            metadata[key.strip()] = value.strip()
        else:
            command_lines.append(raw_line.strip())
    original_command = " && ".join(command_lines)
    destination_command = original_command
    if destination_command.startswith("python3 scripts/"):
        destination_command = destination_command.replace("python3 scripts/", "python3 repo/scripts/", 1)
    destination_argv = command_argv(destination_command)
    return {
        "command_file": path.name,
        "request_id": metadata.get("request_id"),
        "target_binary": metadata.get("target_binary"),
        "guest_binary_path": metadata.get("guest_binary_path"),
        "candidate_ids": split_csv(metadata.get("candidate_ids")),
        "patterns": split_csv(metadata.get("patterns")),
        "original_command": original_command,
        "destination_command": destination_command,
        "destination_argv": destination_argv,
        "destination_shell_command": " ".join(shell_quote(item) for item in destination_argv),
    }


def missing_inputs_for_job(import_root: Path, job: dict[str, Any]) -> list[str]:
    missing: list[str] = []
    if not job.get("request_id"):
        missing.append("request_id")
    if not job.get("destination_command"):
        missing.append("destination_command")
    destination_argv = job.get("destination_argv") or []
    if len(destination_argv) >= 2 and destination_argv[0] == "python3" and str(destination_argv[1]).startswith("repo/scripts/"):
        relative_script = str(destination_argv[1])
        if not (import_root / relative_script).exists():
            missing.append(f"missing-script:{relative_script}")
    return missing


def execution_plan_from_import(
    import_payload: dict[str, Any],
    *,
    import_path: Path = DEFAULT_IMPORT_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    import_root = resolve_path(import_payload.get("output_root"))
    errors = list(import_payload.get("errors") or [])
    jobs: list[dict[str, Any]] = []
    blocked_jobs: list[dict[str, Any]] = []

    if import_payload.get("import_status") != "ok":
        errors.append("import_status is not ok")
    if not import_root:
        errors.append("import output_root missing")
    elif not import_root.exists():
        errors.append(f"import output_root missing: {portable_path(import_root)}")
    else:
        command_files = sorted((import_root / "commands").glob("*.txt"))
        if not command_files:
            errors.append("no command files found in imported transfer pack")
        for path in command_files:
            job = parse_command_file(path)
            missing = missing_inputs_for_job(import_root, job)
            job["missing_inputs"] = missing
            job["ready"] = not missing
            if missing:
                blocked_jobs.append(job)
            else:
                jobs.append(job)

    status = "ready" if jobs and not errors and not blocked_jobs else "blocked"
    if not jobs and not blocked_jobs and not errors:
        status = "idle"

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_import_path": portable_path(import_path),
        "execution_plan_status": status,
        "operator": {
            "blocker": "execution-plan-ready" if status == "ready" else "execution-plan-blocked",
            "next_action": "Run the destination_command values from the imported pack root on the KVM-capable host."
            if status == "ready"
            else "Resolve import or command-file blockers before running destination commands.",
        },
        "import_root": portable_path(import_root) if import_root else None,
        "counts": {
            "ready_jobs": len(jobs),
            "blocked_jobs": len(blocked_jobs),
            "candidate_count": len({candidate for job in [*jobs, *blocked_jobs] for candidate in job.get("candidate_ids", [])}),
        },
        "errors": errors,
        "jobs": jobs,
        "blocked_jobs": blocked_jobs,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# Ghidra Transfer Pack Execution Plan",
        "",
        f"- Execution plan status: `{payload.get('execution_plan_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Import root: `{payload.get('import_root')}`",
        f"- Ready jobs: `{counts.get('ready_jobs')}`",
        f"- Blocked jobs: `{counts.get('blocked_jobs')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        "",
        "## Ready Jobs",
        "",
    ]
    jobs = payload.get("jobs") or []
    if not jobs:
        lines.append("- none")
    for job in jobs:
        lines.append(f"- `{job.get('request_id')}`")
        lines.append(f"  command: `{job.get('destination_shell_command')}`")
    lines.extend(["", "## Errors", ""])
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    for error in errors:
        lines.append(f"- `{error}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate destination-host execution plan from an imported Ghidra transfer pack.")
    parser.add_argument("--import", dest="import_path", type=Path, default=DEFAULT_IMPORT_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    import_payload = load_json(args.import_path)
    payload = execution_plan_from_import(import_payload, import_path=args.import_path)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("execution_plan_status") in {"ready", "idle"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
