[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$SnapshotName = '',
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',
    [int]$PostBootSettleSeconds = 20,
    [int]$PollSeconds = 20,
    [int]$RunTimeoutSeconds = 5400,
    [string[]]$CandidateIds = @(),
    [switch]$RecoverOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\vm\_vmrun-common.ps1')
$guestCredential = Resolve-RegProbeVmCredential -GuestUser $GuestUser -GuestPassword $GuestPassword
$GuestUser = $guestCredential.UserName
$GuestPassword = $guestCredential.GetNetworkCredential().Password
$baselineResolver = Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1'
$vmProfileTag = 'primary'
$repoEvidenceBase = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$hostStagingBase = Join-Path ([System.IO.Path]::GetTempPath()) 'vm-tooling-staging-primary'
$guestScriptRoot = 'C:\Tools\Scripts'
$guestDiagBase = 'C:\RegProbe-Diag'
if (Test-Path $baselineResolver) {
    . $baselineResolver
    $vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
    if ([string]::IsNullOrWhiteSpace($VmPath)) {
        $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile
    }
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        $SnapshotName = Resolve-DefaultVmSnapshotName -VmProfile $VmProfile
    }
    $repoEvidenceBase = Resolve-TrackedVmOutputRoot -VmProfile $VmProfile -Fallback $repoEvidenceBase
    $hostStagingBase = Resolve-HostStagingRoot -VmProfile $VmProfile -Fallback $hostStagingBase
    $guestScriptRoot = Resolve-GuestScriptRoot -VmProfile $VmProfile -Fallback $guestScriptRoot
    $guestDiagBase = Resolve-GuestDiagRoot -VmProfile $VmProfile -Fallback $guestDiagBase
}

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx'
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = 'RegProbe-Baseline-ToolsHardened-20260330'
}

$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$phase0Path = Join-Path $repoRoot 'registry-research-framework\audit\kernel-power-96-phase0-candidates-20260329.json'
$hitQueuePath = Join-Path $repoRoot 'registry-research-framework\audit\kernel-power-96-broad-targeted-string-hit-queue-20260331.json'
$guestPayloadSourcePath = Join-Path $repoRoot 'scripts\vm\run-power-control-batch-mega-trigger-runtime.guest.ps1'
$defaultPilotCandidateIds = @(
    'power.control.allow-audio-to-enable-execution-required-power-requests',
    'power.control.allow-system-required-power-requests',
    'power.control.always-compute-qos-hints',
    'power.control.coalescing-flush-interval',
    'power.control.idle-processors-require-qos-management'
)
$pilotTriggers = @(
    'cpu_stress',
    'power_plan_and_requests',
    'multi_thread_burst',
    'disk_io_burst',
    'process_spawn_burst',
    'foreground_background_switch',
    'timer_resolution_change',
    'network_activity'
)

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeKind = if ($RecoverOnly) { 'recovery' } else { 'runtime' }
$probeName = "power-control-batch-mega-trigger-$probeKind-$vmProfileTag-$stamp"
$repoOutputRoot = Join-Path $repoEvidenceBase $probeName
$hostWorkRoot = Join-Path $hostStagingBase $probeName
$guestBatchRoot = Join-Path $guestDiagBase $probeName
$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'
$repoManifestPath = Join-Path $repoOutputRoot 'manifest.json'
$repoSessionPath = Join-Path $repoOutputRoot 'session.json'
$repoStatePath = Join-Path $repoOutputRoot 'state.json'
$repoRunLogPath = Join-Path $repoOutputRoot 'guest-run.log'
$hostManifestPath = Join-Path $hostWorkRoot 'manifest.json'
$hostSummaryPath = Join-Path $hostWorkRoot 'summary.json'
$hostResultsPath = Join-Path $hostWorkRoot 'results.json'
$hostStatePath = Join-Path $hostWorkRoot 'state.json'
$hostRunLogPath = Join-Path $hostWorkRoot 'guest-run.log'
$guestPayloadPath = Join-Path $guestScriptRoot 'run-power-control-batch-mega-trigger-runtime.guest.ps1'
$guestManifestPath = Join-Path $guestBatchRoot 'manifest.json'
$guestSummaryPath = Join-Path $guestBatchRoot 'summary.json'
$guestResultsPath = Join-Path $guestBatchRoot 'results.json'
$guestStatePath = Join-Path $guestBatchRoot 'state.json'
$guestRunLogPath = Join-Path $guestBatchRoot 'guest-run.log'
$guestRunStdoutPath = Join-Path $hostWorkRoot 'guest-run.stdout.txt'
$guestRunStderrPath = Join-Path $hostWorkRoot 'guest-run.stderr.txt'

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$InputObject,
        [int]$Depth = 12
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

