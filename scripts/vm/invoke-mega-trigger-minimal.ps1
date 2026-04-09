[CmdletBinding()]
param(
    [ValidateSet('arm', 'run')]
    [string]$Phase = 'arm',

    [string]$GuestRoot = $(Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),

    [switch]$ForceManifestRefresh
)

$ErrorActionPreference = 'Stop'

$batchRoot = Join-Path $GuestRoot 'batch-mega-trigger'
$payloadPath = Join-Path $GuestRoot 'scripts\vm\run-power-control-batch-mega-trigger-runtime.guest.ps1'
$manifestPath = Join-Path $batchRoot 'manifest.json'
$statePath = Join-Path $batchRoot 'state.json'
$summaryPath = Join-Path $batchRoot 'summary.json'
$resultsPath = Join-Path $batchRoot 'results.json'
$phase0Path = Join-Path $GuestRoot 'registry-research-framework\audit\kernel-power-96-phase0-candidates-20260329.json'
$hitQueuePath = Join-Path $GuestRoot 'registry-research-framework\audit\kernel-power-96-broad-targeted-string-hit-queue-20260331.json'

$defaultCandidateIds = @(
    'power.control.allow-audio-to-enable-execution-required-power-requests',
    'power.control.allow-system-required-power-requests',
    'power.control.always-compute-qos-hints',
    'power.control.coalescing-flush-interval',
    'power.control.idle-processors-require-qos-management'
)

$defaultTriggers = @(
    'cpu_stress',
    'power_plan_and_requests',
    'multi_thread_burst',
    'disk_io_burst',
    'process_spawn_burst',
    'foreground_background_switch',
    'timer_resolution_change',
    'network_activity'
)

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$InputObject,
        [int]$Depth = 12
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -LiteralPath $Path -Encoding UTF8
}

function New-MinimalManifest {
    param(
        [Parameter(Mandatory = $true)][string]$Phase0Path,
        [Parameter(Mandatory = $true)][string]$HitQueuePath,
        [Parameter(Mandatory = $true)][string[]]$CandidateIds,
        [Parameter(Mandatory = $true)][string[]]$Triggers
    )

    if (-not (Test-Path -LiteralPath $Phase0Path)) {
        throw "Phase0 candidate audit not found: $Phase0Path"
    }

    if (-not (Test-Path -LiteralPath $HitQueuePath)) {
        throw "Hit queue audit not found: $HitQueuePath"
    }

    $phase0 = Read-JsonFile -Path $Phase0Path
    $hitQueue = Read-JsonFile -Path $HitQueuePath
    $powerGroup = @($hitQueue.hit_groups | Where-Object { $_.family -eq 'power-control' })
    if (@($powerGroup).Count -ne 1) {
        throw 'Could not resolve the power-control hit group from the broad hit queue.'
    }

    $allowedCandidateIds = @($powerGroup[0].candidate_ids)
    $candidates = @(
        foreach ($candidateId in $CandidateIds) {
            if ($allowedCandidateIds -notcontains $candidateId) {
                throw "Candidate id is not part of the power-control runtime family: $candidateId"
            }

            $candidate = @($phase0.candidates | Where-Object { $_.candidate_id -eq $candidateId }) | Select-Object -First 1
            if ($null -eq $candidate) {
                throw "Candidate metadata missing from phase0 manifest: $candidateId"
            }

            [ordered]@{
                candidate_id = [string]$candidate.candidate_id
                family = [string]$candidate.family
                route_bucket = [string]$candidate.route_bucket
                registry_path = [string]$candidate.registry_path
                value_name = [string]$candidate.value_name
                probe_value = 1
            }
        }
    )

    return [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        probe_name = ('manual-mega-trigger-{0}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
        snapshot_name = 'manual-kvm'
        collection_mode = 'manual'
        rollback_pending = $false
        family = 'power-control'
        pattern = 'mega-trigger'
        trigger_profile = 'pilot-safe-v1'
        candidate_count = @($candidates).Count
        candidate_ids = @($candidates | ForEach-Object { $_.candidate_id })
        triggers = @($Triggers)
        candidates = @($candidates)
    }
}

if (-not (Test-Path -LiteralPath $payloadPath)) {
    throw "Guest payload not found: $payloadPath"
}

New-Item -ItemType Directory -Path $batchRoot -Force | Out-Null

$manifestNeedsRefresh = $ForceManifestRefresh.IsPresent -or -not (Test-Path -LiteralPath $manifestPath)
if (-not $manifestNeedsRefresh) {
    try {
        $existingManifest = Read-JsonFile -Path $manifestPath
        if ($null -eq $existingManifest -or
            -not ($existingManifest.PSObject.Properties.Name -contains 'candidates') -or
            -not ($existingManifest.PSObject.Properties.Name -contains 'triggers') -or
            @($existingManifest.candidates).Count -eq 0 -or
            @($existingManifest.triggers).Count -eq 0) {
            $manifestNeedsRefresh = $true
        }
    }
    catch {
        $manifestNeedsRefresh = $true
    }
}

if ($manifestNeedsRefresh) {
    $manifest = New-MinimalManifest -Phase0Path $phase0Path -HitQueuePath $hitQueuePath -CandidateIds $defaultCandidateIds -Triggers $defaultTriggers
    Write-JsonFile -Path $manifestPath -InputObject $manifest
}

Write-Host "Phase: $Phase"
Write-Host "RepoRoot: $GuestRoot"
Write-Host "BatchRoot: $batchRoot"
Write-Host "PayloadPath: $payloadPath"
Write-Host "ManifestPath: $manifestPath"
Write-Host "StatePath: $statePath"
Write-Host "SummaryPath: $summaryPath"
Write-Host "ResultsPath: $resultsPath"
Write-Host "TracePath: $(Join-Path $GuestRoot 'mega-trace.etl')"

& powershell -NoProfile -ExecutionPolicy Bypass -File `
    $payloadPath `
    -Phase $Phase `
    -ManifestPath $manifestPath `
    -GuestRoot $GuestRoot `
    -StatePath $statePath `
    -SummaryPath $summaryPath `
    -ResultsPath $resultsPath
