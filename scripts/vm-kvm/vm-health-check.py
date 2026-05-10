#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path

from qga_preflight_lib import run_qga_preflight
from vm_env import vm_connect, vm_domain


def main() -> int:
    parser = argparse.ArgumentParser(description="Run non-mutating KVM/QGA health checks and print the JSON contract.")
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--timeout", type=int, default=10, help="Per virsh/QGA command timeout in seconds.")
    parser.add_argument("--wait-timeout", type=int, default=30, help="guest-exec-status wait timeout in seconds.")
    parser.add_argument("--poll-interval", type=float, default=1.0)
    parser.add_argument("--json", action="store_true", help="Print JSON. Kept explicit for scripts; JSON is the only output format.")
    parser.add_argument("--output", default=None, help="Optional path to also write the JSON health payload.")
    args = parser.parse_args()

    payload = run_qga_preflight(
        domain=args.domain,
        connect=args.connect,
        timeout=args.timeout,
        wait_timeout=args.wait_timeout,
        poll_interval=args.poll_interval,
    )
    text = json.dumps(payload, indent=2) + "\n"
    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(text, encoding="utf-8")
    print(text, end="")
    return 0 if payload.get("status") == "ok" else 2


if __name__ == "__main__":
    raise SystemExit(main())
