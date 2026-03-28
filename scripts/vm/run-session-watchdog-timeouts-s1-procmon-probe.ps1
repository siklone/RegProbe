[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon',
    [string]$SnapshotName = 'baseline-20260327-regprobe-visible-shell-stable',
    [int]$TransitionTimeoutSeconds = 420
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "watchdog-s1-procmon-$stamp"
$recordId = 'power.session-watchdog-timeouts'
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'watchdog-s1-procmon-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'watchdog-s1-procmon-payload.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$prefix = 'watchdog-s1-procmon'
$repoTxt = Join-Path $repoRootOut "$prefix.txt"
$repoHitsCsv = Join-Path $repoRootOut "$prefix.hits.csv"
$repoBefore = Join-Path $repoRootOut "$prefix-before.txt"
$repoAfter = Join-Path $repoRootOut "$prefix-after.txt"
$repoLastWake = Join-Path $repoRootOut "$prefix-lastwake.txt"
$repoKernelPower = Join-Path $repoRootOut "$prefix-kernelpower.txt"
$repoPmlPlaceholder = Join-Path $repoRootOut "$prefix.pml.md"

$guestPml = Join-Path $guestRoot "$prefix.pml"
$guestCsv = Join-Path $guestRoot "$prefix.csv"
$guestHitsCsv = Join-Path $guestRoot "$prefix.hits.csv"
$guestTxt = Join-Path $guestRoot "$prefix.txt"
$guestBefore = Join-Path $guestRoot "$prefix-before.txt"
$guestAfter = Join-Path $guestRoot "$prefix-after.txt"
$guestLastWake = Join-Path $guestRoot "$prefix-lastwake.txt"
$guestKernelPower = Join-Path $guestRoot "$prefix-kernelpower.txt"
$guestSummary = Join-Path $guestRoot "$prefix-summary.json"
$guestWrapperError = Join-Path $guestRoot "$prefix-wrapper-error.txt"

$hostPml = Join-Path $hostRoot "$prefix.pml"
$hostCsv = Join-Path $hostRoot "$prefix.csv"
$hostHitsCsv = Join-Path $hostRoot "$prefix.hits.csv"
$hostTxt = Join-Path $hostRoot "$prefix.txt"
$hostBefore = Join-Path $hostRoot "$prefix-before.txt"
$hostAfter = Join-Path $hostRoot "$prefix-after.txt"
$hostLastWake = Join-Path $hostRoot "$prefix-lastwake.txt"
$hostKernelPower = Join-Path $hostRoot "$prefix-kernelpower.txt"
$hostGuestSummary = Join-Path $hostRoot "$prefix-summary.json"
$hostWrapperError = Join-Path $hostRoot "$prefix-wrapper-error.txt"
$vmrunStdoutPath = Join-Path $hostRoot 'vmrun-stdout.txt'
$vmrunStderrPath = Join-Path $hostRoot 'vmrun-stderr.txt'

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$GuestRoot
)

$ErrorActionPreference = 'Continue'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
$prefix = 'watchdog-s1-procmon'
$pml = Join-Path $GuestRoot "$prefix.pml"
$csv = Join-Path $GuestRoot "$prefix.csv"
$hitsCsv = Join-Path $GuestRoot "$prefix.hits.csv"
$txt = Join-Path $GuestRoot "$prefix.txt"
$beforePath = Join-Path $GuestRoot "$prefix-before.txt"
$afterPath = Join-Path $GuestRoot "$prefix-after.txt"
$lastWakePath = Join-Path $GuestRoot "$prefix-lastwake.txt"
$kernelPowerPath = Join-Path $GuestRoot "$prefix-kernelpower.txt"
$summaryPath = Join-Path $GuestRoot "$prefix-summary.json"
$wrapperErrorPath = Join-Path $GuestRoot "$prefix-wrapper-error.txt"
$targetPathFragment = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power'
$targetProcesses = @('System', 'svchost.exe', 'rundll32.exe', 'powercfg.exe', 'services.exe')
$targetFragments = @('WatchdogResumeTimeout', 'WatchdogSleepTimeout', 'PowerSettingProfile', 'SystemPowerPolicy', 'ShutdownOccurred')

function Write-ErrorFile {
    param([System.Exception]$Exception, $Invocation)
    @(
        ('ERROR=' + $Exception.GetType().FullName + ': ' + $Exception.Message),
        ('AT=' + $Invocation.PositionMessage)
    ) | Set-Content -Path $wrapperErrorPath -Encoding UTF8
}

function Write-Summary {
    param([hashtable]$Payload)
    $Payload | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
}

