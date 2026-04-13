#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import shutil
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
INPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-queue.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-batch.json"
RUNNER_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-ghidra-symbolized-probe.py"
REQUIRED_HOST_TOOLS = ("python3", "curl", "virsh")


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def repo_relative(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


def slugify(value: Any) -> str:
    return re.sub(r"[^a-z0-9]+", "-", str(value or "").lower()).strip("-") or "unnamed"


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8"))


def infer_guest_binary_path(target_binary: Any) -> str | None:
    name = str(target_binary or "").strip()
    if not name:
        return None
    lowered = name.lower()
    if lowered == "ntoskrnl.exe":
        return r"C:\Windows\System32\ntoskrnl.exe"
    if lowered == "explorer.exe":
        return r"C:\Windows\explorer.exe"
    if lowered.endswith(".exe"):
        return rf"C:\Windows\System32\{name}"
    return None


def host_tool_status(tool_names: tuple[str, ...] = REQUIRED_HOST_TOOLS) -> dict[str, dict[str, Any]]:
    return {
        name: {
            "present": bool(shutil.which(name)),
            "path": shutil.which(name) or "",
        }
        for name in tool_names
    }


def build_command_argv(guest_binary_path: str | None, output_name: str, patterns: list[str]) -> list[str] | None:
    if not guest_binary_path or not patterns:
        return None
    argv = [
        "python3",
        repo_relative(RUNNER_PATH),
        "--binary-path",
        guest_binary_path,
        "--output-name",
        output_name,
    ]
    for pattern in patterns:
        argv.extend(["--pattern", pattern])
    return argv


def build_suggested_command(command_argv: list[str] | None) -> str | None:
    if not command_argv:
        return None
    return " ".join(f'"{arg}"' if " " in arg else arg for arg in command_argv)


def symbol_resolution_batch_from_queue(
    payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
    tool_status: dict[str, dict[str, Any]] | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    tool_status = tool_status or host_tool_status()
    missing_host_tools = [name for name, item in tool_status.items() if not item.get("present")]
    requests = payload.get("requests") or []
    jobs: list[dict[str, Any]] = []
    resolution_kind_counts: Counter[str] = Counter()
    missing_input_counts: Counter[str] = Counter()
    blocked_examples: list[dict[str, Any]] = []

    for index, request in enumerate(requests, start=1):
        request_id = str(request.get("request_id") or "")
        resolution_kind = str(request.get("resolution_kind") or "unknown")
        resolution_kind_counts[resolution_kind] += 1
        candidate_ids = request.get("candidate_ids") or []
        output_name = f"ghidra-symbolized-{index:02d}-{slugify(candidate_ids[0] if candidate_ids else request_id)}"
        patterns = [
            str(pattern).strip()
            for pattern in (request.get("suggested_patterns") or [])
            if str(pattern).strip()
        ]
        guest_binary_path = infer_guest_binary_path(request.get("target_binary"))
        missing_inputs: list[str] = []
        if not guest_binary_path:
            missing_inputs.append("guest_binary_path")
        if not patterns:
            missing_inputs.append("patterns")
        for item in missing_inputs:
            missing_input_counts[item] += 1
        command_argv = build_command_argv(guest_binary_path, output_name, patterns)
        can_run_guest_orchestrator = not missing_inputs and not missing_host_tools and isinstance(command_argv, list)
        if (missing_inputs or missing_host_tools) and len(blocked_examples) < 10:
            blocked_examples.append(
                {
                    "request_id": request_id,
                    "lookup_key": request.get("lookup_key"),
                    "missing_inputs": list(missing_inputs),
                    "missing_host_tools": list(missing_host_tools),
                }
            )
        jobs.append(
            {
                "job_id": f"ghidra-symbol-dispatch-{index:02d}-{slugify(request_id)}",
                "request_id": request_id,
                "dispatch_status": "prepared",
                "created_utc": generated_utc,
                "priority_rank": int(request.get("priority_rank") or index),
                "lookup_key": request.get("lookup_key"),
                "resolution_kind": resolution_kind,
                "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                "candidate_ids": candidate_ids,
                "candidate_count": int(request.get("candidate_count") or 0),
                "occurrence_count": int(request.get("occurrence_count") or 0),
                "target_binary": request.get("target_binary"),
                "guest_binary_path": guest_binary_path,
                "patterns": patterns,
                "frame_variants": request.get("frame_variants") or [],
                "next_action_hint": request.get("next_action_hint"),
                "required_host_tools": list(REQUIRED_HOST_TOOLS),
                "missing_host_tools": missing_host_tools,
                "can_run_guest_orchestrator": can_run_guest_orchestrator,
                "missing_inputs": missing_inputs,
                "command_argv": command_argv,
                "suggested_command": build_suggested_command(command_argv),
                "output_dir": f"evidence/files/ghidra/{output_name}",
                "source_request": {
                    "source_bundle_paths": request.get("source_bundle_paths") or [],
                    "source_run_ids": request.get("source_run_ids") or [],
                    "source_event_indices": request.get("source_event_indices") or [],
                },
            }
        )

    runnable_job_count = sum(1 for job in jobs if job.get("can_run_guest_orchestrator"))
    blocked_job_count = len(jobs) - runnable_job_count
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_symbol_queue": repo_relative(INPUT_PATH),
        "job_count": len(jobs),
        "runnable_job_count": runnable_job_count,
        "blocked_job_count": blocked_job_count,
        "missing_host_tools": missing_host_tools,
        "diagnostics": {
            "resolution_kind_counts": dict(sorted(resolution_kind_counts.items())),
            "missing_input_counts": dict(sorted(missing_input_counts.items())),
            "blocked_examples": blocked_examples,
        },
        "jobs": jobs,
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    payload = load_json(INPUT_PATH)
    batch = symbol_resolution_batch_from_queue(payload)
    write_json(OUTPUT_PATH, batch)
    print(json.dumps({"output": repo_relative(OUTPUT_PATH), "job_count": batch["job_count"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
