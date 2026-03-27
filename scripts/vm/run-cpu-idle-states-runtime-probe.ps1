[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\cpu-idle-runtime',
    [string]$RecordId = 'power.disable-cpu-idle-states',
    [string]$SnapshotName = 'baseline-20260327-regprobe-visible-shell-stable',
    [string]$IncidentLogPath = '',
    [int]$SettleSeconds = 20
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "cpu-idle-runtime-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'cpu-idle-runtime-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'cpu-idle-runtime-payload.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('set-baseline', 'set-candidate', 'read-state', 'start-wpr', 'stop-wpr')]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$EtlPath = ''
)

$ErrorActionPreference = 'Stop'
$registryPath = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power'
$valueNames = @('DisableIdleStatesAtBoot', 'IdleStateTimeout', 'ExitLatencyCheckEnabled')
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'

function Get-BundleState {
    $result = [ordered]@{}
    foreach ($name in $valueNames) {
        try {
            $value = (Get-ItemProperty -Path $registryPath -Name $name -ErrorAction Stop).$name
            $result[$name] = $value
        }
        catch {
            $result[$name] = $null
        }
    }

    return [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        last_boot_utc = (Get-CimInstance -ClassName Win32_OperatingSystem).LastBootUpTime.ToUniversalTime().ToString('o')
        values = $result
    }
}

function Write-State {
    param([hashtable]$Payload)

    $Payload | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
}

switch ($Action) {
    'set-baseline' {
        foreach ($name in $valueNames) {
            Remove-ItemProperty -Path $registryPath -Name $name -ErrorAction SilentlyContinue
        }
        Write-State (Get-BundleState)
    }
    'set-candidate' {
        New-Item -Path $registryPath -Force | Out-Null
        New-ItemProperty -Path $registryPath -Name 'DisableIdleStatesAtBoot' -Value 1 -PropertyType DWord -Force | Out-Null
        New-ItemProperty -Path $registryPath -Name 'IdleStateTimeout' -Value 0 -PropertyType DWord -Force | Out-Null
        New-ItemProperty -Path $registryPath -Name 'ExitLatencyCheckEnabled' -Value 1 -PropertyType DWord -Force | Out-Null
        Write-State (Get-BundleState)
    }
    'read-state' {
        Write-State (Get-BundleState)
    }
    'start-wpr' {
        & $wpr -cancel | Out-Null
        & $wpr -start GeneralProfile -filemode | Out-Null
        Write-State ([ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            action = 'start-wpr'
            started = $true
        })
    }
    'stop-wpr' {
        if ([string]::IsNullOrWhiteSpace($EtlPath)) {
            throw 'EtlPath is required for stop-wpr.'
        }

        New-Item -ItemType Directory -Path (Split-Path -Parent $EtlPath) -Force | Out-Null
        & $wpr -stop $EtlPath | Out-Null
        Write-State ([ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            action = 'stop-wpr'
            stopped = $true
            etl_path = $EtlPath
            etl_exists = [bool](Test-Path $EtlPath)
        })
    }
}
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

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

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'runProgramInGuest',
        $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromHostToGuest',
        $VmPath,
        $HostPath,
        $GuestPath
    ) | Out-Null
}

function Copy-FromGuest {
    param([string]$GuestPath, [string]$HostPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromGuestToHost',
        $VmPath,
        $GuestPath,
        $HostPath
    ) | Out-Null
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

function Log-Incident {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestId,
        [Parameter(Mandatory = $true)]
        [string]$ValueState,
        [Parameter(Mandatory = $true)]
        [string]$Symptom,
        [bool]$ShellRecovered = $false,
        [bool]$NeededSnapshotRevert = $false,
        [string]$Notes = ''
    )

    & (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId $RecordId `
        -TweakId $RecordId `
        -TestId $TestId `
        -Family 'power-runtime' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
        -ValueName 'DisableIdleStatesAtBoot + IdleStateTimeout + ExitLatencyCheckEnabled' `
        -ValueState $ValueState `
        -Symptom $Symptom `
        -ShellRecovered:$ShellRecovered `
        -NeededSnapshotRevert:$NeededSnapshotRevert `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

function Invoke-GuestAction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Action,
        [string]$EtlPath = '',
        [string]$HostOutputPath = ''
    )

    if ([string]::IsNullOrWhiteSpace($HostOutputPath)) {
        $HostOutputPath = Join-Path $hostRoot "$Action.json"
    }

    $GuestOutputPath = Join-Path $guestRoot ([System.IO.Path]::GetFileName($HostOutputPath))
    $args = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Action', $Action,
        '-OutputPath', $GuestOutputPath
    )
    if (-not [string]::IsNullOrWhiteSpace($EtlPath)) {
        $args += @('-EtlPath', $EtlPath)
    }

    Invoke-GuestPowerShell -ArgumentList $args
    Copy-FromGuest -GuestPath $GuestOutputPath -HostPath $HostOutputPath
    return (Get-Content -Path $HostOutputPath -Raw | ConvertFrom-Json)
}

