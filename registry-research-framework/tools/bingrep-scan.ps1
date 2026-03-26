[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryPath,
    [Parameter(Mandatory = $true)]
    [string]$Pattern,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$strings = Get-Command strings.exe -ErrorAction SilentlyContinue
if (-not $strings) {
    throw "strings.exe is required for the current BinGrep-style wrapper."
}

$ascii = & $strings.Source -nobanner -accepteula -e a $BinaryPath | Select-String -SimpleMatch $Pattern
$wide = & $strings.Source -nobanner -accepteula -e l $BinaryPath | Select-String -SimpleMatch $Pattern
[pscustomobject]@{
    binary = $BinaryPath
    pattern = $Pattern
    ascii_hits = @($ascii).Count
    wide_hits = @($wide).Count
} | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputFile -Encoding utf8
