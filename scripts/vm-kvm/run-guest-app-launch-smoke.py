#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path
from typing import Any

from command_json_lib import parse_command_json, parse_nested_stdout_json
from summary_contract_lib import apply_summary_contract
from vm_env import crash_log_dir


def run_qga_exec(repo_root: Path, *, path: str, args: list[str] | None = None, wait_timeout: int = 20) -> tuple[int, dict[str, Any]]:
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

    completed = subprocess.run(cmd, cwd=str(repo_root), capture_output=True, text=True)
    return completed.returncode, parse_command_json(completed.stdout, stderr=completed.stderr)


def latest_crash_log(repo_root: Path, *, crash_log_dir: str) -> dict[str, Any] | None:
    ps = (
        f"$dir={quote_ps(crash_log_dir)}; "
        "if(Test-Path -LiteralPath $dir){ "
        "$file=Get-ChildItem -LiteralPath $dir -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1; "
        "if($file){ "
        "[pscustomobject]@{Name=$file.Name; LastWriteTimeUtc=$file.LastWriteTimeUtc.ToString('o')} | ConvertTo-Json -Compress "
        "} "
        "}"
    )
    _, payload = run_qga_exec(
        repo_root,
        path="powershell.exe",
        args=["-NoProfile", "-Command", ps],
    )
    parsed = parse_nested_stdout_json(payload, context="latest-crash-log")
    return parsed if isinstance(parsed, dict) else None


def current_process(repo_root: Path, *, pid: int | None = None) -> dict[str, Any] | None:
    ps = (
        f"Get-Process -Id {pid} -ErrorAction SilentlyContinue | "
        "Select-Object ProcessName,Id,SessionId,Path,StartTime | ConvertTo-Json -Compress"
        if pid is not None
        else "Get-Process RegProbe.App -ErrorAction SilentlyContinue | "
        "Select-Object ProcessName,Id,SessionId,Path,StartTime | ConvertTo-Json -Compress"
    )
    _, payload = run_qga_exec(
        repo_root,
        path="powershell.exe",
        args=["-NoProfile", "-Command", ps],
    )
    parsed = parse_nested_stdout_json(payload, context="current-process")
    return parsed if isinstance(parsed, dict) else None


def stop_regprobe_app(repo_root: Path) -> None:
    ps = "Get-Process RegProbe.App -ErrorAction SilentlyContinue | Stop-Process -Force"
    run_qga_exec(repo_root, path="powershell.exe", args=["-NoProfile", "-Command", ps], wait_timeout=10)


def quote_ps_array(values: list[str]) -> str:
    return "@(" + ", ".join(quote_ps(value) for value in values) + ")"


def launch_app_process(
    repo_root: Path,
    *,
    app_exe: str,
    working_dir: str | None,
    app_args: list[str],
    wait_timeout: int,
) -> tuple[int, dict[str, Any]]:
    ps = (
        f"$exe={quote_ps(app_exe)}; "
        f"$workingDir={quote_ps(working_dir or '')}; "
        f"$arguments={quote_ps_array(app_args)}; "
        "if(-not (Test-Path -LiteralPath $exe)){ throw \"Missing app executable: $exe\" }; "
        "if(-not [string]::IsNullOrWhiteSpace($workingDir) -and -not (Test-Path -LiteralPath $workingDir)){ throw \"Missing working directory: $workingDir\" }; "
        "$startInfo=@{FilePath=$exe; PassThru=$true}; "
        "if(-not [string]::IsNullOrWhiteSpace($workingDir)){ $startInfo.WorkingDirectory=$workingDir }; "
        "if($arguments.Count -gt 0){ $startInfo.ArgumentList=$arguments }; "
        "$proc=Start-Process @startInfo; "
        "[pscustomobject]@{Pid=$proc.Id; ProcessName=$proc.ProcessName; WorkingDirectory=$workingDir; Args=$arguments} | ConvertTo-Json -Compress"
    )
    return run_qga_exec(
        repo_root,
        path="powershell.exe",
        args=["-NoProfile", "-Command", ps],
        wait_timeout=wait_timeout,
    )


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def crash_log_changed(before: dict[str, Any] | None, after: dict[str, Any] | None) -> bool:
    if after is None:
        return False
    if before is None:
        return True
    return (
        str(before.get("Name") or "") != str(after.get("Name") or "")
        or str(before.get("LastWriteTimeUtc") or "") != str(after.get("LastWriteTimeUtc") or "")
    )


def extract_parse_error(value: dict[str, Any] | None) -> str | None:
    if not isinstance(value, dict):
        return None
    return str(value.get("_parse_error") or "").strip() or None


