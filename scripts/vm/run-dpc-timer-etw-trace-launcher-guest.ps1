[CmdletBinding()]
param(
    [string]$OutputPath = "C:\regprobe-dpc-timer-etw",
    [int]$TraceSeconds = 45,
    [string]$UploadBaseUrl = $(if ($env:REGPROBE_VM_BRIDGE_BASE_URL) { $env:REGPROBE_VM_BRIDGE_BASE_URL } else { "http://10.0.2.2:8766" }),
    [string]$UploadPrefix = "dpc-timer-etw"
)

$ErrorActionPreference = "Stop"

$runner = Join-Path $PSScriptRoot "run-dpc-timer-etw-trace-guest.ps1"
if (-not (Test-Path $runner)) {
    $runnerItem = Get-ChildItem -Path $PSScriptRoot -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like "run-dpc-timer-etw-trace-gue*.ps1" -or
            $_.Name -like "RUN_DPC_TIMER_ETW_TRACE_GUE*.PS1"
        } |
        Sort-Object Name |
        Select-Object -First 1
    if (-not $runnerItem) {
        throw "DPC/timer ETW runner not found next to launcher: $runner"
    }
    $runner = $runnerItem.FullName
}

& $runner `
    -OutputPath $OutputPath `
    -TraceSeconds $TraceSeconds `
    -UploadBaseUrl $UploadBaseUrl `
    -UploadPrefix $UploadPrefix
