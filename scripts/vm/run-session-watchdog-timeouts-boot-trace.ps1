[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\watchdog-timeouts-boottrace',
    [string]$SnapshotName = '',
    [string]$IncidentLogPath = '',
    [int]$PostBootSettleSeconds = 30
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
$probeName = "watchdog-timeouts-boottrace-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'watchdog-timeouts-boottrace-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'watchdog-timeouts-boottrace-payload.ps1'
$hostLastBootQueryPath = Join-Path $hostRoot 'watchdog-query-lastboot.ps1'
$guestLastBootQueryPath = Join-Path $guestRoot 'watchdog-query-lastboot.ps1'
$guestLastBootOutputPath = Join-Path $guestRoot 'lastboot.txt'
$hostLastBootOutputPath = Join-Path $hostRoot 'lastboot.txt'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'
$repoSessionPath = Join-Path $repoRootOut 'session.json'
$repoStepArmPath = Join-Path $repoRootOut 'step-arm-summary.json'
$repoStepBootPath = Join-Path $repoRootOut 'step-boot-summary.json'
$repoStepStopPath = Join-Path $repoRootOut 'step-stop-summary.json'
$repoStepEtlPath = Join-Path $repoRootOut 'step-etl-summary.json'
$repoStepCopyPath = Join-Path $repoRootOut 'step-copy-summary.json'
$hostEtlPath = Join-Path $hostRoot 'watchdog-timeouts-boot.etl'
$guestEtlPath = Join-Path $guestRoot 'watchdog-timeouts-boot.etl'
$guestStatePath = Join-Path $guestRoot 'state.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('arm', 'stop')]
    [string]$Phase,

    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,

    [Parameter(Mandatory = $true)]
    [string]$GuestEtlPath,

    [Parameter(Mandatory = $true)]
    [string]$StatePath
)

$ErrorActionPreference = 'Stop'

$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$registryPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power'
$valueNames = @('WatchdogResumeTimeout', 'WatchdogSleepTimeout', 'PowerSettingProfile')

function Get-BaselineValues {
    $item = Get-ItemProperty -Path $registryPath -ErrorAction Stop
    $result = [ordered]@{}
    foreach ($name in $valueNames) {
        $result[$name] = $item.$name
    }
    return $result
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

if ($Phase -eq 'arm') {
    & $wpr -cancelboot | Out-Null
    & $wpr -cancel | Out-Null
    & $wpr -addboot Power -addboot Registry -filemode -recordtempto $GuestRoot | Out-Null

    $state = [ordered]@{
        phase = 'armed'
        generated_utc = [DateTime]::UtcNow.ToString('o')
        guest_root = $GuestRoot
        guest_etl_path = $GuestEtlPath
        baseline = Get-BaselineValues
    }
    $state | ConvertTo-Json -Depth 6 | Set-Content -Path $StatePath -Encoding UTF8
    exit 0
}

$state = if (Test-Path $StatePath) {
    Get-Content -Path $StatePath -Raw | ConvertFrom-Json
} else {
    [pscustomobject]@{
        phase = 'unknown'
        baseline = Get-BaselineValues
    }
}

& $wpr -stopboot $GuestEtlPath 'power.session-watchdog-timeouts baseline boot' | Out-Null

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    lane_label = 'power.session-watchdog-timeouts'
    phase = 'completed'
    guest_root = $GuestRoot
    guest_etl_path = $GuestEtlPath
    baseline = $state.baseline
    after_boot = Get-BaselineValues
    etl_exists = [bool](Test-Path $GuestEtlPath)
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $StatePath -Encoding UTF8
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8
@(
    '$ErrorActionPreference = ''Stop''',
    ('$out = ''{0}''' -f $guestLastBootOutputPath),
    '$dir = Split-Path -Parent $out',
    'if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }',
    '(Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToString(''o'') | Set-Content -Path $out -Encoding ASCII'
) | Set-Content -Path $hostLastBootQueryPath -Encoding ASCII

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

function Get-GuestLastBootUpTime {
    param([string]$HostOutputPath = $hostLastBootOutputPath)

    if (Test-Path $HostOutputPath) {
        Remove-Item -Path $HostOutputPath -Force
    }

    try {
        Invoke-GuestPowerShell -ArgumentList @(
            '-NoProfile',
            '-ExecutionPolicy', 'Bypass',
            '-File', $guestLastBootQueryPath
        )
    }
    catch {
        return $null
    }

    $copyOutput = & $VmrunPath -T ws -gu $GuestUser -gp $GuestPassword CopyFileFromGuestToHost $VmPath $guestLastBootOutputPath $HostOutputPath 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $HostOutputPath)) {
        return $null
    }

    $raw = (Get-Content -Path $HostOutputPath -Raw).Trim()
    if (-not $raw) {
        return $null
    }

    return [datetimeoffset]::Parse($raw)
}

