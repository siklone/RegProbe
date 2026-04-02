[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$SnapshotName = '',
    [int]$PostBootSettleSeconds = 20
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
$manifestPath = Join-Path $repoRoot 'registry-research-framework\audit\power-control-docs-first-value-exists-static-triage-20260329.json'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "power-control-docs-first-stepwise-runtime-$stamp"
$repoOutputRoot = Join-Path $repoRoot "evidence\files\vm-tooling-staging\$probeName"
$hostWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) $probeName
$guestRoot = "C:\RegProbe-Diag\$probeName"
$guestPayloadPath = Join-Path $guestRoot 'power-control-stepwise-runtime-payload.ps1'
$guestLastBootOutputPath = Join-Path $guestRoot 'lastboot.txt'
$hostLastBootQueryPath = Join-Path $hostWorkRoot 'power-control-query-lastboot.ps1'
$guestLastBootQueryPath = Join-Path $guestRoot 'power-control-query-lastboot.ps1'
$guestArmSummaryPath = Join-Path $guestRoot 'summary-arm.json'
$guestCollectSummaryPath = Join-Path $guestRoot 'summary-collect.json'
$guestExactHitsPath = Join-Path $guestRoot 'exact-hits.csv'
$guestPathHitsPath = Join-Path $guestRoot 'path-hits.csv'
$guestPmlPath = Join-Path $guestRoot 'power-control-docs-first-runtime.pml'
$guestCsvPath = Join-Path $guestRoot 'power-control-docs-first-runtime.csv'

$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'
$repoSessionPath = Join-Path $repoOutputRoot 'session.json'
$repoStepAPath = Join-Path $repoOutputRoot 'step-a-summary.json'
$repoStepBPath = Join-Path $repoOutputRoot 'step-b-summary.json'
$repoStepC1Path = Join-Path $repoOutputRoot 'step-c1-summary.json'
$repoStepC2Path = Join-Path $repoOutputRoot 'step-c2-summary.json'
$repoStepC3Path = Join-Path $repoOutputRoot 'step-c3-summary.json'
$repoStepC4Path = Join-Path $repoOutputRoot 'step-c4-summary.json'
$repoStepDPath = Join-Path $repoOutputRoot 'step-d-summary.json'
$repoArmSummaryPath = Join-Path $repoOutputRoot 'summary-arm.json'
$repoCollectSummaryPath = Join-Path $repoOutputRoot 'summary-collect.json'
$repoExactHitsPath = Join-Path $repoOutputRoot 'exact-hits.csv'
$repoPathHitsPath = Join-Path $repoOutputRoot 'path-hits.csv'
$repoPmlPlaceholderPath = Join-Path $repoOutputRoot 'power-control-docs-first-runtime.pml.md'

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

if (-not (Test-Path $manifestPath)) {
    throw "Manifest was not found: $manifestPath"
}

$manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
$candidates = @($manifest.candidates)
if (@($candidates).Count -eq 0) {
    throw 'No docs-first candidates were found in the source manifest.'
}

$candidateLiteralLines = foreach ($candidate in $candidates) {
    @(
        "[ordered]@{",
        "    candidate_id = '$($candidate.candidate_id)'",
        "    value_name = '$($candidate.value_name)'",
        "}"
    ) -join "`r`n"
}
$candidateBlock = ($candidateLiteralLines -join ",`r`n")

