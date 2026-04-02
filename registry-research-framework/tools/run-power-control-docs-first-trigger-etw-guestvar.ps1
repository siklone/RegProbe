[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
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
$probeName = "power-control-docs-first-trigger-etw-guestvar-$stamp"
$repoOutputRoot = Join-Path $repoRoot "evidence\files\vm-tooling-staging\$probeName"
$hostWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) $probeName
$guestScriptRoot = 'C:\Tools\Scripts'
$guestBatchRoot = "C:\RegProbe-Diag\$probeName"

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
    [string]$ResultVariableName
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
            exit_code = $proc.ExitCode
            stdout = Read-TextOrEmpty -Path $stdout
            stderr = Read-TextOrEmpty -Path $stderr
        }
    }
    finally {
        Remove-Item -Path $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

function Stop-ExistingSession {
    param([string]$Name)

    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $Name, '-ets') | Out-Null
    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $Name) | Out-Null
}

function Set-GuestInfoValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $vmtoolsd = 'C:\Program Files\VMware\VMware Tools\vmtoolsd.exe'
    if (-not (Test-Path $vmtoolsd)) {
        throw 'vmtoolsd.exe was not found in the guest.'
    }

    $result = Invoke-CmdCapture -FilePath $vmtoolsd -Arguments @('--cmd', "info-set guestinfo.$Name $Value")
    if ($result.exit_code -ne 0) {
        throw "vmtoolsd info-set failed: $($result.stderr)"
    }
}

function Invoke-TriggerProfile {
    param([string]$Profile)

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

    $failures = @()
    foreach ($command in $commands) {
        $result = Invoke-CmdCapture -FilePath 'C:\Windows\System32\cmd.exe' -Arguments @('/c', $command)
        if ($result.exit_code -ne 0 -or -not [string]::IsNullOrWhiteSpace($result.stderr)) {
            $failures += [ordered]@{
                command = $command
                exit_code = $result.exit_code
                stdout = $result.stdout
                stderr = $result.stderr
            }
        }
    }

    return @($failures)
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null
Set-Content -LiteralPath (Join-Path $GuestRoot 'payload-started.txt') -Value ([DateTime]::UtcNow.ToString('o')) -Encoding ASCII

$sessionName = ('RegProbeRegistry_' + ($ValueName -replace '[^A-Za-z0-9]', ''))
$etlPath = Join-Path $GuestRoot ($ValueName + '.etl')
$csvPath = Join-Path $GuestRoot ($ValueName + '.csv')
$summaryPath = Join-Path $GuestRoot 'summary.json'
$registryPathFragment = 'SYSTEM\\CurrentControlSet\\Control\\Power'

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    value_name = $ValueName
    trigger_profile = $TriggerProfile
    status = 'started'
    etl_exists = $false
    csv_exists = $false
    etl_length = 0
    csv_line_count = 0
    exact_runtime_read = $false
    exact_query_hits = 0
    exact_line_count = 0
    path_line_count = 0
    exact_lines = @()
    path_lines = @()
    command_exit_codes = [ordered]@{}
    trigger_failures = @()
    errors = @()
}

