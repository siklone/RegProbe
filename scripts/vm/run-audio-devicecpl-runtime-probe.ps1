[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\audio-devicecpl-runtime',
    [string]$RecordId = 'audio.show-hidden-devices',
    [string]$ValueName = 'ShowHiddenDevices',
    [int]$ValueData = 1,
    [string]$SnapshotName = 'baseline-20260327-shell-stable',
    [string]$IncidentLogPath = '',
    [int]$SettleSeconds = 8
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "audio-devicecpl-runtime-$($ValueName.ToLower())-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'audio-devicecpl-runtime-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'audio-devicecpl-runtime-payload.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [int]$ValueData,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$EtlPath,

    [int]$SettleSeconds = 8
)

$ErrorActionPreference = 'Stop'
$registryPath = 'HKCU:\Software\Microsoft\Multimedia\Audio\DeviceCpl'
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$launchArgs = 'shell32.dll,Control_RunDLL mmsys.cpl,,0'

function Get-ValueState {
    $pathExists = Test-Path $registryPath
    try {
        $item = Get-ItemProperty -Path $registryPath -Name $ValueName -ErrorAction Stop
        return [ordered]@{
            path_exists = $pathExists
            value_exists = $true
            value = $item.$ValueName
        }
    }
    catch {
        return [ordered]@{
            path_exists = $pathExists
            value_exists = $false
            value = $null
        }
    }
}

function Restore-ValueState {
    param([hashtable]$Original)

    if ($Original.value_exists) {
        New-Item -Path $registryPath -Force | Out-Null
        New-ItemProperty -Path $registryPath -Name $ValueName -PropertyType DWord -Value ([int]$Original.value) -Force | Out-Null
        return
    }

    if (Test-Path $registryPath) {
        Remove-ItemProperty -Path $registryPath -Name $ValueName -ErrorAction SilentlyContinue
    }
}

function Get-ShellState {
    $names = @('explorer', 'sihost', 'ShellHost', 'ctfmon')
    $result = [ordered]@{}
    foreach ($name in $names) {
        $result[$name] = [bool](Get-Process -Name $name -ErrorAction SilentlyContinue)
    }
    return $result
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $EtlPath) -Force | Out-Null

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    registry_path = $registryPath
    value_name = $ValueName
    value_data = $ValueData
    before = $null
    applied = $null
    restored = $null
    shell_after = $null
    control_panel = [ordered]@{
        launched = $false
        exited = $false
    }
    etl_exists = $false
    etl_path = 'audio-devicecpl.etl'
    errors = @()
}

$before = Get-ValueState
$summary.before = $before
$traceStarted = $false
$controlPanel = $null

try {
    New-Item -Path $registryPath -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name $ValueName -PropertyType DWord -Value $ValueData -Force | Out-Null
    $summary.applied = Get-ValueState

    & $wpr -cancel | Out-Null
    & $wpr -start GeneralProfile -filemode | Out-Null
    $traceStarted = $true

    $controlPanel = Start-Process -FilePath 'rundll32.exe' -ArgumentList $launchArgs -PassThru
    $summary.control_panel.launched = $true
    Start-Sleep -Seconds $SettleSeconds
    $summary.shell_after = Get-ShellState
}
catch {
    $summary.errors += $_.Exception.Message
}
finally {
    if ($controlPanel) {
        try {
            if (-not $controlPanel.HasExited) {
                Stop-Process -Id $controlPanel.Id -Force -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 1
            }
            $summary.control_panel.exited = $controlPanel.HasExited
        }
        catch {
        }
    }

    if ($traceStarted) {
        try {
            & $wpr -stop $EtlPath | Out-Null
            $summary.etl_exists = [bool](Test-Path $EtlPath)
        }
        catch {
            $summary.errors += $_.Exception.Message
        }
    }

    Restore-ValueState -Original $before
    $summary.restored = Get-ValueState
    $summary.generated_utc = [DateTime]::UtcNow.ToString('o')
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
}

if ($summary.errors.Count -gt 0) {
    exit 1
}
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
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

    throw 'Guest did not return to a running VMware Tools state in time.'
}

function Get-ShellHealth {
    $processes = Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'listProcessesInGuest',
        $VmPath
    )

    return [ordered]@{
        explorer = [bool]($processes -match '\bexplorer\.exe\b')
        sihost = [bool]($processes -match '\bsihost\.exe\b')
        shellhost = [bool]($processes -match '\bShellHost\.exe\b')
        ctfmon = [bool]($processes -match '\bctfmon\.exe\b')
        process_dump = $processes
    }
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromHostToGuest',
        $VmPath,
        $HostPath,
        $GuestPath
    ) | Out-Null
}

