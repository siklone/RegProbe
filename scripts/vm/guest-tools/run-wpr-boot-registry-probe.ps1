[CmdletBinding()]
param(
    [ValidateSet('arm', 'collect')]
    [string]$Stage = 'arm',

    [string]$RegistryPath = '',
    [string]$ValueName = '',
    [string]$OutputName = 'wpr-boot-registry',
    [string]$OutputRoot = '',
    [string]$UploadBaseUrl = '',
    [string]$StateFile = '',
    [string[]]$MatchFragments = @(),
    [int]$WprTimeoutSeconds = 180,
    [int]$TracerptTimeoutSeconds = 180,
    [int]$UploadRetryCount = 20,
    [int]$UploadRetryDelaySeconds = 5
)

$ErrorActionPreference = 'Stop'

$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$tracerpt = 'C:\Windows\System32\tracerpt.exe'

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )

    $Payload | ConvertTo-Json -Depth 10 | Set-Content -Path $Path -Encoding UTF8
}

function Get-BootTimeUtc {
    try {
        $boot = (Get-CimInstance Win32_OperatingSystem -ErrorAction Stop).LastBootUpTime
        return $boot.ToUniversalTime().ToString('o')
    }
    catch {
        return $null
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
                throw
            }
            Start-Sleep -Seconds ([Math]::Max($UploadRetryDelaySeconds, 1))
        }
    }

    return $null
}

