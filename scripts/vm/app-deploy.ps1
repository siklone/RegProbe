[CmdletBinding()]
param(
    [string]$PublishZipPath = 'C:\Tools\Inbound\app-publish.zip',
    [string]$OutputRoot = 'C:\Tools\ValidationController\smoke',
    [string]$AppRoot = 'C:\Tools\AppSmoke'
)

$ErrorActionPreference = 'Stop'

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

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\Inbound' -Force | Out-Null

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
}

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

$exe = Join-Path $AppRoot 'RegProbe.App.exe'
$docsRoot = Join-Path $AppRoot 'Docs'
$evidenceClasses = Join-Path $docsRoot 'research\evidence-classes.json'
$tweakCatalog = Join-Path $docsRoot 'tweaks\tweak-catalog.html'

$result.legacy_artifacts_removed = @($legacyArtifacts | Select-Object -ExpandProperty FullName)
$result.executable = $exe
$result.docs_root_exists = [bool](Test-Path $docsRoot)
$result.evidence_classes_exists = [bool](Test-Path $evidenceClasses)
$result.tweak_catalog_exists = [bool](Test-Path $tweakCatalog)
$result.ready = [bool](Test-Path $exe)

if (-not (Test-Path $exe)) {
    throw "Published app executable not found at $exe"
}

$resultPath = Join-Path $OutputRoot 'app-deploy.json'
$result | ConvertTo-Json -Depth 6 | Set-Content -Path $resultPath -Encoding UTF8
Write-Output $resultPath
