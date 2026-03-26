[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Phase,
    [string]$TweakId,
    [string]$QueueCsv,
    [switch]$QueueOnly,
    [switch]$ExecuteTools
)

$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
    throw "python is required to run the v3.1 pipeline wrappers."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptRoot "v31_pipeline.py"
$args = @($pythonScript, "--phase", $Phase)

if ($TweakId) {
    $args += @("--tweak-id", $TweakId)
}

if ($QueueOnly.IsPresent) {
    $args += "--queue-only"
}

if ($QueueCsv) {
    $args += @("--queue-csv", $QueueCsv)
}

if ($ExecuteTools.IsPresent) {
    $args += "--execute-tools"
}

& $python.Source @args
exit $LASTEXITCODE
