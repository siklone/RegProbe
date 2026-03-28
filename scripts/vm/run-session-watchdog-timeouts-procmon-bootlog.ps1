[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\watchdog-procmon-bootlog',
    [string]$SnapshotName = 'baseline-20260327-regprobe-visible-shell-stable',
    [int]$PostBootSettleSeconds = 20
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "watchdog-procmon-bootlog-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName

$hostPayloadPath = Join-Path $hostRoot 'watchdog-procmon-bootlog-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'watchdog-procmon-bootlog-payload.ps1'
$hostLastBootQueryPath = Join-Path $hostRoot 'watchdog-query-lastboot.ps1'
$guestLastBootQueryPath = Join-Path $guestRoot 'watchdog-query-lastboot.ps1'
$guestLastBootOutputPath = Join-Path $guestRoot 'lastboot.txt'
$hostLastBootOutputPath = Join-Path $hostRoot 'lastboot.txt'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'
$hostArmSummaryPath = Join-Path $hostRoot 'summary-arm.json'
$hostCollectSummaryPath = Join-Path $hostRoot 'summary-collect.json'
$repoArmSummaryPath = Join-Path $repoRootOut 'summary-arm.json'
$repoCollectSummaryPath = Join-Path $repoRootOut 'summary-collect.json'
$guestArmSummaryPath = Join-Path $guestRoot 'summary-arm.json'
$guestCollectSummaryPath = Join-Path $guestRoot 'summary-collect.json'
$guestPmlPath = Join-Path $guestRoot 'watchdog-procmon-bootlog.pml'
$guestCsvPath = Join-Path $guestRoot 'watchdog-procmon-bootlog.csv'
$guestHitsPath = Join-Path $guestRoot 'watchdog-procmon-bootlog.hits.csv'
$hostPmlPath = Join-Path $hostRoot 'watchdog-procmon-bootlog.pml'
$hostCsvPath = Join-Path $hostRoot 'watchdog-procmon-bootlog.csv'
$hostHitsPath = Join-Path $hostRoot 'watchdog-procmon-bootlog.hits.csv'
$repoPmlPlaceholderPath = Join-Path $repoRootOut 'watchdog-procmon-bootlog.pml.md'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('arm', 'collect')]
    [string]$Phase,

    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

    [Parameter(Mandatory = $true)]
    [string]$GuestPmlPath,

    [Parameter(Mandatory = $true)]
    [string]$GuestCsvPath,

    [Parameter(Mandatory = $true)]
    [string]$GuestHitsPath
)

$ErrorActionPreference = 'Continue'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
$targetPathFragment = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power'
$targetValueFragments = @(
    'WatchdogResumeTimeout',
    'WatchdogSleepTimeout',
    'PowerSettingProfile',
    'SystemPowerPolicy',
    'ShutdownOccurred'
)
$targetProcesses = @('System', 'svchost.exe')
$targetOperations = @('RegQueryValue', 'RegOpenKey', 'RegCreateKey', 'RegSetValue')

function Get-ProcmonState {
    $state = [ordered]@{}
    try {
        $item = Get-ItemProperty -Path 'HKCU:\Software\Sysinternals\Process Monitor' -ErrorAction Stop
        foreach ($name in @('Logfile', 'SourcePath', 'FlightRecorder', 'RingBufferSize', 'RingBufferMin')) {
            $state[$name] = $item.$name
        }
    }
    catch {
        $state['error'] = $_.Exception.Message
    }

    return $state
}

function Find-BootLogCandidates {
    $paths = @(
        'C:\bootlog*.pml',
        'C:\Windows\bootlog*.pml',
        'C:\Tools\Sysinternals\bootlog*.pml',
        'C:\Tools\Perf\Procmon\bootlog*.pml',
        'C:\Users\Administrator\bootlog*.pml',
        'C:\Users\Administrator\AppData\Local\Temp\bootlog*.pml'
    )

    $found = @()
    foreach ($pattern in $paths) {
        try {
            $found += Get-ChildItem -Path $pattern -Force -ErrorAction SilentlyContinue | Select-Object FullName, Length, LastWriteTime
        }
        catch {
        }
    }

    return @($found | Sort-Object FullName -Unique)
}