function Restore-HealthySnapshot {
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        return
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    }
    catch {
    }
    Wait-GuestReady
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    record_id = $RecordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = 'started'
    last_stage = 'initializing'
    failed_stage = $null
    baseline = $null
    candidate_applied = $null
    post_boot = $null
    shell_after = $null
    wpr = [ordered]@{
        started = $false
        stopped = $false
        guest_etl = 'cpu-idle-runtime.etl'
        repo_etl_placeholder = "evidence/files/vm-tooling-staging/$probeName/cpu-idle-runtime.etl.md"
    }
    recovery = [ordered]@{
        performed = $false
        shell_healthy_after_recovery = $false
    }
    errors = @()
}

$probeFailed = $false
$needsRecovery = $false
$incidentNotes = ''

try {
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        $summary.last_stage = 'revert-snapshot'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    }

    $summary.last_stage = 'start-vm'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    $summary.last_stage = 'wait-tools'
    Wait-GuestReady
    $summary.last_stage = 'initial-shell-check'
    $initialShell = Get-ShellHealth
    if (-not ($initialShell.explorer -and $initialShell.sihost -and $initialShell.shellhost)) {
        throw 'Shell health check failed before the runtime probe started.'
    }

    $summary.last_stage = 'prepare-guest-root'
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    $summary.last_stage = 'copy-payload'
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    $summary.last_stage = 'set-baseline'
    $summary.baseline = Invoke-GuestAction -Action 'set-baseline'
    $summary.last_stage = 'start-wpr'
    $summary.wpr.start = Invoke-GuestAction -Action 'start-wpr' -HostOutputPath (Join-Path $hostRoot 'wpr-start.json')
    $summary.wpr.started = $true
    $summary.last_stage = 'set-candidate'
    $summary.candidate_applied = Invoke-GuestAction -Action 'set-candidate'

    $summary.last_stage = 'restart-guest'
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'restartGuest', $VmPath) | Out-Null
    $summary.last_stage = 'wait-tools-post-reboot'
    Wait-GuestReady
    $summary.last_stage = 'settle-post-reboot'
    Start-Sleep -Seconds $SettleSeconds

    $guestEtlPath = Join-Path $guestRoot 'cpu-idle-runtime.etl'
    $summary.last_stage = 'read-post-boot'
    $summary.post_boot = Invoke-GuestAction -Action 'read-state' -HostOutputPath (Join-Path $hostRoot 'post-boot.json')
    $summary.last_stage = 'stop-wpr'
    $summary.wpr.stop = Invoke-GuestAction -Action 'stop-wpr' -EtlPath $guestEtlPath -HostOutputPath (Join-Path $hostRoot 'wpr-stop.json')
    $summary.wpr.stopped = [bool]$summary.wpr.stop.stopped
    $summary.last_stage = 'final-shell-check'
    $summary.shell_after = Get-ShellHealth

    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.shellhost)) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.failed_stage = $summary.last_stage
        $incidentNotes = "Tools returned after reboot, but shell health stayed degraded during stage $($summary.last_stage). Recovered by snapshot revert."
        Log-Incident -TestId $probeName -ValueState 'runtime-lane-post-boot' -Symptom 'Shell health was degraded after the CPU idle runtime probe reboot.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes $incidentNotes
        $summary.errors += 'Shell health was degraded after reboot.'
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.failed_stage = $summary.last_stage
    $summary.errors += $_.Exception.Message
    $incidentNotes = "Runtime lane failed during stage $($summary.last_stage) before a healthy post-boot shell was confirmed. Recovered by snapshot revert."
    Log-Incident -TestId $probeName -ValueState 'runtime-lane-failure' -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes $incidentNotes
}
finally {
    if ($needsRecovery -and -not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        try {
            $summary.last_stage = 'recovery'
            Restore-HealthySnapshot
            $recoveredShell = Get-ShellHealth
            $summary.recovery.performed = $true
            $summary.recovery.shell_healthy_after_recovery = [bool]($recoveredShell.explorer -and $recoveredShell.sihost -and $recoveredShell.shellhost)
        }
        catch {
            $summary.errors += "Recovery failed: $($_.Exception.Message)"
        }
    }
}

$summary.last_stage = 'completed'
$summary.status = if ($probeFailed) { 'failed' } else { 'ok' }
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSummaryPath -Encoding UTF8

$etlPlaceholder = @(
    '# External Evidence Placeholder',
    '',
    'Title: CPU idle runtime WPR trace',
    '',
    'The raw ETL is kept off-git. Use the summary JSON in the same folder and the runtime lane manifest in evidence/records for the machine-readable result.'
) -join "`n"
Set-Content -Path (Join-Path $repoRootOut 'cpu-idle-runtime.etl.md') -Value $etlPlaceholder -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}
