[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BundlePath,

    [string]$OutputRoot = '',

    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',

    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [int]$DebuggerAttachLeadSeconds = 10,
    [int]$ShellHealthTimeoutSeconds = 900,
    [int]$PostShellSettleSeconds = 20,
    [int]$PollIntervalSeconds = 10
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\vm\_vmrun-common.ps1')

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$InputObject,
        [int]$Depth = 10
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

function Get-RepoDisplayPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $full = [System.IO.Path]::GetFullPath($Path)
    if ($full.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/')
    }

    return $full
}

function New-ArtifactRef {
    param([string]$Path)

    $display = Get-RepoDisplayPath -Path $Path
    $full = [System.IO.Path]::GetFullPath($Path)
    $item = if (Test-Path -LiteralPath $full) { Get-Item -LiteralPath $full } else { $null }

    return [ordered]@{
        path = $display
        sha256 = if ($item) { (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
        size = if ($item) { [int64]$item.Length } else { $null }
        collected_utc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        exists = [bool]$item
    }
}

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    return Invoke-RegProbeVmrun -VmrunPath $VmrunPath -Arguments $Arguments -IgnoreExitCode:$IgnoreExitCode
}

function Test-VmRunning {
    param([string]$VmPath)

    $listOutput = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') -IgnoreExitCode
    return ($listOutput -match [regex]::Escape($VmPath))
}

function Get-VmToolsState {
    param([string]$VmPath)

    try {
        return (Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath) -IgnoreExitCode).Trim()
    }
    catch {
        return 'query-failed'
    }
}

function Get-ShellHealthSnapshot {
    param(
        [string]$VmPath,
        [string[]]$GuestAuthArgs
    )

    $processText = ''
    $processQueryError = $null

    try {
        $processText = Invoke-Vmrun -Arguments (@('-T', 'ws') + $GuestAuthArgs + @('listProcessesInGuest', $VmPath))
    }
    catch {
        $processQueryError = $_.Exception.Message
    }

    $checks = [ordered]@{
        explorer = [bool]($processText -match '\bexplorer\.exe\b')
        sihost = [bool]($processText -match '\bsihost\.exe\b')
        shellhost = [bool]($processText -match '\bShellHost\.exe\b')
        ctfmon = [bool]($processText -match '\bctfmon\.exe\b')
    }

    return [ordered]@{
        process_query_error = $processQueryError
        shell_healthy = ($checks.explorer -and $checks.sihost -and $checks.shellhost)
        checks = $checks
    }
}

