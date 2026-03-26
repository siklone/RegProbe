[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputFile,
    [string]$SessionName = "OpenTraceRegistry"
)

$payload = [pscustomobject]@{
    tool = "ETW"
    session_name = $SessionName
    output_file = $OutputFile
    status = "staged"
    note = "Wire this wrapper to the VM-side registry ETW capture lane before using it for live validation."
}

$payload | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputFile -Encoding utf8
Write-Host "Wrote staged ETW trace manifest to $OutputFile"
