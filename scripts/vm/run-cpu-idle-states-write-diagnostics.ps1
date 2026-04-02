[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\cpu-idle-write-diag',
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

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "cpu-idle-write-diagnostics-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'cpu-idle-write-diagnostics-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'cpu-idle-write-diagnostics-payload.ps1'
$hostWrapperPath = Join-Path $hostRoot 'cpu-idle-write-diagnostics-wrapper.ps1'
$guestWrapperPath = Join-Path $guestRoot 'cpu-idle-write-diagnostics-wrapper.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$GuestRoot
)

$ErrorActionPreference = 'Continue'
$registryPath = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power'
$regExePath = 'HKLM\SYSTEM\CurrentControlSet\Control\Power'
$valueMap = [ordered]@{
    DisableIdleStatesAtBoot = 1
    IdleStateTimeout = 0
    ExitLatencyCheckEnabled = 1
}
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$results = New-Object System.Collections.Generic.List[object]

function Get-BundleState {
    $state = [ordered]@{}
    foreach ($name in $valueMap.Keys) {
        try {
            $state[$name] = (Get-ItemProperty -Path $registryPath -Name $name -ErrorAction Stop).$name
        }
        catch {
            $state[$name] = $null
        }
    }
    return $state
}

function Reset-Bundle {
    foreach ($name in $valueMap.Keys) {
        Remove-ItemProperty -Path $registryPath -Name $name -ErrorAction SilentlyContinue
    }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    $entry = [ordered]@{
        label = $Label
        generated_utc = [DateTime]::UtcNow.ToString('o')
        before = Get-BundleState
        ok = $false
        exit_code = 0
        error = $null
        after = $null
    }

    try {
        & $Action
        $entry.ok = $true
    }
    catch {
        $entry.ok = $false
        $entry.error = $_.Exception.Message
        if ($LASTEXITCODE) {
            $entry.exit_code = $LASTEXITCODE
        }
    }
    finally {
        $entry.after = Get-BundleState
        $results.Add([pscustomobject]$entry) | Out-Null
    }
}

function Write-WithProvider {
    New-Item -Path $registryPath -Force | Out-Null
    foreach ($name in $valueMap.Keys) {
        New-ItemProperty -Path $registryPath -Name $name -Value $valueMap[$name] -PropertyType DWord -Force | Out-Null
    }
}

function Write-WithRegExe {
    foreach ($name in $valueMap.Keys) {
        & reg.exe add $regExePath /v $name /t REG_DWORD /d $valueMap[$name] /f | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "reg.exe add failed for $name with exit code $LASTEXITCODE"
        }
    }
}

Reset-Bundle
Invoke-Step -Label 'provider-no-wpr' -Action { Write-WithProvider }
Reset-Bundle
Invoke-Step -Label 'regexe-no-wpr' -Action { Write-WithRegExe }
Reset-Bundle
Invoke-Step -Label 'wpr-start' -Action {
    & $wpr -cancel | Out-Null
    & $wpr -start GeneralProfile -filemode | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "wpr start failed with exit code $LASTEXITCODE"
    }
}
Invoke-Step -Label 'provider-after-wpr' -Action { Write-WithProvider }
Reset-Bundle
Invoke-Step -Label 'regexe-after-wpr' -Action { Write-WithRegExe }
Invoke-Step -Label 'wpr-stop' -Action {
    $etlPath = Join-Path $GuestRoot 'cpu-idle-write-diag.etl'
    & $wpr -stop $etlPath | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "wpr stop failed with exit code $LASTEXITCODE"
    }
}
Reset-Bundle

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    last_boot_utc = (Get-CimInstance -ClassName Win32_OperatingSystem).LastBootUpTime.ToUniversalTime().ToString('o')
    final_state = Get-BundleState
    results = @($results)
} | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

$guestWrapper = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$PayloadPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,

    [Parameter(Mandatory = $true)]
    [string]$WrapperErrorPath
)

$ErrorActionPreference = 'Continue'

try {
    & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -File $PayloadPath -OutputPath $OutputPath -GuestRoot $GuestRoot
    if ($LASTEXITCODE -ne 0) {
        @(
            ('INNER_EXIT=' + $LASTEXITCODE),
            ('PAYLOAD=' + $PayloadPath),
            ('OUTPUT=' + $OutputPath)
        ) | Set-Content -Path $WrapperErrorPath -Encoding UTF8
    }
}
catch {
    @(
        ('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message),
        ('AT=' + $_.InvocationInfo.PositionMessage)
    ) | Set-Content -Path $WrapperErrorPath -Encoding UTF8
}

exit 0
'@

Set-Content -Path $hostWrapperPath -Value $guestWrapper -Encoding UTF8

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

function Ensure-GuestDirectory {
    param([string]$GuestPath)

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'createDirectoryInGuest', $VmPath, $GuestPath
        ) | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'already exists') {
            throw
        }
    }
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

