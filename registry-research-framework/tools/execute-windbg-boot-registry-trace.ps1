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
    [ValidateSet('full', 'minimal', 'symbols', 'attach-only', 'breakin-once', 'breakin-twice', 'breakin-delayed-10', 'breakin-delayed-30', 'roundtrip-once', 'probe', 'firsthit', 'symbolsearch', 'valuenames', 'conditional', 'singlekey-smoke', 'singlekey-firsthit', 'singlekey-rawbounded')]
    [string]$TraceProfile = 'full',
    [int]$DebuggerAttachLeadSeconds = 10,
    [int]$ShellHealthTimeoutSeconds = 900,
    [int]$PostShellSettleSeconds = 20,
    [int]$PollIntervalSeconds = 10,
    [int]$NoiseBudgetBytes = 262144,
    [int]$NoiseBudgetEventLimit = 100
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\vm\_vmrun-common.ps1')
. (Join-Path $PSScriptRoot '_lane-manifest-lib.ps1')

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

function Resolve-TraceLogPath {
    param([string]$PreferredPath)

    if ([string]::IsNullOrWhiteSpace($PreferredPath)) {
        return $null
    }

    $preferredFullPath = [System.IO.Path]::GetFullPath($PreferredPath)
    if (Test-Path -LiteralPath $preferredFullPath) {
        return $preferredFullPath
    }

    $directory = Split-Path -Parent $preferredFullPath
    if (-not (Test-Path -LiteralPath $directory)) {
        return $preferredFullPath
    }

    $stem = [System.IO.Path]::GetFileNameWithoutExtension($preferredFullPath)
    $extension = [System.IO.Path]::GetExtension($preferredFullPath)
    $match = Get-ChildItem -LiteralPath $directory -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like ("{0}*{1}" -f $stem, $extension) } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($match) {
        return $match.FullName
    }

    return $preferredFullPath
}

function Get-ConditionalHitLines {
    param([string[]]$Lines)

    $capturing = $false
    $capturedLines = New-Object System.Collections.Generic.List[string]
    foreach ($line in @($Lines)) {
        if ($line -match 'REGPROBE_KEY_HIT_BEGIN') {
            $capturing = $true
            continue
        }

        if ($line -match 'REGPROBE_KEY_HIT_END') {
            $capturing = $false
            continue
        }

        if ($capturing) {
            $capturedLines.Add($line.Trim()) | Out-Null
        }
    }

    return @($capturedLines)
}

function Get-MarkerBlocks {
    param(
        [string[]]$Lines,
        [string]$BeginMarker,
        [string]$EndMarker
    )

    $blocks = New-Object System.Collections.Generic.List[string]
    $capturing = $false
    $current = New-Object System.Collections.Generic.List[string]
    foreach ($line in @($Lines)) {
        if ($line -match [regex]::Escape($BeginMarker)) {
            $capturing = $true
            $current = New-Object System.Collections.Generic.List[string]
            continue
        }

        if ($line -match [regex]::Escape($EndMarker)) {
            if ($capturing) {
                $blocks.Add(([string]::Join([Environment]::NewLine, @($current)))) | Out-Null
            }
            $capturing = $false
            continue
        }

        if ($capturing) {
            $current.Add($line.Trim()) | Out-Null
        }
    }

    return @($blocks)
}

function ConvertTo-QuotedArgumentString {
    param([string[]]$Arguments)

    $quoted = foreach ($argument in @($Arguments)) {
        if ($null -eq $argument) {
            '""'
            continue
        }

        if ($argument -match '[\s"]') {
            '"' + ($argument -replace '"', '\"') + '"'
        }
        else {
            $argument
        }
    }

    return [string]::Join(' ', @($quoted))
}

function Start-DebuggerProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$RedirectStandardInput
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $FilePath
    $startInfo.Arguments = ConvertTo-QuotedArgumentString -Arguments $Arguments
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardInput = $RedirectStandardInput.IsPresent

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    [void]$process.Start()
    return $process
}

function Get-BreakinProfileSpec {
    param([string]$Profile)

    switch ($Profile) {
        'breakin-once' {
            return [ordered]@{
                breakin_count = 1
                initial_delay_seconds = 0
            }
        }
        'breakin-twice' {
            return [ordered]@{
                breakin_count = 2
                initial_delay_seconds = 0
            }
        }
        'breakin-delayed-10' {
            return [ordered]@{
                breakin_count = 1
                initial_delay_seconds = 10
            }
        }
        'breakin-delayed-30' {
            return [ordered]@{
                breakin_count = 1
                initial_delay_seconds = 30
            }
        }
        default {
            return $null
        }
    }
}

function Send-DebuggerCommand {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)]
        [string]$Command
    )

    if ($Process.HasExited) {
        throw 'Debugger process exited before command dispatch.'
    }

    $Process.StandardInput.WriteLine($Command)
    $Process.StandardInput.Flush()
}

function Invoke-DebuggerBreakinProfile {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)]
        [string]$Profile
    )

    $spec = Get-BreakinProfileSpec -Profile $Profile
    if (-not $spec) {
        return $null
    }

    if ($spec.initial_delay_seconds -gt 0) {
        Start-Sleep -Seconds $spec.initial_delay_seconds
    }

    $attempts = @()
    for ($index = 1; $index -le [int]$spec.breakin_count; $index++) {
        $attempts += [pscustomobject]@{
            index = $index
            requested_utc = [DateTime]::UtcNow.ToString('o')
        }
        Send-DebuggerCommand -Process $Process -Command ('.echo REGPROBE_BREAKIN_REQUEST|{0}' -f $index)
        Send-DebuggerCommand -Process $Process -Command '.breakin'
        Start-Sleep -Seconds 2
        Send-DebuggerCommand -Process $Process -Command ('.echo REGPROBE_BREAKIN_OK|{0}' -f $index)
        Send-DebuggerCommand -Process $Process -Command 'g'
        if ($index -lt [int]$spec.breakin_count) {
            Start-Sleep -Seconds 3
        }
    }

    return [ordered]@{
        profile = $Profile
        breakin_count = [int]$spec.breakin_count
        initial_delay_seconds = [int]$spec.initial_delay_seconds
        attempts = @($attempts)
    }
}

