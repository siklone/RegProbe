[CmdletBinding()]
param(
    [string]$OutputName = 'win32k-etw-s1-20260412c',
    [string]$RegistryPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Power',
    [string]$ValueName = 'Win32kCalloutWatchdogTimeoutSeconds',
    [string]$OutputRoot = '',
    [string]$UploadBaseUrl = 'http://10.0.2.2:8766',
    [int]$TracerptTimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\etw' $OutputName
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$traceName = 'regprobe-win32k-callout-watchdog-etw'
$etlPath = Join-Path $OutputRoot ($OutputName + '.etl')
$xmlPath = Join-Path $OutputRoot ($OutputName + '.xml')
$hitsPath = Join-Path $OutputRoot ($OutputName + '.hits.txt')
$resultPath = Join-Path $OutputRoot ($OutputName + '.txt')
$summaryPath = Join-Path $OutputRoot 'trace-summary.json'
$tracerptStdout = Join-Path $OutputRoot 'tracerpt.stdout.txt'
$tracerptStderr = Join-Path $OutputRoot 'tracerpt.stderr.txt'
$lines = New-Object System.Collections.Generic.List[string]
$uploads = [ordered]@{}
$hadError = $false
$errorMessage = $null
$errorKind = $null
$tracerptTimedOut = $false
$traceStarted = $false
$etlCandidates = @()
$sentinelHits = @()

function Add-Line {
    param([string]$Line)

    $script:lines.Add($Line)
}

function ConvertTo-CompactJson {
    param([object]$Value)

    return ($Value | ConvertTo-Json -Compress -Depth 8)
}

function Get-RegistryValueState {
    param(
        [string]$Path,
        [string]$Name
    )

    $state = [ordered]@{
        path_exists = $false
        value_exists = $false
        value = $null
        value_type = $null
    }

    if (-not (Test-Path $Path)) {
        return $state
    }

    $state.path_exists = $true
    try {
        $item = Get-Item -Path $Path -ErrorAction Stop
        $value = Get-ItemProperty -Path $Path -Name $Name -ErrorAction Stop
        $state.value = $value.$Name
        $state.value_exists = $true
        $state.value_type = [string]$item.GetValueKind($Name)
    }
    catch [System.Management.Automation.ItemNotFoundException] {
    }
    catch {
        $state.value_error = $_.Exception.Message
    }

    return $state
}

function Upload-Artifact {
    param(
        [string]$Path,
        [string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path $Path)) {
        return
    }

    $uri = ('{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName)
    Invoke-WebRequest -Method Put -Uri $uri -InFile $Path -UseBasicParsing | Out-Null
    $script:uploads[$RemoteName] = $uri
}

function Invoke-ProcessWithTimeout {
    param(
        [string]$FilePath,
        [string[]]$ArgumentList,
        [int]$TimeoutSeconds,
        [string]$StdoutPath,
        [string]$StderrPath
    )

    $process = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -NoNewWindow -PassThru -RedirectStandardOutput $StdoutPath -RedirectStandardError $StderrPath
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try {
            $process.Kill()
        }
        catch {
        }
        return [ordered]@{
            exit_code = $null
            timed_out = $true
        }
    }

    $process.Refresh()
    return [ordered]@{
        exit_code = $process.ExitCode
        timed_out = $false
    }
}

function Invoke-S1CalloutTrigger {
    param([string]$DiagPath)

    New-Item -ItemType Directory -Path $DiagPath -Force | Out-Null
    cmd /c "powercfg /a > `"$DiagPath\powercfg-a-before.txt`"" | Out-Null
    cmd /c "powercfg /lastwake > `"$DiagPath\lastwake-before.txt`"" | Out-Null
    cmd /c "wevtutil qe System /q:""*[System[(Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=1 or EventID=42 or EventID=107 or EventID=506))]]"" /c:20 /rd:true /f:text > `"$DiagPath\kernelpower-before.txt`"" | Out-Null

    Start-Sleep -Seconds 3
    Start-Process -FilePath "$env:SystemRoot\System32\rundll32.exe" -ArgumentList 'powrprof.dll,SetSuspendState 0,1,0' -WindowStyle Hidden | Out-Null
    Start-Sleep -Seconds 50

    cmd /c "powercfg /lastwake > `"$DiagPath\lastwake-after.txt`"" | Out-Null
    cmd /c "wevtutil qe System /q:""*[System[(Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=1 or EventID=42 or EventID=107 or EventID=506))]]"" /c:20 /rd:true /f:text > `"$DiagPath\kernelpower-after.txt`"" | Out-Null
    cmd /c "powercfg /a > `"$DiagPath\powercfg-a-after.txt`"" | Out-Null
}

try {
    Add-Line ('OUTPUT_NAME=' + $OutputName)
    Add-Line ('REGISTRY_PATH=' + $RegistryPath)
    Add-Line ('VALUE_NAME=' + $ValueName)

    $stateBefore = Get-RegistryValueState -Path $RegistryPath -Name $ValueName
    Add-Line ('REGISTRY_STATE_BEFORE=' + (ConvertTo-CompactJson $stateBefore))

    & logman stop $traceName 2>$null | Out-Null
    & logman delete $traceName 2>$null | Out-Null

    $createOutput = & logman create trace $traceName `
        -p 'Microsoft-Windows-Kernel-Registry' 0xFFFF 0xFF `
        -o $etlPath `
        -f bincirc -max 64 -nb 16 512 2>&1
    Add-Line ('LOGMAN_CREATE=' + (($createOutput | ForEach-Object { [string]$_ }) -join ' | '))

    if ($LASTEXITCODE -ne 0) {
        throw "logman create failed with exit code $LASTEXITCODE"
    }

    $startOutput = & logman start $traceName 2>&1
    Add-Line ('LOGMAN_START=' + (($startOutput | ForEach-Object { [string]$_ }) -join ' | '))

    if ($LASTEXITCODE -ne 0) {
        throw "logman start failed with exit code $LASTEXITCODE"
    }

    $traceStarted = $true

    $sentinelOutput = cmd /c 'reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v ProductName' 2>&1
    Add-Line ('SENTINEL_REG_QUERY=' + (($sentinelOutput | ForEach-Object { [string]$_ }) -join ' | '))

    Invoke-S1CalloutTrigger -DiagPath (Join-Path $OutputRoot 's1-trigger')
}
catch {
    $hadError = $true
    $errorKind = $_.Exception.GetType().FullName
    $errorMessage = $_.Exception.Message
    Add-Line ('ERROR=' + $errorKind + ': ' + $errorMessage)
}
finally {
    if ($traceStarted) {
        try {
            $stopOutput = & logman stop $traceName 2>&1
            Add-Line ('LOGMAN_STOP=' + (($stopOutput | ForEach-Object { [string]$_ }) -join ' | '))
        }
        catch {
            Add-Line ('LOGMAN_STOP_ERROR=' + $_.Exception.Message)
        }
    }

    try {
        & logman delete $traceName 2>$null | Out-Null
    }
    catch {
    }

    $stateAfter = Get-RegistryValueState -Path $RegistryPath -Name $ValueName
    Add-Line ('REGISTRY_STATE_AFTER=' + (ConvertTo-CompactJson $stateAfter))

    $etlCandidates = @(Get-ChildItem -Path $OutputRoot -Filter '*.etl' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending)
    if (-not (Test-Path $etlPath) -and @($etlCandidates).Count -gt 0) {
        $etlPath = $etlCandidates[0].FullName
    }

    $etlExists = Test-Path $etlPath
    $etlSize = if ($etlExists) { (Get-Item $etlPath).Length } else { 0 }
    Add-Line ('ETL_CANDIDATES=' + (ConvertTo-CompactJson @($etlCandidates | Select-Object FullName, Length, LastWriteTimeUtc)))
    Add-Line ('ETL_EXISTS=' + $etlExists)
    Add-Line ('ETL_SIZE_BYTES=' + $etlSize)

    $tracerptResult = $null
    if ($etlExists) {
        $tracerptResult = Invoke-ProcessWithTimeout `
            -FilePath 'tracerpt.exe' `
            -ArgumentList @($etlPath, '-o', $xmlPath, '-of', 'XML', '-lr') `
            -TimeoutSeconds $TracerptTimeoutSeconds `
            -StdoutPath $tracerptStdout `
            -StderrPath $tracerptStderr
        $tracerptTimedOut = [bool]$tracerptResult.timed_out
        Add-Line ('TRACERPT_RESULT=' + (ConvertTo-CompactJson $tracerptResult))
    }

    $xmlExists = Test-Path $xmlPath
    $hits = @()
    if ($xmlExists) {
        $patterns = @(
            $ValueName,
            'Win32kCalloutWatchdogTimeoutSeconds',
            'Control\Power',
            'HKLM\SYSTEM\CurrentControlSet\Control\Power',
            'REGISTRY\MACHINE\SYSTEM\CurrentControlSet\Control\Power'
        )
        $hits = @(Select-String -Path $xmlPath -Pattern $patterns -SimpleMatch -ErrorAction SilentlyContinue | Select-Object -First 100)
        $sentinelHits = @(Select-String -Path $xmlPath -Pattern @('CurrentVersion', 'ProductName') -SimpleMatch -ErrorAction SilentlyContinue | Select-Object -First 50)
        $hits | ForEach-Object { $_.Line } | Set-Content -Path $hitsPath -Encoding UTF8
    }

    Add-Line ('XML_EXISTS=' + $xmlExists)
    Add-Line ('SENTINEL_HITS_COUNT=' + @($sentinelHits).Count)
    Add-Line ('HITS_COUNT=' + @($hits).Count)
    $lines | Set-Content -Path $resultPath -Encoding UTF8

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        output_name = $OutputName
        registry_path = $RegistryPath
        value_name = $ValueName
        trigger_profile = 'watchdog-s1-callout'
        status = if ($hadError) { 'error' } elseif (@($hits).Count -gt 0) { 'hits' } else { 'no-hit' }
        etl_path = $etlPath
        etl_exists = [bool]$etlExists
        etl_size_bytes = $etlSize
        etl_candidates = @($etlCandidates | Select-Object FullName, Length, LastWriteTimeUtc)
        xml_path = $xmlPath
        xml_exists = [bool]$xmlExists
        hits_path = $hitsPath
        hits_count = @($hits).Count
        sentinel_hits_count = @($sentinelHits).Count
        tracerpt_timed_out = [bool]$tracerptTimedOut
        tracerpt_result = $tracerptResult
        registry_state_before = $stateBefore
        registry_state_after = $stateAfter
        error_kind = $errorKind
        error = $errorMessage
        bench_environment = 'windows-11-25h2-kvm'
        measurement_reliability = 'runtime-etw-relative'
    }

    $summary | ConvertTo-Json -Depth 10 | Set-Content -Path $summaryPath -Encoding UTF8

    foreach ($artifact in @(
        @{ path = $resultPath; name = ($OutputName + '.txt') },
        @{ path = $summaryPath; name = ($OutputName + '-summary.json') },
        @{ path = $hitsPath; name = ($OutputName + '.hits.txt') },
        @{ path = $tracerptStdout; name = ($OutputName + '-tracerpt.stdout.txt') },
        @{ path = $tracerptStderr; name = ($OutputName + '-tracerpt.stderr.txt') },
        @{ path = $etlPath; name = ($OutputName + '.etl') }
    )) {
        try {
            Upload-Artifact -Path $artifact.path -RemoteName $artifact.name
        }
        catch {
            Add-Line ('UPLOAD_ERROR=' + $artifact.name + ': ' + $_.Exception.Message)
        }
    }

    if ($xmlExists -and ((Get-Item $xmlPath).Length -le 16777216)) {
        try {
            Upload-Artifact -Path $xmlPath -RemoteName ($OutputName + '.xml')
        }
        catch {
            Add-Line ('UPLOAD_ERROR=' + $OutputName + '.xml: ' + $_.Exception.Message)
        }
    }
}

if ($hadError) {
    exit 1
}
