[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryPath,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$floss = Get-Command floss.exe -ErrorAction SilentlyContinue
if (-not $floss) {
    throw "floss.exe is not installed in this environment."
}

& $floss.Source $BinaryPath | Set-Content -Path $OutputFile -Encoding utf8