function Write-Summary {
    param([hashtable]$Payload)

    $Payload | ConvertTo-Json -Depth 8 | Set-Content -Path $SummaryPath -Encoding UTF8
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    phase = $Phase
    guest_root = $GuestRoot
    procmon_path = $procmon
    procmon_exists = [bool](Test-Path $procmon)
    procmon_state_before = Get-ProcmonState
    errors = @()
}

try {
    if (-not (Test-Path $procmon)) {
        throw 'Procmon64.exe was not found in the guest.'
    }

    if ($Phase -eq 'arm') {
        & $procmon /Terminate /Quiet | Out-Null
        Start-Sleep -Seconds 2

        $directOutput = & $procmon /AcceptEula /Quiet /EnableBootLogging 2>&1 | Out-String
        $directExit = $LASTEXITCODE

        $minimizedOutput = & $procmon /AcceptEula /Quiet /Minimized /EnableBootLogging 2>&1 | Out-String
        $minimizedExit = $LASTEXITCODE

        $summary['commands'] = [ordered]@{
            direct_enable = [ordered]@{
                exit_code = $directExit
                output = $directOutput.Trim()
            }
            minimized_enable = [ordered]@{
                exit_code = $minimizedExit
                output = $minimizedOutput.Trim()
            }
        }
        $summary['arm_result'] = if ($minimizedExit -eq 0) { 'minimized-enable-returned-zero' } elseif ($directExit -eq 0) { 'direct-enable-returned-zero' } else { 'no-enable-variant-returned-zero' }
        $summary['procmon_state_after'] = Get-ProcmonState
        Write-Summary -Payload $summary
        exit 0
    }

    $summary['bootlog_candidates_before'] = Find-BootLogCandidates

    & $procmon /Terminate /Quiet | Out-Null
    Start-Sleep -Seconds 2

    $convertOutput = & $procmon /AcceptEula /Quiet /Minimized /ConvertBootLog $GuestPmlPath 2>&1 | Out-String
    $convertExit = $LASTEXITCODE

    $summary['commands'] = [ordered]@{
        convert_bootlog = [ordered]@{
            exit_code = $convertExit
            output = $convertOutput.Trim()
        }
    }
    $summary['pml_exists'] = [bool](Test-Path $GuestPmlPath)
    if ($summary['pml_exists']) {
        $summary['pml_length'] = (Get-Item -Path $GuestPmlPath).Length

        $saveOutput = & $procmon /AcceptEula /OpenLog $GuestPmlPath /SaveAs $GuestCsvPath /Quiet 2>&1 | Out-String
        $saveExit = $LASTEXITCODE
        $summary['commands']['save_as_csv'] = [ordered]@{
            exit_code = $saveExit
            output = $saveOutput.Trim()
        }
        $summary['csv_exists'] = [bool](Test-Path $GuestCsvPath)
        if ($summary['csv_exists']) {
            $summary['csv_length'] = (Get-Item -Path $GuestCsvPath).Length
            $rows = Import-Csv -Path $GuestCsvPath
            $hits = $rows | Where-Object {
                ($_.Operation -in $targetOperations) -and
                ($_.Path -like "*$targetPathFragment*") -and
                ($_.Path -match 'WatchdogResumeTimeout|WatchdogSleepTimeout|PowerSettingProfile|SystemPowerPolicy|ShutdownOccurred') -and
                ($_. 'Process Name' -in $targetProcesses)
            }
            $summary['match_count'] = @($hits).Count
            if (@($hits).Count -gt 0) {
                $hits | Export-Csv -Path $GuestHitsPath -NoTypeInformation -Encoding UTF8
            }
            $summary['hits_exists'] = [bool](Test-Path $GuestHitsPath)
        }
    }

    $summary['bootlog_candidates_after'] = Find-BootLogCandidates
    $summary['procmon_state_after'] = Get-ProcmonState
}
catch {
    $summary['errors'] = @($summary['errors']) + $_.Exception.Message
}

