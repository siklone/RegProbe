[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('capture')]
    [string]$Mode = 'capture',

    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [string]$Prefix = '',
    [string]$OutputDirectory = 'C:\Tools\Perf\Procmon',
    [string]$PowerShellCommand = '',
    [string]$StageUploadUri = '',
    [int]$SaveAsTimeoutSeconds = 60,
    [string[]]$MatchFragments = @(),
    [string[]]$ProcessNames = @()
)

$ErrorActionPreference = 'Stop'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'

function Get-ValueState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    try {
        $item = Get-ItemProperty -Path $Path -Name $Name -ErrorAction Stop
        return [ordered]@{
            path_exists = $true
            value_exists = $true
            value = $item.$Name
            value_type = $item.PSObject.Properties[$Name].TypeNameOfValue
        }
    }
    catch {
        return [ordered]@{
            path_exists = [bool](Test-Path $Path)
            value_exists = $false
            value = $null
            value_type = $null
        }
    }
}

function Restore-ValueState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    if ($State.value_exists) {
        if (-not (Test-Path $Path)) {
            New-Item -Path $Path -Force | Out-Null
        }

        $propertyType = switch -Regex ($State.value_type) {
            'Int64|UInt64' { 'QWord'; break }
            'Int32|UInt32|Int16|UInt16' { 'DWord'; break }
            'Byte\[\]' { 'Binary'; break }
            default { 'String' }
        }

        New-ItemProperty -Path $Path -Name $Name -PropertyType $propertyType -Value $State.value -Force | Out-Null
    }
    elseif (Test-Path $Path) {
        Remove-ItemProperty -Path $Path -Name $Name -ErrorAction SilentlyContinue
    }
}