try {
    foreach ($path in @($etlPath, $csvPath, $summaryPath)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
        }
    }

    Stop-ExistingSession -Name $sessionName

    $create = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('create', 'trace', $sessionName, '-o', $etlPath, '-p', 'Microsoft-Windows-Kernel-Registry', '0xFFFF', '5', '-ets')
    $summary.command_exit_codes['create_trace'] = $create.exit_code
    if ($create.exit_code -ne 0) {
        throw "logman create trace failed: $($create.stderr)"
    }

    Start-Sleep -Seconds 2
    $summary.trigger_failures = Invoke-TriggerProfile -Profile $TriggerProfile
    Start-Sleep -Seconds 2

    $stop = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $sessionName, '-ets')
    $summary.command_exit_codes['stop_trace'] = $stop.exit_code
    if ($stop.exit_code -ne 0) {
        throw "logman stop failed: $($stop.stderr)"
    }

    $delete = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $sessionName)
    $summary.command_exit_codes['delete_trace'] = $delete.exit_code

    $summary.etl_exists = [bool](Test-Path $etlPath)
    if ($summary.etl_exists) {
        $summary.etl_length = (Get-Item -Path $etlPath).Length
    }

    $tracerpt = Invoke-CmdCapture -FilePath 'C:\Windows\System32\tracerpt.exe' -Arguments @($etlPath, '-o', $csvPath, '-of', 'CSV')
    $summary.command_exit_codes['tracerpt'] = $tracerpt.exit_code
    if ($tracerpt.exit_code -ne 0) {
        throw "tracerpt failed: $($tracerpt.stderr)"
    }

    $summary.csv_exists = [bool](Test-Path $csvPath)
    if ($summary.csv_exists) {
        $pathPattern = [regex]::Escape($registryPathFragment)
        $valuePattern = [regex]::Escape($ValueName)
        $pathLines = New-Object 'System.Collections.Generic.List[string]'
        $exactLines = New-Object 'System.Collections.Generic.List[string]'
        $csvLineCount = 0
        $pathLineCount = 0
        $exactLineCount = 0
        $exactQueryHits = 0
        $reader = [System.IO.File]::OpenText($csvPath)
        try {
            while (($line = $reader.ReadLine()) -ne $null) {
                $csvLineCount++
                if ($line -match $pathPattern) {
                    $pathLineCount++
                    if ($pathLines.Count -lt 3) {
                        $pathLines.Add($line)
                    }
                }

                if ($line -match $valuePattern) {
                    $exactLineCount++
                    if ($exactLines.Count -lt 3) {
                        $exactLines.Add($line)
                    }
                    if ($line -match 'RegQueryValue|QueryValue') {
                        $exactQueryHits++
                    }
                }
            }
        }
        finally {
            $reader.Close()
        }

        $summary.csv_line_count = $csvLineCount
        $summary.path_line_count = $pathLineCount
        $summary.exact_line_count = $exactLineCount
        $summary.exact_query_hits = $exactQueryHits
        $summary.exact_runtime_read = ($summary.exact_query_hits -gt 0)
        $summary.exact_lines = @($exactLines)
        $summary.path_lines = @($pathLines)
        $summary.status = if ($summary.exact_runtime_read) { 'exact-hit' } elseif ($summary.exact_line_count -gt 0) { 'exact-line-no-query' } elseif ($summary.path_line_count -gt 0) { 'path-only-hit' } else { 'no-hit' }
    }
    else {
        $summary.status = 'no-csv'
    }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}
finally {
    try { Stop-ExistingSession -Name $sessionName } catch {}
    try {
        Set-Content -LiteralPath (Join-Path $GuestRoot 'payload-finally.txt') -Value ([DateTime]::UtcNow.ToString('o')) -Encoding ASCII
    }
    catch {
    }
    try {
        New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null
        $summaryJson = $summary | ConvertTo-Json -Depth 8 -Compress
        [System.IO.File]::WriteAllText($summaryPath, $summaryJson, [System.Text.Encoding]::UTF8)
        Set-Content -LiteralPath (Join-Path $GuestRoot 'payload-summary-written.txt') -Value ([DateTime]::UtcNow.ToString('o')) -Encoding ASCII
    }
    catch {
        try {
            $summary.errors = @($summary.errors) + ("summary-write-failed: " + $_.Exception.Message)
            Set-Content -LiteralPath (Join-Path $GuestRoot 'summary-write-error.txt') -Value $_.Exception.Message -Encoding UTF8
        }
        catch {
        }
    }

    $compactSummary = [ordered]@{
        generated_utc = $summary.generated_utc
        value_name = $summary.value_name
        trigger_profile = $summary.trigger_profile
        status = $summary.status
        etl_exists = $summary.etl_exists
        csv_exists = $summary.csv_exists
        etl_length = $summary.etl_length
        csv_line_count = $summary.csv_line_count
        exact_runtime_read = $summary.exact_runtime_read
        exact_query_hits = $summary.exact_query_hits
        exact_line_count = $summary.exact_line_count
        path_line_count = $summary.path_line_count
        trigger_failure_count = @($summary.trigger_failures).Count
        trigger_failure_commands = @($summary.trigger_failures | ForEach-Object { $_.command } | Select-Object -First 3)
        errors = @($summary.errors | Select-Object -First 3)
    }

    try {
        $summaryEncoded = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(($compactSummary | ConvertTo-Json -Depth 6 -Compress)))
        Set-GuestInfoValue -Name $ResultVariableName -Value $summaryEncoded
    }
    catch {
        try {
            $fallbackSummary = [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                value_name = $ValueName
                trigger_profile = $TriggerProfile
                status = 'guestvar-write-failed'
                exact_runtime_read = $false
                exact_query_hits = 0
                errors = @($_.Exception.Message | Select-Object -First 1)
            }
            $fallbackEncoded = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(($fallbackSummary | ConvertTo-Json -Depth 4 -Compress)))
            Set-GuestInfoValue -Name $ResultVariableName -Value $fallbackEncoded
        }
        catch {
            try {
                Set-Content -LiteralPath (Join-Path $GuestRoot 'guestvar-write-error.txt') -Value $_.Exception.Message -Encoding UTF8
            }
            catch {
            }
        }
    }

    exit 0
}
'@

