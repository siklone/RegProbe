[CmdletBinding()]
param(
    [string]$ArtifactRoot = 'H:\Temp\vm-tooling-staging',
    [string]$RepoRoot = '',
    [string]$ReportPath = '',
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $ArtifactRoot 'cleanup-host-report.json'
}

if (-not (Test-Path $ArtifactRoot)) {
    throw "Artifact root not found: $ArtifactRoot"
}

$scanRoots = @(
    (Join-Path $RepoRoot 'research'),
    (Join-Path $RepoRoot 'Docs\VM_WORKFLOW.md')
)

$artifactPattern = [regex]'H:\\Temp\\vm-tooling-staging[^"''`\r\n\t <>\]\)]+'
$referencedTopLevel = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($scanRoot in $scanRoots) {
    if (-not (Test-Path $scanRoot)) {
        continue
    }

    $files = if ((Get-Item $scanRoot).PSIsContainer) { Get-ChildItem $scanRoot -Recurse -File } else { Get-Item $scanRoot }
    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $content) {
            continue
        }
        foreach ($match in $artifactPattern.Matches($content)) {
            $path = $match.Value.TrimEnd('.', ',', ';', ')', ']', '"', '''')
            if ($path.StartsWith($ArtifactRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                $relative = $path.Substring($ArtifactRoot.Length).TrimStart('\')
                if ($relative) {
                    $top = $relative.Split('\')[0]
                    [void]$referencedTopLevel.Add($top)
                }
            }
        }
    }
}

$explicitKeepPatterns = @(
    '^installers$',
    '^ui-check-publish$',
    '^tool-health.*',
    '^app-launch-smoke.*',
    '^cleanup-.*report\.json$',
    '^ttd-check\.txt$',
    '^windowsdesktop-runtime-.*\.exe$',
    '^ghidra_.*\.zip$',
    '^OpenJDK.*\.zip$'
)

$items = Get-ChildItem $ArtifactRoot -Force
$deleted = @()
$kept = @()

foreach ($item in $items) {
    $keep = $referencedTopLevel.Contains($item.Name)
    if (-not $keep) {
        $keep = [bool]($explicitKeepPatterns | Where-Object { $item.Name -match $_ } | Select-Object -First 1)
    }

    if ($keep) {
        $kept += [pscustomobject]@{
            name = $item.Name
            path = $item.FullName
            deleted = $false
        }
        continue
    }

    $entry = [pscustomobject]@{
        name = $item.Name
        path = $item.FullName
        deleted = [bool]$Apply
    }
    $deleted += $entry

    if ($Apply) {
        Remove-Item -Path $item.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$report = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    artifact_root = $ArtifactRoot
    applied = [bool]$Apply
    referenced_top_level = @($referencedTopLevel)
    kept = $kept
    candidates = $deleted
}

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $ReportPath -Encoding UTF8
Write-Output $ReportPath
