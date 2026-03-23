[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$GuestWorkRoot = 'C:\Tools\ValidationController',
    [string]$HostStageRoot = 'H:\Temp\vm-tooling-staging'
)

$ErrorActionPreference = 'Stop'

$hostCmd = Join-Path $HostStageRoot 'codex-controller-debug.cmd'
$hostStdout = Join-Path $HostStageRoot 'agent-debug-stdout.txt'
$hostStderr = Join-Path $HostStageRoot 'agent-debug-stderr.txt'
$hostStatus = Join-Path $HostStageRoot 'agent-debug-status.json'
$hostAgentLog = Join-Path $HostStageRoot 'agent-debug-agent.log'

foreach ($file in @($hostStdout, $hostStderr, $hostStatus, $hostAgentLog)) {
    if (Test-Path $file) {
        Remove-Item -Path $file -Force
    }
}

$guestCmd = 'C:\Tools\Scripts\codex-controller-debug.cmd'
$guestStdout = Join-Path $GuestWorkRoot 'controller\current\agent-debug-stdout.txt'
$guestStderr = Join-Path $GuestWorkRoot 'controller\current\agent-debug-stderr.txt'
$guestStatus = Join-Path $GuestWorkRoot 'controller\current\status.json'
$guestAgentLog = Join-Path $GuestWorkRoot 'controller\current\agent.log'

@(
    '@echo off',
    ('"{0}" -NoProfile -ExecutionPolicy Bypass -File "{1}" -SharedRoot "{2}" 1> "{3}" 2> "{4}"' -f 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe', 'C:\Tools\Scripts\guest-validation-agent.ps1', $GuestWorkRoot, $guestStdout, $guestStderr)
) | Set-Content -Path $hostCmd -Encoding ASCII

& $VmrunPath -T ws -gu $GuestUser -gp $GuestPassword CopyFileFromHostToGuest $VmPath $hostCmd $guestCmd
try {
    & $VmrunPath -T ws -gu $GuestUser -gp $GuestPassword runProgramInGuest $VmPath 'C:\Windows\System32\cmd.exe' '/c C:\Tools\Scripts\codex-controller-debug.cmd'
} catch {
    # Non-zero exit is expected while debugging; continue to collect artifacts.
}

Start-Sleep -Seconds 5

foreach ($pair in @(
        @{ Guest = $guestStdout; Host = $hostStdout },
        @{ Guest = $guestStderr; Host = $hostStderr },
        @{ Guest = $guestStatus; Host = $hostStatus },
        @{ Guest = $guestAgentLog; Host = $hostAgentLog }
    )) {
    try {
        & $VmrunPath -T ws -gu $GuestUser -gp $GuestPassword CopyFileFromGuestToHost $VmPath $pair.Guest $pair.Host | Out-Null
    } catch {
        # Best effort.
    }
}

Write-Host '--- stdout ---'
if (Test-Path $hostStdout) {
    Get-Content -Path $hostStdout -Raw
} else {
    Write-Host 'missing'
}

Write-Host '--- stderr ---'
if (Test-Path $hostStderr) {
    Get-Content -Path $hostStderr -Raw
} else {
    Write-Host 'missing'
}

Write-Host '--- status ---'
if (Test-Path $hostStatus) {
    Get-Content -Path $hostStatus -Raw
} else {
    Write-Host 'missing'
}

Write-Host '--- agent.log ---'
if (Test-Path $hostAgentLog) {
    Get-Content -Path $hostAgentLog -Raw
} else {
    Write-Host 'missing'
}