function Invoke-NativeProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [int]$TimeoutSeconds = 0,
        [switch]$IgnoreExitCode
    )

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-wpr-' + [Guid]::NewGuid().ToString('N') + '.stdout.txt')
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-wpr-' + [Guid]::NewGuid().ToString('N') + '.stderr.txt')

    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $timedOut = $false
        if ($TimeoutSeconds -gt 0) {
            if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
                $timedOut = $true
                try {
                    $proc.Kill()
                }
                catch {
                }
                $proc.WaitForExit()
            }
        }
        else {
            $proc.WaitForExit()
        }
        $stdout = if (Test-Path $stdoutPath) { (Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        $stderr = if (Test-Path $stderrPath) { (Get-Content -Path $stderrPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        if ($timedOut) {
            if (-not $IgnoreExitCode) {
                throw "$([System.IO.Path]::GetFileName($FilePath)) timed out after $TimeoutSeconds second(s)"
            }
        }
        elseif (-not $IgnoreExitCode -and $proc.ExitCode -ne 0) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $($proc.ExitCode)"
        }

        return [ordered]@{
            exit_code = if ($timedOut) { -1 } else { $proc.ExitCode }
            timed_out = $timedOut
            stdout = ('{0}' -f $stdout).Trim()
            stderr = ('{0}' -f $stderr).Trim()
        }
    }
    finally {
        Remove-Item -Path $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Test-NonZeroExitCode {
    param([object]$Result)

    return ($null -ne $Result -and -not $Result.timed_out -and $null -ne $Result.exit_code -and $Result.exit_code -ne 0)
}

function Get-RowField {
    param(
        [Parameter(Mandatory = $true)]$Row,
        [Parameter(Mandatory = $true)][string[]]$Names
    )

    foreach ($name in $Names) {
        foreach ($property in $Row.PSObject.Properties) {
            if ($property.Name -ieq $name -and $null -ne $property.Value -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
                return ([string]$property.Value).Trim()
            }
        }
    }

    return $null
}

function Get-RawRegistryLineValueName {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) {
        return $null
    }

    $match = [regex]::Match($Line, '"([^"]+)"\s*$')
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return $null
}

function Get-CallerStackFrames {
    param([Parameter(Mandatory = $true)]$Row)

    $stackFieldNames = @(
        'Stack',
        'CallStack',
        'Call Stack',
        'StackTrace',
        'Stack Trace',
        'UserStack',
        'User Stack'
    )
    foreach ($fieldName in $stackFieldNames) {
        $rawStack = Get-RowField -Row $Row -Names @($fieldName)
        if ([string]::IsNullOrWhiteSpace($rawStack)) {
            continue
        }

        $frames = @(
            ($rawStack -split '(?:\r\n|\n|\r|;|\|)') |
                ForEach-Object { ([string]$_).Trim() } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and $_ -notin @('Stack', 'Call Stack', 'Stack Trace') }
        )
        if ($frames.Count -gt 0) {
            return $frames
        }
    }

    return @()
}

function Test-RegistryFragmentMatch {
    param(
        [string]$Line,
        [string]$Fragment,
        [string]$RegistryPath,
        [string]$ValueName
    )

    if ([string]::IsNullOrWhiteSpace($Line) -or [string]::IsNullOrWhiteSpace($Fragment)) {
        return $false
    }

    if (-not [string]::IsNullOrWhiteSpace($ValueName) -and $Fragment.Equals($ValueName, [System.StringComparison]::OrdinalIgnoreCase)) {
        $rawValueName = Get-RawRegistryLineValueName -Line $Line
        return ($null -ne $rawValueName -and $rawValueName.Equals($ValueName, [System.StringComparison]::OrdinalIgnoreCase))
    }

    if (-not [string]::IsNullOrWhiteSpace($RegistryPath) -and $Fragment.Equals($RegistryPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($Line -like "*$RegistryPath*")
    }

    return ($Line -like "*$Fragment*")
}

function Split-RegistryPath {
    param(
        [string]$Path,
        [string]$FallbackPath,
        [string]$FallbackValueName
    )

    $normalized = if ([string]::IsNullOrWhiteSpace($Path)) { $FallbackPath } else { $Path }
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return [ordered]@{
            hive = $null
            key_path = $null
            value_name = $FallbackValueName
        }
    }

    $working = $normalized.Replace('/', '\').Replace('HKLM:\', 'HKLM\').Replace('HKCU:\', 'HKCU\')
    $hive = $null
    $remaining = $working
    foreach ($candidate in @('HKLM', 'HKCU', 'HKCR', 'HKU', 'HKCC')) {
        if ($working.StartsWith($candidate + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
            $hive = $candidate
            $remaining = $working.Substring($candidate.Length + 1)
            break
        }
    }

    return [ordered]@{
        hive = $hive
        key_path = $remaining
        value_name = $FallbackValueName
    }
}

function Write-NormalizedTracerptBundle {
    param(
        [Parameter(Mandatory = $true)][string]$BundlePath,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$CsvPath,
        [Parameter(Mandatory = $true)][string[]]$SearchFragments,
        [Parameter(Mandatory = $true)][string]$RegistryPath,
        [Parameter(Mandatory = $true)][string]$ValueName
    )

    $events = New-Object System.Collections.Generic.List[object]
    foreach ($row in Import-Csv -Path $CsvPath) {
        $eventName = Get-RowField -Row $row -Names @('Event Name', 'EventName')
        if ($eventName -and $eventName -ine 'Registry') {
            continue
        }

        $eventType = Get-RowField -Row $row -Names @('Type', 'Task Name', 'Opcode Name', 'Task')
        $userData = Get-RowField -Row $row -Names @('User Data', 'ValueName', 'Name')
        $rowPath = Get-RowField -Row $row -Names @('KeyName', 'Path', 'Key Path')
        $joined = ($row.PSObject.Properties | ForEach-Object { [string]$_.Value }) -join ' | '
        $matched = $false
        foreach ($fragment in $SearchFragments) {
            if (Test-RegistryFragmentMatch -Line $joined -Fragment $fragment -RegistryPath $RegistryPath -ValueName $ValueName) {
                $matched = $true
                break
            }
        }

        if (-not $matched) {
            continue
        }

        $pathParts = Split-RegistryPath -Path $rowPath -FallbackPath $RegistryPath -FallbackValueName $ValueName
        $rowValueName = $pathParts.value_name
        if (
            $eventType -and
            $eventType -imatch '^(QueryValue|SetValue|DeleteValue)$' -and
            -not [string]::IsNullOrWhiteSpace($userData) -and
            $userData -notmatch '[\\/:]' -and
            $userData -notmatch '^0x'
        ) {
            $rowValueName = $userData.Trim('"')
        }

        if (
            -not [string]::IsNullOrWhiteSpace($ValueName) -and
            -not [string]::IsNullOrWhiteSpace($rowValueName) -and
            -not $rowValueName.Equals($ValueName, [System.StringComparison]::OrdinalIgnoreCase) -and
            -not (Test-RegistryFragmentMatch -Line $joined -Fragment $RegistryPath -RegistryPath $RegistryPath -ValueName $ValueName)
        ) {
            continue
        }

        $pidRaw = Get-RowField -Row $row -Names @('PID', 'Process ID')
        $parsedPid = 0
        $pidValue = $null
        if (-not [string]::IsNullOrWhiteSpace($pidRaw) -and [int]::TryParse($pidRaw, [ref]$parsedPid)) {
            $pidValue = $parsedPid
        }

        $callerStack = @(Get-CallerStackFrames -Row $row)
        $event = [ordered]@{
            run_id = $RunId
            source_tool = 'wpr'
            capture_phase = 'boot'
            process_name = Get-RowField -Row $row -Names @('Process Name', 'ProcessName', 'Process')
            pid = $pidValue
            operation = if ($eventName -ieq 'Registry' -and -not [string]::IsNullOrWhiteSpace($eventType)) { $eventType } else { (Get-RowField -Row $row -Names @('Event Name', 'Task Name', 'Opcode Name', 'EventName', 'Task')) }
            timestamp_utc = (Get-RowField -Row $row -Names @('Event Time', 'TimeCreated', 'Time Stamp', 'Timestamp', 'Time'))
            hive = $pathParts.hive
            key_path = $pathParts.key_path
            value_name = $rowValueName
            value_type = Get-RowField -Row $row -Names @('Type', 'Value Type', 'Data Type')
            data_text = $joined
            result = Get-RowField -Row $row -Names @('Result', 'Status')
            evidence_refs = @($CsvPath)
        }
        if ($callerStack.Count -gt 0) {
            $event.caller_stack = @($callerStack)
        }
        $events.Add($event) | Out-Null
    }

    if ($events.Count -eq 0 -and (Test-Path $CsvPath)) {
        $fallbackPathParts = Split-RegistryPath -Path $RegistryPath -FallbackPath $RegistryPath -FallbackValueName $ValueName
        foreach ($line in (Get-Content -Path $CsvPath -ErrorAction SilentlyContinue | Select-Object -Skip 1)) {
            $cleanLine = ([string]$line).TrimStart([char]0xFEFF).Trim()
            if ($cleanLine -notmatch '^Registry\s*,') {
                continue
            }

            $columns = $cleanLine -split ','
            if ($columns.Count -lt 2) {
                continue
            }

            $operation = $columns[1].Trim()
            if ($operation -notmatch '^(QueryValue|SetValue|DeleteValue)$') {
                continue
            }

            $rawValueName = Get-RawRegistryLineValueName -Line $cleanLine
            $matchesTarget = $false
            foreach ($fragment in $SearchFragments) {
                if (Test-RegistryFragmentMatch -Line $cleanLine -Fragment $fragment -RegistryPath $RegistryPath -ValueName $ValueName) {
                    $matchesTarget = $true
                    break
                }
            }

            if (-not $matchesTarget) {
                continue
            }

            $events.Add([ordered]@{
                run_id = $RunId
                source_tool = 'wpr'
                capture_phase = 'boot'
                process_name = $null
                pid = $null
                operation = $operation
                timestamp_utc = $null
                hive = $fallbackPathParts.hive
                key_path = $fallbackPathParts.key_path
                value_name = if (-not [string]::IsNullOrWhiteSpace($rawValueName)) { $rawValueName } else { $ValueName }
                value_type = $null
                data_text = $cleanLine
                result = $null
                evidence_refs = @($CsvPath)
                normalization_note = 'raw-tracerpt-registry-hit-line'
            }) | Out-Null
        }
    }

    $eventItems = @($events | ForEach-Object { $_ })
    $eventCount = $events.Count
    $stackedEventCount = @($eventItems | Where-Object { @($_.caller_stack).Count -gt 0 }).Count
    $bundleEvidenceRefs = @($CsvPath, $BundlePath)

    $bundle = [ordered]@{
        '$schema' = 'registry-research-framework/schemas/normalized-registry-bundle.schema.json'
        run_id = $RunId
        source_tool = 'wpr'
        capture_phase = 'boot'
        generated_utc = [DateTime]::UtcNow.ToString('o')
        normalizer_name = 'GuestTracerptCsvRegistryNormalizer'
        input_path = $CsvPath
        status = 'ok'
        error_kind = $null
        errors = @()
        event_count = $eventCount
        filtered_event_count = $eventCount
        evidence_refs = $bundleEvidenceRefs
        stack_capture = [ordered]@{
            parser_supported = $true
            captured_event_count = $stackedEventCount
            source_fields = @('Stack', 'CallStack', 'Call Stack', 'StackTrace', 'Stack Trace', 'UserStack', 'User Stack')
        }
        events = $eventItems
    }

    $bundle | ConvertTo-Json -Depth 12 | Set-Content -Path $BundlePath -Encoding UTF8
    return $bundle
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\wpr-boot-registry' $OutputName
}

Ensure-Directory -Path $OutputRoot

if ([string]::IsNullOrWhiteSpace($StateFile)) {
    $StateFile = Join-Path $OutputRoot 'state.json'
}

$summaryArmPath = Join-Path $OutputRoot 'summary-arm.json'
$summaryPath = Join-Path $OutputRoot 'summary.json'
$stagePath = Join-Path $OutputRoot 'stage.json'
$etlPath = Join-Path $OutputRoot ($OutputName + '.etl')
$csvPath = Join-Path $OutputRoot ($OutputName + '.csv')
$hitsCsvPath = Join-Path $OutputRoot ($OutputName + '.hits.csv')
$hitsPath = Join-Path $OutputRoot ($OutputName + '.hits.txt')
$normalizedBundlePath = Join-Path $OutputRoot ($OutputName + '.normalized.json')
$tracerptInputPath = $etlPath

function Publish-Stage {
    param(
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$Message = '',
        [hashtable]$Extra = @{}
    )

    $payload = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        output_name = $OutputName
        stage = $StageName
        status = $Status
        message = $Message
    }
    foreach ($entry in $Extra.GetEnumerator()) {
        $payload[$entry.Key] = $entry.Value
    }

    Write-JsonFile -Path $stagePath -Payload $payload
    try {
        Invoke-ArtifactUpload -Path $stagePath -RemoteName ('{0}-stage.json' -f $OutputName) | Out-Null
    }
    catch {
    }
}

if ($Stage -eq 'arm') {
    foreach ($path in @($summaryArmPath, $summaryPath, $stagePath, $etlPath, $csvPath, $hitsCsvPath, $hitsPath, $normalizedBundlePath)) {
        Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        stage = 'arm'
        registry_path = $RegistryPath
        value_name = $ValueName
        output_name = $OutputName
        status = 'ok'
        error_kind = $null
        error = $null
        recovery_action = 'none'
        transport_blocker = 'none'
        guest_health = 'stable'
        wpr_exists = [bool](Test-Path $wpr)
        tracerpt_exists = [bool](Test-Path $tracerpt)
        before_boot_time_utc = $null
        etl_path = $etlPath
        state_file = $StateFile
    }

    try {
        Publish-Stage -StageName 'arm-start' -Status 'starting'
        $cancelBoot = if (Test-Path $wpr) {
            Publish-Stage -StageName 'arm-cancelboot' -Status 'starting'
            Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-cancelboot') -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        } else {
            $null
        }
        $summary.cancelboot = $cancelBoot

        $cancelLive = if (Test-Path $wpr) {
            Publish-Stage -StageName 'arm-cancel' -Status 'starting'
            Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-cancel') -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        } else {
            $null
        }
        $summary.cancel = $cancelLive

        $arm = if (Test-Path $wpr) {
            Publish-Stage -StageName 'arm-addboot' -Status 'starting'
            Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-addboot', 'Power', '-addboot', 'Registry', '-filemode', '-recordtempto', $OutputRoot) -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        } else {
            $null
        }
        $summary.arm = $arm

        $state = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            registry_path = $RegistryPath
            value_name = $ValueName
            output_name = $OutputName
            output_root = $OutputRoot
            upload_base_url = $UploadBaseUrl
            etl_path = $etlPath
            csv_path = $csvPath
            hits_path = $hitsPath
            before_boot_time_utc = Get-BootTimeUtc
            match_fragments = @($MatchFragments)
        }
        $summary.before_boot_time_utc = $state.before_boot_time_utc
        Write-JsonFile -Path $StateFile -Payload $state

        if ($arm) {
            if ($arm.timed_out) {
                $summary.status = 'error'
                $summary.error_kind = 'wpr-addboot-timeout'
                $summary.error = "wpr -addboot timed out after $WprTimeoutSeconds second(s)."
            }
            elseif (Test-NonZeroExitCode -Result $arm) {
                $summary.status = 'error'
                $summary.error_kind = 'wpr-addboot-nonzero-exit'
                $armExitCode = if ($null -eq $arm.exit_code) { '<null>' } else { [string]$arm.exit_code }
                $summary.error = "wpr -addboot exited with code $armExitCode."
            }
            elseif ($null -eq $arm.exit_code) {
                $summary.arm_exit_code_indeterminate = $true
            }
        }
    }
    catch {
        $summary.status = 'error'
        $summary.error_kind = 'arm-exception'
        $summary.error = $_.Exception.Message
    }

    Write-JsonFile -Path $summaryArmPath -Payload $summary
    Invoke-ArtifactUpload -Path $summaryArmPath -RemoteName ('{0}-summary-arm.json' -f $OutputName) | Out-Null
    Publish-Stage -StageName 'arm-complete' -Status $summary.status -Message $summary.error
    Write-Output $summaryArmPath
    return
}

$state = Get-Content -Path $StateFile -Raw | ConvertFrom-Json
$RegistryPath = [string]$state.registry_path
$ValueName = [string]$state.value_name
$OutputName = [string]$state.output_name
$OutputRoot = [string]$state.output_root
$UploadBaseUrl = [string]$state.upload_base_url
$summaryArmPath = Join-Path $OutputRoot 'summary-arm.json'
$summaryPath = Join-Path $OutputRoot 'summary.json'
$stagePath = Join-Path $OutputRoot 'stage.json'
$etlPath = Join-Path $OutputRoot ($OutputName + '.etl')
$csvPath = Join-Path $OutputRoot ($OutputName + '.csv')
$hitsCsvPath = Join-Path $OutputRoot ($OutputName + '.hits.csv')
$hitsPath = Join-Path $OutputRoot ($OutputName + '.hits.txt')
$normalizedBundlePath = Join-Path $OutputRoot ($OutputName + '.normalized.json')
$tracerptInputPath = $etlPath

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    stage = 'collect'
    registry_path = [string]$state.registry_path
    value_name = [string]$state.value_name
    output_name = [string]$state.output_name
    status = 'ok'
    error_kind = $null
    error = $null
    recovery_action = 'none'
    transport_blocker = 'none'
    guest_health = 'stable'
    before_boot_time_utc = [string]$state.before_boot_time_utc
    after_boot_time_utc = Get-BootTimeUtc
    reboot_observed = $false
    wpr_exists = [bool](Test-Path $wpr)
    tracerpt_exists = [bool](Test-Path $tracerpt)
    stopboot = $null
    etl_path = $etlPath
    etl_exists = $false
    tracerpt_input_path = $etlPath
    csv_path = $csvPath
    csv_exists = $false
    raw_collector_salvage = $false
    raw_collector_path = $null
    raw_collector_size_bytes = 0
    normalized_bundle_path = $normalizedBundlePath
    normalized_bundle_exists = $false
    normalized_result_ref = $null
    normalization_status = 'missing'
    normalizer_name = 'GuestTracerptCsvRegistryNormalizer'
    normalization_errors = @()
    hits_csv_path = $hitsCsvPath
    hits_csv_exists = $false
    hit_line_count = 0
    fragment_hit_counts = [ordered]@{}
}

