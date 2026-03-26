[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryPath,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$symchk = Get-Command symchk.exe -ErrorAction SilentlyContinue
if (-not $symchk) {
    throw "symchk.exe is not installed in this environment."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
& $symchk.Source /r $BinaryPath /s "SRV*$OutputDirectory*https://msdl.microsoft.com/download/symbols"
exit $LASTEXITCODE
