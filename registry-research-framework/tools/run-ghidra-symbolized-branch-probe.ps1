[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetBinary,
    [Parameter(Mandatory = $true)]
    [string]$OutputName,
    [Parameter(Mandatory = $true)]
    [string[]]$Patterns,
    [switch]$NoAnalysis
)

$script = Join-Path (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts\vm') 'run-ghidra-symbolized-branch-probe.ps1'
if (-not (Test-Path $script)) {
    throw "Missing v3.2 Ghidra probe wrapper: $script"
}

$args = @('-TargetBinary', $TargetBinary, '-OutputName', $OutputName)
foreach ($pattern in $Patterns) {
    $args += @('-Patterns', $pattern)
}
if ($NoAnalysis) {
    $args += '-NoAnalysis'
}

& $script @args
exit $LASTEXITCODE
