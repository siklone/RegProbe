[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TestId,

    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$ValueName,

    [Parameter(Mandatory = $true)]
    [string]$ValueType,

    [Parameter(Mandatory = $true)]
    [object]$CandidateValue,

    [Parameter(Mandatory = $true)]
    [string]$BenchmarkCommand,

    [ValidateSet('none', 'reboot')]
    [string]$RestartMode = 'reboot',

    [int]$WarmupRuns = 1,
    [int]$MeasuredRuns = 3,
    [int]$IdleTimeoutSeconds = 180,
    [int]$IdleCpuThreshold = 20,
    [int]$IdleDiskThreshold = 20,
    [int]$IdleWindowSeconds = 30,
    [int]$PollSeconds = 5,
    [int]$PhaseTimeoutMinutes = 30,
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'codexvm',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SharedHostRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestWorkRoot = 'C:\Tools\ValidationController',
    [string]$SnapshotName
)

$ErrorActionPreference = 'Stop'
$controllerRoot = Join-Path $SharedHostRoot 'controller\current'
$configPath = Join-Path $controllerRoot 'config.json'
$statusPath = Join-Path $controllerRoot 'status.json'
$resultPath = Join-Path $controllerRoot 'result.json'
$hostArtifactsPath = Join-Path $controllerRoot 'artifacts'

$guestTestDir = Join-Path $GuestWorkRoot 'controller\current'
$guestConfigPath = Join-Path $guestTestDir 'config.json'
$guestStatusPath = Join-Path $guestTestDir 'status.json'
$guestResultPath = Join-Path $guestTestDir 'result.json'
$guestAgentLogPath = Join-Path $guestTestDir 'agent.log'
$guestArtifactsPath = Join-Path $guestTestDir 'artifacts'
$guestScriptsRoot = 'C:\Tools\Scripts'
$guestPrepCmdPath = Join-Path $guestScriptsRoot 'codex-controller-prep.cmd'
$guestLaunchCmdPath = Join-Path $guestScriptsRoot 'codex-controller-launch.cmd'

function Invoke-Vmrun {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $VmrunPath @Arguments
}

function Write-ControllerFeedback {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Kind,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $timestamp = Get-Date -Format 'HH:mm:ss'
    Write-Host "[$timestamp] ${Kind}: $Message"
}

function Sync-GuestFileToHost {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPath,

        [Parameter(Mandatory = $true)]
        [string]$HostPath
    )

    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath) | Out-Null
        return $true
    } catch {
        return $false
    }
}

function Sync-GuestArtifactsToHost {
    try {
        $listing = Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'listDirectoryInGuest', $VmPath, $guestArtifactsPath) | Out-String
    } catch {
        return
    }

    $items = $listing -split "`r?`n" | Select-Object -Skip 1 | Where-Object { $_.Trim() }
    foreach ($item in $items) {
        $name = $item.Trim()
        $guestFile = Join-Path $guestArtifactsPath $name
        $hostFile = Join-Path $hostArtifactsPath $name
        Sync-GuestFileToHost -GuestPath $guestFile -HostPath $hostFile | Out-Null
    }
}

function Start-GuestValidationAgent {
    Write-ControllerFeedback -Kind 'live' -Message 'Starting guest validation agent.'
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'runProgramInGuest', $VmPath, 'C:\Windows\System32\cmd.exe', '/c', $guestLaunchCmdPath) | Out-Null
}

if (Test-Path $controllerRoot) {
    Remove-Item -Path $controllerRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $controllerRoot | Out-Null
New-Item -ItemType Directory -Force -Path $hostArtifactsPath | Out-Null

$config = [ordered]@{
    test_id = $TestId
    registry_path = $RegistryPath
    value_name = $ValueName
    value_type = $ValueType
    candidate_value = $CandidateValue
    benchmark_command = $BenchmarkCommand
    restart_mode = $RestartMode
    warmup_runs = $WarmupRuns
    measured_runs = $MeasuredRuns
    idle_timeout_seconds = $IdleTimeoutSeconds
    idle_cpu_threshold = $IdleCpuThreshold
    idle_disk_threshold = $IdleDiskThreshold
    idle_window_seconds = $IdleWindowSeconds
}
$config | ConvertTo-Json -Depth 6 | Set-Content -Path $configPath -Encoding UTF8

Write-ControllerFeedback -Kind 'started' -Message "Prepared test '$TestId'."

if ($SnapshotName) {
    Write-ControllerFeedback -Kind 'live' -Message "Reverting snapshot '$SnapshotName'."
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName, 'nogui') | Out-Null
}

$running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') | Out-String
if ($running -notmatch [regex]::Escape($VmPath)) {
    Write-ControllerFeedback -Kind 'live' -Message 'Starting VM.'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') | Out-Null
}

$toolsDeadline = (Get-Date).AddMinutes(5)
do {
    Start-Sleep -Seconds 3
    $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath) | Out-String
} until ($state -match 'running|installed' -or (Get-Date) -ge $toolsDeadline)

