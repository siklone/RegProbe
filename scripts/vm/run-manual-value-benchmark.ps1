[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TestName,

    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [int]$BaselineValue,

    [Parameter(Mandatory = $true)]
    [int]$CandidateValue,

    [string[]]$BenchmarkScripts = @(
        'C:\Tools\Scripts\benchmark-winsat-cpu-wpr.ps1',
        'C:\Tools\Scripts\benchmark-winsat-mem-wpr.ps1'
    ),

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\manual'
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $TestName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $TestName, $stamp)
$hostMeasureScript = Join-Path $hostRoot 'measure-benchmark.ps1'
$guestMeasureScript = Join-Path $guestRoot 'measure-benchmark.ps1'
$hostBootScript = Join-Path $hostRoot 'write-lastboot.ps1'
$guestBootScript = Join-Path $guestRoot 'write-lastboot.ps1'
$hostRegistryScript = Join-Path $hostRoot 'write-reg-value.ps1'
$guestRegistryScript = Join-Path $guestRoot 'write-reg-value.ps1'
$summaryPath = Join-Path $hostRoot 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$guestMeasureScriptContent = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$BenchmarkScript,

    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [Parameter(Mandatory = $true)]
    [string]$TracePrefix,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

    [Parameter(Mandatory = $true)]
    [string]$ZipPath
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $ArtifactRoot -Force | Out-Null

$sw = [System.Diagnostics.Stopwatch]::StartNew()
& $BenchmarkScript -ArtifactRoot $ArtifactRoot -TracePrefix $TracePrefix
$sw.Stop()

$files = Get-ChildItem -Path $ArtifactRoot | Sort-Object LastWriteTime | Select-Object Name, Length, LastWriteTime
$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    benchmark_script = $BenchmarkScript
    artifact_root = $ArtifactRoot
    trace_prefix = $TracePrefix
    duration_seconds = [Math]::Round($sw.Elapsed.TotalSeconds, 2)
    file_count = @($files).Count
    files = @($files)
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $SummaryPath -Encoding UTF8

if (Test-Path $ZipPath) {
    Remove-Item -Path $ZipPath -Force
}

Compress-Archive -Path (Join-Path $ArtifactRoot '*') -DestinationPath $ZipPath -Force
'@

$guestLastBootScriptContent = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$boot = (Get-CimInstance -ClassName Win32_OperatingSystem).LastBootUpTime
Set-Content -Path $OutputPath -Value $boot.ToString('o') -Encoding UTF8
'@

$guestRegistryValueScriptContent = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$value = (Get-ItemProperty -Path ("Registry::{0}" -f $RegistryPath) -Name $ValueName).$ValueName
Set-Content -Path $OutputPath -Value $value -Encoding UTF8
'@

Set-Content -Path $hostMeasureScript -Value $guestMeasureScriptContent -Encoding UTF8
Set-Content -Path $hostBootScript -Value $guestLastBootScriptContent -Encoding UTF8
Set-Content -Path $hostRegistryScript -Value $guestRegistryValueScriptContent -Encoding UTF8

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Invoke-GuestProgram {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$ArgumentList = @()
    )

    $args = @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'runProgramInGuest', $VmPath, $FilePath) + $ArgumentList
    Invoke-Vmrun -Arguments $args | Out-Null
}

function Wait-GuestRunning {
    param([int]$TimeoutSeconds = 600)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw 'Guest did not return to a running VMware Tools state in time.'
}

function Wait-Explorer {
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $processes = Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'listProcessesInGuest', $VmPath)
            if ($processes -match 'Explorer\.EXE') {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw 'Explorer did not come back in time.'
}

function Get-GuestLastBootUpTime {
    $guestOutputPath = Join-Path $guestRoot 'lastboot.txt'
    $hostOutputPath = Join-Path $hostRoot 'lastboot.txt'

    Invoke-GuestProgram -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $guestBootScript,
        '-OutputPath',
        $guestOutputPath
    )

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestOutputPath, $hostOutputPath) | Out-Null
    return [DateTimeOffset]::Parse((Get-Content -Path $hostOutputPath -Raw).Trim())
}