function Wait-ForVmToolsReady {
    param([string]$VmPath)

    $deadline = (Get-Date).AddSeconds([Math]::Max(60, $ShellHealthTimeoutSeconds))
    do {
        if (-not (Test-VmRunning -VmPath $VmPath)) {
            Start-Sleep -Seconds $PollIntervalSeconds
            continue
        }

        $toolsState = Get-VmToolsState -VmPath $VmPath
        if ($toolsState -match 'running|installed') {
            return $toolsState
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for VMware Tools readiness after $ShellHealthTimeoutSeconds seconds."
}

function Wait-ForShellHealthy {
    param(
        [string]$VmPath,
        [string[]]$GuestAuthArgs
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(60, $ShellHealthTimeoutSeconds))
    do {
        $snapshot = Get-ShellHealthSnapshot -VmPath $VmPath -GuestAuthArgs $GuestAuthArgs
        if ($snapshot.shell_healthy) {
            return $snapshot
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for shell health after $ShellHealthTimeoutSeconds seconds."
}

function Request-GuestRestart {
    param(
        [string]$VmPath,
        [string[]]$GuestAuthArgs
    )

    Invoke-Vmrun -Arguments (@('-T', 'ws') + $GuestAuthArgs + @('runProgramInGuest', $VmPath, 'C:\Windows\System32\shutdown.exe', '/r', '/t', '0', '/f')) -IgnoreExitCode | Out-Null
}

$bundleFullPath = [System.IO.Path]::GetFullPath($BundlePath)
if (-not (Test-Path -LiteralPath $bundleFullPath)) {
    throw "WinDbg bundle not found: $bundleFullPath"
}

$bundle = Get-Content -LiteralPath $bundleFullPath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace([string]$bundle.windbg_path) -or -not (Test-Path -LiteralPath $bundle.windbg_path)) {
    throw 'WinDbg/kd binary is not available for execution.'
}
if ([string]::IsNullOrWhiteSpace([string]$bundle.command_script)) {
    throw 'Bundle does not contain a command script reference.'
}

$guestCredential = Resolve-RegProbeVmCredential -GuestUser $GuestUser -GuestPassword $GuestPassword -CredentialFilePath $CredentialFilePath
$guestAuthArgs = Get-RegProbeVmrunAuthArguments -Credential $guestCredential
$vmPath = [string]$bundle.vm_path
$commandScriptPath = Join-Path $repoRoot ($bundle.command_script -replace '/', '\')
$hostDebuggerPath = [string]$bundle.windbg_path
$keys = @($bundle.keys)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$sessionId = "windbg-boot-registry-trace-$timestamp"
$sessionRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    Join-Path $repoRoot "evidence\files\vm-tooling-staging\$sessionId"
}
else {
    Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) $sessionId
}

New-Item -ItemType Directory -Path $sessionRoot -Force | Out-Null
$consoleLogPath = Join-Path $sessionRoot 'kd-console.log'
$traceLogPath = Join-Path $sessionRoot 'windbg-registry-trace.log'
$sessionCommandScriptPath = Join-Path $sessionRoot 'windbg-session-script.txt'
$summaryPath = Join-Path $sessionRoot 'summary.json'
$resultsPath = Join-Path $sessionRoot 'results.json'
$sessionPath = Join-Path $sessionRoot 'session.json'

$session = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    session_id = $sessionId
    collection_mode = $CollectionMode
    vm_path = $vmPath
    debugger_path = $hostDebuggerPath
    command_script = Get-RepoDisplayPath -Path $sessionCommandScriptPath
    console_log = Get-RepoDisplayPath -Path $consoleLogPath
    raw_log = Get-RepoDisplayPath -Path $traceLogPath
    status = 'starting'
    steps = @()
}

$commandLines = @(
    Get-Content -LiteralPath $commandScriptPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith('$$') }
)
$escapedTraceLogPath = $traceLogPath.Replace('\', '\\')
$rewroteLogOpen = $false
$rewrittenCommandLines = foreach ($line in $commandLines) {
    if ($line -match '^\.logopen\s+/t\s+') {
        $rewroteLogOpen = $true
        '.logopen /t "{0}"' -f $escapedTraceLogPath
    }
    else {
        $line
    }
}
if (-not $rewroteLogOpen) {
    $rewrittenCommandLines = @($rewrittenCommandLines) + @('.logopen /t "{0}"' -f $escapedTraceLogPath)
}
Set-Content -LiteralPath $sessionCommandScriptPath -Value $rewrittenCommandLines -Encoding ASCII

$kdArgs = @(
    '-kqm',
    '-k', ("com:pipe,port={0},resets=0,reconnect" -f ([string]$bundle.pipe_name)),
    '-cf', $sessionCommandScriptPath,
    '-logo', $consoleLogPath
)

$kdProcess = $null
$shellSnapshot = $null
$runError = $null
$shellRecovered = $false

try {
    $kdProcess = Start-Process -FilePath $hostDebuggerPath -ArgumentList $kdArgs -WindowStyle Hidden -PassThru
    $session.status = 'debugger-started'
    $session.steps += 'debugger-started'
    Write-JsonFile -Path $sessionPath -InputObject $session

    Start-Sleep -Seconds ([Math]::Max(3, $DebuggerAttachLeadSeconds))

    Request-GuestRestart -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
    $session.status = 'guest-restart-requested'
    $session.steps += 'guest-restart-requested'
    Write-JsonFile -Path $sessionPath -InputObject $session

    Start-Sleep -Seconds 10
    [void](Wait-ForVmToolsReady -VmPath $vmPath)
    $session.steps += 'vmtools-ready-after-restart'
    Write-JsonFile -Path $sessionPath -InputObject $session

    $shellSnapshot = Wait-ForShellHealthy -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
    $shellRecovered = $true
    $session.status = 'shell-healthy'
    $session.steps += 'shell-healthy-after-restart'
    $session.shell_checks = $shellSnapshot.checks
    Write-JsonFile -Path $sessionPath -InputObject $session

    Start-Sleep -Seconds ([Math]::Max(5, $PostShellSettleSeconds))
}
catch {
    $runError = $_.Exception.Message
}
finally {
    if ($kdProcess -and -not $kdProcess.HasExited) {
        Stop-Process -Id $kdProcess.Id -Force -ErrorAction SilentlyContinue
        $kdProcess.WaitForExit()
    }
}

$analysisLogPath = if (Test-Path -LiteralPath $traceLogPath) { $traceLogPath } else { $consoleLogPath }
$analysisSource = if (Test-Path -LiteralPath $traceLogPath) { 'script-log' } elseif (Test-Path -LiteralPath $consoleLogPath) { 'console-log-fallback' } else { 'missing' }
$analysisText = if (Test-Path -LiteralPath $analysisLogPath) { Get-Content -LiteralPath $analysisLogPath -Raw -ErrorAction SilentlyContinue } else { '' }
$perKeyResults = @()
$totalHitKeys = 0
if (Test-Path -LiteralPath $analysisLogPath) {
    foreach ($key in $keys) {
        $matches = @(Select-String -LiteralPath $analysisLogPath -Pattern $key -SimpleMatch)
        if ($matches.Count -gt 0) {
            $totalHitKeys++
        }

        $perKeyResults += [ordered]@{
            key = $key
            hit_count = $matches.Count
            sample_lines = @($matches | Select-Object -First 3 | ForEach-Object { $_.Line.Trim() })
        }
    }
}

$fatalSystemErrorObserved = $analysisText -match 'A fatal system error has occurred'
$kernelConnected = $analysisText -match 'Kernel Debugger connection established'
$resultStatus = if ($runError -and $fatalSystemErrorObserved) {
    'boot-unsafe'
}
elseif ($runError) {
    'trace-error'
}
elseif (-not (Test-Path -LiteralPath $analysisLogPath)) {
    'missing-log'
}
elseif ($analysisSource -eq 'console-log-fallback' -and $totalHitKeys -eq 0) {
    'connected-no-target-hit'
}
elseif ($totalHitKeys -gt 0) {
    'hit-detected'
}
else {
    'no-hit'
}

$results = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    family = 'power-control'
    lane = 'windbg-boot-registry-trace'
    status = $resultStatus
    collection_mode = $CollectionMode
    key_count = @($keys).Count
    hit_key_count = $totalHitKeys
    analysis_source = $analysisSource
    kernel_connected = $kernelConnected
    fatal_system_error_observed = $fatalSystemErrorObserved
    shell_recovered = $shellRecovered
    error = $runError
    keys = $perKeyResults
    shell_checks = if ($shellSnapshot) { $shellSnapshot.checks } else { $null }
}

