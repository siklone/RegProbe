[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GhidraEvidence,
    [Parameter(Mandatory = $true)]
    [string]$IdaEvidence,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script = Join-Path $repoRoot 'scripts\compare_static_cross_verification.py'
if (-not (Test-Path $script)) {
    throw "Missing cross-verification script: $script"
}

python $script --ghidra $GhidraEvidence --ida $IdaEvidence --output $OutputFile
exit $LASTEXITCODE
