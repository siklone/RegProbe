[CmdletBinding()]
param(
    [ValidateSet('baseline', 'root', 'policymanager')]
    [string]$Mode = 'baseline',

    [ValidateSet(0, 1)]
    [int]$State = 1,

    [string]$SnapshotName = 'baseline-20260324-high-risk-lane',
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon'
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("hideexclusions-admins-{0}-{1}-{2}" -f $Mode, $State, $stamp)
New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$probeHostScript = Join-Path $PSScriptRoot 'defender-policy-probe.ps1'
$visibilityHostScript = Join-Path $PSScriptRoot 'defender-hide-exclusions-visibility.ps1'
$guestProbeScript = Join-Path $GuestScriptRoot 'defender-policy-probe.ps1'
$guestVisibilityScript = Join-Path $GuestScriptRoot 'defender-hide-exclusions-visibility.ps1'

$prefix = switch ($Mode) {
    'baseline' { 'hideexclusions-admins-baseline' }
    'root' { 'hideexclusions-admins-root-1' }
    'policymanager' { 'hideexclusions-admins-policymanager-1' }
}

$registryPath = switch ($Mode) {
    'baseline' { 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender' }
    'root' { 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender' }
    'policymanager' { 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager' }
}

$probeMode = if ($Mode -eq 'baseline') { 'baseline' } else { 'set' }
$guestPml = Join-Path $GuestOutputRoot "$prefix.pml"
$guestCsv = Join-Path $GuestOutputRoot "$prefix.csv"
$guestTxt = Join-Path $GuestOutputRoot "$prefix.txt"
$guestVisibilityJson = Join-Path $GuestOutputRoot "$prefix-visibility.json"
$guestVisibilityReg = Join-Path $GuestOutputRoot "$prefix-registry.txt"
$guestVisibilityError = Join-Path $GuestOutputRoot "$prefix-visibility.error.txt"

$hostPml = Join-Path $hostRoot "$prefix.pml"
$hostCsv = Join-Path $hostRoot "$prefix.csv"
$hostTxt = Join-Path $hostRoot "$prefix.txt"
$hostVisibilityJson = Join-Path $hostRoot "$prefix-visibility.json"
$hostVisibilityReg = Join-Path $hostRoot "$prefix-registry.txt"
$hostVisibilityError = Join-Path $hostRoot "$prefix-visibility.error.txt"
$hostGuestRunError = Join-Path $hostRoot "$prefix-guest-run-error.txt"

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

Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
Ensure-VmRunning
Wait-GuestReady

foreach ($scriptPair in @(
    @{ Host = $probeHostScript; Guest = $guestProbeScript },
    @{ Host = $visibilityHostScript; Guest = $guestVisibilityScript }
)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $scriptPair.Host, $scriptPair.Guest) | Out-Null
}

$visibilityCommand = "& '$guestVisibilityScript' -OutputJson '$guestVisibilityJson' -OutputReg '$guestVisibilityReg'"

$matchFragments = @(
    'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\HideExclusionsFromLocalAdmins',
    'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager\HideExclusionsFromLocalAdmins',
    'HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths'
)

$processNames = @(
    'SecurityHealthService.exe',
    'powershell.exe',
    'SecHealthUI.exe',
    'reg.exe'
)

$guestRunFailed = $false
try {
    Invoke-Vmrun -Arguments (
        @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'runProgramInGuest', $VmPath,
            'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
            '-NoProfile',
            '-ExecutionPolicy', 'Bypass',
            '-File', $guestProbeScript,
            '-Mode', $probeMode,
            '-RegistryPath', $registryPath,
            '-ValueName', 'HideExclusionsFromLocalAdmins',
            '-State', "$State",
            '-Prefix', $prefix,
            '-OutputDirectory', $GuestOutputRoot,
            '-PowerShellCommand', $visibilityCommand,
            '-MatchFragments'
        ) + $matchFragments + @(
            '-ProcessNames'
        ) + $processNames
    ) | Out-Null
}
catch {
    $_.Exception.Message | Set-Content -Path $hostGuestRunError -Encoding UTF8
    $guestRunFailed = $true
}

foreach ($pair in @(
    @{ Guest = $guestTxt; Host = $hostTxt },
    @{ Guest = $guestCsv; Host = $hostCsv },
    @{ Guest = $guestPml; Host = $hostPml },
    @{ Guest = $guestVisibilityJson; Host = $hostVisibilityJson },
    @{ Guest = $guestVisibilityReg; Host = $hostVisibilityReg },
    @{ Guest = $guestVisibilityError; Host = $hostVisibilityError }
)) {
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) | Out-Null
    }
    catch {
    }
}

if ($guestRunFailed) {
    throw "Guest probe failed. See $hostGuestRunError and any copied visibility error file under $hostRoot."
}

Write-Output $hostRoot