function Wait-VmPoweredOff {
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
            if ($running -notmatch [regex]::Escape($VmPath)) {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw "VM did not power off within $TimeoutSeconds seconds."
}

function Invoke-HostBootCycle {
    param(
        [Parameter(Mandatory = $true)]
        [datetimeoffset]$PreviousBootUpTime,

        [int]$ShutdownTimeoutSeconds = 240,
        [int]$StartupTimeoutSeconds = 600
    )

    $stopMode = 'soft'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'soft') -IgnoreExitCode | Out-Null
    try {
        Wait-VmPoweredOff -TimeoutSeconds $ShutdownTimeoutSeconds
    }
    catch {
        $stopMode = 'hard'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'hard') -IgnoreExitCode | Out-Null
        Wait-VmPoweredOff -TimeoutSeconds 90
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady -TimeoutSeconds $StartupTimeoutSeconds
    Start-Sleep -Seconds 5

    $currentBoot = Get-GuestLastBootUpTime
    if ($null -eq $currentBoot) {
        throw 'Failed to capture guest LastBootUpTime after the host-driven boot cycle.'
    }

    if ($currentBoot -le $PreviousBootUpTime) {
        throw "Guest boot cycle completed, but LastBootUpTime did not advance. Previous=$($PreviousBootUpTime.ToString('o')) Current=$($currentBoot.ToString('o'))"
    }

    return [ordered]@{
        previous_last_boot_utc = $PreviousBootUpTime.ToString('o')
        current_last_boot_utc = $currentBoot.ToString('o')
        stop_mode = $stopMode
    }
}

