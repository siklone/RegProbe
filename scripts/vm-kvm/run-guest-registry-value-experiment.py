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


def _read_proc_stat_cpu(path: Path = Path("/proc/stat")) -> tuple[int, int]:
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("cpu "):
            fields = [int(value) for value in line.split()[1:]]
            idle = fields[3] + (fields[4] if len(fields) > 4 else 0)
            return idle, sum(fields)
    raise RuntimeError(f"{path}: aggregate cpu line not found")


def _host_cpu_busy_pct(sample_interval_seconds: float) -> float:
    idle0, total0 = _read_proc_stat_cpu()
    time.sleep(sample_interval_seconds)
    idle1, total1 = _read_proc_stat_cpu()
    total_delta = total1 - total0
    idle_delta = idle1 - idle0
    if total_delta <= 0:
        return 0.0
    return round((1.0 - (idle_delta / total_delta)) * 100.0, 2)


def _read_loadavg(path: Path = Path("/proc/loadavg")) -> tuple[float, float, float]:
    parts = path.read_text(encoding="utf-8").split()
    return float(parts[0]), float(parts[1]), float(parts[2])


def _host_cpu_count() -> int:
    count = 0
    try:
        for line in Path("/proc/cpuinfo").read_text(encoding="utf-8").splitlines():
            if line.startswith("processor"):
                count += 1
    except OSError:
        count = 0
    return max(count, 1)