try {
    Publish-Stage -StageName 'collect-start' -Status 'starting'
    if ($summary.before_boot_time_utc -and $summary.after_boot_time_utc) {
        $summary.reboot_observed = ([datetimeoffset]::Parse($summary.after_boot_time_utc) -gt [datetimeoffset]::Parse($summary.before_boot_time_utc))
    }

    if (Test-Path $wpr) {
        Publish-Stage -StageName 'collect-stopboot' -Status 'starting'
        $stopResult = Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-stopboot', $etlPath) -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        $summary.stopboot = $stopResult
        if ($stopResult.timed_out) {
            $summary.status = 'error'
            $summary.error_kind = 'wpr-stopboot-timeout'
            $summary.error = "wpr -stopboot timed out after $WprTimeoutSeconds second(s)."
        }
        elseif (Test-NonZeroExitCode -Result $stopResult) {
            $summary.status = 'error'
            $summary.error_kind = 'wpr-stopboot-nonzero-exit'
            $summary.error = "wpr -stopboot exited with code $($stopResult.exit_code)."
        }
        elseif ($null -eq $stopResult.exit_code) {
            $summary.stopboot_exit_code_indeterminate = $true
        }
    }

    $summary.etl_exists = [bool](Test-Path $etlPath)

    if ($summary.status -eq 'ok' -and -not $summary.etl_exists) {
        $rawCollector = Get-ChildItem -Path $OutputRoot -Filter '*.etl' -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -ine $etlPath } |
            Sort-Object -Property Length -Descending |
            Select-Object -First 1

        if ($null -ne $rawCollector) {
            $tracerptInputPath = $rawCollector.FullName
            $csvPath = Join-Path $OutputRoot ($OutputName + '.raw-collector.csv')
            $hitsCsvPath = Join-Path $OutputRoot ($OutputName + '.raw-collector.hits.csv')
            $hitsPath = Join-Path $OutputRoot ($OutputName + '.raw-collector.hits.txt')
            $summary.tracerpt_input_path = $tracerptInputPath
            $summary.csv_path = $csvPath
            $summary.hits_csv_path = $hitsCsvPath
            $summary.raw_collector_salvage = $true
            $summary.raw_collector_path = $rawCollector.FullName
            $summary.raw_collector_size_bytes = [int64]$rawCollector.Length
        }
    }

    if ($summary.status -eq 'ok' -and (Test-Path $tracerptInputPath) -and (Test-Path $tracerpt)) {
        Publish-Stage -StageName 'collect-tracerpt' -Status 'starting'
        Remove-Item -Path $csvPath, $hitsCsvPath, $hitsPath -Force -ErrorAction SilentlyContinue
        $convertResult = Invoke-NativeProcess -FilePath $tracerpt -ArgumentList @($tracerptInputPath, '-o', $csvPath, '-of', 'CSV') -TimeoutSeconds $TracerptTimeoutSeconds -IgnoreExitCode
        $summary['tracerpt'] = $convertResult
        $summary.csv_exists = [bool](Test-Path $csvPath)
        if ($convertResult.timed_out) {
            $summary.status = 'error'
            $summary.error_kind = 'tracerpt-timeout'
            $summary.error = "tracerpt timed out after $TracerptTimeoutSeconds second(s)."
        }
        elseif (Test-NonZeroExitCode -Result $convertResult) {
            $summary.status = 'error'
            $summary.error_kind = 'tracerpt-nonzero-exit'
            $summary.error = "tracerpt exited with code $($convertResult.exit_code)."
        }
        elseif ($null -eq $convertResult.exit_code) {
            $summary.tracerpt_exit_code_indeterminate = $true
        }
        elseif (-not $summary.csv_exists) {
            $summary.status = 'error'
            $summary.error_kind = 'tracerpt-missing-csv'
            $summary.error = "tracerpt did not create $csvPath"
        }

        if ($summary.csv_exists) {
            $fragments = New-Object System.Collections.Generic.List[string]
            foreach ($fragment in @([string]$state.registry_path, [string]$state.value_name) + @($state.match_fragments)) {
                if (-not [string]::IsNullOrWhiteSpace($fragment) -and -not $fragments.Contains($fragment)) {
                    $fragments.Add($fragment)
                }
            }

            $headerLine = Get-Content -Path $csvPath -TotalCount 1 -ErrorAction SilentlyContinue
            $hitLineSet = New-Object 'System.Collections.Generic.HashSet[string]'
            foreach ($fragment in $fragments) {
                $summary.fragment_hit_counts[$fragment] = 0
            }

            foreach ($match in Select-String -Path $csvPath -Pattern @($fragments) -SimpleMatch -ErrorAction SilentlyContinue) {
                if ($match.Line -match '^\s*Registry\s*,' ) {
                    $lineMatched = $false
                    foreach ($fragment in $fragments) {
                        if (Test-RegistryFragmentMatch -Line $match.Line -Fragment $fragment -RegistryPath ([string]$state.registry_path) -ValueName ([string]$state.value_name)) {
                            $lineMatched = $true
                            break
                        }
                    }

                    if (-not $lineMatched) {
                        continue
                    }

                    [void]$hitLineSet.Add($match.Line)
                }
            }

            $hitLines = New-Object System.Collections.Generic.List[string]
            foreach ($line in $hitLineSet) {
                $hitLines.Add($line)
                foreach ($fragment in $fragments) {
                    if (Test-RegistryFragmentMatch -Line $line -Fragment $fragment -RegistryPath ([string]$state.registry_path) -ValueName ([string]$state.value_name)) {
                        $summary.fragment_hit_counts[$fragment]++
                    }
                }
            }

            if (-not [string]::IsNullOrWhiteSpace($headerLine)) {
                @($headerLine) + @($hitLines) | Set-Content -Path $hitsCsvPath -Encoding UTF8
                $summary.hits_csv_exists = [bool](Test-Path $hitsCsvPath)
            }

            if ($hitLines.Count -gt 0) {
                $hitLines | Set-Content -Path $hitsPath -Encoding UTF8
            }
            $summary.hit_line_count = $hitLines.Count
            $summary['hits_path'] = $hitsPath
            $summary['hits_exists'] = [bool](Test-Path $hitsPath)

            try {
                $normalizerInputPath = if ($summary.hits_csv_exists) { $hitsCsvPath } else { $csvPath }
                $bundle = Write-NormalizedTracerptBundle -BundlePath $normalizedBundlePath -RunId $OutputName -CsvPath $normalizerInputPath -SearchFragments @($fragments) -RegistryPath ([string]$state.registry_path) -ValueName ([string]$state.value_name)
                $summary.normalized_bundle_exists = [bool](Test-Path $normalizedBundlePath)
                $summary.normalized_result_ref = if ($summary.normalized_bundle_exists) { $normalizedBundlePath } else { $null }
                $summary.normalization_status = [string]$bundle.status
            }
            catch {
                $summary.normalization_status = 'error'
                $summary.normalization_errors = @($_.Exception.Message)
            }
        }
    }
}
catch {
    $summary.status = 'error'
    $summary.error_kind = 'collect-exception'
    $summary.error = $_.Exception.Message
    $summary.recovery_action = 'rerun-wpr-collect'
    $summary.transport_blocker = 'collect-exception'
    $summary.guest_health = 'degraded'
}