function Get-ShellHealth {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1')
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

function Copy-FromGuestBounded {
    param(
        [string]$GuestPath,
        [string]$HostPath,
        [int]$TimeoutSeconds = 180
    )

    $stdoutPath = Join-Path $hostRoot ("copy-{0}.stdout.txt" -f ([IO.Path]::GetFileName($HostPath)))
    $stderrPath = Join-Path $hostRoot ("copy-{0}.stderr.txt" -f ([IO.Path]::GetFileName($HostPath)))
    if (Test-Path $stdoutPath) { Remove-Item -Path $stdoutPath -Force }
    if (Test-Path $stderrPath) { Remove-Item -Path $stderrPath -Force }
    if (Test-Path $HostPath) { Remove-Item -Path $HostPath -Force }

    $argumentList = @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromGuestToHost',
        $VmPath,
        $GuestPath,
        $HostPath
    )

    $process = Start-Process -FilePath $VmrunPath -ArgumentList $argumentList -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try { $process.Kill() } catch {}
        throw "vmrun copy timed out after $TimeoutSeconds seconds for $GuestPath"
    }

    if ($process.ExitCode -ne 0) {
        $stderr = if (Test-Path $stderrPath) { (Get-Content -Path $stderrPath -Raw).Trim() } else { '' }
        $stdout = if (Test-Path $stdoutPath) { (Get-Content -Path $stdoutPath -Raw).Trim() } else { '' }
        $detail = ($stderr, $stdout | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' '
        throw "vmrun copy failed with exit code $($process.ExitCode) for $GuestPath. $detail".Trim()
    }

    if (-not (Test-Path $HostPath)) {
        throw "vmrun copy reported success but host file was missing for $GuestPath"
    }
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
        [string]$Symptom,
        [bool]$ShellRecovered = $false,
        [bool]$NeededSnapshotRevert = $false,
        [string]$Notes = ''
    )

    & (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId 'power.session-watchdog-timeouts' `
        -TweakId 'power.session-watchdog-timeouts' `
        -TestId $TestId `
        -Family 'power.session-watchdog-timeouts' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power' `
        -ValueName 'WatchdogResumeTimeout|WatchdogSleepTimeout' `
        -ValueState 'baseline-read-only' `
        -Symptom $Symptom `
        -ShellRecovered:$ShellRecovered `
        -NeededSnapshotRevert:$NeededSnapshotRevert `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    lane_label = 'power.session-watchdog-timeouts'
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = 'started'
    shell_before = $null
    shell_after = $null
    boot_cycle = $null
    summary = $null
    raw_etl_captured = $false
    repo_etl_placeholder = "evidence/files/vm-tooling-staging/$probeName/watchdog-timeouts-boot.etl.md"
    recovery = [ordered]@{
        performed = $false
        shell_healthy_after_recovery = $false
    }
    step_summary_files = [ordered]@{
        arm = "evidence/files/vm-tooling-staging/$probeName/step-arm-summary.json"
        boot = "evidence/files/vm-tooling-staging/$probeName/step-boot-summary.json"
        stop = "evidence/files/vm-tooling-staging/$probeName/step-stop-summary.json"
        etl = "evidence/files/vm-tooling-staging/$probeName/step-etl-summary.json"
        copy = "evidence/files/vm-tooling-staging/$probeName/step-copy-summary.json"
        session = "evidence/files/vm-tooling-staging/$probeName/session.json"
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

    $summary.shell_before = Get-ShellHealth | ConvertFrom-Json
    if (-not $summary.shell_before.shell_healthy) {
        throw 'Shell health check failed before the watchdog boot trace started.'
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath
    Copy-ToGuest -HostPath $hostLastBootQueryPath -GuestPath $guestLastBootQueryPath

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'arm',
        '-GuestRoot', $guestRoot,
        '-GuestEtlPath', $guestEtlPath,
        '-StatePath', $guestStatePath
    )

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        step = 'arm'
        status = 'ok'
        snapshot_name = $SnapshotName
        guest_root = $guestRoot
        guest_etl_path = $guestEtlPath
        state_path = $guestStatePath
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $repoStepArmPath -Encoding UTF8

    $previousBoot = Get-GuestLastBootUpTime
    if ($null -eq $previousBoot) {
        throw 'Failed to capture guest LastBootUpTime before reboot.'
    }

    $summary.boot_cycle = Invoke-HostBootCycle -PreviousBootUpTime $previousBoot
    Start-Sleep -Seconds $PostBootSettleSeconds

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        step = 'boot'
        status = 'ok'
        snapshot_name = $SnapshotName
        boot_cycle = $summary.boot_cycle
        post_boot_settle_seconds = $PostBootSettleSeconds
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $repoStepBootPath -Encoding UTF8

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'stop',
        '-GuestRoot', $guestRoot,
        '-GuestEtlPath', $guestEtlPath,
        '-StatePath', $guestStatePath
    )

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        step = 'stop'
        status = 'ok'
        snapshot_name = $SnapshotName
        guest_etl_path = $guestEtlPath
        guest_state_path = $guestStatePath
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $repoStepStopPath -Encoding UTF8

    Copy-FromGuest -GuestPath $guestStatePath -HostPath $hostSummaryPath
    $summary.summary = Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        step = 'etl'
        status = if ($summary.summary.etl_exists) { 'ok' } else { 'missing' }
        snapshot_name = $SnapshotName
        guest_etl_path = $guestEtlPath
        etl_exists = [bool]$summary.summary.etl_exists
        baseline = $summary.summary.baseline
        after_boot = $summary.summary.after_boot
    } | ConvertTo-Json -Depth 8 | Set-Content -Path $repoStepEtlPath -Encoding UTF8

    $copyStatus = 'ok'
    $copyError = $null
    try {
        Copy-FromGuestBounded -GuestPath $guestEtlPath -HostPath $hostEtlPath
    }
    catch {
        $copyStatus = 'failed'
        $copyError = $_.Exception.Message
    }

    $summary.raw_etl_captured = [bool](Test-Path $hostEtlPath)
    if ($summary.raw_etl_captured -and $copyStatus -eq 'failed') {
        $copyStatus = 'ok-with-timeout'
    }
    elseif (-not $summary.raw_etl_captured) {
        $probeFailed = $true
        if ($copyStatus -eq 'ok') {
            $copyStatus = 'missing'
        }
        if ($copyError) {
            $summary.errors += $copyError
        }
        else {
            $summary.errors += 'The watchdog boot trace ETL did not copy back to the host.'
        }
    }

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        step = 'copy'
        status = $copyStatus
        snapshot_name = $SnapshotName
        guest_etl_path = $guestEtlPath
        host_etl_path = $hostEtlPath
        raw_etl_captured = $summary.raw_etl_captured
        error = $copyError
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $repoStepCopyPath -Encoding UTF8

    $summary.shell_after = Get-ShellHealth | ConvertFrom-Json
    if (-not $summary.shell_after.shell_healthy) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += 'Shell health was degraded after the watchdog boot trace.'
        Log-Incident -TestId $probeName -Symptom 'Shell health was degraded after the watchdog boot trace.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Recovered by snapshot revert after the baseline boot trace lane.'
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Boot trace lane failed before the final summary could be captured.'
}
finally {
    if ($needsRecovery -and -not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        try {
            Restore-HealthySnapshot
            $recoveredShell = Get-ShellHealth | ConvertFrom-Json
            $summary.recovery.performed = $true
            $summary.recovery.shell_healthy_after_recovery = [bool]$recoveredShell.shell_healthy
        }
        catch {
            $summary.errors += "Recovery failed: $($_.Exception.Message)"
        }
    }
}

$summary.status = if ($probeFailed) { 'failed' } else { 'ok' }
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $repoSummaryPath -Encoding UTF8

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    status = $summary.status
    completed_steps = @(
        if (Test-Path $repoStepArmPath) { 'arm' }
        if (Test-Path $repoStepBootPath) { 'boot' }
        if (Test-Path $repoStepStopPath) { 'stop' }
        if (Test-Path $repoStepEtlPath) { 'etl' }
        if (Test-Path $repoStepCopyPath) { 'copy' }
    )
    summary_file = "summary.json"
    step_summary_files = $summary.step_summary_files
} | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSessionPath -Encoding UTF8

$etlPlaceholder = @(
    '# External Evidence Placeholder',
    '',
    'Title: power.session-watchdog-timeouts baseline boot trace',
    '',
    'The raw ETL is kept off-git. Use the summary JSON in the same folder and the runtime-prep audit note for the machine-readable result.'
) -join "`n"
Set-Content -Path (Join-Path $repoRootOut 'watchdog-timeouts-boot.etl.md') -Value $etlPlaceholder -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}
