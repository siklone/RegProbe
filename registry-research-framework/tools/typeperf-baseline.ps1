[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Counter,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile,
    [int]$Samples = 60
)

typeperf $Counter -sc $Samples | Set-Content -Path $OutputFile -Encoding utf8
