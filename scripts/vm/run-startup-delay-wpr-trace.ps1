[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\startup-delay-wpr',
    [string]$RecordId = 'system.disable-startup-delay',
    [string]$SnapshotName = '',
    [string]$IncidentLogPath = '',
    [int]$ExplorerSettleSeconds = 20
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName -Fallback 'baseline-20260325-shell-stable'
}

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputName = "startup-delay-wpr-$stamp"
$hostRoot = Join-Path $HostOutputRoot $outputName
$guestRoot = Join-Path $GuestOutputRoot $outputName
$hostPayloadPath = Join-Path $hostRoot 'startup-delay-wpr-payload.ps1'
$guestPayloadPath = Join-Path $GuestScriptRoot 'startup-delay-wpr-payload.ps1'
$summaryPath = Join-Path $hostRoot 'startup-delay-wpr-summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('missing', '0')]
    [string]$State,

    [Parameter(Mandatory = $true)]
    [string]$GuestOutputRoot,

    [Parameter(Mandatory = $true)]
    [int]$ExplorerSettleSeconds
)

$ErrorActionPreference = 'Stop'

$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize'
$valueName = 'StartupDelayInMSec'
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$stateRoot = Join-Path $GuestOutputRoot $State
$etlPath = Join-Path $stateRoot ("startup-delay-{0}.etl" -f $State)
$summaryPath = Join-Path $stateRoot ("startup-delay-{0}.summary.json" -f $State)

function Get-ValueState {
    try {
        $item = Get-ItemProperty -Path $registryPath -Name $valueName -ErrorAction Stop
        return [ordered]@{
            path_exists = $true
            value_exists = $true
            value = $item.$valueName
        }
    }
    catch {
        return [ordered]@{
            path_exists = [bool](Test-Path $registryPath)
            value_exists = $false
            value = $null
        }
    }
}

function Restore-ValueState {
    param([hashtable]$Original)

    if (-not $Original.path_exists) {
        if (Test-Path $registryPath) {
            Remove-Item -Path $registryPath -Recurse -Force -ErrorAction SilentlyContinue
        }
        return
    }

    New-Item -Path $registryPath -Force | Out-Null
    if ($Original.value_exists) {
        New-ItemProperty -Path $registryPath -Name $valueName -PropertyType DWord -Value ([int]$Original.value) -Force | Out-Null
    }
    else {
        Remove-ItemProperty -Path $registryPath -Name $valueName -ErrorAction SilentlyContinue
    }
}

function Get-ShellState {
    $names = @('explorer', 'sihost', 'ShellHost', 'ctfmon')
    $result = [ordered]@{}
    foreach ($name in $names) {
        $result[$name] = [bool](Get-Process -Name $name -ErrorAction SilentlyContinue)
    }
    return $result
}

function Invoke-NativeProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList,
        [switch]$IgnoreExitCode
    )

    $proc = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -Wait -PassThru -WindowStyle Hidden
    if (-not $IgnoreExitCode -and $proc.ExitCode -ne 0) {
        throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $($proc.ExitCode)"
    }

    return $proc.ExitCode
}

New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
$original = Get-ValueState
$beforeShell = Get-ShellState

if ($State -eq 'missing') {
    if (-not (Test-Path $registryPath)) {
        New-Item -Path $registryPath -Force | Out-Null
    }
    Remove-ItemProperty -Path $registryPath -Name $valueName -ErrorAction SilentlyContinue
}
else {
    New-Item -Path $registryPath -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name $valueName -PropertyType DWord -Value 0 -Force | Out-Null
}

$applied = Get-ValueState

Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-cancel') -IgnoreExitCode | Out-Null
Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-start', 'GeneralProfile', '-filemode') | Out-Null

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
    Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Start-Process explorer.exe | Out-Null
    Start-Sleep -Seconds $ExplorerSettleSeconds
}
finally {
    $stopwatch.Stop()
    Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-stop', $etlPath) | Out-Null
}

$afterShell = Get-ShellState
Restore-ValueState -Original $original
$restored = Get-ValueState

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    state = $State
    registry_path = $registryPath
    value_name = $valueName
    before = $original
    applied = $applied
    restored = $restored
    explorer_restart_duration_seconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
    shell_before = $beforeShell
    shell_after = $afterShell
    etl_exists = (Test-Path $etlPath)
    etl_path = $etlPath
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
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

    throw 'Guest did not reach a ready VMware Tools state in time.'
}

function Get-ShellHealth {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1')
}

function Log-Incident {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestId,
        [Parameter(Mandatory = $true)]
        [string]$ValueState,
        [Parameter(Mandatory = $true)]
        [string]$Symptom,
        [bool]$ShellRecovered = $false
    )

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId $RecordId `
        -TweakId $RecordId `
        -TestId $TestId `
        -Family 'Explorer startup behavior' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize' `
        -ValueName 'StartupDelayInMSec' `
        -ValueState $ValueState `
        -Symptom $Symptom `
        -ShellRecovered:$ShellRecovered `
        -NeededSnapshotRevert:$false `
        -IncidentPath $IncidentLogPath | Out-Null
}

function Run-GuestStateTrace {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('missing', '0')]
        [string]$State
    )

    $guestStateRoot = Join-Path $guestRoot $State
    $hostStateRoot = Join-Path $hostRoot $State
    New-Item -ItemType Directory -Path $hostStateRoot -Force | Out-Null

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CreateDirectoryInGuest', $VmPath, $guestStateRoot) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'runProgramInGuest', $VmPath, 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $guestPayloadPath, '-State', $State, '-GuestOutputRoot', $guestRoot, '-ExplorerSettleSeconds', "$ExplorerSettleSeconds") | Out-Null

    $guestSummaryPath = Join-Path $guestStateRoot ("startup-delay-{0}.summary.json" -f $State)
    $guestEtlPath = Join-Path $guestStateRoot ("startup-delay-{0}.etl" -f $State)
    $hostSummaryPath = Join-Path $hostStateRoot ("startup-delay-{0}.summary.json" -f $State)
    $hostEtlPath = Join-Path $hostStateRoot ("startup-delay-{0}.etl" -f $State)

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestSummaryPath, $hostSummaryPath) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestEtlPath, $hostEtlPath) | Out-Null

    $shellHealthPath = Join-Path $hostStateRoot 'shell-health.json'
    Get-ShellHealth | Set-Content -Path $shellHealthPath -Encoding UTF8
    $summary = Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json
    $shellHealth = Get-Content -Path $shellHealthPath -Raw | ConvertFrom-Json

    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.ShellHost)) {
        Log-Incident -TestId ("startup-delay-wpr-{0}" -f $State) -ValueState $State -Symptom 'Explorer shell restart did not recover cleanly after startup-delay WPR trace.' -ShellRecovered:$false
    }

    return [ordered]@{
        state = $State
        summary_path = $hostSummaryPath
        etl_path = $hostEtlPath
        shell_health_path = $shellHealthPath
        summary = $summary
        shell_health = $shellHealth
    }
}

Wait-GuestReady
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CreateDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostPayloadPath, $guestPayloadPath) | Out-Null

$results = @(
    Run-GuestStateTrace -State 'missing'
    Run-GuestStateTrace -State '0'
)

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    output_name = $outputName
    host_root = $hostRoot
    guest_root = $guestRoot
    record_id = $RecordId
    snapshot_name = $SnapshotName
    traces = $results
} | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8

Write-Output $summaryPath