function Get-TraceObservation {
    param(
        [string]$PreferredTraceLogPath,
        [string]$ConsoleLogPath,
        [string]$TargetKey = ''
    )

    $resolvedTraceLogPath = Resolve-TraceLogPath -PreferredPath $PreferredTraceLogPath
    $analysisLogPath = if (Test-Path -LiteralPath $resolvedTraceLogPath) { $resolvedTraceLogPath } elseif (Test-Path -LiteralPath $ConsoleLogPath) { $ConsoleLogPath } else { $null }
    $analysisSource = if (Test-Path -LiteralPath $resolvedTraceLogPath) { 'script-log' } elseif (Test-Path -LiteralPath $ConsoleLogPath) { 'console-log-fallback' } else { 'missing' }
    $analysisLines = if ($analysisLogPath) { @(Get-Content -LiteralPath $analysisLogPath -ErrorAction SilentlyContinue) } else { @() }
    $analysisText = if ($analysisLines.Count -gt 0) { [string]::Join([Environment]::NewLine, $analysisLines) } else { '' }
    $consoleText = if (Test-Path -LiteralPath $ConsoleLogPath) { Get-Content -LiteralPath $ConsoleLogPath -Raw -ErrorAction SilentlyContinue } else { '' }
    $rawValueBlocks = Get-MarkerBlocks -Lines $analysisLines -BeginMarker 'REGPROBE_VALUE_BEGIN' -EndMarker 'REGPROBE_VALUE_END'
    $firstHitBlocks = Get-MarkerBlocks -Lines $analysisLines -BeginMarker 'REGPROBE_FIRSTHIT_BEGIN' -EndMarker 'REGPROBE_FIRSTHIT_END'
    $conditionalHitLines = Get-ConditionalHitLines -Lines $analysisLines
    $breakinRequestLines = @($analysisLines | Where-Object { $_ -match 'REGPROBE_BREAKIN_REQUEST\|' })
    $breakinSuccessLines = @($analysisLines | Where-Object { $_ -match 'REGPROBE_BREAKIN_OK\|' })
    $roundtripRequestLines = @($analysisLines | Where-Object { $_ -match 'REGPROBE_ROUNDTRIP_REQUEST\|' })
    $roundtripSuccessLines = @($analysisLines | Where-Object { $_ -match 'REGPROBE_ROUNDTRIP_OK\|' })
    $restartBoundaryIndex = -1
    for ($lineIndex = 0; $lineIndex -lt $analysisLines.Count; $lineIndex++) {
        if ($analysisLines[$lineIndex] -match 'Shutdown occurred at|Waiting to reconnect\.\.\.') {
            $restartBoundaryIndex = $lineIndex
            break
        }
    }
    $postRestartLines = if ($restartBoundaryIndex -ge 0 -and $restartBoundaryIndex -lt ($analysisLines.Count - 1)) {
        @($analysisLines[($restartBoundaryIndex + 1)..($analysisLines.Count - 1)])
    }
    else {
        @()
    }
    $postRestartRoundtripRequestLines = @($postRestartLines | Where-Object { $_ -match 'REGPROBE_ROUNDTRIP_REQUEST\|' })
    $postRestartRoundtripSuccessLines = @($postRestartLines | Where-Object { $_ -match 'REGPROBE_ROUNDTRIP_OK\|' })
    $targetMatches = @()
    if (-not [string]::IsNullOrWhiteSpace($TargetKey)) {
        $targetMatches = @($rawValueBlocks | Where-Object { $_ -like "*$TargetKey*" })
        if ($conditionalHitLines.Count -gt 0) {
            $targetMatches += @($conditionalHitLines | Where-Object { $_ -like "*$TargetKey*" })
        }
    }

    return [ordered]@{
        resolved_trace_log_path = $resolvedTraceLogPath
        analysis_log_path = $analysisLogPath
        analysis_source = $analysisSource
        analysis_lines = $analysisLines
        analysis_text = $analysisText
        console_text = $consoleText
        windbg_log_detected = [bool]$analysisLogPath
        log_size_bytes = if ($analysisLogPath -and (Test-Path -LiteralPath $analysisLogPath)) { [int64](Get-Item -LiteralPath $analysisLogPath).Length } else { 0 }
        no_debuggee_waiting = (
            ($analysisText -match 'Kernel Debug Target Status:\s+\[no_debuggee\]') -or
            ($consoleText -match 'Kernel Debug Target Status:\s+\[no_debuggee\]') -or
            (
                (($analysisText -match 'Waiting to reconnect\.\.\.') -or ($consoleText -match 'Waiting to reconnect\.\.\.')) -and
                -not (($analysisText -match 'Kernel Debugger connection established') -or ($consoleText -match 'Kernel Debugger connection established'))
            )
        )
        raw_value_blocks = $rawValueBlocks
        raw_value_event_count = @($rawValueBlocks).Count
        firsthit_observed = @($firstHitBlocks).Count -gt 0
        firsthit_blocks = $firstHitBlocks
        breakin_request_count = @($breakinRequestLines).Count
        breakin_success_count = @($breakinSuccessLines).Count
        breakin_request_lines = @($breakinRequestLines)
        breakin_success_lines = @($breakinSuccessLines)
        roundtrip_request_count = @($roundtripRequestLines).Count
        roundtrip_success_count = @($roundtripSuccessLines).Count
        roundtrip_request_lines = @($roundtripRequestLines)
        roundtrip_success_lines = @($roundtripSuccessLines)
        post_restart_roundtrip_request_count = @($postRestartRoundtripRequestLines).Count
        post_restart_roundtrip_success_count = @($postRestartRoundtripSuccessLines).Count
        post_restart_roundtrip_request_lines = @($postRestartRoundtripRequestLines)
        post_restart_roundtrip_success_lines = @($postRestartRoundtripSuccessLines)
        conditional_hit_lines = $conditionalHitLines
        host_filtered_hits = @($targetMatches).Count
        host_filtered_samples = @($targetMatches | Select-Object -First 3)
        script_execution_observed = (($analysisSource -eq 'script-log' -and -not [string]::IsNullOrWhiteSpace($analysisText)) -or ($analysisText -match 'REGPROBE_'))
        transport_error = ($analysisText -match 'HOST cannot communicate with the TARGET|Failed to write breakin packet|Unable to get target version info') -or ($consoleText -match 'HOST cannot communicate with the TARGET|Failed to write breakin packet|Unable to get target version info')
        breakpoint_unresolved = ($analysisText -match 'Could not resolve|deferred bp could not be resolved|unresolved breakpoint|Breakpoint [0-9]+ could not be resolved')
        parser_invalid = ($analysisText -match 'Syntax error|Extra character error|Bad register|Couldn''t parse')
        kernel_connected = ($analysisText -match 'Kernel Debugger connection established') -or ($consoleText -match 'Kernel Debugger connection established')
        fatal_system_error_observed = ($analysisText -match 'A fatal system error has occurred') -or ($consoleText -match 'A fatal system error has occurred')
    }
}

