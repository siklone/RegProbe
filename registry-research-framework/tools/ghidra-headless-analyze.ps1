[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryPath,
    [Parameter(Mandatory = $true)]
    [string]$SearchTerm
)

$script = Join-Path (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "scripts\vm") "run-ghidra-string-xref-probe.ps1"
if (-not (Test-Path $script)) {
    throw "Missing existing Ghidra probe wrapper: $script"
}

& $script -BinaryPath $BinaryPath -SearchTerm $SearchTerm
exit $LASTEXITCODE
