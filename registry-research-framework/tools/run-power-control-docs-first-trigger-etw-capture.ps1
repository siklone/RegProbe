[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SnapshotName = '',
    [string[]]$CandidateIds = @()
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
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "power-control-docs-first-trigger-etw-$stamp"
$repoOutputRoot = Join-Path $repoRoot "evidence\files\vm-tooling-staging\$probeName"
$hostWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) $probeName
$guestScriptRoot = 'C:\Tools\Scripts'
$guestBatchRoot = "C:\RegProbe-Diag\$probeName"
$guestPayloadPath = Join-Path $guestScriptRoot 'power-control-docs-first-trigger-etw.ps1'

$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'

$candidates = @(
    [ordered]@{
        candidate_id = 'power.control.class1-initial-unpark-count'
        value_name = 'Class1InitialUnparkCount'
        trigger_profile = 'processor-plan-toggle'
    },
    [ordered]@{
        candidate_id = 'power.control.hibernate-enabled-default'
        value_name = 'HibernateEnabledDefault'
        trigger_profile = 'hibernate-toggle'
    },
    [ordered]@{
        candidate_id = 'power.control.mf-buffering-threshold'
        value_name = 'MfBufferingThreshold'
        trigger_profile = 'disk-io-burst'
    },
    [ordered]@{
        candidate_id = 'power.control.perf-calculate-actual-utilization'
        value_name = 'PerfCalculateActualUtilization'
        trigger_profile = 'perf-plan-stress'
    },
    [ordered]@{
        candidate_id = 'power.control.timer-rebase-threshold-on-drips-exit'
        value_name = 'TimerRebaseThresholdOnDripsExit'
        trigger_profile = 'drips-diagnostics'
    }
)

if (@($CandidateIds).Count -gt 0) {
    $wanted = @($CandidateIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $candidates = @($candidates | Where-Object { $wanted -contains $_.candidate_id })
    if (@($candidates).Count -eq 0) {
        throw "No power-control trigger ETW candidates matched the requested ids: $($wanted -join ', ')"
    }
}

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

$guestPayload = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,
    [Parameter(Mandatory = $true)]
    [string]$ValueName,
    [Parameter(Mandatory = $true)]
    [string]$TriggerProfile,
    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,
    [Parameter(Mandatory = $true)]
    [string]$FilteredPath,
    [Parameter(Mandatory = $true)]
    [string]$FilteredCsvPath,
    [Parameter(Mandatory = $true)]
    [string]$TriggerLogPath,
    [Parameter(Mandatory = $true)]
    [string]$ErrorPath
)

$ErrorActionPreference = 'Stop'

function Read-TextOrEmpty {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return ''
    }

    $raw = Get-Content -Path $Path -Raw -ErrorAction SilentlyContinue
    if ($null -eq $raw) {
        return ''
    }

    return ([string]$raw).Trim()
}

function Invoke-CmdCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $stdout = [System.IO.Path]::GetTempFileName()
    $stderr = [System.IO.Path]::GetTempFileName()
    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $Arguments -Wait -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        return [ordered]@{
            file_path = $FilePath
            arguments = @($Arguments)
            exit_code = $proc.ExitCode
            stdout = Read-TextOrEmpty -Path $stdout
            stderr = Read-TextOrEmpty -Path $stderr
        }
    }
    finally {
        Remove-Item -Path $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-PowerShellCapture {
    param([Parameter(Mandatory = $true)][string]$ScriptText)
    return Invoke-CmdCapture -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -Arguments @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $ScriptText)
}

