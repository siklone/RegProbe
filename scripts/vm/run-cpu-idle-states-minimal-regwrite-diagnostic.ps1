[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestRoot = 'C:\RegProbe-Diag',
    [string]$SnapshotName = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "cpu-idle-minimal-regwrite-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'cpu-idle-minimal-regwrite-payload.ps1'
$guestPayloadPath = Join-Path $GuestRoot 'cpu-idle-minimal-regwrite-payload.ps1'
$guestResultPath = Join-Path $GuestRoot 'cpu-idle-minimal-regwrite-result.json'
$hostResultPath = Join-Path $hostRoot 'cpu-idle-minimal-regwrite-result.json'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$ResultPath
)

$ErrorActionPreference = 'Continue'
$registryPath = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power'
$regExePath = 'HKLM\SYSTEM\CurrentControlSet\Control\Power'
$valueMap = [ordered]@{
    DisableIdleStatesAtBoot = 1
    IdleStateTimeout = 0
    ExitLatencyCheckEnabled = 1
}

function Get-BundleState {
    $state = [ordered]@{}
    foreach ($name in $valueMap.Keys) {
        try {
            $state[$name] = (Get-ItemProperty -Path $registryPath -Name $name -ErrorAction Stop).$name
        }
        catch {
            $state[$name] = $null
        }
    }
    return $state
}

function Reset-Bundle {
    foreach ($name in $valueMap.Keys) {
        Remove-ItemProperty -Path $registryPath -Name $name -ErrorAction SilentlyContinue
    }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    $entry = [ordered]@{
        label = $Label
        before = Get-BundleState
        ok = $false
        error = $null
        after = $null
    }

    try {
        & $Action
        $entry.ok = $true
    }
    catch {
        $entry.ok = $false
        $entry.error = $_.Exception.Message
    }
    finally {
        $entry.after = Get-BundleState
    }

    return $entry
}

function Write-WithProvider {
    New-Item -Path $registryPath -Force | Out-Null
    foreach ($name in $valueMap.Keys) {
        New-ItemProperty -Path $registryPath -Name $name -Value $valueMap[$name] -PropertyType DWord -Force | Out-Null
    }
}

function Write-WithRegExe {
    foreach ($name in $valueMap.Keys) {
        & reg.exe add $regExePath /v $name /t REG_DWORD /d $valueMap[$name] /f | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "reg.exe add failed for $name with exit code $LASTEXITCODE"
        }
    }
}

Reset-Bundle
$steps = @()
$steps += [pscustomobject](Invoke-Step -Label 'provider-write' -Action { Write-WithProvider })
Reset-Bundle
$steps += [pscustomobject](Invoke-Step -Label 'regexe-write' -Action { Write-WithRegExe })
Reset-Bundle

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    final_state = Get-BundleState
    steps = $steps
} | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultPath -Encoding UTF8
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Ensure-GuestDirectory {
    param([string]$GuestPath)

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'createDirectoryInGuest', $VmPath, $GuestPath
        ) | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'already exists') {
            throw
        }
    }
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 600)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }
        Start-Sleep -Seconds 5
    }
    throw 'Guest did not return to a running VMware Tools state in time.'
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath
    ) | Out-Null
}

function Copy-FromGuestBestEffort {
    param([string]$GuestPath, [string]$HostPath)

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
        ) | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList)
}

if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
Wait-GuestReady
Ensure-GuestDirectory -GuestPath $GuestRoot
Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    guest_root = $GuestRoot
    copied_result = $false
    provider_write_ok = $false
    regexe_write_ok = $false
    provider_after = $null
    regexe_after = $null
    final_state = $null
    status = 'started'
    errors = @()
}

try {
    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-ResultPath', $guestResultPath
    ) | Out-Null
}
catch {
    $summary.errors += $_.Exception.Message
}

$summary.copied_result = Copy-FromGuestBestEffort -GuestPath $guestResultPath -HostPath $hostResultPath

if ($summary.copied_result) {
    Copy-Item -Path $hostResultPath -Destination (Join-Path $repoRootOut 'cpu-idle-minimal-regwrite-result.json') -Force
    $payload = Get-Content -Path $hostResultPath -Raw | ConvertFrom-Json
    $summary.final_state = $payload.final_state
    foreach ($step in @($payload.steps)) {
        if ($step.label -eq 'provider-write') {
            $summary.provider_write_ok = [bool]$step.ok
            $summary.provider_after = $step.after
        }
        elseif ($step.label -eq 'regexe-write') {
            $summary.regexe_write_ok = [bool]$step.ok
            $summary.regexe_after = $step.after
        }
    }
}

$summary.status =
    if ($summary.copied_result) { 'ok' } else { 'failed' }

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSummaryPath -Encoding UTF8

Write-Output $repoSummaryPath
if ($summary.status -ne 'ok') {
    exit 1
}

