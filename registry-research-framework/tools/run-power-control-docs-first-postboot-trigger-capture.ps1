[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$SnapshotName = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$baselineResolver = Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1'
if (Test-Path $baselineResolver) {
    . $baselineResolver
    if ([string]::IsNullOrWhiteSpace($VmPath)) {
        $VmPath = Resolve-CanonicalVmPath
    }
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        $SnapshotName = Resolve-DefaultVmSnapshotName
    }
}

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx'
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = 'RegProbe-Baseline-Clean-20260329'
}

$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$probeScript = Join-Path $repoRoot 'scripts\vm\registry-policy-probe.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "power-control-docs-first-postboot-trigger-$stamp"
$repoOutputRoot = Join-Path $repoRoot "evidence\files\vm-tooling-staging\$probeName"
$hostWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) $probeName
$guestScriptRoot = 'C:\Tools\Scripts'
$guestBatchRoot = "C:\RegProbe-Diag\$probeName"
$guestProbeScriptPath = Join-Path $guestScriptRoot 'registry-policy-probe.ps1'

$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'

$candidates = @(
    [ordered]@{
        candidate_id = 'power.control.class1-initial-unpark-count'
        value_name = 'Class1InitialUnparkCount'
        trigger_profile = 'processor-load'
    },
    [ordered]@{
        candidate_id = 'power.control.hibernate-enabled-default'
        value_name = 'HibernateEnabledDefault'
        trigger_profile = 'hibernate-query'
    },
    [ordered]@{
        candidate_id = 'power.control.mf-buffering-threshold'
        value_name = 'MfBufferingThreshold'
        trigger_profile = 'media-perf'
    },
    [ordered]@{
        candidate_id = 'power.control.perf-calculate-actual-utilization'
        value_name = 'PerfCalculateActualUtilization'
        trigger_profile = 'processor-load'
    },
    [ordered]@{
        candidate_id = 'power.control.timer-rebase-threshold-on-drips-exit'
        value_name = 'TimerRebaseThresholdOnDripsExit'
        trigger_profile = 'drips-timer'
    }
)

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
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
        if ($RepoPath) {
            Copy-Item -Path $HostPath -Destination $RepoPath -Force
        }
        return $true
    }
    catch {
        return $false
    }
}

function Get-ShellHealthObject {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json
}

function Wait-ShellHealthy {
    param([int]$TimeoutSeconds = 180)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $health = Get-ShellHealthObject
        if ($health.shell_healthy) {
            return $health
        }

        Start-Sleep -Seconds 5
    }

    throw 'Guest reached VMware Tools ready state, but the shell did not become healthy in time.'
}

function Write-Json {
    param([object]$Payload, [string]$HostPath, [string]$RepoPath = '')

    $Payload | ConvertTo-Json -Depth 8 | Set-Content -Path $HostPath -Encoding UTF8
    if ($RepoPath) {
        Copy-Item -Path $HostPath -Destination $RepoPath -Force
    }
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    total_candidates = @($candidates).Count
    exact_hit_candidates = 0
    no_hit_candidates = 0
    error_candidates = 0
    candidate_summary_files = @()
    errors = @()
}

$results = New-Object System.Collections.Generic.List[object]