function Invoke-TriggerProfile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Profile,
        [Parameter(Mandatory = $true)]
        [string]$LogPath
    )

    $commands = switch ($Profile) {
        'processor-plan-toggle' {
            @(
                'powercfg /getactivescheme',
                'powercfg /setactive SCHEME_MIN',
                'timeout /t 5 /nobreak',
                'powercfg /setactive SCHEME_BALANCED',
                'timeout /t 5 /nobreak'
            )
        }
        'hibernate-toggle' {
            @(
                'powercfg /hibernate on',
                'timeout /t 3 /nobreak',
                'powercfg /hibernate off',
                'timeout /t 3 /nobreak'
            )
        }
        'disk-io-burst' {
            @(
                'if not exist C:\RegProbe-Diag mkdir C:\RegProbe-Diag',
                'fsutil file createnew C:\RegProbe-Diag\mf-buffering-threshold.bin 67108864',
                'copy /y C:\RegProbe-Diag\mf-buffering-threshold.bin C:\RegProbe-Diag\mf-buffering-threshold-copy.bin',
                'timeout /t 2 /nobreak',
                'del /f /q C:\RegProbe-Diag\mf-buffering-threshold-copy.bin'
            )
        }
        'perf-plan-stress' {
            @(
                'powercfg /getactivescheme',
                'powercfg /setactive SCHEME_MIN',
                'timeout /t 3 /nobreak',
                'winsat cpuformal',
                'timeout /t 3 /nobreak',
                'powercfg /setactive SCHEME_BALANCED',
                'timeout /t 3 /nobreak'
            )
        }
        'drips-diagnostics' {
            @(
                'powercfg /requests',
                'powercfg /waketimers',
                'powercfg /lastwake',
                'powercfg /sleepstudy'
            )
        }
        default {
            throw "Unknown trigger profile: $Profile"
        }
    }

    $logLines = New-Object System.Collections.Generic.List[string]
    foreach ($command in $commands) {
        $result = Invoke-CmdCapture -FilePath 'C:\Windows\System32\cmd.exe' -Arguments @('/c', $command)
        $logLines.Add("COMMAND=$command")
        $logLines.Add("EXIT_CODE=$($result.exit_code)")
        if (-not [string]::IsNullOrWhiteSpace($result.stdout)) {
            $logLines.Add('STDOUT<<')
            foreach ($line in ($result.stdout -split "`r?`n")) { $logLines.Add($line) }
            $logLines.Add('>>STDOUT')
        }
        if (-not [string]::IsNullOrWhiteSpace($result.stderr)) {
            $logLines.Add('STDERR<<')
            foreach ($line in ($result.stderr -split "`r?`n")) { $logLines.Add($line) }
            $logLines.Add('>>STDERR')
        }
    }
    $logLines | Set-Content -Path $LogPath -Encoding UTF8
}

function Stop-ExistingSession {
    param([string]$Name)
    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $Name, '-ets') | Out-Null
    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $Name) | Out-Null
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

$sessionName = ('RegProbeRegistry_' + ($ValueName -replace '[^A-Za-z0-9]', ''))
$etlPath = Join-Path $GuestRoot ($ValueName + '.etl')
$csvPath = Join-Path $GuestRoot ($ValueName + '.csv')
$registryPathFragment = 'SYSTEM\\CurrentControlSet\\Control\\Power'
$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    session_name = $sessionName
    value_name = $ValueName
    trigger_profile = $TriggerProfile
    guest_root = $GuestRoot
    etl_path = $etlPath
    csv_path = $csvPath
    status = 'started'
    commands = [ordered]@{}
    exact_query_hits = 0
    exact_runtime_read = $false
    path_line_count = 0
    exact_line_count = 0
    exact_lines = @()
    path_lines = @()
    errors = @()
}

