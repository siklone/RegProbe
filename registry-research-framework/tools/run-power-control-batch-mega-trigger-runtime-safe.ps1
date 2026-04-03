[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$VmPath,

    [string]$VmProfile = '',

    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',

    [int]$RecentEventWindowHours = 24,

    [switch]$SkipEventLog,

    [switch]$SkipRunner,

    [string]$OutputRoot = '',

    [string[]]$RunnerArguments = @(),

    [string[]]$CandidateIds = @(),

    [switch]$RecoverOnly
)

$ErrorActionPreference = 'Stop'

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [object]$InputObject,

        [int]$Depth = 10
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$helperPath = Join-Path $repoRoot 'scripts\vm\test-vm-storage-health.ps1'
$runnerPath = Join-Path $PSScriptRoot 'run-power-control-batch-mega-trigger-runtime.ps1'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$sessionId = "power-control-batch-mega-trigger-runtime-storage-preflight-$timestamp"
$sessionRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    Join-Path $repoRoot "evidence\files\vm-tooling-staging\$sessionId"
}
else {
    Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) $sessionId
}

$preflightPath = Join-Path $sessionRoot 'storage-preflight.json'
$summaryPath = Join-Path $sessionRoot 'summary.json'
$resultsPath = Join-Path $sessionRoot 'results.json'
$sessionPath = Join-Path $sessionRoot 'session.json'

$preflightParams = @{
    VmPath = $VmPath
    RecentEventWindowHours = $RecentEventWindowHours
    OutputPath = $preflightPath
}
if ($SkipEventLog) {
    $preflightParams['SkipEventLog'] = $true
}

$preflight = & $helperPath @preflightParams
$findingCodes = @($preflight.findings | ForEach-Object { [string]$_.code })

if ($preflight.status -eq 'unsafe') {
    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        status = 'storage-unsafe'
        collection_mode = $CollectionMode
        vm_path = $preflight.vm_path
        preflight_status = $preflight.status
        finding_codes = @($findingCodes)
        runner_invoked = $false
        note = 'Host storage preflight failed. Do not trust runtime evidence from this VM path until storage is repaired or the VM is migrated.'
    }
    $results = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        status = 'storage-unsafe'
        collection_mode = $CollectionMode
        preflight = $preflight
        candidates = @()
    }
    $session = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        session_id = $sessionId
        status = 'storage-unsafe'
        vm_path = $preflight.vm_path
        collection_mode = $CollectionMode
        preflight_path = $preflightPath
        summary_path = $summaryPath
        results_path = $resultsPath
    }

    Write-JsonFile -Path $summaryPath -InputObject $summary
    Write-JsonFile -Path $resultsPath -InputObject $results
    Write-JsonFile -Path $sessionPath -InputObject $session
    Write-Warning ("VM storage preflight reported unsafe status for {0}. Session: {1}" -f $preflight.vm_path, $sessionId)
    return $summary
}

if ($SkipRunner) {
    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        status = 'preflight-ok'
        collection_mode = $CollectionMode
        vm_path = $preflight.vm_path
        preflight_status = $preflight.status
        finding_codes = @($findingCodes)
        runner_invoked = $false
    }
    Write-JsonFile -Path $summaryPath -InputObject $summary
    Write-JsonFile -Path $resultsPath -InputObject ([ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            family = 'power-control'
            pattern = 'mega-trigger'
            status = 'preflight-ok'
            preflight = $preflight
            candidates = @()
        })
    Write-JsonFile -Path $sessionPath -InputObject ([ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            session_id = $sessionId
            status = 'preflight-ok'
            vm_path = $preflight.vm_path
            collection_mode = $CollectionMode
            preflight_path = $preflightPath
            summary_path = $summaryPath
            results_path = $resultsPath
        })
    return $summary
}

if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw "Mega-trigger runner was not found: $runnerPath"
}

$runnerParams = @{
    VmPath = $VmPath
    CollectionMode = $CollectionMode
}
if (-not [string]::IsNullOrWhiteSpace($VmProfile)) {
    $runnerParams['VmProfile'] = $VmProfile
}
if (@($CandidateIds).Count -gt 0) {
    $runnerParams['CandidateIds'] = @($CandidateIds)
}
if ($RecoverOnly) {
    $runnerParams['RecoverOnly'] = $true
}

& $runnerPath @runnerParams @RunnerArguments
