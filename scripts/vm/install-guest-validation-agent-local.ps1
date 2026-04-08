[CmdletBinding()]
param(
    [string]$SourceRoot = $PSScriptRoot,
    [string]$InstallRoot = 'C:\Tools\Scripts',
    [string]$SharedRoot = 'C:\Tools\ValidationController',
    [string]$TaskName = 'RegProbeValidationAgent',
    [switch]$RegisterStartupTask
)

$ErrorActionPreference = 'Stop'

$guestAgentSource = Join-Path $SourceRoot 'guest-validation-agent.ps1'
$restartHelperSource = Join-Path $SourceRoot 'request-guest-restart.ps1'
$guestAgentTarget = Join-Path $InstallRoot 'guest-validation-agent.ps1'
$restartHelperTarget = Join-Path $InstallRoot 'request-guest-restart.ps1'
$launchCmdTarget = Join-Path $InstallRoot 'run-guest-validation-agent-local.cmd'
$controllerCurrentRoot = Join-Path $SharedRoot 'controller\current'
$artifactsRoot = Join-Path $controllerCurrentRoot 'artifacts'

if (-not (Test-Path $guestAgentSource)) {
    throw "Guest validation agent source not found at $guestAgentSource"
}

if (-not (Test-Path $restartHelperSource)) {
    throw "Guest restart helper source not found at $restartHelperSource"
}

New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
New-Item -ItemType Directory -Force -Path $controllerCurrentRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

Copy-Item -Path $guestAgentSource -Destination $guestAgentTarget -Force
Copy-Item -Path $restartHelperSource -Destination $restartHelperTarget -Force

$launchCmd = @"
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$guestAgentTarget" -SharedRoot "$SharedRoot"
"@
$launchCmd | Set-Content -Path $launchCmdTarget -Encoding ASCII

if ($RegisterStartupTask) {
    $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$guestAgentTarget`" -SharedRoot `"$SharedRoot`""
    $trigger = New-ScheduledTaskTrigger -AtStartup
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -User 'SYSTEM' -RunLevel Highest -Force | Out-Null
}

Write-Host "Installed guest validation agent files to $InstallRoot"
Write-Host "Prepared guest-local controller root at $SharedRoot"
Write-Host "Launch helper: $launchCmdTarget"
if ($RegisterStartupTask) {
    Write-Host "Registered startup task: $TaskName"
} else {
    Write-Host "Startup task not registered. Use -RegisterStartupTask only after a config.json workflow exists."
}