try {
    foreach ($path in @($etlPath, $csvPath, $FilteredPath, $FilteredCsvPath, $SummaryPath, $TriggerLogPath, $ErrorPath)) {
        if (Test-Path $path) { Remove-Item -Path $path -Force -ErrorAction SilentlyContinue }
    }

    Stop-ExistingSession -Name $sessionName

    $create = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('create', 'trace', $sessionName, '-o', $etlPath, '-p', 'Microsoft-Windows-Kernel-Registry', '0xFFFF', '5', '-ets')
    $summary.commands['create_trace'] = $create
    if ($create.exit_code -ne 0) {
        throw "logman create trace failed: $($create.stderr)"
    }

    Start-Sleep -Seconds 2
    Invoke-TriggerProfile -Profile $TriggerProfile -LogPath $TriggerLogPath
    Start-Sleep -Seconds 2

    $stop = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $sessionName, '-ets')
    $summary.commands['stop_trace'] = $stop
    if ($stop.exit_code -ne 0) {
        throw "logman stop failed: $($stop.stderr)"
    }

    $delete = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $sessionName)
    $summary.commands['delete_trace'] = $delete

    $summary['etl_exists'] = [bool](Test-Path $etlPath)
    if ($summary.etl_exists) {
        $summary['etl_length'] = (Get-Item -Path $etlPath).Length
    }

    $tracerpt = Invoke-CmdCapture -FilePath 'C:\Windows\System32\tracerpt.exe' -Arguments @($etlPath, '-o', $csvPath, '-of', 'CSV')
    $summary.commands['tracerpt'] = $tracerpt
    if ($tracerpt.exit_code -ne 0) {
        throw "tracerpt failed: $($tracerpt.stderr)"
    }

    $summary['csv_exists'] = [bool](Test-Path $csvPath)
    if ($summary.csv_exists) {
        $allLines = @(Get-Content -Path $csvPath)
        $pathPattern = [regex]::Escape($registryPathFragment)
        $valuePattern = [regex]::Escape($ValueName)
        $pathLines = @($allLines | Where-Object { $_ -match $pathPattern })
        $exactLines = @($allLines | Where-Object { $_ -match $valuePattern })
        $exactQueryLines = @($exactLines | Where-Object { $_ -match 'RegQueryValue|QueryValue' })
        $filteredLines = @($exactLines + $pathLines | Select-Object -Unique)
        $filteredLines | Set-Content -Path $FilteredPath -Encoding UTF8
        if ($exactLines.Count -gt 0) {
            $exactLines | Set-Content -Path $FilteredCsvPath -Encoding UTF8
        }
        $summary['path_line_count'] = $pathLines.Count
        $summary['exact_line_count'] = $exactLines.Count
        $summary['exact_query_hits'] = $exactQueryLines.Count
        $summary['exact_runtime_read'] = ($exactQueryLines.Count -gt 0)
        $summary['exact_lines'] = @($exactLines | Select-Object -First 10)
        $summary['path_lines'] = @($pathLines | Select-Object -First 10)
        $summary['status'] = if ($summary.exact_runtime_read) { 'exact-hit' } elseif ($exactLines.Count -gt 0) { 'exact-line-no-query' } elseif ($pathLines.Count -gt 0) { 'path-only-hit' } else { 'no-hit' }
    }
    else {
        $summary['status'] = 'no-csv'
    }
}
catch {
    $summary['status'] = 'error'
    $summary['errors'] = @($summary.errors) + $_.Exception.Message
    @(
        ('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message),
        ('AT=' + $_.InvocationInfo.PositionMessage)
    ) | Set-Content -Path $ErrorPath -Encoding UTF8
}
finally {
    try { Stop-ExistingSession -Name $sessionName } catch {}
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $SummaryPath -Encoding UTF8
}
'@

$hostPayloadPath = Join-Path $hostWorkRoot 'power-control-docs-first-trigger-etw.ps1'
Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

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
            $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
            if ($running -notmatch [regex]::Escape($VmPath)) {
                Start-Sleep -Seconds 3
                continue
            }

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

function Get-ShellHealthBestEffort {
    param([int]$TimeoutSeconds = 180)

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

    if ($null -eq $last) {
        return [pscustomobject]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            vm_path = $VmPath
            vm_running = $false
            tools_state = 'unknown'
            process_query_error = 'shell-health-timeout'
            shell_healthy = $false
            checks = [ordered]@{
                explorer = $false
                sihost = $false
                shellhost = $false
                ctfmon = $false
                app = $false
            }
        }
    }

    return $last
}

