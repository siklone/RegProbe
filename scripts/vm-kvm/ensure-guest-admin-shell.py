#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

from guest_bridge import ensure_guest_bridge


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


def send_key(connect: str, domain: str, *keys: str) -> None:
    subprocess.run(
        ["virsh", "-c", connect, "send-key", domain, *keys],
        check=True,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )


def type_text(
    repo_root: Path,
    domain: str,
    connect: str,
    text: str,
    *,
    delay_ms: str,
    wake_key: str = "",
    press_enter: bool = False,
) -> None:
    cmd = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "type-to-guest.py"),
        domain,
        "--connect",
        connect,
        "--delay-ms",
        delay_ms,
    ]
    if wake_key:
        cmd.extend(["--wake-key", wake_key])
    if press_enter:
        cmd.append("--enter")
    cmd.append(text)
    run(cmd, cwd=repo_root)


def main() -> int:
    parser = argparse.ArgumentParser(description="Best-effort host-side helper that reopens an elevated PowerShell session in the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--bridge-base-url", default="http://10.0.2.2:8766")
    parser.add_argument("--upload-dir", default="/tmp/regprobe-bridge")
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--launch-delay-seconds", type=float, default=1.2)
    parser.add_argument("--uac-delay-seconds", type=float, default=1.6)
    parser.add_argument("--timeout-seconds", type=int, default=45)
    parser.add_argument("--marker-name", default="guest-admin-shell-ready")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    upload_dir.mkdir(parents=True, exist_ok=True)
    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)

    bridge = args.bridge_base_url.rstrip("/")
    marker_file = upload_dir / f"{args.marker_name}.txt"
    if marker_file.exists():
        marker_file.unlink()

    elevate_command = "powershell -c Start-Process powershell -Verb RunAs"
    ready_command = (
        f"Invoke-WebRequest -UseBasicParsing -Method Put -Uri '{bridge}/{marker_file.name}' -Body 'ready'|Out-Null"
    )

    send_key(args.connect, args.domain, "KEY_ESC")
    time.sleep(float(args.delay_ms) / 1000.0)
    send_key(args.connect, args.domain, "KEY_LEFTMETA", "KEY_R")
    time.sleep(args.launch_delay_seconds)
    type_text(repo_root, args.domain, args.connect, elevate_command, delay_ms=args.delay_ms, press_enter=True)

    time.sleep(args.uac_delay_seconds)
    send_key(args.connect, args.domain, "KEY_LEFT")
    time.sleep(0.2)
    send_key(args.connect, args.domain, "KEY_ENTER")
    time.sleep(max(args.launch_delay_seconds, 1.0))
    type_text(repo_root, args.domain, args.connect, ready_command, delay_ms=args.delay_ms, press_enter=True)

    deadline = time.time() + args.timeout_seconds
    while time.time() < deadline:
        if marker_file.exists():
            payload = {
                "marker_path": str(marker_file),
                "marker_name": marker_file.name,
                "status": "ready",
            }
            print(json.dumps(payload, indent=2))
            return 0
        time.sleep(1)

    print(
        json.dumps(
            {
                "marker_path": str(marker_file),
                "marker_name": marker_file.name,
                "status": "timeout",
            },
            indent=2,
        )
    )
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
