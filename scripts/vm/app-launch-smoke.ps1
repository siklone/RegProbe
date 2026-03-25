[CmdletBinding()]
param(
    [string]$PublishZipPath = 'C:\Tools\Inbound\app-publish.zip',
    [string]$OutputRoot = 'C:\Tools\ValidationController\smoke',
    [string]$AppRoot = 'C:\Tools\AppSmoke'
)

$ErrorActionPreference = 'Continue'

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\Inbound' -Force | Out-Null

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
}

try {
    if (Test-Path $AppRoot) {
        Remove-Item -Path $AppRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $AppRoot -Force | Out-Null
    Expand-Archive -Path $PublishZipPath -DestinationPath $AppRoot -Force

    $exe = Join-Path $AppRoot 'OpenTraceProject.App.exe'
    $docsRoot = Join-Path $AppRoot 'Docs'
    $evidenceClasses = Join-Path $docsRoot 'tweaks\research\evidence-classes.json'
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
    $running = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue

    $result.process_started = [bool]$proc
    $result.process_alive_after_12s = [bool]$running
    $result.process_id = if ($running) { $running.Id } else { $proc.Id }
    $result.main_window_title = if ($running) { $running.MainWindowTitle } else { '' }

    if ($running) {
        Stop-Process -Id $running.Id -Force
    }
}
catch {
    $result.error = $_.Exception.Message
}

$resultPath = Join-Path $OutputRoot 'app-launch-smoke.json'
$result | ConvertTo-Json -Depth 6 | Set-Content -Path $resultPath -Encoding UTF8
Write-Output $resultPath
