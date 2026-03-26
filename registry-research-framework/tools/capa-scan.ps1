[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryPath,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$capa = Get-Command capa.exe -ErrorAction SilentlyContinue
if (-not $capa) {
    throw "capa.exe is not installed in this environment."
}

& $capa.Source -j $BinaryPath | Set-Content -Path $OutputFile -Encoding utf8
