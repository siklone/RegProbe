[CmdletBinding()]
param(
    [ValidateSet('arm', 'collect')]
    [string]$Stage = 'arm',

    [string]$OutputName = 'kernel-timing-wpr-boot-registry-20260412',
    [string]$RegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel',
    [string]$ValueName = 'TimerCheckFlags',

    [switch]$IncludeLargeArtifacts
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$guestToolPath = Join-Path $scriptRoot 'wprboot.ps1'
$outputRoot = Join-Path 'C:\RegProbe-Diag\wpr-boot-registry' $OutputName
$stateFile = Join-Path $outputRoot 'state.json'
$summaryArmPath = Join-Path $outputRoot 'summary-arm.json'
$summaryPath = Join-Path $outputRoot 'summary.json'
$csvPath = Join-Path $outputRoot ($OutputName + '.csv')
$etlPath = Join-Path $outputRoot ($OutputName + '.etl')
$hitsPath = Join-Path $outputRoot ($OutputName + '.hits.txt')
$normalizedPath = Join-Path $outputRoot ($OutputName + '.normalized.json')

$wprPath = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$tracerptPath = 'C:\Windows\System32\tracerpt.exe'

function Copy-ToWritableDrive {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Paths
    )

    foreach ($code in (([int][char]'E')..([int][char]'Z'))) {
        $drive = ([char]$code) + ':'
        if (-not (Test-Path ($drive + '\'))) {
            continue
        }

        try {
            $probe = Join-Path ($drive + '\') 'regprobe-write-test.tmp'
            Set-Content -Path $probe -Value 'test' -ErrorAction Stop
            $target = Join-Path ($drive + '\') 'kernel-timing-wpr-boot-registry'
            New-Item -ItemType Directory -Force -Path $target | Out-Null

            foreach ($path in $Paths) {
                if (Test-Path $path) {
                    Copy-Item -Force -Path $path -Destination $target
                }
            }

            Remove-Item -Force $probe -ErrorAction SilentlyContinue
            Write-Host "Copied outputs to $target"
            Get-ChildItem -Path $target | Sort-Object Name | Format-Table Name, Length, LastWriteTime -AutoSize
            return $target
        }
        catch {
            Write-Host "Drive $drive not writable: $($_.Exception.Message)"
        }
    }

    throw 'No writable transfer drive found'
}

if (-not (Test-Path $guestToolPath)) {
    throw "Missing guest tool payload: $guestToolPath"
}

if (-not (Test-Path $wprPath)) {
    throw "Missing wpr.exe: $wprPath"
}

if (-not (Test-Path $tracerptPath)) {
    throw "Missing tracerpt.exe: $tracerptPath"
}

$matchFragments = @(
    'TimerCheckFlags',
    'LongDpcRuntimeThreshold',
    'LongDpcQueueThreshold',
    'ForceBugcheckForDpcWatchdog',
    'ForceBugcheckOnDpcWatchdog',
    'DpcWatchdog',
    'Session Manager\Kernel'
)

Write-Host "Guest tool: $guestToolPath"
Write-Host "Stage: $Stage"
Write-Host "Output root: $outputRoot"

if ($Stage -eq 'arm') {
    & $guestToolPath `
        -Stage arm `
        -RegistryPath $RegistryPath `
        -ValueName $ValueName `
        -OutputName $OutputName `
        -OutputRoot $outputRoot `
        -StateFile $stateFile `
        -MatchFragments $matchFragments

    if (Test-Path $summaryArmPath) {
        Write-Host "Arm summary: $summaryArmPath"
        Get-Content -Path $summaryArmPath -Raw
    }
    else {
        throw "Arm summary missing: $summaryArmPath"
    }

    exit 0
}

if (-not (Test-Path $stateFile)) {
    throw "Missing state file for collect stage: $stateFile"
}

& $guestToolPath `
    -Stage collect `
    -StateFile $stateFile

if (-not (Test-Path $summaryPath)) {
    throw "Collect summary missing: $summaryPath"
}

if (-not (Test-Path $hitsPath)) {
    New-Item -ItemType File -Force -Path $hitsPath | Out-Null
}

$copyPaths = @(
    $summaryArmPath,
    $summaryPath,
    $stateFile,
    $hitsPath,
    $normalizedPath
)

if ($IncludeLargeArtifacts) {
    $copyPaths += @($etlPath, $csvPath)
}

$copiedTo = Copy-ToWritableDrive -Paths $copyPaths

Write-Host "Collect summary: $summaryPath"
Get-Content -Path $summaryPath -Raw
Write-Host "Transfer target: $copiedTo"