function Get-ShellHealth {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1') `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword
}

function Wait-ShellHealthy {
    param([int]$TimeoutSeconds = 240)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $health = Get-ShellHealth | ConvertFrom-Json
        if ($health.shell_healthy) {
            return $health
        }
        Start-Sleep -Seconds 5
    }

    throw 'Guest reached VMware Tools ready state, but the shell did not become healthy in time.'
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath
    ) | Out-Null
}

function Copy-FromGuest {
    param([string]$GuestPath, [string]$HostPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
    ) | Out-Null
}

function Copy-FromGuestBestEffort {
    param([string]$GuestPath, [string]$HostPath)

    try {
        Copy-FromGuest -GuestPath $GuestPath -HostPath $HostPath
        return $true
    }
    catch {
        return $false
    }
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

function Restore-HealthySnapshot {
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        return
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-ShellHealthy | Out-Null
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
        -Family 'power-runtime-diagnostics' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
        -ValueName 'DisableIdleStatesAtBoot + IdleStateTimeout + ExitLatencyCheckEnabled' `
        -ValueState 'write-diagnostics' `
        -Symptom $Symptom `
        -ShellRecovered:$false `
        -NeededSnapshotRevert:$true `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    record_id = $RecordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = 'started'
    shell_before = $null
    shell_after = $null
    result = $null
    errors = @()
    recovery = [ordered]@{
        performed = $false
        shell_healthy_after_recovery = $false
    }
}

$probeFailed = $false
$needsRecovery = $false

try {
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    $summary.shell_before = Wait-ShellHealthy

    Ensure-GuestDirectory -GuestPath $GuestOutputRoot
    Ensure-GuestDirectory -GuestPath $guestRoot
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath
    Copy-ToGuest -HostPath $hostWrapperPath -GuestPath $guestWrapperPath

    $guestResultPath = Join-Path $guestRoot 'write-diagnostics.json'
    $hostResultPath = Join-Path $hostRoot 'write-diagnostics.json'
    $guestWrapperErrorPath = Join-Path $guestRoot 'wrapper-error.txt'
    $hostWrapperErrorPath = Join-Path $hostRoot 'wrapper-error.txt'

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestWrapperPath,
        '-PayloadPath', $guestPayloadPath,
        '-OutputPath', $guestResultPath,
        '-GuestRoot', $guestRoot,
        '-WrapperErrorPath', $guestWrapperErrorPath
    )

    $resultCopied = Copy-FromGuestBestEffort -GuestPath $guestResultPath -HostPath $hostResultPath
    $wrapperCopied = Copy-FromGuestBestEffort -GuestPath $guestWrapperErrorPath -HostPath $hostWrapperErrorPath

    if ($resultCopied) {
        Copy-Item -Path $hostResultPath -Destination (Join-Path $repoRootOut 'write-diagnostics.json') -Force
        $summary.result = Get-Content -Path $hostResultPath -Raw | ConvertFrom-Json
    }

    if ($wrapperCopied) {
        Copy-Item -Path $hostWrapperErrorPath -Destination (Join-Path $repoRootOut 'wrapper-error.txt') -Force
    }

    if (-not $resultCopied) {
        $wrapperMessage = if ($wrapperCopied) { Get-Content -Path $hostWrapperErrorPath -Raw } else { 'no wrapper error file' }
        throw "Write diagnostics did not produce a guest result file. Wrapper detail: $wrapperMessage"
    }

    $summary.shell_after = Wait-ShellHealthy
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -Symptom $_.Exception.Message -Notes 'CPU idle write diagnostics failed before a healthy completion and required snapshot recovery.'
}
finally {
    if ($needsRecovery -and -not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        try {
            Restore-HealthySnapshot
            $recoveredShell = Get-ShellHealth | ConvertFrom-Json
            $summary.recovery.performed = $true
            $summary.recovery.shell_healthy_after_recovery = [bool]$recoveredShell.shell_healthy
        }
        catch {
            $summary.errors += "Recovery failed: $($_.Exception.Message)"
        }
    }
}

$summary.status = if ($probeFailed) { 'failed' } else { 'ok' }
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSummaryPath -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}