$captureArtifacts = @()
foreach ($artifactPath in @($copiedTraceLogPath, $consoleLogPath, $resultsPath)) {
    if ($artifactPath -eq $copiedTraceLogPath) {
        continue
    }
}

$captureArtifactPaths = @($traceLogPath, $consoleLogPath, $resultsPath, $sessionCommandScriptPath)
foreach ($artifactPath in $captureArtifactPaths) {
    if (Test-Path -LiteralPath $artifactPath) {
        $captureArtifacts += @(New-ArtifactRef -Path $artifactPath)
    }
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    family = 'power-control'
    lane = 'windbg-boot-registry-trace'
    status = $resultStatus
    capture_status = if ($captureArtifacts.Count -gt 0) { 'captured' } else { 'missing-capture' }
    collection_mode = $CollectionMode
    vm_path = $vmPath
    debugger_path = $hostDebuggerPath
    key_count = @($keys).Count
    hit_key_count = $totalHitKeys
    analysis_source = $analysisSource
    kernel_connected = $kernelConnected
    fatal_system_error_observed = $fatalSystemErrorObserved
    shell_recovered = $shellRecovered
    command_script = Get-RepoDisplayPath -Path $sessionCommandScriptPath
    capture_artifacts = $captureArtifacts
    bundle_ref = Get-RepoDisplayPath -Path $bundleFullPath
    note = 'Boot trace executed through kd over the configured VMware serial pipe.'
}
if ($runError) {
    $summary.error = $runError
}

Write-JsonFile -Path $resultsPath -InputObject $results
$summary.capture_artifacts = @(
    $summary.capture_artifacts + @(New-ArtifactRef -Path $resultsPath)
)
Write-JsonFile -Path $summaryPath -InputObject $summary

$session.status = $resultStatus
$session.steps += 'trace-finished'
$session.summary_path = Get-RepoDisplayPath -Path $summaryPath
$session.results_path = Get-RepoDisplayPath -Path $resultsPath
if ($runError) {
    $session.error = $runError
}
Write-JsonFile -Path $sessionPath -InputObject $session

$summary
