[CmdletBinding()]
param(
    [string]$RepoRoot = '',
    [switch]$WriteArtifacts = $true
)

$ErrorActionPreference = 'Stop'
$repoRoot = if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
}
else {
    [System.IO.Path]::GetFullPath($RepoRoot)
}
$inventoryRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$auditPath = Join-Path $repoRoot 'registry-research-framework\audit\verification-vm-tooling-staging-inventory-20260403.json'
$notePath = Join-Path $repoRoot 'research\notes\verification-vm-tooling-staging-inventory-20260403.md'

if (-not (Test-Path -LiteralPath $inventoryRoot)) {
    throw "vm-tooling-staging inventory root not found: $inventoryRoot"
}

function Get-RelativeRepoPath {
    param([string]$Path)

    $full = [System.IO.Path]::GetFullPath($Path)
    if ($full.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/')
    }

    return $full
}

function Get-InventorySummary {
    param(
        [string]$RootPath
    )

    $items = @(Get-ChildItem -LiteralPath $RootPath -Force -Recurse -ErrorAction SilentlyContinue)
    $dirs = @($items | Where-Object { $_.PSIsContainer })
    $files = @($items | Where-Object { -not $_.PSIsContainer })
    $now = [DateTime]::UtcNow
    $protectedRefs = @(
        'registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json',
        'registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json',
        'registry-research-framework/audit/power-control-mega-trigger-v2-status-20260402.json',
        'registry-research-framework/audit/configure-kernel-debug-baseline.json',
        'registry-research-framework/audit/bcd-current-20260403.txt'
    )

    $groups = [ordered]@{
        'windbg-boot-registry-trace' = @($dirs | Where-Object { $_.Name -like 'windbg-boot-registry-trace-*' })
        'power-control-batch-mega-trigger-runtime-primary' = @($dirs | Where-Object { $_.Name -like 'power-control-batch-mega-trigger-runtime-primary-*' })
        'power-control-batch-mega-trigger-runtime-storage-preflight' = @($dirs | Where-Object { $_.Name -like 'power-control-batch-mega-trigger-runtime-storage-preflight-*' })
        'cpu-idle' = @($dirs | Where-Object { $_.Name -like 'cpu-idle-*' })
        'other' = @($dirs | Where-Object {
            $_.Name -notlike 'windbg-boot-registry-trace-*' -and
            $_.Name -notlike 'power-control-batch-mega-trigger-runtime-primary-*' -and
            $_.Name -notlike 'power-control-batch-mega-trigger-runtime-storage-preflight-*' -and
            $_.Name -notlike 'cpu-idle-*'
        })
    }

    $cleanupReviewCandidates = @(
        $dirs | Where-Object {
            $_.LastWriteTimeUtc -lt $now.AddDays(-7) -and
            $_.FullName -notmatch '\\(windbg-boot-registry-trace|power-control-batch-mega-trigger-runtime-primary|power-control-batch-mega-trigger-runtime-storage-preflight)-' 
        } | Select-Object -First 30
    )

    $summary = [ordered]@{
        generated_utc = $now.ToString('o')
        inventory_root = Get-RelativeRepoPath -Path $RootPath
        directory_count = $dirs.Count
        file_count = $files.Count
        group_counts = [ordered]@{}
        latest_items = @(
            $items | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 12 | ForEach-Object {
                [ordered]@{
                    path = Get-RelativeRepoPath -Path $_.FullName
                    kind = if ($_.PSIsContainer) { 'directory' } else { 'file' }
                    last_write_utc = $_.LastWriteTimeUtc.ToString('o')
                    size = if ($_.PSIsContainer) { $null } else { [int64]$_.Length }
                }
            }
        )
        age_buckets = [ordered]@{
            older_than_7_days = @($dirs | Where-Object { $_.LastWriteTimeUtc -lt $now.AddDays(-7) } | Measure-Object).Count
            older_than_14_days = @($dirs | Where-Object { $_.LastWriteTimeUtc -lt $now.AddDays(-14) } | Measure-Object).Count
            older_than_30_days = @($dirs | Where-Object { $_.LastWriteTimeUtc -lt $now.AddDays(-30) } | Measure-Object).Count
        }
        cleanup_review_candidates = @(
            $cleanupReviewCandidates | ForEach-Object {
                [ordered]@{
                    path = Get-RelativeRepoPath -Path $_.FullName
                    last_write_utc = $_.LastWriteTimeUtc.ToString('o')
                }
            }
        )
        protected_refs = $protectedRefs
    }

    foreach ($groupName in $groups.Keys) {
        $groupItems = @($groups[$groupName])
        $summary.group_counts[$groupName] = [ordered]@{
            count = $groupItems.Count
            newest = if ($groupItems.Count -gt 0) { ($groupItems | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc.ToString('o') } else { $null }
            oldest = if ($groupItems.Count -gt 0) { ($groupItems | Sort-Object LastWriteTimeUtc | Select-Object -First 1).LastWriteTimeUtc.ToString('o') } else { $null }
        }
    }

    return $summary
}

$summary = Get-InventorySummary -RootPath $inventoryRoot
$summaryJson = $summary | ConvertTo-Json -Depth 8

$transportBundlePath = Join-Path $repoRoot 'registry-research-framework\audit\power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json'
$transportStatusPath = Join-Path $repoRoot 'registry-research-framework\audit\power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json'
if (Test-Path -LiteralPath $transportBundlePath) {
    $transportBundle = Get-Content -LiteralPath $transportBundlePath -Raw | ConvertFrom-Json
    foreach ($property in @('target_key', 'trace_profile', 'runner_output_policy')) {
        if ([string]::IsNullOrWhiteSpace([string]$transportBundle.$property)) {
            throw "Transport bundle contract missing property '$property'."
        }
    }
}

if (Test-Path -LiteralPath $transportStatusPath) {
    $transportStatus = Get-Content -LiteralPath $transportStatusPath -Raw | ConvertFrom-Json
    foreach ($property in @('current_status', 'bundle', 'implemented_guards')) {
        if ([string]::IsNullOrWhiteSpace([string]$transportStatus.$property)) {
            throw "Transport status contract missing property '$property'."
        }
    }
    if (-not @($transportStatus.profiles).Count) {
        throw 'Transport status contract missing profiles.'
    }
    if (-not @($transportStatus.transport_error_signature).Count) {
        throw 'Transport status contract missing transport_error_signature.'
    }
}

$latestTransportSession = Get-ChildItem -LiteralPath $inventoryRoot -Directory -Filter 'windbg-boot-registry-trace-*' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1
if ($latestTransportSession) {
    $summaryPath = Join-Path $latestTransportSession.FullName 'summary.json'
    if (Test-Path -LiteralPath $summaryPath) {
        $transportSummary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
        if ($null -eq $transportSummary.runner_output -or $null -eq $transportSummary.runner_output.console) {
            throw 'Transport summary runner_output contract failed.'
        }
        if (-not ($transportSummary.capture_artifacts | Where-Object { $_.path -and $_.sha256 -and $_.size -ne $null -and $_.collected_utc })) {
            throw 'Transport summary capture_artifacts contract failed.'
        }
    }
}

if ($WriteArtifacts) {
    $auditParent = Split-Path -Parent $auditPath
    $noteParent = Split-Path -Parent $notePath
    if (-not [string]::IsNullOrWhiteSpace($auditParent)) {
        New-Item -ItemType Directory -Path $auditParent -Force | Out-Null
    }
    if (-not [string]::IsNullOrWhiteSpace($noteParent)) {
        New-Item -ItemType Directory -Path $noteParent -Force | Out-Null
    }

    Set-Content -LiteralPath $auditPath -Value $summaryJson -Encoding UTF8

    $protectedRefLines = @($summary.protected_refs | ForEach-Object { '- `' + $_ + '`' })
    $note = @(
        '# VM Tooling Staging Inventory'
        ''
        ('Date: `{0}`' -f (Get-Date -Format 'yyyy-MM-dd'))
        ''
        'This is a verification-only inventory of `evidence/files/vm-tooling-staging`.'
        ''
        'Safe cleanup boundary:'
        ''
        '- Do not delete any directory or file referenced by the current WinDbg / debug-baseline audit artifacts.'
        '- Do not delete anything while it is still referenced by active summary, results, or status JSON.'
        '- Treat all entries under `vm-tooling-staging` as scratch until a cleanup review proves otherwise.'
        ''
        'Protected refs:'
        ''
    ) + $protectedRefLines + @(
        ''
        'Inventory snapshot:'
        ''
        ('- Directory count: `{0}`' -f $summary.directory_count)
        ('- File count: `{0}`' -f $summary.file_count)
        ('- WindDbg trace directories: `{0}`' -f $summary.group_counts.'windbg-boot-registry-trace'.count)
        ('- Power-control primary sessions: `{0}`' -f $summary.group_counts.'power-control-batch-mega-trigger-runtime-primary'.count)
        ('- Power-control storage preflight sessions: `{0}`' -f $summary.group_counts.'power-control-batch-mega-trigger-runtime-storage-preflight'.count)
        ('- CPU idle sessions: `{0}`' -f $summary.group_counts.'cpu-idle'.count)
        ''
        'Cleanup review candidates are listed in the JSON report only; no destructive cleanup is performed.'
    )
    Set-Content -LiteralPath $notePath -Value $note -Encoding UTF8
}

[pscustomobject]@{
    generated_utc = $summary.generated_utc
    inventory_root = $summary.inventory_root
    directory_count = $summary.directory_count
    file_count = $summary.file_count
    report_path = Get-RelativeRepoPath -Path $auditPath
    note_path = Get-RelativeRepoPath -Path $notePath
    cleanup_review_candidates = @($summary.cleanup_review_candidates)
} | ConvertTo-Json -Depth 8
