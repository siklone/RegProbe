[CmdletBinding()]
param(
    [string]$SourceRoot = $PSScriptRoot,
    [string]$InstallRoot = 'C:\Tools\Scripts',
    [string]$SharedRoot = 'C:\Tools\ValidationController',
    [string]$TaskName = 'RegProbeValidationAgent',
    [switch]$RegisterStartupTask,
    [switch]$InstallQemuGuestAgent
)

$ErrorActionPreference = 'Stop'

$guestAgentSource = Join-Path $SourceRoot 'guest-validation-agent.ps1'
$restartHelperSource = Join-Path $SourceRoot 'request-guest-restart.ps1'
$guestAgentTarget = Join-Path $InstallRoot 'guest-validation-agent.ps1'
$restartHelperTarget = Join-Path $InstallRoot 'request-guest-restart.ps1'
$launchCmdTarget = Join-Path $InstallRoot 'run-guest-validation-agent-local.cmd'
$controllerCurrentRoot = Join-Path $SharedRoot 'controller\current'
$artifactsRoot = Join-Path $controllerCurrentRoot 'artifacts'

function Find-QemuGuestAgentInstaller {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $patterns = @(
        'qemu-ga*.msi',
        'qemu-ga*.exe',
        'qemu_guest_agent*.msi',
        'qemu_guest_agent*.exe',
        'guest-agent*.msi'
    )

    foreach ($pattern in $patterns) {
        $found = Get-ChildItem -Path $Root -Filter $pattern -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            return $found.FullName
        }

        $extrasRoot = Join-Path $Root 'extras'
        $found = Get-ChildItem -Path $extrasRoot -Filter $pattern -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            return $found.FullName
        }
    }

    return $null
}

function Enable-QemuGuestAgent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $installer = Find-QemuGuestAgentInstaller -Root $Root
    if (-not $installer) {
        Write-Warning "No qemu guest agent installer found under $Root or $Root\\extras."
        return
    }

    if ($installer -match '\.msi$') {
        $proc = Start-Process -FilePath 'msiexec.exe' -ArgumentList @('/i', $installer, '/qn', '/norestart') -Wait -PassThru
        if ($proc.ExitCode -ne 0) {
            throw "QEMU guest agent MSI install failed with exit code $($proc.ExitCode)"
        }
    } elseif ($installer -match '\.exe$') {
        $proc = Start-Process -FilePath $installer -ArgumentList @('/quiet', '/norestart') -Wait -PassThru
        if ($proc.ExitCode -ne 0) {
            throw "QEMU guest agent installer failed with exit code $($proc.ExitCode)"
        }
    } else {
        throw "Unsupported qemu guest agent installer type: $installer"
    }

    $service = Get-Service -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -match 'qemu.*ga|qga' -or $_.DisplayName -match 'QEMU.*Guest.*Agent'
    } | Select-Object -First 1

    if ($service) {
        try {
            Set-Service -Name $service.Name -StartupType Automatic -ErrorAction Stop
        } catch {
        }
        if ($service.Status -ne 'Running') {
            Start-Service -Name $service.Name -ErrorAction SilentlyContinue
        }
        Write-Host "QEMU guest agent service prepared: $($service.Name)"
    } else {
        Write-Warning 'QEMU guest agent installer completed, but no matching service was discovered.'
    }

    Write-Host "QEMU guest agent installer executed from $installer"
}

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

if ($InstallQemuGuestAgent) {
    Enable-QemuGuestAgent -Root $SourceRoot
}

Write-Host "Installed guest validation agent files to $InstallRoot"
Write-Host "Prepared guest-local controller root at $SharedRoot"
Write-Host "Launch helper: $launchCmdTarget"
if ($RegisterStartupTask) {
    Write-Host "Registered startup task: $TaskName"
} else {
    Write-Host "Startup task not registered. Use -RegisterStartupTask only after a config.json workflow exists."
}
if ($InstallQemuGuestAgent) {
    Write-Host "QEMU guest agent installation was requested."
}