function Copy-FromGuest {
    param([string]$GuestPath, [string]$HostPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromGuestToHost',
        $VmPath,
        $GuestPath,
        $HostPath
    ) | Out-Null
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'runProgramInGuest',
        $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

function Restore-HealthySnapshot {
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        return
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
}

function Log-Incident {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestId,
        [Parameter(Mandatory = $true)]
        [string]$ValueState,
        [Parameter(Mandatory = $true)]
        [string]$Symptom,
        [bool]$ShellRecovered = $false,
        [bool]$NeededSnapshotRevert = $false,
        [string]$Notes = ''
    )

    & (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId $RecordId `
        -TweakId $RecordId `
        -TestId $TestId `
        -Family 'audio-devicecpl-runtime' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKCU\Software\Microsoft\Multimedia\Audio\DeviceCpl' `
        -ValueName $ValueName `
        -ValueState $ValueState `
        -Symptom $Symptom `
        -ShellRecovered:$ShellRecovered `
        -NeededSnapshotRevert:$NeededSnapshotRevert `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    record_id = $RecordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = 'started'
    value_name = $ValueName
    value_data = $ValueData
    shell_after = $null
    wpr = [ordered]@{
        started = $false
        stopped = $false
        profile = 'GeneralProfile'
        guest_etl = 'audio-devicecpl.etl'
        repo_etl_placeholder = "evidence/files/vm-tooling-staging/$probeName/audio-devicecpl.etl.md"
    }
    recovery = [ordered]@{
        performed = $false
        shell_healthy_after_recovery = $false
    }
    errors = @()
}

$probeFailed = $false
$needsRecovery = $false

try {
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    $initialShell = Get-ShellHealth
    if (-not ($initialShell.explorer -and $initialShell.sihost -and $initialShell.shellhost)) {
        throw 'Shell health check failed before the audio runtime probe started.'
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    $guestSummaryPath = Join-Path $guestRoot 'summary.json'
    $guestEtlPath = Join-Path $guestRoot 'audio-devicecpl.etl'
    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-ValueName', $ValueName,
        '-ValueData', "$ValueData",
        '-OutputPath', $guestSummaryPath,
        '-EtlPath', $guestEtlPath,
        '-SettleSeconds', "$SettleSeconds"
    )

    Copy-FromGuest -GuestPath $guestSummaryPath -HostPath $hostSummaryPath
    $guestSummary = Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json
    $summary.wpr.started = $true
    $summary.wpr.stopped = [bool]$guestSummary.etl_exists
    $summary.summary = $guestSummary
    $summary.shell_after = Get-ShellHealth

    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.shellhost)) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += 'Shell health was degraded after the audio runtime probe.'
        Log-Incident -TestId $probeName -ValueState "$ValueName=$ValueData" -Symptom 'Shell health was degraded after the audio runtime probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Recovered by snapshot revert after the DeviceCpl runtime lane.'
    }
    elseif ($guestSummary.errors.Count -gt 0) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += @($guestSummary.errors)
        Log-Incident -TestId $probeName -ValueState "$ValueName=$ValueData" -Symptom 'Guest payload reported errors during the audio runtime probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes (($guestSummary.errors -join '; '))
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -ValueState "$ValueName=$ValueData" -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Runtime lane failed before the audio summary completed. Recovered by snapshot revert.'
}
finally {
    if ($needsRecovery -and -not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        try {
            Restore-HealthySnapshot
            $recoveredShell = Get-ShellHealth
            $summary.recovery.performed = $true
            $summary.recovery.shell_healthy_after_recovery = [bool]($recoveredShell.explorer -and $recoveredShell.sihost -and $recoveredShell.shellhost)
        }
        catch {
            $summary.errors += "Recovery failed: $($_.Exception.Message)"
        }
    }
}

$summary.status = if ($probeFailed) { 'failed' } else { 'ok' }
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $repoSummaryPath -Encoding UTF8

$etlPlaceholder = @(
    '# External Evidence Placeholder',
    '',
    'Title: Audio DeviceCpl runtime WPR trace',
    '',
    'The raw ETL is kept off-git. Use the summary JSON in the same folder and the runtime lane manifest in evidence/records for the machine-readable result.'
) -join "`n"
Set-Content -Path (Join-Path $repoRootOut 'audio-devicecpl.etl.md') -Value $etlPlaceholder -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}