function Wait-GuestReboot {
    param(
        [Parameter(Mandatory = $true)]
        [DateTimeOffset]$PreviousBoot,

        [int]$TimeoutSeconds = 600
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                $currentBoot = Get-GuestLastBootUpTime
                if ($currentBoot -gt $PreviousBoot) {
                    Wait-Explorer
                    Start-Sleep -Seconds 20
                    return $currentBoot
                }
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw 'Guest reboot did not complete in time.'
}

function Restart-Guest {
    $previousBoot = Get-GuestLastBootUpTime
    Invoke-GuestProgram -FilePath 'C:\Windows\System32\shutdown.exe' -ArgumentList @('/r', '/t', '0', '/f')
    Wait-GuestReboot -PreviousBoot $previousBoot | Out-Null
}

function Set-GuestDwordValue {
    param([int]$Value)

    Invoke-GuestProgram -FilePath 'C:\Windows\System32\reg.exe' -ArgumentList @(
        'add',
        $RegistryPath,
        '/v',
        $ValueName,
        '/t',
        'REG_DWORD',
        '/d',
        "$Value",
        '/f'
    )
}

function Capture-GuestValue {
    param([string]$Name)

    $guestValuePath = Join-Path $guestRoot ("{0}.value.txt" -f $Name)
    $hostValuePath = Join-Path $hostRoot ("{0}.value.txt" -f $Name)

    Invoke-GuestProgram -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $guestRegistryScript,
        '-RegistryPath',
        $RegistryPath,
        '-ValueName',
        $ValueName,
        '-OutputPath',
        $guestValuePath
    )

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestValuePath, $hostValuePath) | Out-Null
    return (Get-Content -Path $hostValuePath -Raw).Trim()
}

function Get-BenchmarkLabel {
    param([string]$BenchmarkScript)

    switch -Wildcard ([System.IO.Path]::GetFileName($BenchmarkScript)) {
        'benchmark-winsat-cpu-wpr.ps1' { return 'cpu' }
        'benchmark-winsat-mem-wpr.ps1' { return 'mem' }
        'benchmark-diskspd-wpr.ps1' { return 'disk' }
        default { return [System.IO.Path]::GetFileNameWithoutExtension($BenchmarkScript) }
    }
}

function Run-BenchmarkPass {
    param(
        [string]$Phase,
        [int]$Value,
        [string]$BenchmarkScript
    )

    $benchmarkLabel = Get-BenchmarkLabel -BenchmarkScript $BenchmarkScript
    $label = "{0}-{1}" -f $Phase, $benchmarkLabel
    $tracePrefix = "{0}-{1}-{2}" -f $TestName, $Value, $benchmarkLabel
    $guestArtifactRoot = Join-Path $guestRoot $label
    $guestSummary = Join-Path $guestRoot ("{0}.summary.json" -f $label)
    $guestZip = Join-Path $guestRoot ("{0}.zip" -f $label)
    $hostSummary = Join-Path $hostRoot ("{0}.summary.json" -f $label)
    $hostZip = Join-Path $hostRoot ("{0}.zip" -f $label)

    Invoke-GuestProgram -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $guestMeasureScript,
        '-BenchmarkScript',
        $BenchmarkScript,
        '-ArtifactRoot',
        $guestArtifactRoot,
        '-TracePrefix',
        $tracePrefix,
        '-SummaryPath',
        $guestSummary,
        '-ZipPath',
        $guestZip
    )

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestSummary, $hostSummary) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestZip, $hostZip) | Out-Null

    return @{
        label = $label
        summary_path = $hostSummary
        artifact_zip = $hostZip
        summary = Get-Content -Path $hostSummary -Raw | ConvertFrom-Json
    }
}

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CreateDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostMeasureScript, $guestMeasureScript) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostBootScript, $guestBootScript) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostRegistryScript, $guestRegistryScript) | Out-Null

Wait-GuestRunning
Wait-Explorer

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    test_name = $TestName
    registry_path = $RegistryPath
    value_name = $ValueName
    baseline_value = $BaselineValue
    candidate_value = $CandidateValue
    host_root = $hostRoot
    guest_root = $guestRoot
    phases = [ordered]@{}
}

Set-GuestDwordValue -Value $BaselineValue
Restart-Guest
$summary.phases['baseline'] = [ordered]@{
    observed_value = Capture-GuestValue -Name 'baseline-after-reboot'
    benchmarks = @()
}

foreach ($benchmarkScript in $BenchmarkScripts) {
    $summary.phases['baseline'].benchmarks += Run-BenchmarkPass -Phase 'baseline' -Value $BaselineValue -BenchmarkScript $benchmarkScript
}

Set-GuestDwordValue -Value $CandidateValue
Restart-Guest
$summary.phases['candidate'] = [ordered]@{
    observed_value = Capture-GuestValue -Name 'candidate-after-reboot'
    benchmarks = @()
}

foreach ($benchmarkScript in $BenchmarkScripts) {
    $summary.phases['candidate'].benchmarks += Run-BenchmarkPass -Phase 'candidate' -Value $CandidateValue -BenchmarkScript $benchmarkScript
}

Set-GuestDwordValue -Value $BaselineValue
Restart-Guest
$summary.phases['restore'] = [ordered]@{
    observed_value = Capture-GuestValue -Name 'restored-after-reboot'
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
Write-Output $summaryPath
