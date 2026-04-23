$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$LocalDotnetCandidates = @(
    (Join-Path $RepoRoot '.tools/dotnet/dotnet.exe'),
    (Join-Path $RepoRoot '.tools/dotnet/dotnet')
)

if (-not $env:DOTNET_CLI_TELEMETRY_OPTOUT) {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
}

foreach ($candidate in $LocalDotnetCandidates) {
    if (Test-Path $candidate) {
        $env:DOTNET_ROOT = Split-Path -Parent $candidate
        & $candidate @args
        exit $LASTEXITCODE
    }
}

$globalDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -ne $globalDotnet) {
    & $globalDotnet.Source @args
    exit $LASTEXITCODE
}

throw 'dotnetw.ps1: unable to find a repo-local SDK under .tools/dotnet or a global dotnet on PATH.'
