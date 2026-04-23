[CmdletBinding()]
param(
    [ValidateSet('prepare', 'post')]
    [string]$Stage = 'prepare',
    [string]$OutputName = 'dpc-watchdog-profile-cluster-reboot-kvm-20260412a',
    [string]$UploadBaseUrl = $(if ($env:REGPROBE_VM_BRIDGE_BASE_URL) { $env:REGPROBE_VM_BRIDGE_BASE_URL } else { 'http://10.0.2.2:8766' })
)

$ErrorActionPreference = 'Stop'

$registryPath = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel'
$providerPath = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel'
$valueNames = @(
    'DpcWatchdogProfileBufferSizeBytes',
    'DpcWatchdogProfileCumulativeDpcThreshold',
    'DpcWatchdogProfileOffset',
    'DpcWatchdogProfileSingleDpcThreshold'
)

$outputRoot = Join-Path 'C:\RegProbe-Diag\reboot-observation' $OutputName
$statePath = Join-Path $outputRoot 'state.json'
$beforePath = Join-Path $outputRoot 'before.json'
$afterPath = Join-Path $outputRoot 'after.json'
$summaryPath = Join-Path $outputRoot 'summary.json'
$eventPath = Join-Path $outputRoot 'system-events.txt'

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )

    $Payload | ConvertTo-Json -Depth 12 | Set-Content -Path $Path -Encoding UTF8
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

function Get-ClusterSnapshot {
    $snapshot = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        registry_path = $registryPath
        provider_path = $providerPath
        key_exists = $false
        boot_time_utc = Get-BootTimeUtc
        values = [ordered]@{}
        errors = @()
    }

    try {
        $item = Get-Item -Path $providerPath -ErrorAction Stop
        $snapshot.key_exists = $true
        foreach ($name in $valueNames) {
            $entry = [ordered]@{
                value_name = $name
                value_exists = $false
                value_kind = $null
                value = $null
                error = $null
            }
            try {
                $entry.value = $item.GetValue($name, $null, 'DoNotExpandEnvironmentNames')
                $entry.value_kind = [string]$item.GetValueKind($name)
                $entry.value_exists = $true
            }
            catch {
                $entry.error = $_.Exception.Message
            }
            $snapshot.values[$name] = $entry
        }
    }
    catch {
        $snapshot.errors += $_.Exception.Message
    }

    return $snapshot
}

function Invoke-ArtifactUpload {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path $Path -PathType Leaf)) {
        return $null
    }

    $targetUri = '{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName
    Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
    return [ordered]@{
        path = $Path
        uri = $targetUri
    }
}

function Export-RecentSystemEvents {
    param([string]$Path)

    try {
        $events = wevtutil qe System /c:40 /rd:true /f:text 2>&1
        Set-Content -Path $Path -Value ($events -join [Environment]::NewLine) -Encoding UTF8
    }
    catch {
        Set-Content -Path $Path -Value $_.Exception.Message -Encoding UTF8
    }
}

function Test-ValuePreserved {
    param(
        [object]$Before,
        [object]$After
    )

    if ([bool]$Before.value_exists -ne [bool]$After.value_exists) {
        return $false
    }
    if (-not [bool]$Before.value_exists) {
        return $true
    }
    return (
        ([string]$Before.value_kind -eq [string]$After.value_kind) -and
        (($Before.value | ConvertTo-Json -Compress) -eq ($After.value | ConvertTo-Json -Compress))
    )
}

if ($Stage -eq 'prepare') {
    $before = Get-ClusterSnapshot
    Write-JsonFile -Path $beforePath -Payload $before
    $state = [ordered]@{
        output_name = $OutputName
        output_root = $outputRoot
        registry_path = $registryPath
        value_names = $valueNames
        upload_base_url = $UploadBaseUrl
        before_path = $beforePath
        state_path = $statePath
        prepared_utc = [DateTime]::UtcNow.ToString('o')
    }
    Write-JsonFile -Path $statePath -Payload $state
    Invoke-ArtifactUpload -Path $beforePath -RemoteName ($OutputName + '-before.json') | Out-Null
    Write-Output $statePath
    return
}

if (-not (Test-Path $statePath -PathType Leaf)) {
    throw "State file missing: $statePath"
}

$state = Get-Content -Path $statePath -Raw | ConvertFrom-Json
$before = Get-Content -Path ([string]$state.before_path) -Raw | ConvertFrom-Json
$after = Get-ClusterSnapshot
Write-JsonFile -Path $afterPath -Payload $after
Export-RecentSystemEvents -Path $eventPath

$preservation = [ordered]@{}
foreach ($name in $valueNames) {
    $preservation[$name] = Test-ValuePreserved -Before $before.values.$name -After $after.values.$name
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    output_name = $OutputName
    registry_path = $registryPath
    value_names = $valueNames
    before = $before
    after = $after
    reboot_observed = (
        $before.boot_time_utc -and
        $after.boot_time_utc -and
        ([string]$before.boot_time_utc -ne [string]$after.boot_time_utc)
    )
    key_preserved = ([bool]$before.key_exists -eq [bool]$after.key_exists)
    value_preservation = $preservation
    all_values_preserved = -not (@($preservation.Values | Where-Object { -not $_ }).Count -gt 0)
    event_path = $eventPath
    uploads = [ordered]@{}
    errors = @()
}

Write-JsonFile -Path $summaryPath -Payload $summary

foreach ($artifact in @(
    @{ key = 'before'; path = $beforePath; name = ($OutputName + '-before.json') },
    @{ key = 'after'; path = $afterPath; name = ($OutputName + '-after.json') },
    @{ key = 'summary'; path = $summaryPath; name = ($OutputName + '-summary.json') },
    @{ key = 'events'; path = $eventPath; name = ($OutputName + '-system-events.txt') }
)) {
    try {
        $upload = Invoke-ArtifactUpload -Path $artifact.path -RemoteName $artifact.name
        if ($upload) {
            $summary.uploads[$artifact.key] = $upload
        }
    }
    catch {
        $summary.errors += $_.Exception.Message
    }
}

Write-JsonFile -Path $summaryPath -Payload $summary
Write-Output $summaryPath
