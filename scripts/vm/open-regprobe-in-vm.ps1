[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$PublishZipPath = 'H:\Temp\vm-tooling-staging\regprobe-app-publish.zip',
    [string]$GuestPublishZipPath = 'C:\Tools\Inbound\app-publish.zip',
    [string]$GuestScriptPath = 'C:\Tools\Scripts\app-deploy.ps1',
    [string]$GuestResultPath = 'C:\Tools\ValidationController\smoke\app-deploy.json',
    [string]$HostDeployResultPath = 'H:\Temp\vm-tooling-staging\open-regprobe-in-vm.deploy.json',
    [string]$OutputPath = 'H:\Temp\vm-tooling-staging\open-regprobe-in-vm.json',
    [bool]$RefreshPackage = $true
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

if ($RefreshPackage) {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
    $packageScriptPath = Join-Path $repoRoot 'scripts\package_windows.ps1'
    if (-not (Test-Path $packageScriptPath)) {
        throw "Package script not found at $packageScriptPath"
    }

    & $packageScriptPath -Configuration 'Release' -Runtime 'win-x64' -SelfContained:$false

    $freshPackage = Get-ChildItem -Path (Join-Path $repoRoot 'dist') -Filter 'RegProbe-*-win-x64-Release-*.zip' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $freshPackage) {
        throw "Failed to produce a fresh RegProbe package in $repoRoot\\dist"
    }

    $publishZipDir = Split-Path -Parent $PublishZipPath
    if ($publishZipDir) {
        New-Item -ItemType Directory -Path $publishZipDir -Force | Out-Null
    }

    Copy-Item -LiteralPath $freshPackage.FullName -Destination $PublishZipPath -Force
}

if (-not (Test-Path $PublishZipPath)) {
    throw "Publish zip not found at $PublishZipPath"
}

$hostScriptPath = Join-Path $PSScriptRoot 'app-deploy.ps1'
if (-not (Test-Path $hostScriptPath)) {
    throw "Guest deploy script not found at $hostScriptPath"
}

$hostPrepScriptPath = Join-Path $env:TEMP 'regprobe-open-prep.ps1'
$guestPrepScriptPath = 'C:\Tools\Scripts\regprobe-open-prep.ps1'

@'
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path 'C:\Tools\Inbound' -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\Scripts' -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\ValidationController\smoke' -Force | Out-Null
'@ | Set-Content -Path $hostPrepScriptPath -Encoding ASCII

$running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
if ($running -notmatch [regex]::Escape($VmPath)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') | Out-Null
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
    $HostDeployResultPath
) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'runProgramInGuest',
    $VmPath,
    '-interactive',
    '-activeWindow',
    '-noWait',
    'C:\Tools\AppSmoke\RegProbe.App.exe'
) | Out-Null

Start-Sleep -Seconds 8
$guestProcesses = Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'listProcessesInGuest',
    $VmPath
)

$regProbeProcessLines = @(
    $guestProcesses -split "`r?`n" |
    Where-Object { $_ -match 'RegProbe\.App\.exe' }
)

$deployResult = $null
if (Test-Path $HostDeployResultPath) {
    $deployResult = Get-Content -Path $HostDeployResultPath -Raw | ConvertFrom-Json
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    interactive_launch = $true
    launched_executable = 'C:\Tools\AppSmoke\RegProbe.App.exe'
    host_deploy_result_path = $HostDeployResultPath
    guest_process_detected = [bool]($regProbeProcessLines.Count -gt 0)
    guest_process_lines = $regProbeProcessLines
    deploy_result = $deployResult
}

$outputDir = Split-Path -Parent $OutputPath
if ($outputDir) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$result | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
Get-Content -Path $OutputPath
