[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputJson,

    [Parameter(Mandatory = $true)]
    [string]$OutputEvents,

    [string]$OutputError = '',
    [switch]$RestartWinDefend,
    [ValidateSet('com', 'pe')]
    [string]$SampleKind = 'com',
    [int]$ScanTimeoutSeconds = 60,

    [string]$WorkRoot = 'C:\Tools\Perf\Procmon\ThreatFileHashProbe'
)

$ErrorActionPreference = 'Stop'

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $Value | ConvertTo-Json -Depth 8 | Set-Content -Path $Path -Encoding UTF8
}

function Get-RecentDefenderEvents {
    param(
        [datetime]$StartTime
    )

    $eventIds = @(1116, 1120, 5007, 5001, 5013)
    $events = Get-WinEvent -FilterHashtable @{
        LogName = 'Microsoft-Windows-Windows Defender/Operational'
        StartTime = $StartTime
    } -ErrorAction SilentlyContinue

    return @($events | Where-Object { $_.Id -in $eventIds } | Sort-Object TimeCreated)
}

function Get-MpStatusSummary {
    try {
        $status = Get-MpComputerStatus -ErrorAction Stop
        return [ordered]@{
            am_running_mode = $status.AMRunningMode
            antivirus_enabled = $status.AntivirusEnabled
            realtime_enabled = $status.RealTimeProtectionEnabled
            behavior_monitor_enabled = $status.BehaviorMonitorEnabled
            ioav_enabled = $status.IoavProtectionEnabled
            antispyware_enabled = $status.AntispywareEnabled
        }
    }
    catch {
        return [ordered]@{
            error = $_.Exception.Message
        }
    }
}

function New-EicarSample {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [ValidateSet('com', 'pe')]
        [string]$Kind
    )

    $eicar = 'X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*'

    switch ($Kind) {
        'com' {
            $path = Join-Path $Root 'eicar.com'
            [System.IO.File]::WriteAllText($path, $eicar, [System.Text.Encoding]::ASCII)
            return $path
        }

        'pe' {
            $templateCandidates = @(
                (Join-Path $env:SystemRoot 'System32\choice.exe'),
                (Join-Path $env:SystemRoot 'System32\notepad.exe')
            )

            $templatePath = $templateCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
            if (-not $templatePath) {
                throw 'Could not find a built-in PE template for the Defender PE sample.'
            }

            $path = Join-Path $Root 'eicar-pe.exe'
            Copy-Item -Path $templatePath -Destination $path -Force

            $bytes = [System.Text.Encoding]::ASCII.GetBytes($eicar)
            $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::Append, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
            try {
                $stream.Write($bytes, 0, $bytes.Length)
            }
            finally {
                $stream.Dispose()
            }

            return $path
        }
    }
}