function Read-JsonFile {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }
    try {
        return (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

function ConvertTo-SingleQuotedPowerShellLiteral {
    param([string]$Value)
    if ($null -eq $Value) {
        $Value = ''
    }
    return "'" + ($Value -replace "'", "''") + "'"
}

function ConvertTo-EncodedPowerShellCommand {
    param([string]$Command)
    return [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($Command))
}

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)
    return Invoke-RegProbeVmrun -VmrunPath $VmrunPath -Arguments $Arguments -IgnoreExitCode:$IgnoreExitCode
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

function Stop-VmHardBestEffort {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'hard') -IgnoreExitCode | Out-Null
    try {
        Wait-VmPoweredOff -TimeoutSeconds 120
    }
    catch {
    }
}

function Wait-GuestCommandReady {
    param([int]$TimeoutSeconds = 1200)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'runProgramInGuest', $VmPath,
                'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
                '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', 'exit 0'
            ) | Out-Null
            return
        }
        catch {
        }
        Start-Sleep -Seconds 5
    }
    throw 'Guest command execution did not become ready in time.'
}

function Start-VmNonBlocking {
    Start-Process -FilePath $VmrunPath -ArgumentList @('-T', 'ws', 'start', $VmPath, 'nogui') -WindowStyle Hidden | Out-Null
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
    param([string]$GuestPath, [string]$HostPath, [string]$RepoPath = '')
    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
        ) | Out-Null
        if ($RepoPath -and (Test-Path -LiteralPath $HostPath)) {
            Copy-Item -LiteralPath $HostPath -Destination $RepoPath -Force
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

function Start-GuestRunAsync {
    param([string[]]$ArgumentList)

    foreach ($path in @($guestRunStdoutPath, $guestRunStderrPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        }
    }

    $literalArgs = @(
        $ArgumentList | ForEach-Object {
            ConvertTo-SingleQuotedPowerShellLiteral -Value ([string]$_)
        }
    )
    $launcherCommand = @"
`$argList = @(
    $($literalArgs -join ",`r`n    ")
)
Start-Process -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -ArgumentList `$argList -WindowStyle Hidden | Out-Null
"@
    $encodedLauncher = ConvertTo-EncodedPowerShellCommand -Command $launcherCommand
    $launcherOutput = Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $encodedLauncher
    )
    Set-Content -Path $guestRunStdoutPath -Value $launcherOutput -Encoding UTF8
    Set-Content -Path $guestRunStderrPath -Value '' -Encoding UTF8

    return [ordered]@{
        launched_utc = [DateTime]::UtcNow.ToString('o')
        launcher_output = $launcherOutput
    }
}

function Get-ShellHealthObject {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json
}

function Get-ShellHealthBestEffort {
    param([int]$TimeoutSeconds = 120)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $last = Get-ShellHealthObject
            if ($last.shell_healthy) {
                return $last
            }
        }
        catch {
        }
        Start-Sleep -Seconds 5
    }
    return $last
}

function Assert-ShellHealthy {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Phase,
        [int]$TimeoutSeconds = 1200
    )

    $health = Get-ShellHealthBestEffort -TimeoutSeconds $TimeoutSeconds
    if ($null -eq $health -or -not $health.shell_healthy) {
        throw "Shell health check failed at $Phase."
    }

    return $health
}

function Wait-GuestOperational {
    param(
        [string]$Phase = 'guest-operational',
        [int]$ShellTimeoutSeconds = 1200,
        [int]$PostShellDelaySeconds = 30,
        [int]$CommandTimeoutSeconds = 1200
    )

    Assert-ShellHealthy -Phase "$Phase-shell" -TimeoutSeconds $ShellTimeoutSeconds | Out-Null
    Start-Sleep -Seconds $PostShellDelaySeconds
    Wait-GuestCommandReady -TimeoutSeconds $CommandTimeoutSeconds
}

