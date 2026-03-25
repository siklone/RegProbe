[CmdletBinding()]
param(
    [ValidateSet('baseline', 'mpengine', 'policymanager', 'legacyroot')]
    [string]$Mode = 'baseline',

    [ValidateSet(0, 1)]
    [int]$State = 1,

    [string]$SnapshotName = 'baseline-20260325-defender-on',
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon',
    [switch]$RestartWinDefend,
    [switch]$GuestRebootBeforeCapture
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("defender-threat-file-hash-{0}-{1}-{2}" -f $Mode, $State, $stamp)
New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$probeHostScript = Join-Path $PSScriptRoot 'defender-policy-probe.ps1'
$activityHostScript = Join-Path $PSScriptRoot 'defender-threat-file-hash-activity.ps1'
$guestProbeScript = Join-Path $GuestScriptRoot 'defender-policy-probe.ps1'
$guestActivityScript = Join-Path $GuestScriptRoot 'defender-threat-file-hash-activity.ps1'

$prefix = switch ($Mode) {
    'baseline' { 'defender-threat-file-hash-baseline' }
    'mpengine' { 'defender-threat-file-hash-mpengine-1' }
    'policymanager' { 'defender-threat-file-hash-policymanager-1' }
    'legacyroot' { 'defender-threat-file-hash-legacyroot-1' }
}

$registryPath = switch ($Mode) {
    'baseline' { 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine' }
    'mpengine' { 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine' }
    'policymanager' { 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager' }
    'legacyroot' { 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender' }
}

$valueName = switch ($Mode) {
    'baseline' { 'EnableFileHashComputation' }
    'mpengine' { 'EnableFileHashComputation' }
    'policymanager' { 'EnableFileHashComputation' }
    'legacyroot' { 'ThreatFileHashLogging' }
}

$probeMode = if ($Mode -eq 'baseline') {
    'baseline'
}
elseif ($GuestRebootBeforeCapture) {
    'capture'
}
else {
    'set'
}
$guestPml = Join-Path $GuestOutputRoot "$prefix.pml"
$guestCsv = Join-Path $GuestOutputRoot "$prefix.csv"
$guestTxt = Join-Path $GuestOutputRoot "$prefix.txt"
$guestActivityJson = Join-Path $GuestOutputRoot "$prefix-events.json"
$guestActivityTxt = Join-Path $GuestOutputRoot "$prefix-events.txt"
$guestActivityError = Join-Path $GuestOutputRoot "$prefix-events.error.txt"

$hostPml = Join-Path $hostRoot "$prefix.pml"
$hostCsv = Join-Path $hostRoot "$prefix.csv"
$hostTxt = Join-Path $hostRoot "$prefix.txt"
$hostActivityJson = Join-Path $hostRoot "$prefix-events.json"
$hostActivityTxt = Join-Path $hostRoot "$prefix-events.txt"
$hostActivityError = Join-Path $hostRoot "$prefix-events.error.txt"
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

function Invoke-GuestProgram {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$ArgumentList = @()
    )

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath, $FilePath
    ) + $ArgumentList) | Out-Null
}

function Get-GuestLastBootUpTime {
    $guestBootScript = Join-Path $GuestScriptRoot 'write-lastboot.ps1'
    $hostBootScript = Join-Path $HostOutputRoot 'write-lastboot.ps1'
    $guestBootPath = Join-Path $GuestOutputRoot 'threat-file-hash-lastboot.txt'
    $hostBootPath = Join-Path $hostRoot 'threat-file-hash-lastboot.txt'
    $bootScript = @'
param([string]$OutputPath)
$boot = (Get-CimInstance -ClassName Win32_OperatingSystem).LastBootUpTime
Set-Content -Path $OutputPath -Value $boot.ToString('o') -Encoding UTF8
'@

    Set-Content -Path $hostBootScript -Value $bootScript -Encoding UTF8
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostBootScript, $guestBootScript) | Out-Null
    Invoke-GuestProgram -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $guestBootScript,
        '-OutputPath',
        $guestBootPath
    )
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestBootPath, $hostBootPath) | Out-Null
    return [DateTimeOffset]::Parse((Get-Content -Path $hostBootPath -Raw).Trim())
}