function Wait-ForBreakinOutcome {
    param(
        [string]$PreferredTraceLogPath,
        [string]$ConsoleLogPath,
        [string]$TargetKey = '',
        [int]$ExpectedBreakinCount = 1,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(10, $TimeoutSeconds))
    do {
        $observation = Get-TraceObservation -PreferredTraceLogPath $PreferredTraceLogPath -ConsoleLogPath $ConsoleLogPath -TargetKey $TargetKey
        if ($observation.transport_error -or $observation.breakin_success_count -ge $ExpectedBreakinCount) {
            return $observation
        }

        Start-Sleep -Seconds ([Math]::Min($PollIntervalSeconds, 5))
    } while ((Get-Date) -lt $deadline)

    return (Get-TraceObservation -PreferredTraceLogPath $PreferredTraceLogPath -ConsoleLogPath $ConsoleLogPath -TargetKey $TargetKey)
}

function Get-RoundtripProfileSpec {
    param([string]$Profile)

    switch ($Profile) {
        'roundtrip-once' {
            return [ordered]@{
                roundtrip_count = 1
            }
        }
        default {
            return $null
        }
    }
}

function Wait-ForRoundtripOutcome {
    param(
        [string]$PreferredTraceLogPath,
        [string]$ConsoleLogPath,
        [string]$TargetKey = '',
        [int]$ExpectedRoundtripCount = 1,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(10, $TimeoutSeconds))
    do {
        $observation = Get-TraceObservation -PreferredTraceLogPath $PreferredTraceLogPath -ConsoleLogPath $ConsoleLogPath -TargetKey $TargetKey
        if ($observation.transport_error -or $observation.roundtrip_success_count -ge $ExpectedRoundtripCount) {
            return $observation
        }

        Start-Sleep -Seconds ([Math]::Min($PollIntervalSeconds, 5))
    } while ((Get-Date) -lt $deadline)

    return (Get-TraceObservation -PreferredTraceLogPath $PreferredTraceLogPath -ConsoleLogPath $ConsoleLogPath -TargetKey $TargetKey)
}

