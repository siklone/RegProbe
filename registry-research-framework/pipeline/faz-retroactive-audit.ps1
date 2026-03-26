[CmdletBinding()]
param(
    [string]$QueueCsv = (Join-Path (Split-Path -Parent $PSScriptRoot) "audit\re-audit-queue.csv"),
    [switch]$BootstrapEvidence
)

$scanner = Join-Path (Split-Path -Parent $PSScriptRoot) "audit\re-audit-scanner.ps1"
& $scanner -QueueCsv $QueueCsv
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($BootstrapEvidence.IsPresent) {
    & (Join-Path $PSScriptRoot "_invoke-phase.ps1") -Phase "all" -QueueOnly -QueueCsv $QueueCsv
}
