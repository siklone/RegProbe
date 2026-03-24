[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('baseline', 'reporting', 'securitycenter')]
    [string]$Mode = 'baseline',

    [ValidateSet(0, 1)]
    [int]$State = 1,

    [string]$OutputDirectory = 'C:\Tools\Perf\Procmon',
    [string]$LaunchUri = 'windowsdefender:',

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$IgnoredArguments
)

$ErrorActionPreference = 'Stop'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
$reportingPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Reporting'
$securityCenterPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications'
$valueName = 'DisableEnhancedNotifications'
$prefix = "defender-disable-enhanced-$Mode-$State"
$pml = Join-Path $OutputDirectory "$prefix.pml"
$csv = Join-Path $OutputDirectory "$prefix.csv"
$result = Join-Path $OutputDirectory "$prefix.txt"

function Get-ValueState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RegistryPath,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    try {
        $item = Get-ItemProperty -Path $RegistryPath -Name $Name -ErrorAction Stop
        return [ordered]@{
            path_exists = $true
            value_exists = $true
            value = [int]$item.$Name
        }
    }
    catch {
        $pathExists = Test-Path $RegistryPath
        return [ordered]@{
            path_exists = $pathExists
            value_exists = $false
            value = $null
        }
    }
}

function Restore-ValueState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RegistryPath,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    if (-not $State.path_exists) {
        if (Test-Path $RegistryPath) {
            Remove-Item -Path $RegistryPath -Recurse -Force -ErrorAction SilentlyContinue
        }
        return
    }

    New-Item -Path $RegistryPath -Force | Out-Null

    if ($State.value_exists) {
        New-ItemProperty -Path $RegistryPath -Name $Name -PropertyType DWord -Value $State.value -Force | Out-Null
    }
    else {
        Remove-ItemProperty -Path $RegistryPath -Name $Name -ErrorAction SilentlyContinue
    }
}

$lines = New-Object System.Collections.Generic.List[string]
$reportingOriginal = $null
$securityCenterOriginal = $null

try {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

    $reportingOriginal = Get-ValueState -RegistryPath $reportingPath -Name $valueName
    $securityCenterOriginal = Get-ValueState -RegistryPath $securityCenterPath -Name $valueName

    foreach ($path in @($pml, $csv, $result)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force
        }
    }

    switch ($Mode) {
        'baseline' {
            Remove-ItemProperty -Path $reportingPath -Name $valueName -ErrorAction SilentlyContinue
            Remove-ItemProperty -Path $securityCenterPath -Name $valueName -ErrorAction SilentlyContinue
        }
        'reporting' {
            New-Item -Path $reportingPath -Force | Out-Null
            New-ItemProperty -Path $reportingPath -Name $valueName -PropertyType DWord -Value $State -Force | Out-Null
            Remove-ItemProperty -Path $securityCenterPath -Name $valueName -ErrorAction SilentlyContinue
        }
        'securitycenter' {
            New-Item -Path $securityCenterPath -Force | Out-Null
            New-ItemProperty -Path $securityCenterPath -Name $valueName -PropertyType DWord -Value $State -Force | Out-Null
            Remove-ItemProperty -Path $reportingPath -Name $valueName -ErrorAction SilentlyContinue
        }
    }

    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) | Out-Null
    Start-Sleep -Seconds 4

    Start-Process -FilePath $LaunchUri | Out-Null
    Start-Sleep -Seconds 12

    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    & $procmon /AcceptEula /OpenLog $pml /SaveAs $csv /Quiet | Out-Null

    $matches = @()
    if (Test-Path $csv) {
        $rows = Import-Csv $csv
        $matches = $rows | Where-Object {
            $_.Path -like '*DisableEnhancedNotifications*' -or
            $_.Path -like '*\Windows Defender\Reporting*' -or
            $_.Path -like '*\Windows Defender Security Center\Notifications*'
        } | Select-Object -First 50
    }

    $lines.Add("MODE=$Mode")
    $lines.Add("STATE=$State")
    $lines.Add('PML_EXISTS=' + (Test-Path $pml))
    $lines.Add('CSV_EXISTS=' + (Test-Path $csv))
    $lines.Add('MATCH_COUNT=' + @($matches).Count)
    foreach ($match in $matches) {
        $lines.Add(('{0} | {1} | {2} | {3} | {4} | {5}' -f $match.'Time of Day', $match.'Process Name', $match.Operation, $match.Path, $match.Result, $match.Detail))
    }

    $lines.Add('ORIGINAL_REPORTING=' + ($reportingOriginal | ConvertTo-Json -Compress))
    $lines.Add('ORIGINAL_SECURITYCENTER=' + ($securityCenterOriginal | ConvertTo-Json -Compress))
}
catch {
    $lines.Add('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message)
    if ($_.InvocationInfo) {
        $lines.Add('AT=' + $_.InvocationInfo.PositionMessage)
    }
}
finally {
    if ($reportingOriginal) {
        Restore-ValueState -RegistryPath $reportingPath -Name $valueName -State $reportingOriginal
    }
    if ($securityCenterOriginal) {
        Restore-ValueState -RegistryPath $securityCenterPath -Name $valueName -State $securityCenterOriginal
    }

    try {
        $reportingAfter = Get-ValueState -RegistryPath $reportingPath -Name $valueName
        $securityCenterAfter = Get-ValueState -RegistryPath $securityCenterPath -Name $valueName
        $lines.Add('RESTORED_REPORTING=' + ($reportingAfter | ConvertTo-Json -Compress))
        $lines.Add('RESTORED_SECURITYCENTER=' + ($securityCenterAfter | ConvertTo-Json -Compress))
    }
    catch {
    }

    $lines | Set-Content -Path $result -Encoding UTF8
}
