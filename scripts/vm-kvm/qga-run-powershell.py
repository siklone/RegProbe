#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import subprocess
import time
from pathlib import Path

from summary_contract_lib import apply_summary_contract


def run_agent_command(domain: str, payload: dict[str, object], *, connect: str, timeout: int) -> dict[str, object]:
    cmd = ["virsh"]
    if connect:
        cmd.extend(["-c", connect])
    cmd.extend(["qemu-agent-command", domain, json.dumps(payload), "--timeout", str(timeout)])
    output = subprocess.check_output(cmd, text=True)
    return json.loads(output)["return"]


def decode_base64_text(value: str | None) -> str:
    if not value:
        return ""
    return base64.b64decode(value).decode("utf-8", errors="replace")


def sha256_path(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        while True:
            chunk = handle.read(1024 * 1024)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def start_guest_exec(
    domain: str,
    path: str,
    arg: list[str],
    *,
    connect: str,
    timeout: int,
    capture_output: bool,
) -> dict[str, object]:
    return run_agent_command(
        domain,
        {
            "execute": "guest-exec",
            "arguments": {
                "path": path,
                "arg": arg,
                "capture-output": capture_output,
            },
        },
        connect=connect,
        timeout=timeout,
    )


def wait_guest_exec(
    domain: str,
    pid: int,
    path: str,
    arg: list[str],
    *,
    connect: str,
    timeout: int,
    wait_timeout: int,
    poll_interval: float,
) -> dict[str, object]:

    deadline = time.time() + wait_timeout
    status: dict[str, object] | None = None
    while time.time() < deadline:
        status = run_agent_command(
            domain,
            {"execute": "guest-exec-status", "arguments": {"pid": pid}},
            connect=connect,
            timeout=timeout,
        )
        if status.get("exited"):
            break
        time.sleep(poll_interval)

    if not status or not status.get("exited"):
        return apply_summary_contract(
            {
                "status": "timeout",
                "pid": pid,
                "path": path,
                "arg": arg,
                "wait_timeout": wait_timeout,
                "summary_source": "qga-guest-exec-timeout",
            },
            default_error_kind="guest-exec-timeout",
            default_recovery_action="rerun-qga-powershell",
            default_transport_blocker="timeout",
            default_guest_health="unknown",
        )

    return {
        "status": "exited",
        "pid": pid,
        "path": path,
        "arg": arg,
        "exitcode": status.get("exitcode"),
        "stdout": decode_base64_text(status.get("out-data") if isinstance(status.get("out-data"), str) else None),
        "stderr": decode_base64_text(status.get("err-data") if isinstance(status.get("err-data"), str) else None),
    }


def guest_exec(
    domain: str,
    path: str,
    arg: list[str],
    *,
    connect: str,
    timeout: int,
    wait_timeout: int,
    poll_interval: float,
    capture_output: bool,
) -> dict[str, object]:
    started = start_guest_exec(
        domain,
        path,
        arg,
        connect=connect,
        timeout=timeout,
        capture_output=capture_output,
    )
    return wait_guest_exec(
        domain,
        int(started["pid"]),
        path,
        arg,
        connect=connect,
        timeout=timeout,
        wait_timeout=wait_timeout,
        poll_interval=poll_interval,
    )


def powershell_quote(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def ensure_guest_directory(
    domain: str,
    guest_dir: str,
    *,
    connect: str,
    timeout: int,
    wait_timeout: int,
    poll_interval: float,
) -> dict[str, object]:
    command = f"New-Item -ItemType Directory -Force -Path {powershell_quote(guest_dir)} | Out-Null"
    return guest_exec(
        domain,
        "powershell.exe",
        ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command],
        connect=connect,
        timeout=timeout,
        wait_timeout=wait_timeout,
        poll_interval=poll_interval,
        capture_output=True,
    )


def remove_guest_path(
    domain: str,
    guest_path: str,
    *,
    connect: str,
    timeout: int,
    wait_timeout: int,
    poll_interval: float,
) -> dict[str, object]:
    command = (
        f"if (Test-Path -LiteralPath {powershell_quote(guest_path)}) "
        f"{{ Remove-Item -LiteralPath {powershell_quote(guest_path)} -Force }}"
    )
    return guest_exec(
        domain,
        "powershell.exe",
        ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command],
        connect=connect,
        timeout=timeout,
        wait_timeout=wait_timeout,
        poll_interval=poll_interval,
        capture_output=True,
    )


