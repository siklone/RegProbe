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

from vm_env import vm_connect, vm_domain, vm_snapshot


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
    [string]$ExperimentId,

    [Parameter(Mandatory = $true)]
    [ValidateSet('none', 'core', 'gui')]
    [string]$SmokeProfile
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
    if (-not (Test-Path -LiteralPath $psPath)) {
        New-Item -Path $psPath -Force | Out-Null
    }

    $props = Get-ItemProperty -LiteralPath $psPath -ErrorAction Stop
    if ($null -eq $props.PSObject.Properties[$Name]) {
        New-ItemProperty -Path $psPath -Name $Name -PropertyType DWord -Value $Data -Force | Out-Null
    }
    else {
        Set-ItemProperty -LiteralPath $psPath -Name $Name -Value $Data
    }
}

function Restore-RegistryValue {
    param(
        [Parameter(Mandatory = $true)]$State
    )

    $psPath = Convert-ToPsPath -Path ([string]$State.registry_path)
    if ($State.original.key_exists -and $State.original.value_exists) {
        if (-not (Test-Path -LiteralPath $psPath)) {
            New-Item -Path $psPath -Force | Out-Null
            New-ItemProperty -Path $psPath -Name ([string]$State.value_name) -PropertyType DWord -Value ([int]$State.original.value) -Force | Out-Null
        }
        else {
            $props = Get-ItemProperty -LiteralPath $psPath -ErrorAction Stop
            if ($null -eq $props.PSObject.Properties[([string]$State.value_name)]) {
                New-ItemProperty -Path $psPath -Name ([string]$State.value_name) -PropertyType DWord -Value ([int]$State.original.value) -Force | Out-Null
            }
            else {
                Set-ItemProperty -LiteralPath $psPath -Name ([string]$State.value_name) -Value ([int]$State.original.value)
            }
        }
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
            try {
                $key = Get-Item -LiteralPath $psPath -ErrorAction Stop
                $valueNames = @($key.GetValueNames())
                $subKeyNames = @($key.GetSubKeyNames())
                if ($valueNames.Count -eq 0 -and $subKeyNames.Count -eq 0) {
                    Remove-Item -LiteralPath $psPath -Force -ErrorAction Stop
                    return 'removed-created-key'
                }
            }
            catch {
                return "removed-created-value-key-cleanup-error: $($_.Exception.Message)"
            }
        }
        return 'removed-created-value-key-retained'
    }

    return 'restore-noop'
}