$hostPayloadPath = Join-Path $hostWorkRoot 'power-control-docs-first-trigger-etw-guestvar.ps1'
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

function Get-ShellHealthObject {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json
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

function Read-GuestVariableBestEffort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [int]$Attempts = 15,
        [int]$DelaySeconds = 2
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $value = Invoke-Vmrun -Arguments @('-T', 'ws', 'readVariable', $VmPath, 'guestVar', $Name)
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                return $value.Trim()
            }
        }
        catch {
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    return $null
}

function Publish-GuestSummaryVariable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestSummaryPath,
        [Parameter(Mandatory = $true)]
        [string]$VariableName
    )

    $script = @"
`$ErrorActionPreference = 'Stop'
`$vmtoolsd = 'C:\Program Files\VMware\VMware Tools\vmtoolsd.exe'
if (-not (Test-Path -LiteralPath '$GuestSummaryPath')) {
    throw 'Guest summary file is missing.'
}
if (-not (Test-Path -LiteralPath `$vmtoolsd)) {
    throw 'vmtoolsd.exe was not found in the guest.'
}
`$summary = Get-Content -LiteralPath '$GuestSummaryPath' -Raw | ConvertFrom-Json
`$compact = [ordered]@{
    generated_utc = `$summary.generated_utc
    value_name = `$summary.value_name
    trigger_profile = `$summary.trigger_profile
    status = `$summary.status
    etl_exists = [bool]`$summary.etl_exists
    csv_exists = [bool]`$summary.csv_exists
    etl_length = [int64]`$summary.etl_length
    csv_line_count = [int]`$summary.csv_line_count
    exact_runtime_read = [bool]`$summary.exact_runtime_read
    exact_query_hits = [int]`$summary.exact_query_hits
    exact_line_count = [int]`$summary.exact_line_count
    path_line_count = [int]`$summary.path_line_count
    trigger_failure_count = @(`$summary.trigger_failures).Count
    errors = @(`$summary.errors | Select-Object -First 3)
}
`$payload = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((`$compact | ConvertTo-Json -Depth 6 -Compress)))
& `$vmtoolsd --cmd ('info-set guestinfo.$VariableName ' + `$payload) | Out-Null
"@

    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script))
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-EncodedCommand',
        $encoded
    ) | Out-Null
}

function Convert-GuestVariableToObject {
    param([string]$Encoded)

    if ([string]::IsNullOrWhiteSpace($Encoded)) {
        return $null
    }

    $json = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Encoded))
    return $json | ConvertFrom-Json
}

function Write-Json {
    param([object]$Payload, [string]$HostPath, [string]$RepoPath = '')

    $json = ConvertTo-Json -InputObject $Payload -Depth 8
    Set-Content -Path $HostPath -Value $json -Encoding UTF8
    if ($RepoPath) {
        Copy-Item -Path $HostPath -Destination $RepoPath -Force
    }
}

function Invoke-GuestEncodedScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptText
    )

    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($ScriptText))
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-EncodedCommand',
        $encoded
    ) | Out-Null
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
    for ($candidateIndex = 0; $candidateIndex -lt @($candidates).Count; $candidateIndex++) {
        $candidate = $candidates[$candidateIndex]
        $candidateLabel = ($candidate.candidate_id -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
        $prefix = "$candidateLabel-trigger-etw-guestvar"
        $hostCandidateRoot = Join-Path $hostWorkRoot $candidateLabel
        $repoCandidateRoot = Join-Path $repoOutputRoot $candidateLabel
        $guestCandidateRoot = Join-Path $guestBatchRoot $candidateLabel
        $guestPayloadPath = Join-Path $guestScriptRoot 'power-control-docs-first-trigger-etw-guestvar.ps1'
        $resultVariableName = "rp.$stamp.$candidateIndex"

        $hostSummaryPath = Join-Path $hostCandidateRoot 'summary.json'
        $repoCandidateSummaryPath = Join-Path $repoCandidateRoot 'summary.json'
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

        $guestInvokeScript = @"
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path '$guestCandidateRoot' -Force | Out-Null
Set-Content -LiteralPath '$guestCandidateRoot\wrapper-started.txt' -Value 'started' -Encoding ASCII
& '$guestPayloadPath' -GuestRoot '$guestCandidateRoot' -ValueName '$($candidate.value_name)' -TriggerProfile '$($candidate.trigger_profile)' -ResultVariableName '$resultVariableName'
Set-Content -LiteralPath '$guestCandidateRoot\wrapper-finished.txt' -Value 'finished' -Encoding ASCII
exit 0
"@

        $guestInvokeError = $null
        try {
            Invoke-GuestEncodedScript -ScriptText $guestInvokeScript
        }
        catch {
            $guestInvokeError = $_.Exception.Message
        }

        $encodedSummary = Read-GuestVariableBestEffort -Name $resultVariableName
        if ([string]::IsNullOrWhiteSpace($encodedSummary)) {
            try {
                Publish-GuestSummaryVariable -GuestSummaryPath (Join-Path $guestCandidateRoot 'summary.json') -VariableName $resultVariableName
            }
            catch {
            }
            $encodedSummary = Read-GuestVariableBestEffort -Name $resultVariableName
        }
        $guestSummary = Convert-GuestVariableToObject -Encoded $encodedSummary
        $shellAfter = Get-ShellHealthBestEffort

        if ($null -eq $guestSummary) {
            $guestSummary = [pscustomobject]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                value_name = $candidate.value_name
                trigger_profile = $candidate.trigger_profile
                status = 'no-guestvar'
                exact_runtime_read = $false
                exact_query_hits = 0
                errors = @('Guest summary was not returned through guestVar.')
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
            guestvar_returned = -not [string]::IsNullOrWhiteSpace($encodedSummary)
            status = $guestSummary.status
            exact_query_hits = $guestSummary.exact_query_hits
            exact_runtime_read = [bool]$guestSummary.exact_runtime_read
            guest_summary = $guestSummary
            artifacts = [ordered]@{
                summary = "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
            }
        }

        @(
            '# External Evidence Placeholder',
            '',
            "Title: $($candidate.value_name) trigger ETW trace (guestVar return mode)",
            '',
            'The raw ETL and full tracerpt CSV stay in the guest/off-repo.',
            'The compact JSON result was returned through VMware guest variables instead of guest-to-host file copy.'
        ) | Set-Content -Path $repoEtlPlaceholder -Encoding UTF8

        Write-Json -Payload $candidateResult -HostPath $hostSummaryPath -RepoPath $repoCandidateSummaryPath
        $summary.candidate_summary_files += "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
        $results.Add([pscustomobject]$candidateResult) | Out-Null
    }

    $summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
    $summary.exact_line_only_candidates = @($results | Where-Object { $_.status -eq 'exact-line-no-query' }).Count
    $summary.path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
    $summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    $summary.error_candidates = @($results | Where-Object { $_.status -in @('error', 'no-guestvar') }).Count
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

