[CmdletBinding()]
param(
    [ValidateSet('prepare-reboot', 'post-reboot')]
    [string]$Stage = 'prepare-reboot',

    [string]$RegistryPath = '',
    [string]$ValueName = '',
    [string]$OutputName = 'reboot-observation',
    [string]$OutputRoot = '',
    [string]$UploadBaseUrl = '',
    [string]$StateFile = '',
    [string]$TaskName = '',
    [int]$PostRebootDelaySeconds = 20,
    [int]$UploadRetryCount = 40,
    [int]$UploadRetryDelaySeconds = 5,
    [switch]$SkipTaskRegistration,
    [switch]$SkipGuestRestart
)

$ErrorActionPreference = 'Stop'

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

function Convert-ToProviderPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $trimmed = $Path.Trim().Replace('HKLM:\', 'HKLM\').Replace('HKCU:\', 'HKCU\').Replace('HKCR:\', 'HKCR\').Replace('HKU:\', 'HKU\').Replace('HKCC:\', 'HKCC\')
    $map = [ordered]@{
        'HKLM\' = 'Registry::HKEY_LOCAL_MACHINE\'
        'HKEY_LOCAL_MACHINE\' = 'Registry::HKEY_LOCAL_MACHINE\'
        'HKCU\' = 'Registry::HKEY_CURRENT_USER\'
        'HKEY_CURRENT_USER\' = 'Registry::HKEY_CURRENT_USER\'
        'HKCR\' = 'Registry::HKEY_CLASSES_ROOT\'
        'HKEY_CLASSES_ROOT\' = 'Registry::HKEY_CLASSES_ROOT\'
        'HKU\' = 'Registry::HKEY_USERS\'
        'HKEY_USERS\' = 'Registry::HKEY_USERS\'
        'HKCC\' = 'Registry::HKEY_CURRENT_CONFIG\'
        'HKEY_CURRENT_CONFIG\' = 'Registry::HKEY_CURRENT_CONFIG\'
    }

    foreach ($prefix in $map.Keys) {
        if ($trimmed.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $map[$prefix] + $trimmed.Substring($prefix.Length)
        }
    }

    if ($trimmed.StartsWith('Registry::', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $trimmed
    }

    throw "Unsupported registry root in path: $Path"
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

function Get-RegistrySnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $providerPath = Convert-ToProviderPath -Path $Path
    $snapshot = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        registry_path = $Path
        provider_path = $providerPath
        value_name = $Name
        key_exists = $false
        value_exists = $false
        value_kind = $null
        value = $null
        error = $null
        boot_time_utc = Get-BootTimeUtc
    }

    try {
        $item = Get-Item -Path $providerPath -ErrorAction Stop
        $snapshot.key_exists = $true
        try {
            $snapshot.value = $item.GetValue($Name, $null, 'DoNotExpandEnvironmentNames')
            $snapshot.value_kind = [string]$item.GetValueKind($Name)
            $snapshot.value_exists = $true
        }
        catch {
            $snapshot.error = $_.Exception.Message
        }
    }
    catch {
        $snapshot.error = $_.Exception.Message
    }

    return $snapshot
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

function Capture-PowercfgA {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    try {
        $output = cmd /c 'powercfg /a' 2>&1
        Set-Content -Path $Path -Value ($output -join [Environment]::NewLine) -Encoding UTF8
    }
    catch {
        Set-Content -Path $Path -Value $_.Exception.Message -Encoding UTF8
    }
}

function Load-StatePayload {
    param([Parameter(Mandatory = $true)][string]$Path)

    return Get-Content -Path $Path -Raw | ConvertFrom-Json
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\reboot-observation' $OutputName
}

Ensure-Directory -Path $OutputRoot

if ([string]::IsNullOrWhiteSpace($StateFile)) {
    $StateFile = Join-Path $OutputRoot 'state.json'
}

if ([string]::IsNullOrWhiteSpace($TaskName)) {
    $TaskName = 'RegProbe-RebootObservation-' + ($OutputName -replace '[^A-Za-z0-9_.-]', '-')
}

$beforePath = Join-Path $OutputRoot 'before.json'
$afterPath = Join-Path $OutputRoot 'after.json'
$beforePowercfgPath = Join-Path $OutputRoot 'powercfg-a-before.txt'
$afterPowercfgPath = Join-Path $OutputRoot 'powercfg-a-after.txt'
$summaryPath = Join-Path $OutputRoot 'summary.json'

if ($Stage -eq 'prepare-reboot') {
    if ([string]::IsNullOrWhiteSpace($RegistryPath)) {
        throw 'RegistryPath is required for prepare-reboot.'
    }

    $before = Get-RegistrySnapshot -Path $RegistryPath -Name $ValueName
    Write-JsonFile -Path $beforePath -Payload $before
    Capture-PowercfgA -Path $beforePowercfgPath

    $state = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        registry_path = $RegistryPath
        value_name = $ValueName
        output_name = $OutputName
        output_root = $OutputRoot
        upload_base_url = $UploadBaseUrl
        task_name = $TaskName
        state_file = $StateFile
        before_path = $beforePath
        before_powercfg_path = $beforePowercfgPath
        post_reboot_delay_seconds = $PostRebootDelaySeconds
        upload_retry_count = $UploadRetryCount
        upload_retry_delay_seconds = $UploadRetryDelaySeconds
        script_path = $PSCommandPath
    }
    Write-JsonFile -Path $StateFile -Payload $state

    foreach ($entry in @(
        @{ key = 'before'; path = $beforePath; name = ('{0}-before.json' -f $OutputName) },
        @{ key = 'powercfg_before'; path = $beforePowercfgPath; name = ('{0}-powercfg-a-before.txt' -f $OutputName) }
    )) {
        try {
            Invoke-ArtifactUpload -Path $entry.path -RemoteName $entry.name | Out-Null
        }
        catch {
        }
    }

    if (-not $SkipTaskRegistration) {
        try {
            Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
        }
        catch {
        }

        $scriptPath = $PSCommandPath.Replace("'", "''")
        $escapedStateFile = $StateFile.Replace("'", "''")
        $arguments = "-NoProfile -ExecutionPolicy Bypass -File '$scriptPath' -Stage post-reboot -StateFile '$escapedStateFile'"
        $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $arguments
        $trigger = New-ScheduledTaskTrigger -AtStartup
        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 15)
        $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -RunLevel Highest
        $task = New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Principal $principal
        Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null
    }

    if (-not $SkipGuestRestart) {
        shutdown.exe /r /t 0 /f | Out-Null
    }

    Write-Output $StateFile
    return
}

