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
$probeName = 'fullscreen-optimizations-probe'
$recordId = 'system.disable-fullscreen-optimizations'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $probeName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $probeName, $stamp)
$hostScriptPath = Join-Path $hostRoot 'fullscreen-optimizations-probe-inner.ps1'
$guestScriptPath = Join-Path $GuestScriptRoot 'fullscreen-optimizations-probe-inner.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$shellHealthScript = Join-Path $PSScriptRoot 'get-vm-shell-health.ps1'
$incidentScript = Join-Path $PSScriptRoot 'log-vm-incident.ps1'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$guestScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [string]$ProbeName = 'fullscreen-optimizations-probe'
)

$ErrorActionPreference = 'Stop'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
$registryPath = 'HKCU:\System\GameConfigStore'
$targets = @(
    @{ name = 'GameDVR_FSEBehavior'; type = 'DWord'; value = 2 },
    @{ name = 'GameDVR_FSEBehaviorMode'; type = 'DWord'; value = 2 },
    @{ name = 'GameDVR_HonorUserFSEBehaviorMode'; type = 'DWord'; value = 1 },
    @{ name = 'GameDVR_DXGIHonorFSEWindowsCompatible'; type = 'DWord'; value = 1 }
)
$valueNames = @($targets | ForEach-Object { $_.name })

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
$memberHitsCsv = Join-Path $OutputDirectory "$ProbeName.member-hits.csv"
$resultPath = Join-Path $OutputDirectory "$ProbeName.txt"
$summaryPath = Join-Path $OutputDirectory "$ProbeName.json"
$lines = New-Object System.Collections.Generic.List[string]
$originalStates = @()
$matches = @()
$runtimeMatches = @()
$memberHits = @()
$valueHitCounts = [ordered]@{}

foreach ($valueName in $valueNames) {
    $valueHitCounts[$valueName] = 0
}

try {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    foreach ($path in @($pml, $csv, $hitsCsv, $runtimeHitsCsv, $memberHitsCsv, $resultPath, $summaryPath)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
        }
    }

    foreach ($target in $targets) {
        $originalStates += ,(Get-ValueState -Path $registryPath -Name $target.name)
    }

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) | Out-Null
    Start-Sleep -Seconds 4

    if (-not (Test-Path $registryPath)) {
        New-Item -Path $registryPath -Force | Out-Null
    }

    foreach ($target in $targets) {
        New-ItemProperty -Path $registryPath -Name $target.name -PropertyType $target.type -Value $target.value -Force | Out-Null
    }

    Invoke-BestEffort -FilePath 'C:\Windows\System32\cmd.exe' -ArgumentList @('/c', 'start', '', 'ms-settings:gaming') -SleepSeconds 6
    Invoke-BestEffort -FilePath 'C:\Windows\System32\cmd.exe' -ArgumentList @('/c', 'start', '', 'ms-settings:gaming-gamemode') -SleepSeconds 6
    Invoke-BestEffort -FilePath 'C:\Windows\System32\cmd.exe' -ArgumentList @('/c', 'start', '', 'ms-settings:display-advancedgraphics') -SleepSeconds 8
    Invoke-BestEffortPowerShell -Command "$pkg = Get-AppxPackage Microsoft.XboxGamingOverlay -ErrorAction SilentlyContinue; if ($pkg) { Start-Process 'shell:AppsFolder\Microsoft.XboxGamingOverlay_8wekyb3d8bbwe!App' -WindowStyle Hidden }"
    Start-Sleep -Seconds 8

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    & $procmon /AcceptEula /OpenLog $pml /SaveAs $csv /Quiet | Out-Null

    if (Test-Path $csv) {
        $normalizedPath = Normalize-RegistryPathForProcmon -Path $registryPath
        $rows = Import-Csv $csv
        $matches = $rows | Where-Object {
            $_.Operation -like 'Reg*' -and $_.Path -like "*$normalizedPath*"
        }

        if (@($matches).Count -gt 0) {
            $matches | Export-Csv -Path $hitsCsv -NoTypeInformation -Encoding UTF8
        }

        $runtimeMatches = $matches | Where-Object {
            $_.'Process Name' -notin @('powershell.exe', 'reg.exe', 'Procmon64.exe', 'cmd.exe')
        }

        if (@($runtimeMatches).Count -gt 0) {
            $runtimeMatches | Export-Csv -Path $runtimeHitsCsv -NoTypeInformation -Encoding UTF8
        }

        $memberHits = foreach ($valueName in $valueNames) {
            $runtimeMatches | Where-Object { $_.Path -like "*\\$valueName" }
        }

        if (@($memberHits).Count -gt 0) {
            $memberHits | Export-Csv -Path $memberHitsCsv -NoTypeInformation -Encoding UTF8
        }

        foreach ($valueName in $valueNames) {
            $valueHitCounts[$valueName] = @($runtimeMatches | Where-Object { $_.Path -like "*\\$valueName" }).Count
        }
    }

    $lines.Add('REGISTRY_PATH=' + $registryPath)
    $lines.Add('MATCH_COUNT=' + @($matches).Count)
    $lines.Add('RUNTIME_MATCH_COUNT=' + @($runtimeMatches).Count)
    foreach ($valueName in $valueNames) {
        $lines.Add(('VALUE_HIT_{0}={1}' -f $valueName, $valueHitCounts[$valueName]))
    }
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
        $restoredStates += ,(Get-ValueState -Path $registryPath -Name $target.name)
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        registry_path = $registryPath
        original = $originalStates
        applied = $targets
        restored = $restoredStates
        pml_exists = [bool](Test-Path $pml)
        csv_exists = [bool](Test-Path $csv)
        hits_csv_exists = [bool](Test-Path $hitsCsv)
        runtime_hits_csv_exists = [bool](Test-Path $runtimeHitsCsv)
        member_hits_csv_exists = [bool](Test-Path $memberHitsCsv)
        match_count = @($matches).Count
        runtime_match_count = @($runtimeMatches).Count
        value_hit_counts = $valueHitCounts
    }

    $lines.Add('ORIGINAL=' + ($originalStates | ConvertTo-Json -Compress))
    $lines.Add('APPLIED=' + ($targets | ConvertTo-Json -Compress))
    $lines.Add('RESTORED=' + ($restoredStates | ConvertTo-Json -Compress))

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
        -Family 'Fullscreen optimizations' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKCU\System\GameConfigStore' `
        -ValueName 'GameDVR_FSEBehavior + GameDVR_FSEBehaviorMode + GameDVR_HonorUserFSEBehaviorMode + GameDVR_DXGIHonorFSEWindowsCompatible' `
        -ValueState '2/2/1/1' `
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
    @{ Guest = (Join-Path $guestRoot "$probeName.member-hits.csv"); Host = (Join-Path $hostRoot "$probeName.member-hits.csv") },
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
    Log-Incident -Symptom 'Fullscreen optimizations Procmon probe left the shell unhealthy.'
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
    member_hits_csv = if (Test-Path (Join-Path $hostRoot "$probeName.member-hits.csv")) { Join-Path $hostRoot "$probeName.member-hits.csv" } else { $null }
    pml = if (Test-Path (Join-Path $hostRoot "$probeName.pml")) { Join-Path $hostRoot "$probeName.pml" } else { $null }
    shell_health = $shellHealthPath
} | ConvertTo-Json -Depth 5 | Set-Content -Path $hostSummaryPath -Encoding UTF8

Write-Output $hostSummaryPath

