[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('arm','run')]
    [string]$Phase,

    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,

    [Parameter(Mandatory = $true)]
    [string]$StatePath,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

    [Parameter(Mandatory = $true)]
    [string]$ResultsPath
)

$ErrorActionPreference = 'Stop'
$traceName = 'RegProbeMegaTrace'
$etlPath = Join-Path $GuestRoot 'mega-trace.etl'
$csvPath = Join-Path $GuestRoot 'mega-trace.csv'
$runLogPath = Join-Path $GuestRoot 'guest-run.log'
$script:activeTraceOutputPath = $null

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$InputObject,
        [int]$Depth = 12
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $json = $InputObject | ConvertTo-Json -Depth $Depth
    $tempPath = '{0}.{1}.tmp' -f $Path, ([guid]::NewGuid().ToString('N'))
    try {
        $json | Set-Content -Path $tempPath -Encoding UTF8
        $written = $false
        for ($attempt = 1; $attempt -le 10; $attempt++) {
            try {
                Move-Item -Path $tempPath -Destination $Path -Force
                $written = $true
                break
            }
            catch {
                if ($attempt -eq 10) {
                    throw
                }
                Start-Sleep -Milliseconds 200
            }
        }

        if (-not $written) {
            throw "Failed to atomically replace JSON file: $Path"
        }
    }
    finally {
        if (Test-Path -LiteralPath $tempPath) {
            Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Write-JsonFileDirect {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$InputObject,
        [int]$Depth = 12
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Read-JsonFile {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    try {
        return (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

function Read-TextOrEmpty {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }
    $raw = Get-Content -LiteralPath $Path -Raw -ErrorAction SilentlyContinue
    if ($null -eq $raw) {
        return ''
    }
    return ([string]$raw).Trim()
}

function Write-RunLog {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $parent = Split-Path -Parent $runLogPath
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $line = '{0} {1}' -f [DateTime]::UtcNow.ToString('o'), $Message
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        $stream = $null
        $writer = $null
        try {
            $stream = [System.IO.FileStream]::new($runLogPath, [System.IO.FileMode]::Append, [System.IO.FileAccess]::Write, [System.IO.FileShare]::ReadWrite)
            $writer = [System.IO.StreamWriter]::new($stream, [System.Text.UTF8Encoding]::new($false))
            $writer.WriteLine($line)
            $writer.Flush()
            return
        }
        catch {
            if ($attempt -eq 5) {
                return
            }
            Start-Sleep -Milliseconds 100
        }
        finally {
            if ($writer) {
                $writer.Dispose()
            }
            elseif ($stream) {
                $stream.Dispose()
            }
        }
    }
}

function Invoke-CmdCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [int]$TimeoutSeconds = 0
    )

    $stdout = [System.IO.Path]::GetTempFileName()
    $stderr = [System.IO.Path]::GetTempFileName()
    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        if ($TimeoutSeconds -gt 0) {
            if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
                try {
                    $proc.Kill()
                }
                catch {
                }

                return [ordered]@{
                    exit_code = -1
                    stdout = Read-TextOrEmpty -Path $stdout
                    stderr = Read-TextOrEmpty -Path $stderr
                    timed_out = $true
                }
            }
        }
        else {
            $proc.WaitForExit()
        }

        return [ordered]@{
            exit_code = $proc.ExitCode
            stdout = Read-TextOrEmpty -Path $stdout
            stderr = Read-TextOrEmpty -Path $stderr
            timed_out = $false
        }
    }
    finally {
        Remove-Item -Path $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-ProcessNoCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [int]$TimeoutSeconds = 0
    )

    $proc = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -WindowStyle Hidden
    if ($TimeoutSeconds -gt 0) {
        if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
            try {
                $proc.Kill()
            }
            catch {
            }

            return [ordered]@{
                exit_code = -1
                stdout = ''
                stderr = ''
                timed_out = $true
            }
        }
    }
    else {
        $proc.WaitForExit()
    }

    return [ordered]@{
        exit_code = $proc.ExitCode
        stdout = ''
        stderr = ''
        timed_out = $false
    }
}

function Get-BaselineValues {
    param([object[]]$Candidates)

    $values = [ordered]@{}
    foreach ($candidate in $Candidates) {
        $path = [string]$candidate.registry_path -replace '^HKLM', 'Registry::HKEY_LOCAL_MACHINE'
        $name = [string]$candidate.value_name
        try {
            $item = Get-ItemProperty -Path $path -Name $name -ErrorAction Stop
            $values[$name] = $item.$name
        }
        catch {
            $values[$name] = $null
        }
    }

    return $values
}