function Restore-HealthySnapshot {
    Stop-VmHardBestEffort
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Start-VmNonBlocking
    Wait-GuestOperational -Phase 'restore' -ShellTimeoutSeconds 1200 -PostShellDelaySeconds 30 -CommandTimeoutSeconds 1200
}

function Restart-GuestCycle {
    $stopMode = 'soft'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'soft') -IgnoreExitCode | Out-Null
    try {
        Wait-VmPoweredOff -TimeoutSeconds 240
    }
    catch {
        $stopMode = 'hard'
        Stop-VmHardBestEffort
    }

    Start-VmNonBlocking
    Wait-GuestOperational -Phase 'restart-cycle' -ShellTimeoutSeconds 1200 -PostShellDelaySeconds 45 -CommandTimeoutSeconds 1200
    return $stopMode
}

function Get-PreviousMegaTriggerProbes {
    if (-not (Test-Path -LiteralPath $repoEvidenceBase)) {
        return @()
    }

    return @(
        Get-ChildItem -LiteralPath $repoEvidenceBase -Directory |
            Where-Object {
                $_.Name -like "power-control-batch-mega-trigger-runtime-$vmProfileTag-*" -and
                $_.Name -ne $probeName
            } |
            Sort-Object LastWriteTime -Descending
    )
}

function Get-ProbeInfo {
    param([System.IO.DirectoryInfo]$Directory)

    $summaryPath = Join-Path $Directory.FullName 'summary.json'
    $statePath = Join-Path $Directory.FullName 'state.json'
    $sessionPath = Join-Path $Directory.FullName 'session.json'
    $summary = Read-JsonFile -Path $summaryPath
    $state = Read-JsonFile -Path $statePath
    $session = Read-JsonFile -Path $sessionPath
    $summaryStatus = if ($summary) { [string]$summary.status } else { '' }
    $statePhase = if ($state) { [string]$state.phase } else { '' }
    $sessionStatus = if ($session) { [string]$session.status } else { '' }
    $recovered = ($summaryStatus -eq 'aborted-recovered' -or $sessionStatus -eq 'aborted-recovered')
    $missingSession = ($null -eq $session)
    $stale = (-not $recovered) -and (
        $statePhase -in @('armed', 'running', 'parsing') -or
        $summaryStatus -in @('armed', 'running', 'started') -or
        $missingSession
    )

    [ordered]@{
        probe_name = $Directory.Name
        directory = $Directory.FullName
        summary_path = $summaryPath
        state_path = $statePath
        session_path = $sessionPath
        summary = $summary
        state = $state
        session = $session
        summary_status = $summaryStatus
        state_phase = $statePhase
        session_status = $sessionStatus
        stale = $stale
    }
}

function Get-StaleProbeInfos {
    return @(
        Get-PreviousMegaTriggerProbes | ForEach-Object { Get-ProbeInfo -Directory $_ } | Where-Object { $_.stale }
    )
}

function New-PlaceholderResults {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status,
        [Parameter(Mandatory = $true)]
        [object[]]$Candidates
    )

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        total_candidates = @($Candidates).Count
        csv_exists = $false
        csv_line_count = 0
        path_line_count = 0
        exact_hit_count = 0
        exact_line_only_count = 0
        path_only_count = 0
        no_hit_count = 0
        boot_unsafe_count = if ($Status -eq 'boot-unsafe') { @($Candidates).Count } else { 0 }
        aborted_recovered_count = if ($Status -eq 'aborted-recovered') { @($Candidates).Count } else { 0 }
        trigger_log = @()
        candidates = @(
            foreach ($candidate in $Candidates) {
                [ordered]@{
                    candidate_id = [string]$candidate.candidate_id
                    value_name = [string]$candidate.value_name
                    exact_line_count = 0
                    exact_query_hits = 0
                    status = $Status
                    sample_lines = @()
                }
            }
        )
    }
}

