#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any

from qga_preflight_lib import run_qga_preflight
from summary_contract_lib import apply_summary_contract
from vm_env import vm_domain


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_INSTALL_DIR = r"C:\Tools\DotNetSDK\8.0.416"
DEFAULT_SDK_VERSION = "8.0.416"
DEFAULT_DESKTOP_RUNTIME_CHANNEL = "8.0"


def write_guest_bootstrap_script(path: Path) -> None:
    path.write_text(
        r"""param(
    [Parameter(Mandatory = $true)]
    [string]$InstallDir,
    [Parameter(Mandatory = $true)]
    [string]$SdkVersion,
    [Parameter(Mandatory = $true)]
    [string]$DesktopRuntimeChannel,
    [string]$DesktopRuntimeVersion = '',
    [string]$InboundRoot = 'C:\Tools\Inbound'
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Get-DesktopRuntimeVersions {
    param([Parameter(Mandatory = $true)][string]$Root)

    $desktopRoot = Join-Path $Root 'shared\Microsoft.WindowsDesktop.App'
    if (-not (Test-Path -LiteralPath $desktopRoot)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $desktopRoot -Directory -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Name |
        Sort-Object -Unique)
}

function Get-CoreRuntimeVersions {
    param([Parameter(Mandatory = $true)][string]$Root)

    $coreRoot = Join-Path $Root 'shared\Microsoft.NETCore.App'
    if (-not (Test-Path -LiteralPath $coreRoot)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $coreRoot -Directory -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Name |
        Sort-Object -Unique)
}

function Invoke-DotnetInstall {
    param(
        [Parameter(Mandatory = $true)][string]$InstallScript,
        [Parameter(Mandatory = $true)][string[]]$InstallArgs
    )

    $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $InstallScript @InstallArgs 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "dotnet-install failed with exit code $exitCode`: $($output -join "`n")"
    }
    return ($output -join "`n")
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    install_dir = $InstallDir
    sdk_version = $SdkVersion
    desktop_runtime_channel = $DesktopRuntimeChannel
    desktop_runtime_version = $DesktopRuntimeVersion
    inbound_root = $InboundRoot
}

try {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    New-Item -ItemType Directory -Path $InboundRoot -Force | Out-Null

    $dotnetInstallScript = Join-Path $InboundRoot 'dotnet-install.ps1'
    if (-not (Test-Path -LiteralPath $dotnetInstallScript)) {
        Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $dotnetInstallScript -UseBasicParsing
    }

    $dotnetExe = Join-Path $InstallDir 'dotnet.exe'
    $result.dotnet_before_exists = [bool](Test-Path -LiteralPath $dotnetExe)
    $result.core_runtime_before = @(Get-CoreRuntimeVersions -Root $InstallDir)
    $result.desktop_runtime_before = @(Get-DesktopRuntimeVersions -Root $InstallDir)

    if (-not (Test-Path -LiteralPath $dotnetExe)) {
        $sdkArgs = @('-Version', $SdkVersion, '-InstallDir', $InstallDir, '-Architecture', 'x64', '-NoPath')
        $result.sdk_install_output_tail = (Invoke-DotnetInstall -InstallScript $dotnetInstallScript -InstallArgs $sdkArgs).Split("`n") |
            Select-Object -Last 20
    }

    $coreRuntimeArgs = @('-Runtime', 'dotnet', '-InstallDir', $InstallDir, '-Architecture', 'x64', '-NoPath')
    if (-not [string]::IsNullOrWhiteSpace($DesktopRuntimeVersion)) {
        $coreRuntimeArgs += @('-Version', $DesktopRuntimeVersion)
    } else {
        $coreRuntimeArgs += @('-Channel', $DesktopRuntimeChannel)
    }
    $result.core_install_output_tail = (Invoke-DotnetInstall -InstallScript $dotnetInstallScript -InstallArgs $coreRuntimeArgs).Split("`n") |
        Select-Object -Last 20

    $desktopRuntimeArgs = @('-Runtime', 'windowsdesktop', '-InstallDir', $InstallDir, '-Architecture', 'x64', '-NoPath')
    if (-not [string]::IsNullOrWhiteSpace($DesktopRuntimeVersion)) {
        $desktopRuntimeArgs += @('-Version', $DesktopRuntimeVersion)
    } else {
        $desktopRuntimeArgs += @('-Channel', $DesktopRuntimeChannel)
    }
    $result.desktop_install_output_tail = (Invoke-DotnetInstall -InstallScript $dotnetInstallScript -InstallArgs $desktopRuntimeArgs).Split("`n") |
        Select-Object -Last 20

    $result.dotnet_path = $dotnetExe
    $result.dotnet_after_exists = [bool](Test-Path -LiteralPath $dotnetExe)
    $result.core_runtime_after = @(Get-CoreRuntimeVersions -Root $InstallDir)
    $result.desktop_runtime_after = @(Get-DesktopRuntimeVersions -Root $InstallDir)
    $result.dotnet_version = if ($result.dotnet_after_exists) { (& $dotnetExe --version | Select-Object -First 1) } else { '' }
    $result.success = $result.dotnet_after_exists -and ($result.core_runtime_after.Count -gt 0) -and ($result.desktop_runtime_after.Count -gt 0)
}
catch {
    $result.success = $false
    $result.error = $_.Exception.Message
    $result.dotnet_path = Join-Path $InstallDir 'dotnet.exe'
    $result.dotnet_after_exists = [bool](Test-Path -LiteralPath $result.dotnet_path)
    $result.core_runtime_after = @(Get-CoreRuntimeVersions -Root $InstallDir)
    $result.desktop_runtime_after = @(Get-DesktopRuntimeVersions -Root $InstallDir)
}

$result | ConvertTo-Json -Depth 8
exit $(if ($result.success) { 0 } else { 1 })
""",
        encoding="utf-8",
    )


