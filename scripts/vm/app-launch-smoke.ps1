[CmdletBinding()]
param(
    [string]$PublishZipPath = 'C:\Tools\Inbound\app-publish.zip',
    [string]$OutputRoot = 'C:\Tools\ValidationController\smoke',
    [string]$AppRoot = 'C:\Tools\AppSmoke'
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\Inbound' -Force | Out-Null

function Stop-LegacyAppProcesses {
    $targetNames = @(
        'RegProbe.App',
        'RegProbe.ElevatedHost',
        'WindowsOptimizer.App',
        'WindowsOptimizer.ElevatedHost',
        'OpenTraceProject.App',
        'OpenTraceProject.ElevatedHost'
    )

    $stopped = @()

    foreach ($proc in Get-Process -ErrorAction SilentlyContinue | Where-Object { $targetNames -contains $_.ProcessName }) {
        try {
            Stop-Process -Id $proc.Id -Force -ErrorAction Stop
            $stopped += "$($proc.ProcessName):$($proc.Id)"
        }
        catch {
        }
    }

    if ($stopped.Count -gt 0) {
        Start-Sleep -Seconds 2
    }

    return $stopped
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
}

$runningProcess = $null

try {
    $result.stopped_processes = Stop-LegacyAppProcesses

    if (Test-Path $AppRoot) {
        Remove-Item -Path $AppRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $AppRoot -Force | Out-Null
    Expand-Archive -Path $PublishZipPath -DestinationPath $AppRoot -Force

    $legacyArtifacts = Get-ChildItem -Path $AppRoot -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'WindowsOptimizer*' -or $_.Name -like 'OpenTraceProject*' }

    foreach ($artifact in $legacyArtifacts) {
        Remove-Item -LiteralPath $artifact.FullName -Force -ErrorAction SilentlyContinue
    }

    $result.legacy_artifacts_removed = @($legacyArtifacts | Select-Object -ExpandProperty FullName)

    $exe = Join-Path $AppRoot 'RegProbe.App.exe'
    $docsRoot = Join-Path $AppRoot 'Docs'
    $evidenceClasses = Join-Path $docsRoot 'research\evidence-classes.json'
    $tweakCatalog = Join-Path $docsRoot 'tweaks\tweak-catalog.html'

    $result.executable = $exe
    $result.docs_root_exists = [bool](Test-Path $docsRoot)
    $result.evidence_classes_exists = [bool](Test-Path $evidenceClasses)
    $result.tweak_catalog_exists = [bool](Test-Path $tweakCatalog)

    if (-not (Test-Path $exe)) {
        throw "Published app executable not found at $exe"
    }

    $proc = Start-Process -FilePath $exe -PassThru
    Start-Sleep -Seconds 12
    $runningProcess = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue

    $result.process_started = [bool]$proc
    $result.process_alive_after_12s = [bool]$runningProcess
    $result.process_id = if ($runningProcess) { $runningProcess.Id } else { $proc.Id }
    $result.main_window_title = if ($runningProcess) { $runningProcess.MainWindowTitle } else { '' }
}
catch {
    $result.error = $_.Exception.Message
}
finally {
    if ($runningProcess) {
        Stop-Process -Id $runningProcess.Id -Force -ErrorAction SilentlyContinue
    }

    $cleanupRemoved = @()
    foreach ($path in @($AppRoot, $PublishZipPath)) {
        if (-not (Test-Path $path)) {
            continue
        }

        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        if (-not (Test-Path $path)) {
            $cleanupRemoved += $path
        }
    }

    $result.cleanup_removed_paths = $cleanupRemoved
    $result.cleanup_verified =
        (-not (Test-Path $AppRoot)) -and
        (-not (Test-Path $PublishZipPath))
}

$resultPath = Join-Path $OutputRoot 'app-launch-smoke.json'
$result | ConvertTo-Json -Depth 6 | Set-Content -Path $resultPath -Encoding UTF8
Write-Output $resultPath