function Write-ProbeArtifacts {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Summary,
        [Parameter(Mandatory = $true)]
        [object]$Results
    )

    Write-JsonFile -Path $hostSummaryPath -InputObject $Summary
    Write-JsonFile -Path $repoSummaryPath -InputObject $Summary
    Write-JsonFile -Path $hostResultsPath -InputObject $Results
    Write-JsonFile -Path $repoResultsPath -InputObject $Results
}

function Mark-StaleProbeRecovered {
    param(
        [Parameter(Mandatory = $true)]
        [object]$ProbeInfo,
        [Parameter(Mandatory = $true)]
        [string]$RecoveredBy,
        [Parameter(Mandatory = $true)]
        [string[]]$Reasons
    )

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        probe_name = $ProbeInfo.probe_name
        family = 'power-control'
        pattern = 'mega-trigger'
        status = 'aborted-recovered'
        recovery_by = $RecoveredBy
        recovery_reasons = @($Reasons)
        previous_summary_status = $ProbeInfo.summary_status
        previous_state_phase = $ProbeInfo.state_phase
    }

    $session = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        probe_name = $ProbeInfo.probe_name
        status = 'aborted-recovered'
        recovery_by = $RecoveredBy
        recovery_reasons = @($Reasons)
    }

    Write-JsonFile -Path $ProbeInfo.summary_path -InputObject $summary
    Write-JsonFile -Path $ProbeInfo.session_path -InputObject $session
}

function Wait-ForGuestRunCompletion {
    param(
        [int]$StaleProgressSeconds = 900
    )

    $terminalStatuses = @('exact-hit', 'exact-line-no-query', 'path-only-hit', 'no-hit', 'error', 'boot-unsafe', 'aborted-recovered')
    $deadline = (Get-Date).AddSeconds($RunTimeoutSeconds)
    $lastProgress = Get-Date
    $lastSignature = ''
    $pollCount = 0
    while ((Get-Date) -lt $deadline) {
        $pollCount++
        Copy-FromGuestBestEffort -GuestPath $guestStatePath -HostPath $hostStatePath -RepoPath $repoStatePath | Out-Null
        Copy-FromGuestBestEffort -GuestPath $guestRunLogPath -HostPath $hostRunLogPath -RepoPath $repoRunLogPath | Out-Null

        $state = Read-JsonFile -Path $hostStatePath
        $statePhase = if ($state) { [string]$state.phase } else { '' }
        $shouldRefreshSummary = ($pollCount -eq 1 -or ($pollCount % 3) -eq 0 -or $statePhase -in @('completed', 'error'))
        if ($shouldRefreshSummary) {
            Copy-FromGuestBestEffort -GuestPath $guestSummaryPath -HostPath $hostSummaryPath -RepoPath $repoSummaryPath | Out-Null
            Copy-FromGuestBestEffort -GuestPath $guestResultsPath -HostPath $hostResultsPath -RepoPath $repoResultsPath | Out-Null
        }

        $summary = Read-JsonFile -Path $hostSummaryPath
        $results = Read-JsonFile -Path $hostResultsPath
        $summaryStatus = if ($summary) { [string]$summary.status } else { '' }
        $currentTrigger = if ($state -and $state.PSObject.Properties.Name -contains 'current_trigger') { [string]$state.current_trigger } else { '' }
        $resultsMarker = if ($results) { 'results' } else { 'no-results' }
        $runLogBytes = if (Test-Path -LiteralPath $hostRunLogPath) { (Get-Item -LiteralPath $hostRunLogPath).Length } else { 0 }
        $signature = '{0}|{1}|{2}|{3}|{4}' -f $summaryStatus, $statePhase, $currentTrigger, $resultsMarker, $runLogBytes
        if ($signature -ne $lastSignature) {
            $lastSignature = $signature
            $lastProgress = Get-Date
        }

        if ($summaryStatus -in $terminalStatuses) {
            if (-not $results) {
                Copy-FromGuestBestEffort -GuestPath $guestResultsPath -HostPath $hostResultsPath -RepoPath $repoResultsPath | Out-Null
                $results = Read-JsonFile -Path $hostResultsPath
            }
            return $summary
        }

        if ($statePhase -in @('completed', 'error')) {
            Copy-FromGuestBestEffort -GuestPath $guestSummaryPath -HostPath $hostSummaryPath -RepoPath $repoSummaryPath | Out-Null
            Copy-FromGuestBestEffort -GuestPath $guestResultsPath -HostPath $hostResultsPath -RepoPath $repoResultsPath | Out-Null
            $summary = Read-JsonFile -Path $hostSummaryPath
            if ($summary) {
                return $summary
            }

            return [ordered]@{
                status = if ($state.result_status) { [string]$state.result_status } elseif ($statePhase -eq 'error') { 'error' } else { 'no-hit' }
                trigger_count = @($pilotTriggers).Count
                trigger_error_count = if ($state.trigger_log) { @($state.trigger_log | Where-Object { $_.status -eq 'error' }).Count } else { 0 }
            }
        }

        if ((Get-Date) -gt $lastProgress.AddSeconds($StaleProgressSeconds)) {
            $logTail = ''
            if (Test-Path -LiteralPath $hostRunLogPath) {
                $logTail = ((Get-Content -LiteralPath $hostRunLogPath -Tail 20) -join ' | ').Trim()
            }
            $detail = "Guest run stalled for $StaleProgressSeconds seconds. summary_status=$summaryStatus state_phase=$statePhase current_trigger=$currentTrigger"
            if (-not [string]::IsNullOrWhiteSpace($logTail)) {
                $detail = "$detail log_tail=$logTail"
            }
            throw $detail
        }

        Start-Sleep -Seconds $PollSeconds
    }

    throw "Guest run timed out after $RunTimeoutSeconds seconds."
}