try {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputJson) | Out-Null
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputEvents) | Out-Null
    New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null

    $startTime = Get-Date
    $serviceRestart = [ordered]@{
        requested = [bool]$RestartWinDefend
        attempted = $false
        succeeded = $false
        error = $null
    }
    $samplePath = New-EicarSample -Root $WorkRoot -Kind $SampleKind

    $statusBefore = Get-MpStatusSummary
    $mpCmdRun = Join-Path $env:ProgramFiles 'Windows Defender\MpCmdRun.exe'

    if ($RestartWinDefend) {
        $serviceRestart.attempted = $true
        try {
            Restart-Service -Name 'WinDefend' -Force -ErrorAction Stop
            Start-Sleep -Seconds 10
            $serviceRestart.succeeded = $true
        }
        catch {
            $serviceRestart.error = $_.Exception.Message
        }
    }

    $events = @()
    $deadline = (Get-Date).AddSeconds(45)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $events = Get-RecentDefenderEvents -StartTime $startTime
        if (@($events | Where-Object Id -eq 1116).Count -gt 0) {
            break
        }
    }

    $mpCmdRunTimedOut = $false
    $mpCmdRunExitCode = $null
    if (@($events | Where-Object Id -eq 1116).Count -eq 0 -and (Test-Path $mpCmdRun)) {
        $scanProcess = Start-Process -FilePath $mpCmdRun -ArgumentList @('-Scan', '-ScanType', '3', '-File', $samplePath) -PassThru -WindowStyle Hidden
        if (-not $scanProcess.WaitForExit($ScanTimeoutSeconds * 1000)) {
            $mpCmdRunTimedOut = $true
            try {
                Stop-Process -Id $scanProcess.Id -Force -ErrorAction Stop
            }
            catch {
            }
        }
        else {
            $mpCmdRunExitCode = $scanProcess.ExitCode
        }

        $deadline = (Get-Date).AddSeconds(30)
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Seconds 2
            $events = Get-RecentDefenderEvents -StartTime $startTime
            if (@($events | Where-Object Id -eq 1116).Count -gt 0) {
                break
            }
        }
    }

    $events = Get-RecentDefenderEvents -StartTime $startTime

    $summary = [ordered]@{
        start_time = $startTime.ToString('o')
        sample_kind = $SampleKind
        sample_path = $samplePath
        eicar_path = $samplePath
        file_exists_after = Test-Path $samplePath
        mp_status_before = $statusBefore
        service_restart = $serviceRestart
        mpcmdrun_exists = Test-Path $mpCmdRun
        mpcmdrun_exit_code = $mpCmdRunExitCode
        mpcmdrun_timed_out = $mpCmdRunTimedOut
        detection_event_count = @($events | Where-Object Id -eq 1116).Count
        hash_event_count = @($events | Where-Object Id -eq 1120).Count
        config_change_event_count = @($events | Where-Object Id -eq 5007).Count
        recent_event_ids = @($events | Select-Object -ExpandProperty Id)
        hash_event_messages = @(
            $events |
                Where-Object Id -eq 1120 |
                ForEach-Object { ($_.Message -replace '\r?\n', ' ') }
        )
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('START_TIME=' + $summary.start_time)
    $lines.Add('SAMPLE_KIND=' + $summary.sample_kind)
    $lines.Add('SAMPLE_PATH=' + $summary.sample_path)
    $lines.Add('EICAR_PATH=' + $summary.eicar_path)
    $lines.Add('FILE_EXISTS_AFTER=' + $summary.file_exists_after)
    $lines.Add('MP_STATUS=' + (($summary.mp_status_before | ConvertTo-Json -Compress)))
    $lines.Add('SERVICE_RESTART=' + (($summary.service_restart | ConvertTo-Json -Compress)))
    $lines.Add('MPCMDRUN_EXISTS=' + $summary.mpcmdrun_exists)
    $lines.Add('MPCMDRUN_EXIT_CODE=' + $summary.mpcmdrun_exit_code)
    $lines.Add('MPCMDRUN_TIMED_OUT=' + $summary.mpcmdrun_timed_out)
    $lines.Add('DETECTION_EVENT_COUNT=' + $summary.detection_event_count)
    $lines.Add('HASH_EVENT_COUNT=' + $summary.hash_event_count)
    $lines.Add('CONFIG_CHANGE_EVENT_COUNT=' + $summary.config_change_event_count)
    foreach ($event in $events) {
        $message = ($event.Message -replace '\r?\n', ' ').Trim()
        $lines.Add(('{0:o} | ID={1} | {2}' -f $event.TimeCreated, $event.Id, $message))
    }

    Write-JsonFile -Value $summary -Path $OutputJson
    $lines | Set-Content -Path $OutputEvents -Encoding UTF8
}
catch {
    if (-not [string]::IsNullOrWhiteSpace($OutputError)) {
        @(
            'ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message,
            'AT=' + $_.InvocationInfo.PositionMessage
        ) | Set-Content -Path $OutputError -Encoding UTF8
    }

    throw
}
