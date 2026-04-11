[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [string]$PipeName = '\\.\pipe\regprobe_debug',
    [ValidateSet('server', 'client')]
    [string]$PipeEndpoint = 'server',
    [ValidateSet('TRUE', 'FALSE')]
    [string]$TryNoRxLoss = 'TRUE',
    [int]$DebugPort = 1,
    [int]$BaudRate = 115200,
    [string]$CreateSnapshotName = '',
    [string]$OutputPath = '',
    [int]$VmToolsTimeoutSeconds = 600,
    [int]$ShellHealthTimeoutSeconds = 1200,
    [int]$PollIntervalSeconds = 10,
    [switch]$RevertDebugSettings,
    [switch]$ForceWithoutSnapshot,
    [switch]$SkipVmxUpdate,
    [switch]$SkipGuestBootConfig
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $PSScriptRoot '_vmrun-common.ps1')
$resolverPath = Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1'
if (Test-Path -LiteralPath $resolverPath) {
    . $resolverPath
    if ([string]::IsNullOrWhiteSpace($VmPath)) {
        $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile
    }
}

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx'
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$InputObject,
        [int]$Depth = 8
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    return Invoke-RegProbeVmrun -VmrunPath $VmrunPath -Arguments $Arguments -IgnoreExitCode:$IgnoreExitCode
}

function Test-VmRunning {
    $listOutput = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') -IgnoreExitCode
    return ($listOutput -match [regex]::Escape($VmPath))
}

function Set-VmxKeyValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Key,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "VMX file not found: $Path"
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in (Get-Content -LiteralPath $Path)) {
        [void]$lines.Add([string]$line)
    }

    $pattern = '^{0}\s*=' -f [regex]::Escape($Key)
    $replacement = '{0} = "{1}"' -f $Key, $Value
    $updated = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $pattern) {
            $lines[$i] = $replacement
            $updated = $true
            break
        }
    }

    if (-not $updated) {
        [void]$lines.Add($replacement)
    }

    Set-Content -LiteralPath $Path -Value $lines -Encoding ASCII
}

function Invoke-GuestCmd {
    param([string]$Command)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws'
    ) + $guestAuthArgs + @(
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\cmd.exe',
        '/c', $Command
    )) | Out-Null
}

function Invoke-GuestProgram {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$ArgumentList = @()
    )

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws'
    ) + $guestAuthArgs + @(
        'runProgramInGuest', $vmxPath,
        $FilePath
    ) + $ArgumentList) | Out-Null
}

function Get-VmToolsState {
    try {
        return (Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $vmxPath) -IgnoreExitCode).Trim()
    }
    catch {
        return 'query-failed'
    }
}

function Get-ShellHealthSnapshot {
    $processText = ''
    $processQueryError = $null

    try {
        $processText = Invoke-Vmrun -Arguments (@('-T', 'ws') + $guestAuthArgs + @('listProcessesInGuest', $vmxPath))
    }
    catch {
        $processQueryError = $_.Exception.Message
    }

    $checks = [ordered]@{
        explorer = [bool]($processText -match '\bexplorer\.exe\b')
        sihost = [bool]($processText -match '\bsihost\.exe\b')
        shellhost = [bool]($processText -match '\bShellHost\.exe\b')
        ctfmon = [bool]($processText -match '\bctfmon\.exe\b')
    }

    return [ordered]@{
        process_query_error = $processQueryError
        shell_healthy = ($checks.explorer -and $checks.sihost -and $checks.shellhost)
        checks = $checks
    }
}

