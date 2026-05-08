#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import time
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from vm_env import vm_connect, vm_domain


REPO_ROOT = Path(__file__).resolve().parents[2]


def slug(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9_.-]+", "-", value).strip("-").lower() or "registry-value"


def run(cmd: list[str], *, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(REPO_ROOT), capture_output=True, text=True, timeout=timeout)


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def write_guest_stage_script(path: Path) -> None:
    path.write_text(
        r'''
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('apply', 'post-reboot-rollback', 'post-rollback')]
    [string]$Stage,

    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [string]$ValueData,

    [Parameter(Mandatory = $true)]
    [string]$ExperimentId
)

$ErrorActionPreference = 'Stop'
$stateRoot = Join-Path 'C:\RegProbe-Diag\registry-value-experiments' $ExperimentId
$statePath = Join-Path $stateRoot 'state.json'
New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null

function Convert-ToPsPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    return ($Path -replace '^HKLM\\', 'HKLM:\')
}

function Read-RegistryValue {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $psPath = Convert-ToPsPath -Path $Path
    $row = [ordered]@{
        path = $Path
        value_name = $Name
        key_exists = $false
        value_exists = $false
        value = $null
        value_kind = $null
        value_hex = $null
        status = 'not-read'
        error = $null
    }

    try {
        if (-not (Test-Path -LiteralPath $psPath)) {
            $row.status = 'key-missing'
            return [pscustomobject]$row
        }

        $row.key_exists = $true
        $props = Get-ItemProperty -LiteralPath $psPath -ErrorAction Stop
        $prop = $props.PSObject.Properties[$Name]
        if ($null -eq $prop) {
            $row.status = 'value-missing'
            return [pscustomobject]$row
        }

        $row.value_exists = $true
        $row.value = $prop.Value
        $row.value_kind = if ($null -eq $prop.Value) { 'null' } else { $prop.Value.GetType().FullName }
        if ($prop.Value -is [int] -or $prop.Value -is [long] -or $prop.Value -is [uint32] -or $prop.Value -is [uint64]) {
            $row.value_hex = ('0x{0:x}' -f ([int64]$prop.Value))
        }
        $row.status = 'value-present'
        return [pscustomobject]$row
    }
    catch {
        $row.status = 'error'
        $row.error = $_.Exception.Message
        return [pscustomobject]$row
    }
}

function Set-DwordValue {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][int]$Data
    )

    $psPath = Convert-ToPsPath -Path $Path
    New-Item -Path $psPath -Force | Out-Null
    New-ItemProperty -Path $psPath -Name $Name -PropertyType DWord -Value $Data -Force | Out-Null
}

function Restore-RegistryValue {
    param(
        [Parameter(Mandatory = $true)]$State
    )

    $psPath = Convert-ToPsPath -Path ([string]$State.registry_path)
    if ($State.original.key_exists -and $State.original.value_exists) {
        New-Item -Path $psPath -Force | Out-Null
        New-ItemProperty -Path $psPath -Name ([string]$State.value_name) -PropertyType DWord -Value ([int]$State.original.value) -Force | Out-Null
        return 'restored-original-value'
    }

    if ($State.original.key_exists -and -not $State.original.value_exists) {
        if (Test-Path -LiteralPath $psPath) {
            Remove-ItemProperty -Path $psPath -Name ([string]$State.value_name) -Force -ErrorAction SilentlyContinue
        }
        return 'removed-created-value'
    }

    if (-not $State.original.key_exists) {
        if (Test-Path -LiteralPath $psPath) {
            Remove-ItemProperty -Path $psPath -Name ([string]$State.value_name) -Force -ErrorAction SilentlyContinue
        }
        return 'removed-created-value-key-left-in-place'
    }

    return 'restore-noop'
}

function Invoke-ProcessSmoke {
    $items = New-Object System.Collections.Generic.List[object]

    function Add-SmokeResult {
        param([string]$Name, [bool]$Success, [string]$Detail)
        $items.Add([pscustomobject]@{
            name = $Name
            success = $Success
            detail = $Detail
        }) | Out-Null
    }

    try {
        $processNames = @('explorer', 'sihost', 'StartMenuExperienceHost', 'ShellExperienceHost', 'SearchHost')
        $running = @{}
        foreach ($name in $processNames) {
            $running[$name] = [bool](Get-Process -Name $name -ErrorAction SilentlyContinue)
        }
        Add-SmokeResult -Name 'shell-process-presence' -Success ([bool]$running['explorer']) -Detail (($running.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ';')
    }
    catch {
        Add-SmokeResult -Name 'shell-process-presence' -Success $false -Detail $_.Exception.Message
    }

    foreach ($probe in @(
        @{ name = 'cmd-ver'; file = "$env:SystemRoot\System32\cmd.exe"; args = '/c ver'; wait = $true },
        @{ name = 'powershell-version'; file = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"; args = '-NoProfile -Command "$PSVersionTable.PSVersion.ToString()"'; wait = $true },
        @{ name = 'notepad-x64-launch'; file = "$env:SystemRoot\System32\notepad.exe"; args = ''; wait = $false },
        @{ name = 'notepad-x86-launch'; file = "$env:SystemRoot\SysWOW64\notepad.exe"; args = ''; wait = $false },
        @{ name = 'calc-launch'; file = "$env:SystemRoot\System32\calc.exe"; args = ''; wait = $false }
    )) {
        try {
            if (-not (Test-Path -LiteralPath $probe.file)) {
                Add-SmokeResult -Name $probe.name -Success $false -Detail "missing: $($probe.file)"
                continue
            }

            if ($probe.wait) {
                if ([string]::IsNullOrWhiteSpace([string]$probe.args)) {
                    $proc = Start-Process -FilePath $probe.file -PassThru -WindowStyle Hidden
                }
                else {
                    $proc = Start-Process -FilePath $probe.file -ArgumentList $probe.args -PassThru -WindowStyle Hidden
                }
                if (-not $proc.WaitForExit(15000)) {
                    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                    Add-SmokeResult -Name $probe.name -Success $false -Detail 'timeout'
                }
                else {
                    Add-SmokeResult -Name $probe.name -Success ($proc.ExitCode -eq 0) -Detail "exit=$($proc.ExitCode)"
                }
            }
            else {
                if ([string]::IsNullOrWhiteSpace([string]$probe.args)) {
                    $proc = Start-Process -FilePath $probe.file -PassThru
                }
                else {
                    $proc = Start-Process -FilePath $probe.file -ArgumentList $probe.args -PassThru
                }
                Start-Sleep -Seconds 2
                $alive = [bool](Get-Process -Id $proc.Id -ErrorAction SilentlyContinue)
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                Add-SmokeResult -Name $probe.name -Success ([bool]$proc) -Detail "started=$([bool]$proc);alive_after_2s=$alive"
            }
        }
        catch {
            Add-SmokeResult -Name $probe.name -Success $false -Detail $_.Exception.Message
        }
    }

    foreach ($uriProbe in @(
        @{ name = 'settings-uri-launch'; uri = 'ms-settings:' },
        @{ name = 'store-uri-launch'; uri = 'ms-windows-store:' }
    )) {
        try {
            Start-Process -FilePath $uriProbe.uri -ErrorAction Stop
            Start-Sleep -Seconds 3
            Add-SmokeResult -Name $uriProbe.name -Success $true -Detail 'launch-command-succeeded'
        }
        catch {
            Add-SmokeResult -Name $uriProbe.name -Success $false -Detail $_.Exception.Message
        }
    }

    $failed = @($items | Where-Object { -not $_.success })
    return [pscustomobject]@{
        success = ($failed.Count -eq 0)
        failure_count = $failed.Count
        items = $items
    }
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    stage = $Stage
    experiment_id = $ExperimentId
    registry_path = $RegistryPath
    value_name = $ValueName
    value_data = $ValueData
    status = 'ok'
    error = $null
}

try {
    if ($Stage -eq 'apply') {
        $original = Read-RegistryValue -Path $RegistryPath -Name $ValueName
        $state = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
            experiment_id = $ExperimentId
            registry_path = $RegistryPath
            value_name = $ValueName
            value_data = [int]$ValueData
            original = $original
        }
        $state | ConvertTo-Json -Depth 8 | Set-Content -Path $statePath -Encoding UTF8
        Set-DwordValue -Path $RegistryPath -Name $ValueName -Data ([int]$ValueData)
        $result.original = $original
        $result.after_apply = Read-RegistryValue -Path $RegistryPath -Name $ValueName
        $result.smoke = Invoke-ProcessSmoke
    }
    elseif ($Stage -eq 'post-reboot-rollback') {
        if (-not (Test-Path -LiteralPath $statePath)) {
            throw "Missing experiment state file: $statePath"
        }
        $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
        $result.after_reboot = Read-RegistryValue -Path $RegistryPath -Name $ValueName
        $result.smoke = Invoke-ProcessSmoke
        $result.restore_action = Restore-RegistryValue -State $state
        $result.after_restore = Read-RegistryValue -Path $RegistryPath -Name $ValueName
    }
    elseif ($Stage -eq 'post-rollback') {
        $result.final = Read-RegistryValue -Path $RegistryPath -Name $ValueName
        $result.smoke = Invoke-ProcessSmoke
    }
}
catch {
    $result.status = 'error'
    $result.error = $_.Exception.Message
}

$result | ConvertTo-Json -Depth 12
if ($result.status -ne 'ok') {
    exit 1
}
'''.lstrip(),
        encoding="utf-8",
    )


