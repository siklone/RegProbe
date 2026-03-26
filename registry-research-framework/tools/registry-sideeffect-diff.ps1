[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BeforeFile,
    [Parameter(Mandatory = $true)]
    [string]$AfterFile,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$before = Get-Content -Path $BeforeFile
$after = Get-Content -Path $AfterFile
Compare-Object -ReferenceObject $before -DifferenceObject $after |
    Out-String |
    Set-Content -Path $OutputFile -Encoding utf8