try {
    foreach ($candidate in $candidates) {
        $candidateLabel = ($candidate.candidate_id -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
        $prefix = "$candidateLabel-postboot-trigger"
        $hostCandidateRoot = Join-Path $hostWorkRoot $candidateLabel
        $repoCandidateRoot = Join-Path $repoOutputRoot $candidateLabel
        $guestCandidateRoot = Join-Path $guestBatchRoot $candidateLabel
        $hostWrapperPath = Join-Path $hostCandidateRoot "$prefix-wrapper.ps1"
        $guestWrapperPath = Join-Path $guestScriptRoot "$prefix-wrapper.ps1"
        $guestPowercfgLog = Join-Path $guestCandidateRoot "$prefix-powercfg.txt"
        $guestTypeperfLog = Join-Path $guestCandidateRoot "$prefix-typeperf.txt"
        $guestWinsatLog = Join-Path $guestCandidateRoot "$prefix-winsat.txt"
        $guestEnergyReport = Join-Path $guestCandidateRoot "$prefix-energy-report.html"
        $guestJobsLog = Join-Path $guestCandidateRoot "$prefix-jobs.json"
        $guestWrapperError = Join-Path $guestCandidateRoot "$prefix-wrapper-error.txt"
        $guestTxt = Join-Path $guestCandidateRoot "$prefix.txt"
        $guestHitsCsv = Join-Path $guestCandidateRoot "$prefix.hits.csv"

        $hostSummaryPath = Join-Path $hostCandidateRoot 'summary.json'
        $repoCandidateSummaryPath = Join-Path $repoCandidateRoot 'summary.json'
        $hostTxt = Join-Path $hostCandidateRoot "$prefix.txt"
        $repoTxt = Join-Path $repoCandidateRoot "$prefix.txt"
        $hostHitsCsv = Join-Path $hostCandidateRoot "$prefix.hits.csv"
        $repoHitsCsv = Join-Path $repoCandidateRoot "$prefix.hits.csv"
        $hostPowercfgLog = Join-Path $hostCandidateRoot "$prefix-powercfg.txt"
        $repoPowercfgLog = Join-Path $repoCandidateRoot "$prefix-powercfg.txt"
        $hostTypeperfLog = Join-Path $hostCandidateRoot "$prefix-typeperf.txt"
        $repoTypeperfLog = Join-Path $repoCandidateRoot "$prefix-typeperf.txt"
        $hostWinsatLog = Join-Path $hostCandidateRoot "$prefix-winsat.txt"
        $repoWinsatLog = Join-Path $repoCandidateRoot "$prefix-winsat.txt"
        $hostJobsLog = Join-Path $hostCandidateRoot "$prefix-jobs.json"
        $repoJobsLog = Join-Path $repoCandidateRoot "$prefix-jobs.json"
        $hostWrapperError = Join-Path $hostCandidateRoot "$prefix-wrapper-error.txt"
        $repoWrapperError = Join-Path $repoCandidateRoot "$prefix-wrapper-error.txt"
        $repoPmlPlaceholder = Join-Path $repoCandidateRoot "$prefix.pml.md"

        New-Item -ItemType Directory -Path $hostCandidateRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $repoCandidateRoot -Force | Out-Null

        $wrapperTemplate = @'
$ErrorActionPreference = 'Continue'

$probeScript = '__PROBE_SCRIPT__'
$guestRoot = '__GUEST_ROOT__'
$prefix = '__PREFIX__'
$triggerProfile = '__TRIGGER_PROFILE__'
$powercfgLog = '__POWERCFG_LOG__'
$typeperfLog = '__TYPEPERF_LOG__'
$winsatLog = '__WINSAT_LOG__'
$energyReport = '__ENERGY_REPORT__'
$jobsLog = '__JOBS_LOG__'
$wrapperError = '__WRAPPER_ERROR__'

try {
    if (-not (Test-Path $guestRoot)) {
        New-Item -ItemType Directory -Force -Path $guestRoot | Out-Null
    }
    if (-not (Test-Path $probeScript)) {
        throw "Probe script was not found: $probeScript"
    }

    $common = @(
        ('cmd /c powercfg /getactivescheme > "{0}"' -f $powercfgLog),
        ('cmd /c powercfg /a >> "{0}" 2>&1' -f $powercfgLog),
        ('cmd /c powercfg /q >> "{0}" 2>&1' -f $powercfgLog),
        ('cmd /c powercfg /qh >> "{0}" 2>&1' -f $powercfgLog),
        ('cmd /c powercfg /energy /duration 5 /output "{0}" >> "{1}" 2>&1' -f $energyReport, $powercfgLog),
        ('$jobs = 1..4 | ForEach-Object { Start-Job -Name (''power-trigger-'' + $_) -ScriptBlock { $deadline = (Get-Date).AddSeconds(12); while ((Get-Date) -lt $deadline) { [Math]::Sqrt((Get-Random -Minimum 1000 -Maximum 50000)) | Out-Null; Start-Sleep -Milliseconds 40 } } }; Wait-Job -Job $jobs | Out-Null; $jobs | Select-Object Id,Name,State,HasMoreData | ConvertTo-Json -Depth 4 | Set-Content -Path ''' + $jobsLog + ''' -Encoding UTF8; Remove-Job -Job $jobs -Force')
    )

    $profileCommands = switch ($triggerProfile) {
        'hibernate-query' {
            @(
                ('cmd /c powercfg /devicequery wake_armed >> "{0}" 2>&1' -f $powercfgLog),
                ('cmd /c powercfg /requests >> "{0}" 2>&1' -f $powercfgLog),
                'Start-Sleep -Seconds 3'
            )
        }
        'media-perf' {
            @(
                ('cmd /c winsat cpuformal > "{0}" 2>&1' -f $winsatLog),
                'Start-Sleep -Seconds 3'
            )
        }
        'drips-timer' {
            @(
                ('cmd /c powercfg /requests >> "{0}" 2>&1' -f $powercfgLog),
                ('cmd /c powercfg /waketimers >> "{0}" 2>&1' -f $powercfgLog),
                ('cmd /c powercfg /lastwake >> "{0}" 2>&1' -f $powercfgLog),
                'Start-Sleep -Seconds 3'
            )
        }
        default {
            @(
                ('cmd /c winsat cpuformal > "{0}" 2>&1' -f $winsatLog),
                'Start-Sleep -Seconds 3'
            )
        }
    }

    $trigger = ($common + $profileCommands) -join '; '

    $probeOutput = & $probeScript -Mode capture -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' -ValueName '__VALUE_NAME__' -Prefix $prefix -OutputDirectory $guestRoot -PowerShellCommand $trigger -ProcessNames 'System','smss.exe','svchost.exe','powercfg.exe','winsat.exe','typeperf.exe' 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        @(
            ('PROBE_EXIT=' + $LASTEXITCODE),
            ('PROBE_OUTPUT=' + $probeOutput.Trim())
        ) | Set-Content -Path $wrapperError -Encoding UTF8
    }
}
catch {
    @(
        ('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message),
        ('AT=' + $_.InvocationInfo.PositionMessage)
    ) | Set-Content -Path $wrapperError -Encoding UTF8
}

exit 0
'@

        $wrapperContent = $wrapperTemplate.
            Replace('__PROBE_SCRIPT__', $guestProbeScriptPath).
            Replace('__GUEST_ROOT__', $guestCandidateRoot).
            Replace('__PREFIX__', $prefix).
            Replace('__TRIGGER_PROFILE__', $candidate.trigger_profile).
            Replace('__POWERCFG_LOG__', $guestPowercfgLog).
            Replace('__TYPEPERF_LOG__', $guestTypeperfLog).
            Replace('__WINSAT_LOG__', $guestWinsatLog).
            Replace('__ENERGY_REPORT__', $guestEnergyReport).
            Replace('__JOBS_LOG__', $guestJobsLog).
            Replace('__WRAPPER_ERROR__', $guestWrapperError).
            Replace('__VALUE_NAME__', $candidate.value_name)
        $wrapperContent | Set-Content -Path $hostWrapperPath -Encoding UTF8

        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
        Wait-GuestReady
        $shellBefore = Wait-ShellHealthy

        Ensure-GuestDirectory -GuestPath $guestScriptRoot
        Ensure-GuestDirectory -GuestPath $guestCandidateRoot
        Copy-ToGuest -HostPath $probeScript -GuestPath $guestProbeScriptPath
        Copy-ToGuest -HostPath $hostWrapperPath -GuestPath $guestWrapperPath
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'runProgramInGuest', $VmPath,
            'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
            '-NoProfile',
            '-ExecutionPolicy',
            'Bypass',
            '-File',
            $guestWrapperPath
        ) | Out-Null

        $copied = [ordered]@{
            summary_txt = Copy-FromGuestBestEffort -GuestPath $guestTxt -HostPath $hostTxt -RepoPath $repoTxt
            hits_csv = Copy-FromGuestBestEffort -GuestPath $guestHitsCsv -HostPath $hostHitsCsv -RepoPath $repoHitsCsv
            powercfg_log = Copy-FromGuestBestEffort -GuestPath $guestPowercfgLog -HostPath $hostPowercfgLog -RepoPath $repoPowercfgLog
            typeperf_log = Copy-FromGuestBestEffort -GuestPath $guestTypeperfLog -HostPath $hostTypeperfLog -RepoPath $repoTypeperfLog
            winsat_log = Copy-FromGuestBestEffort -GuestPath $guestWinsatLog -HostPath $hostWinsatLog -RepoPath $repoWinsatLog
            jobs_log = Copy-FromGuestBestEffort -GuestPath $guestJobsLog -HostPath $hostJobsLog -RepoPath $repoJobsLog
            wrapper_error = Copy-FromGuestBestEffort -GuestPath $guestWrapperError -HostPath $hostWrapperError -RepoPath $repoWrapperError
        }

        $shellAfter = Wait-ShellHealthy
        $hits = @()
        if (Test-Path $hostHitsCsv) {
            $hits = @(Import-Csv -Path $hostHitsCsv)
        }

        $exactReadHits = @($hits | Where-Object {
            $_.Path -like "*\$($candidate.value_name)" -and $_.Operation -like 'RegQuery*'
        })

        $candidateSummary = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            candidate_id = $candidate.candidate_id
            value_name = $candidate.value_name
            trigger_profile = $candidate.trigger_profile
            snapshot_name = $SnapshotName
            shell_before = $shellBefore
            shell_after = $shellAfter
            copied = $copied
            exact_hit_count = @($exactReadHits).Count
            exact_runtime_read = (@($exactReadHits).Count -gt 0)
            hit_processes = @($exactReadHits | Select-Object -ExpandProperty 'Process Name' -Unique)
            hit_operations = @($exactReadHits | Select-Object -ExpandProperty Operation -Unique)
            status = if (@($exactReadHits).Count -gt 0) { 'exact-hit' } elseif ($copied.wrapper_error) { 'wrapper-error' } else { 'no-hit' }
            artifacts = [ordered]@{
                summary_txt = "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix.txt"
                hits_csv = if ($copied.hits_csv) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix.hits.csv" } else { $null }
                powercfg_log = if ($copied.powercfg_log) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix-powercfg.txt" } else { $null }
                typeperf_log = if ($copied.typeperf_log) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix-typeperf.txt" } else { $null }
                winsat_log = if ($copied.winsat_log) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix-winsat.txt" } else { $null }
                jobs_log = if ($copied.jobs_log) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix-jobs.json" } else { $null }
                wrapper_error = if ($copied.wrapper_error) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix-wrapper-error.txt" } else { $null }
            }
        }

        @(
            "Guest-processed Procmon capture executed for $($candidate.value_name).",
            "Raw PML/CSV stayed in the guest; only summary and filtered hit artifacts were copied back.",
            "Trigger profile: $($candidate.trigger_profile)"
        ) | Set-Content -Path $repoPmlPlaceholder -Encoding UTF8

        Write-Json -Payload $candidateSummary -HostPath $hostSummaryPath -RepoPath $repoCandidateSummaryPath
        $summary.candidate_summary_files += "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
        $results.Add([pscustomobject]$candidateSummary) | Out-Null
    }

    $summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
    $summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    $summary.error_candidates = @($results | Where-Object { $_.status -eq 'wrapper-error' }).Count
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.error_candidates -gt 0) { 'error' } else { 'no-hit' }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}

Write-Json -Payload $summary -HostPath (Join-Path $hostWorkRoot 'summary.json') -RepoPath $repoSummaryPath
$resultArray = @($results.ToArray())
Write-Json -Payload $resultArray -HostPath (Join-Path $hostWorkRoot 'results.json') -RepoPath $repoResultsPath