Write-ControllerFeedback -Kind 'live' -Message "VMware Tools state: $($state.Trim())"

Write-ControllerFeedback -Kind 'live' -Message 'Preparing guest-local controller workspace.'
$guestPrepHostCmd = Join-Path $controllerRoot 'guest-prep.cmd'
@(
    '@echo off',
    ('if exist "{0}" rmdir /s /q "{0}"' -f $guestTestDir),
    ('mkdir "{0}"' -f $guestArtifactsPath)
) | Set-Content -Path $guestPrepHostCmd -Encoding ASCII
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $guestPrepHostCmd, $guestPrepCmdPath) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'runProgramInGuest', $VmPath, 'C:\Windows\System32\cmd.exe', '/c', $guestPrepCmdPath) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $configPath, $guestConfigPath) | Out-Null

$guestLaunchHostCmd = Join-Path $controllerRoot 'guest-launch.cmd'
@(
    '@echo off',
    ('start "" /b "{0}" -NoProfile -ExecutionPolicy Bypass -File "{1}" -SharedRoot "{2}"' -f 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe', 'C:\Tools\Scripts\guest-validation-agent.ps1', $GuestWorkRoot)
) | Set-Content -Path $guestLaunchHostCmd -Encoding ASCII
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $guestLaunchHostCmd, $guestLaunchCmdPath) | Out-Null
Start-GuestValidationAgent

$phaseDeadline = (Get-Date).AddMinutes($PhaseTimeoutMinutes)
$initialStatusDeadline = (Get-Date).AddSeconds(90)
$lastPhase = ''
$statusSeen = $false
while ((Get-Date) -lt $phaseDeadline) {
    Start-Sleep -Seconds $PollSeconds

    Sync-GuestFileToHost -GuestPath $guestStatusPath -HostPath $statusPath | Out-Null
    Sync-GuestFileToHost -GuestPath $guestResultPath -HostPath $resultPath | Out-Null
    Sync-GuestFileToHost -GuestPath $guestAgentLogPath -HostPath (Join-Path $controllerRoot 'agent.log') | Out-Null

    if (Test-Path $statusPath) {
        $statusSeen = $true
        $status = Get-Content -Path $statusPath -Raw | ConvertFrom-Json
        if ($status.phase -ne $lastPhase) {
            $lastPhase = $status.phase
            $detail = if ($status.PSObject.Properties.Name -contains 'last_detail') { $status.last_detail } else { '' }
            Write-ControllerFeedback -Kind 'live' -Message "$($status.phase) $detail".Trim()
        }

        if ($status.phase -eq 'COMPLETE') {
            Write-ControllerFeedback -Kind 'done' -Message "Test '$TestId' completed."
            break
        }

        if ($status.phase -eq 'ERROR') {
            $detail = if ($status.errors) { ($status.errors | Select-Object -Last 1) } else { 'Unknown error.' }
            throw "Guest agent reported ERROR: $detail"
        }

        if ($status.phase -like 'RESTART_*') {
            Write-ControllerFeedback -Kind 'live' -Message 'Guest restart requested; waiting for the VM to come back.'
            Invoke-Vmrun -Arguments @('-T', 'ws', 'rebootGuest', $VmPath) | Out-Null
            $toolsBackDeadline = (Get-Date).AddMinutes(10)
            do {
                Start-Sleep -Seconds 5
                $toolsState = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath) | Out-String
            } until ($toolsState -match 'running|installed' -or (Get-Date) -ge $toolsBackDeadline)
            Write-ControllerFeedback -Kind 'live' -Message "Post-reboot Tools state: $($toolsState.Trim())"
            Start-GuestValidationAgent
        }
    } elseif (-not $statusSeen -and (Get-Date) -ge $initialStatusDeadline) {
        Write-ControllerFeedback -Kind 'blocked' -Message 'Guest agent did not produce an initial status within 90 seconds.'
        exit 1
    }
}

if (-not (Test-Path $resultPath)) {
    Write-ControllerFeedback -Kind 'blocked' -Message 'Result file not produced yet.'
    exit 1
}

Sync-GuestArtifactsToHost

$result = Get-Content -Path $resultPath -Raw | ConvertFrom-Json
$measured = @($result.benchmark_runs | Where-Object { $_.kind -eq 'measured' })
if ($measured.Count -gt 0) {
    $durations = $measured | ForEach-Object { [double]$_.duration_seconds }
    $avg = [math]::Round((($durations | Measure-Object -Average).Average), 2)
    $min = [math]::Round((($durations | Measure-Object -Minimum).Minimum), 2)
    $max = [math]::Round((($durations | Measure-Object -Maximum).Maximum), 2)
    Write-ControllerFeedback -Kind 'done' -Message "Measured runs: avg=$avg s min=$min s max=$max s"
}

Write-ControllerFeedback -Kind 'next' -Message "Artifacts: $controllerRoot"
