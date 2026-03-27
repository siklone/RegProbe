[CmdletBinding()]
param(
    [string]$AuditPath = (Join-Path $PWD "research\evidence-audit.json"),
    [string]$QueueCsv = '',
    [string]$ReportPath = ''
)

$auditRoot = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $PSScriptRoot
}
elseif (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
    Split-Path -Parent $PSCommandPath
}
else {
    Join-Path $PWD 'registry-research-framework\audit'
}

if ([string]::IsNullOrWhiteSpace($QueueCsv)) {
    $QueueCsv = Join-Path $auditRoot 're-audit-queue.csv'
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $auditRoot 're-audit-report.md'
}

$audit = Get-Content -Raw $AuditPath | ConvertFrom-Json
$entries = @($audit.entries | Where-Object { $_.re_audit_required -eq $true })
$queue = @(
    $entries |
        Sort-Object @{Expression = { $_.re_audit_priority }}, @{Expression = { $_.tweak_id }} |
        Select-Object tweak_id, evidence_class, official_doc_exists, lane, suspected_layer, re_audit_priority, re_audit_reason, cross_layer_satisfied, next_missing_layer, source_file
)

$queue | Export-Csv -Path $QueueCsv -NoTypeInformation -Encoding utf8

$lines = @()
$lines += "# Re-audit Report"
$lines += ""
$lines += "- Generated: $(Get-Date -Format o)"
$lines += "- Total queued: $($queue.Count)"
$lines += ""
$priority1 = @($queue | Where-Object { $_.re_audit_priority -eq 1 })
$priority2 = @($queue | Where-Object { $_.re_audit_priority -eq 2 })
$priority3 = @($queue | Where-Object { $_.re_audit_priority -ge 3 })
$lines += "## Priority summary"
$lines += ""
$lines += "| Priority | Count |"
$lines += "| --- | --- |"
$lines += "| 1 | $($priority1.Count) |"
$lines += "| 2 | $($priority2.Count) |"
$lines += "| 3+ | $($priority3.Count) |"
$lines += ""
$lines += "## Queue"
$lines += ""
$lines += "| Tweak | Class | Official | Lane | Layer | Priority | Reason |"
$lines += "| --- | --- | --- | --- | --- | --- | --- |"
foreach ($item in $queue) {
    $lines += "| $($item.tweak_id) | $($item.evidence_class) | $($item.official_doc_exists) | $($item.lane) | $($item.suspected_layer) | $($item.re_audit_priority) | $($item.re_audit_reason) |"
}

Set-Content -Path $ReportPath -Value ($lines -join "`n") -Encoding utf8
Write-Host "Wrote $QueueCsv"
Write-Host "Wrote $ReportPath"
