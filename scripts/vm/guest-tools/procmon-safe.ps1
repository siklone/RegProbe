[CmdletBinding()]
param(
    [int]$DurationSeconds = 90,
    [int]$MaxMegabytes = 256,
    [string]$OutputDirectory = 'C:\Tools\Perf\Procmon',
    [string]$OutputName = '',
    [switch]$TerminateExisting
)

$ErrorActionPreference = 'Stop'

function Get-ProcmonPath {
    $candidates = @(
        'C:\Tools\Sysinternals\Procmon64.exe',
        'C:\Tools\SysinternalsSuite\Procmon64.exe',
        'C:\Tools\Procmon.exe'
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw 'Procmon64.exe not found in the expected tooling paths.'
}

function Stop-ProcmonProcess {
    $existing = Get-Process -Name 'Procmon64' -ErrorAction SilentlyContinue
    if ($existing) {
        $existing | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
}

$procmon = Get-ProcmonPath
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($OutputName)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputName = "procmon-$stamp.pml"
}

$outputPath = Join-Path $OutputDirectory $OutputName
if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Force -ErrorAction SilentlyContinue
}

if ($TerminateExisting) {
    Stop-ProcmonProcess
}

try {
    & $procmon /AcceptEula /Quiet /Minimized /BackingFile $outputPath /Runtime ([Math]::Max($DurationSeconds, 1)) /Terminate | Out-Null
    if (-not (Test-Path $outputPath)) {
        throw "Procmon fast-path did not create $outputPath"
    }
}
catch {
    # Older Procmon builds can reject one or more optional switches. Fall back to the
    # smallest reliable launch shape and terminate explicitly after the dwell window.
    Stop-ProcmonProcess
    $fallback = Start-Process -FilePath $procmon -ArgumentList @(
        '/AcceptEula',
        '/Quiet',
        '/Minimized',
        '/BackingFile', $outputPath
    ) -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds ([Math]::Max($DurationSeconds, 1))
    & $procmon /Terminate | Out-Null
    if (-not $fallback.WaitForExit(15000)) {
        Stop-ProcmonProcess
    }
}

if (-not (Test-Path $outputPath)) {
    throw "Procmon output was not created at $outputPath"
}

$fileInfo = Get-Item -Path $outputPath
if ($fileInfo.Length -gt ($MaxMegabytes * 1MB)) {
    throw "Procmon output exceeded the requested size budget: $([Math]::Round($fileInfo.Length / 1MB, 2)) MiB"
}

Write-Output $outputPath
