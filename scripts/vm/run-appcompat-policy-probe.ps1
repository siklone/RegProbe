[CmdletBinding()]
param(
    [ValidateSet('appcompat-bundle', 'appdeviceinventory', 'disable-pca')]
    [string]$Probe = 'appcompat-bundle',

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon',
    [string]$SnapshotName = 'baseline-20260325-shell-stable',
    [string]$IncidentLogPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$probeMap = @{
    'appcompat-bundle' = @{
        output_name = 'appcompat-policy-bundle-procmon'
        record_id = 'privacy.disable-appcompat-engine.policy'
        registry_path = 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat'
        value_name = 'DisableEngine'
        power_shell_command = @"
if (-not (Test-Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat')) { New-Item -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Force | Out-Null }
New-ItemProperty -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Name 'DisableEngine' -PropertyType DWord -Value 1 -Force | Out-Null
New-ItemProperty -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Name 'SbEnable' -PropertyType DWord -Value 0 -Force | Out-Null
reg query "HKLM\Software\Policies\Microsoft\Windows\AppCompat" /v DisableEngine | Out-Null
reg query "HKLM\Software\Policies\Microsoft\Windows\AppCompat" /v SbEnable | Out-Null
"@
        match_fragments = @('DisableEngine', 'SbEnable')
        process_names = @('powershell.exe', 'reg.exe')
    }
    'appdeviceinventory' = @{
        output_name = 'appdeviceinventory-policy-procmon'
        record_id = 'privacy.disable-appdeviceinventory.policy'
        registry_path = 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat'
        value_name = 'DisableAPISamping'
        power_shell_command = @"
if (-not (Test-Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat')) { New-Item -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Force | Out-Null }
New-ItemProperty -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Name 'DisableAPISamping' -PropertyType DWord -Value 1 -Force | Out-Null
New-ItemProperty -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Name 'DisableApplicationFootprint' -PropertyType DWord -Value 1 -Force | Out-Null
New-ItemProperty -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Name 'DisableInstallTracing' -PropertyType DWord -Value 1 -Force | Out-Null
New-ItemProperty -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Name 'DisableWin32AppBackup' -PropertyType DWord -Value 1 -Force | Out-Null
reg query "HKLM\Software\Policies\Microsoft\Windows\AppCompat" /v DisableAPISamping | Out-Null
reg query "HKLM\Software\Policies\Microsoft\Windows\AppCompat" /v DisableApplicationFootprint | Out-Null
reg query "HKLM\Software\Policies\Microsoft\Windows\AppCompat" /v DisableInstallTracing | Out-Null
reg query "HKLM\Software\Policies\Microsoft\Windows\AppCompat" /v DisableWin32AppBackup | Out-Null
"@
        match_fragments = @('DisableAPISamping', 'DisableApplicationFootprint', 'DisableInstallTracing', 'DisableWin32AppBackup')
        process_names = @('powershell.exe', 'reg.exe')
    }
    'disable-pca' = @{
        output_name = 'disable-pca-policy-procmon'
        record_id = 'privacy.disable-program-compatibility-assistant'
        registry_path = 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat'
        value_name = 'DisablePCA'
        power_shell_command = @"
if (-not (Test-Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat')) { New-Item -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Force | Out-Null }
New-ItemProperty -Path 'HKLM:\Software\Policies\Microsoft\Windows\AppCompat' -Name 'DisablePCA' -PropertyType DWord -Value 1 -Force | Out-Null
reg query "HKLM\Software\Policies\Microsoft\Windows\AppCompat" /v DisablePCA | Out-Null
"@
        match_fragments = @('DisablePCA')
        process_names = @('powershell.exe', 'reg.exe')
    }
}

$probeConfig = $probeMap[$Probe]
if (-not $probeConfig) {
    throw "Unknown probe '$Probe'."
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $probeConfig.output_name, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $probeConfig.output_name, $stamp)
$hostPayloadPath = Join-Path $hostRoot 'registry-policy-probe.ps1'
$guestPayloadPath = Join-Path $GuestScriptRoot 'registry-policy-probe.ps1'
$hostDriverPath = Join-Path $hostRoot 'driver.ps1'
$guestDriverPath = Join-Path $GuestScriptRoot 'registry-policy-probe-driver.ps1'
$summaryPath = Join-Path $hostRoot 'summary.json'
$shellHealthScript = Join-Path $PSScriptRoot 'get-vm-shell-health.ps1'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 600)

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

        Start-Sleep -Seconds 5
    }

    throw 'Guest did not reach a ready VMware Tools state in time.'
}

function Get-ShellHealthObject {
    return (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript | ConvertFrom-Json)
}

function Log-Incident {
    param([string]$Symptom)

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId $probeConfig.record_id `
        -TweakId $probeConfig.record_id `
        -TestId ("{0}-{1}" -f $probeConfig.output_name, $stamp) `
        -Family 'AppCompat policy path' `
        -SnapshotName $SnapshotName `
        -RegistryPath ($probeConfig.registry_path.Replace('HKLM:\', 'HKLM\')) `
        -ValueName $probeConfig.value_name `
        -ValueState 'capture' `
        -Symptom $Symptom `
        -ShellRecovered:$false `
        -NeededSnapshotRevert:$false `
        -IncidentPath $IncidentLogPath | Out-Null
}

$payloadSource = Join-Path $PSScriptRoot 'registry-policy-probe.ps1'
Copy-Item -Path $payloadSource -Destination $hostPayloadPath -Force

$matchLiterals = ($probeConfig.match_fragments | ForEach-Object { "'{0}'" -f $_.Replace("'", "''") }) -join ', '
$processLiterals = ($probeConfig.process_names | ForEach-Object { "'{0}'" -f $_.Replace("'", "''") }) -join ', '
$driverContent = @"
`$params = @{
    Mode = 'capture'
    RegistryPath = '$($probeConfig.registry_path)'
    ValueName = '$($probeConfig.value_name)'
    Prefix = '$($probeConfig.output_name)'
    OutputDirectory = '$guestRoot'
    PowerShellCommand = @'
$($probeConfig.power_shell_command.Trim())
'@
    MatchFragments = @($matchLiterals)
    ProcessNames = @($processLiterals)
}

& '$guestPayloadPath' @params
"@
Set-Content -Path $hostDriverPath -Value $driverContent -Encoding UTF8

Wait-GuestReady
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CreateDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostPayloadPath, $guestPayloadPath) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostDriverPath, $guestDriverPath) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $guestDriverPath
) | Out-Null

$hostTxt = Join-Path $hostRoot ("{0}.txt" -f $probeConfig.output_name)
$hostCsv = Join-Path $hostRoot ("{0}.csv" -f $probeConfig.output_name)
$hostHitsCsv = Join-Path $hostRoot ("{0}.hits.csv" -f $probeConfig.output_name)
$hostPml = Join-Path $hostRoot ("{0}.pml" -f $probeConfig.output_name)

foreach ($pair in @(
    @{ Guest = (Join-Path $guestRoot ("{0}.txt" -f $probeConfig.output_name)); Host = $hostTxt },
    @{ Guest = (Join-Path $guestRoot ("{0}.csv" -f $probeConfig.output_name)); Host = $hostCsv },
    @{ Guest = (Join-Path $guestRoot ("{0}.hits.csv" -f $probeConfig.output_name)); Host = $hostHitsCsv },
    @{ Guest = (Join-Path $guestRoot ("{0}.pml" -f $probeConfig.output_name)); Host = $hostPml }
)) {
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) | Out-Null
    }
    catch {
    }
}

$shellHealthPath = Join-Path $hostRoot 'shell-health.json'
$shellHealth = Get-ShellHealthObject
$shellHealth | ConvertTo-Json -Depth 5 | Set-Content -Path $shellHealthPath -Encoding UTF8

if (-not $shellHealth.shell_healthy) {
    Log-Incident -Symptom 'AppCompat policy Procmon probe left the shell unhealthy.'
}

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe = $Probe
    registry_path = $probeConfig.registry_path
    value_name = $probeConfig.value_name
    host_output_root = $hostRoot
    txt = if (Test-Path $hostTxt) { $hostTxt } else { $null }
    csv = if (Test-Path $hostCsv) { $hostCsv } else { $null }
    hits_csv = if (Test-Path $hostHitsCsv) { $hostHitsCsv } else { $null }
    pml = if (Test-Path $hostPml) { $hostPml } else { $null }
    shell_health = $shellHealthPath
} | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8

Write-Output $summaryPath