$phase0 = Get-Content -LiteralPath $phase0Path -Raw | ConvertFrom-Json
$hitQueue = Get-Content -LiteralPath $hitQueuePath -Raw | ConvertFrom-Json
$powerGroup = @($hitQueue.hit_groups | Where-Object { $_.family -eq 'power-control' })
if (@($powerGroup).Count -ne 1) {
    throw 'Could not resolve the power-control hit group from the broad hit queue.'
}

$allowedCandidateIds = @($powerGroup[0].candidate_ids)
$requestedCandidateIds = if (@($CandidateIds).Count -gt 0) {
    @($CandidateIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}
else {
    $defaultPilotCandidateIds
}

$candidates = @(
    foreach ($candidateId in $requestedCandidateIds) {
        if ($allowedCandidateIds -notcontains $candidateId) {
            throw "Candidate id is not part of the power-control runtime family: $candidateId"
        }

        $candidate = @($phase0.candidates | Where-Object { $_.candidate_id -eq $candidateId }) | Select-Object -First 1
        if ($null -eq $candidate) {
            throw "Candidate metadata missing from phase0 manifest: $candidateId"
        }

        [ordered]@{
            candidate_id = [string]$candidate.candidate_id
            family = [string]$candidate.family
            route_bucket = [string]$candidate.route_bucket
            registry_path = [string]$candidate.registry_path
            value_name = [string]$candidate.value_name
            probe_value = 1
        }
    }
)

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

$manifest = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    collection_mode = $CollectionMode
    rollback_pending = ($CollectionMode -eq 'evidence')
    family = 'power-control'
    pattern = 'mega-trigger'
    trigger_profile = 'pilot-safe-v1'
    candidate_count = @($candidates).Count
    candidate_ids = @($candidates | ForEach-Object { $_.candidate_id })
    triggers = @($pilotTriggers)
    candidates = $candidates
}
Write-JsonFile -Path $hostManifestPath -InputObject $manifest
Write-JsonFile -Path $repoManifestPath -InputObject $manifest

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    family = 'power-control'
    pattern = 'mega-trigger'
    snapshot_name = $SnapshotName
    collection_mode = $CollectionMode
    rollback_pending = ($CollectionMode -eq 'evidence')
    trigger_profile = 'pilot-safe-v1'
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    total_candidates = @($candidates).Count
    candidate_ids = @($candidates | ForEach-Object { $_.candidate_id })
    shell_before = $null
    shell_after = $null
    reboot_mode = $null
    preflight_recovery_performed = $false
    preflight_recovery_reasons = @()
    status = 'started'
    exact_hit_candidates = @()
    exact_line_only_candidates = @()
    path_only_candidates = @()
    no_hit_candidates = @()
    trigger_count = @($pilotTriggers).Count
    trigger_error_count = 0
    errors = @()
}

