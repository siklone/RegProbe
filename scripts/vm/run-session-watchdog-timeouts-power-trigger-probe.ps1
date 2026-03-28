[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon',
    [string]$SnapshotName = 'baseline-20260327-regprobe-visible-shell-stable'
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "watchdog-power-trigger-$stamp"
$recordId = 'power.session-watchdog-timeouts'
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$guestProbeScriptPath = Join-Path $GuestScriptRoot 'registry-policy-probe.ps1'
$hostWrapperPath = Join-Path $hostRoot 'watchdog-power-trigger-wrapper.ps1'
$guestWrapperPath = Join-Path $GuestScriptRoot 'watchdog-power-trigger-wrapper.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$probeScript = Join-Path $PSScriptRoot 'registry-policy-probe.ps1'
$prefix = 'watchdog-power-trigger'
$repoTxt = Join-Path $repoRootOut "$prefix.txt"
$repoHitsCsv = Join-Path $repoRootOut "$prefix.hits.csv"
$repoTasklist = Join-Path $repoRootOut "$prefix-tasklist.txt"
$repoPowercfgA = Join-Path $repoRootOut "$prefix-powercfg-a.txt"
$repoScPower = Join-Path $repoRootOut "$prefix-sc-power.txt"
$repoPmlPlaceholder = Join-Path $repoRootOut "$prefix.pml.md"
$repoWrapperError = Join-Path $repoRootOut "$prefix-wrapper-error.txt"
$guestPml = Join-Path $guestRoot "$prefix.pml"
$guestCsv = Join-Path $guestRoot "$prefix.csv"
$guestHitsCsv = Join-Path $guestRoot "$prefix.hits.csv"
$guestTxt = Join-Path $guestRoot "$prefix.txt"
$guestTasklist = Join-Path $guestRoot "$prefix-tasklist.txt"
$guestPowercfgQ = Join-Path $guestRoot "$prefix-powercfg-q.txt"
$guestPowercfgA = Join-Path $guestRoot "$prefix-powercfg-a.txt"
$guestScPower = Join-Path $guestRoot "$prefix-sc-power.txt"
$guestWrapperError = Join-Path $guestRoot "$prefix-wrapper-error.txt"

$hostPml = Join-Path $hostRoot "$prefix.pml"
$hostCsv = Join-Path $hostRoot "$prefix.csv"
$hostHitsCsv = Join-Path $hostRoot "$prefix.hits.csv"
$hostTxt = Join-Path $hostRoot "$prefix.txt"
$hostTasklist = Join-Path $hostRoot "$prefix-tasklist.txt"
$hostPowercfgQ = Join-Path $hostRoot "$prefix-powercfg-q.txt"
$hostPowercfgA = Join-Path $hostRoot "$prefix-powercfg-a.txt"
$hostScPower = Join-Path $hostRoot "$prefix-sc-power.txt"
$hostWrapperError = Join-Path $hostRoot "$prefix-wrapper-error.txt"

@"
`$ErrorActionPreference = 'Continue'

`$probeScript = '$guestProbeScriptPath'
`$guestRoot = '$guestRoot'
`$prefix = '$prefix'
`$tasklist = '$guestTasklist'
`$powercfgQ = '$guestPowercfgQ'
`$powercfgA = '$guestPowercfgA'
`$scPower = '$guestScPower'
`$wrapperError = '$guestWrapperError'

try {
    if (-not (Test-Path `$guestRoot)) {
        New-Item -ItemType Directory -Force -Path `$guestRoot | Out-Null
    }

    `$trigger = @(
        ('cmd /c tasklist /svc > "{0}"' -f `$tasklist),
        ('cmd /c powercfg /q > "{0}"' -f `$powercfgQ),
        ('cmd /c powercfg /a > "{0}"' -f `$powercfgA),
        ('cmd /c sc queryex Power > "{0}"' -f `$scPower),
        'Start-Sleep -Seconds 5'
    ) -join '; '

    & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -File `$probeScript -Mode capture -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power' -ValueName 'WatchdogSleepTimeout' -Prefix `$prefix -OutputDirectory `$guestRoot -PowerShellCommand `$trigger -MatchFragments 'WatchdogResumeTimeout','WatchdogSleepTimeout','PowerSettingProfile','SystemPowerPolicy','ShutdownOccurred' -ProcessNames 'System','svchost.exe','powercfg.exe','services.exe'
}
catch {
    @(
        ('ERROR=' + `$_.Exception.GetType().FullName + ': ' + `$_.Exception.Message),
        ('AT=' + `$_.InvocationInfo.PositionMessage)
    ) | Set-Content -Path `$wrapperError -Encoding UTF8
}

exit 0
"@ | Set-Content -Path $hostWrapperPath -Encoding UTF8

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

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'createDirectoryInGuest', $VmPath, $GuestScriptRoot
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $probeScript, $guestProbeScriptPath
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $hostWrapperPath, $guestWrapperPath
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'createDirectoryInGuest', $VmPath, $guestRoot
) | Out-Null

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
    powercfg_q = Copy-FromGuestBestEffort -GuestPath $guestPowercfgQ -HostPath $hostPowercfgQ
    powercfg_a = Copy-FromGuestBestEffort -GuestPath $guestPowercfgA -HostPath $hostPowercfgA
    sc_power = Copy-FromGuestBestEffort -GuestPath $guestScPower -HostPath $hostScPower
    wrapper_error = Copy-FromGuestBestEffort -GuestPath $guestWrapperError -HostPath $hostWrapperError
}

