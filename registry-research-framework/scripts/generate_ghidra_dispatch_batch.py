#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-job-queue.jsonl"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-dispatch-batch.json"
TOOL_PATH = FRAMEWORK_ROOT / "tools" / "ghidra-headless-analyze.ps1"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def repo_relative(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    if not path.exists():
        return rows
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        rows.append(json.loads(line))
    return rows


def split_patterns(value_name: Any) -> list[str]:
    if not value_name:
        return []
    raw_parts = re.split(r"\s*/\s*", str(value_name))
    patterns: list[str] = []
    for raw in raw_parts:
        cleaned = raw.strip()
        if cleaned and cleaned not in patterns:
            patterns.append(cleaned)
    return patterns


def infer_target_binary(key_path: Any) -> str | None:
    normalized = str(key_path or "").lower()
    kernel_prefixes = (
        "hklm\\system\\currentcontrolset\\control\\power",
        "hklm\\system\\currentcontrolset\\control\\session manager\\power",
        "hklm\\system\\currentcontrolset\\control\\session manager\\kernel",
        "hklm\\system\\controlset001\\control\\power",
        "hklm\\system\\controlset001\\control\\session manager\\power",
        "hklm\\system\\controlset001\\control\\session manager\\kernel",
    )
    if normalized.startswith(kernel_prefixes):
        return "ntoskrnl.exe"
    if normalized.startswith("hklm\\software\\microsoft\\windows\\currentversion\\explorer"):
        return "explorer.exe"
    return None


def slugify(value: Any) -> str:
    lowered = re.sub(r"[^a-z0-9]+", "-", str(value or "").lower()).strip("-")
    return lowered or "unnamed"


def build_suggested_command(target_binary: str | None, output_name: str, patterns: list[str]) -> str | None:
    if not target_binary or not patterns:
        return None
    pattern_args = " ".join(f'-Patterns "{pattern}"' for pattern in patterns)
    return (
        f'pwsh -File "{repo_relative(TOOL_PATH)}" '
        f'-TargetBinary "{target_binary}" '
        f'-OutputName "{output_name}" '
        f"{pattern_args}"
    )


def build_command_argv(target_binary: str | None, output_name: str, patterns: list[str]) -> list[str] | None:
    if not target_binary or not patterns:
        return None
    argv = [
        "pwsh",
        "-File",
        repo_relative(TOOL_PATH),
        "-TargetBinary",
        target_binary,
        "-OutputName",
        output_name,
    ]
    for pattern in patterns:
        argv.extend(["-Patterns", pattern])
    return argv


def dispatch_batch_from_queue(rows: list[dict[str, Any]], generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    jobs: list[dict[str, Any]] = []
    for index, row in enumerate(rows, start=1):
        candidate_id = str(row.get("candidate_id") or "")
        output_name = f"ghidra-auto-{index:02d}-{slugify(candidate_id)}"
        patterns = split_patterns(row.get("value_name"))
        target_binary = infer_target_binary(row.get("key_path"))
        command_argv = build_command_argv(target_binary, output_name, patterns)
        missing_inputs: list[str] = []
        if not target_binary:
            missing_inputs.append("target_binary")
        if not patterns:
            missing_inputs.append("patterns")
        jobs.append(
            {
                "job_id": f"ghidra-dispatch-{index:02d}-{slugify(candidate_id)}",
                "dispatch_status": "prepared",
                "queue_status": row.get("status") or "queued",
                "created_utc": generated_utc,
                "candidate_id": candidate_id,
                "feature_area": row.get("feature_area"),
                "key_path": row.get("key_path"),
                "patterns": patterns,
                "target_binary": target_binary,
                "analysis_mode": "registry-string-xref",
                "tool_path": repo_relative(TOOL_PATH),
                "output_name": output_name,
                "output_dir": f"evidence/files/ghidra/{output_name}",
                "can_run_headless": not missing_inputs,
                "missing_inputs": missing_inputs,
                "command_argv": command_argv,
                "suggested_command": build_suggested_command(target_binary, output_name, patterns),
                "promotion_blockers": row.get("promotion_blockers") or [],
                "next_action_hint": row.get("next_action_hint"),
                "source_job": {
                    "priority_rank": row.get("priority_rank"),
                    "trigger": row.get("trigger"),
                },
            }
        )

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_queue": repo_relative(QUEUE_PATH),
        "job_count": len(jobs),
        "jobs": jobs,
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    rows = load_jsonl(QUEUE_PATH)
    payload = dispatch_batch_from_queue(rows)
    write_json(OUTPUT_PATH, payload)
    print(json.dumps({"output": repo_relative(OUTPUT_PATH), "job_count": payload["job_count"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
