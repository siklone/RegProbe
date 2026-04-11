param(
    [ValidateSet('default-alias', 'exact-openpath')]
    [string]$Mode = 'exact-openpath',
    [string]$ProbeResultsPath = 'C:\RegProbe-Diag\bootstrap\qga-vioserial-path-results.txt',
    [string]$QgaPath = 'C:\Program Files\qemu-ga\qemu-ga.exe',
    [string]$LogPath = 'C:\RegProbe-Diag\bootstrap\qga-runtime-handshake.log',
    [string]$OutputPath = 'C:\RegProbe-Diag\bootstrap\qga-runtime-handshake-results.txt'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ExactOpenPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResultsPath
    )

    if (-not (Test-Path -LiteralPath $ResultsPath)) {
        throw "Missing probe results file: $ResultsPath"
    }

    $candidate = Get-Content -LiteralPath $ResultsPath | Where-Object {
        $_ -like 'OpenTest=* => OK' -and $_ -match '&02#'
    } | Select-Object -First 1

    if (-not $candidate) {
        throw "No openable &02 qga path found in $ResultsPath"
    }

    return (($candidate -replace '^OpenTest=', '') -replace ' => OK$', '')
}

function Add-Section {
    param(
        [System.Collections.Generic.List[string]]$Sink,
        [string]$Title,
        [string[]]$Lines
    )

    $Sink.Add("=== $Title ===")
    foreach ($line in $Lines) {
        $Sink.Add($line)
    }
    $Sink.Add('')
}

$selectedPath = if ($Mode -eq 'default-alias') {
    '\\.\Global\org.qemu.guest_agent.0'
} else {
    Get-ExactOpenPath -ResultsPath $ProbeResultsPath
}

if (Test-Path -LiteralPath $LogPath) {
    Remove-Item -LiteralPath $LogPath -Force
}

Get-Process qemu-ga -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 750

$proc = Start-Process -FilePath $QgaPath -ArgumentList @('-v', '-l', $LogPath, '-m', 'virtio-serial', '-p', $selectedPath) -PassThru
Start-Sleep -Seconds 4

$alive = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
$serviceLines = @(sc.exe query QEMU-GA 2>&1)
$taskLines = @(tasklist /fi "imagename eq qemu-ga.exe" 2>&1)
$logLines = if (Test-Path -LiteralPath $LogPath) {
    @(Get-Content -LiteralPath $LogPath -Tail 120)
} else {
    @('LOG_MISSING')
}

$eventLines = @(Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = (Get-Date).AddMinutes(-15) } -ErrorAction SilentlyContinue |
    Where-Object {
        ($_.ProviderName -match 'QEMU|qemu|Virtio|vioser') -or
        ($_.Message -match 'QEMU|qemu|Virtio|vioser')
    } |
    Select-Object -First 12 |
    ForEach-Object {
        "[{0:u}] {1} {2} {3}" -f $_.TimeCreated, $_.ProviderName, $_.Id, (($_.Message -replace '\r?\n', ' ') -replace '\s+', ' ').Trim()
    })

if (-not $eventLines) {
    $eventLines = @('NO_RECENT_MATCHING_EVENTS')
}

$report = [System.Collections.Generic.List[string]]::new()
$report.Add("Timestamp={0:o}" -f [datetime]::UtcNow)
$report.Add("Mode=$Mode")
$report.Add("SelectedPath=$selectedPath")
$report.Add("StartedPid=$($proc.Id)")
$report.Add("Alive=$([bool]$alive)")
$report.Add("LogPath=$LogPath")
$report.Add('')

Add-Section -Sink $report -Title 'SERVICE STATE' -Lines $serviceLines
Add-Section -Sink $report -Title 'TASKLIST' -Lines $taskLines
Add-Section -Sink $report -Title 'QGA LOG TAIL' -Lines $logLines
Add-Section -Sink $report -Title 'APPLICATION EVENTS' -Lines $eventLines

$directory = Split-Path -Parent $OutputPath
if ($directory) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$report | Set-Content -LiteralPath $OutputPath -Encoding UTF8
$report | ForEach-Object { Write-Host $_ }
