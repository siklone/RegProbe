[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [ValidateSet('custom', 'uuid-rpc-com-burst', 'uac-policy-surface-burst', 'session-manager-io-raw-burst', 'executive-worker-burst', 'hiber-file-size-burst', 'watchdog-power-burst')]
    [string]$TriggerProfile = 'custom',

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
        @{ key = 'csv'; path = $csvPath; name = ('{0}.csv' -f $OutputName) }
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
        probe_stage_exists = [bool](Test-Path $probeStagePath)
        probe_stage = if ($probeStage) { $probeStage.stage } else { $null }
        probe_stage_status = if ($probeStage) { $probeStage.status } else { $null }
        probe_stage_message = if ($probeStage) { $probeStage.message } else { $null }
        result_error_line = $resultErrorLine
        error_kind = $errorKind
        error = $errorMessage
        error_position = $errorPosition
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