if ($summary.status -eq 'ok' -and $summary.normalization_status -ne 'ok') {
    $summary.status = 'error'
    $summary.error_kind = if ($summary.normalization_status -eq 'missing') { 'normalized-bundle-missing' } else { 'normalization-error' }
    $summary.error = if (@($summary.normalization_errors).Count -gt 0) { (@($summary.normalization_errors) -join '; ') } else { 'Normalized bundle was not produced.' }
    $summary.recovery_action = 'inspect-normalized-bundle'
    $summary.transport_blocker = if ($summary.normalization_status -eq 'missing') { 'normalized-bundle-missing' } else { 'normalization-failed' }
    $summary.guest_health = 'stable'
}

Write-JsonFile -Path $summaryPath -Payload $summary
Invoke-ArtifactUpload -Path $summaryPath -RemoteName ('{0}-summary.json' -f $OutputName) | Out-Null
if (Test-Path $hitsPath) {
    Invoke-ArtifactUpload -Path $hitsPath -RemoteName ('{0}.hits.txt' -f $OutputName) | Out-Null
}
if (Test-Path $normalizedBundlePath) {
    Invoke-ArtifactUpload -Path $normalizedBundlePath -RemoteName ('{0}.normalized.json' -f $OutputName) | Out-Null
}
Publish-Stage -StageName 'collect-complete' -Status $summary.status -Message $summary.error

Write-Output $summaryPath