def run_json_command(cmd: list[str], *, cwd: Path) -> tuple[int, dict[str, Any]]:
    completed = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
    try:
        payload = json.loads(completed.stdout)
        if not isinstance(payload, dict):
            payload = {"status": "error", "stdout_parse_error": "stdout JSON payload is not an object"}
    except json.JSONDecodeError as exc:
        payload = {"status": "error", "stdout_parse_error": str(exc), "stdout": completed.stdout}
    if completed.stderr.strip():
        payload["stderr"] = completed.stderr.strip()
    return completed.returncode, payload


def parse_guest_bootstrap_result(qga_payload: dict[str, Any]) -> dict[str, Any]:
    execution = qga_payload.get("execution") if isinstance(qga_payload, dict) else None
    stdout = execution.get("stdout") if isinstance(execution, dict) else None
    if not isinstance(stdout, str) or not stdout.strip():
        return {
            "status": "error",
            "error_kind": "missing-guest-bootstrap-json",
            "qga_payload_status": qga_payload.get("status") if isinstance(qga_payload, dict) else None,
            "execution_exit": execution.get("exitcode") if isinstance(execution, dict) else None,
        }
    try:
        payload = json.loads(stdout)
    except json.JSONDecodeError as exc:
        return {
            "status": "error",
            "error_kind": "guest-bootstrap-json-parse-error",
            "parse_error": str(exc),
            "raw_stdout": stdout,
        }
    if not isinstance(payload, dict):
        return {
            "status": "error",
            "error_kind": "guest-bootstrap-json-not-object",
            "raw_stdout": stdout,
        }
    payload["status"] = "PASS" if payload.get("success") is True else "FAIL"
    return payload


