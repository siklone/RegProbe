[CmdletBinding()]
param(
    [string]$DesktopPath = 'C:\Users\Administrator\Desktop',
    [string]$ArchiveRoot = 'C:\Tools\Archive',
    [string]$ToolsRoot = 'C:\Tools',
    [int]$RetentionDays = 7,
    [switch]$Apply,
    [string]$OutputPath = 'C:\Tools\cleanup-guest-report.json'
)

$ErrorActionPreference = 'Stop'

$expectedDesktop = 'C:\Users\Administrator\Desktop'
if ([string]::IsNullOrWhiteSpace($DesktopPath) -or -not (Test-Path $DesktopPath)) {
    $DesktopPath = $expectedDesktop
}

$desktopResolved = (Resolve-Path $DesktopPath).Path
if ($desktopResolved -like 'C:\Windows\System32*') {
    throw "Refusing to treat $desktopResolved as a desktop path."
}

$desktopArchive = Join-Path $ArchiveRoot 'Desktop'
New-Item -ItemType Directory -Path $desktopArchive -Force | Out-Null

$movedDesktop = @()
if (Test-Path $desktopResolved) {
    foreach ($item in Get-ChildItem $desktopResolved -Force -ErrorAction SilentlyContinue) {
        if ($item.Name -ieq 'desktop.ini') {
            continue
        }

        $destination = Join-Path $desktopArchive $item.Name
        if ($Apply) {
            Move-Item -Path $item.FullName -Destination $destination -Force
        }
        $movedDesktop += [pscustomobject]@{
            name = $item.Name
            from = $item.FullName
            to = $destination
        }
    }
}

$cutoff = (Get-Date).AddDays(-1 * $RetentionDays)
$removals = @()
$cleanupTargets = @(
    'C:\Tools\ValidationController\manual',
    'C:\Tools\ValidationController\smoke',
    'C:\Users\Administrator\AppData\Roaming\OCCT',
    'C:\Users\Administrator\AppData\Local\OCCT'
)

foreach ($target in $cleanupTargets) {
    if (-not (Test-Path $target)) {
        continue
    }
    foreach ($item in Get-ChildItem $target -Force -ErrorAction SilentlyContinue) {
        if ($item.LastWriteTime -gt $cutoff -and $target -notlike '*OCCT*') {
            continue
        }

        $removals += [pscustomobject]@{
            path = $item.FullName
            kind = if ($item.PSIsContainer) { 'directory' } else { 'file' }
        }
        if ($Apply) {
            Remove-Item -Path $item.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

$perfRoot = 'C:\Tools\Perf'
if (Test-Path $perfRoot) {
    $perfItems = Get-ChildItem $perfRoot -Force -ErrorAction SilentlyContinue | Where-Object {
        $_.PSIsContainer -or $_.Extension -in @('.etl', '.txt', '.log', '.csv', '.json', '.zip')
    }
    foreach ($item in $perfItems) {
        if ($item.LastWriteTime -gt $cutoff) {
            continue
        }

        $removals += [pscustomobject]@{
            path = $item.FullName
            kind = if ($item.PSIsContainer) { 'directory' } else { 'file' }
        }
        if ($Apply) {
            Remove-Item -Path $item.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

$occtLoose = Get-ChildItem $ToolsRoot -Force -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^OCCT' }
foreach ($item in $occtLoose) {
    $removals += [pscustomobject]@{
        path = $item.FullName
        kind = if ($item.PSIsContainer) { 'directory' } else { 'file' }
    }
    if ($Apply) {
        Remove-Item -Path $item.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$report = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    applied = [bool]$Apply
    retention_days = $RetentionDays
    moved_desktop = $movedDesktop
    removals = $removals
}

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
Write-Output $OutputPath
