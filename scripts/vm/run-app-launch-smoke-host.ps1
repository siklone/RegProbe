[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$PublishZipPath = 'H:\Temp\vm-tooling-staging\regprobe-app-publish.zip',
    [string]$GuestPublishZipPath = 'C:\Tools\Inbound\app-publish.zip',
    [string]$GuestScriptPath = 'C:\Tools\Scripts\app-launch-smoke.ps1',
    [string]$GuestResultPath = 'C:\Tools\ValidationController\smoke\app-launch-smoke.json',
    [string]$OutputPath = 'H:\Temp\vm-tooling-staging\app-launch-smoke.json'
)

$ErrorActionPreference = 'Stop'

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

if (-not (Test-Path $PublishZipPath)) {
    throw "Publish zip not found at $PublishZipPath"
}

$hostScriptPath = Join-Path $PSScriptRoot 'app-launch-smoke.ps1'
if (-not (Test-Path $hostScriptPath)) {
    throw "Guest smoke script not found at $hostScriptPath"
}
$hostPrepScriptPath = Join-Path $env:TEMP 'regprobe-app-launch-smoke-prep.ps1'
$guestPrepScriptPath = 'C:\Tools\Scripts\app-launch-smoke-prep.ps1'

@'
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path 'C:\Tools\Inbound' -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\Scripts' -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\ValidationController\smoke' -Force | Out-Null
'@ | Set-Content -Path $hostPrepScriptPath -Encoding ASCII

$running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
if ($running -notmatch [regex]::Escape($VmPath)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) | Out-Null
}

$toolsDeadline = (Get-Date).AddMinutes(5)
do {
    Start-Sleep -Seconds 3
    $toolsState = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
} until ($toolsState -match 'running|installed' -or (Get-Date) -ge $toolsDeadline)

if ($toolsState -notmatch 'running|installed') {
    throw "VMware Tools not ready for $VmPath"
}

Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'CopyFileFromHostToGuest',
    $VmPath,
    $hostPrepScriptPath,
    $guestPrepScriptPath
) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'runProgramInGuest',
    $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $guestPrepScriptPath
) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'CopyFileFromHostToGuest',
    $VmPath,
    $PublishZipPath,
    $GuestPublishZipPath
) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'CopyFileFromHostToGuest',
    $VmPath,
    $hostScriptPath,
    $GuestScriptPath
) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'runProgramInGuest',
    $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $GuestScriptPath
) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'CopyFileFromGuestToHost',
    $VmPath,
    $GuestResultPath,
    $OutputPath
) | Out-Null

Get-Content -Path $OutputPath
