[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(0, 1)]
    [int]$State,

    [string]$OutputDirectory = 'C:\Tools\Perf\Procmon',
    [string]$SettingsUri = 'ms-settings:gaming-gamemode',

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$IgnoredArguments
)

$ErrorActionPreference = 'Stop'

$procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
$registryPath = 'HKCU:\Software\Microsoft\GameBar'
$valueName = 'AutoGameModeEnabled'
$prefix = "gamemode-state-$State"
$pml = Join-Path $OutputDirectory "$prefix.pml"
$csv = Join-Path $OutputDirectory "$prefix.csv"
$result = Join-Path $OutputDirectory "$prefix.txt"

$lines = New-Object System.Collections.Generic.List[string]
$hadValue = $false
$original = $null

try {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

    try {
        $item = Get-ItemProperty -Path $registryPath -Name $valueName -ErrorAction Stop
        $hadValue = $true
        $original = [int]$item.$valueName
    } catch {
        $hadValue = $false
    }

    foreach ($path in @($pml, $csv, $result)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force
        }
    }

    New-Item -Path $registryPath -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name $valueName -PropertyType DWord -Value $State -Force | Out-Null

    Start-Process -FilePath $procmon -ArgumentList @('/AcceptEula', '/Quiet', '/Minimized', '/BackingFile', $pml) | Out-Null
    Start-Sleep -Seconds 4
    Start-Process -FilePath $SettingsUri | Out-Null
    Start-Sleep -Seconds 10
    & $procmon -Terminate -Quiet | Out-Null
    Start-Sleep -Seconds 2
    & $procmon /AcceptEula /OpenLog $pml /SaveAs $csv /Quiet | Out-Null

    $matches = @()
    if (Test-Path $csv) {
        $rows = Import-Csv $csv
        $matches = $rows | Where-Object {
            $_.'Process Name' -eq 'SystemSettings.exe' -and
            $_.Operation -eq 'RegQueryValue' -and
            $_.Path -like '*\Software\Microsoft\GameBar\AutoGameModeEnabled'
        } | Select-Object -First 20
    }

    $lines.Add("STATE=$State")
    $lines.Add('PML_EXISTS=' + (Test-Path $pml))
    $lines.Add('CSV_EXISTS=' + (Test-Path $csv))
    $lines.Add('MATCHES=' + ($(if ($matches.Count -gt 0) { 'YES' } else { 'NO' })))
    foreach ($match in $matches) {
        $lines.Add(('{0} | {1} | {2} | {3} | {4} | {5}' -f $match.'Time of Day', $match.'Process Name', $match.Operation, $match.Path, $match.Result, $match.Detail))
    }

    if ($hadValue) {
        Set-ItemProperty -Path $registryPath -Name $valueName -Value $original
        $lines.Add('RESTORED=' + $original)
    } else {
        Remove-ItemProperty -Path $registryPath -Name $valueName -ErrorAction SilentlyContinue
        $lines.Add('RESTORED=ABSENT')
    }

    $lines.Add('HAD_VALUE=' + $hadValue)
    $lines.Add('ORIGINAL=' + ($original -as [string]))
}
catch {
    $lines.Add('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message)
    if ($_.InvocationInfo) {
        $lines.Add('AT=' + $_.InvocationInfo.PositionMessage)
    }
}
finally {
    $lines | Set-Content -Path $result -Encoding UTF8
}