function Wait-ForDebuggerAwareBoot {
    param(
        [string]$VmPath,
        [string[]]$GuestAuthArgs,
        [string]$PreferredTraceLogPath,
        [string]$ConsoleLogPath,
        [string]$TargetKey = '',
        [switch]$EnableNoiseBudget
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(60, $ShellHealthTimeoutSeconds))
    $toolsState = $null
    $shellSnapshot = $null
    $observation = $null
    $noiseBudgetExceeded = $false

    do {
        $observation = Get-TraceObservation -PreferredTraceLogPath $PreferredTraceLogPath -ConsoleLogPath $ConsoleLogPath -TargetKey $TargetKey
        if ($EnableNoiseBudget -and (
                (($NoiseBudgetBytes -gt 0) -and ($observation.log_size_bytes -ge $NoiseBudgetBytes)) -or
                (($NoiseBudgetEventLimit -gt 0) -and ($observation.raw_value_event_count -ge $NoiseBudgetEventLimit))
            )) {
            $noiseBudgetExceeded = $true
            break
        }

        $toolsState = Get-VmToolsState -VmPath $VmPath
        if ($toolsState -match 'running|installed') {
            $candidateShellSnapshot = Get-ShellHealthSnapshot -VmPath $VmPath -GuestAuthArgs $GuestAuthArgs
            if ($candidateShellSnapshot.shell_healthy) {
                $shellSnapshot = $candidateShellSnapshot
                break
            }
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    } while ((Get-Date) -lt $deadline)

    if (-not $noiseBudgetExceeded -and [string]::IsNullOrWhiteSpace([string]$toolsState)) {
        throw "Timed out waiting for VMware Tools readiness after $ShellHealthTimeoutSeconds seconds."
    }

    if (-not $noiseBudgetExceeded -and -not $shellSnapshot) {
        throw "Timed out waiting for shell health after $ShellHealthTimeoutSeconds seconds."
    }

    if (-not $noiseBudgetExceeded -and $shellSnapshot) {
        $settleDeadline = (Get-Date).AddSeconds([Math]::Max(0, $PostShellSettleSeconds))
        while ((Get-Date) -lt $settleDeadline) {
            $observation = Get-TraceObservation -PreferredTraceLogPath $PreferredTraceLogPath -ConsoleLogPath $ConsoleLogPath -TargetKey $TargetKey
            if ($EnableNoiseBudget -and (
                    (($NoiseBudgetBytes -gt 0) -and ($observation.log_size_bytes -ge $NoiseBudgetBytes)) -or
                    (($NoiseBudgetEventLimit -gt 0) -and ($observation.raw_value_event_count -ge $NoiseBudgetEventLimit))
                )) {
                $noiseBudgetExceeded = $true
                break
            }

            Start-Sleep -Seconds ([Math]::Min($PollIntervalSeconds, 5))
        }
    }

    return [ordered]@{
        tools_state = $toolsState
        shell_snapshot = $shellSnapshot
        observation = $observation
        noise_budget_exceeded = $noiseBudgetExceeded
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

function Recover-DebugTargetVm {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VmPath,
        [Parameter(Mandatory = $true)]
        [string]$SnapshotName
    )

    $vmDirectory = Split-Path -Parent $VmPath
    $lockPath = Join-Path $vmDirectory ([IO.Path]::GetFileName($VmPath) + '.lck')
    $steps = New-Object System.Collections.Generic.List[string]

    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'hard') -IgnoreExitCode | Out-Null
    Start-Sleep -Seconds 5

    if (-not (Test-VmRunning -VmPath $VmPath) -and (Test-Path -LiteralPath $lockPath)) {
        Remove-Item -LiteralPath $lockPath -Recurse -Force -ErrorAction SilentlyContinue
        $steps.Add('stale-lock-removed') | Out-Null
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    $steps.Add('reverted-to-debug-snapshot') | Out-Null

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') | Out-Null
    $steps.Add('vm-started-nogui') | Out-Null

    [void](Wait-ForVmToolsReady -VmPath $VmPath)
    $shellSnapshot = Wait-ForShellHealthy -VmPath $VmPath -GuestAuthArgs $guestAuthArgs
    $steps.Add('shell-healthy-after-recovery') | Out-Null

    return [ordered]@{
        recovered = $true
        steps = @($steps)
        shell_checks = $shellSnapshot.checks
    }
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
$keys = @($bundle.keys | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
$targetKey = if (-not [string]::IsNullOrWhiteSpace([string]$bundle.target_key)) {
    [string]$bundle.target_key
}
elseif ($keys.Count -eq 1) {
    [string]$keys[0]
}
else {
    $null
}
$breakOnConnectMode = if (-not [string]::IsNullOrWhiteSpace([string]$bundle.break_on_connect_mode)) {
    [string]$bundle.break_on_connect_mode
}
else {
    'bonc'
}
$bootMode = if (-not [string]::IsNullOrWhiteSpace([string]$bundle.boot_mode)) {
    [string]$bundle.boot_mode
}
elseif ($TraceProfile -like 'singlekey-*' -and -not [string]::IsNullOrWhiteSpace([string]$bundle.debug_snapshot_name)) {
    'cold-boot'
}
else {
    'guest-restart'
}
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
    trace_profile = $TraceProfile
    boot_mode = $bootMode
    break_on_connect_mode = $breakOnConnectMode
    target_key = $targetKey
    noise_budget_bytes = $NoiseBudgetBytes
    noise_budget_event_limit = $NoiseBudgetEventLimit
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
$registryWatchSymbol = 'nt!CmQueryValueKey'
$conditionalComparisons = foreach ($key in ($keys | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
    'wcscmp((wchar_t*)poi(@rdx+8), L\"{0}\")==0' -f $key.Replace('\', '\\').Replace('"', '\"')
}
$conditionalExpression = if (@($conditionalComparisons).Count -gt 0) {
    '@@c++(@rdx != 0 && poi(@rdx+8) != 0 && ({0}))' -f ([string]::Join(' || ', @($conditionalComparisons)))
}
else {
    '0'
}
$conditionalBreakpointCommand = 'bs 0 ".if ({0}) {{ .echo REGPROBE_KEY_HIT_BEGIN; du poi(@rdx+8); .echo REGPROBE_KEY_HIT_END; }}; gc"' -f $conditionalExpression
$modeCommandLines = switch ($TraceProfile) {
    'minimal' {
        @(
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_MINIMAL_BEGIN',
            'g'
        )
    }
    'symbols' {
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_SYMBOLS_BEGIN',
            'g'
        )
    }
    'attach-only' {
        @(
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_ATTACH_BEGIN',
            '.echo REGPROBE_ATTACH_READY',
            'g'
        )
    }
    'breakin-once' {
        @(
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_ATTACH_BEGIN',
            '.echo REGPROBE_ATTACH_READY',
            'g'
        )
    }
    'breakin-twice' {
        @(
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_ATTACH_BEGIN',
            '.echo REGPROBE_ATTACH_READY',
            'g'
        )
    }
    'breakin-delayed-10' {
        @(
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_ATTACH_BEGIN',
            '.echo REGPROBE_ATTACH_READY',
            'g'
        )
    }
    'breakin-delayed-30' {
        @(
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_ATTACH_BEGIN',
            '.echo REGPROBE_ATTACH_READY',
            'g'
        )
    }
    'roundtrip-once' {
        @(
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            ('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile),
            '.echo REGPROBE_ATTACH_BEGIN',
            '.echo REGPROBE_ATTACH_READY',
            'g',
            '.echo REGPROBE_ROUNDTRIP_REQUEST|1',
            '.echo REGPROBE_ROUNDTRIP_OK|1',
            'g'
        )
    }
    'probe' {
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_PROBE_BEGIN',
            'bc *',
            ('bu {0}' -f $registryWatchSymbol),
            'bs 0 "gc"',
            'g'
        )
    }
    'firsthit' {
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_FIRSTHIT_BEGIN',
            'bc *',
            ('bu {0}' -f $registryWatchSymbol),
            'bs 0 ".echo REGPROBE_FIRSTHIT_BREAK; r rcx; r rdx; r r8; r r9; dt nt!_UNICODE_STRING @rdx; du poi(@rdx+8); dt nt!_UNICODE_STRING @r9; du poi(@r9+8); .echo REGPROBE_FIRSTHIT_END; bc 0; gc"',
            'g'
        )
    }
    'symbolsearch' {
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_SYMBOLSEARCH_BEGIN',
            'x nt!*CmpQuery*',
            'x nt!*Cm*Query*Key*',
            'x nt!*QueryValueKey*',
            '.echo REGPROBE_SYMBOLSEARCH_END',
            'g'
        )
    }
    'valuenames' {
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_VALUENAMES_BEGIN',
            'bc *',
            ('bu {0}' -f $registryWatchSymbol),
            'bs 0 ".echo REGPROBE_VALUE_BEGIN; du poi(@rdx+8); .echo REGPROBE_VALUE_END; gc"',
            'g'
        )
    }
    'conditional' {
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            '.echo REGPROBE_CONDITIONAL_BEGIN',
            'bc *',
            ('bu {0}' -f $registryWatchSymbol),
            $conditionalBreakpointCommand,
            'g'
        )
    }
    'singlekey-smoke' {
        if ([string]::IsNullOrWhiteSpace($targetKey)) {
            throw 'singlekey-smoke requires a single target key in the bundle.'
        }
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            ('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile),
            ('.echo REGPROBE_TARGET|{0}' -f $targetKey),
            'bc *',
            ('bu {0}' -f $registryWatchSymbol),
            'bs 0 "gc"',
            'g'
        )
    }
    'singlekey-firsthit' {
        if ([string]::IsNullOrWhiteSpace($targetKey)) {
            throw 'singlekey-firsthit requires a single target key in the bundle.'
        }
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            ('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile),
            ('.echo REGPROBE_TARGET|{0}' -f $targetKey),
            'bc *',
            ('bu {0}' -f $registryWatchSymbol),
            'bs 0 ".echo REGPROBE_FIRSTHIT_BEGIN; r rdx; dq @rdx L4; du poi(@rdx+8) L1; .echo REGPROBE_FIRSTHIT_END; qd"',
            'g'
        )
    }
    'singlekey-rawbounded' {
        if ([string]::IsNullOrWhiteSpace($targetKey)) {
            throw 'singlekey-rawbounded requires a single target key in the bundle.'
        }
        @(
            '.symfix',
            '.reload /f',
            ('.logopen /t "{0}"' -f $escapedTraceLogPath),
            ('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile),
            ('.echo REGPROBE_TARGET|{0}' -f $targetKey),
            ('.echo REGPROBE_RAW_HIT_LIMIT|{0}' -f $NoiseBudgetEventLimit),
            'bc *',
            ('bu {0}' -f $registryWatchSymbol),
            'bs 0 ".echo REGPROBE_VALUE_BEGIN; du poi(@rdx+8) L1; .echo REGPROBE_VALUE_END; gc"',
            'g'
        )
    }
    default {
        $null
    }
}
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
    $rewrittenCommandLines = @($rewrittenCommandLines) + @(( '.logopen /t "{0}"' -f $escapedTraceLogPath ))
}
if ($TraceProfile -ne 'full') {
    $rewrittenCommandLines = $modeCommandLines
}
Set-Content -LiteralPath $sessionCommandScriptPath -Value $rewrittenCommandLines -Encoding ASCII

$kdProcess = $null
$shellSnapshot = $null
$runError = $null
$shellRecovered = $false
$recovery = $null
$debuggerNoiseBudgetExceeded = $false
$traceObservation = $null
$breakinExecution = $null
$roundtripExecution = $null
$requiresBreakinProfile = ($TraceProfile -like 'breakin-*')
$requiresRoundtripProfile = ($TraceProfile -like 'roundtrip-*')
$useColdBoot = ($bootMode -eq 'cold-boot') -and -not [string]::IsNullOrWhiteSpace([string]$bundle.debug_snapshot_name)
$debuggerLaunchMode = if (-not [string]::IsNullOrWhiteSpace([string]$bundle.debugger_launch_mode)) { [string]$bundle.debugger_launch_mode } else { 'quiet' }
$kdArgs = @()
if (($debuggerLaunchMode -eq 'quiet') -and -not $useColdBoot) {
    $kdArgs += '-kqm'
}
switch ($breakOnConnectMode) {
    'bonc' { $kdArgs += '-bonc' }
    'b' { $kdArgs += '-b' }
    default { }
}
$kdArgs += @(
    '-k', ("com:pipe,port={0},resets=0,reconnect" -f ([string]$bundle.pipe_name)),
    '-cfr', $sessionCommandScriptPath,
    '-logo', $consoleLogPath
)

try {
    if ($bootMode -eq 'attach-after-shell' -and -not [string]::IsNullOrWhiteSpace([string]$bundle.debug_snapshot_name)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $vmPath, 'hard') -IgnoreExitCode | Out-Null
        Start-Sleep -Seconds 5
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $vmPath, ([string]$bundle.debug_snapshot_name)) | Out-Null
        $session.steps += 'reverted-to-debug-snapshot'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $vmPath, 'nogui') | Out-Null
        $session.steps += 'vm-started-before-attach'
        [void](Wait-ForVmToolsReady -VmPath $vmPath)
        $preAttachShell = Wait-ForShellHealthy -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
        $session.steps += 'shell-healthy-before-attach'
        $session.pre_attach_shell_checks = $preAttachShell.checks
        Write-JsonFile -Path $sessionPath -InputObject $session
    }
    elseif ($useColdBoot) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $vmPath, 'hard') -IgnoreExitCode | Out-Null
        Start-Sleep -Seconds 5
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $vmPath, ([string]$bundle.debug_snapshot_name)) | Out-Null
        $session.steps += 'reverted-to-debug-snapshot'
        Write-JsonFile -Path $sessionPath -InputObject $session
    }

    $kdProcess = Start-DebuggerProcess -FilePath $hostDebuggerPath -Arguments $kdArgs -RedirectStandardInput:$requiresBreakinProfile
    $session.status = 'debugger-started'
    $session.steps += 'debugger-started'
    Write-JsonFile -Path $sessionPath -InputObject $session

    Start-Sleep -Seconds ([Math]::Max(3, $DebuggerAttachLeadSeconds))

    if ($bootMode -eq 'attach-after-shell') {
        $session.status = 'attach-after-shell-active'
        $session.steps += 'attach-after-shell-active'
    }
    elseif ($useColdBoot) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $vmPath, 'nogui') | Out-Null
        $session.status = 'vm-cold-boot-requested'
        $session.steps += 'vm-cold-boot-requested'
    }
    else {
        Request-GuestRestart -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
        $session.status = 'guest-restart-requested'
        $session.steps += 'guest-restart-requested'
    }
    Write-JsonFile -Path $sessionPath -InputObject $session

    Start-Sleep -Seconds 5
    if ($bootMode -eq 'attach-after-shell') {
        $settleDeadline = (Get-Date).AddSeconds([Math]::Max(5, $PostShellSettleSeconds))
        do {
            $traceObservation = Get-TraceObservation -PreferredTraceLogPath $traceLogPath -ConsoleLogPath $consoleLogPath -TargetKey $targetKey
            if ((($NoiseBudgetBytes -gt 0) -and ($traceObservation.log_size_bytes -ge $NoiseBudgetBytes)) -or (($NoiseBudgetEventLimit -gt 0) -and ($traceObservation.raw_value_event_count -ge $NoiseBudgetEventLimit))) {
                $debuggerNoiseBudgetExceeded = $true
                break
            }
            Start-Sleep -Seconds ([Math]::Min($PollIntervalSeconds, 5))
        } while ((Get-Date) -lt $settleDeadline)

        $shellSnapshot = Get-ShellHealthSnapshot -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
        if ($shellSnapshot.shell_healthy) {
            $shellRecovered = $true
            $session.status = 'shell-healthy'
            $session.steps += 'shell-healthy-after-attach'
            $session.shell_checks = $shellSnapshot.checks
            if ($requiresBreakinProfile) {
                $breakinExecution = Invoke-DebuggerBreakinProfile -Process $kdProcess -Profile $TraceProfile
                $session.breakin_execution = $breakinExecution
                $session.steps += ('breakin-profile-{0}-dispatched' -f $TraceProfile)
                $traceObservation = Wait-ForBreakinOutcome -PreferredTraceLogPath $traceLogPath -ConsoleLogPath $consoleLogPath -TargetKey $targetKey -ExpectedBreakinCount ([int]$breakinExecution.breakin_count)
                $shellSnapshot = Get-ShellHealthSnapshot -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
                $shellRecovered = [bool]$shellSnapshot.shell_healthy
                if ($shellRecovered) {
                    $session.steps += 'shell-healthy-after-breakin'
                    $session.shell_checks = $shellSnapshot.checks
                }
            }
            elseif ($requiresRoundtripProfile) {
                $roundtripExecution = Get-RoundtripProfileSpec -Profile $TraceProfile
                $session.roundtrip_execution = $roundtripExecution
                $session.steps += ('roundtrip-profile-{0}-armed' -f $TraceProfile)
                $traceObservation = Wait-ForRoundtripOutcome -PreferredTraceLogPath $traceLogPath -ConsoleLogPath $consoleLogPath -TargetKey $targetKey -ExpectedRoundtripCount ([int]$roundtripExecution.roundtrip_count)
                $shellSnapshot = Get-ShellHealthSnapshot -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
                $shellRecovered = [bool]$shellSnapshot.shell_healthy
                if ($shellRecovered) {
                    $session.steps += 'shell-healthy-after-roundtrip'
                    $session.shell_checks = $shellSnapshot.checks
                }
            }
        }
        elseif ($debuggerNoiseBudgetExceeded) {
            $session.status = 'noise-budget-stop'
            $session.steps += 'noise-budget-stop'
        }
    }
    else {
        $monitor = Wait-ForDebuggerAwareBoot -VmPath $vmPath -GuestAuthArgs $guestAuthArgs -PreferredTraceLogPath $traceLogPath -ConsoleLogPath $consoleLogPath -TargetKey $targetKey -EnableNoiseBudget:($TraceProfile -eq 'singlekey-rawbounded')
        $traceObservation = $monitor.observation
        $debuggerNoiseBudgetExceeded = [bool]$monitor.noise_budget_exceeded
        if (-not [string]::IsNullOrWhiteSpace([string]$monitor.tools_state)) {
            $session.steps += 'vmtools-ready-after-restart'
        }
        Write-JsonFile -Path $sessionPath -InputObject $session

        if ($monitor.shell_snapshot) {
            $shellSnapshot = $monitor.shell_snapshot
            $shellRecovered = $true
            $session.status = 'shell-healthy'
            $session.steps += 'shell-healthy-after-restart'
            $session.shell_checks = $shellSnapshot.checks
            if ($requiresBreakinProfile) {
                $breakinExecution = Invoke-DebuggerBreakinProfile -Process $kdProcess -Profile $TraceProfile
                $session.breakin_execution = $breakinExecution
                $session.steps += ('breakin-profile-{0}-dispatched' -f $TraceProfile)
                $traceObservation = Wait-ForBreakinOutcome -PreferredTraceLogPath $traceLogPath -ConsoleLogPath $consoleLogPath -TargetKey $targetKey -ExpectedBreakinCount ([int]$breakinExecution.breakin_count)
                $shellSnapshot = Get-ShellHealthSnapshot -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
                $shellRecovered = [bool]$shellSnapshot.shell_healthy
                if ($shellRecovered) {
                    $session.steps += 'shell-healthy-after-breakin'
                    $session.shell_checks = $shellSnapshot.checks
                }
            }
            elseif ($requiresRoundtripProfile) {
                $roundtripExecution = Get-RoundtripProfileSpec -Profile $TraceProfile
                $session.roundtrip_execution = $roundtripExecution
                $session.steps += ('roundtrip-profile-{0}-armed' -f $TraceProfile)
                $traceObservation = Wait-ForRoundtripOutcome -PreferredTraceLogPath $traceLogPath -ConsoleLogPath $consoleLogPath -TargetKey $targetKey -ExpectedRoundtripCount ([int]$roundtripExecution.roundtrip_count)
                $shellSnapshot = Get-ShellHealthSnapshot -VmPath $vmPath -GuestAuthArgs $guestAuthArgs
                $shellRecovered = [bool]$shellSnapshot.shell_healthy
                if ($shellRecovered) {
                    $session.steps += 'shell-healthy-after-roundtrip'
                    $session.shell_checks = $shellSnapshot.checks
                }
            }
        }
        elseif ($debuggerNoiseBudgetExceeded) {
            $session.status = 'noise-budget-stop'
            $session.steps += 'noise-budget-stop'
        }
    }

    Write-JsonFile -Path $sessionPath -InputObject $session
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