function Apply-CandidateValues {
    param([object[]]$Candidates)

    $failures = @()
    foreach ($candidate in $Candidates) {
        $providerPath = ([string]$candidate.registry_path -replace '^HKLM\\', 'HKLM:\')
        $valueName = [string]$candidate.value_name
        $probeValue = [int]$candidate.probe_value
        try {
            New-ItemProperty -Path $providerPath -Name $valueName -PropertyType DWord -Value $probeValue -Force | Out-Null
        }
        catch {
            $failures += [ordered]@{
                candidate_id = $candidate.candidate_id
                value_name = $valueName
                error = $_.Exception.Message
            }
        }
    }

    return @($failures)
}

function Stop-TraceBestEffort {
    foreach ($args in @(
            @('stop', $traceName, '-ets'),
            @('delete', $traceName)
        )) {
        try {
            Invoke-ProcessNoCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments $args -TimeoutSeconds 30 | Out-Null
        }
        catch {
        }
    }
}

function Get-ActiveSchemeGuid {
    $raw = (& powercfg /getactivescheme 2>$null | Out-String)
    if ($raw -match '([A-Fa-f0-9\-]{36})') {
        return $Matches[1]
    }
    return $null
}

function Start-Trace {
    Stop-TraceBestEffort
    foreach ($path in @($etlPath, $csvPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        }
    }

    Write-RunLog -Message ("trace-create-start name={0}" -f $traceName)
    $create = Invoke-ProcessNoCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @(
        'create', 'trace', $traceName,
        '-p', 'Microsoft-Windows-Kernel-Registry',
        '-o', $etlPath,
        '-bs', '64',
        '-nb', '64', '256'
    ) -TimeoutSeconds 90
    if ($create.timed_out) {
        throw "logman create timed out for trace session $traceName."
    }
    if ($create.exit_code -ne 0) {
        throw "logman create failed for ${traceName}: stdout=$($create.stdout) stderr=$($create.stderr)"
    }
    Write-RunLog -Message ("trace-create-complete name={0}" -f $traceName)

    Write-RunLog -Message ("trace-start-begin name={0}" -f $traceName)
    $start = Invoke-ProcessNoCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('start', $traceName, '-ets') -TimeoutSeconds 90
    if ($start.timed_out) {
        throw "logman start timed out for trace session $traceName."
    }
    if ($start.exit_code -ne 0) {
        throw "logman start failed for ${traceName}: stdout=$($start.stdout) stderr=$($start.stderr)"
    }
    Write-RunLog -Message ("trace-start-complete name={0}" -f $traceName)

    $sessionOutput = Get-TraceSessionOutputLocation -TraceName $traceName
    if ($sessionOutput) {
        $script:activeTraceOutputPath = [string]$sessionOutput.path
        Write-RunLog -Message ("trace-start-output path={0} source={1}" -f $sessionOutput.path, $sessionOutput.source)
    }
    else {
        $script:activeTraceOutputPath = $null
        Write-RunLog -Message ("trace-start-output path=unknown name={0}" -f $traceName)
    }
}

function Stop-Trace {
    Write-RunLog -Message ("trace-stop-begin name={0}" -f $traceName)
    $stop = Invoke-ProcessNoCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $traceName, '-ets') -TimeoutSeconds 90
    if ($stop.timed_out) {
        throw "logman stop timed out for trace session $traceName."
    }
    if ($stop.exit_code -ne 0) {
        throw "logman stop failed for ${traceName}: stdout=$($stop.stdout) stderr=$($stop.stderr)"
    }

    Write-RunLog -Message ("trace-stop-complete name={0}" -f $traceName)
    Write-RunLog -Message ("trace-delete-begin name={0}" -f $traceName)
    $delete = Invoke-ProcessNoCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $traceName) -TimeoutSeconds 60
    if ($delete.timed_out) {
        throw "logman delete timed out for trace session $traceName."
    }
    if ($delete.exit_code -ne 0) {
        throw "logman delete failed for ${traceName}: stdout=$($delete.stdout) stderr=$($delete.stderr)"
    }
    Write-RunLog -Message ("trace-delete-complete name={0}" -f $traceName)
}

function Invoke-Trigger {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    $started = Get-Date
    $status = 'completed'
    $errorText = $null
    Write-RunLog -Message ("trigger-start name={0}" -f $Name)
    try {
        & $Action
    }
    catch {
        $status = 'error'
        $errorText = $_.Exception.Message
        Write-RunLog -Message ("trigger-error name={0} error={1}" -f $Name, $errorText)
    }
    if ($status -eq 'completed') {
        Write-RunLog -Message ("trigger-complete name={0}" -f $Name)
    }

    return [ordered]@{
        name = $Name
        start_utc = $started.ToUniversalTime().ToString('o')
        end_utc = (Get-Date).ToUniversalTime().ToString('o')
        status = $status
        error = $errorText
    }
}

function Invoke-CpuStressTrigger {
    $coreCount = [Math]::Min([Environment]::ProcessorCount, 8)
    $jobs = 1..$coreCount | ForEach-Object {
        Start-Job -ScriptBlock {
            $end = (Get-Date).AddSeconds(10)
            while ((Get-Date) -lt $end) {
                [Math]::Sqrt(12345.6789) | Out-Null
            }
        }
    }
    Start-Sleep -Seconds 11
    $jobs | Stop-Job -ErrorAction SilentlyContinue | Out-Null
    $jobs | Remove-Job -Force -ErrorAction SilentlyContinue | Out-Null
}

