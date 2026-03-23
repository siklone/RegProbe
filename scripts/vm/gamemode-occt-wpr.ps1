[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(0, 1)]
    [int]$State,

    [string]$OutputDirectory = 'C:\Tools\Perf\GameModeSuite',
    [int]$WaitSeconds = 20,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$IgnoredArguments
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$registryPath = 'HKCU:\Software\Microsoft\GameBar'
$valueName = 'AutoGameModeEnabled'
$occt = 'C:\Tools\Perf\OCCT.exe'
$wprExe = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'

$prefix = "gamemode-occt-state-$State"
$result = Join-Path $OutputDirectory "$prefix.txt"
$perfCsv = Join-Path $OutputDirectory "$prefix.perf.csv"
$etl = Join-Path $OutputDirectory "$prefix.etl"
$baselineExists = $false
$baselineValue = $null
$restored = $false
$lines = New-Object System.Collections.Generic.List[string]
$counterSamples = New-Object System.Collections.Generic.List[object]

try {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

    try {
        $item = Get-ItemProperty -Path $registryPath -Name $valueName -ErrorAction Stop
        $baselineExists = $true
        $baselineValue = [int]$item.$valueName
    } catch {
        $baselineExists = $false
    }

    foreach ($path in @($result, $perfCsv, $etl)) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Force
        }
    }

    New-Item -Path $registryPath -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name $valueName -PropertyType DWord -Value $State -Force | Out-Null
    $lines.Add("STATE=$State")
    $lines.Add('BASELINE_EXISTS=' + $baselineExists)
    $lines.Add('BASELINE_VALUE=' + ($baselineValue -as [string]))

    Get-Process -Name 'OCCT' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1

    Start-Process -FilePath $occt | Out-Null
    $lines.Add('OCCT_LAUNCHED=1')

    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $window = $null
    for ($i = 0; $i -lt 15 -and -not $window; $i++) {
        Start-Sleep -Seconds 2
        $window = $root.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition
        ) | Where-Object { $_.Current.Name -eq 'OCCT - Stability Testing since 2003' } | Select-Object -First 1
    }

    if (-not $window) {
        throw 'OCCT window not found.'
    }

    $lines.Add('OCCT_WINDOW=FOUND')
    $tabs = $window.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::TabItem))
    )
    if ($tabs.Count -ge 3) {
        try {
            $tabs[2].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            $lines.Add('TAB_SELECT=1')
        } catch {
            $lines.Add('TAB_SELECT=0')
        }
        Start-Sleep -Seconds 1
    }

    if (Test-Path $wprExe) {
        try {
            & $wprExe -cancel 1>$null 2>$null
        } catch {
        }
        & $wprExe -start GeneralProfile -filemode | Out-Null
    } else {
        throw "WPR executable not found at $wprExe"
    }
    $lines.Add('WPR_STARTED=1')
    Start-Sleep -Seconds 3

    $buttons = $window.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button))
    ) | Where-Object { $_.Current.Name -eq 'START' }
    $lines.Add('START_BUTTONS=' + $buttons.Count)
    foreach ($button in ($buttons | Select-Object -First 10)) {
        $lines.Add(('BUTTON=START|X={0}|Y={1}|W={2}|H={3}' -f
            [math]::Round($button.Current.BoundingRectangle.X, 2),
            [math]::Round($button.Current.BoundingRectangle.Y, 2),
            [math]::Round($button.Current.BoundingRectangle.Width, 2),
            [math]::Round($button.Current.BoundingRectangle.Height, 2)))
    }
    $start = $buttons | Where-Object { $_.Current.BoundingRectangle.X -gt 200 } | Select-Object -First 1
    if (-not $start) {
        throw 'Benchmark START button not found.'
    }

    $start.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $lines.Add('START_CLICK=1')

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($stopwatch.Elapsed.TotalSeconds -lt $WaitSeconds) {
        $cpu = (Get-Counter '\Processor(_Total)\% Processor Time').CounterSamples[0].CookedValue
        $commit = (Get-Counter '\Memory\Committed Bytes').CounterSamples[0].CookedValue
        $diskLatency = 0
        $diskTransfers = 0
        try {
            $diskLatency = (Get-Counter '\PhysicalDisk(_Total)\Avg. Disk sec/Transfer').CounterSamples[0].CookedValue
            $diskTransfers = (Get-Counter '\PhysicalDisk(_Total)\Disk Transfers/sec').CounterSamples[0].CookedValue
        } catch {
            $diskLatency = 0
            $diskTransfers = 0
        }

        $counterSamples.Add([ordered]@{
            timestamp = (Get-Date).ToString('o')
            cpu_percent = [math]::Round($cpu, 2)
            committed_bytes = [int64]$commit
            avg_disk_sec_per_transfer = [math]::Round($diskLatency, 6)
            disk_transfers_per_sec = [math]::Round($diskTransfers, 2)
        }) | Out-Null

        Start-Sleep -Seconds 2
    }
    $stopwatch.Stop()
    $lines.Add('BENCHMARK_OBSERVATION_SECONDS=' + [math]::Round($stopwatch.Elapsed.TotalSeconds, 2))

    & $wprExe -stop $etl | Out-Null
    $lines.Add('ETL_EXISTS=' + (Test-Path $etl))

    $texts = $window.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text))
    )
    $seen = @()
    foreach ($text in $texts) {
        $name = $text.Current.Name
        if ($name -and $seen -notcontains $name) {
            $seen += $name
        }
    }

    $lines.Add('OCCT_TEXT_COUNT=' + $seen.Count)
    foreach ($entry in ($seen | Select-Object -First 40)) {
        $lines.Add('TEXT=' + $entry)
    }

    Get-Process -Name 'OCCT' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

    $counterSamples | ForEach-Object { [pscustomobject]$_ } | Export-Csv -Path $perfCsv -NoTypeInformation -Encoding UTF8
    $lines.Add('PERF_CSV_EXISTS=' + (Test-Path $perfCsv))

    if ($baselineExists) {
        Set-ItemProperty -Path $registryPath -Name $valueName -Value $baselineValue
        $lines.Add('RESTORED=' + $baselineValue)
        $restored = $true
    } else {
        Remove-ItemProperty -Path $registryPath -Name $valueName -ErrorAction SilentlyContinue
        $lines.Add('RESTORED=ABSENT')
        $restored = $true
    }
}
catch {
    $lines.Add('ERROR=' + $_.Exception.Message)
    if ($_.InvocationInfo) {
        $lines.Add('AT=' + $_.InvocationInfo.PositionMessage)
    }
}
finally {
    if (-not $restored) {
        try {
            if ($baselineExists) {
                Set-ItemProperty -Path $registryPath -Name $valueName -Value $baselineValue
                $lines.Add('RESTORED_IN_FINALLY=' + $baselineValue)
            } else {
                Remove-ItemProperty -Path $registryPath -Name $valueName -ErrorAction SilentlyContinue
                $lines.Add('RESTORED_IN_FINALLY=ABSENT')
            }
        } catch {
            $lines.Add('RESTORE_ERROR=' + $_.Exception.Message)
        }
    }
    $lines | Set-Content -Path $result -Encoding UTF8
}
