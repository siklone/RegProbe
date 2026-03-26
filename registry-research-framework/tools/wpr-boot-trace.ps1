[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$payload = [pscustomobject]@{
    tool = "WPR"
    trace_kind = "boot"
    output_file = $OutputFile
    status = "staged"
    note = "Use the VM WPR lane for live boot tracing. This wrapper reserves the output contract."
}

$payload | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputFile -Encoding utf8