$guestPayload = @"
param(
    [Parameter(Mandatory = `$true)]
    [ValidateSet('arm', 'collect')]
    [string]`$Phase,
    [Parameter(Mandatory = `$true)]
    [string]`$GuestRoot,
    [Parameter(Mandatory = `$true)]
    [string]`$ArmSummaryPath,
    [Parameter(Mandatory = `$true)]
    [string]`$CollectSummaryPath,
    [Parameter(Mandatory = `$true)]
    [string]`$ExactHitsPath,
    [Parameter(Mandatory = `$true)]
    [string]`$PathHitsPath,
    [Parameter(Mandatory = `$true)]
    [string]`$GuestPmlPath,
    [Parameter(Mandatory = `$true)]
    [string]`$GuestCsvPath
)

`$ErrorActionPreference = 'Continue'
`$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
`$targetPathFragment = 'HKLM\SYSTEM\CurrentControlSet\Control\Power'
`$candidates = @(
$candidateBlock
)

function Get-ProcmonState {
    `$state = [ordered]@{}
    try {
        `$item = Get-ItemProperty -Path 'HKCU:\Software\Sysinternals\Process Monitor' -ErrorAction Stop
        foreach (`$name in @('Logfile', 'SourcePath', 'FlightRecorder', 'RingBufferSize', 'RingBufferMin')) {
            `$state[`$name] = `$item.`$name
        }
    }
    catch {
        `$state['error'] = `$_.Exception.Message
    }
    return `$state
}

function Find-BootLogCandidates {
    `$patterns = @(
        'C:\bootlog*.pml',
        'C:\Windows\bootlog*.pml',
        'C:\Tools\Sysinternals\bootlog*.pml',
        'C:\Tools\Perf\Procmon\bootlog*.pml',
        'C:\Users\Administrator\bootlog*.pml',
        'C:\Users\Administrator\AppData\Local\Temp\bootlog*.pml'
    )
    `$found = @()
    foreach (`$pattern in `$patterns) {
        try {
            `$found += Get-ChildItem -Path `$pattern -Force -ErrorAction SilentlyContinue | Select-Object FullName, Length, LastWriteTime
        }
        catch {
        }
    }
    return @(`$found | Sort-Object FullName -Unique)
}

function Write-JsonSummary {
    param([hashtable]`$Payload, [string]`$Path)
    `$Payload | ConvertTo-Json -Depth 8 | Set-Content -Path `$Path -Encoding UTF8
}

New-Item -ItemType Directory -Path `$GuestRoot -Force | Out-Null

if (`$Phase -eq 'arm') {
    `$summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        phase = 'arm'
        guest_root = `$GuestRoot
        procmon_path = `$procmon
        procmon_exists = [bool](Test-Path `$procmon)
        bootlog_candidates_before = Find-BootLogCandidates
        procmon_state_before = Get-ProcmonState
        errors = @()
    }
    try {
        if (-not (Test-Path `$procmon)) { throw 'Procmon64.exe was not found in the guest.' }
        & `$procmon /Terminate /Quiet | Out-Null
        Start-Sleep -Seconds 2
        `$directOutput = & `$procmon /AcceptEula /Quiet /EnableBootLogging 2>&1 | Out-String
        `$directExit = `$LASTEXITCODE
        `$minimizedOutput = & `$procmon /AcceptEula /Quiet /Minimized /EnableBootLogging 2>&1 | Out-String
        `$minimizedExit = `$LASTEXITCODE
        `$summary['commands'] = [ordered]@{
            direct_enable = [ordered]@{ exit_code = `$directExit; output = `$directOutput.Trim() }
            minimized_enable = [ordered]@{ exit_code = `$minimizedExit; output = `$minimizedOutput.Trim() }
        }
        `$summary['arm_result'] = if (`$minimizedExit -eq 0) { 'minimized-enable-returned-zero' } elseif (`$directExit -eq 0) { 'direct-enable-returned-zero' } else { 'no-enable-variant-returned-zero' }
    }
    catch {
        `$summary['errors'] = @(`$summary['errors']) + `$_.Exception.Message
    }
    `$summary['procmon_state_after'] = Get-ProcmonState
    `$summary['bootlog_candidates_after'] = Find-BootLogCandidates
    Write-JsonSummary -Payload `$summary -Path `$ArmSummaryPath
    exit 0
}

`$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    phase = 'collect'
    guest_root = `$GuestRoot
    procmon_path = `$procmon
    procmon_exists = [bool](Test-Path `$procmon)
    bootlog_candidates_before = Find-BootLogCandidates
    procmon_state_before = Get-ProcmonState
    errors = @()
}

try {
    if (-not (Test-Path `$procmon)) { throw 'Procmon64.exe was not found in the guest.' }
    foreach (`$path in @(`$ExactHitsPath, `$PathHitsPath, `$GuestCsvPath, `$GuestPmlPath)) {
        if (Test-Path `$path) { Remove-Item -Path `$path -Force -ErrorAction SilentlyContinue }
    }
    & `$procmon /Terminate /Quiet | Out-Null
    Start-Sleep -Seconds 2
    `$convertOutput = & `$procmon /AcceptEula /Quiet /Minimized /ConvertBootLog `$GuestPmlPath 2>&1 | Out-String
    `$convertExit = `$LASTEXITCODE
    `$summary['commands'] = [ordered]@{
        convert_bootlog = [ordered]@{ exit_code = `$convertExit; output = `$convertOutput.Trim() }
    }
    `$summary['pml_exists'] = [bool](Test-Path `$GuestPmlPath)
    if (`$summary['pml_exists']) { `$summary['pml_length'] = (Get-Item -Path `$GuestPmlPath).Length }
    `$saveOutput = & `$procmon /AcceptEula /OpenLog `$GuestPmlPath /SaveAs `$GuestCsvPath /Quiet 2>&1 | Out-String
    `$saveExit = `$LASTEXITCODE
    `$summary['commands']['save_as_csv'] = [ordered]@{ exit_code = `$saveExit; output = `$saveOutput.Trim() }
    `$summary['csv_exists'] = [bool](Test-Path `$GuestCsvPath)
    if (`$summary['csv_exists']) {
        `$summary['csv_length'] = (Get-Item -Path `$GuestCsvPath).Length
        `$rows = @(Import-Csv -Path `$GuestCsvPath)
        `$pathHits = @(`$rows | Where-Object { (`$_.Operation -like 'Reg*') -and (`$_.Path -like "*`$targetPathFragment*") })
        if (@(`$pathHits).Count -gt 0) { `$pathHits | Export-Csv -Path `$PathHitsPath -NoTypeInformation -Encoding UTF8 }
        `$exactHits = foreach (`$row in `$pathHits) {
            foreach (`$candidate in `$candidates) {
                if (`$row.Path -like "*\`$(`$candidate.value_name)") {
                    [pscustomobject]@{
                        CandidateId = `$candidate.candidate_id
                        ValueName = `$candidate.value_name
                        TimeOfDay = `$row.'Time of Day'
                        ProcessName = `$row.'Process Name'
                        PID = `$row.PID
                        Operation = `$row.Operation
                        Path = `$row.Path
                        Result = `$row.Result
                        Detail = `$row.Detail
                    }
                    break
                }
            }
        }
        if (@(`$exactHits).Count -gt 0) { `$exactHits | Export-Csv -Path `$ExactHitsPath -NoTypeInformation -Encoding UTF8 }
        `$candidateSummaries = foreach (`$candidate in `$candidates) {
            `$candidateExactHits = @(`$exactHits | Where-Object { `$_.CandidateId -eq `$candidate.candidate_id })
            `$queryHits = @(`$candidateExactHits | Where-Object { `$_.Operation -like 'RegQuery*' })
            `$pathCount = @(`$pathHits | Where-Object { `$_.Path -like "*\`$(`$candidate.value_name)" }).Count
            [ordered]@{
                candidate_id = `$candidate.candidate_id
                value_name = `$candidate.value_name
                path_hit_count = `$pathCount
                exact_value_hit_count = `$candidateExactHits.Count
                exact_read_hit_count = `$queryHits.Count
                exact_runtime_read = (`$candidateExactHits.Count -gt 0)
                hit_processes = @(`$candidateExactHits | Select-Object -ExpandProperty ProcessName -Unique)
                hit_operations = @(`$candidateExactHits | Select-Object -ExpandProperty Operation -Unique)
                status = if (`$candidateExactHits.Count -gt 0) { 'exact-hit' } elseif (`$pathCount -gt 0) { 'path-only-hit' } else { 'no-hit' }
            }
        }
        `$summary['total_path_hits'] = @(`$pathHits).Count
        `$summary['total_exact_hits'] = @(`$exactHits).Count
        `$summary['path_hits_exists'] = [bool](Test-Path `$PathHitsPath)
        `$summary['exact_hits_exists'] = [bool](Test-Path `$ExactHitsPath)
        `$summary['candidate_results'] = `$candidateSummaries
        `$summary['exact_hit_candidates'] = @(`$candidateSummaries | Where-Object { `$_.exact_runtime_read }).Count
        `$summary['path_only_candidates'] = @(`$candidateSummaries | Where-Object { `$_.status -eq 'path-only-hit' }).Count
        `$summary['no_hit_candidates'] = @(`$candidateSummaries | Where-Object { `$_.status -eq 'no-hit' }).Count
        `$summary['status'] = if (`$summary['exact_hit_candidates'] -gt 0) { 'exact-hit' } elseif (`$summary['path_only_candidates'] -gt 0) { 'path-only-hit' } else { 'no-hit' }
    }
}
catch {
    `$summary['errors'] = @(`$summary['errors']) + `$_.Exception.Message
    `$summary['status'] = 'error'
}
`$summary['procmon_state_after'] = Get-ProcmonState
`$summary['bootlog_candidates_after'] = Find-BootLogCandidates
Write-JsonSummary -Payload `$summary -Path `$CollectSummaryPath
"@

$hostPayloadPath = Join-Path $hostWorkRoot 'power-control-stepwise-runtime-payload.ps1'
Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8
@(
    '$ErrorActionPreference = ''Stop''',
    ('$out = ''{0}''' -f $guestLastBootOutputPath),
    '$dir = Split-Path -Parent $out',
    'if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }',
    '(Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToString(''o'') | Set-Content -Path $out -Encoding ASCII'
) | Set-Content -Path $hostLastBootQueryPath -Encoding ASCII

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)
    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) { throw "vmrun failed ($LASTEXITCODE): $($output.Trim())" }
    return $output.Trim()
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 600)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') { return }
        } catch {}
        Start-Sleep -Seconds 5
    }
    throw 'Guest did not return to a running VMware Tools state in time.'
}

function Wait-VmPoweredOff {
    param([int]$TimeoutSeconds = 300)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
            if ($running -notmatch [regex]::Escape($VmPath)) { return }
        } catch {}
        Start-Sleep -Seconds 5
    }
    throw "VM did not power off within $TimeoutSeconds seconds."
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath) | Out-Null
}

function Copy-FromGuestBestEffort {
    param([string]$GuestPath, [string]$HostPath, [string]$RepoPath = '')
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath) | Out-Null
        if ($RepoPath) { Copy-Item -Path $HostPath -Destination $RepoPath -Force }
        return $true
    } catch { return $false }
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList, [switch]$IgnoreExitCode)
    $output = & $VmrunPath @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'runProgramInGuest', $VmPath, 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe') @ArgumentList 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) { throw "Guest PowerShell failed ($LASTEXITCODE): $($output.Trim())" }
    return $output.Trim()
}

