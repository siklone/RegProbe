#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


def main() -> int:
    parser = argparse.ArgumentParser(description="Stage and run run-local-kd-smoke.ps1 inside the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--bridge-base-url", default="http://10.0.2.2:8766")
    parser.add_argument("--upload-dir", default="/tmp/regprobe-bridge")
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--timeout-seconds", type=int, default=240)
    parser.add_argument("--smoke-timeout-seconds", type=int, default=180)
    parser.add_argument("--output-name", required=True)
    parser.add_argument("--query-symbol", default="nt!CmQueryValueKey")
    parser.add_argument("--kd-command", action="append", default=[], help="Optional custom KD command(s) to run instead of the default x <query-symbol> probe.")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)

    guest_scripts_root = args.guest_scripts_root
    bridge = args.bridge_base_url.rstrip("/")
    generated_name = f"guest-local-kd-smoke-{args.output_name}.ps1"
    generated_path = generated_dir / generated_name

    kd_command_args = ""
    if args.kd_command:
        quoted_commands = ", ".join(quote_ps(command) for command in args.kd_command)
        kd_command_args = f" -DebuggerCommands @({quoted_commands})"

    command_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
        (
            f"Invoke-WebRequest -UseBasicParsing -Uri "
            f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-local-kd-smoke.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-local-kd-smoke.ps1')}"
        ),
        (
            f"& {quote_ps(guest_scripts_root + r'\\run-local-kd-smoke.ps1')} "
            f"-OutputName {quote_ps(args.output_name)} "
            f"-UploadBaseUrl {quote_ps(bridge)} "
            f"-TimeoutSeconds {args.smoke_timeout_seconds} "
            f"-QuerySymbol {quote_ps(args.query_symbol)}"
            f"{kd_command_args}"
        ),
    ]
    generated_path.write_text("\n".join(command_lines) + "\n", encoding="utf-8")

    guest_launcher = "\n".join(
        [
            f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
            (
                f"Invoke-WebRequest -UseBasicParsing -Uri "
                f"{quote_ps(bridge + '/dist/kvm-generated/' + generated_name)} "
                f"-OutFile {quote_ps(guest_scripts_root + '\\\\' + generated_name)}"
            ),
            f"powershell -NoProfile -ExecutionPolicy Bypass -File {quote_ps(guest_scripts_root + '\\\\' + generated_name)}",
        ]
    )

    run(
        [
            sys.executable,
            str(repo_root / "scripts" / "vm-kvm" / "type-to-guest.py"),
            args.domain,
            "--connect",
            args.connect,
            "--delay-ms",
            args.delay_ms,
            "--wake-key",
            args.wake_key,
            "--enter",
            guest_launcher,
        ],
        cwd=repo_root,
    )

    deadline = time.time() + args.timeout_seconds
    while time.time() < deadline:
        if summary_path.exists():
            payload = {
                "summary_path": str(summary_path),
                "output_name": args.output_name,
                "query_symbol": args.query_symbol,
            }
            print(json.dumps(payload, indent=2))
            return 0
        time.sleep(2)

    print(
        json.dumps(
            {
                "summary_path": str(summary_path),
                "output_name": args.output_name,
                "query_symbol": args.query_symbol,
                "status": "timeout",
            },
            indent=2,
        )
    )
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
