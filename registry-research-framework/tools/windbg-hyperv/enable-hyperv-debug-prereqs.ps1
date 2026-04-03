[CmdletBinding()]
param(
    [switch]$NoRestart = $true,
    [string]$AuditDate = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$feasibilityScript = Join-Path $repoRoot 'scripts\vm-hyperv\test-hyperv-debug-feasibility.ps1'
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$notesRoot = Join-Path $repoRoot 'research\notes'
$stagingRoot = Join-Path $repoRoot ("evidence\files\vm-tooling-staging\windbg-hyperv-prereqs-{0}" -f $(if ([string]::IsNullOrWhiteSpace($AuditDate)) { Get-Date -Format 'yyyyMMdd' } else { $AuditDate }))
$resolvedAuditDate = if ([string]::IsNullOrWhiteSpace($AuditDate)) { Get-Date -Format 'yyyyMMdd' } else { $AuditDate }

New-Item -ItemType Directory -Path $auditRoot -Force | Out-Null
New-Item -ItemType Directory -Path $notesRoot -Force | Out-Null
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-FeatureState {
    param([Parameter(Mandatory = $true)][string]$Name)

    $feature = Get-WindowsOptionalFeature -Online -FeatureName $Name -ErrorAction Stop
    return [string]$feature.State
}

function Test-RebootPending {
    $pendingPaths = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'
    )

    foreach ($path in $pendingPaths) {
        if (Test-Path -LiteralPath $path) {
            return $true
        }
    }

    try {
        $pendingRenames = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager' -ErrorAction Stop).PendingFileRenameOperations
        if ($pendingRenames) {
            return $true
        }
    }
    catch {
    }

    return $false
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Hyper-V prerequisite enablement requires an elevated PowerShell session.'
}

$beforePath = Join-Path $stagingRoot 'hyperv-feasibility-before.json'
$afterPath = Join-Path $stagingRoot 'hyperv-feasibility-after.json'
$before = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $feasibilityScript -OutputPath $beforePath | ConvertFrom-Json

$targetFeatures = @(
    'Microsoft-Hyper-V-All',
    'Microsoft-Hyper-V-Management-PowerShell'
)

$enableOutputs = New-Object System.Collections.Generic.List[object]
$restartNeeded = $false

foreach ($featureName in $targetFeatures) {
    $stateBefore = Get-FeatureState -Name $featureName
    if ($stateBefore -eq 'Enabled') {
        $enableOutputs.Add([ordered]@{
            feature = $featureName
            action = 'already-enabled'
            restart_needed = $false
        })
        continue
    }

    $enableArgs = @{
        Online = $true
        FeatureName = $featureName
        All = $true
        ErrorAction = 'Stop'
    }
    if ($NoRestart) {
        $enableArgs.NoRestart = $true
    }

    $featureResult = Enable-WindowsOptionalFeature @enableArgs
    $featureRestartNeeded = [bool]$featureResult.RestartNeeded
    if ($featureRestartNeeded) {
        $restartNeeded = $true
    }

    $enableOutputs.Add([ordered]@{
        feature = $featureName
        action = 'enable-requested'
        restart_needed = $featureRestartNeeded
        state_after_request = [string]$featureResult.State
    })
}

$after = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $feasibilityScript -OutputPath $afterPath | ConvertFrom-Json
$rebootPending = Test-RebootPending

$status = if ([string]$after.selected_status -eq 'ready' -and -not $rebootPending) {
    'enabled-ready'
}
elseif ($restartNeeded -or $rebootPending) {
    'enabled-pending-restart'
}
elseif ([string]$before.selected_status -eq [string]$after.selected_status) {
    'no-state-change'
}
else {
    'enabled-partial'
}

$nextStep = if ($restartNeeded -or $rebootPending) {
    'host-reboot-required'
}
elseif ([string]$after.selected_status -eq 'ready') {
    'provision-hyperv-debug-baseline'
}
else {
    're-run-feasibility-and-investigate'
}

$actionsArray = @($enableOutputs.ToArray())

$auditResult = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = $status
    debug_environment = 'hyperv'
    phase = 'prereq-enable'
    restart_required = ($restartNeeded -or $rebootPending)
    reboot_pending = $rebootPending
    no_restart_requested = [bool]$NoRestart
    before_status = [string]$before.selected_status
    after_status = [string]$after.selected_status
    features_requested = $targetFeatures
    actions = $actionsArray
    before_ref = ("evidence/files/vm-tooling-staging/windbg-hyperv-prereqs-{0}/hyperv-feasibility-before.json" -f $resolvedAuditDate)
    after_ref = ("evidence/files/vm-tooling-staging/windbg-hyperv-prereqs-{0}/hyperv-feasibility-after.json" -f $resolvedAuditDate)
    next_step = $nextStep
}

$auditPath = Join-Path $auditRoot ("windbg-hyperv-prereqs-{0}.json" -f $resolvedAuditDate)
$notePath = Join-Path $notesRoot ("windbg-hyperv-prereqs-{0}.md" -f $resolvedAuditDate)

Write-JsonFile -Path $auditPath -InputObject $auditResult

$noteLines = @(
    '# Hyper-V Debug Prerequisites',
    '',
    "- Date: ``$resolvedAuditDate``",
    "- Status: ``$($auditResult.status)``",
    "- Restart required: ``$($auditResult.restart_required)``",
    "- Reboot pending: ``$($auditResult.reboot_pending)``",
    "- Before: ``$($auditResult.before_status)``",
    "- After: ``$($auditResult.after_status)``",
    "- Next step: ``$($auditResult.next_step)``"
)
Set-Content -LiteralPath $notePath -Value (($noteLines -join "`n") + "`n") -Encoding UTF8

$auditResult | ConvertTo-Json -Depth 10