def classify_missing_process_failure(
    *,
    launch_returncode: int,
    launch_payload: dict[str, Any],
    process_info_parse_error: str | None,
) -> dict[str, str]:
    if process_info_parse_error:
        return {
            "error_kind": "guest-process-probe-parse-error",
            "error": "The guest process probe returned an invalid payload after the smoke window.",
            "recovery_action": "rerun-guest-app-launch-smoke",
            "transport_blocker": "summary-parse-error",
            "guest_health": "unknown",
        }

    if launch_returncode != 0 or launch_payload.get("status") == "error":
        return {
            "error_kind": "guest-app-launch-transport-failed",
            "error": "The guest process launch command did not complete successfully.",
            "recovery_action": "inspect-app-launch",
            "transport_blocker": "guest-app-launch",
            "guest_health": "degraded",
        }

    return {
        "error_kind": "app-launch-failed",
        "error": "RegProbe.App was not running after the smoke window.",
        "recovery_action": "inspect-app-launch",
        "transport_blocker": "guest-app-launch",
        "guest_health": "degraded",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a guest-side RegProbe app launch smoke through qga-exec.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--app-exe", default=r"C:\Tools\AppSmoke\RegProbe.App.exe")
    parser.add_argument(
        "--working-dir",
        default="",
        help="Optional guest working directory for the app process. Use a repo checkout path for Contributor Lab readiness smoke.",
    )
    parser.add_argument("--crash-log-dir", default=crash_log_dir())
    parser.add_argument("--launch-wait-timeout", type=int, default=20)
    parser.add_argument("--linger-seconds", type=int, default=5)
    parser.add_argument("--leave-running", action="store_true")
    parser.add_argument(
        "--app-arg",
        action="append",
        default=[],
        help="Argument to pass to RegProbe.App.exe. Use --app-arg=--contributor-lab for dash-prefixed app args.",
    )
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    baseline_crash = latest_crash_log(repo_root, crash_log_dir=args.crash_log_dir)
    stop_regprobe_app(repo_root)

    launch_returncode, launch_payload = launch_app_process(
        repo_root,
        app_exe=args.app_exe,
        working_dir=args.working_dir or None,
        app_args=args.app_arg,
        wait_timeout=args.launch_wait_timeout,
    )

    time.sleep(args.linger_seconds)
    launch_pid = None
    if isinstance(launch_payload.get("stdout"), str) and launch_payload["stdout"].strip():
        launch_info = parse_nested_stdout_json(launch_payload, context="launch-process")
        launch_info_parse_error = extract_parse_error(launch_info)
        if launch_info_parse_error:
            launch_payload["stdout_parse_error"] = launch_info_parse_error
        elif isinstance(launch_info, dict):
            try:
                launch_pid = int(launch_info.get("Pid"))
            except (ValueError, TypeError) as exc:
                launch_payload["stdout_parse_error"] = str(exc)
                launch_pid = None
    process_info = current_process(repo_root, pid=launch_pid)
    latest_crash = latest_crash_log(repo_root, crash_log_dir=args.crash_log_dir)
    baseline_crash_parse_error = extract_parse_error(baseline_crash)
    latest_crash_parse_error = extract_parse_error(latest_crash)
    process_info_parse_error = extract_parse_error(process_info)
    if baseline_crash_parse_error:
        baseline_crash = None
    if latest_crash_parse_error:
        latest_crash = None
    if process_info_parse_error:
        process_info = None

    summary: dict[str, Any] = {
        "summary_source": "guest-app-launch-smoke",
        "app_exe": args.app_exe,
        "working_dir": args.working_dir,
        "app_args": args.app_arg,
        "crash_log_dir": args.crash_log_dir,
        "launch_wait_timeout": args.launch_wait_timeout,
        "linger_seconds": args.linger_seconds,
        "launch_returncode": launch_returncode,
        "launch_payload": launch_payload,
        "process_info": process_info,
        "baseline_crash_log": baseline_crash,
        "latest_crash_log": latest_crash,
        "baseline_crash_log_parse_error": baseline_crash_parse_error,
        "latest_crash_log_parse_error": latest_crash_parse_error,
        "process_info_parse_error": process_info_parse_error,
        "new_crash_log_detected": crash_log_changed(baseline_crash, latest_crash),
    }

    try:
        if process_info is None:
            summary.update({"status": "error", **classify_missing_process_failure(
                launch_returncode=launch_returncode,
                launch_payload=launch_payload,
                process_info_parse_error=process_info_parse_error,
            )})
            payload = apply_summary_contract(
                summary,
                default_error_kind=str(summary["error_kind"]),
                default_recovery_action=str(summary["recovery_action"]),
                default_transport_blocker=str(summary["transport_blocker"]),
                default_guest_health=str(summary["guest_health"]),
            )
            print(json.dumps(payload, indent=2))
            return 1

        if crash_log_changed(baseline_crash, latest_crash):
            summary.update(
                {
                    "status": "error",
                    "error_kind": "app-startup-crash",
                    "error": "A new crash log was written during the launch smoke window.",
                }
            )
            payload = apply_summary_contract(
                summary,
                default_error_kind="app-startup-crash",
                default_recovery_action="inspect-app-crash-logs",
                default_transport_blocker="app-startup-crash",
                default_guest_health="degraded",
            )
            print(json.dumps(payload, indent=2))
            return 1

        summary["status"] = "ok"
        payload = apply_summary_contract(summary)
        print(json.dumps(payload, indent=2))
        return 0
    finally:
        if not args.leave_running:
            stop_regprobe_app(repo_root)


if __name__ == "__main__":
    raise SystemExit(main())
