[CmdletBinding()]
param(
    [string]$CandidateManifest = '',
    [string]$SourceConfig = '',
    [string]$OutputRoot = '',
    [string]$Family = '',
    [string[]]$CandidateIds = @(),
    [string[]]$SourceIds = @(),
    [switch]$CloneMissing
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$pythonScript = Join-Path $repoRoot 'scripts\source_enrichment_scan.py'
if (-not (Test-Path -LiteralPath $pythonScript)) {
    throw "Missing source enrichment scanner: $pythonScript"
}

if ([string]::IsNullOrWhiteSpace($CandidateManifest)) {
    $CandidateManifest = Join-Path $repoRoot 'registry-research-framework\audit\kernel-power-96-phase0-candidates-20260329.json'
}

if ([string]::IsNullOrWhiteSpace($SourceConfig)) {
    $SourceConfig = Join-Path $repoRoot 'registry-research-framework\config\source-enrichment-sources.json'
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputRoot = Join-Path $repoRoot ("registry-research-framework\enrichment\outputs\source-enrichment-{0}" -f $stamp)
}

$outputLeaf = Split-Path -Leaf $OutputRoot
$auditPath = Join-Path $repoRoot ("registry-research-framework\audit\{0}.json" -f $outputLeaf)
$notePath = Join-Path $repoRoot ("research\notes\{0}.md" -f $outputLeaf)
$dateToken = if ($outputLeaf -match '(\d{8})-\d{6}$') { $Matches[1] } else { Get-Date -Format 'yyyyMMdd' }
$priorityAuditPath = Join-Path $repoRoot ("registry-research-framework\audit\source-enrichment-priority-queue-{0}.json" -f $dateToken)

function Expand-SourceRoot {
    param([string]$Value)
    return [Environment]::ExpandEnvironmentVariables($Value)
}

$cloneFailures = @()
if ($CloneMissing) {
    $config = Get-Content -LiteralPath $SourceConfig -Raw | ConvertFrom-Json
    $selectedSources = if (@($SourceIds).Count -gt 0) {
        @($config.sources | Where-Object { $SourceIds -contains $_.id })
    }
    else {
        @($config.sources | Where-Object { $_.enabled_by_default -or $_.git_url })
    }

    foreach ($source in $selectedSources) {
        if (-not $source.git_url) {
            continue
        }

        $expandedRoot = Expand-SourceRoot -Value ([string]$source.root)
        if (Test-Path -LiteralPath $expandedRoot) {
            continue
        }

        $parent = Split-Path -Parent $expandedRoot
        if (-not [string]::IsNullOrWhiteSpace($parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }

        & git clone --depth 1 $source.git_url $expandedRoot
        if ($LASTEXITCODE -ne 0) {
            $cloneFailures += [ordered]@{
                id = $source.id
                git_url = $source.git_url
                root = $expandedRoot
                exit_code = $LASTEXITCODE
            }
            Write-Warning "git clone failed for $($source.id); continuing with remaining sources"
        }
    }
}

$args = @(
    $pythonScript,
    '--candidate-manifest', $CandidateManifest,
    '--source-config', $SourceConfig,
    '--output-root', $OutputRoot
)
if (-not [string]::IsNullOrWhiteSpace($Family)) {
    $args += @('--family', $Family)
}
foreach ($candidateId in @($CandidateIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
    $args += @('--candidate-id', $candidateId)
}
foreach ($sourceId in @($SourceIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
    $args += @('--source-id', $sourceId)
}

& python @args
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$masterPath = Join-Path $OutputRoot 'master-enrichment.json'
if (-not (Test-Path -LiteralPath $masterPath)) {
    throw "Missing master enrichment output: $masterPath"
}

$master = Get-Content -LiteralPath $masterPath -Raw | ConvertFrom-Json
$audit = [ordered]@{
    generated_utc = $master.generated_utc
    candidate_manifest = $master.candidate_manifest
    source_config = $master.source_config
    output_root = $master.output_root
    master_enrichment = $masterPath
    source_index = Join-Path $OutputRoot 'source-index.json'
    priority_queue = Join-Path $OutputRoot 'priority-queue.json'
    markdown_summary = Join-Path $OutputRoot 'master-enrichment.md'
    total_candidates = $master.total_candidates
    total_sources = $master.total_sources
    route_counts = $master.route_counts
    trigger_family_counts = $master.trigger_family_counts
    sources = @($master.sources | ForEach-Object {
        [ordered]@{
            id = $_.id
            exists = $_.exists
            missing_reason = $_.missing_reason
            hit_count = $_.hit_count
            candidate_hit_count = $_.candidate_hit_count
            surface_group = $_.surface_group
        }
    })
    priority_queue_counts = [ordered]@{
        high_priority_runtime = @($master.priority_queue.high_priority_runtime).Count
        high_priority_windbg = @($master.priority_queue.high_priority_windbg).Count
        low_priority_hold = @($master.priority_queue.low_priority_hold).Count
    }
    top_runtime_candidates = @($master.priority_queue.high_priority_runtime | Select-Object -First 10)
    top_windbg_candidates = @($master.priority_queue.high_priority_windbg | Select-Object -First 10)
    clone_failures = @($cloneFailures)
}

$auditJson = $audit | ConvertTo-Json -Depth 8
Set-Content -LiteralPath $auditPath -Value $auditJson -Encoding UTF8

$candidateIndex = @{}
foreach ($candidate in @($master.candidates)) {
    $candidateIndex[[string]$candidate.candidate_id] = $candidate
}

$priorityAudit = [ordered]@{
    generated_utc = $master.generated_utc
    output_root = $master.output_root
    source_audit = $auditPath
    high_priority_runtime = @($master.priority_queue.high_priority_runtime | ForEach-Object {
        $candidate = $candidateIndex[[string]$_]
        [ordered]@{
            candidate_id = $_
            enrichment_score = $candidate.enrichment_score
            support_count = $candidate.support_count
            suggested_trigger_family = $candidate.suggested_trigger_family
            suggested_trigger = @($candidate.suggested_trigger)
            route_bucket = $candidate.route_bucket
        }
    })
    high_priority_windbg = @($master.priority_queue.high_priority_windbg | ForEach-Object {
        $candidate = $candidateIndex[[string]$_]
        [ordered]@{
            candidate_id = $_
            enrichment_score = $candidate.enrichment_score
            support_count = $candidate.support_count
            suggested_trigger_family = $candidate.suggested_trigger_family
            suggested_trigger = @($candidate.suggested_trigger)
            route_bucket = $candidate.route_bucket
        }
    })
    low_priority_hold = @($master.priority_queue.low_priority_hold | ForEach-Object {
        $candidate = $candidateIndex[[string]$_]
        [ordered]@{
            candidate_id = $_
            enrichment_score = $candidate.enrichment_score
            support_count = $candidate.support_count
            suggested_trigger_family = $candidate.suggested_trigger_family
            suggested_trigger = @($candidate.suggested_trigger)
            route_bucket = $candidate.route_bucket
        }
    })
}
$priorityAuditJson = $priorityAudit | ConvertTo-Json -Depth 8
Set-Content -LiteralPath $priorityAuditPath -Value $priorityAuditJson -Encoding UTF8

$noteLines = @(
    "# Source Enrichment Wave",
    "",
    "- Generated: ``$($master.generated_utc)``",
    "- Output root: ``$($master.output_root)``",
    "- Candidate manifest: ``$($master.candidate_manifest)``",
    "- Sources: ``$($master.total_sources)``",
    "- Candidates: ``$($master.total_candidates)``",
    "",
    "## Source Snapshot"
)
$noteLines += @($master.sources | ForEach-Object {
    $state = if ($_.exists) { 'present' } else { "missing ($($_.missing_reason))" }
    "- ``$($_.id)``: $state, hits ``$($_.hit_count)``"
})
$noteLines += @(
    "",
    "## Priority Queue",
    "- Runtime: ``$(@($master.priority_queue.high_priority_runtime).Count)``",
    "- WinDbg: ``$(@($master.priority_queue.high_priority_windbg).Count)``",
    "- Hold: ``$(@($master.priority_queue.low_priority_hold).Count)``",
    "",
    "## First-Run Preview",
    "- Runtime queue heads: ``$((@($master.priority_queue.high_priority_runtime) | Select-Object -First 5) -join ', ')``",
    "- WinDbg queue heads: ``$((@($master.priority_queue.high_priority_windbg) | Select-Object -First 5) -join ', ')``"
)
if (@($cloneFailures).Count -gt 0) {
    $noteLines += @(
        "",
        "## Clone Failures"
    )
    $noteLines += @($cloneFailures | ForEach-Object { "- ``$($_.id)``: exit ``$($_.exit_code)``" })
}

Set-Content -LiteralPath $notePath -Value (($noteLines -join "`n") + "`n") -Encoding UTF8

Write-Host $auditPath
Write-Host $notePath
Write-Host $priorityAuditPath
