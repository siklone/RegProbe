[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon',
    [string]$SnapshotName = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "executive-worker-threads-stress-$stamp"
$recordId = 'system.executive-additional-worker-threads'
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$guestProbeScriptPath = Join-Path $GuestScriptRoot 'registry-policy-probe.ps1'
$hostWrapperPath = Join-Path $hostRoot 'executive-worker-threads-stress-wrapper.ps1'
$guestWrapperPath = Join-Path $GuestScriptRoot 'executive-worker-threads-stress-wrapper.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$probeScript = Join-Path $PSScriptRoot 'registry-policy-probe.ps1'
$prefix = 'executive-worker-threads-stress'
$repoTxt = Join-Path $repoRootOut "$prefix.txt"
$repoHitsCsv = Join-Path $repoRootOut "$prefix.hits.csv"
$repoTasklist = Join-Path $repoRootOut "$prefix-tasklist.txt"
$repoScAll = Join-Path $repoRootOut "$prefix-sc-all.txt"
$repoEventLogs = Join-Path $repoRootOut "$prefix-event-logs.txt"
$repoSystemEvents = Join-Path $repoRootOut "$prefix-system-events.json"
$repoCimServices = Join-Path $repoRootOut "$prefix-cim-services.json"
$repoStressJobs = Join-Path $repoRootOut "$prefix-stress-jobs.json"
$repoPmlPlaceholder = Join-Path $repoRootOut "$prefix.pml.md"
$repoWrapperError = Join-Path $repoRootOut "$prefix-wrapper-error.txt"
$guestPml = Join-Path $guestRoot "$prefix.pml"
$guestCsv = Join-Path $guestRoot "$prefix.csv"
$guestHitsCsv = Join-Path $guestRoot "$prefix.hits.csv"
$guestTxt = Join-Path $guestRoot "$prefix.txt"
$guestTasklist = Join-Path $guestRoot "$prefix-tasklist.txt"
$guestScAll = Join-Path $guestRoot "$prefix-sc-all.txt"
$guestEventLogs = Join-Path $guestRoot "$prefix-event-logs.txt"
$guestSystemEvents = Join-Path $guestRoot "$prefix-system-events.json"
$guestCimServices = Join-Path $guestRoot "$prefix-cim-services.json"
$guestStressJobs = Join-Path $guestRoot "$prefix-stress-jobs.json"
$guestWrapperError = Join-Path $guestRoot "$prefix-wrapper-error.txt"

$hostPml = Join-Path $hostRoot "$prefix.pml"
$hostCsv = Join-Path $hostRoot "$prefix.csv"
$hostHitsCsv = Join-Path $hostRoot "$prefix.hits.csv"
$hostTxt = Join-Path $hostRoot "$prefix.txt"
$hostTasklist = Join-Path $hostRoot "$prefix-tasklist.txt"
$hostScAll = Join-Path $hostRoot "$prefix-sc-all.txt"
$hostEventLogs = Join-Path $hostRoot "$prefix-event-logs.txt"
$hostSystemEvents = Join-Path $hostRoot "$prefix-system-events.json"
$hostCimServices = Join-Path $hostRoot "$prefix-cim-services.json"
$hostStressJobs = Join-Path $hostRoot "$prefix-stress-jobs.json"
$hostWrapperError = Join-Path $hostRoot "$prefix-wrapper-error.txt"

$wrapperTemplate = @'
$ErrorActionPreference = 'Continue'

$probeScript = '__PROBE_SCRIPT__'
$guestRoot = '__GUEST_ROOT__'
$prefix = '__PREFIX__'
$tasklist = '__TASKLIST__'
$scAll = '__SC_ALL__'
$eventLogs = '__EVENT_LOGS__'
$systemEvents = '__SYSTEM_EVENTS__'
$cimServices = '__CIM_SERVICES__'
$stressJobs = '__STRESS_JOBS__'
$wrapperError = '__WRAPPER_ERROR__'

try {
    if (-not (Test-Path $guestRoot)) {
        New-Item -ItemType Directory -Force -Path $guestRoot | Out-Null
    }

    $trigger = @(
        "cmd /c tasklist /svc > `"$tasklist`"",
        "cmd /c sc query type= service state= all > `"$scAll`"",
        "cmd /c wevtutil el > `"$eventLogs`"",
        "Get-CimInstance Win32_Service | Select-Object Name,State,StartMode,ProcessId | ConvertTo-Json -Depth 4 | Set-Content -Path `"$cimServices`" -Encoding UTF8",
        "`$jobs = 1..4 | ForEach-Object { Start-Job -Name ('exec-stress-' + `$_) -ScriptBlock { `$deadline = (Get-Date).AddSeconds(12); while ((Get-Date) -lt `$deadline) { Get-CimInstance Win32_Service | Out-Null; Get-ChildItem 'C:\Windows\System32' -File | Select-Object -First 1200 | Out-Null; Get-WinEvent -LogName 'System' -MaxEvents 80 | Out-Null; Start-Sleep -Milliseconds 400 } } }; Wait-Job -Job `$jobs | Out-Null; `$jobs | Select-Object Id,Name,State,HasMoreData | ConvertTo-Json -Depth 4 | Set-Content -Path `"$stressJobs`" -Encoding UTF8; Remove-Job -Job `$jobs -Force",
        "Get-WinEvent -LogName 'System' -MaxEvents 120 | Select-Object TimeCreated,Id,ProviderName,LevelDisplayName | ConvertTo-Json -Depth 4 | Set-Content -Path `"$systemEvents`" -Encoding UTF8",
        "Start-Sleep -Seconds 5"
    ) -join '; '

    & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -File $probeScript -Mode capture -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Executive' -ValueName 'AdditionalCriticalWorkerThreads' -Prefix $prefix -OutputDirectory $guestRoot -PowerShellCommand $trigger -MatchFragments 'AdditionalDelayedWorkerThreads','UuidSequenceNumber'
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
    Replace('__GUEST_ROOT__', $guestRoot).
    Replace('__PREFIX__', $prefix).
    Replace('__TASKLIST__', $guestTasklist).
    Replace('__SC_ALL__', $guestScAll).
    Replace('__EVENT_LOGS__', $guestEventLogs).
    Replace('__SYSTEM_EVENTS__', $guestSystemEvents).
    Replace('__CIM_SERVICES__', $guestCimServices).
    Replace('__STRESS_JOBS__', $guestStressJobs).
    Replace('__WRAPPER_ERROR__', $guestWrapperError)

$wrapperContent | Set-Content -Path $hostWrapperPath -Encoding UTF8

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

function Copy-FromGuestBestEffort {
    param([string]$GuestPath, [string]$HostPath)

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
        ) | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Sync-ToRepoBestEffort {
    param([string]$HostPath, [string]$RepoPath)

    try {
        if (Test-Path $HostPath) {
            Copy-Item -Path $HostPath -Destination $RepoPath -Force
            return $true
        }
    }
    catch {
    }

    return $false
}

function Get-ShellHealth {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1') `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword
}

function Wait-ShellHealthy {
    param([int]$TimeoutSeconds = 180)

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

Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
Ensure-VmRunning
Wait-GuestReady

$shellBefore = Wait-ShellHealthy

Ensure-GuestDirectory -GuestPath $GuestScriptRoot
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $probeScript, $guestProbeScriptPath
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $hostWrapperPath, $guestWrapperPath
) | Out-Null
Ensure-GuestDirectory -GuestPath $guestRoot

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
    pml = Copy-FromGuestBestEffort -GuestPath $guestPml -HostPath $hostPml
    csv = Copy-FromGuestBestEffort -GuestPath $guestCsv -HostPath $hostCsv
    hits = Copy-FromGuestBestEffort -GuestPath $guestHitsCsv -HostPath $hostHitsCsv
    txt = Copy-FromGuestBestEffort -GuestPath $guestTxt -HostPath $hostTxt
    tasklist = Copy-FromGuestBestEffort -GuestPath $guestTasklist -HostPath $hostTasklist
    sc_all = Copy-FromGuestBestEffort -GuestPath $guestScAll -HostPath $hostScAll
    event_logs = Copy-FromGuestBestEffort -GuestPath $guestEventLogs -HostPath $hostEventLogs
    system_events = Copy-FromGuestBestEffort -GuestPath $guestSystemEvents -HostPath $hostSystemEvents
    cim_services = Copy-FromGuestBestEffort -GuestPath $guestCimServices -HostPath $hostCimServices
    stress_jobs = Copy-FromGuestBestEffort -GuestPath $guestStressJobs -HostPath $hostStressJobs
    wrapper_error = Copy-FromGuestBestEffort -GuestPath $guestWrapperError -HostPath $hostWrapperError
}

$repoCopied = [ordered]@{
    txt = Sync-ToRepoBestEffort -HostPath $hostTxt -RepoPath $repoTxt
    hits = Sync-ToRepoBestEffort -HostPath $hostHitsCsv -RepoPath $repoHitsCsv
    tasklist = Sync-ToRepoBestEffort -HostPath $hostTasklist -RepoPath $repoTasklist
    sc_all = Sync-ToRepoBestEffort -HostPath $hostScAll -RepoPath $repoScAll
    event_logs = Sync-ToRepoBestEffort -HostPath $hostEventLogs -RepoPath $repoEventLogs
    system_events = Sync-ToRepoBestEffort -HostPath $hostSystemEvents -RepoPath $repoSystemEvents
    cim_services = Sync-ToRepoBestEffort -HostPath $hostCimServices -RepoPath $repoCimServices
    stress_jobs = Sync-ToRepoBestEffort -HostPath $hostStressJobs -RepoPath $repoStressJobs
    wrapper_error = Sync-ToRepoBestEffort -HostPath $hostWrapperError -RepoPath $repoWrapperError
}

$shellAfter = Wait-ShellHealthy

$hits = @()
if (Test-Path $hostHitsCsv) {
    $hits = Import-Csv -Path $hostHitsCsv
}

$probeErrorPresent = $false
if (Test-Path $hostTxt) {
    $probeErrorPresent = [bool](Select-String -Path $hostTxt -Pattern '^ERROR=' -Quiet)
}

if ($copied.pml) {
    @(
        '# External Evidence Placeholder',
        '',
        'Title: system.executive-additional-worker-threads stress trigger Procmon lane',
        '',
        'The raw Procmon PML for this lane is not committed here. Use the summary JSON, the filtered hits CSV, and the companion exports in the same folder.'
    ) -join "`n" | Set-Content -Path $repoPmlPlaceholder -Encoding UTF8
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    lane_label = $recordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    trigger = 'tasklist /svc + sc query all + wevtutil el + CIM service export + parallel service/event/file stress'
    registry_path = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive'
    shell_before = $shellBefore
    shell_after = $shellAfter
    copied = $copied
    repo_copied = $repoCopied
    hit_count = @($hits).Count
    hit_processes = @($hits | Select-Object -ExpandProperty 'Process Name' -Unique)
    hit_paths = @($hits | Select-Object -ExpandProperty Path -Unique)
    match_fragments = @('AdditionalCriticalWorkerThreads', 'AdditionalDelayedWorkerThreads', 'UuidSequenceNumber')
    probe_error_present = $probeErrorPresent
    wrapper_error_present = [bool](Test-Path $hostWrapperError)
    status = if (@($hits).Count -gt 0) { 'hits-found' } elseif (Test-Path $hostWrapperError) { 'guest-wrapper-error' } elseif ($probeErrorPresent) { 'guest-probe-error' } elseif ($copied.txt) { 'no-hits' } else { 'copy-incomplete' }
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSummaryPath -Encoding UTF8

Write-Output $repoSummaryPath

