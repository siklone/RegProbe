[CmdletBinding()]
param(
    [string]$RunId = 'registry-stackwalk-operator',
    [string]$OutputRoot = 'C:\RegProbe-Diag\etw-stackwalk',
    [string]$RegistryPath = '',
    [string]$ValueName = '',
    [int]$DurationSeconds = 60,
    [string[]]$KernelFlags = @('PROC_THREAD', 'LOADER', 'REGISTRY'),
    [string[]]$StackwalkEvents = @(
        'RegCreateKey',
        'RegOpenKey',
        'RegQueryKey',
        'RegSetValue',
        'RegQueryValue',
        'RegDeleteValue',
        'RegCloseKey'
    ),
    [int]$BufferSizeKb = 1024,
    [int]$MinBuffers = 64,
    [int]$MaxBuffers = 256,
    [switch]$SkipTracerpt
)

$ErrorActionPreference = 'Stop'

$xperf = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\xperf.exe'
$tracerpt = 'C:\Windows\System32\tracerpt.exe'

function ConvertTo-SafeName {
    param([string]$Value)
    $clean = ($Value -replace '[^A-Za-z0-9_.-]+', '-').Trim('-')
    if ([string]::IsNullOrWhiteSpace($clean)) {
        return 'registry-stackwalk'
    }
    return $clean
}

function Invoke-NativeProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [switch]$IgnoreExitCode
    )

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-etw-stackwalk-' + [Guid]::NewGuid().ToString('N') + '.stdout.txt')
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-etw-stackwalk-' + [Guid]::NewGuid().ToString('N') + '.stderr.txt')
    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $proc.WaitForExit()
        $stdout = if (Test-Path $stdoutPath) { (Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        $stderr = if (Test-Path $stderrPath) { (Get-Content -Path $stderrPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        if (-not $IgnoreExitCode -and $proc.ExitCode -ne 0) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $($proc.ExitCode): $stderr"
        }
        return [ordered]@{
            exit_code = $proc.ExitCode
            stdout = ('{0}' -f $stdout).Trim()
            stderr = ('{0}' -f $stderr).Trim()
        }
    }
    finally {
        Remove-Item -Path $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )
    $Payload | ConvertTo-Json -Depth 12 | Set-Content -Path $Path -Encoding UTF8
}

$safeRunId = ConvertTo-SafeName -Value $RunId
$runRoot = Join-Path $OutputRoot $safeRunId
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$rawEtlPath = Join-Path $runRoot ($safeRunId + '.raw.etl')
$etlPath = Join-Path $runRoot ($safeRunId + '.etl')
$xmlPath = Join-Path $runRoot ($safeRunId + '.xml')
$summaryPath = Join-Path $runRoot 'summary.json'

$summary = [ordered]@{
    schema_version = '1.0'
    generated_utc = [DateTime]::UtcNow.ToString('o')
    run_id = $safeRunId
    capture_profile = 'kernel-registry-stackwalk-v1'
    status = 'ok'
    error_kind = $null
    error = $null
    registry_path = $RegistryPath
    value_name = $ValueName
    duration_seconds = $DurationSeconds
    kernel_flags = @($KernelFlags)
    stackwalk_events = @($StackwalkEvents)
    stack_capture_expected = $true
    xperf_exists = [bool](Test-Path $xperf)
    tracerpt_exists = [bool](Test-Path $tracerpt)
    output_root = $runRoot
    raw_etl_path = $rawEtlPath
    etl_path = $etlPath
    xml_path = $xmlPath
    etl_exists = $false
    xml_exists = $false
    stack_field_hit_count = 0
    commands = [ordered]@{}
}

try {
    if (-not (Test-Path $xperf)) {
        throw 'xperf.exe not found. Install Windows Performance Toolkit.'
    }

    Remove-Item -Path $rawEtlPath, $etlPath, $xmlPath -Force -ErrorAction SilentlyContinue

    $stopExisting = Invoke-NativeProcess -FilePath $xperf -ArgumentList @('-stop') -IgnoreExitCode
    $summary.commands['stop_existing'] = $stopExisting

    $startArgs = @(
        '-on',
        (($KernelFlags | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join '+'),
        '-stackwalk',
        (($StackwalkEvents | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join '+'),
        '-BufferSize',
        [string]$BufferSizeKb,
        '-MinBuffers',
        [string]$MinBuffers,
        '-MaxBuffers',
        [string]$MaxBuffers,
        '-f',
        $rawEtlPath
    )
    $summary.commands['start_args'] = @($startArgs)
    $summary.commands['start'] = Invoke-NativeProcess -FilePath $xperf -ArgumentList $startArgs

    Start-Sleep -Seconds ([Math]::Max($DurationSeconds, 1))

    $stopArgs = @('-d', $etlPath)
    $summary.commands['stop_args'] = @($stopArgs)
    $summary.commands['stop'] = Invoke-NativeProcess -FilePath $xperf -ArgumentList $stopArgs
    $summary.etl_exists = [bool](Test-Path $etlPath)

    if (-not $SkipTracerpt) {
        if (-not (Test-Path $tracerpt)) {
            throw 'tracerpt.exe not found.'
        }
        $parseArgs = @($etlPath, '-o', $xmlPath, '-of', 'XML')
        $summary.commands['tracerpt_args'] = @($parseArgs)
        $summary.commands['tracerpt'] = Invoke-NativeProcess -FilePath $tracerpt -ArgumentList $parseArgs
        $summary.xml_exists = [bool](Test-Path $xmlPath)
        if ($summary.xml_exists) {
            $summary.stack_field_hit_count = @(
                Select-String -Path $xmlPath -Pattern 'Name="Stack"', 'Name="CallStack"', 'Name="StackTrace"', 'Name="UserStack"' -SimpleMatch -ErrorAction SilentlyContinue
            ).Count
        }
    }
}
catch {
    $summary.status = 'error'
    $summary.error_kind = 'etw-stackwalk-capture-failed'
    $summary.error = $_.Exception.Message
}

$summary.generated_utc = [DateTime]::UtcNow.ToString('o')
Write-JsonFile -Path $summaryPath -Payload $summary
Write-Output $summaryPath
