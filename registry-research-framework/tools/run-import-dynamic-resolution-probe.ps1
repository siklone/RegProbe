[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryPath,
    [string]$Label = '',
    [string]$OutputFile = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$pythonScript = Join-Path $repoRoot 'scripts\find_dynamic_resolution_patterns.py'

if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $binaryName = [System.IO.Path]::GetFileNameWithoutExtension($BinaryPath)
    $OutputFile = Join-Path $repoRoot ("registry-research-framework\audit\dynamic-resolution-$binaryName-$stamp.json")
}

if (-not (Test-Path $BinaryPath)) {
    throw "Binary not found: $BinaryPath"
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputFile) -Force | Out-Null

$args = @(
    $pythonScript,
    '--binary', $BinaryPath,
    '--output', $OutputFile
)

if (-not [string]::IsNullOrWhiteSpace($Label)) {
    $args += @('--label', $Label)
}

& python @args
if ($LASTEXITCODE -ne 0) {
    throw "Dynamic resolution probe failed with exit code $LASTEXITCODE."
}

Get-Item $OutputFile | Select-Object FullName, Length, LastWriteTimeUtc