def upload_guest_file(
    domain: str,
    source: Path,
    destination: str,
    *,
    connect: str,
    timeout: int,
    chunk_size: int,
) -> dict[str, object]:
    opened = run_agent_command(
        domain,
        {
            "execute": "guest-file-open",
            "arguments": {
                "path": destination,
                "mode": "wb",
            },
        },
        connect=connect,
        timeout=timeout,
    )
    handle = int(opened)

    written_total = 0
    try:
        with source.open("rb") as infile:
            while True:
                chunk = infile.read(chunk_size)
                if not chunk:
                    break
                result = run_agent_command(
                    domain,
                    {
                        "execute": "guest-file-write",
                        "arguments": {
                            "handle": handle,
                            "buf-b64": base64.b64encode(chunk).decode("ascii"),
                        },
                    },
                    connect=connect,
                    timeout=timeout,
                )
                written_total += int(result.get("count", 0))

        run_agent_command(
            domain,
            {"execute": "guest-file-flush", "arguments": {"handle": handle}},
            connect=connect,
            timeout=timeout,
        )
    finally:
        run_agent_command(
            domain,
            {"execute": "guest-file-close", "arguments": {"handle": handle}},
            connect=connect,
            timeout=timeout,
        )

    return {
        "source": str(source),
        "destination": destination,
        "size_bytes": source.stat().st_size,
        "written_bytes": written_total,
        "sha256": sha256_path(source),
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Upload a local PowerShell script into the KVM guest through qemu guest agent and execute it."
    )
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="")
    parser.add_argument("--script", required=True, help="Host PowerShell script path.")
    parser.add_argument("--guest-dir", default=r"C:\RegProbe-Diag\staging")
    parser.add_argument("--guest-script-path", default="", help="Optional explicit guest script path.")
    parser.add_argument("--powershell-path", default="powershell.exe")
    parser.add_argument("--ps-arg", action="append", default=[], help="PowerShell script argument. Repeat for multiple args.")
    parser.add_argument("--timeout", type=int, default=10)
    parser.add_argument("--wait-timeout", type=int, default=900)
    parser.add_argument("--poll-interval", type=float, default=1.0)
    parser.add_argument("--chunk-size", type=int, default=32768)
    parser.add_argument("--keep", action="store_true", help="Keep the uploaded guest script after execution.")
    parser.add_argument("--no-wait", action="store_true", help="Start the guest script and return immediately without waiting for completion.")
    parser.add_argument("--propagate-exit-code", action="store_true")
    args = parser.parse_args()

    source = Path(args.script).resolve()
    if not source.is_file():
        raise FileNotFoundError(f"Host script not found: {source}")
    if args.no_wait and args.propagate_exit_code:
        parser.error("--propagate-exit-code requires waiting for the guest process to exit")

    guest_script_path = (args.guest_script_path or (args.guest_dir.rstrip("\\") + "\\" + source.name)).replace("/", "\\")
    guest_dir = guest_script_path.rsplit("\\", 1)[0] if "\\" in guest_script_path else args.guest_dir

    ensure_result = ensure_guest_directory(
        args.domain,
        guest_dir,
        connect=args.connect,
        timeout=args.timeout,
        wait_timeout=max(args.wait_timeout, 60),
        poll_interval=args.poll_interval,
    )
    ensure_exitcode = ensure_result.get("exitcode")
    if ensure_result.get("status") != "exited" or (ensure_exitcode is None or int(ensure_exitcode) != 0):
        print(
            json.dumps(
                apply_summary_contract(
                    {
                        "status": "error",
                        "summary_source": "qga-ensure-guest-dir-error",
                        "domain": args.domain,
                        "guest_dir": guest_dir,
                        "result": ensure_result,
                    },
                    default_error_kind="guest-dir-ensure-failed",
                    default_recovery_action="rerun-qga-powershell",
                    default_transport_blocker="guest-dir-ensure",
                    default_guest_health="unknown",
                ),
                indent=2,
            )
        )
        return 1

    upload_result = upload_guest_file(
        args.domain,
        source,
        guest_script_path,
        connect=args.connect,
        timeout=args.timeout,
        chunk_size=args.chunk_size,
    )

    exec_args = ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", guest_script_path] + args.ps_arg
    started = start_guest_exec(
        args.domain,
        args.powershell_path,
        exec_args,
        connect=args.connect,
        timeout=args.timeout,
        capture_output=not args.no_wait,
    )
    pid = int(started["pid"])

    if args.no_wait:
        result = {
            "status": "started",
            "domain": args.domain,
            "guest_dir": guest_dir,
            "guest_script_path": guest_script_path,
            "upload": upload_result,
            "execution": {
                "status": "started",
                "pid": pid,
                "path": args.powershell_path,
                "arg": exec_args,
            },
            "cleanup": {
                "status": "skipped",
                "reason": "no-wait-launch",
            },
        }
        print(json.dumps(result, indent=2))
        return 0

    exec_result = wait_guest_exec(
        args.domain,
        pid,
        args.powershell_path,
        exec_args,
        connect=args.connect,
        timeout=args.timeout,
        wait_timeout=args.wait_timeout,
        poll_interval=args.poll_interval,
    )

    cleanup_result = None
    if not args.keep:
        cleanup_result = remove_guest_path(
            args.domain,
            guest_script_path,
            connect=args.connect,
            timeout=args.timeout,
            wait_timeout=max(args.wait_timeout, 60),
            poll_interval=args.poll_interval,
        )

    result = {
        "status": "completed" if exec_result.get("status") == "exited" else exec_result.get("status"),
        "domain": args.domain,
        "guest_dir": guest_dir,
        "guest_script_path": guest_script_path,
        "upload": upload_result,
        "execution": exec_result,
        "cleanup": cleanup_result,
    }
    print(json.dumps(result, indent=2))

    if args.propagate_exit_code and exec_result.get("status") == "exited":
        return int(exec_result.get("exitcode") or 0)
    if exec_result.get("status") != "exited":
        return 124
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