if (-not $traceObservation) {
    $traceObservation = Get-TraceObservation -PreferredTraceLogPath $traceLogPath -ConsoleLogPath $consoleLogPath -TargetKey $targetKey
}

$resolvedTraceLogPath = $traceObservation.resolved_trace_log_path
$analysisLogPath = $traceObservation.analysis_log_path
$analysisSource = $traceObservation.analysis_source
$consoleText = $traceObservation.console_text
$analysisLines = @($traceObservation.analysis_lines)
$analysisText = $traceObservation.analysis_text
$scriptExecutionObserved = [bool]$traceObservation.script_execution_observed
$perKeyResults = @()
$totalHitKeys = 0
foreach ($key in $keys) {
    $matches = if ($TraceProfile -eq 'conditional') {
        @($traceObservation.conditional_hit_lines | Where-Object { $_ -like "*$key*" })
    }
    elseif ($TraceProfile -eq 'singlekey-rawbounded' -and $key -eq $targetKey) {
        @($traceObservation.host_filtered_samples)
    }
    elseif ($TraceProfile -like 'singlekey-*') {
        @()
    }
    else {
        if ($analysisLogPath) { @(Select-String -LiteralPath $analysisLogPath -Pattern $key -SimpleMatch) } else { @() }
    }

    $sampleLines = if ($TraceProfile -eq 'conditional' -or $TraceProfile -eq 'singlekey-rawbounded') {
        @($matches | Select-Object -First 3 | ForEach-Object { [string]$_ })
    }
    else {
        @($matches | Select-Object -First 3 | ForEach-Object { $_.Line.Trim() })
    }
    $hitCount = if ($TraceProfile -eq 'singlekey-rawbounded' -and $key -eq $targetKey) {
        [int]$traceObservation.host_filtered_hits
    }
    elseif ($TraceProfile -like 'singlekey-*') {
        0
    }
    else {
        @($matches).Count
    }
    if ($hitCount -gt 0) {
        $totalHitKeys++
    }

    $perKeyResults += [ordered]@{
        key = $key
        hit_count = $hitCount
        sample_lines = $sampleLines
    }
}