function Wait-GuestReboot {
    param([DateTimeOffset]$PreviousBoot, [int]$TimeoutSeconds = 600)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                $currentBoot = Get-GuestLastBootUpTime
                if ($currentBoot -gt $PreviousBoot) {
                    Start-Sleep -Seconds 20
                    return
                }
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw 'Guest reboot did not complete in time.'
}

function Set-GuestDwordValue {
    param([string]$RegistryKeyPath, [string]$Name, [int]$Value)

    Invoke-GuestProgram -FilePath 'C:\Windows\System32\reg.exe' -ArgumentList @(
        'add',
        $RegistryKeyPath.Replace('HKLM:\', 'HKLM\').Replace('HKCU:\', 'HKCU\'),
        '/v',
        $Name,
        '/t',
        'REG_DWORD',
        '/d',
        "$Value",
        '/f'
    )
}

function Remove-GuestValue {
    param([string]$RegistryKeyPath, [string]$Name)

    try {
        Invoke-GuestProgram -FilePath 'C:\Windows\System32\reg.exe' -ArgumentList @(
            'delete',
            $RegistryKeyPath.Replace('HKLM:\', 'HKLM\').Replace('HKCU:\', 'HKCU\'),
            '/v',
            $Name,
            '/f'
        )
    }
    catch {
    }
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
Ensure-VmRunning
Wait-GuestReady

foreach ($scriptPair in @(
    @{ Host = $probeHostScript; Guest = $guestProbeScript },
    @{ Host = $activityHostScript; Guest = $guestActivityScript }
)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $scriptPair.Host, $scriptPair.Guest) | Out-Null
}

if ($GuestRebootBeforeCapture -and $Mode -ne 'baseline') {
    Set-GuestDwordValue -RegistryKeyPath $registryPath -Name $valueName -Value $State
    $previousBoot = Get-GuestLastBootUpTime
    Invoke-GuestProgram -FilePath 'C:\Windows\System32\shutdown.exe' -ArgumentList @('/r', '/t', '0', '/f')
    Wait-GuestReboot -PreviousBoot $previousBoot
}

$activityCommand = "& '$guestActivityScript' -OutputJson '$guestActivityJson' -OutputEvents '$guestActivityTxt' -OutputError '$guestActivityError'"
if ($RestartWinDefend) {
    $activityCommand += ' -RestartWinDefend'
}

$matchFragments = @(
    'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\ThreatFileHashLogging',
    'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager\EnableFileHashComputation',
    'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine\EnableFileHashComputation',
    'HKLM\SOFTWARE\Microsoft\Windows Defender\ThreatFileHashLogging'
)

$processNames = @(
    'MsMpEng.exe',
    'SecurityHealthService.exe',
    'wmiprvse.exe',
    'svchost.exe'
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
            '-ValueName', $valueName,
            '-State', "$State",
            '-Prefix', $prefix,
            '-OutputDirectory', $GuestOutputRoot,
            '-PowerShellCommand', $activityCommand,
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
    @{ Guest = $guestActivityJson; Host = $hostActivityJson },
    @{ Guest = $guestActivityTxt; Host = $hostActivityTxt },
    @{ Guest = $guestActivityError; Host = $hostActivityError }
)) {
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) | Out-Null
    }
    catch {
    }
}

if ($GuestRebootBeforeCapture -and $Mode -ne 'baseline') {
    Remove-GuestValue -RegistryKeyPath $registryPath -Name $valueName
}

if ($guestRunFailed) {
    throw "Guest probe failed. See $hostGuestRunError and copied artifacts under $hostRoot."
}

Write-Output $hostRoot