try {
    New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null
    foreach ($path in @($pml, $csv, $hitsCsv, $txt, $beforePath, $afterPath, $lastWakePath, $kernelPowerPath, $summaryPath, $wrapperErrorPath)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
        }
    }

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) -WindowStyle Hidden | Out-Null
    Start-Sleep -Seconds 4

    [DateTime]::UtcNow.ToString('o') | Set-Content -Path $beforePath -Encoding UTF8

    Start-Process -FilePath "$env:SystemRoot\System32\rundll32.exe" -ArgumentList 'powrprof.dll,SetSuspendState 0,1,0' -WindowStyle Hidden | Out-Null
    Start-Sleep -Seconds 35

    [DateTime]::UtcNow.ToString('o') | Set-Content -Path $afterPath -Encoding UTF8
    & cmd.exe /c ('powercfg /lastwake > "{0}" 2>&1' -f $lastWakePath) | Out-Null
    & cmd.exe /c ('wevtutil qe System /q:"*[System[(Provider[@Name=''Microsoft-Windows-Kernel-Power''] and (EventID=1 or EventID=42 or EventID=107 or EventID=506))]]" /c:20 /rd:true /f:text > "{0}" 2>&1' -f $kernelPowerPath) | Out-Null

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    & $procmon /AcceptEula /OpenLog $pml /SaveAs $csv /Quiet | Out-Null

    $matches = @()
    if (Test-Path $csv) {
        $rows = Import-Csv $csv
        $matches = $rows | Where-Object {
            $path = $_.Path
            $processName = $_.'Process Name'
            $operation = $_.Operation

            if ($operation -notlike 'Reg*') {
                return $false
            }

            if ($path -notlike "*$targetPathFragment*") {
                return $false
            }

            $fragmentMatched = $false
            foreach ($fragment in $targetFragments) {
                if ($path -like "*$fragment*") {
                    $fragmentMatched = $true
                    break
                }
            }

            if (-not $fragmentMatched) {
                return $false
            }

            foreach ($name in $targetProcesses) {
                if ($processName -ieq $name) {
                    return $true
                }
            }

            return $false
        }

        if (@($matches).Count -gt 0) {
            $matches | Export-Csv -Path $hitsCsv -NoTypeInformation -Encoding UTF8
        }
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        lane_label = 'power.session-watchdog-timeouts'
        probe = 's1-procmon'
        suspend_command = 'rundll32.exe powrprof.dll,SetSuspendState 0,1,0'
        pml_exists = [bool](Test-Path $pml)
        csv_exists = [bool](Test-Path $csv)
        hits_exists = [bool](Test-Path $hitsCsv)
        before_marker_exists = [bool](Test-Path $beforePath)
        after_marker_exists = [bool](Test-Path $afterPath)
        lastwake_exists = [bool](Test-Path $lastWakePath)
        kernelpower_exists = [bool](Test-Path $kernelPowerPath)
        match_count = @($matches).Count
    }

    $lines = @(
        'PROBE=s1-procmon',
        ('PML_EXISTS=' + $summary.pml_exists),
        ('CSV_EXISTS=' + $summary.csv_exists),
        ('HITS_EXISTS=' + $summary.hits_exists),
        ('BEFORE_EXISTS=' + $summary.before_marker_exists),
        ('AFTER_EXISTS=' + $summary.after_marker_exists),
        ('LASTWAKE_EXISTS=' + $summary.lastwake_exists),
        ('KERNELPOWER_EXISTS=' + $summary.kernelpower_exists),
        ('MATCH_COUNT=' + $summary.match_count)
    )
    $lines | Set-Content -Path $txt -Encoding UTF8
    Write-Summary -Payload $summary
}
catch {
    Write-ErrorFile -Exception $_.Exception -Invocation $_.InvocationInfo
    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        lane_label = 'power.session-watchdog-timeouts'
        probe = 's1-procmon'
        error = $_.Exception.Message
        pml_exists = [bool](Test-Path $pml)
        csv_exists = [bool](Test-Path $csv)
        before_marker_exists = [bool](Test-Path $beforePath)
        after_marker_exists = [bool](Test-Path $afterPath)
        lastwake_exists = [bool](Test-Path $lastWakePath)
        kernelpower_exists = [bool](Test-Path $kernelPowerPath)
    }
    Write-Summary -Payload $summary
}

exit 0
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

    throw 'Guest did not reach a running VMware Tools state in time.'
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

function Restore-HealthySnapshot {
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        return
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
Wait-GuestReady

$shellBefore = Wait-ShellHealthy

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'createDirectoryInGuest', $VmPath, $guestRoot
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $hostPayloadPath, $guestPayloadPath
) | Out-Null

$vmrunArgs = @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $guestPayloadPath,
    '-GuestRoot', $guestRoot
)

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $VmrunPath
$psi.Arguments = ($vmrunArgs | ForEach-Object {
    if ($_ -match '\s') { '"{0}"' -f ($_ -replace '"', '\"') } else { $_ }
}) -join ' '
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$proc = [System.Diagnostics.Process]::Start($psi)

$toolsTransitions = New-Object System.Collections.Generic.List[string]
$deadline = (Get-Date).AddSeconds($TransitionTimeoutSeconds)