function Wait-GuestCommandReady {
    param([int]$TimeoutSeconds = 180)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'runProgramInGuest', $VmPath,
                'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
                '-NoProfile',
                '-ExecutionPolicy',
                'Bypass',
                '-Command',
                'exit 0'
            ) | Out-Null
            return
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw 'Guest command execution did not become ready in time.'
}

function Write-Json {
    param([object]$Payload, [string]$HostPath, [string]$RepoPath = '')

    $json = ConvertTo-Json -InputObject $Payload -Depth 8
    Set-Content -Path $HostPath -Value $json -Encoding UTF8
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
    exact_line_only_candidates = 0
    path_only_candidates = 0
    no_hit_candidates = 0
    error_candidates = 0
    candidate_summary_files = @()
    errors = @()
}

$results = New-Object System.Collections.Generic.List[object]

try {
    foreach ($candidate in $candidates) {
        $candidateLabel = ($candidate.candidate_id -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
        $prefix = "$candidateLabel-trigger-etw"
        $hostCandidateRoot = Join-Path $hostWorkRoot $candidateLabel
        $repoCandidateRoot = Join-Path $repoOutputRoot $candidateLabel
        $guestCandidateRoot = Join-Path $guestBatchRoot $candidateLabel

        $guestSummaryPath = Join-Path $guestCandidateRoot 'summary.json'
        $guestFilteredPath = Join-Path $guestCandidateRoot "$prefix.txt"
        $guestFilteredCsvPath = Join-Path $guestCandidateRoot "$prefix.filtered.csv"
        $guestTriggerLogPath = Join-Path $guestCandidateRoot "$prefix-trigger.log"
        $guestErrorPath = Join-Path $guestCandidateRoot "$prefix-error.txt"

        $hostSummaryPath = Join-Path $hostCandidateRoot 'summary.json'
        $repoCandidateSummaryPath = Join-Path $repoCandidateRoot 'summary.json'
        $hostFilteredPath = Join-Path $hostCandidateRoot "$prefix.txt"
        $repoFilteredPath = Join-Path $repoCandidateRoot "$prefix.txt"
        $hostFilteredCsvPath = Join-Path $hostCandidateRoot "$prefix.filtered.csv"
        $repoFilteredCsvPath = Join-Path $repoCandidateRoot "$prefix.filtered.csv"
        $hostTriggerLogPath = Join-Path $hostCandidateRoot "$prefix-trigger.log"
        $repoTriggerLogPath = Join-Path $repoCandidateRoot "$prefix-trigger.log"
        $hostErrorPath = Join-Path $hostCandidateRoot "$prefix-error.txt"
        $repoErrorPath = Join-Path $repoCandidateRoot "$prefix-error.txt"
        $repoEtlPlaceholder = Join-Path $repoCandidateRoot "$prefix.etl.md"

        New-Item -ItemType Directory -Path $hostCandidateRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $repoCandidateRoot -Force | Out-Null

        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
        Wait-GuestReady
        Wait-GuestCommandReady
        $shellBefore = Get-ShellHealthBestEffort

        Ensure-GuestDirectory -GuestPath $guestScriptRoot
        Ensure-GuestDirectory -GuestPath $guestCandidateRoot
        Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

        $guestInvokeError = $null
        try {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'runProgramInGuest', $VmPath,
                'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
                '-NoProfile',
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                $guestPayloadPath,
                '-GuestRoot',
                $guestCandidateRoot,
                '-ValueName',
                $candidate.value_name,
                '-TriggerProfile',
                $candidate.trigger_profile,
                '-SummaryPath',
                $guestSummaryPath,
                '-FilteredPath',
                $guestFilteredPath,
                '-FilteredCsvPath',
                $guestFilteredCsvPath,
                '-TriggerLogPath',
                $guestTriggerLogPath,
                '-ErrorPath',
                $guestErrorPath
            ) | Out-Null
        }
        catch {
            $guestInvokeError = $_.Exception.Message
        }

        $copied = [ordered]@{
            summary = Copy-FromGuestBestEffort -GuestPath $guestSummaryPath -HostPath $hostSummaryPath -RepoPath $repoCandidateSummaryPath
            filtered = Copy-FromGuestBestEffort -GuestPath $guestFilteredPath -HostPath $hostFilteredPath -RepoPath $repoFilteredPath
            filtered_csv = Copy-FromGuestBestEffort -GuestPath $guestFilteredCsvPath -HostPath $hostFilteredCsvPath -RepoPath $repoFilteredCsvPath
            trigger_log = Copy-FromGuestBestEffort -GuestPath $guestTriggerLogPath -HostPath $hostTriggerLogPath -RepoPath $repoTriggerLogPath
            error = Copy-FromGuestBestEffort -GuestPath $guestErrorPath -HostPath $hostErrorPath -RepoPath $repoErrorPath
        }

        $shellAfter = Get-ShellHealthBestEffort
        $candidateSummary = if ($copied.summary) {
            Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json
        }
        else {
            [pscustomobject]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                value_name = $candidate.value_name
                trigger_profile = $candidate.trigger_profile
                status = 'copy-incomplete'
                exact_runtime_read = $false
                exact_query_hits = 0
                errors = @('Failed to copy guest ETW summary back to the host.')
            }
        }

        $candidateResult = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            candidate_id = $candidate.candidate_id
            value_name = $candidate.value_name
            trigger_profile = $candidate.trigger_profile
            snapshot_name = $SnapshotName
            shell_before = $shellBefore
            shell_after = $shellAfter
            guest_invoke_error = $guestInvokeError
            copied = $copied
            status = $candidateSummary.status
            exact_query_hits = $candidateSummary.exact_query_hits
            exact_runtime_read = [bool]$candidateSummary.exact_runtime_read
            guest_summary = $candidateSummary
            artifacts = [ordered]@{
                summary = "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
                filtered = if ($copied.filtered) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix.txt" } else { $null }
                filtered_csv = if ($copied.filtered_csv) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix.filtered.csv" } else { $null }
                trigger_log = if ($copied.trigger_log) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix-trigger.log" } else { $null }
                error = if ($copied.error) { "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/$prefix-error.txt" } else { $null }
            }
        }

        @(
            '# External Evidence Placeholder',
            '',
            "Title: $($candidate.value_name) trigger ETW trace",
            '',
            'The raw ETL and full tracerpt CSV stay in the guest/off-repo.',
            'This folder keeps the guest-processed summary plus filtered ETW lines and trigger logs.'
        ) | Set-Content -Path $repoEtlPlaceholder -Encoding UTF8

        Write-Json -Payload $candidateResult -HostPath $hostSummaryPath -RepoPath $repoCandidateSummaryPath
        $summary.candidate_summary_files += "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
        $results.Add([pscustomobject]$candidateResult) | Out-Null
    }

    $summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
    $summary.exact_line_only_candidates = @($results | Where-Object { $_.status -eq 'exact-line-no-query' }).Count
    $summary.path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
    $summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    $summary.error_candidates = @($results | Where-Object { $_.status -in @('error', 'copy-incomplete') }).Count
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.exact_line_only_candidates -gt 0) { 'exact-line-no-query' } elseif ($summary.path_only_candidates -gt 0) { 'path-only-hit' } elseif ($summary.error_candidates -gt 0) { 'error' } else { 'no-hit' }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}

Write-Json -Payload $summary -HostPath (Join-Path $hostWorkRoot 'summary.json') -RepoPath $repoSummaryPath
$resultArray = @($results.ToArray())
Write-Json -Payload $resultArray -HostPath (Join-Path $hostWorkRoot 'results.json') -RepoPath $repoResultsPath
Write-Output $repoSummaryPath