function Invoke-PowerPlanTrigger {
    $original = Get-ActiveSchemeGuid
    & powercfg /setactive SCHEME_MIN 2>$null | Out-Null
    Start-Sleep -Seconds 3
    & powercfg /setactive SCHEME_BALANCED 2>$null | Out-Null
    Start-Sleep -Seconds 3

    Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class RegProbePowerRequest {
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern IntPtr PowerCreateRequest(ref REASON_CONTEXT Context);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern bool PowerSetRequest(IntPtr PowerRequest, int RequestType);
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern bool PowerClearRequest(IntPtr PowerRequest, int RequestType);
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct REASON_CONTEXT {
        public uint Version;
        public uint Flags;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string SimpleReasonString;
    }
}
"@

    $ctx = New-Object RegProbePowerRequest+REASON_CONTEXT
    $ctx.Version = 0
    $ctx.Flags = 1
    $ctx.SimpleReasonString = 'RegProbe mega trigger'
    $request = [RegProbePowerRequest]::PowerCreateRequest([ref]$ctx)
    if ($request -ne [IntPtr]::Zero) {
        foreach ($kind in @(0, 1, 3)) {
            [RegProbePowerRequest]::PowerSetRequest($request, $kind) | Out-Null
            Start-Sleep -Seconds 2
        }
        foreach ($kind in @(0, 1, 3)) {
            [RegProbePowerRequest]::PowerClearRequest($request, $kind) | Out-Null
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($original)) {
        & powercfg /setactive $original 2>$null | Out-Null
    }
}

function Invoke-ThreadBurstTrigger {
    $jobs = 1..50 | ForEach-Object {
        Start-Job -ScriptBlock {
            Start-Sleep -Milliseconds (Get-Random -Minimum 200 -Maximum 2000)
        }
    }
    Start-Sleep -Seconds 5
    $jobs | Stop-Job -ErrorAction SilentlyContinue | Out-Null
    $jobs | Remove-Job -Force -ErrorAction SilentlyContinue | Out-Null
}

function Invoke-DiskIoTrigger {
    $root = 'C:\RegProbe-Work\mega-io-burst'
    New-Item -Path $root -ItemType Directory -Force | Out-Null
    try {
        1..50 | ForEach-Object {
            $file = Join-Path $root ("f{0}.bin" -f $_)
            [byte[]]$data = New-Object byte[] 1048576
            [IO.File]::WriteAllBytes($file, $data)
        }
    }
    finally {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-ProcessBurstTrigger {
    1..20 | ForEach-Object {
        $proc = Start-Process -FilePath 'C:\Windows\System32\cmd.exe' -ArgumentList '/c exit' -WindowStyle Hidden -PassThru
        $proc.WaitForExit(5000) | Out-Null
    }
}

function Invoke-ForegroundSwitchTrigger {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class RegProbeUser32 {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

    $first = Start-Process -FilePath 'notepad.exe' -PassThru
    $second = Start-Process -FilePath 'notepad.exe' -PassThru
    try {
        try {
            $null = $first.WaitForInputIdle(5000)
        }
        catch {
        }
        try {
            $null = $second.WaitForInputIdle(5000)
        }
        catch {
        }
        Start-Sleep -Seconds 2
        1..10 | ForEach-Object {
            $first.Refresh()
            $second.Refresh()
            $firstHandle = [IntPtr]::Zero
            $secondHandle = [IntPtr]::Zero
            if ($first.MainWindowHandle) {
                $firstHandle = [IntPtr]$first.MainWindowHandle
            }
            if ($second.MainWindowHandle) {
                $secondHandle = [IntPtr]$second.MainWindowHandle
            }

            if ($firstHandle -ne [IntPtr]::Zero) {
                [RegProbeUser32]::SetForegroundWindow($firstHandle) | Out-Null
            }
            Start-Sleep -Milliseconds 200
            if ($secondHandle -ne [IntPtr]::Zero) {
                [RegProbeUser32]::SetForegroundWindow($secondHandle) | Out-Null
            }
            Start-Sleep -Milliseconds 200
        }
    }
    finally {
        $first, $second | Where-Object { $_ -and -not $_.HasExited } | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-TimerResolutionTrigger {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class RegProbeTimerApi {
    [DllImport("ntdll.dll")]
    public static extern int NtSetTimerResolution(int DesiredResolution, bool SetResolution, out int CurrentResolution);
    [DllImport("winmm.dll")]
    public static extern uint timeBeginPeriod(uint uPeriod);
    [DllImport("winmm.dll")]
    public static extern uint timeEndPeriod(uint uPeriod);
}
"@

    $grantedResolution = 0
    [RegProbeTimerApi]::NtSetTimerResolution(5000, $true, [ref]$grantedResolution) | Out-Null
    [RegProbeTimerApi]::timeBeginPeriod(1) | Out-Null
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        while ($stopwatch.Elapsed.TotalSeconds -lt 6) {
            [Math]::Sqrt(9999.99) | Out-Null
            Start-Sleep -Milliseconds 25
        }
    }
    finally {
        [RegProbeTimerApi]::timeEndPeriod(1) | Out-Null
        $releasedResolution = 0
        [RegProbeTimerApi]::NtSetTimerResolution(5000, $false, [ref]$releasedResolution) | Out-Null
    }
}

function Invoke-NetworkTrigger {
    1..10 | ForEach-Object {
        Test-Connection -ComputerName localhost -Count 1 -ErrorAction SilentlyContinue | Out-Null
        try {
            Invoke-WebRequest -Uri 'http://127.0.0.1' -UseBasicParsing -TimeoutSec 1 -ErrorAction SilentlyContinue | Out-Null
        }
        catch {
        }
    }
}

function New-RunningSummary {
    param(
        [Parameter(Mandatory = $true)]
        [object]$State,
        [Parameter(Mandatory = $true)]
        [object[]]$Candidates
    )

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        total_candidates = @($Candidates).Count
        csv_exists = $false
        csv_line_count = 0
        path_line_count = 0
        exact_hit_count = 0
        exact_line_only_count = 0
        path_only_count = 0
        no_hit_count = 0
        trigger_count = @($State.trigger_log).Count + @($State.remaining_triggers).Count
        completed_trigger_count = @($State.trigger_log).Count
        trigger_error_count = @($State.trigger_log | Where-Object { $_.status -eq 'error' }).Count
        current_trigger = $State.current_trigger
        baseline_missing_count = @($State.baseline_values.PSObject.Properties | Where-Object { $null -eq $_.Value }).Count
        apply_failure_count = @($State.apply_failures).Count
        status = 'running'
    }
}

function Parse-Results {
    param(
        [object[]]$Candidates,
        [string[]]$Lines,
        [bool]$CsvExists = $false,
        [string]$ParserSource = 'unknown'
    )

    $pathPattern = [regex]::Escape('SYSTEM\\CurrentControlSet\\Control\\Power')
    $lineCount = 0
    $pathLineCount = 0
    $results = [ordered]@{}

    foreach ($candidate in $Candidates) {
        $results[[string]$candidate.candidate_id] = [ordered]@{
            candidate_id = [string]$candidate.candidate_id
            value_name = [string]$candidate.value_name
            exact_line_count = 0
            exact_query_hits = 0
            status = 'no-hit'
            sample_lines = @()
        }
    }

    foreach ($line in @($Lines)) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $lineCount++
        if ($line -match $pathPattern) {
            $pathLineCount++
        }

        foreach ($candidate in $Candidates) {
            $bucket = $results[[string]$candidate.candidate_id]
            $valuePattern = [regex]::Escape([string]$bucket.value_name)
            if ($line -match $valuePattern) {
                $bucket.exact_line_count++
                if ($line -match 'RegQueryValue|QueryValue|CmpQueryValueKey|NtQueryValueKey') {
                    $bucket.exact_query_hits++
                }
                if (@($bucket.sample_lines).Count -lt 3) {
                    $bucket.sample_lines += $line
                }
            }
        }
    }

    foreach ($bucket in $results.Values) {
        $bucket.status = if ($bucket.exact_query_hits -gt 0) {
            'exact-hit'
        }
        elseif ($bucket.exact_line_count -gt 0) {
            'exact-line-no-query'
        }
        elseif ($pathLineCount -gt 0) {
            'path-only-hit'
        }
        else {
            'no-hit'
        }
    }

    return [ordered]@{
        csv_exists = $CsvExists
        csv_line_count = $lineCount
        path_line_count = $pathLineCount
        parser_source = $ParserSource
        candidates = @($results.Values)
    }
}

function Wait-TraceArtifactReady {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [int]$TimeoutSeconds = 60,
        [int]$PollMilliseconds = 1000,
        [int]$StablePollsRequired = 3
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $previousLength = -1L
    $stablePolls = 0

    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $Path) {
            $item = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
            if ($item -and $item.Length -gt 0) {
                if ($item.Length -eq $previousLength) {
                    $stablePolls++
                }
                else {
                    $previousLength = [int64]$item.Length
                    $stablePolls = 1
                }

                if ($stablePolls -ge $StablePollsRequired) {
                    return [ordered]@{
                        ready = $true
                        exists = $true
                        length = [int64]$item.Length
                        stable_polls = $stablePolls
                        last_write_utc = $item.LastWriteTimeUtc.ToString('o')
                    }
                }
            }
        }

        Start-Sleep -Milliseconds $PollMilliseconds
    }

    $finalItem = if (Test-Path -LiteralPath $Path) { Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue } else { $null }
    return [ordered]@{
        ready = $false
        exists = [bool]$finalItem
        length = if ($finalItem) { [int64]$finalItem.Length } else { 0 }
        stable_polls = $stablePolls
        last_write_utc = if ($finalItem) { $finalItem.LastWriteTimeUtc.ToString('o') } else { $null }
    }
}

function Get-TraceSessionOutputLocation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TraceName
    )

    $query = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('query', $TraceName, '-ets') -TimeoutSeconds 30
    if ($query.timed_out -or $query.exit_code -ne 0) {
        return $null
    }

    $stdout = [string]$query.stdout
    $outputLocation = $null
    $rootPath = $null
    foreach ($line in ($stdout -split "`r?`n")) {
        if ($line -match '^\s*Output Location:\s+(.+?)\s*$') {
            $outputLocation = $Matches[1].Trim()
            continue
        }
        if ($line -match '^\s*Root Path:\s+(.+?)\s*$') {
            $rootPath = $Matches[1].Trim()
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($outputLocation)) {
        return [ordered]@{
            path = $outputLocation
            source = 'logman-query-output'
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($rootPath)) {
        return [ordered]@{
            path = (Join-Path $rootPath ("{0}.etl" -f $TraceName))
            source = 'logman-query-root'
        }
    }

    return $null
}

function Resolve-TraceArtifactPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PreferredPath
    )

    if (Test-Path -LiteralPath $PreferredPath) {
        return [ordered]@{
            path = $PreferredPath
            source = 'preferred'
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($script:activeTraceOutputPath)) {
        if (Test-Path -LiteralPath $script:activeTraceOutputPath) {
            return [ordered]@{
                path = $script:activeTraceOutputPath
                source = 'active-output'
            }
        }
    }

    foreach ($direct in @(
            @{ Path = $script:activeTraceOutputPath; Source = 'active-output-hint' },
            @{ Path = if (-not [string]::IsNullOrWhiteSpace($traceName)) { Join-Path 'C:\Windows\System32' ("{0}.etl" -f $traceName) } else { $null }; Source = 'system32-trace-name-hint' },
            @{ Path = $PreferredPath; Source = 'preferred-hint' }
        )) {
        $candidatePath = [string]$direct.Path
        if ([string]::IsNullOrWhiteSpace($candidatePath)) {
            continue
        }

        if (Test-Path -LiteralPath $candidatePath) {
            return [ordered]@{
                path = $candidatePath
                source = ($direct.Source -replace '-hint$', '')
            }
        }
    }

    $system32ByName = if (-not [string]::IsNullOrWhiteSpace($traceName)) { Join-Path 'C:\Windows\System32' ("{0}.etl" -f $traceName) } else { $null }
    if (-not [string]::IsNullOrWhiteSpace($system32ByName)) {
        return [ordered]@{
            path = $system32ByName
            source = 'system32-trace-name-hint'
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($script:activeTraceOutputPath)) {
        return [ordered]@{
            path = $script:activeTraceOutputPath
            source = 'active-output-hint'
        }
    }

    return [ordered]@{
        path = $PreferredPath
        source = 'preferred-hint'
    }
}

function Get-TraceLinesFromCsv {
    if (-not (Test-Path -LiteralPath $csvPath)) {
        return @()
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $reader = [System.IO.File]::OpenText($csvPath)
    try {
        while (($line = $reader.ReadLine()) -ne $null) {
            $lines.Add($line)
        }
    }
    finally {
        $reader.Close()
    }

    return @($lines)
}

function Get-BinaryMatchCount {
    param(
        [byte[]]$Bytes,
        [byte[]]$Pattern
    )

    if ($null -eq $Bytes -or $null -eq $Pattern -or $Pattern.Length -eq 0 -or $Bytes.Length -lt $Pattern.Length) {
        return 0
    }

    $count = 0
    for ($index = 0; $index -le ($Bytes.Length - $Pattern.Length); $index++) {
        $matched = $true
        for ($patternIndex = 0; $patternIndex -lt $Pattern.Length; $patternIndex++) {
            if ($Bytes[$index + $patternIndex] -ne $Pattern[$patternIndex]) {
                $matched = $false
                break
            }
        }

        if ($matched) {
            $count++
        }
    }

    return $count
}

function Get-TraceLinesFromEtlBinary {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object[]]$Candidates
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return @()
    }

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $matches = New-Object System.Collections.Generic.List[string]
    $pathToken = 'SYSTEM\CurrentControlSet\Control\Power'
    $pathAscii = [System.Text.Encoding]::ASCII.GetBytes($pathToken)
    $pathUnicode = [System.Text.Encoding]::Unicode.GetBytes($pathToken)
    $pathMatchCount = (Get-BinaryMatchCount -Bytes $bytes -Pattern $pathAscii) + (Get-BinaryMatchCount -Bytes $bytes -Pattern $pathUnicode)
    if ($pathMatchCount -gt 0) {
        $matches.Add(("ETL_BINARY_MATCH path={0} count={1}" -f $pathToken, $pathMatchCount))
    }

    foreach ($candidate in $Candidates) {
        $valueName = [string]$candidate.value_name
        if ([string]::IsNullOrWhiteSpace($valueName)) {
            continue
        }

        $ascii = [System.Text.Encoding]::ASCII.GetBytes($valueName)
        $unicode = [System.Text.Encoding]::Unicode.GetBytes($valueName)
        $matchCount = (Get-BinaryMatchCount -Bytes $bytes -Pattern $ascii) + (Get-BinaryMatchCount -Bytes $bytes -Pattern $unicode)
        if ($matchCount -gt 0) {
            $matches.Add(("ETL_BINARY_MATCH value={0} count={1}" -f $valueName, $matchCount))
        }
    }

    return @($matches)
}