$repoCopied = [ordered]@{
    txt = Sync-ToRepoBestEffort -HostPath $hostTxt -RepoPath $repoTxt
    hits = Sync-ToRepoBestEffort -HostPath $hostHitsCsv -RepoPath $repoHitsCsv
    tasklist = Sync-ToRepoBestEffort -HostPath $hostTasklist -RepoPath $repoTasklist
    powercfg_a = Sync-ToRepoBestEffort -HostPath $hostPowercfgA -RepoPath $repoPowercfgA
    sc_power = Sync-ToRepoBestEffort -HostPath $hostScPower -RepoPath $repoScPower
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
        'Title: power.session-watchdog-timeouts targeted post-boot Procmon trigger',
        '',
        'The raw Procmon PML for this lane is not committed here. Use the summary JSON, the filtered hits CSV, and the companion text exports in the same folder.'
    ) -join "`n" | Set-Content -Path $repoPmlPlaceholder -Encoding UTF8
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    lane_label = $recordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    dcomlaunch_trigger_context = [ordered]@{
        service_group = 'DcomLaunch'
        service_hint = 'Power'
        source = 'evidence/files/vm-tooling-staging/watchdog-dcomlaunch-attribution-20260328/summary.json'
    }
    registry_path = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power'
    trigger = 'tasklist /svc + powercfg /q + powercfg /a + sc queryex Power'
    shell_before = $shellBefore
    shell_after = $shellAfter
    copied = $copied
    repo_copied = $repoCopied
    hit_count = @($hits).Count
    hit_processes = @($hits | Select-Object -ExpandProperty 'Process Name' -Unique)
    hit_paths = @($hits | Select-Object -ExpandProperty Path -Unique)
    probe_error_present = $probeErrorPresent
    wrapper_error_present = [bool](Test-Path $hostWrapperError)
    status = if (@($hits).Count -gt 0) { 'hits-found' } elseif (Test-Path $hostWrapperError) { 'guest-wrapper-error' } elseif ($probeErrorPresent) { 'guest-probe-error' } elseif ($copied.txt) { 'no-hits' } else { 'copy-incomplete' }
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSummaryPath -Encoding UTF8

Write-Output $repoSummaryPath
