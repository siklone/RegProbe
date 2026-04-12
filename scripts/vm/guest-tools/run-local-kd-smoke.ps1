[CmdletBinding()]
param(
    [string]$OutputName = 'local-kd-smoke',
    [string]$OutputRoot = '',
    [string]$SymbolRoot = '',
    [string]$UploadBaseUrl = '',
    [int]$TimeoutSeconds = 180,
    [string]$QuerySymbol = 'nt!CmQueryValueKey',
    [string[]]$DebuggerCommands = @(),
    [string]$TriggerPowerShellCommand = '',
    [int]$TriggerDelaySeconds = 0
)

$ErrorActionPreference = 'Stop'

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Invoke-ArtifactUpload {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path -Path $Path -PathType Leaf)) {
        return $null
    }

    $targetUri = '{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName
    Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
    return [ordered]@{
        path = $Path
        uri = $targetUri
    }
}

function Find-FirstDebuggerTool {
    param([Parameter(Mandatory = $true)][string]$ToolName)

    $candidates = @(
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64'
    )

    foreach ($candidate in $candidates) {
        $path = Join-Path $candidate $ToolName
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\debugger-smoke' $OutputName
}

if ([string]::IsNullOrWhiteSpace($SymbolRoot)) {
    $SymbolRoot = Join-Path 'C:\Tools\Symbols' $OutputName
}

Ensure-Directory -Path $OutputRoot
Ensure-Directory -Path $SymbolRoot

$commandFile = Join-Path $OutputRoot 'local-kd.txt'
$logPath = Join-Path $OutputRoot 'local-kd.log'
$summaryPath = Join-Path $OutputRoot 'summary.json'
$stdoutPath = Join-Path $OutputRoot 'stdout.txt'
$stderrPath = Join-Path $OutputRoot 'stderr.txt'
$symchkLog = Join-Path $OutputRoot 'symchk.txt'
$triggerStdoutPath = Join-Path $OutputRoot 'trigger.stdout.txt'
$triggerStderrPath = Join-Path $OutputRoot 'trigger.stderr.txt'

$kdPath = Find-FirstDebuggerTool -ToolName 'kd.exe'
$symchkPath = Find-FirstDebuggerTool -ToolName 'symchk.exe'

if (-not $kdPath) {
    throw 'kd.exe not found in expected debugger roots.'
}

if (-not $symchkPath) {
    throw 'symchk.exe not found in expected debugger roots.'
}

$symchkArgs = @(
    'C:\Windows\System32\ntoskrnl.exe',
    '/s', ('SRV*{0}*https://msdl.microsoft.com/download/symbols' -f $SymbolRoot),
    '/ocx', $symchkLog
)
$symchkProc = Start-Process -FilePath $symchkPath -ArgumentList $symchkArgs -PassThru -Wait -WindowStyle Hidden

$effectiveCommands = @(
    ('.sympath {0}' -f $SymbolRoot)
    '.reload /f nt'
    '.echo REGPROBE_LOCALKD_BEGIN'
)

if ($DebuggerCommands -and $DebuggerCommands.Count -gt 0) {
    $effectiveCommands += $DebuggerCommands
}
elseif (-not [string]::IsNullOrWhiteSpace($QuerySymbol)) {
    $effectiveCommands += ('x {0}' -f $QuerySymbol)
}

$effectiveCommands += @(
    '.echo REGPROBE_LOCALKD_END'
    'q'
)

$effectiveCommands | Set-Content -Path $commandFile -Encoding ASCII

$kdArgs = @('-kl', '-cf', $commandFile, '-logo', $logPath)
$proc = Start-Process `
    -FilePath $kdPath `
    -ArgumentList $kdArgs `
    -PassThru `
    -RedirectStandardOutput $stdoutPath `
    -RedirectStandardError $stderrPath `
    -WindowStyle Hidden

$triggerExecuted = $false
$triggerExitCode = $null
$triggerError = ''
if (-not [string]::IsNullOrWhiteSpace($TriggerPowerShellCommand)) {
    if ($TriggerDelaySeconds -gt 0) {
        Start-Sleep -Seconds $TriggerDelaySeconds
    }

    try {
        $triggerProc = Start-Process `
            -FilePath 'powershell.exe' `
            -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $TriggerPowerShellCommand) `
            -PassThru `
            -Wait `
            -RedirectStandardOutput $triggerStdoutPath `
            -RedirectStandardError $triggerStderrPath `
            -WindowStyle Hidden
        $triggerExecuted = $true
        $triggerExitCode = $triggerProc.ExitCode
    }
    catch {
        $triggerError = $_.Exception.Message
    }
}

$completed = $proc.WaitForExit($TimeoutSeconds * 1000)
$proc.Refresh()
if (-not $completed) {
    try {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    catch {
    }
}

$stdoutText = if (Test-Path $stdoutPath) { Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue } else { '' }
$logText = if (Test-Path $logPath) { Get-Content -Path $logPath -Raw -ErrorAction SilentlyContinue } else { '' }
$combinedText = '{0}{1}{2}' -f $logText, [Environment]::NewLine, $stdoutText
$breakpointCommandsPresent = @($effectiveCommands | Where-Object { $_ -match '^(bp|bu)\s+' }).Count -gt 0
$runControlRequested = @($effectiveCommands | Where-Object { $_ -match '^g(\s|$)' }).Count -gt 0
$attached = ($combinedText -match 'Connected to Windows')
$querySymbolSeen = ($combinedText -match [regex]::Escape($QuerySymbol))
$status = 'ok'
$statusError = $null

if (-not $completed) {
    $status = 'error'
    $statusError = "kd.exe timed out after $TimeoutSeconds second(s)."
}
elseif (-not $attached) {
    $status = 'error'
    $statusError = 'Local KD did not attach to the live kernel target.'
}
elseif ((-not ($DebuggerCommands -and $DebuggerCommands.Count -gt 0)) -and (-not [string]::IsNullOrWhiteSpace($QuerySymbol)) -and (-not $querySymbolSeen)) {
    $status = 'error'
    $statusError = "Query symbol did not appear in debugger output: $QuerySymbol"
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = $status
    error = $statusError
    kd_path = $kdPath
    symchk_path = $symchkPath
    symchk_exit_code = $symchkProc.ExitCode
    symbol_root = $SymbolRoot
    query_symbol = $QuerySymbol
    debugger_commands = @($DebuggerCommands)
    effective_commands = @($effectiveCommands)
    used_custom_commands = [bool]($DebuggerCommands -and $DebuggerCommands.Count -gt 0)
    trigger_command_present = -not [string]::IsNullOrWhiteSpace($TriggerPowerShellCommand)
    trigger_executed = $triggerExecuted
    trigger_delay_seconds = $TriggerDelaySeconds
    trigger_exit_code = $triggerExitCode
    trigger_error = $triggerError
    breakpoint_commands_present = $breakpointCommandsPresent
    run_control_requested = $runControlRequested
    completed = $completed
    exit_code = if ($completed) { $proc.ExitCode } else { $null }
    attached = $attached
    local_kernel_disabled = ($combinedText -match 'Local kernel debugging is disabled by default')
    symbol_reload_started = ($combinedText -match 'Loading Kernel Symbols')
    breakpoint_supported = if ($breakpointCommandsPresent) { -not ($combinedText -match 'Operation not supported by current debuggee') } else { $null }
    runnable_debuggee = if ($runControlRequested) { -not ($combinedText -match 'No runnable debuggees') } else { $null }
    query_symbol_seen = $querySymbolSeen
    command_file_exists = [bool](Test-Path $commandFile)
    log_exists = [bool](Test-Path $logPath)
    stdout_exists = [bool](Test-Path $stdoutPath)
    stderr_exists = [bool](Test-Path $stderrPath)
    symchk_log_exists = [bool](Test-Path -Path $symchkLog -PathType Leaf)
    symchk_log_is_directory = [bool](Test-Path -Path $symchkLog -PathType Container)
    trigger_stdout_exists = [bool](Test-Path $triggerStdoutPath)
    trigger_stderr_exists = [bool](Test-Path $triggerStderrPath)
    uploads = [ordered]@{}
}

foreach ($entry in @(
    @{ key = 'command_file'; path = $commandFile; name = ('{0}.txt' -f $OutputName) },
    @{ key = 'log'; path = $logPath; name = ('{0}.log' -f $OutputName) },
    @{ key = 'stdout'; path = $stdoutPath; name = ('{0}.stdout.txt' -f $OutputName) },
    @{ key = 'stderr'; path = $stderrPath; name = ('{0}.stderr.txt' -f $OutputName) },
    @{ key = 'symchk_log'; path = $symchkLog; name = ('{0}-symchk.txt' -f $OutputName) },
    @{ key = 'trigger_stdout'; path = $triggerStdoutPath; name = ('{0}.trigger.stdout.txt' -f $OutputName) },
    @{ key = 'trigger_stderr'; path = $triggerStderrPath; name = ('{0}.trigger.stderr.txt' -f $OutputName) }
)) {
    $upload = Invoke-ArtifactUpload -Path $entry.path -RemoteName $entry.name
    if ($upload) {
        $summary.uploads[$entry.key] = $upload
    }
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
Invoke-ArtifactUpload -Path $summaryPath -RemoteName ('{0}-summary.json' -f $OutputName) | Out-Null
Write-Output $summaryPath
