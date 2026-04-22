#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_HANDOFF_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-handoff.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer.md"
REQUIRED_REPO_PATHS = [
    Path("scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"),
    Path("scripts/vm-kvm/ensure-guest-admin-shell.py"),
    Path("scripts/vm-kvm/type-to-guest.py"),
    Path("scripts/vm-kvm/guest_bridge.py"),
    Path("scripts/vm-kvm/summary_contract_lib.py"),
    Path("scripts/vm/guest-tools/run-ghidra-symbolized-probe.ps1"),
    Path("scripts/vm/guest-tools/ghidra-headless.cmd"),
    Path("scripts/vm/ghidra/ExportBranchAnalysis.java"),
    Path("scripts/vm/ghidra/SetPdbSymbolRepository.java"),
]


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


def repo_file_manifest() -> tuple[list[str], list[str]]:
    present: list[str] = []
    missing: list[str] = []
    for relative_path in REQUIRED_REPO_PATHS:
        full_path = REPO_ROOT / relative_path
        if full_path.exists():
            present.append(relative_path.as_posix())
        else:
            missing.append(relative_path.as_posix())
    return present, missing


def derive_transfer_state(handoff: dict[str, Any], missing_repo_paths: list[str]) -> dict[str, Any]:
    selected_jobs = int((handoff.get("counts") or {}).get("selected_jobs") or 0)
    handoff_status = str(handoff.get("handoff_status") or "idle")
    if selected_jobs <= 0:
        return {
            "status": "idle",
            "blocker": "no-selected-symbol-jobs",
            "next_action": "Refresh the autotrigger lane until the handoff surface exposes at least one selected symbol-resolution job.",
        }
    if missing_repo_paths:
        return {
            "status": "blocked",
            "blocker": "missing-transfer-files",
            "next_action": "Restore the missing repo files before exporting this symbol-resolution pack to another host.",
        }
    if handoff_status == "ready":
        return {
            "status": "ready",
            "blocker": "transfer-pack-ready",
            "next_action": "Copy the listed repo files and use the exported commands on the destination KVM-capable host.",
        }
    return {
        "status": "blocked",
        "blocker": "handoff-not-ready",
        "next_action": "Resolve the handoff blocker before preparing a transfer pack.",
    }


def transfer_payload(
    handoff: dict[str, Any],
    *,
    handoff_path: Path = DEFAULT_HANDOFF_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    present_repo_paths, missing_repo_paths = repo_file_manifest()
    selected_jobs = handoff.get("selected_jobs") or []
    blocked_jobs = handoff.get("blocked_jobs") or []
    candidate_ids = sorted(
        {
            str(candidate_id or "")
            for job in selected_jobs
            for candidate_id in (job.get("candidate_ids") or [])
            if str(candidate_id or "")
        }
    )
    transfer = derive_transfer_state(handoff, missing_repo_paths)
    exported_jobs = [
        {
            "request_id": job.get("request_id"),
            "job_id": job.get("job_id"),
            "target_binary": job.get("target_binary"),
            "guest_binary_path": job.get("guest_binary_path"),
            "patterns": job.get("patterns") or [],
            "candidate_ids": job.get("candidate_ids") or [],
            "suggested_command": job.get("suggested_command"),
            "output_dir": job.get("output_dir"),
        }
        for job in selected_jobs
    ]
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_handoff_path": portable_path(handoff_path),
        "transfer_status": transfer.get("status"),
        "operator": {
            "blocker": transfer.get("blocker"),
            "next_action": transfer.get("next_action"),
        },
        "counts": {
            "selected_jobs": len(selected_jobs),
            "blocked_jobs": len(blocked_jobs),
            "candidate_count": len(candidate_ids),
            "repo_file_count": len(present_repo_paths),
            "missing_repo_file_count": len(missing_repo_paths),
        },
        "candidate_ids": candidate_ids,
        "required_host_tools": sorted(
            {
                str(tool)
                for tool in (handoff.get("required_host_tools") or [])
                if str(tool)
            }
        ),
        "required_repo_paths": present_repo_paths,
        "missing_repo_paths": missing_repo_paths,
        "jobs": exported_jobs,
        "blocked_jobs": [
            {
                "request_id": job.get("request_id"),
                "missing_inputs": job.get("missing_inputs") or [],
                "missing_host_tools": job.get("missing_host_tools") or [],
            }
            for job in blocked_jobs[:10]
        ],
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# Ghidra Symbol Resolution Transfer",
        "",
        f"- Transfer status: `{payload.get('transfer_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Selected jobs: `{counts.get('selected_jobs')}`",
        f"- Candidate count: `{counts.get('candidate_count')}`",
        f"- Required repo files: `{counts.get('repo_file_count')}`",
        f"- Missing repo files: `{counts.get('missing_repo_file_count')}`",
        "",
        "## Transfer Jobs",
        "",
    ]
    jobs = payload.get("jobs") or []
    if not jobs:
        lines.append("- none")
    for job in jobs:
        lines.append(
            f"- `{job.get('request_id')}` -> `{job.get('target_binary')}` | patterns={len(job.get('patterns') or [])} | candidates={len(job.get('candidate_ids') or [])}"
        )
        lines.append(f"  command: `{job.get('suggested_command')}`")
    lines.extend(
        [
            "",
            "## Required Repo Paths",
            "",
        ]
    )
    repo_paths = payload.get("required_repo_paths") or []
    if not repo_paths:
        lines.append("- none")
    for item in repo_paths:
        lines.append(f"- `{item}`")
    lines.extend(
        [
            "",
            "## Missing Repo Paths",
            "",
        ]
    )
    missing_paths = payload.get("missing_repo_paths") or []
    if not missing_paths:
        lines.append("- none")
    for item in missing_paths:
        lines.append(f"- `{item}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a portable transfer pack for prepared Ghidra symbol-resolution jobs.")
    parser.add_argument("--handoff", type=Path, default=DEFAULT_HANDOFF_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    handoff = load_json(args.handoff)
    payload = transfer_payload(handoff, handoff_path=args.handoff)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(
        json.dumps(
            {
                "output": portable_path(args.output),
                "transfer_status": payload.get("transfer_status"),
                "selected_jobs": (payload.get("counts") or {}).get("selected_jobs"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