$fatalSystemErrorObserved = [bool]$traceObservation.fatal_system_error_observed
$kernelConnected = [bool]$traceObservation.kernel_connected
$transportError = [bool]$traceObservation.transport_error
$noDebuggeeWaiting = [bool]$traceObservation.no_debuggee_waiting
$breakpointUnresolved = [bool]$traceObservation.breakpoint_unresolved
$parserInvalid = [bool]$traceObservation.parser_invalid
$rawNoiseEventCount = [int]$traceObservation.raw_value_event_count
$hostFilteredHits = [int]$traceObservation.host_filtered_hits
$windbgLogDetected = [bool]$traceObservation.windbg_log_detected
$firstHitObserved = [bool]$traceObservation.firsthit_observed
$breakinRequestCount = [int]$traceObservation.breakin_request_count
$breakinSuccessCount = [int]$traceObservation.breakin_success_count
$roundtripRequestCount = [int]$traceObservation.roundtrip_request_count
$roundtripSuccessCount = [int]$traceObservation.roundtrip_success_count
$postRestartRoundtripRequestCount = [int]$traceObservation.post_restart_roundtrip_request_count
$postRestartRoundtripSuccessCount = [int]$traceObservation.post_restart_roundtrip_success_count
$effectiveRoundtripRequestCount = if ($bootMode -eq 'attach-after-shell') { $roundtripRequestCount } else { $postRestartRoundtripRequestCount }
$effectiveRoundtripSuccessCount = if ($bootMode -eq 'attach-after-shell') { $roundtripSuccessCount } else { $postRestartRoundtripSuccessCount }
$breakinDispatchCount = if ($breakinExecution) { [int]$breakinExecution.breakin_count } else { 0 }
$roundtripDispatchCount = if ($roundtripExecution) { [int]$roundtripExecution.roundtrip_count } else { 0 }
$breakinAttempted = ($breakinRequestCount -gt 0) -or ($breakinDispatchCount -gt 0)
$roundtripAttempted = ($effectiveRoundtripRequestCount -gt 0) -or ($roundtripDispatchCount -gt 0)
$resultStatus = if ($runError -and $fatalSystemErrorObserved) {
    'boot-unsafe'
}
elseif ($parserInvalid) {
    'blocked-parser-invalid'
}
elseif ($breakpointUnresolved) {
    'breakpoint-unresolved'
}
elseif ($transportError) {
    'trace-error'
}
elseif ($debuggerNoiseBudgetExceeded) {
    'debugger-noise-budget-exceeded'
}
elseif ($runError) {
    'trace-error'
}
elseif (-not $windbgLogDetected) {
    'missing-log'
}
elseif (-not $scriptExecutionObserved) {
    'script-not-executed'
}
elseif ($noDebuggeeWaiting) {
    'attach-ok-no-debuggee'
}
elseif ($TraceProfile -in @('minimal', 'symbols', 'attach-only')) {
    if ($shellRecovered) { 'runner-ok' } else { 'transport-unstable' }
}
elseif ($TraceProfile -like 'breakin-*') {
    if ($breakinSuccessCount -gt 0) { 'runner-ok' }
    elseif ($breakinRequestCount -gt 0) { 'attach-ok-breakin-failed' }
    elseif ($noDebuggeeWaiting) { 'attach-ok-no-debuggee' }
    elseif ($shellRecovered) { 'attach-ok-command-not-executed' }
    else { 'transport-unstable' }
}
elseif ($TraceProfile -like 'roundtrip-*') {
    if ($effectiveRoundtripSuccessCount -gt 0) { 'runner-ok' }
    elseif ($effectiveRoundtripRequestCount -gt 0) { 'attach-ok-roundtrip-missing' }
    elseif ($noDebuggeeWaiting) { 'attach-ok-no-debuggee' }
    elseif ($shellRecovered) { 'attach-ok-command-not-executed' }
    else { 'transport-unstable' }
}
elseif ($TraceProfile -eq 'singlekey-smoke') {
    if ($shellRecovered) { 'runner-ok' } else { 'connected-no-target-hit' }
}
elseif ($TraceProfile -eq 'singlekey-firsthit') {
    if ($firstHitObserved) { 'runner-ok' } else { 'connected-no-target-hit' }
}
elseif ($hostFilteredHits -gt 0) {
    'hit-detected'
}
elseif ($rawNoiseEventCount -gt 0) {
    'connected-raw-noise-only'
}
elseif ($kernelConnected) {
    'connected-no-target-hit'
}
else {
    'no-hit'
}

