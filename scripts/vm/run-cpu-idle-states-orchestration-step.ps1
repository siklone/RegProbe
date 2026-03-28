[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('A', 'B', 'C', 'D')]
    [string]$Step,

    [string]$SessionId = '',
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestRootBase = 'C:\RegProbe-Diag',
    [string]$RecordId = 'power.disable-cpu-idle-states',
    [string]$SnapshotName = 'baseline-20260327-regprobe-visible-shell-stable',
    [string]$IncidentLogPath = '',
    [int]$SettleSeconds = 20,
    [switch]$PerformReboot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

if ($Step -eq 'B' -and -not $PSBoundParameters.ContainsKey('PerformReboot')) {
    $PerformReboot = $true
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'

if ($Step -eq 'A' -and [string]::IsNullOrWhiteSpace($SessionId)) {
    $SessionId = 'cpu-idle-stepwise-{0}' -f (Get-Date -Format 'yyyyMMdd-HHmmss')
}

if ([string]::IsNullOrWhiteSpace($SessionId)) {
    throw 'SessionId is required for steps B, C, and D.'
}

$hostRoot = Join-Path $HostOutputRoot $SessionId
$repoRootOut = Join-Path $repoEvidenceRoot $SessionId
$guestRoot = Join-Path $GuestRootBase $SessionId
$hostPayloadPath = Join-Path $hostRoot 'cpu-idle-stepwise-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'cpu-idle-stepwise-payload.ps1'
$hostRestartScriptPath = Join-Path $PSScriptRoot 'request-guest-restart.ps1'
$guestRestartScriptPath = Join-Path $guestRoot 'request-guest-restart.ps1'
$hostManifestPath = Join-Path $hostRoot 'session.json'
$repoManifestPath = Join-Path $repoRootOut 'session.json'
$guestEtlPath = Join-Path $guestRoot 'cpu-idle-stepwise.etl'
$hostEtlPath = Join-Path $hostRoot 'cpu-idle-stepwise.etl'
$repoEtlPlaceholderPath = Join-Path $repoRootOut 'cpu-idle-stepwise.etl.md'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('set-baseline', 'set-candidate', 'read-state', 'start-wpr', 'stop-wpr', 'restore-baseline')]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$EtlPath = ''
)

$ErrorActionPreference = 'Stop'
$registryPath = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power'
$regExePath = 'HKLM\SYSTEM\CurrentControlSet\Control\Power'
$valueNames = @('DisableIdleStatesAtBoot', 'IdleStateTimeout', 'ExitLatencyCheckEnabled')
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'

function Get-BundleState {
    $result = [ordered]@{}
    foreach ($name in $valueNames) {
        try {
            $value = (Get-ItemProperty -Path $registryPath -Name $name -ErrorAction Stop).$name
            $result[$name] = $value
        }
        catch {
            $result[$name] = $null
        }
    }

    return [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        last_boot_utc = (Get-CimInstance -ClassName Win32_OperatingSystem).LastBootUpTime.ToUniversalTime().ToString('o')
        values = $result
    }
}

function Reset-Bundle {
    foreach ($name in $valueNames) {
        Remove-ItemProperty -Path $registryPath -Name $name -ErrorAction SilentlyContinue
    }
}

