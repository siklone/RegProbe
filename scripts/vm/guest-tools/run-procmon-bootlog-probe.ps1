[CmdletBinding()]
param(
    [ValidateSet('arm', 'collect')]
    [string]$Stage = 'arm',

    [string]$RegistryPath = '',
    [string]$ValueName = '',
    [string]$OutputName = 'procmon-bootlog',
    [string]$OutputRoot = '',
    [string]$UploadBaseUrl = '',
    [string]$StateFile = '',
    [string[]]$MatchFragments = @(),
    [string[]]$ProcessNames = @(),
    [int]$UploadRetryCount = 20,
    [int]$UploadRetryDelaySeconds = 5
)

$ErrorActionPreference = 'Stop'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )

    $json = $Payload | ConvertTo-Json -Depth 12
    Set-Content -Path $Path -Value $json -Encoding UTF8
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

function Normalize-RegistryPathForProcmon {
    param([Parameter(Mandatory = $true)][string]$Path)

    return $Path.Replace('HKLM:\', 'HKLM\').Replace('HKCU:\', 'HKCU\').Replace('HKCR:\', 'HKCR\').Replace('HKU:\', 'HKU\')
}

function Get-ProcmonState {
    $state = [ordered]@{}
    try {
        $item = Get-ItemProperty -Path 'HKCU:\Software\Sysinternals\Process Monitor' -ErrorAction Stop
        foreach ($name in @('Logfile', 'SourcePath', 'FlightRecorder', 'RingBufferSize', 'RingBufferMin')) {
            $state[$name] = $item.$name
        }
    }
    catch {
        $state['error'] = $_.Exception.Message
    }

    return $state
}

function Invoke-ProcmonCli {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-procmon-' + [Guid]::NewGuid().ToString('N') + '.stdout.txt')
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-procmon-' + [Guid]::NewGuid().ToString('N') + '.stderr.txt')

    try {
        $proc = Start-Process -FilePath $procmon -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $stdoutContent = ''
        $stderrContent = ''
        if (Test-Path $stdoutPath) {
            $stdoutContent = Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue
        }

        if (Test-Path $stderrPath) {
            $stderrContent = Get-Content -Path $stderrPath -Raw -ErrorAction SilentlyContinue
        }

        $stdout = ('{0}' -f $stdoutContent)
        $stderr = ('{0}' -f $stderrContent)
        $combined = @($stdout.Trim(), $stderr.Trim()) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

        return [ordered]@{
            exit_code = $proc.ExitCode
            stdout = $stdout.Trim()
            stderr = $stderr.Trim()
            output = ($combined -join [Environment]::NewLine)
        }
    }
    catch {
        return [ordered]@{
            exit_code = $null
            stdout = ''
            stderr = ''
            output = ''
            error = $_.Exception.Message
            position = if ($_.InvocationInfo) { $_.InvocationInfo.PositionMessage } else { $null }
        }
    }
    finally {
        Remove-Item -Path $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Find-BootLogCandidates {
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

function Remove-StaleBootLogs {
    $removed = @()
    foreach ($candidate in Find-BootLogCandidates) {
        try {
            Remove-Item -Path $candidate.full_name -Force -ErrorAction Stop
            $removed += [pscustomobject]@{
                full_name = $candidate.full_name
                removed = $true
            }
        }
        catch {
            $removed += [pscustomobject]@{
                full_name = $candidate.full_name
                removed = $false
                error = $_.Exception.Message
            }
        }
    }

    return @($removed)
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

function Get-PathMatchSummary {
    param(
        [Parameter(Mandatory = $true)][string]$CsvPath,
        [Parameter(Mandatory = $true)][string[]]$SearchFragments,
        [Parameter(Mandatory = $true)][string[]]$ProcessFilters
    )

    $fragmentCounts = [ordered]@{}
    foreach ($fragment in $SearchFragments) {
        if (-not [string]::IsNullOrWhiteSpace($fragment) -and -not $fragmentCounts.Contains($fragment)) {
            $fragmentCounts[$fragment] = 0
        }
    }

    $matches = New-Object 'System.Collections.Generic.List[object]'
    $csvRowCount = 0
    $registryRowCount = 0

    Import-Csv -Path $CsvPath | ForEach-Object {
        $csvRowCount++
        $path = [string]$_.Path
        $operation = [string]$_.Operation
        $processName = [string]$_.'Process Name'
        if ($operation -notlike 'Reg*') {
            return
        }

        $registryRowCount++
        $pathMatched = $false
        foreach ($fragment in $fragmentCounts.Keys) {
            if ($path -like "*$fragment*") {
                $fragmentCounts[$fragment]++
                $pathMatched = $true
            }
        }

        if (-not $pathMatched) {
            return
        }

        if (@($ProcessFilters).Count -gt 0) {
            $processMatched = $false
            foreach ($filter in $ProcessFilters) {
                if ($processName -ieq $filter) {
                    $processMatched = $true
                    break
                }
            }

            if (-not $processMatched) {
                return
            }
        }

        $matches.Add($_)
    }

    return [ordered]@{
        csv_row_count = $csvRowCount
        registry_row_count = $registryRowCount
        fragment_hit_counts = $fragmentCounts
        matches = @($matches)
    }
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\procmon-bootlog' $OutputName
}

Ensure-Directory -Path $OutputRoot

if ([string]::IsNullOrWhiteSpace($StateFile)) {
    $StateFile = Join-Path $OutputRoot 'state.json'
}

$armSummaryPath = Join-Path $OutputRoot 'summary-arm.json'
$collectSummaryPath = Join-Path $OutputRoot 'summary-collect.json'
$summaryPath = Join-Path $OutputRoot 'summary.json'
$pmlPath = Join-Path $OutputRoot ($OutputName + '.pml')
$csvPath = Join-Path $OutputRoot ($OutputName + '.csv')
$hitsPath = Join-Path $OutputRoot ($OutputName + '.hits.csv')
$normalizedProcmonPath = if ([string]::IsNullOrWhiteSpace($RegistryPath)) { $null } else { Normalize-RegistryPathForProcmon -Path $RegistryPath }

if ($Stage -eq 'arm') {
    if ([string]::IsNullOrWhiteSpace($RegistryPath)) {
        throw 'RegistryPath is required for arm.'
    }

    if ([string]::IsNullOrWhiteSpace($ValueName)) {
        throw 'ValueName is required for arm.'
    }

    foreach ($path in @($armSummaryPath, $collectSummaryPath, $summaryPath, $pmlPath, $csvPath, $hitsPath, $StateFile)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
        }
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        stage = 'arm'
        output_name = $OutputName
        output_root = $OutputRoot
        registry_path = $RegistryPath
        value_name = $ValueName
        procmon_path = $procmon
        procmon_exists = [bool](Test-Path $procmon)
        normalized_procmon_path = $normalizedProcmonPath
        match_fragments = @($MatchFragments | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        process_names = @($ProcessNames | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        boot_time_utc_before = Get-BootTimeUtc
        bootlog_candidates_before = Find-BootLogCandidates
        removed_stale_bootlogs = @()
        procmon_state_before = Get-ProcmonState
        commands = [ordered]@{}
        procmon_state_after = $null
        errors = @()
        uploads = [ordered]@{}
    }

    try {
        if (-not (Test-Path $procmon)) {
            throw 'Procmon64.exe was not found in the guest.'
        }

        $summary.removed_stale_bootlogs = Remove-StaleBootLogs
        & $procmon /Terminate /Quiet | Out-Null
        Start-Sleep -Seconds 2

        $summary['commands']['direct_enable'] = Invoke-ProcmonCli -Arguments @('/AcceptEula', '/Quiet', '/EnableBootLogging')
        $summary['commands']['minimized_enable'] = Invoke-ProcmonCli -Arguments @('/AcceptEula', '/Quiet', '/Minimized', '/EnableBootLogging')
    }
    catch {
        $summary.errors = @($summary.errors) + $_.Exception.Message
        if ($_.InvocationInfo) {
            $summary.errors = @($summary.errors) + $_.InvocationInfo.PositionMessage
        }
    }

    $summary.procmon_state_after = Get-ProcmonState
    Write-JsonFile -Path $StateFile -Payload ([ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        output_name = $OutputName
        output_root = $OutputRoot
        registry_path = $RegistryPath
        value_name = $ValueName
        normalized_procmon_path = $normalizedProcmonPath
        match_fragments = @($summary.match_fragments)
        process_names = @($summary.process_names)
        boot_time_utc_before = $summary.boot_time_utc_before
        arm_commands = $summary['commands']
        arm_errors = @($summary.errors)
        procmon_state_before = $summary.procmon_state_before
        procmon_state_after = $summary.procmon_state_after
    })
    Write-JsonFile -Path $armSummaryPath -Payload $summary

    try {
        $upload = Invoke-ArtifactUpload -Path $armSummaryPath -RemoteName ($OutputName + '-summary-arm.json')
        if ($upload) {
            $summary['uploads']['summary_arm'] = $upload
            Write-JsonFile -Path $armSummaryPath -Payload $summary
        }
    }
    catch {
        $summary.errors = @($summary.errors) + ('Failed to upload arm summary: ' + $_.Exception.Message)
        Write-JsonFile -Path $armSummaryPath -Payload $summary
    }

    Write-Output $armSummaryPath
    return
}

if (-not (Test-Path -Path $StateFile -PathType Leaf)) {
    throw "State file not found: $StateFile"
}

$state = Get-Content -Path $StateFile -Raw | ConvertFrom-Json
$RegistryPath = [string]$state.registry_path
$ValueName = [string]$state.value_name
$OutputName = [string]$state.output_name
$OutputRoot = [string]$state.output_root
$normalizedProcmonPath = [string]$state.normalized_procmon_path
$stateMatchFragments = @($state.match_fragments)
$stateProcessNames = @($state.process_names)
$armCommands = $state.arm_commands
$searchFragments = @($normalizedProcmonPath, $ValueName) + @($stateMatchFragments)
$searchFragments = @($searchFragments | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    stage = 'collect'
    output_name = $OutputName
    output_root = $OutputRoot
    registry_path = $RegistryPath
    value_name = $ValueName
    procmon_path = $procmon
    procmon_exists = [bool](Test-Path $procmon)
    normalized_procmon_path = $normalizedProcmonPath
    search_fragments = @($searchFragments)
    process_names = @($stateProcessNames)
    bootlog_enable_accepted = $true
    boot_time_utc_before = [string]$state.boot_time_utc_before
    boot_time_utc_after = Get-BootTimeUtc
    reboot_observed = $false
    bootlog_candidates_before = Find-BootLogCandidates
    procmon_state_before = Get-ProcmonState
    commands = [ordered]@{}
    pml_exists = $false
    csv_exists = $false
    hits_exists = $false
    pml_length = 0
    csv_length = 0
    csv_row_count = 0
    registry_row_count = 0
    match_count = 0
    fragment_hit_counts = [ordered]@{}
    bootlog_candidates_after = @()
    procmon_state_after = $null
    errors = @()
    uploads = [ordered]@{}
}

if ($summary.boot_time_utc_before -and $summary.boot_time_utc_after) {
    $summary.reboot_observed = ($summary.boot_time_utc_before -ne $summary.boot_time_utc_after)
}

    try {
        if (-not (Test-Path $procmon)) {
            throw 'Procmon64.exe was not found in the guest.'
        }

    $directExit = $null
    $minimizedExit = $null
    if ($armCommands) {
        if ($armCommands.direct_enable) {
            $directExit = $armCommands.direct_enable.exit_code
        }
        if ($armCommands.minimized_enable) {
            $minimizedExit = $armCommands.minimized_enable.exit_code
        }
    }

    if (($null -ne $directExit -and $directExit -ne 0) -and ($null -ne $minimizedExit -and $minimizedExit -ne 0)) {
        $summary.bootlog_enable_accepted = $false
        $summary['commands']['convert_bootlog'] = [ordered]@{
            skipped = $true
            reason = 'bootlog-enable-nonzero-exit'
            arm_direct_exit_code = $directExit
            arm_minimized_exit_code = $minimizedExit
        }
    }
    else {
        & $procmon /Terminate /Quiet | Out-Null
        Start-Sleep -Seconds 2

        $summary['commands']['convert_bootlog'] = Invoke-ProcmonCli -Arguments @('/AcceptEula', '/Quiet', '/Minimized', '/ConvertBootLog', $pmlPath)

        $summary.pml_exists = [bool](Test-Path $pmlPath)
        if ($summary.pml_exists) {
            $summary.pml_length = (Get-Item -Path $pmlPath).Length

            $summary['commands']['save_as_csv'] = Invoke-ProcmonCli -Arguments @('/AcceptEula', '/OpenLog', $pmlPath, '/SaveAs', $csvPath, '/Quiet')

            $summary.csv_exists = [bool](Test-Path $csvPath)
            if ($summary.csv_exists) {
                $summary.csv_length = (Get-Item -Path $csvPath).Length
                $matchSummary = Get-PathMatchSummary -CsvPath $csvPath -SearchFragments $searchFragments -ProcessFilters $stateProcessNames
                $summary.csv_row_count = $matchSummary.csv_row_count
                $summary.registry_row_count = $matchSummary.registry_row_count
                $summary.fragment_hit_counts = $matchSummary.fragment_hit_counts
                $summary.match_count = @($matchSummary.matches).Count
                if ($summary.match_count -gt 0) {
                    @($matchSummary.matches) | Export-Csv -Path $hitsPath -NoTypeInformation -Encoding UTF8
                }
                $summary.hits_exists = [bool](Test-Path $hitsPath)
            }
        }
    }
}
catch {
    $summary.errors = @($summary.errors) + $_.Exception.Message
    if ($_.InvocationInfo) {
        $summary.errors = @($summary.errors) + $_.InvocationInfo.PositionMessage
    }
}

$summary.bootlog_candidates_after = Find-BootLogCandidates
$summary.procmon_state_after = Get-ProcmonState
Write-JsonFile -Path $collectSummaryPath -Payload $summary
Write-JsonFile -Path $summaryPath -Payload $summary

foreach ($artifact in @(
    @{ key = 'summary_collect'; path = $collectSummaryPath; name = ($OutputName + '-summary-collect.json') },
    @{ key = 'summary'; path = $summaryPath; name = ($OutputName + '-summary.json') },
    @{ key = 'hits'; path = $hitsPath; name = ($OutputName + '.hits.csv') }
)) {
    try {
        $upload = Invoke-ArtifactUpload -Path $artifact.path -RemoteName $artifact.name
        if ($upload) {
            $summary['uploads'][$artifact.key] = $upload
        }
    }
    catch {
        $summary.errors = @($summary.errors) + ("Failed to upload {0}: {1}" -f $artifact.key, $_.Exception.Message)
    }
}

Write-JsonFile -Path $collectSummaryPath -Payload $summary
Write-JsonFile -Path $summaryPath -Payload $summary
Write-Output $summaryPath