Write-Summary -Payload $summary
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
    param(
        [string[]]$ArgumentList,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'runProgramInGuest',
        $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) @ArgumentList 2>&1 | Out-String

    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "Guest PowerShell failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Get-GuestLastBootUpTime {
    if (Test-Path $hostLastBootOutputPath) {
        Remove-Item -Path $hostLastBootOutputPath -Force
    }

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestLastBootQueryPath
    ) | Out-Null

    Copy-FromGuest -GuestPath $guestLastBootOutputPath -HostPath $hostLastBootOutputPath
    $raw = (Get-Content -Path $hostLastBootOutputPath -Raw).Trim()
    return [datetimeoffset]::Parse($raw)
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
    arm_summary_path = "evidence/files/vm-tooling-staging/$probeName/summary-arm.json"
    collect_summary_path = "evidence/files/vm-tooling-staging/$probeName/summary-collect.json"
    procmon_pml_placeholder = "evidence/files/vm-tooling-staging/$probeName/watchdog-procmon-bootlog.pml.md"
    pml_captured = $false
    csv_captured = $false
    hits_captured = $false
    arm_summary = $null
    collect_summary = $null
    errors = @()
}

$probeFailed = $false

try {
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady

    $summary.shell_before = Get-ShellHealth | ConvertFrom-Json

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command', "New-Item -ItemType Directory -Force -Path '$guestRoot' | Out-Null"
    )
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath
    Copy-ToGuest -HostPath $hostLastBootQueryPath -GuestPath $guestLastBootQueryPath

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'arm',
        '-GuestRoot', $guestRoot,
        '-SummaryPath', $guestArmSummaryPath,
        '-GuestPmlPath', $guestPmlPath,
        '-GuestCsvPath', $guestCsvPath,
        '-GuestHitsPath', $guestHitsPath
    ) -IgnoreExitCode

    try {
        Copy-FromGuest -GuestPath $guestArmSummaryPath -HostPath $hostArmSummaryPath
        Copy-FromGuest -GuestPath $guestArmSummaryPath -HostPath $repoArmSummaryPath
        $summary.arm_summary = Get-Content -Path $hostArmSummaryPath -Raw | ConvertFrom-Json
    }
    catch {
        $summary.errors += "Failed to copy arm summary: $($_.Exception.Message)"
    }

    $previousBoot = Get-GuestLastBootUpTime
    $summary.boot_cycle = Invoke-HostBootCycle -PreviousBootUpTime $previousBoot
    Start-Sleep -Seconds $PostBootSettleSeconds

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'collect',
        '-GuestRoot', $guestRoot,
        '-SummaryPath', $guestCollectSummaryPath,
        '-GuestPmlPath', $guestPmlPath,
        '-GuestCsvPath', $guestCsvPath,
        '-GuestHitsPath', $guestHitsPath
    ) -IgnoreExitCode

    try {
        Copy-FromGuest -GuestPath $guestCollectSummaryPath -HostPath $hostCollectSummaryPath
        Copy-FromGuest -GuestPath $guestCollectSummaryPath -HostPath $repoCollectSummaryPath
        $summary.collect_summary = Get-Content -Path $hostCollectSummaryPath -Raw | ConvertFrom-Json
    }
    catch {
        $summary.errors += "Failed to copy collect summary: $($_.Exception.Message)"
    }

    foreach ($pair in @(
        @{ Guest = $guestPmlPath; Host = $hostPmlPath },
        @{ Guest = $guestCsvPath; Host = $hostCsvPath },
        @{ Guest = $guestHitsPath; Host = $hostHitsPath }
    )) {
        try {
            Copy-FromGuest -GuestPath $pair.Guest -HostPath $pair.Host
        }
        catch {
        }
    }

    $summary.pml_captured = [bool](Test-Path $hostPmlPath)
    $summary.csv_captured = [bool](Test-Path $hostCsvPath)
    $summary.hits_captured = [bool](Test-Path $hostHitsPath)
    $summary.shell_after = Get-ShellHealth | ConvertFrom-Json
}
catch {
    $probeFailed = $true
    $summary.errors += $_.Exception.Message
}

$summary.status = if ($probeFailed) { 'failed' } elseif ($summary.pml_captured) { 'procmon-bootlog-captured' } else { 'procmon-bootlog-cli-unresolved' }
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $repoSummaryPath -Encoding UTF8

$pmlPlaceholder = @(
    '# External Evidence Placeholder',
    '',
    'Title: power.session-watchdog-timeouts Procmon boot log',
    '',
    'The raw Procmon boot-log artifact is not committed here. Use the summary JSON, the phase summaries, and any filtered CSV hits in the same folder.'
) -join "`n"
Set-Content -Path $repoPmlPlaceholderPath -Value $pmlPlaceholder -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}
