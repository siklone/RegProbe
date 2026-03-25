param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true,
    [switch]$Clean,
    [switch]$ReadyToRun
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $scriptDir = $PSScriptRoot
    return (Resolve-Path (Join-Path $scriptDir "..")).Path
}

function Get-Version {
    param([string]$RepoRoot)
    $propsPath = Join-Path $RepoRoot "Directory.Build.props"
    if (!(Test-Path $propsPath)) {
        return "0.0.0"
    }

    $match = Select-String -Path $propsPath -Pattern "<Version>(.*?)</Version>" | Select-Object -First 1
    if ($match -and $match.Matches.Count -gt 0) {
        return $match.Matches[0].Groups[1].Value
    }

    return "0.0.0"
}

$repoRoot = Get-RepoRoot
$projectPath = Join-Path $repoRoot "OpenTraceProject.App\OpenTraceProject.App.csproj"
$publishDir = Join-Path $repoRoot "OpenTraceProject.App\bin\$Configuration\net8.0-windows\$Runtime\publish"

if ($Clean -and (Test-Path $publishDir)) {
    Remove-Item -Recurse -Force $publishDir
}

if (!(Test-Path $projectPath)) {
    throw "Project not found at $projectPath"
}

$readyToRunValue = if ($ReadyToRun.IsPresent) { "true" } else { "false" }

Write-Host "Publishing OpenTraceProject.App ($Configuration, $Runtime)..."
Write-Host "PublishReadyToRun=$readyToRunValue"
dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained:$SelfContained /p:PublishReadyToRun=$readyToRunValue

if (!(Test-Path $publishDir)) {
    throw "Publish output not found at $publishDir"
}

# Ensure Docs are available in publish output
$docsSource = Join-Path $repoRoot "Docs"
$docsTarget = Join-Path $publishDir "Docs"
if (Test-Path $docsSource) {
    if (Test-Path $docsTarget) {
        Remove-Item -Recurse -Force $docsTarget
    }
    Copy-Item -Recurse -Force $docsSource $docsTarget
}

# Ensure ElevatedHost is present in publish output
$hostTargetDir = Join-Path $publishDir "ElevatedHost"
$hostTargetExe = Join-Path $hostTargetDir "OpenTraceProject.ElevatedHost.exe"
if (!(Test-Path $hostTargetExe)) {
    $candidates = @(
        Join-Path $repoRoot "OpenTraceProject.ElevatedHost\bin\$Configuration\net8.0-windows\$Runtime\OpenTraceProject.ElevatedHost.exe",
        Join-Path $repoRoot "OpenTraceProject.ElevatedHost\bin\$Configuration\net8.0-windows\$Runtime\publish\OpenTraceProject.ElevatedHost.exe"
    ) | Where-Object { Test-Path $_ }

    if ($candidates.Count -gt 0) {
        New-Item -ItemType Directory -Path $hostTargetDir -Force | Out-Null
        Copy-Item -Force $candidates[0] $hostTargetExe
    }
}

# Create a zip package in dist/
$distDir = Join-Path $repoRoot "dist"
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$version = Get-Version -RepoRoot $repoRoot
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$zipName = "OpenTraceProject-$version-$Runtime-$Configuration-$timestamp.zip"
$zipPath = Join-Path $distDir $zipName

if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Package created:" $zipPath