function Invoke-TracerptParse {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TracePath,
        [Parameter(Mandatory = $true)]
        [string]$CsvOutputPath,
        [int]$Attempts = 2,
        [int]$TimeoutSeconds = 180
    )

    $attemptLog = New-Object System.Collections.Generic.List[object]
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        if (Test-Path -LiteralPath $CsvOutputPath) {
            Remove-Item -LiteralPath $CsvOutputPath -Force -ErrorAction SilentlyContinue
        }

        $result = Invoke-CmdCapture -FilePath 'C:\Windows\System32\tracerpt.exe' -Arguments @($TracePath, '-o', $CsvOutputPath, '-of', 'CSV', '-y') -TimeoutSeconds $TimeoutSeconds
        $csvExists = Test-Path -LiteralPath $CsvOutputPath
        $csvLength = if ($csvExists) { (Get-Item -LiteralPath $CsvOutputPath).Length } else { 0 }
        $attemptLog.Add([ordered]@{
                attempt = $attempt
                exit_code = $result.exit_code
                timed_out = $result.timed_out
                csv_exists = $csvExists
                csv_length = $csvLength
                stderr = $result.stderr
            })

        if (-not $result.timed_out -and $result.exit_code -eq 0 -and $csvExists -and $csvLength -gt 0) {
            return [ordered]@{
                success = $true
                attempt = $attempt
                exit_code = $result.exit_code
                timed_out = $result.timed_out
                csv_exists = $csvExists
                csv_length = $csvLength
                stdout = $result.stdout
                stderr = $result.stderr
                attempts = @($attemptLog)
            }
        }

        if ($attempt -lt $Attempts) {
            Start-Sleep -Seconds 3
        }
    }

    $lastAttempt = @($attemptLog)[@($attemptLog).Count - 1]
    return [ordered]@{
        success = $false
        attempt = $lastAttempt.attempt
        exit_code = $lastAttempt.exit_code
        timed_out = $lastAttempt.timed_out
        csv_exists = $lastAttempt.csv_exists
        csv_length = $lastAttempt.csv_length
        stdout = ''
        stderr = $lastAttempt.stderr
        attempts = @($attemptLog)
    }
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null
if (Test-Path -LiteralPath $runLogPath) {
    Remove-Item -LiteralPath $runLogPath -Force -ErrorAction SilentlyContinue
}
Write-RunLog -Message ("phase-start phase={0}" -f $Phase)
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$traceSeed = if ($manifest.PSObject.Properties.Name -contains 'probe_name') {
    [string]$manifest.probe_name
}
else {
    [System.IO.Path]::GetFileName($GuestRoot)
}
$traceSuffix = ($traceSeed -replace '[^A-Za-z0-9]', '')
if ([string]::IsNullOrWhiteSpace($traceSuffix)) {
    $traceSuffix = (Get-Date -Format 'yyyyMMddHHmmss')
}
if ($traceSuffix.Length -gt 32) {
    $traceSuffix = $traceSuffix.Substring($traceSuffix.Length - 32)
}
$script:traceName = "RegProbe$traceSuffix"
Write-RunLog -Message ("manifest-loaded candidates={0} triggers={1} trace_name={2}" -f @($manifest.candidates).Count, @($manifest.triggers).Count, $traceName)
$candidates = @($manifest.candidates)
$triggerNames = @($manifest.triggers)