def wait_for_quiet_host(
    *,
    enabled: bool = True,
    max_retries: int = 5,
    interval_seconds: float = 5.0,
    busy_threshold_pct: float = 20.0,
    load1_per_cpu_threshold: float = 0.75,
    sample_interval_seconds: float = 0.5,
) -> dict[str, Any]:
    started = now_utc()
    if not enabled:
        return {
            "noise_status": "skipped",
            "noise_reason": "host-noise-gate-disabled",
            "sample_started_utc": started,
            "sample_finished_utc": now_utc(),
        }

    try:
        cpu_count = _host_cpu_count()
        load1_threshold = load1_per_cpu_threshold * cpu_count
        last_busy = 0.0
        last_load1 = 0.0
        last_load5 = 0.0
        for attempt in range(max_retries + 1):
            last_busy = _host_cpu_busy_pct(sample_interval_seconds)
            last_load1, last_load5, _ = _read_loadavg()
            busy_ok = last_busy <= busy_threshold_pct
            load_ok = last_load1 <= load1_threshold
            if busy_ok and load_ok:
                return {
                    "noise_status": "ok",
                    "noise_reason": None,
                    "host_cpu_busy_pct": last_busy,
                    "load1": last_load1,
                    "load5": last_load5,
                    "host_cpu_count": cpu_count,
                    "retry_count": attempt,
                    "max_retries": max_retries,
                    "busy_threshold_pct": busy_threshold_pct,
                    "load1_per_cpu_threshold": load1_per_cpu_threshold,
                    "sample_started_utc": started,
                    "sample_finished_utc": now_utc(),
                }
            if attempt < max_retries:
                time.sleep(interval_seconds)

        reasons: list[str] = []
        if last_busy > busy_threshold_pct:
            reasons.append(f"cpu_busy={last_busy}% > threshold={busy_threshold_pct}%")
        if last_load1 > load1_threshold:
            reasons.append(f"load1={last_load1} > threshold={load1_threshold:.2f}")
        return {
            "noise_status": "noisy",
            "noise_reason": "; ".join(reasons) or "quiet-host-threshold-not-met",
            "host_cpu_busy_pct": last_busy,
            "load1": last_load1,
            "load5": last_load5,
            "host_cpu_count": cpu_count,
            "retry_count": max_retries,
            "max_retries": max_retries,
            "busy_threshold_pct": busy_threshold_pct,
            "load1_per_cpu_threshold": load1_per_cpu_threshold,
            "sample_started_utc": started,
            "sample_finished_utc": now_utc(),
        }
    except (OSError, RuntimeError, ValueError) as error:
        return {
            "noise_status": "unknown",
            "noise_reason": str(error),
            "sample_started_utc": started,
            "sample_finished_utc": now_utc(),
        }


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
    public static double CpuIterationsPerSecond(int threads, int milliseconds)
    {
        threads = Math.Max(1, threads);
        milliseconds = Math.Max(250, milliseconds);
        long total = 0;
        var sw = Stopwatch.StartNew();
        long deadline = Stopwatch.GetTimestamp() + (long)((milliseconds / 1000.0) * Stopwatch.Frequency);
        Parallel.For<long>(
            0,
            threads,
            () => 0L,
            (worker, state, local) =>
            {
                double acc = worker + 1.0000001;
                while (Stopwatch.GetTimestamp() < deadline)
                {
                    acc = (acc * 1.0000001) + 0.000001;
                    local++;
                    if ((local & 65535L) == 0)
                    {
                        acc = Math.Sqrt(acc * acc);
                    }
                }
                if (acc < 0) throw new InvalidOperationException("unreachable");
                return local;
            },
            local => System.Threading.Interlocked.Add(ref total, local));
        sw.Stop();
        return total / Math.Max(sw.Elapsed.TotalSeconds, 0.001);
    }

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

    public static double[] IoMegabytesPerSecond(string path, int megabytes)
    {
        megabytes = Math.Max(1, megabytes);
        byte[] buffer = new byte[megabytes * 1024 * 1024];
        new Random(1234).NextBytes(buffer);
        var writeSw = Stopwatch.StartNew();
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.WriteThrough))
        {
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush(true);
        }
        writeSw.Stop();

        byte[] readBuffer = new byte[1024 * 1024];
        var readSw = Stopwatch.StartNew();
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, readBuffer.Length, FileOptions.SequentialScan))
        {
            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
            }
        }
        readSw.Stop();
        try { File.Delete(path); } catch { }
        return new double[] {
            megabytes / Math.Max(writeSw.Elapsed.TotalSeconds, 0.001),
            megabytes / Math.Max(readSw.Elapsed.TotalSeconds, 0.001)
        };
    }
}
'@
        }

        function Get-Median {
            param([double[]]$Values)
            $sorted = @($Values | Sort-Object)
            if ($sorted.Count -eq 0) {
                return $null
            }
            if (($sorted.Count % 2) -eq 1) {
                return [double]$sorted[[int](($sorted.Count - 1) / 2)]
            }
            return ([double]$sorted[[int]($sorted.Count / 2 - 1)] + [double]$sorted[[int]($sorted.Count / 2)]) / 2.0
        }

        function Get-BenchStats {
            param([double[]]$Values)
            $valuesArray = @($Values)
            $median = Get-Median -Values $valuesArray
            if ($null -eq $median) {
                return [pscustomobject]@{
                    median = $null
                    min = $null
                    max = $null
                    spread_pct = $null
                    samples = @()
                }
            }
            $min = ($valuesArray | Measure-Object -Minimum).Minimum
            $max = ($valuesArray | Measure-Object -Maximum).Maximum
            $spread = if ([double]$median -eq 0) { 0.0 } else { (([double]$max - [double]$min) / [Math]::Abs([double]$median)) * 100.0 }
            return [pscustomobject]@{
                median = [Math]::Round([double]$median, 4)
                min = [Math]::Round([double]$min, 4)
                max = [Math]::Round([double]$max, 4)
                spread_pct = [Math]::Round([double]$spread, 2)
                samples = @($valuesArray | ForEach-Object { [Math]::Round([double]$_, 4) })
            }
        }

        $processorCount = [Environment]::ProcessorCount
        $benchRoot = 'C:\RegProbe-Diag\benchmarks'
        New-Item -ItemType Directory -Path $benchRoot -Force | Out-Null
        $sampleCount = 5
        $cpuDurationMs = 3000
        $ioSizeMiB = 16
        $cpuSingle = New-Object System.Collections.Generic.List[double]
        $cpuMulti = New-Object System.Collections.Generic.List[double]
        $ioWrite = New-Object System.Collections.Generic.List[double]
        $ioRead = New-Object System.Collections.Generic.List[double]
        for ($sampleIndex = 0; $sampleIndex -lt $sampleCount; $sampleIndex++) {
            $cpuSingle.Add([RegProbeMicroBench]::CpuIterationsPerSecond(1, $cpuDurationMs)) | Out-Null
            $cpuMulti.Add([RegProbeMicroBench]::CpuIterationsPerSecond($processorCount, $cpuDurationMs)) | Out-Null
            $ioPath = Join-Path $benchRoot ('io-' + [Guid]::NewGuid().ToString('N') + '.bin')
            $ioPair = [RegProbeMicroBench]::IoMegabytesPerSecond($ioPath, $ioSizeMiB)
            $ioWrite.Add([double]$ioPair[0]) | Out-Null
            $ioRead.Add([double]$ioPair[1]) | Out-Null
        }
        $singleStats = Get-BenchStats -Values $cpuSingle.ToArray()
        $multiStats = Get-BenchStats -Values $cpuMulti.ToArray()
        $writeStats = Get-BenchStats -Values $ioWrite.ToArray()
        $readStats = Get-BenchStats -Values $ioRead.ToArray()
        $combinedIo = @()
        for ($i = 0; $i -lt $ioWrite.Count; $i++) {
            $combinedIo += (($ioWrite[$i] + $ioRead[$i]) / 2.0)
        }
        $combinedIoStats = Get-BenchStats -Values ([double[]]$combinedIo)
        return [pscustomobject]@{
            status = 'ok'
            version = 2
            sample_count = $sampleCount
            cpu_duration_seconds = [Math]::Round(($cpuDurationMs / 1000.0), 3)
            cpu_threads = $processorCount
            io_size_mib = $ioSizeMiB
            cpu_single_iterations_per_second = $singleStats.median
            cpu_multi_iterations_per_second = $multiStats.median
            io_write_mib_per_second = $writeStats.median
            io_read_mib_per_second = $readStats.median
            io_write_read_mib_per_second = $combinedIoStats.median
            spreads = [pscustomobject]@{
                cpu_single_iterations_per_second = $singleStats.spread_pct
                cpu_multi_iterations_per_second = $multiStats.spread_pct
                io_write_mib_per_second = $writeStats.spread_pct
                io_read_mib_per_second = $readStats.spread_pct
                io_write_read_mib_per_second = $combinedIoStats.spread_pct
            }
            samples = [pscustomobject]@{
                cpu_single_iterations_per_second = $singleStats.samples
                cpu_multi_iterations_per_second = $multiStats.samples
                io_write_mib_per_second = $writeStats.samples
                io_read_mib_per_second = $readStats.samples
                io_write_read_mib_per_second = $combinedIoStats.samples
            }
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


def mark_controlled_recovery(result: dict[str, Any], *, error: str, outcome: str, recovery: dict[str, Any] | None) -> bool:
    if not isinstance(recovery, dict) or recovery.get("status") != "ok":
        return False
    result["status"] = "ok"
    result["error"] = error
    result["outcome"] = outcome
    result["controlled_failure"] = True
    result["recovery"] = recovery
    result["smoke"] = {
        "apply_smoke_hard_success": smoke_success(result["stages"].get("apply", {}).get("result")),
        "post_reboot_smoke_hard_success": False,
        "post_rollback_smoke_hard_success": False,
    }
    return True


def mark_recovered_boot_failure(result: dict[str, Any], error: str, recovery: dict[str, Any] | None) -> bool:
    return mark_controlled_recovery(result, error=error, outcome="boot-failure-recovered", recovery=recovery)


def recover_stage_failure(
    result: dict[str, Any],
    *,
    error: str,
    outcome: str,
    args: argparse.Namespace,
) -> bool:
    if not args.auto_revert_snapshot_on_boot_failure:
        return False
    recovery = recover_from_snapshot(
        domain=args.domain,
        connect=args.connect,
        snapshot_name=args.revert_snapshot_name,
        wait_timeout=args.reboot_wait_timeout,
    )
    if mark_controlled_recovery(result, error=error, outcome=outcome, recovery=recovery):
        return True
    result["recovery"] = recovery
    return False


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

    apply_noise = wait_for_quiet_host(
        enabled=not args.no_host_noise_gate,
        max_retries=args.host_noise_max_retries,
        interval_seconds=args.host_noise_retry_interval_seconds,
        busy_threshold_pct=args.host_noise_busy_threshold_pct,
        load1_per_cpu_threshold=args.host_noise_load1_per_cpu_threshold,
        sample_interval_seconds=args.host_noise_sample_interval_seconds,
    )
    if getattr(args, "abort_on_noisy_host", False) and apply_noise.get("noise_status") in {"noisy", "unknown"}:
        result["status"] = "error"
        result["error"] = "host-noise-preflight-failed"
        result["outcome"] = "aborted-before-apply"
        result["preflight"] = {"host_noise_meta": apply_noise}
        result["safety"]["mutation_started"] = False
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
    result["stages"]["apply"] = {"returncode": rc, "qga": payload, "result": stage_payload, "host_noise_meta": apply_noise}
    if rc != 0 or not isinstance(stage_payload, dict) or stage_payload.get("status") != "ok":
        error = "apply-stage-failed"
        result["status"] = "error"
        result["error"] = error
        if recover_stage_failure(result, error=error, outcome="apply-stage-failure-recovered", args=args):
            return result
        return result

    result["reboots"].append({"phase": "after-apply", **reboot_guest(args.domain, args.connect)})
    if args.post_reboot_delay_seconds > 0:
        time.sleep(args.post_reboot_delay_seconds)
    health = wait_for_qga(args.domain, args.connect, args.reboot_wait_timeout)
    result["health_checks"].append({"phase": "after-apply", "payload": health})
    if health.get("status") != "ok":
        error = "guest-did-not-return-after-apply-reboot"
        result["status"] = "error"
        result["error"] = error
        if args.auto_revert_snapshot_on_boot_failure:
            recovery = recover_from_snapshot(
                domain=args.domain,
                connect=args.connect,
                snapshot_name=args.revert_snapshot_name,
                wait_timeout=args.reboot_wait_timeout,
            )
            if mark_recovered_boot_failure(result, error, recovery):
                return result
            result["recovery"] = recovery
        return result

    post_reboot_noise = wait_for_quiet_host(
        enabled=not args.no_host_noise_gate,
        max_retries=args.host_noise_max_retries,
        interval_seconds=args.host_noise_retry_interval_seconds,
        busy_threshold_pct=args.host_noise_busy_threshold_pct,
        load1_per_cpu_threshold=args.host_noise_load1_per_cpu_threshold,
        sample_interval_seconds=args.host_noise_sample_interval_seconds,
    )
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
    result["stages"]["post_reboot_rollback"] = {
        "returncode": rc,
        "qga": payload,
        "result": stage_payload,
        "host_noise_meta": post_reboot_noise,
    }
    if rc != 0 or not isinstance(stage_payload, dict) or stage_payload.get("status") != "ok":
        error = "post-reboot-rollback-stage-failed"
        result["status"] = "error"
        result["error"] = error
        if recover_stage_failure(result, error=error, outcome="rollback-stage-failure-recovered", args=args):
            return result
        return result

    result["reboots"].append({"phase": "after-rollback", **reboot_guest(args.domain, args.connect)})
    if args.post_reboot_delay_seconds > 0:
        time.sleep(args.post_reboot_delay_seconds)
    health = wait_for_qga(args.domain, args.connect, args.reboot_wait_timeout)
    result["health_checks"].append({"phase": "after-rollback", "payload": health})
    if health.get("status") != "ok":
        error = "guest-did-not-return-after-rollback-reboot"
        result["status"] = "error"
        result["error"] = error
        if args.auto_revert_snapshot_on_boot_failure:
            recovery = recover_from_snapshot(
                domain=args.domain,
                connect=args.connect,
                snapshot_name=args.revert_snapshot_name,
                wait_timeout=args.reboot_wait_timeout,
            )
            if mark_recovered_boot_failure(result, error, recovery):
                return result
            result["recovery"] = recovery
        return result

    post_rollback_noise = wait_for_quiet_host(
        enabled=not args.no_host_noise_gate,
        max_retries=args.host_noise_max_retries,
        interval_seconds=args.host_noise_retry_interval_seconds,
        busy_threshold_pct=args.host_noise_busy_threshold_pct,
        load1_per_cpu_threshold=args.host_noise_load1_per_cpu_threshold,
        sample_interval_seconds=args.host_noise_sample_interval_seconds,
    )
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
    result["stages"]["post_rollback"] = {
        "returncode": rc,
        "qga": payload,
        "result": stage_payload,
        "host_noise_meta": post_rollback_noise,
    }
    if rc != 0 or not isinstance(stage_payload, dict) or stage_payload.get("status") != "ok":
        error = "post-rollback-stage-failed"
        result["status"] = "error"
        result["error"] = error
        if recover_stage_failure(result, error=error, outcome="post-rollback-stage-failure-recovered", args=args):
            return result
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
        f"- Outcome: `{summary.get('outcome') or 'completed'}`",
        "",
        "## Result",
        "",
    ]
    if summary.get("error"):
        lines.append(f"- Error: `{summary.get('error')}`")
    if summary.get("controlled_failure"):
        lines.append("- Controlled failure: `true`")
    recovery = summary.get("recovery")
    if isinstance(recovery, dict):
        lines.append(f"- Snapshot recovery: `{recovery.get('status')}`")
    if summary.get("status") == "ok":
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
    parser.add_argument("--no-host-noise-gate", action="store_true", help="Skip host CPU/load preflight and mark stage noise metadata as skipped.")
    parser.add_argument("--host-noise-max-retries", type=int, default=5)
    parser.add_argument("--host-noise-retry-interval-seconds", type=float, default=5.0)
    parser.add_argument("--host-noise-busy-threshold-pct", type=float, default=20.0)
    parser.add_argument("--host-noise-load1-per-cpu-threshold", type=float, default=0.75)
    parser.add_argument("--host-noise-sample-interval-seconds", type=float, default=0.5)
    parser.add_argument(
        "--abort-on-noisy-host",
        action="store_true",
        help="Fail before registry apply if the host noise gate remains noisy or unknown after retries.",
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
