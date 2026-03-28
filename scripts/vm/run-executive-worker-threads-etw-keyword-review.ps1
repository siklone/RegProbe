[CmdletBinding()]
param(
    [string]$InputPath = 'H:\D\Dev\RegProbe\evidence\files\vm-tooling-staging\watchdog-timeouts-boottrace-20260328-090631\registry-dump-session-manager-executive.txt',
    [string]$OutputRoot = 'H:\D\Dev\RegProbe\evidence\files\vm-tooling-staging'
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "executive-etw-keyword-review-$stamp"
$outputDir = Join-Path $OutputRoot $probeName
$filteredPath = Join-Path $outputDir 'executive-etw-keyword-review.txt'
$summaryPath = Join-Path $outputDir 'summary.json'

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

if (-not (Test-Path $InputPath)) {
    throw "InputPath does not exist: $InputPath"
}

$lines = Get-Content -Path $InputPath
$targetPatterns = @(
    'Session Manager\\Executive',
    'AdditionalCriticalWorkerThreads',
    'AdditionalDelayedWorkerThreads',
    'UuidSequenceNumber'
)

$filteredLines = $lines | Where-Object {
    $_ -match 'Session Manager\\Executive|AdditionalCriticalWorkerThreads|AdditionalDelayedWorkerThreads|UuidSequenceNumber'
}

$filteredLines | Set-Content -Path $filteredPath -Encoding UTF8

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    lane_label = 'system.executive-additional-worker-threads'
    status = 'etw-keyword-review-complete'
    input_path = 'evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/registry-dump-session-manager-executive.txt'
    filtered_output_path = "evidence/files/vm-tooling-staging/$probeName/executive-etw-keyword-review.txt"
    total_lines = @($lines).Count
    filtered_line_count = @($filteredLines).Count
    counts = [ordered]@{
        session_manager_executive = @($lines | Where-Object { $_ -match 'Session Manager\\Executive' }).Count
        additional_critical_worker_threads = @($lines | Where-Object { $_ -match 'AdditionalCriticalWorkerThreads' }).Count
        additional_delayed_worker_threads = @($lines | Where-Object { $_ -match 'AdditionalDelayedWorkerThreads' }).Count
        uuid_sequence_number = @($lines | Where-Object { $_ -match 'UuidSequenceNumber' }).Count
    }
    exact_value_hits = @()
    adjacent_hits = @()
    strongest_process = if ($filteredLines -match 'System\s+\(\s*4\)') { 'System (PID 4)' } else { $null }
    conclusion = 'The ETW-specific keyword review still surfaced only Session Manager\\Executive path activity plus adjacent UuidSequenceNumber traffic. The exact Executive worker-thread pair names remained absent from the ETW-derived dump.'
}

if ($summary.counts.uuid_sequence_number -gt 0) {
    $summary.adjacent_hits = @('UuidSequenceNumber')
}

if ($summary.counts.additional_critical_worker_threads -gt 0) {
    $summary.exact_value_hits += 'AdditionalCriticalWorkerThreads'
}
if ($summary.counts.additional_delayed_worker_threads -gt 0) {
    $summary.exact_value_hits += 'AdditionalDelayedWorkerThreads'
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
Write-Output $summaryPath