try {
    switch ($Action) {
        'set-baseline' {
            Reset-Bundle
            [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                action = $Action
                status = 'ok'
                state = (Get-BundleState)
            } | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
        }
        'set-candidate' {
            & reg.exe add $regExePath /v DisableIdleStatesAtBoot /t REG_DWORD /d 1 /f | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "reg.exe add failed for DisableIdleStatesAtBoot with exit code $LASTEXITCODE" }
            & reg.exe add $regExePath /v IdleStateTimeout /t REG_DWORD /d 0 /f | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "reg.exe add failed for IdleStateTimeout with exit code $LASTEXITCODE" }
            & reg.exe add $regExePath /v ExitLatencyCheckEnabled /t REG_DWORD /d 1 /f | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "reg.exe add failed for ExitLatencyCheckEnabled with exit code $LASTEXITCODE" }
            [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                action = $Action
                status = 'ok'
                state = (Get-BundleState)
            } | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
        }
        'read-state' {
            [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                action = $Action
                status = 'ok'
                state = (Get-BundleState)
            } | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
        }
        'restore-baseline' {
            Reset-Bundle
            [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                action = $Action
                status = 'ok'
                state = (Get-BundleState)
            } | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
        }
        'start-wpr' {
            & $wpr -cancel | Out-Null
            & $wpr -start GeneralProfile -filemode | Out-Null
            [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                action = 'start-wpr'
                status = 'ok'
                started = $true
            } | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
        }
        'stop-wpr' {
            if ([string]::IsNullOrWhiteSpace($EtlPath)) {
                throw 'EtlPath is required for stop-wpr.'
            }

            New-Item -ItemType Directory -Path (Split-Path -Parent $EtlPath) -Force | Out-Null
            & $wpr -stop $EtlPath | Out-Null
            [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                action = 'stop-wpr'
                status = 'ok'
                stopped = $true
                etl_path = $EtlPath
                etl_exists = [bool](Test-Path $EtlPath)
            } | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
        }
    }
}
catch {
    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        action = $Action
        status = 'error'
        error = $_.Exception.Message
        at = $_.InvocationInfo.PositionMessage
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
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
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1') `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword
}

function Wait-ShellHealthy {
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $health = Get-ShellHealth | ConvertFrom-Json
        if ($health.shell_healthy) {
            return $health
        }

        Start-Sleep -Seconds 5
    }

    throw 'Guest reached VMware Tools ready state, but the shell did not become healthy in time.'
}

function Ensure-GuestDirectory {
    param([string]$GuestPath)

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'createDirectoryInGuest', $VmPath, $GuestPath
        ) | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'already exists') {
            throw
        }
    }
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath
    ) | Out-Null
}

function Copy-FromGuestBestEffort {
    param(
        [string]$GuestPath,
        [string]$HostPath,
        [string]$RepoPath = ''
    )

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
        ) | Out-Null
        if (-not [string]::IsNullOrWhiteSpace($RepoPath)) {
            Copy-Item -Path $HostPath -Destination $RepoPath -Force
        }
        return $true
    }
    catch {
        return $false
    }
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
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

function Restart-GuestCycle {
    $stopMode = 'soft'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'soft') -IgnoreExitCode | Out-Null
    try {
        Wait-VmPoweredOff -TimeoutSeconds 240
    }
    catch {
        $stopMode = 'hard'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'hard') -IgnoreExitCode | Out-Null
        Wait-VmPoweredOff -TimeoutSeconds 90
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Start-Sleep -Seconds 5
    return $stopMode
}

function Restore-HealthySnapshot {
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        return
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-ShellHealthy | Out-Null
}

function Log-Incident {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestId,
        [Parameter(Mandatory = $true)]
        [string]$ValueState,
        [Parameter(Mandatory = $true)]
        [string]$Symptom,
        [string]$Notes = ''
    )

    & (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId $RecordId `
        -TweakId $RecordId `
        -TestId $TestId `
        -Family 'power-runtime-stepwise' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
        -ValueName 'DisableIdleStatesAtBoot + IdleStateTimeout + ExitLatencyCheckEnabled' `
        -ValueState $ValueState `
        -Symptom $Symptom `
        -ShellRecovered:$false `
        -NeededSnapshotRevert:$true `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

function New-StepSummary {
    param([string]$CurrentStep)

    return [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        record_id = $RecordId
        session_id = $SessionId
        step = $CurrentStep
        snapshot_name = $SnapshotName
        guest_root = $guestRoot
        repo_root = "evidence/files/vm-tooling-staging/$SessionId"
        status = 'started'
        failed_stage = $null
        errors = @()
        recovery = [ordered]@{
            performed = $false
            shell_healthy_after_recovery = $false
        }
    }
}

function Save-StepSummary {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Summary
    )

    $fileName = "step-$($Summary.step.ToLowerInvariant())-summary.json"
    $hostPath = Join-Path $hostRoot $fileName
    $repoPath = Join-Path $repoRootOut $fileName
    $Summary.generated_utc = [DateTime]::UtcNow.ToString('o')
    $Summary | ConvertTo-Json -Depth 10 | Set-Content -Path $hostPath -Encoding UTF8
    $Summary | ConvertTo-Json -Depth 10 | Set-Content -Path $repoPath -Encoding UTF8
    return $repoPath
}