def run_guest_stage(
    *,
    script: Path,
    stage: str,
    registry_path: str,
    value_name: str,
    value_data: int,
    experiment_id: str,
    domain: str,
    connect: str,
    wait_timeout: int,
) -> tuple[int, dict[str, Any], dict[str, Any] | None]:
    cmd = [
        sys.executable,
        str(REPO_ROOT / "scripts" / "vm-kvm" / "qga-run-powershell.py"),
        "--domain",
        domain,
        "--connect",
        connect,
        "--script",
        str(script),
        "--guest-dir",
        rf"C:\RegProbe-Diag\registry-value-experiments\{experiment_id}",
        "--wait-timeout",
        str(wait_timeout),
        "--keep",
        "--ps-arg=-Stage",
        f"--ps-arg={stage}",
        "--ps-arg=-RegistryPath",
        f"--ps-arg={registry_path}",
        "--ps-arg=-ValueName",
        f"--ps-arg={value_name}",
        "--ps-arg=-ValueData",
        f"--ps-arg={value_data}",
        "--ps-arg=-ExperimentId",
        f"--ps-arg={experiment_id}",
    ]
    completed = run(cmd, timeout=wait_timeout + 60)
    try:
        payload = json.loads(completed.stdout)
    except json.JSONDecodeError:
        payload = {
            "status": "parse-error",
            "stdout": completed.stdout,
            "stderr": completed.stderr,
            "returncode": completed.returncode,
        }
        return completed.returncode, payload, None

    stdout = ((payload.get("execution") or {}).get("stdout") if isinstance(payload.get("execution"), dict) else None) or ""
    stage_payload = None
    if isinstance(stdout, str) and stdout.strip():
        try:
            stage_payload = json.loads(stdout)
        except json.JSONDecodeError:
            stage_payload = {"status": "parse-error", "stdout": stdout}
    return completed.returncode, payload, stage_payload