function Get-ShellHealthObject {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json
}

function Wait-ShellHealthy {
    param([int]$TimeoutSeconds = 600)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $health = Get-ShellHealthObject
        if ($health.shell_healthy) { return $health }
        Start-Sleep -Seconds 5
    }
    throw 'Guest shell did not become healthy in time.'
}

function Get-GuestLastBootUpTime {
    $hostLastBootOutputPath = Join-Path $hostWorkRoot 'lastboot.txt'
    if (Test-Path $hostLastBootOutputPath) { Remove-Item -Path $hostLastBootOutputPath -Force }
    Invoke-GuestPowerShell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $guestLastBootQueryPath) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestLastBootOutputPath, $hostLastBootOutputPath) | Out-Null
    $raw = (Get-Content -Path $hostLastBootOutputPath -Raw).Trim()
    [datetimeoffset]::Parse($raw)
}

function Invoke-HostBootCycle {
    param([datetimeoffset]$PreviousBootUpTime, [int]$ShutdownTimeoutSeconds = 240, [int]$StartupTimeoutSeconds = 600)
    $stopMode = 'soft'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'soft') -IgnoreExitCode | Out-Null
    try { Wait-VmPoweredOff -TimeoutSeconds $ShutdownTimeoutSeconds } catch {
        $stopMode = 'hard'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'hard') -IgnoreExitCode | Out-Null
        Wait-VmPoweredOff -TimeoutSeconds 90
    }
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady -TimeoutSeconds $StartupTimeoutSeconds
    Start-Sleep -Seconds 5
    $currentBoot = Get-GuestLastBootUpTime
    if ($currentBoot -le $PreviousBootUpTime) { throw "Guest boot cycle completed, but LastBootUpTime did not advance. Previous=$($PreviousBootUpTime.ToString('o')) Current=$($currentBoot.ToString('o'))" }
    [ordered]@{ previous_last_boot_utc = $PreviousBootUpTime.ToString('o'); current_last_boot_utc = $currentBoot.ToString('o'); stop_mode = $stopMode }
}

