[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon',
    [string]$SnapshotName = 'baseline-20260325-shell-stable',
    [string]$IncidentLogPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = 'reliability-timestamp-probe'
$recordId = 'system.reliability-timestamp-enabled'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $probeName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $probeName, $stamp)
$hostScriptPath = Join-Path $hostRoot 'reliability-timestamp-probe-inner.ps1'
$guestScriptPath = Join-Path $GuestScriptRoot 'reliability-timestamp-probe-inner.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$shellHealthScript = Join-Path $PSScriptRoot 'get-vm-shell-health.ps1'
$incidentScript = Join-Path $PSScriptRoot 'log-vm-incident.ps1'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$guestScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [string]$ProbeName = 'reliability-timestamp-probe'
)

$ErrorActionPreference = 'Stop'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
$policyPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Reliability'
$fallbackPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability'
$targets = @(
    @{ path = $policyPath; name = 'TimeStampEnabled'; type = 'DWord'; value = 1 },
    @{ path = $policyPath; name = 'TimeStampInterval'; type = 'DWord'; value = 86400 },
    @{ path = $fallbackPath; name = 'TimeStampInterval'; type = 'DWord'; value = 1 }
)

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
            path = $Path
            name = $Name
            path_exists = $true
            value_exists = $true
            value = $item.$Name
            value_type = $item.PSObject.Properties[$Name].TypeNameOfValue
        }
    }
    catch {
        return [ordered]@{
            path = $Path
            name = $Name
            path_exists = [bool](Test-Path $Path)
            value_exists = $false
            value = $null
            value_type = $null
        }
    }
}

function Restore-ValueState {
    param([Parameter(Mandatory = $true)][hashtable]$State)

    if ($State.value_exists) {
        if (-not (Test-Path $State.path)) {
            New-Item -Path $State.path -Force | Out-Null
        }

        $propertyType = switch -Regex ($State.value_type) {
            'Int64|UInt64' { 'QWord'; break }
            'Int32|UInt32|Int16|UInt16' { 'DWord'; break }
            'Byte\[\]' { 'Binary'; break }
            default { 'String' }
        }

        New-ItemProperty -Path $State.path -Name $State.name -PropertyType $propertyType -Value $State.value -Force | Out-Null
    }
    elseif (Test-Path $State.path) {
        Remove-ItemProperty -Path $State.path -Name $State.name -ErrorAction SilentlyContinue
    }
}

function Invoke-BestEffort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string[]]$ArgumentList = @(),
        [int]$SleepSeconds = 0
    )

    try {
        Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -WindowStyle Hidden | Out-Null
    }
    catch {
    }

    if ($SleepSeconds -gt 0) {
        Start-Sleep -Seconds $SleepSeconds
    }
}

function Invoke-BestEffortPowerShell {
    param([Parameter(Mandatory = $true)][string]$Command)

    try {
        & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -Command $Command | Out-Null
    }
    catch {
    }
}