def wait_for_qga(domain: str, connect: str, timeout_seconds: int) -> dict[str, Any]:
    deadline = time.time() + timeout_seconds
    last_payload: dict[str, Any] | None = None
    while time.time() < deadline:
        completed = run(
            [
                sys.executable,
                str(REPO_ROOT / "scripts" / "vm-kvm" / "vm-health-check.py"),
                "--domain",
                domain,
                "--connect",
                connect,
                "--json",
            ],
            timeout=30,
        )
        try:
            payload = json.loads(completed.stdout)
        except json.JSONDecodeError:
            payload = {"status": "parse-error", "stdout": completed.stdout, "stderr": completed.stderr}
        last_payload = payload
        if completed.returncode == 0 and payload.get("status") == "ok":
            return payload
        time.sleep(5)
    return last_payload or {"status": "timeout"}


def reboot_guest(domain: str, connect: str) -> dict[str, Any]:
    completed = run(["virsh", "-c", connect, "reboot", domain], timeout=30)
    return {
        "returncode": completed.returncode,
        "stdout": completed.stdout,
        "stderr": completed.stderr,
    }


def list_domain_snapshots(domain: str, connect: str) -> dict[str, Any]:
    completed = run(["virsh", "-c", connect, "snapshot-list", domain, "--name"], timeout=30)
    snapshots = [line.strip() for line in completed.stdout.splitlines() if line.strip()]
    return {
        "returncode": completed.returncode,
        "snapshots": snapshots,
        "stderr": completed.stderr.strip(),
    }


