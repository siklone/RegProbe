param(
    [string]$ArtifactRoot = 'C:\Tools\ValidationController\controller\current\artifacts',
    [string]$TracePrefix = 'diskspd',
    [int]$DurationSeconds = 20,
    [int]$WarmupSeconds = 5,
    [int]$CooldownSeconds = 3,
    [int]$FileSizeMiB = 256,
    [switch]$SkipWpr
)

$ErrorActionPreference = 'Stop'

$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$diskspd = 'C:\Tools\Perf\diskspd.exe'
$targetDir = 'C:\Tools\DiskSpd\work'

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

if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$etlPath = Join-Path $ArtifactRoot ("{0}-{1}.etl" -f $TracePrefix, $stamp)
$stdoutPath = Join-Path $ArtifactRoot ("{0}-{1}.diskspd.txt" -f $TracePrefix, $stamp)
$targetFile = Join-Path $targetDir 'diskspd-test.dat'

if (-not (Test-Path $diskspd)) {
    throw "diskspd.exe not found at $diskspd"
}

if (-not $SkipWpr) {
    if (-not (Test-Path $wpr)) {
        throw "wpr.exe not found at $wpr"
    }

    Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-cancel') -IgnoreExitCode | Out-Null
    Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-start', 'GeneralProfile', '-filemode') | Out-Null
}

try {
    # Keep the workload bounded: moderate queue depth, medium block size, short duration.
    $output = & $diskspd `
        "-c${FileSizeMiB}M" `
        '-b64K' `
        '-d' $DurationSeconds `
        '-W' $WarmupSeconds `
        '-C' $CooldownSeconds `
        '-r' `
        '-w30' `
        '-o2' `
        '-t2' `
        '-Sh' `
        '-L' `
        $targetFile 2>&1

    $output | Set-Content -Path $stdoutPath -Encoding UTF8
    $output
}
finally {
    if (-not $SkipWpr) {
        Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-stop', $etlPath) | Out-Null
    }
}
