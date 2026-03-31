[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetBinary,
    [Parameter(Mandatory = $true)]
    [string]$OutputName,
    [Parameter(Mandatory = $true)]
    [string[]]$Patterns
)

$script = Join-Path (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts\vm') 'run-ida-string-xref-probe.ps1'
if (-not (Test-Path $script)) {
    throw "Missing IDA probe wrapper: $script"
}

$args = @('-TargetBinary', $TargetBinary, '-OutputName', $OutputName)
foreach ($pattern in $Patterns) {
    $args += @('-Patterns', $pattern)
}

& $script @args
exit $LASTEXITCODE