$results = New-PlaceholderResults -Status 'aborted-recovered' -Candidates $candidates

$staleProbes = @()
$currentHealth = $null
try {
    $staleProbes = Get-StaleProbeInfos
    $currentHealth = Get-ShellHealthBestEffort -TimeoutSeconds 30
}
catch {
    $summary.errors += "Preflight health inspection failed: $($_.Exception.Message)"
}

$preflightReasons = @()
if (@($staleProbes).Count -gt 0) {
    $preflightReasons += @($staleProbes | ForEach-Object { "stale-probe:$($_.probe_name)" })
}
if ($null -eq $currentHealth -or -not $currentHealth.shell_healthy) {
    $preflightReasons += 'vm-unhealthy'
}

if ($RecoverOnly -or @($preflightReasons).Count -gt 0) {
    try {
        Restore-HealthySnapshot
        $summary.preflight_recovery_performed = $true
        $summary.preflight_recovery_reasons = @($preflightReasons)
        $summary.shell_after = Assert-ShellHealthy -Phase 'recovery-complete' -TimeoutSeconds 180
        foreach ($probe in $staleProbes) {
            Mark-StaleProbeRecovered -ProbeInfo $probe -RecoveredBy $probeName -Reasons $preflightReasons
        }
    }
    catch {
        $summary.status = 'error'
        $summary.errors += "Recovery failed: $($_.Exception.Message)"
        $results = New-PlaceholderResults -Status 'aborted-recovered' -Candidates $candidates
    }

    if ($RecoverOnly -or $summary.status -eq 'error') {
        $summary.status = if ($summary.status -eq 'error') { 'error' } else { 'aborted-recovered' }
        $results = New-PlaceholderResults -Status 'aborted-recovered' -Candidates $candidates
        $session = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            probe_name = $probeName
            snapshot_name = $SnapshotName
            status = $summary.status
            summary_file = "evidence/files/vm-tooling-staging/$probeName/summary.json"
            results_file = "evidence/files/vm-tooling-staging/$probeName/results.json"
            manifest_file = "evidence/files/vm-tooling-staging/$probeName/manifest.json"
        }
        Write-ProbeArtifacts -Summary $summary -Results $results
        Write-JsonFile -Path $repoSessionPath -InputObject $session
        Write-Output $repoSummaryPath
        if ($summary.status -eq 'error') {
            exit 1
        }
        exit 0
    }
}