function Write-Json {
    param([object]$Payload, [string]$HostPath, [string]$RepoPath = '')
    $Payload | ConvertTo-Json -Depth 8 | Set-Content -Path $HostPath -Encoding UTF8
    if ($RepoPath) { Copy-Item -Path $HostPath -Destination $RepoPath -Force }
}

$hostSummaryPath = Join-Path $hostWorkRoot 'summary.json'
$hostResultsPath = Join-Path $hostWorkRoot 'results.json'
$hostSessionPath = Join-Path $hostWorkRoot 'session.json'
$hostStepAPath = Join-Path $hostWorkRoot 'step-a-summary.json'
$hostStepBPath = Join-Path $hostWorkRoot 'step-b-summary.json'
$hostStepC1Path = Join-Path $hostWorkRoot 'step-c1-summary.json'
$hostStepC2Path = Join-Path $hostWorkRoot 'step-c2-summary.json'
$hostStepC3Path = Join-Path $hostWorkRoot 'step-c3-summary.json'
$hostStepC4Path = Join-Path $hostWorkRoot 'step-c4-summary.json'
$hostStepDPath = Join-Path $hostWorkRoot 'step-d-summary.json'
$hostArmSummaryPath = Join-Path $hostWorkRoot 'summary-arm.json'
$hostCollectSummaryPath = Join-Path $hostWorkRoot 'summary-collect.json'