$windbgTransportState = if ($transportError -or $resultStatus -eq 'boot-unsafe') {
    'transport_error'
}
elseif ($TraceProfile -like 'breakin-*') {
    if ($breakinSuccessCount -gt 0 -and $shellRecovered) {
        'transport_ok'
    }
    elseif ($breakinRequestCount -gt 0 -and $shellRecovered) {
        'attach_ok_breakin_failed'
    }
    elseif ($noDebuggeeWaiting -and $shellRecovered) {
        'attach_ok_no_debuggee'
    }
    elseif ($shellRecovered -and $scriptExecutionObserved) {
        'attach_ok_command_not_executed'
    }
    else {
        'transport_unstable'
    }
}
elseif ($TraceProfile -like 'roundtrip-*') {
    if ($effectiveRoundtripSuccessCount -gt 0 -and $shellRecovered) {
        'transport_ok'
    }
    elseif ($effectiveRoundtripRequestCount -gt 0 -and $shellRecovered) {
        'attach_ok_roundtrip_missing'
    }
    elseif ($noDebuggeeWaiting -and $shellRecovered) {
        'attach_ok_no_debuggee'
    }
    elseif ($shellRecovered -and $scriptExecutionObserved) {
        'attach_ok_command_not_executed'
    }
    else {
        'transport_unstable'
    }
}
elseif ($noDebuggeeWaiting -and $shellRecovered -and $scriptExecutionObserved) {
    'attach_ok_no_debuggee'
}
elseif ($shellRecovered -and $scriptExecutionObserved -and ($kernelConnected -or $bootMode -eq 'attach-after-shell')) {
    'transport_ok'
}
elseif ($shellRecovered -and $scriptExecutionObserved) {
    'transport_partial'
}
else {
    'transport_unstable'
}

$transportScore = 0
if ($scriptExecutionObserved) { $transportScore++ }
if ($shellRecovered) { $transportScore++ }
if ($kernelConnected -or $bootMode -eq 'attach-after-shell') { $transportScore++ }
if (-not $transportError) { $transportScore++ }
if ($breakinSuccessCount -gt 0) { $transportScore++ }
if ($effectiveRoundtripSuccessCount -gt 0) { $transportScore++ }
$windbgSemanticReady = ($TraceProfile -like 'singlekey-*')

if ($resultStatus -eq 'boot-unsafe' -and -not [string]::IsNullOrWhiteSpace([string]$bundle.debug_snapshot_name)) {
    try {
        $recovery = Recover-DebugTargetVm -VmPath $vmPath -SnapshotName ([string]$bundle.debug_snapshot_name)
    }
    catch {
        $recovery = [ordered]@{
            recovered = $false
            error = $_.Exception.Message
        }
    }
}

