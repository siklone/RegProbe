#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path
from typing import Any

from summary_contract_lib import apply_summary_contract


def run_json_command(cmd: list[str], *, cwd: Path) -> tuple[int, dict[str, Any]]:
    completed = subprocess.run(cmd, cwd=str(cwd), capture_output=True, text=True)
    stdout = completed.stdout.strip()
    if not stdout:
        return completed.returncode, {}
    return completed.returncode, json.loads(stdout)


def run_qga_put_file(
    repo_root: Path,
    *,
    source: Path,
    destination: str,
) -> tuple[int, dict[str, Any]]:
    cmd = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "qga-put-file.py"),
        "--source",
        str(source),
        "--destination",
        destination,
    ]
    return run_json_command(cmd, cwd=repo_root)


def run_qga_exec(
    repo_root: Path,
    *,
    path: str,
    args: list[str] | None = None,
    wait_timeout: int = 120,
) -> tuple[int, dict[str, Any]]:
    cmd = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "qga-exec.py"),
        "--path",
        path,
        "--wait-timeout",
        str(wait_timeout),
    ]
    for arg in args or []:
        cmd.append(f"--arg={arg}")
    return run_json_command(cmd, cwd=repo_root)


def run_app_launch_smoke(
    repo_root: Path,
    *,
    app_exe: str,
    linger_seconds: int,
    leave_running: bool,
) -> tuple[int, dict[str, Any]]:
    cmd = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "run-guest-app-launch-smoke.py"),
        "--app-exe",
        app_exe,
        "--linger-seconds",
        str(linger_seconds),
    ]
    if leave_running:
        cmd.append("--leave-running")
    return run_json_command(cmd, cwd=repo_root)


def deploy_publish_zip(
    repo_root: Path,
    *,
    guest_publish_zip_path: str,
    guest_app_root: str,
) -> tuple[int, dict[str, Any]]:
    ps = (
        "Get-Process RegProbe.App -ErrorAction SilentlyContinue | Stop-Process -Force; "
        f"Remove-Item -LiteralPath {quote_ps(guest_app_root)} -Recurse -Force -ErrorAction SilentlyContinue; "
        f"New-Item -ItemType Directory -Path {quote_ps(guest_app_root)} -Force | Out-Null; "
        f"Expand-Archive -LiteralPath {quote_ps(guest_publish_zip_path)} -DestinationPath {quote_ps(guest_app_root)} -Force; "
        f"$exe = Join-Path {quote_ps(guest_app_root)} 'RegProbe.App.exe'; "
        "[pscustomobject]@{"
        f"AppRoot={quote_ps(guest_app_root)}; "
        f"PublishZip={quote_ps(guest_publish_zip_path)}; "
        "Executable=$exe; "
        "ExecutableExists=[bool](Test-Path -LiteralPath $exe)"
        "} | ConvertTo-Json -Compress"
    )
    return run_qga_exec(
        repo_root,
        path="powershell.exe",
        args=["-NoProfile", "-Command", ps],
        wait_timeout=180,
    )


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def main() -> int:
    parser = argparse.ArgumentParser(description="Upload a publish zip to the KVM guest, expand it, and run app launch smoke.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--publish-zip", required=True)
    parser.add_argument("--guest-publish-zip-path", default=r"C:\Tools\Inbound\app-publish-current-branch.zip")
    parser.add_argument("--guest-app-root", default=r"C:\Tools\AppSmoke")
    parser.add_argument("--guest-app-exe", default=r"C:\Tools\AppSmoke\RegProbe.App.exe")
    parser.add_argument("--linger-seconds", type=int, default=5)
    parser.add_argument("--leave-running", action="store_true")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    publish_zip = Path(args.publish_zip).resolve()

    summary: dict[str, Any] = {
        "summary_source": "guest-app-deploy-smoke",
        "publish_zip": str(publish_zip),
        "guest_publish_zip_path": args.guest_publish_zip_path,
        "guest_app_root": args.guest_app_root,
        "guest_app_exe": args.guest_app_exe,
        "linger_seconds": args.linger_seconds,
    }

    upload_returncode, upload_payload = run_qga_put_file(
        repo_root,
        source=publish_zip,
        destination=args.guest_publish_zip_path,
    )
    summary["upload_returncode"] = upload_returncode
    summary["upload_payload"] = upload_payload
    if upload_returncode != 0 or upload_payload.get("status") != "uploaded":
        summary.update(
            {
                "status": "error",
                "error_kind": "guest-publish-upload-failed",
                "error": "Failed to upload the publish zip into the guest inbound directory.",
            }
        )
        payload = apply_summary_contract(
            summary,
            default_error_kind="guest-publish-upload-failed",
            default_recovery_action="rerun-guest-app-deploy-smoke",
            default_transport_blocker="qga-file-upload",
            default_guest_health="unknown",
        )
        print(json.dumps(payload, indent=2))
        return 1

    deploy_returncode, deploy_payload = deploy_publish_zip(
        repo_root,
        guest_publish_zip_path=args.guest_publish_zip_path,
        guest_app_root=args.guest_app_root,
    )
    summary["deploy_returncode"] = deploy_returncode
    summary["deploy_payload"] = deploy_payload
    if deploy_returncode != 0:
        summary.update(
            {
                "status": "error",
                "error_kind": "guest-publish-deploy-failed",
                "error": "Failed to expand the publish zip into the guest app root.",
            }
        )
        payload = apply_summary_contract(
            summary,
            default_error_kind="guest-publish-deploy-failed",
            default_recovery_action="inspect-guest-deploy",
            default_transport_blocker="guest-publish-deploy",
            default_guest_health="degraded",
        )
        print(json.dumps(payload, indent=2))
        return 1

    smoke_returncode, smoke_payload = run_app_launch_smoke(
        repo_root,
        app_exe=args.guest_app_exe,
        linger_seconds=args.linger_seconds,
        leave_running=args.leave_running,
    )
    summary["smoke_returncode"] = smoke_returncode
    summary["smoke_payload"] = smoke_payload
    if smoke_returncode != 0:
        summary.update(
            {
                "status": "error",
                "error_kind": "guest-app-smoke-failed",
                "error": "The guest app launch smoke did not complete successfully after deploy.",
            }
        )
        payload = apply_summary_contract(
            summary,
            default_error_kind="guest-app-smoke-failed",
            default_recovery_action="inspect-app-launch",
            default_transport_blocker="guest-app-launch",
            default_guest_health="degraded",
        )
        print(json.dumps(payload, indent=2))
        return 1

    summary["status"] = "ok"
    payload = apply_summary_contract(summary)
    print(json.dumps(payload, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
