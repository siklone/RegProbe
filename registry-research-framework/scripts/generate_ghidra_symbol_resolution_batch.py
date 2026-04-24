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
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def infer_guest_binary_path(target_binary: Any) -> str | None:
    name = str(target_binary or "").strip()
    if not name:
        return None
    lowered = name.lower()
    if lowered == "ntoskrnl.exe":
        return r"C:\Windows\System32\ntoskrnl.exe"
    if lowered == "explorer.exe":
        return r"C:\Windows\explorer.exe"
    if lowered.endswith(".dll"):
        return rf"C:\Windows\System32\{name}"
    if lowered.endswith(".sys"):
        return rf"C:\Windows\System32\drivers\{name}"
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


def build_command_argv(
    guest_binary_path: str | None,
    output_name: str,
    patterns: list[str],
    *,
    module_offsets: list[str] | None = None,
) -> list[str] | None:
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
    for module_offset in module_offsets or []:
        argv.extend(["--module-offset", module_offset])
    return argv


def build_suggested_command(command_argv: list[str] | None) -> str | None:
    if not command_argv:
        return None
    return " ".join(f'"{arg}"' if " " in arg else arg for arg in command_argv)


def raw_address_requires_module_base(request: dict[str, Any]) -> bool:
    if str(request.get("resolution_kind") or "") != "raw_address":
        return False
    if request.get("offset_hex"):
        return False
    return not str(request.get("module_base") or "").strip()


