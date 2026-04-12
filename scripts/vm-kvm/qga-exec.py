#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import json
import subprocess
import sys
import time


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


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a command in a KVM Windows guest through qemu guest agent guest-exec.")
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="")
    parser.add_argument("--path", required=True, help="Guest executable path, for example cmd.exe or powershell.exe.")
    parser.add_argument("--arg", action="append", default=[], help="Guest executable argument. Repeat for multiple args.")
    parser.add_argument("--timeout", type=int, default=10, help="Timeout for each qemu-agent-command call.")
    parser.add_argument("--wait-timeout", type=int, default=120, help="Maximum seconds to wait for guest process exit.")
    parser.add_argument("--poll-interval", type=float, default=1.0)
    parser.add_argument("--no-capture-output", action="store_true")
    parser.add_argument("--propagate-exit-code", action="store_true")
    args = parser.parse_args()

    start_payload = {
        "execute": "guest-exec",
        "arguments": {
            "path": args.path,
            "arg": args.arg,
            "capture-output": not args.no_capture_output,
        },
    }
    started = run_agent_command(args.domain, start_payload, connect=args.connect, timeout=args.timeout)
    pid = int(started["pid"])

    deadline = time.time() + args.wait_timeout
    status: dict[str, object] | None = None
    while time.time() < deadline:
        status = run_agent_command(
            args.domain,
            {"execute": "guest-exec-status", "arguments": {"pid": pid}},
            connect=args.connect,
            timeout=args.timeout,
        )
        if status.get("exited"):
            break
        time.sleep(args.poll_interval)

    if not status or not status.get("exited"):
        print(
            json.dumps(
                {
                    "status": "timeout",
                    "pid": pid,
                    "path": args.path,
                    "arg": args.arg,
                    "wait_timeout": args.wait_timeout,
                },
                indent=2,
            )
        )
        return 124

    result = {
        "status": "exited",
        "pid": pid,
        "path": args.path,
        "arg": args.arg,
        "exitcode": status.get("exitcode"),
        "stdout": decode_base64_text(status.get("out-data") if isinstance(status.get("out-data"), str) else None),
        "stderr": decode_base64_text(status.get("err-data") if isinstance(status.get("err-data"), str) else None),
    }
    print(json.dumps(result, indent=2))

    if args.propagate_exit_code:
        return int(status.get("exitcode") or 0)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
