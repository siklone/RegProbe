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

$effectivePrefix = New-ProbePrefix -ConfiguredPrefix $Prefix -Path $RegistryPath -Name $ValueName
$pml = Join-Path $OutputDirectory "$effectivePrefix.pml"
$csv = Join-Path $OutputDirectory "$effectivePrefix.csv"
$hitsCsv = Join-Path $OutputDirectory "$effectivePrefix.hits.csv"
$result = Join-Path $OutputDirectory "$effectivePrefix.txt"
$normalizedProcmonPath = Normalize-RegistryPathForProcmon -Path $RegistryPath
$original = $null
$lines = New-Object System.Collections.Generic.List[string]

try {
    if ([string]::IsNullOrWhiteSpace($PowerShellCommand)) {
        throw 'PowerShellCommand is required.'
    }

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $original = Get-ValueState -Path $RegistryPath -Name $ValueName

    foreach ($path in @($pml, $csv, $hitsCsv, $result)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
        }
    }

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) | Out-Null
    Start-Sleep -Seconds 4

    & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -Command $PowerShellCommand | Out-Null
    Start-Sleep -Seconds 5

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    & $procmon /AcceptEula /OpenLog $pml /SaveAs $csv /Quiet | Out-Null

    $matches = @()
    if (Test-Path $csv) {
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
    }

    $lines.Add("MODE=$Mode")
    $lines.Add("REGISTRY_PATH=$RegistryPath")
    $lines.Add("VALUE_NAME=$ValueName")
    $lines.Add("PML_EXISTS=" + (Test-Path $pml))
    $lines.Add("CSV_EXISTS=" + (Test-Path $csv))
    $lines.Add("HITSCSV_EXISTS=" + (Test-Path $hitsCsv))
    $lines.Add("MATCH_COUNT=" + @($matches).Count)
    foreach ($match in $matches) {
        $lines.Add(('{0} | {1} | {2} | {3} | {4} | {5}' -f $match.'Time of Day', $match.'Process Name', $match.Operation, $match.Path, $match.Result, $match.Detail))
    }
    $lines.Add('ORIGINAL=' + ($original | ConvertTo-Json -Compress))
}
catch {
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