$session = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    record_family = 'power-control-docs-first'
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    guest_root = $guestRoot
    guest_payload_path = $guestPayloadPath
    completed_steps = @()
    next_step = 'A'
    phase_status = [ordered]@{}
    artifacts = [ordered]@{
        summary = "evidence/files/vm-tooling-staging/$probeName/summary.json"
        results = "evidence/files/vm-tooling-staging/$probeName/results.json"
        arm_summary = "evidence/files/vm-tooling-staging/$probeName/summary-arm.json"
        collect_summary = "evidence/files/vm-tooling-staging/$probeName/summary-collect.json"
        exact_hits = "evidence/files/vm-tooling-staging/$probeName/exact-hits.csv"
        path_hits = "evidence/files/vm-tooling-staging/$probeName/path-hits.csv"
        pml_placeholder = "evidence/files/vm-tooling-staging/$probeName/power-control-docs-first-runtime.pml.md"
    }
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    manifest = 'registry-research-framework/audit/power-control-docs-first-value-exists-static-triage-20260329.json'
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = 'started'
    shell_before = $null
    shell_after = $null
    boot_cycle = $null
    total_candidates = @($candidates).Count
    exact_hit_candidates = 0
    path_only_candidates = 0
    no_hit_candidates = 0
    copy_incomplete_candidates = 0
    error_candidates = 0
    exact_hits_path = "evidence/files/vm-tooling-staging/$probeName/exact-hits.csv"
    path_hits_path = "evidence/files/vm-tooling-staging/$probeName/path-hits.csv"
    step_summary_files = [ordered]@{
        A = "evidence/files/vm-tooling-staging/$probeName/step-a-summary.json"
        B = "evidence/files/vm-tooling-staging/$probeName/step-b-summary.json"
        C1 = "evidence/files/vm-tooling-staging/$probeName/step-c1-summary.json"
        C2 = "evidence/files/vm-tooling-staging/$probeName/step-c2-summary.json"
        C3 = "evidence/files/vm-tooling-staging/$probeName/step-c3-summary.json"
        C4 = "evidence/files/vm-tooling-staging/$probeName/step-c4-summary.json"
        D = "evidence/files/vm-tooling-staging/$probeName/step-d-summary.json"
    }
    errors = @()
}

