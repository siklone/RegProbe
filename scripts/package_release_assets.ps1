param(
    [Parameter(Mandatory = $true)]
    [string]$VersionLabel,

    [Parameter(Mandatory = $true)]
    [string]$AppPublishDir,

    [Parameter(Mandatory = $true)]
    [string]$CliPublishDir,

    [string]$Runtime = "win-x64",

    [string]$OutputDir = "release-assets"
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param([string]$PathValue)

    return [System.IO.Path]::GetFullPath($PathValue)
}

function New-ZipFromDirectory {
    param(
        [string]$SourceDir,
        [string]$DestinationZip
    )

    if (Test-Path $DestinationZip) {
        Remove-Item -Force $DestinationZip
    }

    Compress-Archive -Path (Join-Path $SourceDir "*") -DestinationPath $DestinationZip -Force
}

$appPublishDir = Resolve-FullPath $AppPublishDir
$cliPublishDir = Resolve-FullPath $CliPublishDir
$outputDir = Resolve-FullPath $OutputDir

if (!(Test-Path $appPublishDir)) {
    throw "App publish directory not found at $appPublishDir"
}

if (!(Test-Path $cliPublishDir)) {
    throw "CLI publish directory not found at $cliPublishDir"
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$portableZipName = "RegProbe-Portable-$VersionLabel-$Runtime.zip"
$cliZipName = "RegProbe-Cli-$VersionLabel-$Runtime.zip"
$checksumName = "RegProbe-$VersionLabel-$Runtime-sha256.txt"

$portableZipPath = Join-Path $outputDir $portableZipName
$cliZipPath = Join-Path $outputDir $cliZipName
$checksumPath = Join-Path $outputDir $checksumName

New-ZipFromDirectory -SourceDir $appPublishDir -DestinationZip $portableZipPath
New-ZipFromDirectory -SourceDir $cliPublishDir -DestinationZip $cliZipPath

$hashLines = @()
foreach ($artifact in @($portableZipPath, $cliZipPath)) {
    $hash = Get-FileHash -Path $artifact -Algorithm SHA256
    $hashLines += "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), (Split-Path $artifact -Leaf)
}

Set-Content -Path $checksumPath -Value $hashLines -Encoding utf8

Write-Host "Portable package:" $portableZipPath
Write-Host "CLI package:" $cliZipPath
Write-Host "Checksums:" $checksumPath