def group_key_for_request(
    request: dict[str, Any],
    *,
    guest_binary_path: str | None,
    patterns: list[str],
) -> tuple[Any, ...]:
    resolution_kind = str(request.get("resolution_kind") or "")
    if resolution_kind == "module_offset":
        return (
            "module_offset",
            str(request.get("target_binary") or ""),
            str(guest_binary_path or ""),
            tuple(str(item) for item in (request.get("candidate_ids") or [])),
            tuple(patterns),
        )
    return ("single", str(request.get("request_id") or ""))


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
    grouped_jobs: dict[tuple[Any, ...], dict[str, Any]] = {}
    resolution_kind_counts: Counter[str] = Counter()
    missing_input_counts: Counter[str] = Counter()
    blocked_examples: list[dict[str, Any]] = []

    for request in requests:
        request_id = str(request.get("request_id") or "")
        resolution_kind = str(request.get("resolution_kind") or "unknown")
        resolution_kind_counts[resolution_kind] += 1
        candidate_ids = request.get("candidate_ids") or []
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
        if raw_address_requires_module_base(request):
            missing_inputs.append("module_base")
        for item in missing_inputs:
            missing_input_counts[item] += 1
        module_offsets = [
            str(frame).strip()
            for frame in (request.get("frame_variants") or [])
            if str(frame).strip() and str(request.get("resolution_kind") or "") == "module_offset"
        ]
        group_key = group_key_for_request(request, guest_binary_path=guest_binary_path, patterns=patterns)
        group = grouped_jobs.get(group_key)
        if group is None:
            lookup_slug = slugify(request.get("lookup_key") or request_id)
            candidate_slug = slugify(candidate_ids[0] if candidate_ids else request_id)
            group = {
                "request_id": request_id,
                "request_ids": [],
                "lookup_key": request.get("lookup_key"),
                "lookup_keys": [],
                "dispatch_status": "prepared",
                "created_utc": generated_utc,
                "priority_rank": int(request.get("priority_rank") or len(grouped_jobs) + 1),
                "resolution_kind": resolution_kind,
                "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                "candidate_ids": sorted({str(item) for item in candidate_ids if str(item)}),
                "candidate_count": 0,
                "occurrence_count": 0,
                "target_binary": request.get("target_binary"),
                "guest_binary_path": guest_binary_path,
                "patterns": [],
                "frame_variants": [],
                "module_offsets": [],
                "next_action_hint": request.get("next_action_hint"),
                "required_host_tools": list(REQUIRED_HOST_TOOLS),
                "missing_host_tools": list(missing_host_tools),
                "missing_inputs": [],
                "output_name": f"ghidra-symbolized-{lookup_slug}-{candidate_slug}",
                "output_dir": "",
                "source_request": {
                    "source_bundle_paths": [],
                    "source_run_ids": [],
                    "source_event_indices": [],
                },
            }
            grouped_jobs[group_key] = group

        group["request_ids"].append(request_id)
        lookup_key = str(request.get("lookup_key") or "")
        if lookup_key and lookup_key not in group["lookup_keys"]:
            group["lookup_keys"].append(lookup_key)
        group["candidate_count"] = max(int(group["candidate_count"] or 0), int(request.get("candidate_count") or 0))
        group["occurrence_count"] = int(group["occurrence_count"] or 0) + int(request.get("occurrence_count") or 0)
        for pattern in patterns:
            if pattern not in group["patterns"]:
                group["patterns"].append(pattern)
        for frame in request.get("frame_variants") or []:
            cleaned = str(frame).strip()
            if cleaned and cleaned not in group["frame_variants"]:
                group["frame_variants"].append(cleaned)
        for module_offset in module_offsets:
            if module_offset not in group["module_offsets"]:
                group["module_offsets"].append(module_offset)
        for item in missing_inputs:
            if item not in group["missing_inputs"]:
                group["missing_inputs"].append(item)
        for item in (request.get("source_bundle_paths") or []):
            if item not in group["source_request"]["source_bundle_paths"]:
                group["source_request"]["source_bundle_paths"].append(item)
        for item in (request.get("source_run_ids") or []):
            if item not in group["source_request"]["source_run_ids"]:
                group["source_request"]["source_run_ids"].append(item)
        for item in (request.get("source_event_indices") or []):
            if item not in group["source_request"]["source_event_indices"]:
                group["source_request"]["source_event_indices"].append(item)

    jobs: list[dict[str, Any]] = []
    grouped_rows = sorted(
        grouped_jobs.values(),
        key=lambda item: (
            int(item.get("priority_rank") or 999999),
            -int(item.get("candidate_count") or 0),
            -int(item.get("occurrence_count") or 0),
            str(item.get("request_id") or ""),
        ),
    )
    for index, group in enumerate(grouped_rows, start=1):
        output_name = f"{group['output_name']}"
        command_argv = None if group["missing_inputs"] else build_command_argv(
            group.get("guest_binary_path"),
            output_name,
            group.get("patterns") or [],
            module_offsets=group.get("module_offsets") or [],
        )
        can_run_guest_orchestrator = not group["missing_inputs"] and not group["missing_host_tools"] and isinstance(command_argv, list)
        if (group["missing_inputs"] or group["missing_host_tools"]) and len(blocked_examples) < 10:
            blocked_examples.append(
                {
                    "request_id": group.get("request_id"),
                    "lookup_key": group.get("lookup_key"),
                    "missing_inputs": list(group.get("missing_inputs") or []),
                    "missing_host_tools": list(group.get("missing_host_tools") or []),
                }
            )
        group["output_dir"] = f"evidence/raw/ghidra/{output_name}"
        jobs.append(
            {
                "job_id": f"ghidra-symbol-dispatch-{index:02d}-{slugify(group.get('request_id'))}",
                "request_id": group.get("request_id"),
                "request_ids": group.get("request_ids") or [],
                "request_count": len(group.get("request_ids") or []),
                "dispatch_status": group.get("dispatch_status"),
                "created_utc": group.get("created_utc"),
                "priority_rank": int(group.get("priority_rank") or index),
                "lookup_key": group.get("lookup_key"),
                "lookup_keys": group.get("lookup_keys") or [],
                "resolution_kind": group.get("resolution_kind"),
                "analysis_mode": group.get("analysis_mode"),
                "candidate_ids": group.get("candidate_ids") or [],
                "candidate_count": int(group.get("candidate_count") or 0),
                "occurrence_count": int(group.get("occurrence_count") or 0),
                "target_binary": group.get("target_binary"),
                "guest_binary_path": group.get("guest_binary_path"),
                "patterns": group.get("patterns") or [],
                "frame_variants": group.get("frame_variants") or [],
                "module_offsets": group.get("module_offsets") or [],
                "next_action_hint": group.get("next_action_hint"),
                "required_host_tools": list(REQUIRED_HOST_TOOLS),
                "missing_host_tools": group.get("missing_host_tools") or [],
                "can_run_guest_orchestrator": can_run_guest_orchestrator,
                "missing_inputs": group.get("missing_inputs") or [],
                "command_argv": command_argv,
                "suggested_command": build_suggested_command(command_argv),
                "output_dir": group["output_dir"],
                "source_request": group.get("source_request") or {},
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