try {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    $shellBefore = Wait-ShellHealthy
    $summary.shell_before = $shellBefore

    Invoke-GuestPowerShell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', "New-Item -ItemType Directory -Path '$guestRoot' -Force | Out-Null") | Out-Null
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath
    Copy-ToGuest -HostPath $hostLastBootQueryPath -GuestPath $guestLastBootQueryPath

    $stepA = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        status = 'ok'
        guest_root = $guestRoot
        snapshot_name = $SnapshotName
        shell_before = $shellBefore
    }
    Write-Json -Payload $stepA -HostPath $hostStepAPath -RepoPath $repoStepAPath
    $session.completed_steps += 'A'
    $session.phase_status['A'] = [ordered]@{ status = 'ok'; failed_stage = $null; summary_file = 'step-a-summary.json' }
    $session.next_step = 'B'
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath

    Invoke-GuestPowerShell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $guestPayloadPath, '-Phase', 'arm', '-GuestRoot', $guestRoot, '-ArmSummaryPath', $guestArmSummaryPath, '-CollectSummaryPath', $guestCollectSummaryPath, '-ExactHitsPath', $guestExactHitsPath, '-PathHitsPath', $guestPathHitsPath, '-GuestPmlPath', $guestPmlPath, '-GuestCsvPath', $guestCsvPath) | Out-Null
    $armCopied = Copy-FromGuestBestEffort -GuestPath $guestArmSummaryPath -HostPath $hostArmSummaryPath -RepoPath $repoArmSummaryPath
    $previousBoot = Get-GuestLastBootUpTime
    $bootCycle = Invoke-HostBootCycle -PreviousBootUpTime $previousBoot
    Start-Sleep -Seconds $PostBootSettleSeconds
    $shellAfterBoot = Wait-ShellHealthy
    $stepB = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        status = if ($armCopied) { 'ok' } else { 'copy-incomplete' }
        arm_summary_file = if ($armCopied) { 'summary-arm.json' } else { $null }
        boot_cycle = $bootCycle
        shell_after_boot = $shellAfterBoot
    }
    Write-Json -Payload $stepB -HostPath $hostStepBPath -RepoPath $repoStepBPath
    $summary.boot_cycle = $bootCycle
    $session.completed_steps += 'B'
    $session.phase_status['B'] = [ordered]@{ status = 'ok'; failed_stage = $null; summary_file = 'step-b-summary.json' }
    $session.next_step = 'C1'
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath

    Invoke-GuestPowerShell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $guestPayloadPath, '-Phase', 'collect', '-GuestRoot', $guestRoot, '-ArmSummaryPath', $guestArmSummaryPath, '-CollectSummaryPath', $guestCollectSummaryPath, '-ExactHitsPath', $guestExactHitsPath, '-PathHitsPath', $guestPathHitsPath, '-GuestPmlPath', $guestPmlPath, '-GuestCsvPath', $guestCsvPath) | Out-Null
    $collectCopied = Copy-FromGuestBestEffort -GuestPath $guestCollectSummaryPath -HostPath $hostCollectSummaryPath -RepoPath $repoCollectSummaryPath
    $stepC1 = [ordered]@{ generated_utc = [DateTime]::UtcNow.ToString('o'); status = if ($collectCopied) { 'ok' } else { 'copy-incomplete' }; collect_summary_file = if ($collectCopied) { 'summary-collect.json' } else { $null } }
    Write-Json -Payload $stepC1 -HostPath $hostStepC1Path -RepoPath $repoStepC1Path
    $session.completed_steps += 'C1'
    $session.phase_status['C1'] = [ordered]@{ status = $stepC1.status; failed_stage = if ($collectCopied) { $null } else { 'collect-summary-copy' }; summary_file = 'step-c1-summary.json' }
    $session.next_step = 'C2'
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath
    if (-not $collectCopied) { throw 'Failed to copy guest collect summary back to the host.' }

    $collectSummary = Get-Content -Path $hostCollectSummaryPath -Raw | ConvertFrom-Json
    $results = @($collectSummary.candidate_results)
    $exactCopied = Copy-FromGuestBestEffort -GuestPath $guestExactHitsPath -HostPath (Join-Path $hostWorkRoot 'exact-hits.csv') -RepoPath $repoExactHitsPath
    $pathCopied = Copy-FromGuestBestEffort -GuestPath $guestPathHitsPath -HostPath (Join-Path $hostWorkRoot 'path-hits.csv') -RepoPath $repoPathHitsPath
    $stepC2 = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        status = 'ok'
        exact_hits_copied = $exactCopied
        path_hits_copied = $pathCopied
        exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
        path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
        no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    }
    Write-Json -Payload $stepC2 -HostPath $hostStepC2Path -RepoPath $repoStepC2Path
    $session.completed_steps += 'C2'
    $session.phase_status['C2'] = [ordered]@{ status = 'ok'; failed_stage = $null; summary_file = 'step-c2-summary.json' }
    $session.next_step = 'C3'
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath

    $stepC3 = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        status = 'ok'
        guest_processed = $true
        copied_files = [ordered]@{ collect_summary = $collectCopied; exact_hits = $exactCopied; path_hits = $pathCopied }
    }
    Write-Json -Payload $stepC3 -HostPath $hostStepC3Path -RepoPath $repoStepC3Path
    $session.completed_steps += 'C3'
    $session.phase_status['C3'] = [ordered]@{ status = 'ok'; failed_stage = $null; summary_file = 'step-c3-summary.json' }
    $session.next_step = 'C4'
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath

    if ($collectSummary.pml_exists) {
        @('# External Evidence Placeholder', '', 'Title: Power-control docs-first Procmon boot log', '', 'The raw Procmon PML stays out of git. Use the guest-processed summary JSON plus the exact/path hit CSVs in this folder as the committed runtime proof package.') -join "`n" | Set-Content -Path $repoPmlPlaceholderPath -Encoding UTF8
    }
    $stepC4 = [ordered]@{ generated_utc = [DateTime]::UtcNow.ToString('o'); status = 'ok'; pml_placeholder_written = [bool](Test-Path $repoPmlPlaceholderPath); results_count = @($results).Count }
    Write-Json -Payload $stepC4 -HostPath $hostStepC4Path -RepoPath $repoStepC4Path
    $session.completed_steps += 'C4'
    $session.phase_status['C4'] = [ordered]@{ status = 'ok'; failed_stage = $null; summary_file = 'step-c4-summary.json' }
    $session.next_step = 'D'
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath

    $shellAfter = Wait-ShellHealthy
    $summary.shell_after = $shellAfter
    $summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
    $summary.path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
    $summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.path_only_candidates -gt 0) { 'path-only-hit' } else { 'no-hit' }
    Write-Json -Payload $results -HostPath $hostResultsPath -RepoPath $repoResultsPath
    Write-Json -Payload $summary -HostPath $hostSummaryPath -RepoPath $repoSummaryPath

    $stepD = [ordered]@{ generated_utc = [DateTime]::UtcNow.ToString('o'); status = 'ok'; shell_after = $shellAfter; summary_file = 'summary.json'; results_file = 'results.json' }
    Write-Json -Payload $stepD -HostPath $hostStepDPath -RepoPath $repoStepDPath
    $session.completed_steps += 'D'
    $session.phase_status['D'] = [ordered]@{ status = 'ok'; failed_stage = $null; summary_file = 'step-d-summary.json' }
    $session.next_step = ''
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
    Write-Json -Payload $summary -HostPath $hostSummaryPath -RepoPath $repoSummaryPath
    Write-Json -Payload $session -HostPath $hostSessionPath -RepoPath $repoSessionPath
    throw
}

Write-Output $repoSummaryPath