function Normalize-RegistryPathForProcmon {
    param([string]$Path)

    return $Path.Replace('HKLM:\', 'HKLM\').Replace('HKCU:\', 'HKCU\')
}

$pml = Join-Path $OutputDirectory "$ProbeName.pml"
$csv = Join-Path $OutputDirectory "$ProbeName.csv"
$hitsCsv = Join-Path $OutputDirectory "$ProbeName.hits.csv"
$runtimeHitsCsv = Join-Path $OutputDirectory "$ProbeName.runtime.hits.csv"
$resultPath = Join-Path $OutputDirectory "$ProbeName.txt"
$summaryPath = Join-Path $OutputDirectory "$ProbeName.json"
$lines = New-Object System.Collections.Generic.List[string]
$originalStates = @()
$matches = @()
$runtimeMatches = @()

try {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    foreach ($path in @($pml, $csv, $hitsCsv, $runtimeHitsCsv, $resultPath, $summaryPath)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
        }
    }

    foreach ($target in $targets) {
        $originalStates += ,(Get-ValueState -Path $target.path -Name $target.name)
    }

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) | Out-Null
    Start-Sleep -Seconds 4

    foreach ($target in $targets) {
        if (-not (Test-Path $target.path)) {
            New-Item -Path $target.path -Force | Out-Null
        }
        New-ItemProperty -Path $target.path -Name $target.name -PropertyType $target.type -Value $target.value -Force | Out-Null
    }

    Invoke-BestEffortPowerShell -Command "Restart-Service -Name DiagTrack -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 8"
    Invoke-BestEffort -FilePath 'C:\Windows\System32\schtasks.exe' -ArgumentList @('/Run', '/TN', '\Microsoft\Windows\Customer Experience Improvement Program\Consolidator') -SleepSeconds 5
    Invoke-BestEffort -FilePath 'C:\Windows\System32\schtasks.exe' -ArgumentList @('/Run', '/TN', '\Microsoft\Windows\Windows Error Reporting\QueueReporting') -SleepSeconds 5
    Invoke-BestEffort -FilePath 'C:\Windows\System32\perfmon.exe' -ArgumentList @('/rel') -SleepSeconds 8
    Invoke-BestEffort -FilePath 'C:\Windows\System32\control.exe' -ArgumentList @('/name', 'Microsoft.ProblemReportsAndSolutions') -SleepSeconds 8
    Start-Sleep -Seconds 20

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    & $procmon /AcceptEula /OpenLog $pml /SaveAs $csv /Quiet | Out-Null

    if (Test-Path $csv) {
        $fragments = @(
            (Normalize-RegistryPathForProcmon -Path $policyPath),
            (Normalize-RegistryPathForProcmon -Path $fallbackPath),
            'TimeStampEnabled',
            'TimeStampInterval'
        )

        $rows = Import-Csv $csv
        $matches = $rows | Where-Object {
            $operation = $_.Operation
            $path = $_.Path
            if ($operation -notlike 'Reg*') {
                return $false
            }

            foreach ($fragment in $fragments) {
                if ($path -like "*$fragment*") {
                    return $true
                }
            }

            return $false
        }

        if (@($matches).Count -gt 0) {
            $matches | Export-Csv -Path $hitsCsv -NoTypeInformation -Encoding UTF8
        }

        $runtimeMatches = $matches | Where-Object {
            $_.'Process Name' -notin @('powershell.exe', 'reg.exe', 'Procmon64.exe')
        }

        if (@($runtimeMatches).Count -gt 0) {
            $runtimeMatches | Export-Csv -Path $runtimeHitsCsv -NoTypeInformation -Encoding UTF8
        }
    }

    $lines.Add("MATCH_COUNT=$(@($matches).Count)")
    $lines.Add("RUNTIME_MATCH_COUNT=$(@($runtimeMatches).Count)")
    foreach ($match in $runtimeMatches) {
        $lines.Add(('{0} | {1} | {2} | {3} | {4} | {5}' -f $match.'Time of Day', $match.'Process Name', $match.Operation, $match.Path, $match.Result, $match.Detail))
    }
}
catch {
    $lines.Add('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message)
    if ($_.InvocationInfo) {
        $lines.Add('AT=' + $_.InvocationInfo.PositionMessage)
    }
}
finally {
    foreach ($state in $originalStates) {
        Restore-ValueState -State $state
    }

    $restoredStates = @()
    foreach ($target in $targets) {
        $restoredStates += ,(Get-ValueState -Path $target.path -Name $target.name)
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        policy_path = $policyPath
        fallback_path = $fallbackPath
        original = $originalStates
        restored = $restoredStates
        pml_exists = [bool](Test-Path $pml)
        csv_exists = [bool](Test-Path $csv)
        hits_csv_exists = [bool](Test-Path $hitsCsv)
        runtime_hits_csv_exists = [bool](Test-Path $runtimeHitsCsv)
        match_count = @($matches).Count
        runtime_match_count = @($runtimeMatches).Count
    }

    $summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
    $lines | Set-Content -Path $resultPath -Encoding UTF8
}
'@

Set-Content -Path $hostScriptPath -Value $guestScript -Encoding UTF8

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 600)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw 'Guest did not reach a ready VMware Tools state in time.'
}