def build_qga_runner_command(
    *,
    script_path: Path,
    domain: str,
    connect: str,
    install_dir: str,
    sdk_version: str,
    desktop_runtime_channel: str,
    desktop_runtime_version: str,
    wait_timeout: int,
) -> list[str]:
    cmd = [
        sys.executable,
        str(REPO_ROOT / "scripts" / "vm-kvm" / "qga-run-powershell.py"),
        "--domain",
        domain,
        "--script",
        str(script_path),
        "--guest-dir",
        r"C:\Tools\ValidationController\dotnet-toolchain",
        "--ps-arg",
        install_dir,
        "--ps-arg",
        sdk_version,
        "--ps-arg",
        desktop_runtime_channel,
        "--ps-arg",
        desktop_runtime_version,
        "--wait-timeout",
        str(wait_timeout),
        "--propagate-exit-code",
    ]
    if connect:
        cmd.extend(["--connect", connect])
    return cmd


def main() -> int:
    parser = argparse.ArgumentParser(description="Install portable .NET SDK plus WindowsDesktop runtime inside the KVM guest.")
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--install-dir", default=DEFAULT_INSTALL_DIR)
    parser.add_argument("--sdk-version", default=DEFAULT_SDK_VERSION)
    parser.add_argument("--desktop-runtime-channel", default=DEFAULT_DESKTOP_RUNTIME_CHANNEL)
    parser.add_argument("--desktop-runtime-version", default="", help="Optional exact WindowsDesktop runtime version. Defaults to channel latest.")
    parser.add_argument("--wait-timeout", type=int, default=1800)
    parser.add_argument("--health-timeout", type=int, default=30)
    parser.add_argument("--skip-post-health", action="store_true")
    args = parser.parse_args()

    with tempfile.TemporaryDirectory(prefix="regprobe-dotnet-toolchain-") as temp_dir:
        script_path = Path(temp_dir) / "bootstrap-dotnet-toolchain.ps1"
        write_guest_bootstrap_script(script_path)
        command = build_qga_runner_command(
            script_path=script_path,
            domain=args.domain,
            connect=args.connect,
            install_dir=args.install_dir,
            sdk_version=args.sdk_version,
            desktop_runtime_channel=args.desktop_runtime_channel,
            desktop_runtime_version=args.desktop_runtime_version,
            wait_timeout=args.wait_timeout,
        )
        returncode, qga_payload = run_json_command(command, cwd=REPO_ROOT)
        guest_result = parse_guest_bootstrap_result(qga_payload)
        dotnet_path = args.install_dir.rstrip("\\/") + r"\dotnet.exe"
        post_health: dict[str, Any] | None = None
        if not args.skip_post_health:
            post_health = run_qga_preflight(
                domain=args.domain,
                connect=args.connect,
                timeout=args.health_timeout,
                wait_timeout=args.health_timeout,
                check_guest_dotnet=True,
                guest_dotnet_path=dotnet_path,
            )

    health_ok = post_health is None or post_health.get("status") == "ok"
    guest_ok = guest_result.get("status") == "PASS"
    status = "PASS" if returncode == 0 and guest_ok and health_ok else "FAIL"
    raw_summary = {
        "summary_source": "guest-dotnet-toolchain-bootstrap",
        "status": status,
        "domain": args.domain,
        "connect": args.connect,
        "install_dir": args.install_dir,
        "guest_dotnet_path": dotnet_path,
        "sdk_version": args.sdk_version,
        "desktop_runtime_channel": args.desktop_runtime_channel,
        "desktop_runtime_version": args.desktop_runtime_version,
        "qga_runner_returncode": returncode,
        "qga_payload_status": qga_payload.get("status"),
        "guest_result": guest_result,
        "post_health": post_health,
    }
    if status != "PASS":
        raw_summary.update(
            {
                "error_kind": "guest-dotnet-toolchain-bootstrap-failed",
                "recovery_action": "rerun-guest-dotnet-toolchain-bootstrap",
                "guest_health": "degraded",
            }
        )

    summary = apply_summary_contract(
        raw_summary,
        default_error_kind=None if status == "PASS" else "guest-dotnet-toolchain-bootstrap-failed",
        default_recovery_action="none" if status == "PASS" else "rerun-guest-dotnet-toolchain-bootstrap",
        default_guest_health="stable" if status == "PASS" else "degraded",
    )
    print(json.dumps(summary, indent=2))
    return 0 if status == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