try {
    if ($Phase -eq 'arm') {
        foreach ($path in @($etlPath, $csvPath, $SummaryPath, $ResultsPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            }
        }

        Stop-TraceBestEffort
        $baselineValues = Get-BaselineValues -Candidates $candidates
        $applyFailures = Apply-CandidateValues -Candidates $candidates
        $state = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            phase = 'armed'
            pattern = 'mega-trigger'
            current_trigger = $null
            trigger_log = @()
            remaining_triggers = @($triggerNames)
            baseline_values = $baselineValues
            candidate_values = (Get-BaselineValues -Candidates $candidates)
            apply_failures = @($applyFailures)
        }
        Write-JsonFile -Path $StatePath -InputObject $state
        Write-RunLog -Message 'arm-state-written'
        Write-JsonFile -Path $SummaryPath -InputObject ([ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            phase = 'arm'
            pattern = 'mega-trigger'
            status = if (@($applyFailures).Count -eq 0) { 'armed' } else { 'armed-with-errors' }
            apply_failure_count = @($applyFailures).Count
            baseline_missing_count = @($baselineValues.Values | Where-Object { $null -eq $_ }).Count
        })
        Write-RunLog -Message 'arm-summary-written'
        Write-RunLog -Message ("phase-complete phase=arm apply_failures={0}" -f @($applyFailures).Count)
        exit 0
    }

    $state = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
    $state.phase = 'running'
    $state.current_trigger = $null
    Write-JsonFile -Path $StatePath -InputObject $state
    Write-RunLog -Message 'run-state-written'
    Write-JsonFile -Path $SummaryPath -InputObject (New-RunningSummary -State $state -Candidates $candidates)
    Write-RunLog -Message 'run-summary-written'

    Start-Trace
    Write-RunLog -Message 'trace-started'
    Start-Sleep -Seconds 2

    $triggerMap = @{
        cpu_stress = { Invoke-CpuStressTrigger }
        power_plan_and_requests = { Invoke-PowerPlanTrigger }
        multi_thread_burst = { Invoke-ThreadBurstTrigger }
        disk_io_burst = { Invoke-DiskIoTrigger }
        process_spawn_burst = { Invoke-ProcessBurstTrigger }
        foreground_background_switch = { Invoke-ForegroundSwitchTrigger }
        timer_resolution_change = { Invoke-TimerResolutionTrigger }
        network_activity = { Invoke-NetworkTrigger }
    }

    foreach ($triggerName in $triggerNames) {
        if (-not $triggerMap.ContainsKey($triggerName)) {
            throw "Unknown trigger in manifest: $triggerName"
        }

        $state.current_trigger = $triggerName
        $state.remaining_triggers = @($state.remaining_triggers | Where-Object { $_ -ne $triggerName })
        Write-JsonFile -Path $StatePath -InputObject $state
        Write-JsonFile -Path $SummaryPath -InputObject (New-RunningSummary -State $state -Candidates $candidates)

        $entry = Invoke-Trigger -Name $triggerName -Action $triggerMap[$triggerName]
        $state.trigger_log = @($state.trigger_log) + $entry
        Write-JsonFile -Path $StatePath -InputObject $state
        Write-JsonFile -Path $SummaryPath -InputObject (New-RunningSummary -State $state -Candidates $candidates)
    }

    $state.current_trigger = $null
    $state.phase = 'parsing'
    Write-JsonFile -Path $StatePath -InputObject $state
    Write-RunLog -Message 'parse-state-written'
    Write-JsonFile -Path $SummaryPath -InputObject (New-RunningSummary -State $state -Candidates $candidates)

    Start-Sleep -Seconds 2
    Stop-Trace
    Write-RunLog -Message 'trace-stopped'

    $resolvedTrace = Resolve-TraceArtifactPath -PreferredPath $etlPath
    if ($resolvedTrace) {
        Write-RunLog -Message ("trace-artifact-path path={0} source={1}" -f $resolvedTrace.path, $resolvedTrace.source)
    }
    else {
        Write-RunLog -Message ("trace-artifact-path path=missing source=none preferred={0}" -f $etlPath)
    }

    $traceArtifactPath = if ($resolvedTrace) { [string]$resolvedTrace.path } else { $etlPath }
    $etlReadiness = Wait-TraceArtifactReady -Path $traceArtifactPath -TimeoutSeconds 45 -PollMilliseconds 1000 -StablePollsRequired 3
    Write-RunLog -Message ("trace-artifact-ready ready={0} exists={1} length={2} stable_polls={3} path={4}" -f $etlReadiness.ready, $etlReadiness.exists, $etlReadiness.length, $etlReadiness.stable_polls, $traceArtifactPath)

    $traceLines = @()
    $parserSource = 'etl-binary-fallback'
    $tracerpt = [ordered]@{
        success = $false
        attempt = 0
        exit_code = $null
        timed_out = $false
        csv_exists = $false
        csv_length = 0
        stdout = ''
        stderr = 'skipped-for-binary-first-runtime-parser'
        attempts = @()
    }
    Write-RunLog -Message 'trace-parse-start parser=etl-binary'
    $traceLines = Get-TraceLinesFromEtlBinary -Path $traceArtifactPath -Candidates $candidates
    Write-RunLog -Message ("trace-parsed parser=etl-binary line_count={0}" -f @($traceLines).Count)

    $parsed = Parse-Results -Candidates $candidates -Lines $traceLines -CsvExists (Test-Path -LiteralPath $csvPath) -ParserSource $parserSource
    Write-RunLog -Message ("trace-results-parsed candidate_count={0}" -f @($parsed.candidates).Count)
    $exactHit = @($parsed.candidates | Where-Object { $_.status -eq 'exact-hit' })
    $exactLineOnly = @($parsed.candidates | Where-Object { $_.status -eq 'exact-line-no-query' })
    $pathOnly = @($parsed.candidates | Where-Object { $_.status -eq 'path-only-hit' })
    $noHit = @($parsed.candidates | Where-Object { $_.status -eq 'no-hit' })
    $triggerErrors = @($state.trigger_log | Where-Object { $_.status -eq 'error' })
    Write-RunLog -Message ("trace-results-classified exact={0} exact_line={1} path_only={2} no_hit={3}" -f @($exactHit).Count, @($exactLineOnly).Count, @($pathOnly).Count, @($noHit).Count)

    Write-RunLog -Message 'finalize-build-start'
    $results = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        total_candidates = @($candidates).Count
        csv_exists = $parsed.csv_exists
        csv_line_count = $parsed.csv_line_count
        path_line_count = $parsed.path_line_count
        parser_source = $parsed.parser_source
        etl_path = $traceArtifactPath
        etl_path_source = if ($resolvedTrace) { [string]$resolvedTrace.source } else { 'missing' }
        etl_ready = [bool]$etlReadiness.ready
        etl_length = [int64]$etlReadiness.length
        tracerpt = $tracerpt
        exact_hit_count = @($exactHit).Count
        exact_line_only_count = @($exactLineOnly).Count
        path_only_count = @($pathOnly).Count
        no_hit_count = @($noHit).Count
        trigger_log = @($state.trigger_log)
        candidates = @($parsed.candidates)
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        total_candidates = @($candidates).Count
        etl_exists = [bool](Test-Path -LiteralPath $traceArtifactPath)
        etl_length = if (Test-Path -LiteralPath $traceArtifactPath) { (Get-Item -LiteralPath $traceArtifactPath).Length } else { 0 }
        csv_exists = $parsed.csv_exists
        csv_line_count = $parsed.csv_line_count
        path_line_count = $parsed.path_line_count
        parser_source = $parsed.parser_source
        etl_path = $traceArtifactPath
        etl_path_source = if ($resolvedTrace) { [string]$resolvedTrace.source } else { 'missing' }
        etl_ready = [bool]$etlReadiness.ready
        tracerpt = $tracerpt
        exact_hit_count = @($exactHit).Count
        exact_line_only_count = @($exactLineOnly).Count
        path_only_count = @($pathOnly).Count
        no_hit_count = @($noHit).Count
        trigger_count = @($triggerNames).Count
        completed_trigger_count = @($triggerNames).Count
        trigger_error_count = @($triggerErrors).Count
        current_trigger = $null
        baseline_missing_count = @($state.baseline_values.PSObject.Properties | Where-Object { $null -eq $_.Value }).Count
        apply_failure_count = @($state.apply_failures).Count
        exact_hit_candidates = @($exactHit | ForEach-Object { $_.candidate_id })
        exact_line_only_candidates = @($exactLineOnly | ForEach-Object { $_.candidate_id })
        path_only_candidates = @($pathOnly | ForEach-Object { $_.candidate_id })
        no_hit_candidates = @($noHit | ForEach-Object { $_.candidate_id })
        status = if (@($exactHit).Count -gt 0) { 'exact-hit' } elseif (@($exactLineOnly).Count -gt 0) { 'exact-line-no-query' } elseif (@($pathOnly).Count -gt 0) { 'path-only-hit' } else { 'no-hit' }
    }
    Write-RunLog -Message ("finalize-build-complete status={0}" -f $summary.status)

    Write-RunLog -Message 'finalize-write-summary-start'
    Write-JsonFileDirect -Path $SummaryPath -InputObject $summary
    Write-RunLog -Message 'finalize-write-summary-complete'
    try {
        $state.phase = 'completed'
        $state.result_status = $summary.status
        Write-RunLog -Message 'finalize-write-state-start'
        Write-JsonFileDirect -Path $StatePath -InputObject $state
        Write-RunLog -Message 'finalize-write-state-complete'
    }
    catch {
        Write-RunLog -Message ("finalize-write-state-error error={0}" -f $_.Exception.Message)
    }
    try {
        Write-RunLog -Message 'finalize-write-results-start'
        Write-JsonFileDirect -Path $ResultsPath -InputObject $results
        Write-RunLog -Message 'finalize-write-results-complete'
    }
    catch {
        Write-RunLog -Message ("finalize-write-results-error error={0}" -f $_.Exception.Message)
    }
    Write-RunLog -Message ("phase-complete phase=run status={0}" -f $summary.status)
}
catch {
    Stop-TraceBestEffort
    if (Test-Path -LiteralPath $StatePath) {
        $state = Read-JsonFile -Path $StatePath
        if ($state) {
            $state.phase = 'error'
            $state.error = $_.Exception.Message
            $state.result_status = 'error'
            Write-JsonFile -Path $StatePath -InputObject $state
        }
    }

    $errorCandidates = if ($candidates) {
        @(
            Parse-Results -Candidates $candidates -Lines @() -CsvExists (Test-Path -LiteralPath $csvPath) -ParserSource 'parser-error'
        )[0].candidates
    }
    else {
        @()
    }
    Write-JsonFile -Path $ResultsPath -InputObject ([ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        parser_source = 'parser-error'
        error = $_.Exception.Message
        candidates = @($errorCandidates)
    })
    Write-JsonFile -Path $SummaryPath -InputObject ([ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'power-control'
        pattern = 'mega-trigger'
        status = 'error'
        error = $_.Exception.Message
    })
    Write-RunLog -Message ("phase-error phase={0} error={1}" -f $Phase, $_.Exception.Message)
    throw
}
finally {
    Stop-TraceBestEffort
}
