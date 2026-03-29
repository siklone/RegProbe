[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\cpu-idle-benchmark',
    [string]$RecordId = 'power.disable-cpu-idle-states',
    [string]$SnapshotName = '',
    [string]$IncidentLogPath = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName
}

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$testName = "cpu-idle-states-$stamp"
$hostRoot = Join-Path $HostOutputRoot $testName
$guestRoot = Join-Path $GuestOutputRoot $testName
$hostRegistryScript = Join-Path $hostRoot 'set-cpu-idle-bundle.ps1'
$guestRegistryScript = Join-Path $guestRoot 'set-cpu-idle-bundle.ps1'
$hostReadScript = Join-Path $hostRoot 'read-cpu-idle-bundle.ps1'
$guestReadScript = Join-Path $guestRoot 'read-cpu-idle-bundle.ps1'
$hostMeasureScript = Join-Path $hostRoot 'measure-benchmark.ps1'
$guestMeasureScript = Join-Path $guestRoot 'measure-benchmark.ps1'
$summaryPath = Join-Path $hostRoot 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$guestRegistryScriptContent = @'
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('baseline', 'candidate', 'restore')]
    [string]$Mode,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$path = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power'
$names = @('DisableIdleStatesAtBoot', 'IdleStateTimeout', 'ExitLatencyCheckEnabled')

if ($Mode -eq 'candidate') {
    New-Item -Path $path -Force | Out-Null
    New-ItemProperty -Path $path -Name 'DisableIdleStatesAtBoot' -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $path -Name 'IdleStateTimeout' -Value 0 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $path -Name 'ExitLatencyCheckEnabled' -Value 1 -PropertyType DWord -Force | Out-Null
}
else {
    foreach ($name in $names) {
        Remove-ItemProperty -Path $path -Name $name -ErrorAction SilentlyContinue
    }
}

$result = [ordered]@{}
foreach ($name in $names) {
    try {
        $value = (Get-ItemProperty -Path $path -Name $name).$name
        $result[$name] = $value
    }
    catch {
        $result[$name] = $null
    }
}

$result | ConvertTo-Json -Depth 3 | Set-Content -Path $OutputPath -Encoding UTF8
'@

$guestReadScriptContent = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$path = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power'
$result = [ordered]@{}
foreach ($name in @('DisableIdleStatesAtBoot', 'IdleStateTimeout', 'ExitLatencyCheckEnabled')) {
    try {
        $value = (Get-ItemProperty -Path $path -Name $name).$name
        $result[$name] = $value
    }
    catch {
        $result[$name] = $null
    }
}
$boot = (Get-CimInstance -ClassName Win32_OperatingSystem).LastBootUpTime
[ordered]@{
    last_boot_utc = $boot.ToUniversalTime().ToString('o')
    values = $result
} | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputPath -Encoding UTF8
'@

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

Set-Content -Path $hostRegistryScript -Value $guestRegistryScriptContent -Encoding UTF8
Set-Content -Path $hostReadScript -Value $guestReadScriptContent -Encoding UTF8
Set-Content -Path $hostMeasureScript -Value $guestMeasureScriptContent -Encoding UTF8

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Wait-GuestReady {
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

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath) | Out-Null
}

function Copy-FromGuest {
    param([string]$GuestPath, [string]$HostPath)
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath) | Out-Null
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)
    Invoke-Vmrun -Arguments (@('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'runProgramInGuest', $VmPath, 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe') + $ArgumentList) | Out-Null
}

function Set-BundleState {
    param(
        [ValidateSet('baseline', 'candidate', 'restore')]
        [string]$Mode
    )

    $guestOut = Join-Path $guestRoot "$Mode.json"
    $hostOut = Join-Path $hostRoot "$Mode.json"

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestRegistryScript,
        '-Mode', $Mode,
        '-OutputPath', $guestOut
    )
    Copy-FromGuest -GuestPath $guestOut -HostPath $hostOut
    return Get-Content -Path $hostOut -Raw | ConvertFrom-Json
}

function Read-BundleState {
    param([string]$Tag)

    $guestOut = Join-Path $guestRoot "$Tag-read.json"
    $hostOut = Join-Path $hostRoot "$Tag-read.json"

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestReadScript,
        '-OutputPath', $guestOut
    )
    Copy-FromGuest -GuestPath $guestOut -HostPath $hostOut
    return Get-Content -Path $hostOut -Raw | ConvertFrom-Json
}

function Wait-VmPoweredOff {
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
            if ($running -notmatch [regex]::Escape($VmPath)) {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw "VM did not power off within $TimeoutSeconds seconds."
}

function Restart-Guest {
    $stopMode = 'soft'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'soft') -IgnoreExitCode | Out-Null
    try {
        Wait-VmPoweredOff -TimeoutSeconds 240
    }
    catch {
        $stopMode = 'hard'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'hard') -IgnoreExitCode | Out-Null
        Wait-VmPoweredOff -TimeoutSeconds 90
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-Explorer
    return $stopMode
}

function Get-ShellHealth {
    $processes = Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'listProcessesInGuest',
        $VmPath
    )

    return [ordered]@{
        explorer = [bool]($processes -match '\bexplorer\.exe\b')
        sihost = [bool]($processes -match '\bsihost\.exe\b')
        shellhost = [bool]($processes -match '\bShellHost\.exe\b')
        ctfmon = [bool]($processes -match '\bctfmon\.exe\b')
        process_dump = $processes
    }
}

function Restore-HealthySnapshot {
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        return
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-Explorer
}