$state = Load-StatePayload -Path $StateFile
$RegistryPath = [string]$state.registry_path
$ValueName = [string]$state.value_name
$OutputName = [string]$state.output_name
$OutputRoot = [string]$state.output_root
$UploadBaseUrl = [string]$state.upload_base_url
$TaskName = [string]$state.task_name
$UploadRetryCount = [int]$state.upload_retry_count
$UploadRetryDelaySeconds = [int]$state.upload_retry_delay_seconds
$beforePath = [string]$state.before_path
$beforePowercfgPath = [string]$state.before_powercfg_path
$summaryPath = Join-Path $OutputRoot 'summary.json'
$afterPath = Join-Path $OutputRoot 'after.json'
$afterPowercfgPath = Join-Path $OutputRoot 'powercfg-a-after.txt'

Start-Sleep -Seconds ([Math]::Max([int]$state.post_reboot_delay_seconds, 1))

$before = $null
if (Test-Path -Path $beforePath -PathType Leaf) {
    $before = Get-Content -Path $beforePath -Raw | ConvertFrom-Json
}

$after = Get-RegistrySnapshot -Path $RegistryPath -Name $ValueName
Write-JsonFile -Path $afterPath -Payload $after
Capture-PowercfgA -Path $afterPowercfgPath

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = 'ok'
    error = $null
    registry_path = $RegistryPath
    value_name = $ValueName
    output_name = $OutputName
    output_root = $OutputRoot
    task_name = $TaskName
    before = $before
    after = $after
    before_powercfg_path = $beforePowercfgPath
    after_powercfg_path = $afterPowercfgPath
    reboot_observed = $false
    value_changed = $false
    value_preserved = $false
    uploads = [ordered]@{}
    errors = @()
}

if ($before -and $before.boot_time_utc -and $after.boot_time_utc) {
    $summary.reboot_observed = ($before.boot_time_utc -ne $after.boot_time_utc)
}

if ($before) {
    $summary.value_changed = (
        [bool]$before.key_exists -ne [bool]$after.key_exists -or
        [bool]$before.value_exists -ne [bool]$after.value_exists -or
        [string]$before.value_kind -ne [string]$after.value_kind -or
        (($before.value | ConvertTo-Json -Compress) -ne ($after.value | ConvertTo-Json -Compress))
    )
    $summary.value_preserved = -not $summary.value_changed
}

if (-not $summary.reboot_observed) {
    $summary.status = 'error'
    $summary.error = 'Boot time did not change across the reboot observation.'
}

foreach ($entry in @(
    @{ key = 'before'; path = $beforePath; name = ('{0}-before.json' -f $OutputName) },
    @{ key = 'after'; path = $afterPath; name = ('{0}-after.json' -f $OutputName) },
    @{ key = 'powercfg_before'; path = $beforePowercfgPath; name = ('{0}-powercfg-a-before.txt' -f $OutputName) },
    @{ key = 'powercfg_after'; path = $afterPowercfgPath; name = ('{0}-powercfg-a-after.txt' -f $OutputName) }
)) {
    try {
        $upload = Invoke-ArtifactUpload -Path $entry.path -RemoteName $entry.name
        if ($upload) {
            $summary.uploads[$entry.key] = $upload
        }
    }
    catch {
        $summary.errors += $_.Exception.Message
    }
}

Write-JsonFile -Path $summaryPath -Payload $summary

try {
    $upload = Invoke-ArtifactUpload -Path $summaryPath -RemoteName ('{0}-summary.json' -f $OutputName)
    if ($upload) {
        $summary.uploads['summary'] = $upload
        Write-JsonFile -Path $summaryPath -Payload $summary
    }
}
catch {
    $summary.errors += $_.Exception.Message
    Write-JsonFile -Path $summaryPath -Payload $summary
}

try {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
}
catch {
}

Write-Output $summaryPath
