#!/usr/bin/env python3
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from guest_bridge import ensure_guest_bridge
from summary_contract_lib import read_json_object
from vm_env import bridge_base_url, upload_dir, vm_connect, vm_domain


def run(cmd: list[str], cwd: Path | None = None, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        cmd,
        cwd=str(cwd) if cwd else None,
        check=check,
        text=True,
        capture_output=True,
    )


def find_latest(paths: list[Path]) -> Path | None:
    existing = [path for path in paths if path.exists()]
    if not existing:
        return None
    return max(existing, key=lambda path: path.stat().st_mtime)


def load_json(
    path: Path | None,
    load_errors: list[dict[str, str]] | None = None,
    label: str = "",
) -> dict[str, object] | None:
    if path is None or not path.exists():
        return None

    try:
        payload = read_json_object(path, context=label or "research lane payload")
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        if load_errors is not None:
            load_errors.append(
                {
                    "label": label,
                    "path": str(path),
                    "error": str(exc),
                }
            )
        return None
    return payload


def utc_timestamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate host-side KVM research lane prerequisites and buildability.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--uri", default=vm_connect("qemu:///session"))
    parser.add_argument("--vm-name", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--bridge-url", default=bridge_base_url("http://127.0.0.1:8766").rstrip("/") + "/healthz")
    parser.add_argument("--output", default="")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    output_path = Path(args.output).resolve() if args.output else repo_root / "registry-research-framework" / "audit" / "kvm-research-lane-health-latest.json"

    required_commands = [
        "python3",
        "curl",
        "virsh",
        "qemu-img",
        "xorriso",
        "bash",
    ]

    host_tools: dict[str, dict[str, object]] = {}
    for tool in required_commands:
        resolved = shutil.which(tool)
        host_tools[tool] = {
            "present": bool(resolved),
            "path": resolved or "",
        }

    bridge = {
        "healthy": False,
        "url": args.bridge_url,
        "status": "",
        "error_kind": "",
        "error": "",
        "autostarted": False,
        "launch": {},
    }
    bridge_base_url = args.bridge_url.removesuffix("/healthz")
    try:
        bridge_info = ensure_guest_bridge(
            repo_root=repo_root,
            bridge_base_url=bridge_base_url,
            upload_root=Path(upload_dir("/tmp/regprobe-bridge")),
        )
        bridge["launch"] = bridge_info
        bridge["autostarted"] = bool(bridge_info.get("launched"))
        bridge_resp = run(["curl", "-fsS", args.bridge_url])
        bridge["healthy"] = bridge_resp.stdout.strip() == "ok"
        bridge["status"] = bridge_resp.stdout.strip()
    except subprocess.CalledProcessError as exc:
        bridge["error_kind"] = "bridge-health-check-error"
        bridge["error"] = (exc.stderr or exc.stdout).strip()
    except Exception as exc:
        bridge["error_kind"] = type(exc).__name__
        bridge["error"] = str(exc)

    vm = {
        "defined": False,
        "running": False,
        "uri": args.uri,
        "name": args.vm_name,
        "state": "",
        "error": "",
    }
    try:
        vm_resp = run(["virsh", "-c", args.uri, "dominfo", args.vm_name])
        vm["defined"] = True
        for line in vm_resp.stdout.splitlines():
            if line.startswith("State:"):
                vm["state"] = line.split(":", 1)[1].strip()
                vm["running"] = "running" in vm["state"].lower()
                break
    except subprocess.CalledProcessError as exc:
        vm["error"] = (exc.stderr or exc.stdout).strip()

    bootstrap_iso = {
        "build_ok": False,
        "path": "",
        "error": "",
    }
    with tempfile.TemporaryDirectory(prefix="regprobe-kvm-validate-") as temp_root:
        payload_dir = Path(temp_root) / "payload"
        iso_path = Path(temp_root) / "bootstrap.iso"
        try:
            run(
                [
                    "bash",
                    str(repo_root / "scripts" / "vm-kvm" / "build-research-bootstrap-iso.sh"),
                    str(payload_dir),
                    str(iso_path),
                ],
                cwd=repo_root,
            )
            bootstrap_iso["build_ok"] = iso_path.exists()
            bootstrap_iso["path"] = str(iso_path)
        except subprocess.CalledProcessError as exc:
            bootstrap_iso["error"] = (exc.stderr or exc.stdout).strip()

    tracked_files = [
        repo_root / "scripts" / "vm-kvm" / "bootstrap-research-lane.ps1",
        repo_root / "scripts" / "vm-kvm" / "build-research-bootstrap-iso.sh",
        repo_root / "scripts" / "vm-kvm" / "attach-bootstrap-iso.sh",
        repo_root / "scripts" / "vm-kvm" / "serve-guest-bridge.py",
        repo_root / "scripts" / "vm-kvm" / "guest_bridge.py",
        repo_root / "scripts" / "vm-kvm" / "ensure-guest-admin-shell.py",
        repo_root / "scripts" / "vm-kvm" / "type-to-guest.py",
        repo_root / "scripts" / "vm-kvm" / "run-guest-registry-policy-probe.py",
        repo_root / "scripts" / "vm-kvm" / "run-guest-local-kd-smoke.py",
        repo_root / "scripts" / "vm" / "guest-tools" / "run-ghidra-symbolized-probe.ps1",
        repo_root / "scripts" / "vm" / "guest-tools" / "run-ghidra-string-xref-probe.ps1",
        repo_root / "scripts" / "vm" / "guest-tools" / "run-registry-policy-probe.ps1",
        repo_root / "scripts" / "vm" / "guest-tools" / "run-local-kd-smoke.ps1",
        repo_root / "scripts" / "vm" / "guest-tools" / "procmon-safe.ps1",
        repo_root / "scripts" / "vm" / "registry-policy-probe.ps1",
        repo_root / "scripts" / "vm" / "tool-health-smoke.ps1",
    ]
    repo_files = {
        str(path.relative_to(repo_root)): {
            "exists": path.exists(),
        }
        for path in tracked_files
    }

    required_artifacts = [
        repo_root / "evidence" / "files" / "ghidra" / "allowremotedasd-kvm-20260406b" / "evidence.json",
        repo_root / "evidence" / "files" / "ghidra" / "uuidsequence-string-kvm-20260406h" / "uuidsequence-string-kvm-20260406h-evidence.json",
        repo_root / "evidence" / "files" / "vm-tooling-staging" / "uuidsequence-procmon-kvm-20260406a" / "uuidsequence-procmon-kvm-20260406a-summary.json",
    ]
    artifacts = {
        str(path.relative_to(repo_root)): {
            "exists": path.exists(),
        }
        for path in required_artifacts
    }

    lane_health_dir = find_latest(
        sorted((repo_root / "evidence" / "files" / "vm-tooling-staging").glob("kvm-tool-health-rerun-*"))
    )
    bootstrap_summary_path = None
    tool_health_summary_path = None
    procmon_direct_1s_path = None
    procmon_direct_5s_path = None
    if lane_health_dir:
        bootstrap_summary_path = lane_health_dir / "bootstrap-summary.json"
        tool_health_summary_path = lane_health_dir / "tool-health.json"
        procmon_direct_1s_path = lane_health_dir / "procmon-direct-1s-summary.json"
        procmon_direct_5s_path = lane_health_dir / "procmon-direct-5s-summary.json"

    lane_health_load_errors: list[dict[str, str]] = []
    bootstrap_summary = load_json(bootstrap_summary_path, lane_health_load_errors, "bootstrap-summary")
    tool_health_summary = load_json(tool_health_summary_path, lane_health_load_errors, "tool-health-summary")
    procmon_direct_1s = load_json(procmon_direct_1s_path, lane_health_load_errors, "procmon-direct-1s")
    procmon_direct_5s = load_json(procmon_direct_5s_path, lane_health_load_errors, "procmon-direct-5s")

    required_tool_names = [
        "procmon",
        "procmon_wrapper",
        "wpr",
        "wpa",
        "xperf",
        "dotnet",
        "winsat",
        "diskspd",
        "ghidra_launcher",
        "symchk",
        "dbghelp",
    ]
    required_smoke_names = [
        "dotnet_info",
        "procmon",
        "wpr",
        "winsat_cpu",
        "winsat_mem",
        "diskspd",
        "symchk_choice",
    ]

    lane_health = {
        "artifact_dir": str(lane_health_dir.relative_to(repo_root)) if lane_health_dir else "",
        "bootstrap_summary_path": str(bootstrap_summary_path.relative_to(repo_root)) if bootstrap_summary_path and bootstrap_summary_path.exists() else "",
        "tool_health_summary_path": str(tool_health_summary_path.relative_to(repo_root)) if tool_health_summary_path and tool_health_summary_path.exists() else "",
        "bootstrap_ok": False,
        "tool_health_ok": False,
        "bootstrap_failed_steps": [],
        "missing_tools": [],
        "failed_smokes": [],
        "load_errors": lane_health_load_errors,
        "procmon_direct_1s": procmon_direct_1s or {},
        "procmon_direct_5s": procmon_direct_5s or {},
    }

    if bootstrap_summary:
        lane_health["bootstrap_ok"] = bootstrap_summary.get("status") == "ok"
        lane_health["bootstrap_failed_steps"] = list(bootstrap_summary.get("failed_steps", []))

    if tool_health_summary:
        missing_tools = [
            name
            for name in required_tool_names
            if not bool(tool_health_summary.get("tools", {}).get(name, {}).get("exists"))
        ]
        failed_smokes = [
            name
            for name in required_smoke_names
            if not bool(tool_health_summary.get("smokes", {}).get(name, {}).get("success"))
        ]
        lane_health["missing_tools"] = missing_tools
        lane_health["failed_smokes"] = failed_smokes
        lane_health["tool_health_ok"] = not missing_tools and not failed_smokes

    status = "ok"
    blockers: list[str] = []
    if not all(item["present"] for item in host_tools.values()):
        status = "needs-attention"
        blockers.append("missing host commands")
    if not bridge["healthy"]:
        status = "needs-attention"
        blockers.append("bridge unhealthy")
    if not vm["defined"] or not vm["running"]:
        status = "needs-attention"
        blockers.append("vm not running")
    if not bootstrap_iso["build_ok"]:
        status = "needs-attention"
        blockers.append("bootstrap iso build failed")
    if not all(item["exists"] for item in repo_files.values()):
        status = "needs-attention"
        blockers.append("missing tracked kvm lane files")
    if not all(item["exists"] for item in artifacts.values()):
        status = "needs-attention"
        blockers.append("missing expected kvm artifacts")
    if not lane_health_dir or not bootstrap_summary or not tool_health_summary:
        status = "needs-attention"
        blockers.append("missing lane health evidence")
    if lane_health_load_errors:
        status = "needs-attention"
        blockers.append("lane health evidence unreadable")
    if lane_health_dir and bootstrap_summary and tool_health_summary:
        if not lane_health["bootstrap_ok"]:
            status = "needs-attention"
            blockers.append("bootstrap summary not ok")
        if not lane_health["tool_health_ok"]:
            status = "needs-attention"
            blockers.append("tool health summary not ok")

    result = {
        "generated_utc": utc_timestamp(),
        "status": status,
        "blockers": blockers,
        "host_tools": host_tools,
        "bridge": bridge,
        "vm": vm,
        "bootstrap_iso": bootstrap_iso,
        "repo_files": repo_files,
        "artifacts": artifacts,
        "lane_health": lane_health,
    }

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(output_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