try {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Start-VmNonBlocking
    Wait-GuestOperational -Phase 'initial-start' -ShellTimeoutSeconds 1200 -PostShellDelaySeconds 30 -CommandTimeoutSeconds 1200

    Ensure-GuestDirectory -GuestPath $guestScriptRoot
    Ensure-GuestDirectory -GuestPath $guestBatchRoot
    Copy-ToGuest -HostPath $guestPayloadSourcePath -GuestPath $guestPayloadPath
    Copy-ToGuest -HostPath $hostManifestPath -GuestPath $guestManifestPath

    $summary.shell_before = Assert-ShellHealthy -Phase 'before-arm' -TimeoutSeconds 180

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'arm',
        '-ManifestPath', $guestManifestPath,
        '-GuestRoot', $guestBatchRoot,
        '-StatePath', $guestStatePath,
        '-SummaryPath', $guestSummaryPath,
        '-ResultsPath', $guestResultsPath
    )

    Copy-FromGuestBestEffort -GuestPath $guestSummaryPath -HostPath $hostSummaryPath -RepoPath $repoSummaryPath | Out-Null
    Copy-FromGuestBestEffort -GuestPath $guestStatePath -HostPath $hostStatePath -RepoPath $repoStatePath | Out-Null
    $armSummary = Read-JsonFile -Path $hostSummaryPath
    if ($armSummary -and [int]$armSummary.apply_failure_count -gt 0) {
        throw "Guest arm phase failed to apply $($armSummary.apply_failure_count) candidate values."
    }

    try {
        $summary.reboot_mode = Restart-GuestCycle
        Start-Sleep -Seconds $PostBootSettleSeconds
        Wait-GuestOperational -Phase 'before-run' -ShellTimeoutSeconds 1200 -PostShellDelaySeconds 20 -CommandTimeoutSeconds 1200
    }
    catch {
        $summary.status = 'boot-unsafe'
        $summary.errors += $_.Exception.Message
        $results = New-PlaceholderResults -Status 'boot-unsafe' -Candidates $candidates
        Write-ProbeArtifacts -Summary $summary -Results $results
        throw [System.Exception]::new('BOOT_UNSAFE')
    }

    $guestProcess = Start-GuestRunAsync -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'run',
        '-ManifestPath', $guestManifestPath,
        '-GuestRoot', $guestBatchRoot,
        '-StatePath', $guestStatePath,
        '-SummaryPath', $guestSummaryPath,
        '-ResultsPath', $guestResultsPath
    )

    $null = $guestProcess
    $finalSummary = Wait-ForGuestRunCompletion
    if (-not (Copy-FromGuestBestEffort -GuestPath $guestResultsPath -HostPath $hostResultsPath -RepoPath $repoResultsPath)) {
        throw 'Guest results copy failed.'
    }
    Copy-FromGuestBestEffort -GuestPath $guestStatePath -HostPath $hostStatePath -RepoPath $repoStatePath | Out-Null

    $finalResults = Read-JsonFile -Path $hostResultsPath
    if ($null -eq $finalResults) {
        throw 'Guest results could not be parsed.'
    }

    $summary.status = [string]$finalSummary.status
    $summary.trigger_count = if ($finalSummary.trigger_count) { [int]$finalSummary.trigger_count } else { @($pilotTriggers).Count }
    $summary.trigger_error_count = if ($finalSummary.trigger_error_count) { [int]$finalSummary.trigger_error_count } else { 0 }
    $summary.exact_hit_candidates = @($finalResults.candidates | Where-Object { $_.status -eq 'exact-hit' } | ForEach-Object { $_.candidate_id })
    $summary.exact_line_only_candidates = @($finalResults.candidates | Where-Object { $_.status -eq 'exact-line-no-query' } | ForEach-Object { $_.candidate_id })
    $summary.path_only_candidates = @($finalResults.candidates | Where-Object { $_.status -eq 'path-only-hit' } | ForEach-Object { $_.candidate_id })
    $summary.no_hit_candidates = @($finalResults.candidates | Where-Object { $_.status -eq 'no-hit' } | ForEach-Object { $_.candidate_id })
    $results = $finalResults
}
catch {
    if ($_.Exception.Message -ne 'BOOT_UNSAFE') {
        $summary.status = if ($summary.status -eq 'started') { 'error' } else { $summary.status }
        $summary.errors += $_.Exception.Message
    }
}
finally {
    try {
        if ($CollectionMode -eq 'operational' -or $summary.status -eq 'boot-unsafe') {
            Restore-HealthySnapshot
            $summary.shell_after = Assert-ShellHealthy -Phase 'after-restore' -TimeoutSeconds 180
            $summary.rollback_pending = $false
        }
    }
    catch {
        $summary.errors += "Final recovery failed: $($_.Exception.Message)"
        if ($summary.status -ne 'boot-unsafe') {
            $summary.status = 'error'
        }
    }

    $session = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        probe_name = $probeName
        snapshot_name = $SnapshotName
        status = $summary.status
        collection_mode = $CollectionMode
        rollback_pending = [bool]$summary.rollback_pending
        summary_file = "evidence/files/vm-tooling-staging/$probeName/summary.json"
        results_file = "evidence/files/vm-tooling-staging/$probeName/results.json"
        manifest_file = "evidence/files/vm-tooling-staging/$probeName/manifest.json"
    }

    Write-ProbeArtifacts -Summary $summary -Results $results
    Write-JsonFile -Path $repoSessionPath -InputObject $session
}

Write-Output $repoSummaryPath
if ($summary.status -eq 'error') {
    exit 1
}

