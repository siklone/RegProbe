#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

from guest_bridge import ensure_guest_bridge
from summary_contract_lib import apply_summary_contract, write_summary_contract


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


def launch_generated_script(
    *,
    repo_root: Path,
    generated_path: Path,
    guest_launcher: str,
    guest_scripts_root: str,
    output_name: str,
    args: argparse.Namespace,
) -> str:
    if args.launch_transport in {"auto", "qga"}:
        qga_cmd = [
            sys.executable,
            str(repo_root / "scripts" / "vm-kvm" / "qga-run-powershell.py"),
            "--domain",
            args.domain,
            "--connect",
            args.connect,
            "--script",
            str(generated_path),
            "--guest-dir",
            guest_scripts_root,
            "--no-wait",
        ]
        qga_result = subprocess.run(qga_cmd, cwd=str(repo_root), capture_output=True, text=True)
        if qga_result.returncode == 0:
            return "qga"
        if args.launch_transport == "qga":
            raise subprocess.CalledProcessError(
                qga_result.returncode,
                qga_cmd,
                output=qga_result.stdout,
                stderr=qga_result.stderr,
            )
        sys.stderr.write(
            f"[run-guest-local-kd-smoke] qga launch failed, falling back to send-key transport for {output_name}.\n"
        )
        if qga_result.stdout:
            sys.stderr.write(qga_result.stdout)
        if qga_result.stderr:
            sys.stderr.write(qga_result.stderr)

    run(
        [
            sys.executable,
            str(repo_root / "scripts" / "vm-kvm" / "ensure-guest-admin-shell.py"),
            "--repo-root",
            str(repo_root),
            "--domain",
            args.domain,
            "--connect",
            args.connect,
            "--bridge-base-url",
            args.bridge_base_url,
            "--upload-dir",
            str(Path(args.upload_dir).resolve()),
            "--guest-scripts-root",
            guest_scripts_root,
            "--delay-ms",
            args.delay_ms,
            "--marker-name",
            f"{output_name}-admin-shell-ready",
        ],
        cwd=repo_root,
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
    return "send-key"


def resolve_trigger_profile(profile: str) -> str:
    if profile == "uuid-rpc-com-burst":
        return """1..80 | ForEach-Object {
    [guid]::NewGuid() | Out-Null
    try { [System.Runtime.InteropServices.Marshal]::GenerateGuidForType([type][string]) | Out-Null } catch {}
    foreach ($progId in 'WScript.Shell','Shell.Application','Scripting.Dictionary') {
        try {
            $obj = New-Object -ComObject $progId
            if ($obj) {
                [void]$obj
            }
        }
        catch {
        }
    }
    try { Get-CimInstance Win32_ComputerSystemProduct | Select-Object -ExpandProperty UUID | Out-Null } catch {}
    try { cmd /c wmic csproduct get uuid >nul 2>nul } catch {}
    Start-Sleep -Milliseconds 150
}"""
    raise ValueError(f"Unsupported trigger profile: {profile}")


def resolve_summary_status(summary: dict[str, object]) -> str:
    status = summary.get("status")
    if isinstance(status, str) and status:
        return status

    attached = bool(summary.get("attached"))
    completed = bool(summary.get("completed"))
    used_custom_commands = bool(summary.get("used_custom_commands"))
    query_symbol_seen = bool(summary.get("query_symbol_seen"))
    if attached and completed and (used_custom_commands or query_symbol_seen):
        return "ok"
    return "unknown"


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
    parser.add_argument("--trigger-command", default="", help="Optional PowerShell command to run while KD is attached.")
    parser.add_argument("--trigger-profile", default="", help="Optional built-in trigger profile name.")
    parser.add_argument("--trigger-delay-seconds", type=int, default=2)
    parser.add_argument("--launch-transport", choices=["auto", "qga", "send-key"], default="auto")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)
    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)
    guest_scripts_root = args.guest_scripts_root

    bridge = args.bridge_base_url.rstrip("/")
    generated_name = f"guest-local-kd-smoke-{args.output_name}.ps1"
    generated_path = generated_dir / generated_name

    if args.trigger_command and args.trigger_profile:
        parser.error("--trigger-command and --trigger-profile are mutually exclusive")

    trigger_command = args.trigger_command
    if args.trigger_profile:
        trigger_command = resolve_trigger_profile(args.trigger_profile)

    kd_command_args = ""
    if args.kd_command:
        quoted_commands = ", ".join(quote_ps(command) for command in args.kd_command)
        kd_command_args = f" -DebuggerCommands @({quoted_commands})"

    trigger_args = ""
    if trigger_command:
        trigger_args = (
            f" -TriggerPowerShellCommand {quote_ps(trigger_command)}"
            f" -TriggerDelaySeconds {args.trigger_delay_seconds}"
        )

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
            f"{trigger_args}"
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

    launcher_transport = launch_generated_script(
        repo_root=repo_root,
        generated_path=generated_path,
        guest_launcher=guest_launcher,
        guest_scripts_root=guest_scripts_root,
        output_name=args.output_name,
        args=args,
    )

    deadline = time.time() + args.timeout_seconds
    while time.time() < deadline:
        if summary_path.exists():
            summary = apply_summary_contract(json.loads(summary_path.read_text(encoding="utf-8-sig")))
            summary_status = resolve_summary_status(summary)
            payload = {
                "summary_path": str(summary_path),
                "output_name": args.output_name,
                "query_symbol": args.query_symbol,
                "launch_transport": launcher_transport,
                "status": summary_status,
                "error_kind": summary.get("error_kind"),
                "recovery_action": summary.get("recovery_action"),
                "transport_blocker": summary.get("transport_blocker"),
                "guest_health": summary.get("guest_health"),
            }
            print(json.dumps(payload, indent=2))
            return 0 if summary_status != "error" else 1
        time.sleep(2)

    timeout_summary = write_summary_contract(
        summary_path,
        {
            "summary_path": str(summary_path),
            "output_name": args.output_name,
            "query_symbol": args.query_symbol,
            "launch_transport": launcher_transport,
            "status": "timeout",
        },
        default_error_kind="runner-timeout",
        default_recovery_action="rerun-local-kd-smoke",
        default_transport_blocker="timeout",
        default_guest_health="unknown",
    )
    print(json.dumps(timeout_summary, indent=2))
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
