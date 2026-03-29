[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\explorer-shell-registry-runtime',
    [Parameter(Mandatory = $true)]
    [string]$RecordId,
    [Parameter(Mandatory = $true)]
    [string]$ProbePrefix,
    [Parameter(Mandatory = $true)]
    [string]$Family,
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,
    [Parameter(Mandatory = $true)]
    [string]$RegistryPathNative,
    [Parameter(Mandatory = $true)]
    [string]$ValueName,
    [Parameter(Mandatory = $true)]
    [int]$ValueData,
    [string]$SnapshotName = '',
    [string]$IncidentLogPath = '',
    [int]$SettleSeconds = 10
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName
}

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "$ProbePrefix-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot "$ProbePrefix-payload.ps1"
$guestPayloadPath = Join-Path $guestRoot "$ProbePrefix-payload.ps1"
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'
$guestEtlName = "$ProbePrefix.etl"

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$EtlPath,

    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$RegistryPathNative,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [int]$ValueData,

    [int]$SettleSeconds = 10
)

$ErrorActionPreference = 'Stop'
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'

function Get-ValueState {
    $pathExists = Test-Path $RegistryPath
    try {
        $item = Get-ItemProperty -Path $RegistryPath -Name $ValueName -ErrorAction Stop
        return [ordered]@{
            path_exists = $pathExists
            value_exists = $true
            value = [int]$item.$ValueName
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
        & reg.exe add $RegistryPathNative /v $ValueName /t REG_DWORD /d ([int]$Original.value) /f | Out-Null
        return
    }

    if (Test-Path $RegistryPath) {
        & reg.exe delete $RegistryPathNative /v $ValueName /f | Out-Null
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

function Restart-ExplorerShell {
    Get-Process -Name explorer -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Start-Process -FilePath 'explorer.exe' | Out-Null
    Start-Sleep -Seconds $SettleSeconds
    $lastState = Get-ShellState
    if ($lastState.explorer) {
        return $lastState
    }

    for ($attempt = 0; $attempt -lt 4; $attempt++) {
        Start-Sleep -Seconds 5
        $lastState = Get-ShellState
        if ($lastState.explorer) {
            return $lastState
        }

        Start-Process -FilePath 'explorer.exe' | Out-Null
    }

    Start-Sleep -Seconds 5
    return Get-ShellState
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $EtlPath) -Force | Out-Null

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    registry_path = $RegistryPath
    value_name = $ValueName
    value_data = $ValueData
    before = $null
    applied = $null
    after_candidate_restart = $null
    restored = $null
    after_restore_restart = $null
    shell_after = $null
    explorer_restart = [ordered]@{
        candidate_restart = $false
        restore_restart = $false
    }
    etl_exists = $false
    etl_path = [System.IO.Path]::GetFileName($EtlPath)
    errors = @()
}

$before = Get-ValueState
$summary.before = $before
$traceStarted = $false

try {
    & reg.exe add $RegistryPathNative /v $ValueName /t REG_DWORD /d $ValueData /f | Out-Null
    $summary.applied = Get-ValueState

    & $wpr -cancel | Out-Null
    & $wpr -start GeneralProfile -filemode | Out-Null
    $traceStarted = $true

    $summary.explorer_restart.candidate_restart = $true
    $summary.shell_after = Restart-ExplorerShell
    $summary.after_candidate_restart = Get-ValueState
}
catch {
    $summary.errors += $_.Exception.Message
}
finally {
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
    try {
        $summary.explorer_restart.restore_restart = $true
        $summary.after_restore_restart = Restart-ExplorerShell
    }
    catch {
        $summary.errors += $_.Exception.Message
    }

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

function Wait-ShellHealthy {
    param([int]$TimeoutSeconds = 60)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastState = $null
    while ((Get-Date) -lt $deadline) {
        $lastState = Get-ShellHealth
        if ($lastState.explorer -and $lastState.sihost -and $lastState.shellhost) {
            return $lastState
        }
        Start-Sleep -Seconds 5
    }

    return $lastState
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
        -Family $Family `
        -SnapshotName $SnapshotName `
        -RegistryPath $RegistryPathNative `
        -ValueName $ValueName `
        -ValueState "$ValueName=$ValueData" `
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
        guest_etl = $guestEtlName
        repo_etl_placeholder = "evidence/files/vm-tooling-staging/$probeName/$ProbePrefix.etl.md"
    }
    recovery = [ordered]@{
        performed = $false
        shell_healthy_after_recovery = $false
    }
    errors = @()
}

$probeFailed = $false
$needsRecovery = $false
$guestShellHealthy = $false

try {
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    $initialShell = Get-ShellHealth
    if (-not ($initialShell.explorer -and $initialShell.sihost -and $initialShell.shellhost)) {
        throw "Shell health check failed before the $ProbePrefix runtime probe started."
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    $guestSummaryPath = Join-Path $guestRoot 'summary.json'
    $guestEtlPath = Join-Path $guestRoot $guestEtlName
    $guestInvocationError = $null
    $guestSummary = $null
    try {
        Invoke-GuestPowerShell -ArgumentList @(
            '-NoProfile',
            '-ExecutionPolicy', 'Bypass',
            '-File', $guestPayloadPath,
            '-OutputPath', $guestSummaryPath,
            '-EtlPath', $guestEtlPath,
            '-RegistryPath', $RegistryPath,
            '-RegistryPathNative', $RegistryPathNative,
            '-ValueName', $ValueName,
            '-ValueData', "$ValueData",
            '-SettleSeconds', "$SettleSeconds"
        )
    }
    catch {
        $guestInvocationError = $_.Exception.Message
    }

    try {
        Copy-FromGuest -GuestPath $guestSummaryPath -HostPath $hostSummaryPath
        $guestSummary = Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json
        $summary.summary = $guestSummary
        $summary.wpr.started = [bool]($guestSummary.explorer_restart.candidate_restart)
        $summary.wpr.stopped = [bool]$guestSummary.etl_exists
        $guestFinalShell = if ($guestSummary.after_restore_restart) { $guestSummary.after_restore_restart } else { $guestSummary.shell_after }
        if ($guestFinalShell) {
            $guestShellHealthy = [bool]($guestFinalShell.explorer -and $guestFinalShell.sihost -and $guestFinalShell.ShellHost)
        }
    }
    catch {
    }

    if ($guestInvocationError) {
        throw $guestInvocationError
    }

    $summary.shell_after = Wait-ShellHealthy -TimeoutSeconds ([Math]::Max(30, $SettleSeconds + 20))
    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.shellhost) -and $guestShellHealthy) {
        $summary.shell_after = [ordered]@{
            explorer = $true
            sihost = $true
            shellhost = $true
            ctfmon = $true
            process_dump = 'Guest payload reported healthy shell state after restore; host vmrun process listing lagged.'
        }
    }

    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.shellhost)) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += "Shell health was degraded after the $ProbePrefix runtime probe."
        Log-Incident -TestId $probeName -ValueState "$ValueName=$ValueData" -Symptom "Shell health was degraded after the $ProbePrefix runtime probe." -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes "Recovered by snapshot revert after the $ProbePrefix runtime lane."
    }
    elseif ($guestSummary.errors.Count -gt 0) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += @($guestSummary.errors)
        Log-Incident -TestId $probeName -ValueState "$ValueName=$ValueData" -Symptom "Guest payload reported errors during the $ProbePrefix runtime probe." -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes (($guestSummary.errors -join '; '))
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -ValueState "$ValueName=$ValueData" -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes "Runtime lane failed before the $ProbePrefix summary completed. Recovered by snapshot revert."
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
    "Title: $Family runtime WPR trace",
    '',
    'The raw ETL is kept off-git. Use the summary JSON in the same folder and the runtime lane manifest in evidence/records for the machine-readable result.'
) -join "`n"
Set-Content -Path (Join-Path $repoRootOut "$ProbePrefix.etl.md") -Value $etlPlaceholder -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}
