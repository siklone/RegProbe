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
    [string]$UploadBaseUrl = '',
    [int]$UploadRetryCount = 10,
    [int]$UploadRetryDelaySeconds = 3,
    [switch]$UploadEtl,
    [switch]$SkipTracerpt
)

$ErrorActionPreference = 'Stop'

$xperf = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\xperf.exe'
$tracerpt = 'C:\Windows\System32\tracerpt.exe'
$regExe = 'C:\Windows\System32\reg.exe'

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

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-etw-stackwalk-' + [Guid]::NewGuid().ToString('N') + '.stdout.txt')
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-etw-stackwalk-' + [Guid]::NewGuid().ToString('N') + '.stderr.txt')
    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList (ConvertTo-QuotedArgumentString -Arguments $ArgumentList) -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $proc.WaitForExit()
        try {
            $proc.Refresh()
        }
        catch {
        }
        $stdout = if (Test-Path $stdoutPath) { (Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        $stderr = if (Test-Path $stderrPath) { (Get-Content -Path $stderrPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        $exitCode = if ($null -eq $proc.ExitCode) { 0 } else { [int]$proc.ExitCode }
        if (-not $IgnoreExitCode -and $exitCode -ne 0) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code ${exitCode}: $stderr"
        }
        return [ordered]@{
            exit_code = $exitCode
            stdout = ('{0}' -f $stdout).Trim()
            stderr = ('{0}' -f $stderr).Trim()
        }
    }
    finally {
        Remove-Item -Path $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Format-CommandFailure {
    param(
        [Parameter(Mandatory = $true)][string]$ToolName,
        [Parameter(Mandatory = $true)][hashtable]$Result,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    $message = if (-not [string]::IsNullOrWhiteSpace([string]$Result.stderr)) {
        [string]$Result.stderr
    }
    elseif (-not [string]::IsNullOrWhiteSpace([string]$Result.stdout)) {
        [string]$Result.stdout
    }
    else {
        'no diagnostic output captured'
    }

    return "$ToolName failed with exit code $($Result.exit_code): $message | argv=$($ArgumentList -join ' ')"
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )
    $Payload | ConvertTo-Json -Depth 12 | Set-Content -Path $Path -Encoding UTF8
}

function Invoke-RegistryProbe {
    param(
        [Parameter(Mandatory = $true)][string]$RegistryPath,
        [string]$ValueName
    )

    $probeArgs = @('query', $RegistryPath)
    if (-not [string]::IsNullOrWhiteSpace($ValueName)) {
        $probeArgs += @('/v', $ValueName)
    }

    return [ordered]@{
        file_path = $regExe
        argument_list = @($probeArgs)
        result = Invoke-NativeProcess -FilePath $regExe -ArgumentList $probeArgs -IgnoreExitCode
    }
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
    for ($attempt = 1; $attempt -le [Math]::Max($UploadRetryCount, 1); $attempt++) {
        try {
            Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
            return [ordered]@{
                path = $Path
                uri = $targetUri
                attempts = $attempt
            }
        }
        catch {
            if ($attempt -ge [Math]::Max($UploadRetryCount, 1)) {
                return [ordered]@{
                    path = $Path
                    uri = $targetUri
                    attempts = $attempt
                    error = $_.Exception.Message
                }
            }
            Start-Sleep -Seconds ([Math]::Max($UploadRetryDelaySeconds, 1))
        }
    }

    return $null
}

$safeRunId = ConvertTo-SafeName -Value $RunId
$runRoot = Join-Path $OutputRoot $safeRunId
if (Test-Path -LiteralPath $runRoot) {
    Remove-Item -LiteralPath $runRoot -Recurse -Force -ErrorAction Stop
}
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
    registry_probe_attempted = $false
    xperf_exists = [bool](Test-Path $xperf)
    tracerpt_exists = [bool](Test-Path $tracerpt)
    output_root = $runRoot
    raw_etl_path = $rawEtlPath
    etl_path = $etlPath
    xml_path = $xmlPath
    etl_exists = $false
    xml_exists = $false
    stack_field_hit_count = 0
    upload_base_url = $UploadBaseUrl
    artifact_uploads = [ordered]@{}
    commands = [ordered]@{}
}

try {
    if (-not (Test-Path $xperf)) {
        throw 'xperf.exe not found. Install Windows Performance Toolkit.'
    }

    foreach ($pathToRemove in @($rawEtlPath, $etlPath, $xmlPath, $summaryPath)) {
        if (Test-Path -LiteralPath $pathToRemove) {
            Remove-Item -LiteralPath $pathToRemove -Force -ErrorAction Stop
        }
    }

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
    $summary.commands['start'] = Invoke-NativeProcess -FilePath $xperf -ArgumentList $startArgs -IgnoreExitCode
    if (($summary.commands['start'].exit_code) -ne 0) {
        throw (Format-CommandFailure -ToolName 'xperf.exe' -Result $summary.commands['start'] -ArgumentList $startArgs)
    }

    if (-not [string]::IsNullOrWhiteSpace($RegistryPath) -and (Test-Path $regExe)) {
        $summary.registry_probe_attempted = $true
        $summary.commands['registry_probe'] = Invoke-RegistryProbe -RegistryPath $RegistryPath -ValueName $ValueName
    }

    Start-Sleep -Seconds ([Math]::Max($DurationSeconds, 1))

    $stopArgs = @('-d', $etlPath)
    $summary.commands['stop_args'] = @($stopArgs)
    $summary.commands['stop'] = Invoke-NativeProcess -FilePath $xperf -ArgumentList $stopArgs -IgnoreExitCode
    if (($summary.commands['stop'].exit_code) -ne 0) {
        throw (Format-CommandFailure -ToolName 'xperf.exe' -Result $summary.commands['stop'] -ArgumentList $stopArgs)
    }
    $summary.etl_exists = [bool](Test-Path $etlPath)

    if (-not $SkipTracerpt) {
        if (-not (Test-Path $tracerpt)) {
            throw 'tracerpt.exe not found.'
        }
        if (Test-Path -LiteralPath $xmlPath) {
            Remove-Item -LiteralPath $xmlPath -Force -ErrorAction Stop
        }
        $parseArgs = @($etlPath, '-o', $xmlPath, '-of', 'XML', '-lr')
        $summary.commands['tracerpt_args'] = @($parseArgs)
        $summary.commands['tracerpt'] = Invoke-NativeProcess -FilePath $tracerpt -ArgumentList $parseArgs -IgnoreExitCode
        if (($summary.commands['tracerpt'].exit_code) -ne 0) {
            throw (Format-CommandFailure -ToolName 'tracerpt.exe' -Result $summary.commands['tracerpt'] -ArgumentList $parseArgs)
        }
        $summary.xml_exists = [bool](Test-Path $xmlPath)
        if ($summary.xml_exists) {
            $summary.stack_field_hit_count = @(
                Select-String -Path $xmlPath -Pattern 'Name="Stack\d+"', 'Name="CallStack"', 'Name="StackTrace"', 'Name="UserStack"', '>StackWalk<' -ErrorAction SilentlyContinue
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
$summary.artifact_uploads['xml'] = if ($summary.xml_exists) { Invoke-ArtifactUpload -Path $xmlPath -RemoteName ($safeRunId + '.xml') } else { $null }
$summary.artifact_uploads['etl'] = if ($UploadEtl -and $summary.etl_exists) { Invoke-ArtifactUpload -Path $etlPath -RemoteName ($safeRunId + '.etl') } else { $null }
Write-JsonFile -Path $summaryPath -Payload $summary
$summary.artifact_uploads['summary'] = Invoke-ArtifactUpload -Path $summaryPath -RemoteName ($safeRunId + '-summary.json')
Write-JsonFile -Path $summaryPath -Payload $summary
Write-Output $summaryPath
