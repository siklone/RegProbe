[CmdletBinding()]
param(
    [string]$OutputPath = "C:\regprobe-dpc-timer-etw",
    [int]$TraceSeconds = 45,
    [string]$UploadBaseUrl = "http://10.0.2.2:8766",
    [string]$UploadPrefix = "dpc-timer-etw"
)

$ErrorActionPreference = "Stop"

$runner = Join-Path $PSScriptRoot "run-dpc-timer-etw-trace-guest.ps1"
if (-not (Test-Path $runner)) {
    throw "DPC/timer ETW runner not found next to launcher: $runner"
}

& $runner `
    -OutputPath $OutputPath `
    -TraceSeconds $TraceSeconds `
    -UploadBaseUrl $UploadBaseUrl `
    -UploadPrefix $UploadPrefix
