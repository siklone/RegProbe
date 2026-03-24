[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputPath = 'H:\Temp\vm-tooling-staging\defender-runtime-repair.json',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputPath = 'C:\Tools\Perf\Procmon\defender-runtime-repair.json',
    [switch]$Repair
)

$ErrorActionPreference = 'Stop'

$hostScript = Join-Path $PSScriptRoot 'repair-defender-runtime.ps1'
$guestScript = Join-Path $GuestScriptRoot 'repair-defender-runtime.ps1'

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
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) | Out-Null
    }
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 180)

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

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScript, $guestScript) | Out-Null

$guestArgs = @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $guestScript,
    '-OutputJson', $GuestOutputPath
)

if ($Repair) {
    $guestArgs += '-Repair'
}

$guestRunFailed = $false
try {
    Invoke-Vmrun -Arguments $guestArgs | Out-Null
}
catch {
    $guestRunFailed = $true
}

try {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $GuestOutputPath, $HostOutputPath) | Out-Null
}
catch {
}

if ($guestRunFailed) {
    throw "Guest runtime-repair script failed. Check $HostOutputPath if it was produced."
}

Write-Output $HostOutputPath