function Normalize-RegistryPathForProcmon {
    param([string]$Path)

    return $Path.Replace('HKLM:\', 'HKLM\').Replace('HKCU:\', 'HKCU\').Replace('HKCR:\', 'HKCR\').Replace('HKU:\', 'HKU\')
}

function Split-RegistryPath {
    param(
        [string]$Path,
        [bool]$TreatLastSegmentAsValue = $false
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return [ordered]@{
            hive = $null
            key_path = $null
            value_name = $null
        }
    }

    $normalized = Normalize-RegistryPathForProcmon -Path $Path
    $hive = $null
    $remaining = $normalized
    foreach ($candidate in @('HKLM', 'HKCU', 'HKCR', 'HKU', 'HKCC')) {
        if ($normalized.StartsWith($candidate + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
            $hive = $candidate
            $remaining = $normalized.Substring($candidate.Length + 1)
            break
        }
        if ($normalized.Equals($candidate, [System.StringComparison]::OrdinalIgnoreCase)) {
            $hive = $candidate
            $remaining = ''
            break
        }
    }

    if (-not $TreatLastSegmentAsValue -or [string]::IsNullOrWhiteSpace($remaining)) {
        return [ordered]@{
            hive = $hive
            key_path = $remaining
            value_name = $null
        }
    }

    $separator = $remaining.LastIndexOf('\')
    if ($separator -le 0 -or $separator -ge ($remaining.Length - 1)) {
        return [ordered]@{
            hive = $hive
            key_path = $remaining
            value_name = $null
        }
    }

    return [ordered]@{
        hive = $hive
        key_path = $remaining.Substring(0, $separator)
        value_name = $remaining.Substring($separator + 1)
    }
}

function Parse-ProcmonDetail {
    param([string]$Detail)

    $valueType = $null
    $dataText = $null
    if (-not [string]::IsNullOrWhiteSpace($Detail)) {
        foreach ($segment in $Detail -split ',') {
            $trimmed = $segment.Trim()
            if ($trimmed -like 'Type:*') {
                $valueType = $trimmed.Substring('Type:'.Length).Trim()
            }
            elseif ($trimmed -like 'Data:*') {
                $dataText = $trimmed.Substring('Data:'.Length).Trim()
            }
        }
    }

    return [ordered]@{
        value_type = $valueType
        data_text = if ($dataText) { $dataText } else { $Detail }
    }
}

function Write-NormalizedProcmonBundle {
    param(
        [Parameter(Mandatory = $true)][string]$BundlePath,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][object[]]$Rows,
        [Parameter(Mandatory = $true)][string]$InputPath,
        [Parameter(Mandatory = $true)][string]$RegistryPath,
        [Parameter(Mandatory = $true)][string]$ValueName
    )

    $events = New-Object System.Collections.Generic.List[object]
    foreach ($row in @($Rows)) {
        $operation = [string]$row.Operation
        $rawPath = [string]$row.Path
        $pathParts = Split-RegistryPath -Path $rawPath -TreatLastSegmentAsValue:($operation -like '*Value*')
        $detailParts = Parse-ProcmonDetail -Detail ([string]$row.Detail)
        $parsedPid = 0
        $pidValue = $null
        if ([int]::TryParse([string]$row.PID, [ref]$parsedPid)) {
            $pidValue = $parsedPid
        }
        $events.Add([ordered]@{
            run_id = $RunId
            source_tool = 'procmon'
            capture_phase = 'runtime'
            process_name = [string]$row.'Process Name'
            pid = $pidValue
            operation = $operation
            timestamp_utc = [string]$row.'Time of Day'
            hive = $pathParts.hive
            key_path = $pathParts.key_path
            value_name = if ($pathParts.value_name) { $pathParts.value_name } else { $ValueName }
            value_type = $detailParts.value_type
            data_text = $detailParts.data_text
            result = [string]$row.Result
            evidence_refs = @($InputPath)
        }) | Out-Null
    }

    $bundle = [ordered]@{
        '$schema' = 'registry-research-framework/schemas/normalized-registry-bundle.schema.json'
        run_id = $RunId
        source_tool = 'procmon'
        capture_phase = 'runtime'
        generated_utc = [DateTime]::UtcNow.ToString('o')
        normalizer_name = 'GuestProcmonCsvRegistryNormalizer'
        input_path = $InputPath
        status = 'ok'
        error_kind = $null
        errors = @()
        event_count = @($Rows).Count
        filtered_event_count = @($events).Count
        evidence_refs = @($InputPath, $BundlePath)
        events = @($events)
    }

    $bundle | ConvertTo-Json -Depth 12 | Set-Content -Path $BundlePath -Encoding UTF8
    return $bundle
}

function New-ProbePrefix {
    param(
        [string]$ConfiguredPrefix,
        [string]$Path,
        [string]$Name
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPrefix)) {
        return $ConfiguredPrefix
    }

    $pathPart = ($Path -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
    $namePart = ($Name -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
    return "$pathPart-$namePart"
}

function Publish-ProbeStage {
    param(
        [Parameter(Mandatory = $true)][string]$Stage,
        [string]$Status = 'running',
        [string]$Message = ''
    )

    $payload = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        prefix = $effectivePrefix
        registry_path = $RegistryPath
        value_name = $ValueName
        stage = $Stage
        status = $Status
        message = $Message
    }

    $payload | ConvertTo-Json -Depth 6 | Set-Content -Path $stagePath -Encoding UTF8
    if (-not [string]::IsNullOrWhiteSpace($StageUploadUri)) {
        try {
            Invoke-WebRequest -Method Put -Uri $StageUploadUri -InFile $stagePath -UseBasicParsing | Out-Null
        }
        catch {
        }
    }
}

function Find-StaleProcmonBootLogs {
    $patterns = @(
        'C:\bootlog*.pml',
        'C:\Windows\bootlog*.pml',
        'C:\Tools\Sysinternals\bootlog*.pml',
        'C:\Tools\Perf\Procmon\bootlog*.pml',
        'C:\Users\*\bootlog*.pml',
        'C:\Users\*\AppData\Local\Temp\bootlog*.pml'
    )

    $results = @()
    foreach ($pattern in $patterns) {
        try {
            $items = Get-ChildItem -Path $pattern -Force -ErrorAction SilentlyContinue
            foreach ($item in $items) {
                if ($item -and $item.FullName) {
                    $results += [pscustomobject]@{
                        full_name = $item.FullName
                        length = $item.Length
                        last_write_time_utc = $item.LastWriteTimeUtc.ToString('o')
                    }
                }
            }
        }
        catch {
        }
    }

    return @($results | Sort-Object full_name -Unique)
}

function Clear-ProcmonBootResidue {
    $result = [ordered]@{
        removed_bootlog_files = @()
        removed_registry_values = @()
        errors = @()
    }

    foreach ($candidate in Find-StaleProcmonBootLogs) {
        try {
            Remove-Item -Path $candidate.full_name -Force -ErrorAction Stop
            $result.removed_bootlog_files += [ordered]@{
                full_name = $candidate.full_name
                removed = $true
            }
        }
        catch {
            $result.removed_bootlog_files += [ordered]@{
                full_name = $candidate.full_name
                removed = $false
                error = $_.Exception.Message
            }
        }
    }

    $procmonStatePath = 'HKCU:\Software\Sysinternals\Process Monitor'
    if (Test-Path $procmonStatePath) {
        foreach ($name in @('Logfile', 'SourcePath', 'FlightRecorder')) {
            try {
                $value = Get-ItemProperty -Path $procmonStatePath -Name $name -ErrorAction Stop
                Remove-ItemProperty -Path $procmonStatePath -Name $name -ErrorAction Stop
                $result.removed_registry_values += [ordered]@{
                    path = $procmonStatePath
                    name = $name
                    previous_value = $value.$name
                    removed = $true
                }
            }
            catch [System.Management.Automation.ItemNotFoundException] {
            }
            catch {
                $result.errors += [ordered]@{
                    path = $procmonStatePath
                    name = $name
                    error = $_.Exception.Message
                }
            }
        }
    }

    return $result
}

$effectivePrefix = New-ProbePrefix -ConfiguredPrefix $Prefix -Path $RegistryPath -Name $ValueName
$pml = Join-Path $OutputDirectory "$effectivePrefix.pml"
$csv = Join-Path $OutputDirectory "$effectivePrefix.csv"
$hitsCsv = Join-Path $OutputDirectory "$effectivePrefix.hits.csv"
$result = Join-Path $OutputDirectory "$effectivePrefix.txt"
$stagePath = Join-Path $OutputDirectory "$effectivePrefix.stage.json"
$normalizedBundle = Join-Path $OutputDirectory "$effectivePrefix.normalized.json"
$normalizedProcmonPath = Normalize-RegistryPathForProcmon -Path $RegistryPath
$original = $null
$lines = New-Object System.Collections.Generic.List[string]
$normalizedStatus = 'missing'
$normalizedError = $null

try {
    if ([string]::IsNullOrWhiteSpace($PowerShellCommand)) {
        throw 'PowerShellCommand is required.'
    }

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $original = Get-ValueState -Path $RegistryPath -Name $ValueName

    foreach ($path in @($pml, $csv, $hitsCsv, $result, $stagePath, $normalizedBundle)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
        }
    }

    Publish-ProbeStage -Stage 'procmon-cleanup-boot-residue'
    $cleanup = Clear-ProcmonBootResidue
    $lines.Add('PROCMON_BOOT_RESIDUE_CLEANUP=' + ($cleanup | ConvertTo-Json -Compress -Depth 6))

    Publish-ProbeStage -Stage 'procmon-terminate-existing'
    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    Publish-ProbeStage -Stage 'procmon-start'
    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) | Out-Null
    Start-Sleep -Seconds 4

    Publish-ProbeStage -Stage 'trigger-start'
    & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -Command $PowerShellCommand | Out-Null
    Start-Sleep -Seconds 5
    Publish-ProbeStage -Stage 'trigger-done'

    Publish-ProbeStage -Stage 'procmon-stop'
    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    Publish-ProbeStage -Stage 'procmon-saveas'
    $saveProc = Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/OpenLog', $pml, '/SaveAs', $csv, '/Quiet') -PassThru -WindowStyle Hidden
    if (-not $saveProc.WaitForExit([Math]::Max($SaveAsTimeoutSeconds, 1) * 1000)) {
        try {
            Stop-Process -Id $saveProc.Id -Force -ErrorAction SilentlyContinue
        }
        catch {
        }
        throw "Procmon SaveAs timed out after $SaveAsTimeoutSeconds second(s)."
    }
    if ($saveProc.ExitCode -ne 0) {
        throw "Procmon SaveAs exited with code $($saveProc.ExitCode)."
    }
    if (-not (Test-Path $csv)) {
        throw "Procmon SaveAs did not create $csv"
    }

    $matches = @()
    if (Test-Path $csv) {
        Publish-ProbeStage -Stage 'csv-import'
        $fragments = @($normalizedProcmonPath, $ValueName) + @($MatchFragments | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $processNames = @($ProcessNames | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $rows = Import-Csv $csv
        $matches = $rows | Where-Object {
            $path = $_.Path
            $processName = $_.'Process Name'
            $operation = $_.Operation

            if ($operation -notlike 'Reg*') {
                return $false
            }

            $pathMatched = $false
            foreach ($fragment in $fragments) {
                if ($path -like "*$fragment*") {
                    $pathMatched = $true
                    break
                }
            }

            if (-not $pathMatched) {
                return $false
            }

            if (@($processNames).Count -eq 0) {
                return $true
            }

            foreach ($name in $processNames) {
                if ($processName -ieq $name) {
                    return $true
                }
            }

            return $false
        }

        if (@($matches).Count -gt 0) {
            $matches | Export-Csv -Path $hitsCsv -NoTypeInformation -Encoding UTF8
        }

        try {
            $bundle = Write-NormalizedProcmonBundle -BundlePath $normalizedBundle -RunId $effectivePrefix -Rows @($matches) -InputPath $csv -RegistryPath $RegistryPath -ValueName $ValueName
            $normalizedStatus = [string]$bundle.status
        }
        catch {
            $normalizedStatus = 'error'
            $normalizedError = $_.Exception.Message
        }
    }

    $lines.Add("MODE=$Mode")
    $lines.Add("REGISTRY_PATH=$RegistryPath")
    $lines.Add("VALUE_NAME=$ValueName")
    $lines.Add("PML_EXISTS=" + (Test-Path $pml))
    $lines.Add("CSV_EXISTS=" + (Test-Path $csv))
    $lines.Add("HITSCSV_EXISTS=" + (Test-Path $hitsCsv))
    $lines.Add("NORMALIZED_BUNDLE_EXISTS=" + (Test-Path $normalizedBundle))
    $lines.Add("NORMALIZATION_STATUS=" + $normalizedStatus)
    if ($normalizedError) {
        $lines.Add("NORMALIZATION_ERROR=" + $normalizedError)
    }
    $lines.Add("MATCH_COUNT=" + @($matches).Count)
    foreach ($match in $matches) {
        $lines.Add(('{0} | {1} | {2} | {3} | {4} | {5}' -f $match.'Time of Day', $match.'Process Name', $match.Operation, $match.Path, $match.Result, $match.Detail))
    }
    $lines.Add('ORIGINAL=' + ($original | ConvertTo-Json -Compress))
    Publish-ProbeStage -Stage 'complete' -Status 'ok'
}
catch {
    Publish-ProbeStage -Stage 'exception' -Status 'error' -Message $_.Exception.Message
    $lines.Add('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message)
    if ($_.InvocationInfo) {
        $lines.Add('AT=' + $_.InvocationInfo.PositionMessage)
    }
}
finally {
    if ($original) {
        Restore-ValueState -Path $RegistryPath -Name $ValueName -State $original
    }

    try {
        $restored = Get-ValueState -Path $RegistryPath -Name $ValueName
        $lines.Add('RESTORED=' + ($restored | ConvertTo-Json -Compress))
    }
    catch {
    }

    $lines | Set-Content -Path $result -Encoding UTF8
}
