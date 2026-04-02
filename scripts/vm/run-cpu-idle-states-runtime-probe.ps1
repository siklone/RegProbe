[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\RegProbe-Diag',
    [string]$RecordId = 'power.disable-cpu-idle-states',
    [string]$SnapshotName = '',
    [string]$IncidentLogPath = '',
    [int]$SettleSeconds = 20
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName
}

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "cpu-idle-runtime-$stamp"
$sessionId = "cpu-idle-stepwise-$stamp"
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'
$repoStepRoot = Join-Path $repoEvidenceRoot $sessionId

New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Step,
        [hashtable]$ExtraArgs = @{}
    )

    $scriptPath = Join-Path $PSScriptRoot 'run-cpu-idle-states-orchestration-step.ps1'
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $scriptPath,
        '-Step', $Step,
        '-SessionId', $sessionId,
        '-VmPath', $VmPath,
        '-VmrunPath', $VmrunPath,
        '-GuestUser', $GuestUser,
        '-GuestPassword', $GuestPassword,
        '-HostOutputRoot', $HostOutputRoot,
        '-GuestRootBase', $GuestOutputRoot,
        '-RecordId', $RecordId,
        '-SnapshotName', $SnapshotName,
        '-IncidentLogPath', $IncidentLogPath
    )

    foreach ($key in $ExtraArgs.Keys) {
        $value = $ExtraArgs[$key]
        if ($value -is [switch]) {
            continue
        }

        if ($value -is [bool]) {
            if ($value) {
                $arguments += "-$key"
            }
            continue
        }

        $arguments += "-$key"
        $arguments += [string]$value
    }

    & powershell.exe @arguments | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Step $Step failed."
    }

    $summaryPath = Join-Path $repoStepRoot ("step-{0}-summary.json" -f $Step.ToLowerInvariant())
    if (-not (Test-Path $summaryPath)) {
        throw "Missing step summary after step ${Step}: $summaryPath"
    }

    return Get-Content -Path $summaryPath -Raw | ConvertFrom-Json
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    session_id = $sessionId
    record_id = $RecordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    session_root = "evidence/files/vm-tooling-staging/$sessionId"
    status = 'started'
    failed_step = $null
    step_summaries = [ordered]@{}
    errors = @()
}

$currentStep = 'A'

try {
    $currentStep = 'A'
    $summary.step_summaries.A = Invoke-Step -Step 'A'
    $currentStep = 'B'
    $summary.step_summaries.B = Invoke-Step -Step 'B' -ExtraArgs @{ SettleSeconds = $SettleSeconds; PerformReboot = $true }
    $currentStep = 'C1'
    $summary.step_summaries.C1 = Invoke-Step -Step 'C1'
    $currentStep = 'C2'
    $summary.step_summaries.C2 = Invoke-Step -Step 'C2'
    $currentStep = 'C3'
    $summary.step_summaries.C3 = Invoke-Step -Step 'C3'
    $currentStep = 'C4'
    $summary.step_summaries.C4 = Invoke-Step -Step 'C4'
    $currentStep = 'D'
    $summary.step_summaries.D = Invoke-Step -Step 'D' -ExtraArgs @{ PerformReboot = $true }
    $summary.status = 'ok'
}
catch {
    $summary.failed_step = $currentStep
    $summary.errors += $_.Exception.Message
    $summary.status = 'failed'
}

$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $repoSummaryPath -Encoding UTF8
Write-Output $repoSummaryPath

if ($summary.status -ne 'ok') {
    exit 1
}