function Wait-ForVmToolsReady {
    param([int]$TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds([Math]::Max(30, $TimeoutSeconds))
    do {
        if (-not (Test-VmRunning)) {
            Start-Sleep -Seconds $PollIntervalSeconds
            continue
        }

        $toolsState = Get-VmToolsState
        if ($toolsState -match 'running|installed') {
            return $toolsState
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for VMware Tools readiness after $TimeoutSeconds seconds."
}

function Wait-ForVmStopped {
    param([int]$TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds([Math]::Max(30, $TimeoutSeconds))
    do {
        if (-not (Test-VmRunning)) {
            return $true
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for the VM to stop after $TimeoutSeconds seconds."
}

function Wait-ForShellHealthy {
    param([int]$TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds([Math]::Max(30, $TimeoutSeconds))
    do {
        $snapshot = Get-ShellHealthSnapshot
        if ($snapshot.shell_healthy) {
            return $snapshot
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for shell health after $TimeoutSeconds seconds."
}

function Request-GuestRestartAsync {
    $restartCommand = @'
Start-Process -FilePath "$env:SystemRoot\System32\shutdown.exe" -ArgumentList "/r","/t","0","/f" -WindowStyle Hidden
Start-Sleep -Milliseconds 250
'@
    Invoke-Vmrun -Arguments (@(
        '-T', 'ws'
    ) + $guestAuthArgs + @(
        'runProgramInGuest', $vmxPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command', $restartCommand
    )) -IgnoreExitCode | Out-Null
}

$vmxPath = [System.IO.Path]::GetFullPath($VmPath)
$resolvedVmProfile = if ([string]::IsNullOrWhiteSpace($VmProfile)) { 'primary' } else { $VmProfile }
$resolvedSnapshotName = if ([string]::IsNullOrWhiteSpace($CreateSnapshotName)) { $null } else { $CreateSnapshotName }
$mutationRequested = (-not $SkipVmxUpdate) -or (-not $SkipGuestBootConfig) -or $RevertDebugSettings

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_profile = $resolvedVmProfile
    vm_path = $vmxPath
    vm_running = $false
    vm_started = $false
    vm_power_cycled = $false
    pipe_name = $PipeName
    pipe_endpoint = $PipeEndpoint
    try_no_rx_loss = $TryNoRxLoss
    debug_port = $DebugPort
    baud_rate = $BaudRate
    vmx_updated = $false
    guest_boot_debug_updated = $false
    guest_rebooted = $false
    tools_state = $null
    shell_healthy = $false
    shell_checks = $null
    snapshot_created = $false
    snapshot_existing = $false
    snapshot_name = $resolvedSnapshotName
    snapshot_gate_enforced = $true
    snapshot_gate_passed = $false
    force_without_snapshot = [bool]$ForceWithoutSnapshot
    revert_guard_requested = [bool]$RevertDebugSettings
    revert_guard_applied = $false
    guest_boot_debug_reverted = $false
    vmx_serial_pipe_reverted = $false
    steps = @()
    status = 'staged'
}

if ($RevertDebugSettings -and $SkipGuestBootConfig) {
    $result.status = 'blocked-invalid-revert-mode'
    $result.error = 'RevertDebugSettings requires guest boot configuration access.'
    if ($OutputPath) {
        Write-JsonFile -Path $OutputPath -InputObject $result
    }
    $result | ConvertTo-Json -Depth 8
    return
}

if ($mutationRequested -and [string]::IsNullOrWhiteSpace($resolvedSnapshotName) -and -not $ForceWithoutSnapshot) {
    $result.status = 'blocked-snapshot-gate'
    $result.error = 'Snapshot gate refused to mutate kernel debug settings without CreateSnapshotName. Pass -ForceWithoutSnapshot only for explicit emergency use.'
    $result.steps += 'snapshot-gate-blocked'
    if ($OutputPath) {
        Write-JsonFile -Path $OutputPath -InputObject $result
    }
    $result | ConvertTo-Json -Depth 8
    return
}

$result.snapshot_gate_passed = $true
$guestCredential = Resolve-RegProbeVmCredential -GuestUser $GuestUser -GuestPassword $GuestPassword -CredentialFilePath $CredentialFilePath
$guestAuthArgs = Get-RegProbeVmrunAuthArguments -Credential $guestCredential

try {
    $result.vm_running = Test-VmRunning

    if (-not $SkipVmxUpdate -and $result.vm_running) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $vmxPath, 'hard') -IgnoreExitCode | Out-Null
        Wait-ForVmStopped -TimeoutSeconds 180 | Out-Null
        $result.vm_running = $false
        $result.vm_power_cycled = $true
        $result.steps += 'vm-stopped-for-vmx-edit'
    }

    if (-not $SkipVmxUpdate) {
        if ($RevertDebugSettings) {
            Set-VmxKeyValue -Path $vmxPath -Key 'serial0.present' -Value 'FALSE'
            $result.vmx_serial_pipe_reverted = $true
            $result.steps += 'vmx-serial-pipe-disabled'
        }
        else {
            Set-VmxKeyValue -Path $vmxPath -Key 'serial0.present' -Value 'TRUE'
            Set-VmxKeyValue -Path $vmxPath -Key 'serial0.fileType' -Value 'pipe'
            Set-VmxKeyValue -Path $vmxPath -Key 'serial0.fileName' -Value $PipeName
            Set-VmxKeyValue -Path $vmxPath -Key 'serial0.tryNoRxLoss' -Value $TryNoRxLoss
            Set-VmxKeyValue -Path $vmxPath -Key 'serial0.pipe.endPoint' -Value $PipeEndpoint
            $result.steps += 'vmx-serial-pipe-configured'
        }
        $result.vmx_updated = $true
    }

    if (-not $SkipGuestBootConfig) {
        if (-not $result.vm_running) {
            Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $vmxPath, 'nogui') | Out-Null
            $result.vm_started = $true
            $result.steps += 'vm-started-nogui'
            Start-Sleep -Seconds 5
            $result.vm_running = Test-VmRunning
        }

        if (-not $result.vm_running) {
            throw 'VM did not reach running state after start request.'
        }

        $result.tools_state = Wait-ForVmToolsReady -TimeoutSeconds $VmToolsTimeoutSeconds
        $result.steps += 'vmtools-ready'
        $shellSnapshot = Wait-ForShellHealthy -TimeoutSeconds $ShellHealthTimeoutSeconds
        $result.shell_healthy = [bool]$shellSnapshot.shell_healthy
        $result.shell_checks = $shellSnapshot.checks
        $result.steps += 'shell-healthy-before-bcdedit'

        if ($RevertDebugSettings) {
            Invoke-GuestProgram -FilePath 'C:\Windows\System32\bcdedit.exe' -ArgumentList @('/debug', 'off')
            $result.guest_boot_debug_reverted = $true
            $result.revert_guard_applied = $true
            $result.steps += 'guest-bcdedit-debug-disabled'
        }
        else {
            Invoke-GuestProgram -FilePath 'C:\Windows\System32\bcdedit.exe' -ArgumentList @('/debug', 'on')
            Invoke-GuestProgram -FilePath 'C:\Windows\System32\bcdedit.exe' -ArgumentList @('/dbgsettings', 'serial', ("debugport:{0}" -f $DebugPort), ("baudrate:{0}" -f $BaudRate))
            $result.guest_boot_debug_updated = $true
            $result.steps += 'guest-bcdedit-debug-enabled'
        }

        Request-GuestRestartAsync
        $result.guest_rebooted = $true
        $result.steps += if ($RevertDebugSettings) { 'guest-restart-requested-for-revert' } else { 'guest-restart-requested' }
        Start-Sleep -Seconds 10

        $result.tools_state = Wait-ForVmToolsReady -TimeoutSeconds $VmToolsTimeoutSeconds
        $result.steps += 'vmtools-ready-after-restart'
        $shellSnapshot = Wait-ForShellHealthy -TimeoutSeconds $ShellHealthTimeoutSeconds
        $result.shell_healthy = [bool]$shellSnapshot.shell_healthy
        $result.shell_checks = $shellSnapshot.checks
        $result.steps += 'shell-healthy-after-restart'
    }

    if (-not [string]::IsNullOrWhiteSpace($CreateSnapshotName)) {
        $existingSnapshots = Invoke-Vmrun -Arguments @('-T', 'ws', 'listSnapshots', $vmxPath) -IgnoreExitCode
        if ($existingSnapshots -match [regex]::Escape($CreateSnapshotName)) {
            $result.snapshot_existing = $true
            $result.steps += 'snapshot-already-present'
        }
        else {
            Invoke-Vmrun -Arguments @('-T', 'ws', 'snapshot', $vmxPath, $CreateSnapshotName) | Out-Null
            $result.snapshot_created = $true
            $result.steps += 'snapshot-created'
        }
    }

    $result.status = if ($RevertDebugSettings -and ($result.guest_boot_debug_reverted -or $result.vmx_serial_pipe_reverted)) {
        'reverted'
    }
    elseif ($result.vmx_updated -or $result.guest_boot_debug_updated -or $result.snapshot_created) {
        'configured'
    }
    else {
        'staged'
    }
}
catch {
    $result.status = 'error'
    $result.error = $_.Exception.Message

    if (-not $RevertDebugSettings -and $result.guest_boot_debug_updated -and -not $result.snapshot_created -and -not $result.snapshot_existing) {
        try {
            if (-not $result.vm_running) {
                Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $vmxPath, 'nogui') -IgnoreExitCode | Out-Null
                Start-Sleep -Seconds 5
                $result.vm_running = Test-VmRunning
            }

            if ($result.vm_running) {
                $result.tools_state = Wait-ForVmToolsReady -TimeoutSeconds $VmToolsTimeoutSeconds
                $shellSnapshot = Wait-ForShellHealthy -TimeoutSeconds $ShellHealthTimeoutSeconds
                $result.shell_healthy = [bool]$shellSnapshot.shell_healthy
                $result.shell_checks = $shellSnapshot.checks
                Invoke-GuestProgram -FilePath 'C:\Windows\System32\bcdedit.exe' -ArgumentList @('/debug', 'off')
                $result.guest_boot_debug_reverted = $true
                $result.revert_guard_applied = $true
                $result.steps += 'revert-guard-debug-disabled'
                Request-GuestRestartAsync
                $result.steps += 'revert-guard-restart-requested'
            }
        }
        catch {
            $result.revert_guard_error = $_.Exception.Message
        }
    }
}

if ($OutputPath) {
    Write-JsonFile -Path $OutputPath -InputObject $result
}

$result | ConvertTo-Json -Depth 8