def smoke_success(stage_payload: dict[str, Any] | None) -> bool:
    if not isinstance(stage_payload, dict) or stage_payload.get("status") != "ok":
        return False
    smoke = stage_payload.get("smoke")
    if not isinstance(smoke, dict):
        return False
    # URI app launches are best-effort under QGA/SYSTEM. Treat command/process smokes as hard checks.
    hard_items = [
        item
        for item in smoke.get("items", [])
        if isinstance(item, dict) and not str(item.get("name", "")).endswith("-uri-launch")
    ]
    return all(bool(item.get("success")) for item in hard_items)


def run_experiment(args: argparse.Namespace) -> dict[str, Any]:
    experiment_id = args.output_name or f"{slug(args.value_name)}-{args.value_data}-{datetime.now(UTC).strftime('%Y%m%d%H%M%S')}"
    generated_dir = REPO_ROOT / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)
    stage_script = generated_dir / f"registry-value-experiment-{experiment_id}.ps1"
    write_guest_stage_script(stage_script)

    result: dict[str, Any] = {
        "generated_utc": now_utc(),
        "status": "running",
        "experiment_id": experiment_id,
        "domain": args.domain,
        "connect": args.connect,
        "registry_path": args.registry_path,
        "value_name": args.value_name,
        "value_data": args.value_data,
        "stages": {},
        "reboots": [],
        "health_checks": [],
    }
    snapshot_state = list_domain_snapshots(args.domain, args.connect)
    result["safety"] = {
        "domain_snapshot_count": len(snapshot_state["snapshots"]),
        "domain_snapshots": snapshot_state["snapshots"],
        "snapshot_check": snapshot_state,
        "warning": None,
    }
    if not snapshot_state["snapshots"]:
        result["safety"]["warning"] = "no-libvirt-domain-snapshot-present"
        if args.require_domain_snapshot:
            result["status"] = "error"
            result["error"] = "missing-required-domain-snapshot"
            return result

    rc, payload, stage_payload = run_guest_stage(
        script=stage_script,
        stage="apply",
        registry_path=args.registry_path,
        value_name=args.value_name,
        value_data=args.value_data,
        experiment_id=experiment_id,
        domain=args.domain,
        connect=args.connect,
        wait_timeout=args.stage_wait_timeout,
    )
    result["stages"]["apply"] = {"returncode": rc, "qga": payload, "result": stage_payload}
    if rc != 0 or not isinstance(stage_payload, dict) or stage_payload.get("status") != "ok":
        result["status"] = "error"
        result["error"] = "apply-stage-failed"
        return result

    result["reboots"].append({"phase": "after-apply", **reboot_guest(args.domain, args.connect)})
    health = wait_for_qga(args.domain, args.connect, args.reboot_wait_timeout)
    result["health_checks"].append({"phase": "after-apply", "payload": health})
    if health.get("status") != "ok":
        result["status"] = "error"
        result["error"] = "guest-did-not-return-after-apply-reboot"
        return result

    rc, payload, stage_payload = run_guest_stage(
        script=stage_script,
        stage="post-reboot-rollback",
        registry_path=args.registry_path,
        value_name=args.value_name,
        value_data=args.value_data,
        experiment_id=experiment_id,
        domain=args.domain,
        connect=args.connect,
        wait_timeout=args.stage_wait_timeout,
    )
    result["stages"]["post_reboot_rollback"] = {"returncode": rc, "qga": payload, "result": stage_payload}
    if rc != 0 or not isinstance(stage_payload, dict) or stage_payload.get("status") != "ok":
        result["status"] = "error"
        result["error"] = "post-reboot-rollback-stage-failed"
        return result

    result["reboots"].append({"phase": "after-rollback", **reboot_guest(args.domain, args.connect)})
    health = wait_for_qga(args.domain, args.connect, args.reboot_wait_timeout)
    result["health_checks"].append({"phase": "after-rollback", "payload": health})
    if health.get("status") != "ok":
        result["status"] = "error"
        result["error"] = "guest-did-not-return-after-rollback-reboot"
        return result

    rc, payload, stage_payload = run_guest_stage(
        script=stage_script,
        stage="post-rollback",
        registry_path=args.registry_path,
        value_name=args.value_name,
        value_data=args.value_data,
        experiment_id=experiment_id,
        domain=args.domain,
        connect=args.connect,
        wait_timeout=args.stage_wait_timeout,
    )
    result["stages"]["post_rollback"] = {"returncode": rc, "qga": payload, "result": stage_payload}
    if rc != 0 or not isinstance(stage_payload, dict) or stage_payload.get("status") != "ok":
        result["status"] = "error"
        result["error"] = "post-rollback-stage-failed"
        return result

    result["status"] = "ok"
    result["smoke"] = {
        "apply_smoke_hard_success": smoke_success(result["stages"]["apply"].get("result")),
        "post_reboot_smoke_hard_success": smoke_success(result["stages"]["post_reboot_rollback"].get("result")),
        "post_rollback_smoke_hard_success": smoke_success(result["stages"]["post_rollback"].get("result")),
    }
    return result


