[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$PublishZipPath = '',
    [string]$GuestPublishZipPath = 'C:\Tools\Inbound\app-publish.zip',
    [string]$GuestScriptPath = 'C:\Tools\Scripts\app-launch-smoke.ps1',
    [string]$GuestResultPath = 'C:\Tools\ValidationController\smoke\app-launch-smoke.json',
    [string]$OutputPath = '',
    [bool]$RefreshPackage = $true,
    [string]$PreCleanupOutputPath = '',
    [string]$PostCleanupOutputPath = '',
    [bool]$EnforceEphemeralCleanup = $true
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

$vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile
}

$hostStagingRoot = Resolve-HostStagingRoot -VmProfile $VmProfile
if ([string]::IsNullOrWhiteSpace($PublishZipPath)) {
    $PublishZipPath = Join-Path $hostStagingRoot 'regprobe-app-publish.zip'
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $hostStagingRoot ("app-launch-smoke-{0}.json" -f $vmProfileTag)
}

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

$hostScriptPath = Join-Path $PSScriptRoot 'app-launch-smoke.ps1'
if (-not (Test-Path $hostScriptPath)) {
    throw "Guest smoke script not found at $hostScriptPath"
}
$artifactAuditScriptPath = Join-Path $PSScriptRoot 'run-guest-app-artifact-audit.ps1'
if (-not (Test-Path $artifactAuditScriptPath)) {
    throw "Guest app artifact audit script not found at $artifactAuditScriptPath"
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

if ([string]::IsNullOrWhiteSpace($PreCleanupOutputPath)) {
    $PreCleanupOutputPath = Join-Path $outputDirectory ("app-launch-smoke-pre-cleanup-{0}.json" -f $vmProfileTag)
}

if ([string]::IsNullOrWhiteSpace($PostCleanupOutputPath)) {
    $PostCleanupOutputPath = Join-Path $outputDirectory ("app-launch-smoke-post-cleanup-{0}.json" -f $vmProfileTag)
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

if ($EnforceEphemeralCleanup) {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $artifactAuditScriptPath `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword `
        -Mode cleanup `
        -RequireClean `
        -OutputPath $PreCleanupOutputPath | Out-Null
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

$capturedError = $null

try {
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

    $guestResult = Get-Content -Path $OutputPath -Raw | ConvertFrom-Json
    if ($guestResult.PSObject.Properties.Name -contains 'error' -and -not [string]::IsNullOrWhiteSpace($guestResult.error)) {
        throw "Guest app smoke failed: $($guestResult.error)"
    }

    if (-not $guestResult.process_started -or -not $guestResult.process_alive_after_12s) {
        throw 'Guest app smoke did not keep the app process alive through the validation window.'
    }
}
catch {
    $capturedError = $_.Exception.Message
}
finally {
    if ($EnforceEphemeralCleanup) {
        try {
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $artifactAuditScriptPath `
                -VmPath $VmPath `
                -VmrunPath $VmrunPath `
                -GuestUser $GuestUser `
                -GuestPassword $GuestPassword `
                -Mode cleanup `
                -RequireClean `
                -OutputPath $PostCleanupOutputPath | Out-Null
        }
        catch {
            if ([string]::IsNullOrWhiteSpace($capturedError)) {
                $capturedError = $_.Exception.Message
            }
            else {
                $capturedError = "$capturedError; $($_.Exception.Message)"
            }
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($capturedError)) {
    throw $capturedError
}

Get-Content -Path $OutputPath
