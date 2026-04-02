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
    $OutputRoot = Join-Path $repoRoot ("registry-research-framework\audit\source-enrichment-{0}" -f $stamp)
}

function Expand-SourceRoot {
    param([string]$Value)
    return [Environment]::ExpandEnvironmentVariables($Value)
}

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
            throw "git clone failed for $($source.id)"
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