def write_markdown(summary: dict[str, Any], path: Path) -> None:
    lines = [
        f"# Registry Value Experiment - {summary.get('experiment_id')}",
        "",
        f"- Status: **{summary.get('status')}**",
        f"- Generated UTC: `{summary.get('generated_utc')}`",
        f"- Target: `{summary.get('registry_path')}\\{summary.get('value_name')}`",
        f"- Test value: `{summary.get('value_data')}`",
        "",
        "## Result",
        "",
    ]
    if summary.get("status") != "ok":
        lines.append(f"- Error: `{summary.get('error')}`")
    else:
        smoke = summary.get("smoke") or {}
        for key, value in smoke.items():
            lines.append(f"- `{key}`: `{value}`")

    for name, stage in (summary.get("stages") or {}).items():
        result = stage.get("result") if isinstance(stage, dict) else None
        if not isinstance(result, dict):
            continue
        lines.extend(["", f"## Stage: {name}", ""])
        for key in ("status", "error", "restore_action"):
            if key in result:
                lines.append(f"- `{key}`: `{result.get(key)}`")
        for read_key in ("original", "after_apply", "after_reboot", "after_restore", "final"):
            read = result.get(read_key)
            if isinstance(read, dict):
                lines.append(
                    f"- `{read_key}`: status=`{read.get('status')}`, "
                    f"value_exists=`{read.get('value_exists')}`, value=`{read.get('value')}`"
                )
        smoke = result.get("smoke")
        if isinstance(smoke, dict):
            lines.append(f"- `smoke.failure_count`: `{smoke.get('failure_count')}`")
            for item in smoke.get("items", []):
                if isinstance(item, dict):
                    lines.append(f"- `{item.get('name')}`: `{item.get('success')}` - {item.get('detail')}")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Apply one registry DWORD value in the KVM guest, reboot-smoke it, rollback, and verify recovery.")
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--registry-path", required=True)
    parser.add_argument("--value-name", required=True)
    parser.add_argument("--value-data", required=True, type=int)
    parser.add_argument("--output-name", default="")
    parser.add_argument("--output-dir", default=str(REPO_ROOT / "registry-research-framework" / "audit" / "registry-value-experiments"))
    parser.add_argument("--stage-wait-timeout", type=int, default=180)
    parser.add_argument("--reboot-wait-timeout", type=int, default=240)
    parser.add_argument(
        "--require-domain-snapshot",
        action="store_true",
        help="Fail before applying a value unless a libvirt domain snapshot exists.",
    )
    args = parser.parse_args()

    summary = run_experiment(args)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    experiment_id = str(summary.get("experiment_id") or "registry-value-experiment")
    json_path = output_dir / f"{experiment_id}.json"
    md_path = output_dir / f"{experiment_id}.md"
    json_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    write_markdown(summary, md_path)

    print(json.dumps({"status": summary.get("status"), "json": str(json_path), "markdown": str(md_path), "error": summary.get("error")}, indent=2))
    return 0 if summary.get("status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