while (-not $proc.HasExited -and (Get-Date) -lt $deadline) {
    try {
        $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath) -IgnoreExitCode
        if (-not [string]::IsNullOrWhiteSpace($state)) {
            $toolsTransitions.Add(("{0} {1}" -f (Get-Date).ToString('o'), $state))
        }
    }
    catch {
        $toolsTransitions.Add(("{0} query-failed" -f (Get-Date).ToString('o')))
    }

    Start-Sleep -Seconds 5
}

$timedOut = $false
if (-not $proc.HasExited) {
    $timedOut = $true
    try { $proc.Kill() } catch {}
}
else {
    $proc.WaitForExit()
}

$proc.StandardOutput.ReadToEnd() | Set-Content -Path $vmrunStdoutPath -Encoding UTF8
$proc.StandardError.ReadToEnd() | Set-Content -Path $vmrunStderrPath -Encoding UTF8

$copied = [ordered]@{
    summary = Copy-FromGuestBestEffort -GuestPath $guestSummary -HostPath $hostGuestSummary
    pml = Copy-FromGuestBestEffort -GuestPath $guestPml -HostPath $hostPml
    csv = Copy-FromGuestBestEffort -GuestPath $guestCsv -HostPath $hostCsv
    hits = Copy-FromGuestBestEffort -GuestPath $guestHitsCsv -HostPath $hostHitsCsv
    txt = Copy-FromGuestBestEffort -GuestPath $guestTxt -HostPath $hostTxt
    before = Copy-FromGuestBestEffort -GuestPath $guestBefore -HostPath $hostBefore
    after = Copy-FromGuestBestEffort -GuestPath $guestAfter -HostPath $hostAfter
    lastwake = Copy-FromGuestBestEffort -GuestPath $guestLastWake -HostPath $hostLastWake
    kernelpower = Copy-FromGuestBestEffort -GuestPath $guestKernelPower -HostPath $hostKernelPower
    wrapper_error = Copy-FromGuestBestEffort -GuestPath $guestWrapperError -HostPath $hostWrapperError
}

$repoCopied = [ordered]@{
    txt = Sync-ToRepoBestEffort -HostPath $hostTxt -RepoPath $repoTxt
    hits = Sync-ToRepoBestEffort -HostPath $hostHitsCsv -RepoPath $repoHitsCsv
    before = Sync-ToRepoBestEffort -HostPath $hostBefore -RepoPath $repoBefore
    after = Sync-ToRepoBestEffort -HostPath $hostAfter -RepoPath $repoAfter
    lastwake = Sync-ToRepoBestEffort -HostPath $hostLastWake -RepoPath $repoLastWake
    kernelpower = Sync-ToRepoBestEffort -HostPath $hostKernelPower -RepoPath $repoKernelPower
    wrapper_error = Sync-ToRepoBestEffort -HostPath $hostWrapperError -RepoPath $repoWrapperError
}

$probeErrorPresent = $false
if (Test-Path $hostTxt) {
    $probeErrorPresent = [bool](Select-String -Path $hostTxt -Pattern '^ERROR=' -Quiet)
}

$guestSummary = $null
if (Test-Path $hostGuestSummary) {
    $guestSummary = Get-Content -Path $hostGuestSummary -Raw | ConvertFrom-Json
}

$shellAfter = $null
$needsRecovery = $false
try {
    $shellAfter = Wait-ShellHealthy -TimeoutSeconds 180
}
catch {
    $needsRecovery = $true
}

if ($needsRecovery) {
    Restore-HealthySnapshot
    $shellAfter = Wait-ShellHealthy -TimeoutSeconds 180
}

if ($copied.pml) {
    @(
        '# External Evidence Placeholder',
        '',
        'Title: power.session-watchdog-timeouts S1 Procmon transition probe',
        '',
        'The raw Procmon PML for this lane is not committed here. Use the summary JSON, the companion text exports, and any filtered hits CSV in the same folder.'
    ) -join "`n" | Set-Content -Path $repoPmlPlaceholder -Encoding UTF8
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    lane_label = $recordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    shell_before = $shellBefore
    shell_after = $shellAfter
    tools_transitions = @($toolsTransitions)
    vmrun_exit_code = if ($proc.HasExited) { $proc.ExitCode } else { $null }
    timed_out = $timedOut
    copied = $copied
    repo_copied = $repoCopied
    guest_summary = $guestSummary
    probe_error_present = $probeErrorPresent
    wrapper_error_present = [bool](Test-Path $hostWrapperError)
    status = if ($timedOut) { 'timed-out' } elseif (Test-Path $hostWrapperError) { 'guest-wrapper-error' } elseif ($probeErrorPresent) { 'guest-probe-error' } elseif ($guestSummary -and $guestSummary.match_count -gt 0) { 'hits-found' } elseif ($guestSummary) { 'no-hits' } else { 'copy-incomplete' }
}

$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $repoSummaryPath -Encoding UTF8

Write-Output $repoSummaryPath
if ($timedOut) {
    exit 1
}
