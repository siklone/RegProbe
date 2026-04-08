[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [ValidateSet('custom', 'uuid-rpc-com-burst', 'uac-policy-surface-burst', 'session-manager-io-raw-burst', 'executive-worker-burst', 'hiber-file-size-burst', 'watchdog-power-burst', 'power-request-simulation', 'timer-dpc-stress')]
    [string]$TriggerProfile = 'custom',

    [int]$SaveAsTimeoutSeconds = 60,

    [string]$PowerShellCommand = '',
    [string]$ScriptsRoot = 'C:\Tools\Scripts',
    [string]$OutputRoot = '',
    [string]$UploadBaseUrl = '',
    [string[]]$MatchFragments = @(),
    [string[]]$ProcessNames = @()
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

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path $Path)) {
        return $null
    }

    $targetUri = '{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName
    Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
    return [ordered]@{
        path = $Path
        uri = $targetUri
    }
}

function Resolve-TriggerCommand {
    param(
        [Parameter(Mandatory = $true)][string]$Profile,
        [string]$CustomCommand
    )

    switch ($Profile) {
        'uuid-rpc-com-burst' {
            return @'
1..80 | ForEach-Object {
    [guid]::NewGuid() | Out-Null
    try { [System.Runtime.InteropServices.Marshal]::GenerateGuidForType([type][string]) | Out-Null } catch {}
    foreach ($progId in 'WScript.Shell','Shell.Application','Scripting.Dictionary') {
        try {
            $obj = New-Object -ComObject $progId
            if ($obj) {
                [void]$obj
            }
        }
        catch {
        }
    }
    try { Get-CimInstance Win32_ComputerSystemProduct | Select-Object -ExpandProperty UUID | Out-Null } catch {}
    try { cmd /c wmic csproduct get uuid >nul 2>nul } catch {}
    Start-Sleep -Milliseconds 150
}
'@
        }
        'uac-policy-surface-burst' {
            return @'
foreach ($target in @("$env:SystemRoot\System32\ComputerDefaults.exe", "$env:SystemRoot\System32\fodhelper.exe")) {
    try {
        $proc = Start-Process -FilePath $target -PassThru -ErrorAction Stop
        Start-Sleep -Seconds 4
        if ($proc -and -not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
    }
}

try {
    cmd /c gpupdate /target:computer /force >nul 2>nul
}
catch {
}
'@
        }
        'session-manager-io-raw-burst' {
            return @'
try {
    $path = 'C:\RegProbe-Diag\io-session-manager'
    New-Item -ItemType Directory -Path $path -Force | Out-Null
    1..24 | ForEach-Object {
        $filePath = Join-Path $path ("io" + $_ + '.bin')
        $data = New-Object byte[] 1048576
        [System.IO.File]::WriteAllBytes($filePath, $data)
    }

    try {
        $stream = [System.IO.File]::Open('\\.\C:', [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $buffer = New-Object byte[] 4096
        $null = $stream.Read($buffer, 0, $buffer.Length)
        $stream.Close()
    }
    catch {
    }

    cmd /c 'fsutil fsinfo ntfsInfo C:' | Out-Null
    cmd /c 'fltmc volumes' | Out-Null
    cmd /c 'mountvol' | Out-Null
    Get-Volume | Out-Null
    Get-Disk | Out-Null
}
catch {
}
finally {
    Remove-Item -Path 'C:\RegProbe-Diag\io-session-manager' -Recurse -Force -ErrorAction SilentlyContinue
}
'@
        }
        'executive-worker-burst' {
            return @'
try {
    $diagPath = 'C:\RegProbe-Diag\executive-worker-burst'
    New-Item -ItemType Directory -Path $diagPath -Force | Out-Null

    cmd /c "tasklist /svc > `"$diagPath\tasklist.txt`"" | Out-Null
    cmd /c "sc query type= service state= all > `"$diagPath\sc-all.txt`"" | Out-Null
    cmd /c "wevtutil el > `"$diagPath\event-logs.txt`"" | Out-Null

    Get-CimInstance Win32_Service |
        Select-Object Name,State,StartMode,ProcessId |
        ConvertTo-Json -Depth 4 |
        Set-Content -Path (Join-Path $diagPath 'cim-services.json') -Encoding UTF8

    $jobs = 1..4 | ForEach-Object {
        Start-Job -Name ('exec-stress-' + $_) -ScriptBlock {
            $deadline = (Get-Date).AddSeconds(12)
            while ((Get-Date) -lt $deadline) {
                Get-CimInstance Win32_Service | Out-Null
                Get-ChildItem 'C:\Windows\System32' -File | Select-Object -First 1200 | Out-Null
                Get-WinEvent -LogName 'System' -MaxEvents 80 | Out-Null
                Start-Sleep -Milliseconds 400
            }
        }
    }

    Wait-Job -Job $jobs | Out-Null
    $jobs |
        Select-Object Id,Name,State,HasMoreData |
        ConvertTo-Json -Depth 4 |
        Set-Content -Path (Join-Path $diagPath 'stress-jobs.json') -Encoding UTF8
    Remove-Job -Job $jobs -Force

    Get-WinEvent -LogName 'System' -MaxEvents 120 |
        Select-Object TimeCreated,Id,ProviderName,LevelDisplayName |
        ConvertTo-Json -Depth 4 |
        Set-Content -Path (Join-Path $diagPath 'system-events.json') -Encoding UTF8

    Start-Sleep -Seconds 5
}
catch {
}
finally {
    Remove-Item -Path $diagPath -Recurse -Force -ErrorAction SilentlyContinue
}
'@
        }
        'hiber-file-size-burst' {
            return @'
try {
    $path = 'C:\RegProbe-Diag\hiber-io-burst'
    New-Item -ItemType Directory -Path $path -Force | Out-Null

    try { cmd /c 'powercfg /hibernate on' | Out-Null } catch {}
    Start-Sleep -Milliseconds 300
    try { cmd /c 'powercfg /hibernate off' | Out-Null } catch {}
    Start-Sleep -Milliseconds 300
    try { cmd /c 'powercfg /a' | Out-Null } catch {}

    1..24 | ForEach-Object {
        $filePath = Join-Path $path ('hiber' + $_ + '.bin')
        $data = New-Object byte[] 2097152
        [System.IO.File]::WriteAllBytes($filePath, $data)
    }
}
catch {
}
finally {
    Remove-Item -Path 'C:\RegProbe-Diag\hiber-io-burst' -Recurse -Force -ErrorAction SilentlyContinue
}
'@
        }
        'watchdog-power-burst' {
            return @'
try {
    $diagPath = 'C:\RegProbe-Diag\watchdog-power-burst'
    New-Item -ItemType Directory -Path $diagPath -Force | Out-Null

    cmd /c "tasklist /svc > `"$diagPath\tasklist.txt`"" | Out-Null
    cmd /c "powercfg /q > `"$diagPath\powercfg-q.txt`"" | Out-Null
    cmd /c "powercfg /a > `"$diagPath\powercfg-a.txt`"" | Out-Null
    cmd /c "sc queryex Power > `"$diagPath\sc-power.txt`"" | Out-Null

    Get-WinEvent -LogName 'System' -MaxEvents 120 |
        Select-Object TimeCreated, Id, ProviderName, LevelDisplayName |
        ConvertTo-Json -Depth 4 |
        Set-Content -Path (Join-Path $diagPath 'system-events.json') -Encoding UTF8

    1..4 | ForEach-Object {
        try { cmd /c 'powercfg /q >nul 2>nul' | Out-Null } catch {}
        try { cmd /c 'powercfg /a >nul 2>nul' | Out-Null } catch {}
        Start-Sleep -Milliseconds 600
    }

    Start-Sleep -Seconds 5
}
catch {
}
finally {
    Remove-Item -Path $diagPath -Recurse -Force -ErrorAction SilentlyContinue
}
'@
        }
        'power-request-simulation' {
            return @'
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

try {
    $diagPath = 'C:\RegProbe-Diag\power-request-simulation'
    New-Item -ItemType Directory -Path $diagPath -Force | Out-Null

    cmd /c "powercfg /requests > `"$diagPath\powercfg-requests-before.txt`"" | Out-Null
    cmd /c "powercfg /requestsoverride > `"$diagPath\powercfg-requestsoverride.txt`"" | Out-Null
    cmd /c "powercfg /energy /duration 5 /output `"$diagPath\energy-report.html`" >nul 2>nul" | Out-Null

    $ctx = New-Object RegProbePowerRequest+REASON_CONTEXT
    $ctx.Version = 0
    $ctx.Flags = 1
    $ctx.SimpleReasonString = 'RegProbe power-request simulation'
    $request = [RegProbePowerRequest]::PowerCreateRequest([ref]$ctx)

    if ($request -ne [IntPtr]::Zero) {
        foreach ($kind in @(0, 1, 3)) {
            [RegProbePowerRequest]::PowerSetRequest($request, $kind) | Out-Null
            Start-Sleep -Seconds 2
            cmd /c "powercfg /requests > `"$diagPath\powercfg-requests-kind-$kind.txt`"" | Out-Null
        }

        foreach ($kind in @(0, 1, 3)) {
            [RegProbePowerRequest]::PowerClearRequest($request, $kind) | Out-Null
        }
    }

    try {
        1..3 | ForEach-Object {
            $player = New-Object System.Media.SoundPlayer
            Start-Sleep -Milliseconds 250
            $player.Dispose()
        }
    }
    catch {
    }

    cmd /c "powercfg /requests > `"$diagPath\powercfg-requests-after.txt`"" | Out-Null
    Start-Sleep -Seconds 4
}
catch {
}
finally {
    Remove-Item -Path 'C:\RegProbe-Diag\power-request-simulation' -Recurse -Force -ErrorAction SilentlyContinue
}
'@
        }
        'timer-dpc-stress' {
            return @'
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

$timers = @()
$jobs = @()
try {
    $callback = [System.Threading.TimerCallback]{
        param($state)
        $iterations = 64
        if ($state -is [int]) {
            $iterations = [Math]::Max([int]$state, 16)
        }

        for ($index = 0; $index -lt $iterations; $index++) {
            [Math]::Sqrt(12345.6789) | Out-Null
        }
    }

    foreach ($periodMs in @(5, 7, 11, 13, 17, 19, 23, 29)) {
        $timers += [System.Threading.Timer]::new($callback, 64, 0, $periodMs)
    }

    $coreCount = [Math]::Min([Environment]::ProcessorCount, 4)
    $jobs = 1..$coreCount | ForEach-Object {
        Start-Job -ScriptBlock {
            $deadline = (Get-Date).AddSeconds(8)
            while ((Get-Date) -lt $deadline) {
                for ($index = 0; $index -lt 2048; $index++) {
                    [Math]::Sqrt(54321.1234) | Out-Null
                }
                Start-Sleep -Milliseconds 2
            }
        }
    }

    $deadline = (Get-Date).AddSeconds(8)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 100
    }
}
catch {
}
finally {
    $jobs | Stop-Job -ErrorAction SilentlyContinue | Out-Null
    $jobs | Remove-Job -Force -ErrorAction SilentlyContinue | Out-Null

    foreach ($timer in @($timers)) {
        if ($timer) {
            $timer.Dispose()
        }
    }

    [RegProbeTimerApi]::timeEndPeriod(1) | Out-Null
    $releasedResolution = 0
    [RegProbeTimerApi]::NtSetTimerResolution(5000, $false, [ref]$releasedResolution) | Out-Null
}
'@
        }
        'custom' {
            if ([string]::IsNullOrWhiteSpace($CustomCommand)) {
                throw 'PowerShellCommand is required when TriggerProfile=custom.'
            }

            return $CustomCommand
        }
        default {
            throw "Unsupported TriggerProfile: $Profile"
        }
    }
}

$probeScript = Join-Path $ScriptsRoot 'registry-policy-probe.ps1'
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\procmon' $OutputName
}

Ensure-Directory -Path $OutputRoot

$txtPath = Join-Path $OutputRoot ('{0}.txt' -f $OutputName)
$csvPath = Join-Path $OutputRoot ('{0}.csv' -f $OutputName)
$hitsCsvPath = Join-Path $OutputRoot ('{0}.hits.csv' -f $OutputName)
$normalizedBundlePath = Join-Path $OutputRoot ('{0}.normalized.json' -f $OutputName)
$probeStagePath = Join-Path $OutputRoot ('{0}.stage.json' -f $OutputName)
$summaryPath = Join-Path $OutputRoot 'run-summary.json'
$triggerCommand = $null
$uploads = [ordered]@{}
$summaryUpload = $null
$hadError = $false
$errorKind = $null
$errorMessage = $null
$errorPosition = $null
$probeStage = $null
$resultErrorLine = $null

try {
    if (-not (Test-Path $probeScript)) {
        throw "registry-policy-probe.ps1 not found at $probeScript"
    }

    $triggerCommand = Resolve-TriggerCommand -Profile $TriggerProfile -CustomCommand $PowerShellCommand

    $probeParams = @{
        Mode = 'capture'
        RegistryPath = $RegistryPath
        ValueName = $ValueName
        Prefix = $OutputName
        OutputDirectory = $OutputRoot
        PowerShellCommand = $triggerCommand
        SaveAsTimeoutSeconds = $SaveAsTimeoutSeconds
    }

    if (-not [string]::IsNullOrWhiteSpace($UploadBaseUrl)) {
        $probeParams.StageUploadUri = ('{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), ('{0}-probe-stage.json' -f $OutputName))
    }

    if (@($MatchFragments).Count -gt 0) {
        $probeParams.MatchFragments = @($MatchFragments)
    }

    if (@($ProcessNames).Count -gt 0) {
        $probeParams.ProcessNames = @($ProcessNames)
    }

    & $probeScript @probeParams
}
catch {
    $hadError = $true
    $errorKind = $_.Exception.GetType().FullName
    $errorMessage = $_.Exception.Message
    if ($_.InvocationInfo) {
        $errorPosition = $_.InvocationInfo.PositionMessage
    }
}
finally {
    if (Test-Path $probeStagePath) {
        try {
            $probeStage = Get-Content -Path $probeStagePath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            if (-not $hadError) {
                $hadError = $true
                $errorKind = 'probe-stage-parse-error'
                $errorMessage = $_.Exception.Message
                if ($_.InvocationInfo) {
                    $errorPosition = $_.InvocationInfo.PositionMessage
                }
            }
        }
    }

    if (Test-Path $txtPath) {
        try {
            $resultErrorLine = Get-Content -Path $txtPath -ErrorAction Stop |
                Where-Object { $_ -like 'ERROR=*' } |
                Select-Object -First 1
        }
        catch {
        }
    }

    if (-not $hadError -and $probeStage -and $probeStage.status -eq 'error') {
        $hadError = $true
        $errorKind = 'probe-stage-error'
        $errorMessage = if (-not [string]::IsNullOrWhiteSpace($probeStage.message)) {
            [string]$probeStage.message
        }
        else {
            'registry-policy-probe.ps1 reported an error stage.'
        }
    }

    if (-not $hadError -and -not [string]::IsNullOrWhiteSpace($resultErrorLine)) {
        $hadError = $true
        $errorKind = 'probe-result-error'
        $errorMessage = $resultErrorLine.Substring('ERROR='.Length)
    }

    foreach ($entry in @(
        @{ key = 'result'; path = $txtPath; name = ('{0}.txt' -f $OutputName) },
        @{ key = 'hits_csv'; path = $hitsCsvPath; name = ('{0}.hits.csv' -f $OutputName) },
        @{ key = 'csv'; path = $csvPath; name = ('{0}.csv' -f $OutputName) },
        @{ key = 'normalized_bundle'; path = $normalizedBundlePath; name = ('{0}.normalized.json' -f $OutputName) }
    )) {
        try {
            $upload = Invoke-ArtifactUpload -Path $entry.path -RemoteName $entry.name
            if ($upload) {
                $uploads[$entry.key] = $upload
            }
        }
        catch {
            $uploads[('{0}_upload_error' -f $entry.key)] = $_.Exception.Message
        }
    }

    $normalizedBundle = $null
    if (Test-Path $normalizedBundlePath) {
        try {
            $normalizedBundle = Get-Content -Path $normalizedBundlePath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            if (-not $hadError) {
                $hadError = $true
                $errorKind = 'normalized-bundle-parse-error'
                $errorMessage = $_.Exception.Message
            }
        }
    }

    $normalizationStatus = if ($normalizedBundle) { [string]$normalizedBundle.status } elseif (Test-Path $normalizedBundlePath) { 'parse-error' } else { 'missing' }
    $normalizationErrors = if ($normalizedBundle -and $normalizedBundle.errors) { @($normalizedBundle.errors) } elseif ($normalizedBundle -and $normalizedBundle.error_kind) { @([string]$normalizedBundle.error_kind) } else { @() }
    if (-not $hadError -and ($normalizationStatus -ne 'ok' -or -not (Test-Path $normalizedBundlePath))) {
        $hadError = $true
        $errorKind = if ($normalizationStatus -eq 'missing') { 'normalized-bundle-missing' } else { 'normalization-error' }
        $errorMessage = if ($normalizationErrors.Count -gt 0) { ($normalizationErrors -join '; ') } else { 'Normalized registry bundle was not produced.' }
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        registry_path = $RegistryPath
        value_name = $ValueName
        output_name = $OutputName
        trigger_profile = $TriggerProfile
        output_root = $OutputRoot
        status = if ($hadError) { 'error' } else { 'ok' }
        result_exists = [bool](Test-Path $txtPath)
        csv_exists = [bool](Test-Path $csvPath)
        hits_csv_exists = [bool](Test-Path $hitsCsvPath)
        normalized_bundle_path = $normalizedBundlePath
        normalized_bundle_exists = [bool](Test-Path $normalizedBundlePath)
        normalized_result_ref = if (Test-Path $normalizedBundlePath) { $normalizedBundlePath } else { $null }
        normalization_status = $normalizationStatus
        normalizer_name = if ($normalizedBundle) { [string]$normalizedBundle.normalizer_name } else { 'GuestProcmonCsvRegistryNormalizer' }
        normalization_errors = @($normalizationErrors)
        probe_stage_exists = [bool](Test-Path $probeStagePath)
        probe_stage = if ($probeStage) { $probeStage.stage } else { $null }
        probe_stage_status = if ($probeStage) { $probeStage.status } else { $null }
        probe_stage_message = if ($probeStage) { $probeStage.message } else { $null }
        result_error_line = $resultErrorLine
        error_kind = $errorKind
        error = $errorMessage
        error_position = $errorPosition
        recovery_action = if ($hadError) { 'inspect-normalized-bundle' } else { 'none' }
        transport_blocker = if ($normalizationStatus -eq 'missing') { 'normalized-bundle-missing' } elseif ($normalizationStatus -ne 'ok') { 'normalization-failed' } else { 'none' }
        guest_health = if ($hadError) { 'degraded' } else { 'stable' }
        uploads = $uploads
    }

    $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8

    try {
        $summaryUpload = Invoke-ArtifactUpload -Path $summaryPath -RemoteName ('{0}-summary.json' -f $OutputName)
    }
    catch {
        $summaryUpload = [ordered]@{
            error = $_.Exception.Message
        }
    }

    if ($summaryUpload) {
        $summary['summary_upload'] = $summaryUpload
        $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
    }
}

if ($hadError) {
    exit 1
}

Write-Output $summaryPath