function Get-ShellHealthObject {
    return (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript | ConvertFrom-Json)
}

function Log-Incident {
    param([string]$Symptom)

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $incidentScript `
        -RecordId $recordId `
        -TweakId $recordId `
        -TestId ("{0}-{1}" -f $probeName, $stamp) `
        -Family 'Reliability timestamping' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Reliability' `
        -ValueName 'TimeStampEnabled + TimeStampInterval' `
        -ValueState 'capture' `
        -Symptom $Symptom `
        -ShellRecovered:$false `
        -NeededSnapshotRevert:$false `
        -IncidentPath $IncidentLogPath | Out-Null
}

Wait-GuestReady
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CreateDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScriptPath, $guestScriptPath) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NonInteractive',
    '-NoProfile',
    '-WindowStyle',
    'Hidden',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $guestScriptPath,
    '-ProbeName',
    $probeName,
    '-OutputDirectory',
    $guestRoot
) | Out-Null

$copyPairs = @(
    @{ Guest = (Join-Path $guestRoot "$probeName.txt"); Host = (Join-Path $hostRoot "$probeName.txt") },
    @{ Guest = (Join-Path $guestRoot "$probeName.json"); Host = (Join-Path $hostRoot "$probeName.json") },
    @{ Guest = (Join-Path $guestRoot "$probeName.csv"); Host = (Join-Path $hostRoot "$probeName.csv") },
    @{ Guest = (Join-Path $guestRoot "$probeName.hits.csv"); Host = (Join-Path $hostRoot "$probeName.hits.csv") },
    @{ Guest = (Join-Path $guestRoot "$probeName.runtime.hits.csv"); Host = (Join-Path $hostRoot "$probeName.runtime.hits.csv") },
    @{ Guest = (Join-Path $guestRoot "$probeName.pml"); Host = (Join-Path $hostRoot "$probeName.pml") }
)

foreach ($pair in $copyPairs) {
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) | Out-Null
    }
    catch {
    }
}

$shellHealthPath = Join-Path $hostRoot 'shell-health.json'
$shellHealth = Get-ShellHealthObject
$shellHealth | ConvertTo-Json -Depth 5 | Set-Content -Path $shellHealthPath -Encoding UTF8

if (-not $shellHealth.shell_healthy) {
    Log-Incident -Symptom 'Reliability timestamp Procmon probe left the shell unhealthy.'
}

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe = $probeName
    record_id = $recordId
    host_output_root = $hostRoot
    txt = if (Test-Path (Join-Path $hostRoot "$probeName.txt")) { Join-Path $hostRoot "$probeName.txt" } else { $null }
    json = if (Test-Path (Join-Path $hostRoot "$probeName.json")) { Join-Path $hostRoot "$probeName.json" } else { $null }
    csv = if (Test-Path (Join-Path $hostRoot "$probeName.csv")) { Join-Path $hostRoot "$probeName.csv" } else { $null }
    hits_csv = if (Test-Path (Join-Path $hostRoot "$probeName.hits.csv")) { Join-Path $hostRoot "$probeName.hits.csv" } else { $null }
    runtime_hits_csv = if (Test-Path (Join-Path $hostRoot "$probeName.runtime.hits.csv")) { Join-Path $hostRoot "$probeName.runtime.hits.csv" } else { $null }
    pml = if (Test-Path (Join-Path $hostRoot "$probeName.pml")) { Join-Path $hostRoot "$probeName.pml" } else { $null }
    shell_health = $shellHealthPath
} | ConvertTo-Json -Depth 5 | Set-Content -Path $hostSummaryPath -Encoding UTF8

Write-Output $hostSummaryPath