function Invoke-ProcessSmoke {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('none', 'core', 'gui')]
        [string]$Profile
    )

    $items = New-Object System.Collections.Generic.List[object]

    function Add-SmokeResult {
        param([string]$Name, [bool]$Success, [string]$Detail)
        $items.Add([pscustomobject]@{
            name = $Name
            success = $Success
            detail = $Detail
        }) | Out-Null
    }

    if ($Profile -eq 'none') {
        Add-SmokeResult -Name 'process-smoke-skipped' -Success $true -Detail 'smoke_profile=none'
        return [pscustomobject]@{
            success = $true
            failure_count = 0
            items = $items
            interactive_user_smoke = [pscustomobject]@{
                status = 'skipped'
                reason = 'smoke_profile=none'
            }
            benchmarks = Invoke-MicroBenchmarks -Profile $Profile
        }
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

    $processProbes = @(
        @{ name = 'cmd-ver'; file = "$env:SystemRoot\System32\cmd.exe"; args = '/c ver'; wait = $true },
        @{ name = 'powershell-version'; file = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"; args = '-NoProfile -Command "$PSVersionTable.PSVersion.ToString()"'; wait = $true }
    )
    if ($Profile -eq 'gui') {
        $processProbes += @(
            @{ name = 'notepad-x64-launch'; file = "$env:SystemRoot\System32\notepad.exe"; args = ''; wait = $false },
            @{ name = 'notepad-x86-launch'; file = "$env:SystemRoot\SysWOW64\notepad.exe"; args = ''; wait = $false },
            @{ name = 'calc-launch'; file = "$env:SystemRoot\System32\calc.exe"; args = ''; wait = $false }
        )
    }

    foreach ($probe in $processProbes) {
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

    if ($Profile -eq 'gui') {
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
    }

    $failed = @($items | Where-Object { -not $_.success })
    $hardFailed = @($items | Where-Object {
        -not $_.success -and -not ([string]$_.name).EndsWith('-uri-launch')
    })
    return [pscustomobject]@{
        success = ($hardFailed.Count -eq 0)
        failure_count = $failed.Count
        hard_failure_count = $hardFailed.Count
        best_effort_failure_count = $failed.Count - $hardFailed.Count
        items = $items
        interactive_user_smoke = Invoke-InteractiveUserSmoke -Profile $Profile
        benchmarks = Invoke-MicroBenchmarks -Profile $Profile
    }
}

function Invoke-InteractiveUserSmoke {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('none', 'core', 'gui')]
        [string]$Profile
    )

    if ($Profile -ne 'gui') {
        return [pscustomobject]@{
            status = 'skipped'
            reason = "smoke_profile=$Profile"
        }
    }

    try {
        $explorer = Get-Process explorer -IncludeUserName -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $explorer -or [string]::IsNullOrWhiteSpace([string]$explorer.UserName)) {
            return [pscustomobject]@{
                status = 'skipped'
                reason = 'no-interactive-explorer-user'
            }
        }

        $taskId = [Guid]::NewGuid().ToString('N')
        $taskName = "RegProbe-InteractiveSmoke-$taskId"
        $scriptPath = Join-Path $stateRoot "interactive-smoke-$taskId.ps1"
        $resultPath = Join-Path $stateRoot "interactive-smoke-$taskId.json"
        $script = @"
`$ErrorActionPreference = 'Continue'
`$items = New-Object System.Collections.Generic.List[object]
function Add-Item([string]`$Name, [bool]`$Success, [string]`$Detail) {
    `$items.Add([pscustomobject]@{ name = `$Name; success = `$Success; detail = `$Detail }) | Out-Null
}
try { Start-Process 'ms-settings:' -ErrorAction Stop; Start-Sleep -Seconds 2; Add-Item 'interactive-settings-uri' `$true 'launch-command-succeeded' } catch { Add-Item 'interactive-settings-uri' `$false `$_.Exception.Message }
try { Start-Process 'ms-windows-store:' -ErrorAction Stop; Start-Sleep -Seconds 4; Add-Item 'interactive-store-uri' `$true 'launch-command-succeeded' } catch { Add-Item 'interactive-store-uri' `$false `$_.Exception.Message }
foreach (`$probe in @(
    @{ name = 'interactive-notepad-x64'; file = "`$env:SystemRoot\System32\notepad.exe" },
    @{ name = 'interactive-notepad-x86'; file = "`$env:SystemRoot\SysWOW64\notepad.exe" },
    @{ name = 'interactive-calc'; file = "`$env:SystemRoot\System32\calc.exe" }
)) {
    try {
        if (-not (Test-Path -LiteralPath `$probe.file)) { Add-Item `$probe.name `$false "missing: `$(`$probe.file)"; continue }
        `$proc = Start-Process -FilePath `$probe.file -PassThru
        Start-Sleep -Seconds 2
        `$alive = [bool](Get-Process -Id `$proc.Id -ErrorAction SilentlyContinue)
        Stop-Process -Id `$proc.Id -Force -ErrorAction SilentlyContinue
        Add-Item `$probe.name `$true "started=true;alive_after_2s=`$alive"
    } catch { Add-Item `$probe.name `$false `$_.Exception.Message }
}
`$failed = @(`$items | Where-Object { -not `$_.success })
[pscustomobject]@{
    status = 'ok'
    user = [Environment]::UserName
    failure_count = `$failed.Count
    items = `$items
} | ConvertTo-Json -Depth 8 | Set-Content -Path '$resultPath' -Encoding UTF8
"@
        $script | Set-Content -Path $scriptPath -Encoding UTF8

        $start = (Get-Date).AddMinutes(1).ToString('HH:mm')
        $taskCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""
        $createOutput = & "$env:SystemRoot\System32\schtasks.exe" /Create /TN $taskName /TR $taskCommand /SC ONCE /ST $start /F /RL LIMITED /IT /RU ([string]$explorer.UserName) 2>&1
        $createExitCode = $LASTEXITCODE
        if ($createExitCode -ne 0) {
            return [pscustomobject]@{
                status = 'error'
                stage = 'create-scheduled-task'
                exit_code = $createExitCode
                user = [string]$explorer.UserName
                output = [string]::Join("`n", @($createOutput | ForEach-Object { [string]$_ }))
            }
        }

        $runOutput = & "$env:SystemRoot\System32\schtasks.exe" /Run /TN $taskName 2>&1
        $runExitCode = $LASTEXITCODE
        if ($runExitCode -ne 0) {
            schtasks.exe /Delete /TN $taskName /F | Out-Null
            return [pscustomobject]@{
                status = 'error'
                stage = 'run-scheduled-task'
                exit_code = $runExitCode
                user = [string]$explorer.UserName
                output = [string]::Join("`n", @($runOutput | ForEach-Object { [string]$_ }))
            }
        }

        $deadline = (Get-Date).AddSeconds(45)
        while ((Get-Date) -lt $deadline) {
            if (Test-Path -LiteralPath $resultPath) {
                $payload = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
                schtasks.exe /Delete /TN $taskName /F | Out-Null
                return $payload
            }
            Start-Sleep -Seconds 2
        }

        schtasks.exe /Delete /TN $taskName /F | Out-Null
        return [pscustomobject]@{
            status = 'timeout'
            user = [string]$explorer.UserName
            timeout_seconds = 45
        }
    }
    catch {
        return [pscustomobject]@{
            status = 'error'
            error = $_.Exception.Message
        }
    }
}

function Invoke-MicroBenchmarks {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('none', 'core', 'gui')]
        [string]$Profile
    )

    if ($Profile -eq 'none') {
        return [pscustomobject]@{
            status = 'skipped'
            reason = 'smoke_profile=none'
        }
    }

    try {
        if (-not ('RegProbeMicroBench' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public static class RegProbeMicroBench
{
    public static double CpuSeconds(int threads, int iterations)
    {
        threads = Math.Max(1, threads);
        iterations = Math.Max(1, iterations);
        var sw = Stopwatch.StartNew();
        Parallel.For(0, threads, worker =>
        {
            double acc = 0;
            for (int i = 1; i <= iterations; i++)
            {
                acc += Math.Sqrt(i + worker);
            }
            if (acc < 0) throw new InvalidOperationException("unreachable");
        });
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }

    public static double IoMegabytesPerSecond(string path, int megabytes)
    {
        megabytes = Math.Max(1, megabytes);
        byte[] buffer = new byte[1024 * 1024];
        new Random(1234).NextBytes(buffer);
        var sw = Stopwatch.StartNew();
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, buffer.Length, FileOptions.WriteThrough))
        {
            for (int i = 0; i < megabytes; i++)
            {
                stream.Write(buffer, 0, buffer.Length);
            }
            stream.Flush(true);
            stream.Position = 0;
            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
            }
        }
        sw.Stop();
        try { File.Delete(path); } catch { }
        return megabytes / Math.Max(sw.Elapsed.TotalSeconds, 0.001);
    }
}
'@
        }

        $processorCount = [Environment]::ProcessorCount
        $benchRoot = 'C:\RegProbe-Diag\benchmarks'
        New-Item -ItemType Directory -Path $benchRoot -Force | Out-Null
        $ioPath = Join-Path $benchRoot ('io-' + [Guid]::NewGuid().ToString('N') + '.bin')
        return [pscustomobject]@{
            status = 'ok'
            cpu_single_seconds = [Math]::Round([RegProbeMicroBench]::CpuSeconds(1, 200000000), 4)
            cpu_multi_seconds = [Math]::Round([RegProbeMicroBench]::CpuSeconds($processorCount, 100000000), 4)
            cpu_threads = $processorCount
            io_write_read_mib_per_second = [Math]::Round([RegProbeMicroBench]::IoMegabytesPerSecond($ioPath, 128), 2)
        }
    }
    catch {
        return [pscustomobject]@{
            status = 'error'
            error = $_.Exception.Message
        }
    }
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    stage = $Stage
    experiment_id = $ExperimentId
    registry_path = $RegistryPath
    value_name = $ValueName
    value_data = $ValueData
    smoke_profile = $SmokeProfile
    status = 'ok'
    error = $null
}

try {
    if ($Stage -eq 'apply') {
        $original = Read-RegistryValue -Path $RegistryPath -Name $ValueName
        $result.baseline_smoke = Invoke-ProcessSmoke -Profile $SmokeProfile
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
        $result.smoke = Invoke-ProcessSmoke -Profile $SmokeProfile
    }
    elseif ($Stage -eq 'post-reboot-rollback') {
        if (-not (Test-Path -LiteralPath $statePath)) {
            throw "Missing experiment state file: $statePath"
        }
        $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
        $result.after_reboot = Read-RegistryValue -Path $RegistryPath -Name $ValueName
        $result.smoke = Invoke-ProcessSmoke -Profile $SmokeProfile
        $result.restore_action = Restore-RegistryValue -State $state
        $result.after_restore = Read-RegistryValue -Path $RegistryPath -Name $ValueName
    }
    elseif ($Stage -eq 'post-rollback') {
        $result.final = Read-RegistryValue -Path $RegistryPath -Name $ValueName
        $result.smoke = Invoke-ProcessSmoke -Profile $SmokeProfile
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
    smoke_profile: str,
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
        "--ps-arg=-SmokeProfile",
        f"--ps-arg={smoke_profile}",
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
    try:
        completed = run(["virsh", "-c", connect, "reboot", domain], timeout=30)
    except subprocess.TimeoutExpired as error:
        return {
            "status": "timeout",
            "error": "guest-reboot-command-timeout",
            "timeout_seconds": error.timeout,
            "stdout": (error.stdout or "").decode("utf-8", errors="replace")
            if isinstance(error.stdout, bytes)
            else (error.stdout or ""),
            "stderr": (error.stderr or "").decode("utf-8", errors="replace")
            if isinstance(error.stderr, bytes)
            else (error.stderr or ""),
        }
    return {
        "status": "ok" if completed.returncode == 0 else "error",
        "returncode": completed.returncode,
        "stdout": completed.stdout,
        "stderr": completed.stderr,
    }


def recover_from_snapshot(
    *,
    domain: str,
    connect: str,
    snapshot_name: str,
    wait_timeout: int,
) -> dict[str, Any]:
    recovery: dict[str, Any] = {
        "snapshot": snapshot_name,
        "steps": [],
        "health": None,
        "status": "running",
    }

    destroy = run(["virsh", "-c", connect, "destroy", domain], timeout=30)
    recovery["steps"].append(
        {
            "action": "destroy-runtime",
            "returncode": destroy.returncode,
            "stdout": destroy.stdout.strip(),
            "stderr": destroy.stderr.strip(),
        }
    )

    revert = run(["virsh", "-c", connect, "snapshot-revert", domain, snapshot_name, "--force"], timeout=120)
    recovery["steps"].append(
        {
            "action": "snapshot-revert",
            "returncode": revert.returncode,
            "stdout": revert.stdout.strip(),
            "stderr": revert.stderr.strip(),
        }
    )
    if revert.returncode != 0:
        recovery["status"] = "error"
        recovery["error"] = "snapshot-revert-failed"
        return recovery

    start = run(["virsh", "-c", connect, "start", domain], timeout=30)
    recovery["steps"].append(
        {
            "action": "start-domain",
            "returncode": start.returncode,
            "stdout": start.stdout.strip(),
            "stderr": start.stderr.strip(),
        }
    )
    if start.returncode != 0:
        recovery["status"] = "error"
        recovery["error"] = "start-after-revert-failed"
        return recovery

    health = wait_for_qga(domain, connect, wait_timeout)
    recovery["health"] = health
    recovery["status"] = "ok" if health.get("status") == "ok" else "error"
    if recovery["status"] != "ok":
        recovery["error"] = "guest-did-not-return-after-snapshot-revert"
    return recovery


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
        "smoke_profile": args.smoke_profile,
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
        smoke_profile=args.smoke_profile,
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
    if args.post_reboot_delay_seconds > 0:
        time.sleep(args.post_reboot_delay_seconds)
    health = wait_for_qga(args.domain, args.connect, args.reboot_wait_timeout)
    result["health_checks"].append({"phase": "after-apply", "payload": health})
    if health.get("status") != "ok":
        result["status"] = "error"
        result["error"] = "guest-did-not-return-after-apply-reboot"
        if args.auto_revert_snapshot_on_boot_failure:
            result["recovery"] = recover_from_snapshot(
                domain=args.domain,
                connect=args.connect,
                snapshot_name=args.revert_snapshot_name,
                wait_timeout=args.reboot_wait_timeout,
            )
        return result

    rc, payload, stage_payload = run_guest_stage(
        script=stage_script,
        stage="post-reboot-rollback",
        registry_path=args.registry_path,
        value_name=args.value_name,
        value_data=args.value_data,
        experiment_id=experiment_id,
        smoke_profile=args.smoke_profile,
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
    if args.post_reboot_delay_seconds > 0:
        time.sleep(args.post_reboot_delay_seconds)
    health = wait_for_qga(args.domain, args.connect, args.reboot_wait_timeout)
    result["health_checks"].append({"phase": "after-rollback", "payload": health})
    if health.get("status") != "ok":
        result["status"] = "error"
        result["error"] = "guest-did-not-return-after-rollback-reboot"
        if args.auto_revert_snapshot_on_boot_failure:
            result["recovery"] = recover_from_snapshot(
                domain=args.domain,
                connect=args.connect,
                snapshot_name=args.revert_snapshot_name,
                wait_timeout=args.reboot_wait_timeout,
            )
        return result

    rc, payload, stage_payload = run_guest_stage(
        script=stage_script,
        stage="post-rollback",
        registry_path=args.registry_path,
        value_name=args.value_name,
        value_data=args.value_data,
        experiment_id=experiment_id,
        smoke_profile=args.smoke_profile,
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
            lines.append(f"- `smoke.hard_failure_count`: `{smoke.get('hard_failure_count')}`")
            lines.append(f"- `smoke.best_effort_failure_count`: `{smoke.get('best_effort_failure_count')}`")
            for item in smoke.get("items", []):
                if isinstance(item, dict):
                    lines.append(f"- `{item.get('name')}`: `{item.get('success')}` - {item.get('detail')}")
            interactive = smoke.get("interactive_user_smoke")
            if isinstance(interactive, dict):
                lines.append(
                    f"- `interactive_user_smoke`: status=`{interactive.get('status')}`, "
                    f"failure_count=`{interactive.get('failure_count')}`"
                )
            benchmarks = smoke.get("benchmarks")
            if isinstance(benchmarks, dict):
                lines.append(
                    f"- `benchmarks`: status=`{benchmarks.get('status')}`, "
                    f"cpu_single_seconds=`{benchmarks.get('cpu_single_seconds')}`, "
                    f"cpu_multi_seconds=`{benchmarks.get('cpu_multi_seconds')}`, "
                    f"io_mib_s=`{benchmarks.get('io_write_read_mib_per_second')}`"
                )
        baseline_smoke = result.get("baseline_smoke")
        if isinstance(baseline_smoke, dict):
            lines.append(f"- `baseline_smoke.failure_count`: `{baseline_smoke.get('failure_count')}`")
            lines.append(f"- `baseline_smoke.hard_failure_count`: `{baseline_smoke.get('hard_failure_count')}`")
            lines.append(f"- `baseline_smoke.best_effort_failure_count`: `{baseline_smoke.get('best_effort_failure_count')}`")
            benchmarks = baseline_smoke.get("benchmarks")
            if isinstance(benchmarks, dict):
                lines.append(
                    f"- `baseline_benchmarks`: status=`{benchmarks.get('status')}`, "
                    f"cpu_single_seconds=`{benchmarks.get('cpu_single_seconds')}`, "
                    f"cpu_multi_seconds=`{benchmarks.get('cpu_multi_seconds')}`, "
                    f"io_mib_s=`{benchmarks.get('io_write_read_mib_per_second')}`"
                )

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
        "--smoke-profile",
        choices=["none", "core", "gui"],
        default="none",
        help="none skips process smoke; core runs command/shell checks; gui additionally launches desktop apps and URI handlers from QGA/SYSTEM context.",
    )
    parser.add_argument(
        "--require-domain-snapshot",
        action="store_true",
        help="Fail before applying a value unless a libvirt domain snapshot exists.",
    )
    parser.add_argument(
        "--auto-revert-snapshot-on-boot-failure",
        action="store_true",
        help="If the guest does not return to QGA after an experiment reboot, destroy the runtime, revert a snapshot, restart, and record recovery details.",
    )
    parser.add_argument(
        "--revert-snapshot-name",
        default=vm_snapshot("clean-25h2-qga"),
        help="Snapshot to use with --auto-revert-snapshot-on-boot-failure.",
    )
    parser.add_argument(
        "--post-reboot-delay-seconds",
        type=int,
        default=20,
        help="Wait this long after virsh reboot before accepting QGA health as a post-boot signal.",
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
