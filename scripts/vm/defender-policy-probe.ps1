[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('baseline', 'set')]
    [string]$Mode = 'baseline',

    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [int]$State = 1,

    [string]$Prefix = '',
    [string]$OutputDirectory = 'C:\Tools\Perf\Procmon',
    [string]$LaunchUri = 'windowsdefender:',
    [string]$PowerShellCommand = '',
    [string[]]$MatchFragments = @(),
    [string[]]$ProcessNames = @(),

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$IgnoredArguments
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
        }
    }
    catch {
        return [ordered]@{
            path_exists = [bool](Test-Path $Path)
            value_exists = $false
            value = $null
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

    if (-not $State.path_exists) {
        if (Test-Path $Path) {
            Remove-Item -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
        }
        return
    }

    New-Item -Path $Path -Force | Out-Null

    if ($State.value_exists) {
        $propertyType = if ($State.value -is [int] -or $State.value -is [long]) { 'DWord' } else { 'String' }
        New-ItemProperty -Path $Path -Name $Name -PropertyType $propertyType -Value $State.value -Force | Out-Null
    }
    else {
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
        [string]$Name,
        [string]$ProbeMode,
        [int]$ProbeState
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPrefix)) {
        return $ConfiguredPrefix
    }

    $sanitized = ($Name -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
    return "defender-$sanitized-$ProbeMode-$ProbeState"
}

$effectivePrefix = New-ProbePrefix -ConfiguredPrefix $Prefix -Name $ValueName -ProbeMode $Mode -ProbeState $State
$pml = Join-Path $OutputDirectory "$effectivePrefix.pml"
$csv = Join-Path $OutputDirectory "$effectivePrefix.csv"
$result = Join-Path $OutputDirectory "$effectivePrefix.txt"
$normalizedProcmonPath = Normalize-RegistryPathForProcmon -Path $RegistryPath
$original = $null
$lines = New-Object System.Collections.Generic.List[string]

try {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $original = Get-ValueState -Path $RegistryPath -Name $ValueName

    foreach ($path in @($pml, $csv, $result)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force
        }
    }

    if ($Mode -eq 'baseline') {
        Remove-ItemProperty -Path $RegistryPath -Name $ValueName -ErrorAction SilentlyContinue
    }
    else {
        New-Item -Path $RegistryPath -Force | Out-Null
        New-ItemProperty -Path $RegistryPath -Name $ValueName -PropertyType DWord -Value $State -Force | Out-Null
    }

    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) | Out-Null
    Start-Sleep -Seconds 4

    if (-not [string]::IsNullOrWhiteSpace($PowerShellCommand)) {
        & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -Command $PowerShellCommand | Out-Null
        Start-Sleep -Seconds 4
    }
    else {
        Start-Process -FilePath $LaunchUri | Out-Null
        Start-Sleep -Seconds 12
    }

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
        } | Select-Object -First 80
    }

    $lines.Add("MODE=$Mode")
    $lines.Add("REGISTRY_PATH=$RegistryPath")
    $lines.Add("VALUE_NAME=$ValueName")
    $lines.Add("STATE=$State")
    $lines.Add("PML_EXISTS=" + (Test-Path $pml))
    $lines.Add("CSV_EXISTS=" + (Test-Path $csv))
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