function Load-Session {
    if (-not (Test-Path $repoManifestPath)) {
        throw "Missing session manifest: $repoManifestPath"
    }

    return Get-Content -Path $repoManifestPath -Raw | ConvertFrom-Json
}

function Save-Session {
    param([Parameter(Mandatory = $true)]$Session)

    $Session.generated_utc = [DateTime]::UtcNow.ToString('o')
    $Session | ConvertTo-Json -Depth 10 | Set-Content -Path $hostManifestPath -Encoding UTF8
    $Session | ConvertTo-Json -Depth 10 | Set-Content -Path $repoManifestPath -Encoding UTF8
}

function New-Session {
    $session = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        record_id = $RecordId
        session_id = $SessionId
        snapshot_name = $SnapshotName
        host_root = $hostRoot
        repo_root = "evidence/files/vm-tooling-staging/$SessionId"
        guest_root = $guestRoot
        guest_payload_path = $guestPayloadPath
        guest_etl_path = $guestEtlPath
        completed_steps = @()
        next_step = 'A'
        phase_status = [ordered]@{}
        artifacts = [ordered]@{
            baseline = 'baseline.json'
            wpr_start = 'wpr-start.json'
            candidate = 'candidate.json'
            post_boot = 'post-boot.json'
            wpr_stop = 'wpr-stop.json'
            restore = 'restore.json'
            restore_after_boot = 'restore-after-boot.json'
            etl_placeholder = 'cpu-idle-stepwise.etl.md'
        }
    }

    Save-Session -Session $session
    return (Load-Session)
}

function Set-SessionPhaseStatus {
    param(
        [Parameter(Mandatory = $true)]$Session,
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][hashtable]$Summary
    )

    if ($null -eq $Session.phase_status) {
        $Session.phase_status = [ordered]@{}
    }
    elseif ($Session.phase_status -isnot [System.Collections.IDictionary]) {
        $normalizedPhaseStatus = [ordered]@{}
        foreach ($property in $Session.phase_status.PSObject.Properties) {
            $normalizedPhaseStatus[$property.Name] = $property.Value
        }
        $Session.phase_status = $normalizedPhaseStatus
    }

    $Session.phase_status[$Phase] = [ordered]@{
        status = $Summary.status
        failed_stage = $Summary.failed_stage
        summary_file = "step-$($Phase.ToLowerInvariant())-summary.json"
    }
}

function Invoke-GuestActionAndCopy {
    param(
        [Parameter(Mandatory = $true)][string]$Action,
        [Parameter(Mandatory = $true)][string]$OutputFileName,
        [string]$EtlPath = ''
    )

    $guestOutputPath = Join-Path $guestRoot $OutputFileName
    $hostOutputPath = Join-Path $hostRoot $OutputFileName
    $repoOutputPath = Join-Path $repoRootOut $OutputFileName

    $args = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Action', $Action,
        '-OutputPath', $guestOutputPath
    )
    if (-not [string]::IsNullOrWhiteSpace($EtlPath)) {
        $args += @('-EtlPath', $EtlPath)
    }

    $runError = $null
    try {
        Invoke-GuestPowerShell -ArgumentList $args
    }
    catch {
        $runError = $_.Exception.Message
    }
    $copied = Copy-FromGuestBestEffort -GuestPath $guestOutputPath -HostPath $hostOutputPath -RepoPath $repoOutputPath
    if (-not $copied -and $runError) {
        throw "Guest action $Action failed before a copyable output file existed: $runError"
    }
    if (-not $copied) {
        throw "Guest action $Action did not produce a copyable output file: $guestOutputPath"
    }

    $payload = Get-Content -Path $hostOutputPath -Raw | ConvertFrom-Json
    if ($runError) {
        $errorMessage = if ($payload.status -eq 'error' -and $payload.error) { $payload.error } else { $runError }
        throw "Guest action $Action failed: $errorMessage"
    }

    return $payload
}