$results = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    family = 'power-control'
    lane = 'windbg-boot-registry-trace'
    status = $resultStatus
    collection_mode = $CollectionMode
    target_key = $targetKey
    key_count = @($keys).Count
    hit_key_count = $totalHitKeys
    analysis_source = $analysisSource
    trace_log_path = if ($resolvedTraceLogPath) { Get-RepoDisplayPath -Path $resolvedTraceLogPath } else { $null }
    trace_profile = $TraceProfile
    boot_mode = $bootMode
    break_on_connect_mode = $breakOnConnectMode
    debugger_launch_mode = $debuggerLaunchMode
    windbg_log_detected = $windbgLogDetected
    windbg_transport_state = $windbgTransportState
    windbg_semantic_ready = $windbgSemanticReady
    transport_score = $transportScore
    no_debuggee_waiting = $noDebuggeeWaiting
    breakin_attempted = $breakinAttempted
    breakin_dispatched_count = $breakinDispatchCount
    breakin_request_count = $breakinRequestCount
    breakin_success_count = $breakinSuccessCount
    roundtrip_attempted = $roundtripAttempted
    roundtrip_dispatched_count = $roundtripDispatchCount
    roundtrip_request_count = $roundtripRequestCount
    roundtrip_success_count = $roundtripSuccessCount
    post_restart_roundtrip_request_count = $postRestartRoundtripRequestCount
    post_restart_roundtrip_success_count = $postRestartRoundtripSuccessCount
    roundtrip_ready = ($effectiveRoundtripSuccessCount -gt 0)
    kernel_connected = $kernelConnected
    transport_error = $transportError
    fatal_system_error_observed = $fatalSystemErrorObserved
    shell_recovered = $shellRecovered
    script_execution_observed = $scriptExecutionObserved
    breakpoint_set = ($scriptExecutionObserved -and -not $breakpointUnresolved)
    breakpoint_unresolved = $breakpointUnresolved
    parser_invalid = $parserInvalid
    debugger_noise_budget_exceeded = $debuggerNoiseBudgetExceeded
    raw_noise_event_count = $rawNoiseEventCount
    host_filtered_hits = $hostFilteredHits
    error = $runError
    recovery = $recovery
    keys = $perKeyResults
    shell_checks = if ($shellSnapshot) { $shellSnapshot.checks } else { $null }
}

$traceRunnerOutput = if ($resolvedTraceLogPath -and (Test-Path -LiteralPath $resolvedTraceLogPath)) {
    Publish-RunnerOutputArtifacts -Label 'windbg-trace-log' -RawPath $resolvedTraceLogPath -SanitizedOutputPath (Join-Path $sessionRoot 'windbg-registry-trace.public.log')
}
else {
    $null
}
$consoleRunnerOutput = if (Test-Path -LiteralPath $consoleLogPath) {
    Publish-RunnerOutputArtifacts -Label 'kd-console-log' -RawPath $consoleLogPath -SanitizedOutputPath (Join-Path $sessionRoot 'kd-console.public.log')
}
else {
    $null
}
$results.runner_output = [ordered]@{
    trace = $traceRunnerOutput
    console = $consoleRunnerOutput
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    family = 'power-control'
    lane = 'windbg-boot-registry-trace'
    status = $resultStatus
    capture_status = 'missing-capture'
    collection_mode = $CollectionMode
    vm_path = $vmPath
    debugger_path = $hostDebuggerPath
    target_key = $targetKey
    key_count = @($keys).Count
    hit_key_count = $totalHitKeys
    analysis_source = $analysisSource
    trace_log_path = if ($resolvedTraceLogPath) { Get-RepoDisplayPath -Path $resolvedTraceLogPath } else { $null }
    trace_profile = $TraceProfile
    boot_mode = $bootMode
    break_on_connect_mode = $breakOnConnectMode
    debugger_launch_mode = $debuggerLaunchMode
    windbg_log_detected = $windbgLogDetected
    windbg_transport_state = $windbgTransportState
    windbg_semantic_ready = $windbgSemanticReady
    transport_score = $transportScore
    no_debuggee_waiting = $noDebuggeeWaiting
    breakin_attempted = $breakinAttempted
    breakin_dispatched_count = $breakinDispatchCount
    breakin_request_count = $breakinRequestCount
    breakin_success_count = $breakinSuccessCount
    roundtrip_attempted = $roundtripAttempted
    roundtrip_dispatched_count = $roundtripDispatchCount
    roundtrip_request_count = $roundtripRequestCount
    roundtrip_success_count = $roundtripSuccessCount
    post_restart_roundtrip_request_count = $postRestartRoundtripRequestCount
    post_restart_roundtrip_success_count = $postRestartRoundtripSuccessCount
    roundtrip_ready = ($effectiveRoundtripSuccessCount -gt 0)
    kernel_connected = $kernelConnected
    transport_error = $transportError
    fatal_system_error_observed = $fatalSystemErrorObserved
    shell_recovered = $shellRecovered
    script_execution_observed = $scriptExecutionObserved
    breakpoint_set = ($scriptExecutionObserved -and -not $breakpointUnresolved)
    breakpoint_unresolved = $breakpointUnresolved
    parser_invalid = $parserInvalid
    debugger_noise_budget_exceeded = $debuggerNoiseBudgetExceeded
    raw_noise_event_count = $rawNoiseEventCount
    host_filtered_hits = $hostFilteredHits
    command_script = Get-RepoDisplayPath -Path $sessionCommandScriptPath
    capture_artifacts = @()
    runner_output = [ordered]@{
        trace = $traceRunnerOutput
        console = $consoleRunnerOutput
    }
    bundle_ref = Get-RepoDisplayPath -Path $bundleFullPath
    note = 'Boot trace executed through kd over the configured VMware serial pipe with raw+sanitized runner-output handling.'
}
if ($runError) {
    $summary.error = $runError
}
if ($recovery) {
    $summary.recovery = $recovery
}

Write-JsonFile -Path $resultsPath -InputObject $results
$captureArtifacts = @(
    @($traceRunnerOutput.public_sanitized_ref) +
    @($consoleRunnerOutput.public_sanitized_ref) +
    @(New-LaneArtifactRef -Path $resultsPath) +
    @(New-LaneArtifactRef -Path $sessionCommandScriptPath)
) | Where-Object { $_ }
$summary.capture_artifacts = $captureArtifacts
$summary.capture_status = if ($captureArtifacts.Count -gt 0) { 'captured' } else { 'missing-capture' }
Write-JsonFile -Path $summaryPath -InputObject $summary

$session.status = $resultStatus
$session.steps += 'trace-finished'
$session.summary_path = Get-RepoDisplayPath -Path $summaryPath
$session.results_path = Get-RepoDisplayPath -Path $resultsPath
if ($runError) {
    $session.error = $runError
}
if ($recovery) {
    $session.recovery = $recovery
}
Write-JsonFile -Path $sessionPath -InputObject $session

$summary
