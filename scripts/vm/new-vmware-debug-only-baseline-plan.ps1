[CmdletBinding()]
param(
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

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

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1')

$configPath = Join-Path $repoRoot 'registry-research-framework\config\debug-environments.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$vmwareDebugOnlyConfig = @($config.environments | Where-Object { $_.id -eq 'vmware-debug-only' }) | Select-Object -First 1

if ($null -eq $vmwareDebugOnlyConfig) {
    throw "VMware debug-only environment config missing from $configPath"
}

$sourceProfile = [string]$vmwareDebugOnlyConfig.source_runtime_profile
$sourceVmName = Resolve-CanonicalVmName -VmProfile $sourceProfile
$sourceSnapshot = if (-not [string]::IsNullOrWhiteSpace([string]$vmwareDebugOnlyConfig.source_runtime_snapshot)) {
    [string]$vmwareDebugOnlyConfig.source_runtime_snapshot
}
else {
    Resolve-SeedVmSnapshotName -VmProfile $sourceProfile
}

$plan = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = 'ready-to-short-try'
    debug_environment = 'vmware-debug-only'
    debug_vm_name = [string]$vmwareDebugOnlyConfig.vm_name
    debug_transport = [string]$vmwareDebugOnlyConfig.transport_candidates[0]
    debug_transport_candidates = @($vmwareDebugOnlyConfig.transport_candidates)
    debug_baseline = [string]$vmwareDebugOnlyConfig.baseline_name
    transport_reliability = 'unknown'
    reproducible_runs = 0
    vm_role = [string]$vmwareDebugOnlyConfig.role
    source_runtime_profile = $sourceProfile
    source_vm_name = $sourceVmName
    source_vm_path = "<resolve-from-vm-baselines:$sourceProfile>"
    source_vm_path_resolution = 'resolve-at-provision-time'
    source_snapshot = $sourceSnapshot
    trial_policy = [string]$vmwareDebugOnlyConfig.trial_policy
    frozen_lane_return_allowed = $false
    stop_condition = 'repeat-current-transport-blocker'
    role_split = [ordered]@{
        vmware_runtime = 'runtime_research'
        vmware_debug_only = 'debug_arbiter_only_short_try'
        hyperv = 'preferred_long_term_debug_arbiter'
    }
    provisioning_steps = @(
        'Clone from the runtime VMware baseline into a fresh debug-only VM instead of reusing the frozen WinDbg lane.',
        'Keep the new VM debugger-first and minimal; do not copy runtime scratch or mega-trigger tooling clutter into it.',
        'Enable only the transport and shell-health helpers needed for a short WinDbg transport try.',
        'Run a short transport-only smoke before any single-key semantic lane.',
        'If the current VMware transport blocker reproduces, stop the VMware branch and move directly to Hyper-V prerequisites.'
    )
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-JsonFile -Path $OutputPath -InputObject $plan
}

$plan | ConvertTo-Json -Depth 10
