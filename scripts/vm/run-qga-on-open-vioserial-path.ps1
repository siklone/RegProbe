param(
    [string]$ResultsPath = 'C:\RegProbe-Diag\bootstrap\qga-vioserial-path-results.txt',
    [string]$QgaPath = 'C:\Program Files\qemu-ga\qemu-ga.exe'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ResultsPath)) {
    throw "Missing results file: $ResultsPath"
}

$candidate = Get-Content -LiteralPath $ResultsPath | Where-Object {
    $_ -like 'OpenTest=* => OK' -and $_ -match '&02#'
} | Select-Object -First 1

if (-not $candidate) {
    throw "No openable &02 path found in $ResultsPath"
}

$path = ($candidate -replace '^OpenTest=', '') -replace ' => OK$', ''

Get-Process qemu-ga -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$proc = Start-Process -FilePath $QgaPath -ArgumentList @('-v', '-m', 'virtio-serial', '-p', $path) -PassThru
Start-Sleep -Seconds 2

$alive = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue

"SelectedPath=$path"
"StartedPid=$($proc.Id)"
"Alive=$([bool]$alive)"
