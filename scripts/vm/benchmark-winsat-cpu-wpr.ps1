param(
    [string]$ArtifactRoot = 'C:\Tools\ValidationController\controller\current\artifacts',
    [string]$TracePrefix = 'winsat-cpu',
    [switch]$SkipWpr
)

$ErrorActionPreference = 'Stop'

$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$winsat = 'C:\Windows\System32\winsat.exe'

function Invoke-NativeProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList,

        [switch]$IgnoreExitCode
    )

    $proc = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -NoNewWindow -Wait -PassThru
    if (-not $IgnoreExitCode -and $proc.ExitCode -ne 0) {
        throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $($proc.ExitCode)"
    }

    return $proc.ExitCode
}

if (-not (Test-Path $ArtifactRoot)) {
    New-Item -ItemType Directory -Path $ArtifactRoot -Force | Out-Null
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$etlPath = Join-Path $ArtifactRoot ("{0}-{1}.etl" -f $TracePrefix, $stamp)
$stdoutPath = Join-Path $ArtifactRoot ("{0}-{1}.winsat.txt" -f $TracePrefix, $stamp)

if (-not (Test-Path $winsat)) {
    throw "winsat.exe not found at $winsat"
}

if (-not $SkipWpr) {
    if (-not (Test-Path $wpr)) {
        throw "wpr.exe not found at $wpr"
    }

    Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-cancel') -IgnoreExitCode | Out-Null
    Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-start', 'GeneralProfile', '-filemode') | Out-Null
}

try {
    $output = & $winsat cpuformal 2>&1
    $output | Set-Content -Path $stdoutPath -Encoding UTF8
    $output
}
finally {
    if (-not $SkipWpr) {
        Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-stop', $etlPath) | Out-Null
    }
}