function Write-EtlPlaceholder {
    $content = @(
        '# External Evidence Placeholder',
        '',
        'Title: CPU idle stepwise WPR trace',
        '',
        'The raw ETL is kept off-git. Use the step summaries, session manifest, and WPR stop manifest in the same folder for the machine-readable result.'
    ) -join "`n"
    Set-Content -Path $repoEtlPlaceholderPath -Value $content -Encoding UTF8
}

$session = if (Test-Path $repoManifestPath) { Load-Session } else { $null }
if ($Step -eq 'A' -and -not $session) {
    $session = New-Session
}
elseif (-not $session) {
    throw "Session $SessionId does not exist."
}

$summary = New-StepSummary -CurrentStep $Step
$needsRecovery = $false

try {
    switch ($Step) {
        'A' {
            $summary.failed_stage = 'revert-snapshot'
            if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
                Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
            }

            $summary.failed_stage = 'start-vm'
            Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
            $summary.failed_stage = 'wait-tools'
            Wait-GuestReady
            $summary.failed_stage = 'wait-shell'
            $summary.shell_before = Wait-ShellHealthy
            $summary.failed_stage = 'prepare-guest-root'
            Ensure-GuestDirectory -GuestPath $guestRoot
            $summary.failed_stage = 'copy-payload'
            Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath
            $summary.failed_stage = 'set-baseline'
            $summary.baseline = Invoke-GuestActionAndCopy -Action 'set-baseline' -OutputFileName 'baseline.json'
            $summary.failed_stage = 'start-wpr'
            $summary.wpr_start = Invoke-GuestActionAndCopy -Action 'start-wpr' -OutputFileName 'wpr-start.json'
            $summary.failed_stage = 'set-candidate'
            $summary.candidate = Invoke-GuestActionAndCopy -Action 'set-candidate' -OutputFileName 'candidate.json'
            $summary.status = 'ok'
            $summary.failed_stage = $null
            if ($session.completed_steps -notcontains 'A') {
                $session.completed_steps += 'A'
            }
            $session.next_step = 'B'
        }
        'B' {
            $summary.failed_stage = 'wait-tools-precheck'
            Wait-GuestReady
            if ($PerformReboot) {
                $summary.failed_stage = 'restart-guest'
                $summary.reboot_mode = Restart-GuestCycle
            }
            $summary.failed_stage = 'wait-tools-post-reboot'
            Wait-GuestReady
            $summary.failed_stage = 'wait-shell-post-reboot'
            $summary.shell_after_reboot = Wait-ShellHealthy
            $summary.failed_stage = 'settle'
            Start-Sleep -Seconds $SettleSeconds
            $summary.failed_stage = 'read-post-boot'
            $summary.post_boot = Invoke-GuestActionAndCopy -Action 'read-state' -OutputFileName 'post-boot.json'
            $summary.failed_stage = 'stop-wpr'
            $summary.wpr_stop = Invoke-GuestActionAndCopy -Action 'stop-wpr' -OutputFileName 'wpr-stop.json' -EtlPath $guestEtlPath
            $summary.status = 'ok'
            $summary.failed_stage = $null
            if ($session.completed_steps -notcontains 'B') {
                $session.completed_steps += 'B'
            }
            $session.next_step = 'C'
        }
        'C' {
            $summary.failed_stage = 'collect-baseline'
            $summary.baseline_collected = Copy-FromGuestBestEffort -GuestPath (Join-Path $guestRoot 'baseline.json') -HostPath (Join-Path $hostRoot 'baseline.json') -RepoPath (Join-Path $repoRootOut 'baseline.json')
            $summary.failed_stage = 'collect-wpr-start'
            $summary.wpr_start_collected = Copy-FromGuestBestEffort -GuestPath (Join-Path $guestRoot 'wpr-start.json') -HostPath (Join-Path $hostRoot 'wpr-start.json') -RepoPath (Join-Path $repoRootOut 'wpr-start.json')
            $summary.failed_stage = 'collect-candidate'
            $summary.candidate_collected = Copy-FromGuestBestEffort -GuestPath (Join-Path $guestRoot 'candidate.json') -HostPath (Join-Path $hostRoot 'candidate.json') -RepoPath (Join-Path $repoRootOut 'candidate.json')
            $summary.failed_stage = 'collect-post-boot'
            $summary.post_boot_collected = Copy-FromGuestBestEffort -GuestPath (Join-Path $guestRoot 'post-boot.json') -HostPath (Join-Path $hostRoot 'post-boot.json') -RepoPath (Join-Path $repoRootOut 'post-boot.json')
            $summary.failed_stage = 'collect-wpr-stop'
            $summary.wpr_stop_collected = Copy-FromGuestBestEffort -GuestPath (Join-Path $guestRoot 'wpr-stop.json') -HostPath (Join-Path $hostRoot 'wpr-stop.json') -RepoPath (Join-Path $repoRootOut 'wpr-stop.json')
            $summary.failed_stage = 'collect-etl'
            $summary.etl_copied_to_host = Copy-FromGuestBestEffort -GuestPath $guestEtlPath -HostPath $hostEtlPath
            $summary.failed_stage = 'write-placeholder'
            Write-EtlPlaceholder
            $summary.status = 'ok'
            $summary.failed_stage = $null
            if ($session.completed_steps -notcontains 'C') {
                $session.completed_steps += 'C'
            }
            $session.next_step = if ($session.completed_steps -contains 'D') { '' } else { 'D' }
        }
        'D' {
            $summary.failed_stage = 'restore-baseline'
            $summary.restore = Invoke-GuestActionAndCopy -Action 'restore-baseline' -OutputFileName 'restore.json'
            if ($PerformReboot) {
                $summary.failed_stage = 'restore-restart-guest'
                $summary.reboot_mode = Restart-GuestCycle
                $summary.failed_stage = 'restore-wait-tools'
                Wait-GuestReady
                $summary.failed_stage = 'restore-wait-shell'
                $summary.restore_shell = Wait-ShellHealthy
                $summary.failed_stage = 'restore-read-state'
                $summary.restore_after_boot = Invoke-GuestActionAndCopy -Action 'read-state' -OutputFileName 'restore-after-boot.json'
            }
            $summary.status = 'ok'
            $summary.failed_stage = $null
            if ($session.completed_steps -notcontains 'D') {
                $session.completed_steps += 'D'
            }
            $session.next_step = ''
        }
    }
}
catch {
    $summary.status = 'failed'
    $summary.errors += $_.Exception.Message
    $needsRecovery = $true
    Log-Incident -TestId $SessionId -ValueState ("cpu-idle-step-{0}" -f $Step.ToLowerInvariant()) -Symptom $_.Exception.Message -Notes "Stepwise CPU idle orchestration failed during stage $($summary.failed_stage)."
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

    Set-SessionPhaseStatus -Session $session -Phase $Step -Summary $summary
    Save-Session -Session $session
    $repoSummaryPath = Save-StepSummary -Summary $summary
}

Write-Output $repoSummaryPath
if ($summary.status -ne 'ok') {
    exit 1
}