function Log-Incident {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestId,
        [Parameter(Mandatory = $true)]
        [string]$Symptom,
        [string]$Notes = ''
    )

    & (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId $RecordId `
        -TweakId $RecordId `
        -TestId $TestId `
        -Family 'power-benchmark' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
        -ValueName 'DisableIdleStatesAtBoot + IdleStateTimeout + ExitLatencyCheckEnabled' `
        -ValueState 'behavior-lane-reboot' `
        -Symptom $Symptom `
        -ShellRecovered:$false `
        -NeededSnapshotRevert:$true `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    record_id = $RecordId
    snapshot_name = $SnapshotName
    test_name = $testName
    status = 'started'
    failed_stage = $null
    baseline_write = $null
    baseline_read = $null
    baseline_after_boot = $null
    candidate_write = $null
    candidate_after_boot = $null
    cpu_summary = $null
    mem_summary = $null
    restore_write = $null
    restore_after_boot = $null
    recovery = [ordered]@{
        performed = $false
        shell_healthy_after_recovery = $false
    }
    errors = @()
}

$needsRecovery = $false

try {
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        $summary.failed_stage = 'revert-snapshot'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
        Wait-GuestReady
        Wait-Explorer
    }

    $summary.failed_stage = 'prepare-guest-root'
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Copy-ToGuest -HostPath $hostRegistryScript -GuestPath $guestRegistryScript
    Copy-ToGuest -HostPath $hostReadScript -GuestPath $guestReadScript
    Copy-ToGuest -HostPath $hostMeasureScript -GuestPath $guestMeasureScript

    $summary.failed_stage = 'baseline-write'
    $summary.baseline_write = Set-BundleState -Mode baseline
    $summary.baseline_read = Read-BundleState -Tag 'baseline'
    $summary.failed_stage = 'baseline-reboot'
    Restart-Guest
    $summary.baseline_after_boot = Read-BundleState -Tag 'baseline-after-boot'

    $summary.failed_stage = 'candidate-write'
    $summary.candidate_write = Set-BundleState -Mode candidate
    $summary.failed_stage = 'candidate-reboot'
    Restart-Guest
    $summary.candidate_after_boot = Read-BundleState -Tag 'candidate-after-boot'

    $cpuArtifactRoot = Join-Path $guestRoot 'cpu'
    $cpuZip = Join-Path $guestRoot 'cpu.zip'
    $cpuSummary = Join-Path $guestRoot 'cpu-summary.json'
    $summary.failed_stage = 'cpu-benchmark'
    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestMeasureScript,
        '-BenchmarkScript', 'C:\Tools\Scripts\benchmark-winsat-cpu-wpr.ps1',
        '-ArtifactRoot', $cpuArtifactRoot,
        '-TracePrefix', 'cpu-idle-cpu',
        '-SummaryPath', $cpuSummary,
        '-ZipPath', $cpuZip
    )

    $hostCpuZip = Join-Path $hostRoot 'cpu.zip'
    $hostCpuSummary = Join-Path $hostRoot 'cpu-summary.json'
    Copy-FromGuest -GuestPath $cpuZip -HostPath $hostCpuZip
    Copy-FromGuest -GuestPath $cpuSummary -HostPath $hostCpuSummary
    Expand-Archive -Path $hostCpuZip -DestinationPath (Join-Path $hostRoot 'cpu') -Force
    $summary.cpu_summary = (Get-Content -Path $hostCpuSummary -Raw | ConvertFrom-Json)

    $memArtifactRoot = Join-Path $guestRoot 'mem'
    $memZip = Join-Path $guestRoot 'mem.zip'
    $memSummary = Join-Path $guestRoot 'mem-summary.json'
    $summary.failed_stage = 'memory-benchmark'
    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestMeasureScript,
        '-BenchmarkScript', 'C:\Tools\Scripts\benchmark-winsat-mem-wpr.ps1',
        '-ArtifactRoot', $memArtifactRoot,
        '-TracePrefix', 'cpu-idle-mem',
        '-SummaryPath', $memSummary,
        '-ZipPath', $memZip
    )

    $hostMemZip = Join-Path $hostRoot 'mem.zip'
    $hostMemSummary = Join-Path $hostRoot 'mem-summary.json'
    Copy-FromGuest -GuestPath $memZip -HostPath $hostMemZip
    Copy-FromGuest -GuestPath $memSummary -HostPath $hostMemSummary
    Expand-Archive -Path $hostMemZip -DestinationPath (Join-Path $hostRoot 'mem') -Force
    $summary.mem_summary = (Get-Content -Path $hostMemSummary -Raw | ConvertFrom-Json)

    $summary.failed_stage = 'restore-write'
    $summary.restore_write = Set-BundleState -Mode restore
    $summary.failed_stage = 'restore-reboot'
    Restart-Guest
    $summary.restore_after_boot = Read-BundleState -Tag 'restore-after-boot'

    $summary.status = 'ok'
    $summary.failed_stage = $null
}
catch {
    $summary.status = 'failed'
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $testName -Symptom $_.Exception.Message -Notes "Behavior lane failed during stage $($summary.failed_stage). Recovered by snapshot revert."
}
finally {
    if ($needsRecovery -and -not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        try {
            Restore-HealthySnapshot
            $recoveredShell = Get-ShellHealth
            $summary.recovery.performed = $true
            $summary.recovery.shell_healthy_after_recovery = [bool]($recoveredShell.explorer -and $recoveredShell.sihost -and $recoveredShell.shellhost)
        }
        catch {
            $summary.errors += "Recovery failed: $($_.Exception.Message)"
        }
    }

    $summary.generated_utc = [DateTime]::UtcNow.ToString('o')
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
}

$summary | ConvertTo-Json -Depth 8
if ($summary.status -ne 'ok') {
    exit 1
}
