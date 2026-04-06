[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [ValidateSet('custom', 'uuid-rpc-com-burst')]
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
if (-not (Test-Path $probeScript)) {
    throw "registry-policy-probe.ps1 not found at $probeScript"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\procmon' $OutputName
}

$triggerCommand = Resolve-TriggerCommand -Profile $TriggerProfile -CustomCommand $PowerShellCommand
Ensure-Directory -Path $OutputRoot

$txtPath = Join-Path $OutputRoot ('{0}.txt' -f $OutputName)
$csvPath = Join-Path $OutputRoot ('{0}.csv' -f $OutputName)
$hitsCsvPath = Join-Path $OutputRoot ('{0}.hits.csv' -f $OutputName)
$summaryPath = Join-Path $OutputRoot 'run-summary.json'

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $probeScript `
    -Mode capture `
    -RegistryPath $RegistryPath `
    -ValueName $ValueName `
    -Prefix $OutputName `
    -OutputDirectory $OutputRoot `
    -PowerShellCommand $triggerCommand `
    -MatchFragments $MatchFragments `
    -ProcessNames $ProcessNames

$uploads = [ordered]@{}
foreach ($entry in @(
    @{ key = 'result'; path = $txtPath; name = ('{0}.txt' -f $OutputName) },
    @{ key = 'hits_csv'; path = $hitsCsvPath; name = ('{0}.hits.csv' -f $OutputName) },
    @{ key = 'csv'; path = $csvPath; name = ('{0}.csv' -f $OutputName) }
)) {
    $upload = Invoke-ArtifactUpload -Path $entry.path -RemoteName $entry.name
    if ($upload) {
        $uploads[$entry.key] = $upload
    }
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    registry_path = $RegistryPath
    value_name = $ValueName
    output_name = $OutputName
    trigger_profile = $TriggerProfile
    output_root = $OutputRoot
    result_exists = [bool](Test-Path $txtPath)
    csv_exists = [bool](Test-Path $csvPath)
    hits_csv_exists = [bool](Test-Path $hitsCsvPath)
    uploads = $uploads
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
Invoke-ArtifactUpload -Path $summaryPath -RemoteName ('{0}-summary.json' -f $OutputName) | Out-Null
Write-Output $summaryPath
