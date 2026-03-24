[CmdletBinding()]
param(
    [ValidateSet('baseline', 'reporting', 'securitycenter')]
    [string]$Mode = 'baseline',

    [ValidateSet(0, 1)]
    [int]$State = 1,

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptPath = 'C:\Tools\Scripts\defender-enhanced-notifications-probe.ps1',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon'
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("defender-enhanced-notifications-{0}-{1}-{2}" -f $Mode, $State, $stamp)
New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$probeScript = Join-Path $PSScriptRoot 'defender-enhanced-notifications-probe.ps1'
$prefix = "defender-disable-enhanced-$Mode-$State"
$guestPml = Join-Path $GuestOutputRoot "$prefix.pml"
$guestCsv = Join-Path $GuestOutputRoot "$prefix.csv"
$guestTxt = Join-Path $GuestOutputRoot "$prefix.txt"
$hostPml = Join-Path $hostRoot "$prefix.pml"
$hostCsv = Join-Path $hostRoot "$prefix.csv"
$hostTxt = Join-Path $hostRoot "$prefix.txt"

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Ensure-VmRunning {
    $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
    if ($running -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') | Out-Null
    }
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }
        Start-Sleep -Seconds 3
    }

    throw 'Guest is not ready for vmrun guest operations.'
}

Ensure-VmRunning
Wait-GuestReady

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $probeScript, $GuestScriptPath) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $GuestScriptPath,
    '-Mode',
    $Mode,
    '-State',
    "$State"
) | Out-Null

foreach ($pair in @(
    @{ Guest = $guestTxt; Host = $hostTxt },
    @{ Guest = $guestCsv; Host = $hostCsv },
    @{ Guest = $guestPml; Host = $hostPml }
)) {
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) | Out-Null
    }
    catch {
    }
}

Write-Output $hostRoot
