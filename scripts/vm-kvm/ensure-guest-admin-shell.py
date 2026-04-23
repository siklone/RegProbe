#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

from summary_contract_lib import apply_summary_contract

from guest_bridge import ensure_guest_bridge
from vm_env import bridge_base_url, upload_dir as default_upload_dir, vm_connect, vm_domain


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


def annotate_process_error(exc: subprocess.CalledProcessError, *, stage: str) -> subprocess.CalledProcessError:
    setattr(exc, "stage", stage)
    return exc


def format_process_error(error: Exception) -> str:
    if not isinstance(error, subprocess.CalledProcessError):
        return str(error)
    details = [f"command exited with code {error.returncode}"]
    stdout = (error.output or "").strip()
    stderr = (error.stderr or "").strip()
    if stdout:
        details.append(f"stdout: {stdout}")
    if stderr:
        details.append(f"stderr: {stderr}")
    return " | ".join(details)


def send_key(connect: str, domain: str, *keys: str) -> None:
    try:
        subprocess.run(
            ["virsh", "-c", connect, "send-key", domain, *keys],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
    except subprocess.CalledProcessError as exc:
        raise annotate_process_error(exc, stage=f"send-key:{'+'.join(keys)}") from exc


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
    try:
        run(cmd, cwd=repo_root)
    except subprocess.CalledProcessError as exc:
        stage = "type-to-guest-enter" if press_enter else "type-to-guest"
        raise annotate_process_error(exc, stage=stage) from exc


def print_error_payload(
    *,
    domain: str,
    marker_name: str,
    bridge_base_url: str,
    error: Exception,
) -> None:
    print(
        json.dumps(
            apply_summary_contract(
                {
                    "status": "error",
                    "summary_source": "guest-admin-shell-launch-error",
                    "domain": domain,
                    "marker_name": marker_name,
                    "bridge_base_url": bridge_base_url,
                    "message": format_process_error(error),
                    "exception_type": type(error).__name__,
                    "host_step": getattr(error, "stage", None),
                    "exit_code": error.returncode if isinstance(error, subprocess.CalledProcessError) else None,
                    "command": [str(part) for part in error.cmd] if isinstance(error, subprocess.CalledProcessError) and isinstance(error.cmd, list) else (str(error.cmd) if isinstance(error, subprocess.CalledProcessError) else None),
                },
                default_error_kind="guest-admin-shell-launch-error",
                default_recovery_action="rerun-admin-shell-recovery",
                default_transport_blocker="host-launch-error",
                default_guest_health="unknown",
            ),
            indent=2,
        )
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Best-effort host-side helper that reopens an elevated PowerShell session in the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--bridge-base-url", default=bridge_base_url("http://10.0.2.2:8766"))
    parser.add_argument("--upload-dir", default=default_upload_dir("/tmp/regprobe-bridge"))
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--launch-delay-seconds", type=float, default=1.2)
    parser.add_argument("--uac-delay-seconds", type=float, default=1.6)
    parser.add_argument("--timeout-seconds", type=int, default=45)
    parser.add_argument("--marker-name", default="guest-admin-shell-ready")
    args = parser.parse_args()
    try:
        repo_root = Path(args.repo_root).resolve()
        upload_dir = Path(args.upload_dir).resolve()
        upload_dir.mkdir(parents=True, exist_ok=True)
        ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)

        bridge = args.bridge_base_url.rstrip("/")
        marker_file = upload_dir / f"{args.marker_name}.txt"
        if marker_file.exists():
            marker_file.unlink()

        elevate_command = "powershell -c Start-Process powershell -Verb RunAs"
        ready_command = f"iwr -UseBasicParsing -Method Put -Uri '{bridge}/{marker_file.name}' -Body 'ready'|Out-Null"

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

        send_key(args.connect, args.domain, "KEY_ESC")
        time.sleep(float(args.delay_ms) / 1000.0)
        send_key(args.connect, args.domain, "KEY_LEFTMETA", "KEY_R")
        time.sleep(max(args.launch_delay_seconds, 1.5))
        type_text(repo_root, args.domain, args.connect, elevate_command, delay_ms=args.delay_ms, press_enter=True)

        time.sleep(max(args.uac_delay_seconds, 2.0))
        send_key(args.connect, args.domain, "KEY_LEFT")
        time.sleep(0.2)
        send_key(args.connect, args.domain, "KEY_ENTER")
        time.sleep(max(args.launch_delay_seconds, 1.5))
        type_text(repo_root, args.domain, args.connect, ready_command, delay_ms=args.delay_ms, press_enter=True)

        retry_deadline = time.time() + 20
        while time.time() < retry_deadline:
            if marker_file.exists():
                payload = apply_summary_contract({
                    "marker_path": str(marker_file),
                    "marker_name": marker_file.name,
                    "status": "ready-via-retry",
                })
                print(json.dumps(payload, indent=2))
                return 0
            time.sleep(1)

        send_key(args.connect, args.domain, "KEY_ESC")
        time.sleep(float(args.delay_ms) / 1000.0)
        type_text(repo_root, args.domain, args.connect, ready_command, delay_ms=args.delay_ms, press_enter=True)

        fallback_deadline = time.time() + 10
        while time.time() < fallback_deadline:
            if marker_file.exists():
                payload = apply_summary_contract({
                    "marker_path": str(marker_file),
                    "marker_name": marker_file.name,
                    "status": "ready-via-fallback",
                })
                print(json.dumps(payload, indent=2))
                return 0
            time.sleep(1)

        print(
            json.dumps(
                apply_summary_contract(
                    {
                        "marker_path": str(marker_file),
                        "marker_name": marker_file.name,
                        "status": "timeout",
                    },
                    default_error_kind="runner-timeout",
                    default_recovery_action="rerun-admin-shell-recovery",
                    default_transport_blocker="timeout",
                    default_guest_health="unknown",
                ),
                indent=2,
            )
        )
        return 2
    except Exception as error:  # pragma: no cover - exercised via CLI-facing tests
        print_error_payload(
            domain=args.domain,
            marker_name=args.marker_name,
            bridge_base_url=args.bridge_base_url,
            error=error,
        )
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
