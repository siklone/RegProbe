[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\explorer-compact-runtime',
    [string]$RecordId = 'explorer.enable-explorer-compact-mode',
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
$probeName = "explorer-compact-runtime-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'explorer-compact-runtime-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'explorer-compact-runtime-payload.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$EtlPath,

    [int]$SettleSeconds = 10
)

$ErrorActionPreference = 'Stop'
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'
$registryPathNative = 'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'
$valueName = 'UseCompactMode'
$valueData = 1
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'

function Get-ValueState {
    $pathExists = Test-Path $registryPath
    try {
        $item = Get-ItemProperty -Path $registryPath -Name $valueName -ErrorAction Stop
        return [ordered]@{
            path_exists = $pathExists
            value_exists = $true
            value = $item.$valueName
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
        & reg.exe add $registryPathNative /v $valueName /t REG_DWORD /d ([int]$Original.value) /f | Out-Null
        return
    }

    if (Test-Path $registryPath) {
        & reg.exe delete $registryPathNative /v $valueName /f | Out-Null
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
    return Get-ShellState
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $EtlPath) -Force | Out-Null

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    registry_path = $registryPath
    value_name = $valueName
    value_data = $valueData
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
    etl_path = 'explorer-compact-runtime.etl'
    errors = @()
}

$before = Get-ValueState
$summary.before = $before
$traceStarted = $false

try {
    & reg.exe add $registryPathNative /v $valueName /t REG_DWORD /d $valueData /f | Out-Null
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
        -Family 'explorer-compact-runtime' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' `
        -ValueName 'UseCompactMode' `
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
    value_name = 'UseCompactMode'
    value_data = 1
    shell_after = $null
    wpr = [ordered]@{
        started = $false
        stopped = $false
        profile = 'GeneralProfile'
        guest_etl = 'explorer-compact-runtime.etl'
        repo_etl_placeholder = "evidence/files/vm-tooling-staging/$probeName/explorer-compact-runtime.etl.md"
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
        throw 'Shell health check failed before the explorer compact runtime probe started.'
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    $guestSummaryPath = Join-Path $guestRoot 'summary.json'
    $guestEtlPath = Join-Path $guestRoot 'explorer-compact-runtime.etl'
    $guestInvocationError = $null
    $guestSummary = $null
    try {
        Invoke-GuestPowerShell -ArgumentList @(
            '-NoProfile',
            '-ExecutionPolicy', 'Bypass',
            '-File', $guestPayloadPath,
            '-OutputPath', $guestSummaryPath,
            '-EtlPath', $guestEtlPath,
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
    }
    catch {
    }

    if ($guestInvocationError) {
        throw $guestInvocationError
    }

    $summary.shell_after = Get-ShellHealth

    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.shellhost)) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += 'Shell health was degraded after the explorer compact runtime probe.'
        Log-Incident -TestId $probeName -ValueState 'UseCompactMode=1' -Symptom 'Shell health was degraded after the explorer compact runtime probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Recovered by snapshot revert after the Explorer compact runtime lane.'
    }
    elseif ($guestSummary.errors.Count -gt 0) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += @($guestSummary.errors)
        Log-Incident -TestId $probeName -ValueState 'UseCompactMode=1' -Symptom 'Guest payload reported errors during the explorer compact runtime probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes (($guestSummary.errors -join '; '))
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -ValueState 'UseCompactMode=1' -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Runtime lane failed before the explorer compact summary completed. Recovered by snapshot revert.'
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
    'Title: Explorer compact mode runtime WPR trace',
    '',
    'The raw ETL is kept off-git. Use the summary JSON in the same folder and the runtime lane manifest in evidence/records for the machine-readable result.'
) -join "`n"
Set-Content -Path (Join-Path $repoRootOut 'explorer-compact-runtime.etl.md') -Value $etlPlaceholder -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}

