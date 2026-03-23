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

    [int]$WarmupRuns = 0,
    [int]$MeasuredRuns = 1,
    [int]$PollSeconds = 5,
    [int]$PhaseTimeoutMinutes = 15,
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SharedHostRoot = 'H:\Temp\vm-tooling-staging'
)

$ErrorActionPreference = 'Stop'

$statusPath = Join-Path $SharedHostRoot 'controller\current\status.json'
$logPath = Join-Path $SharedHostRoot ("{0}.watch.txt" -f $TestId)
$controllerScript = Join-Path $PSScriptRoot 'host-validation-controller.ps1'
$helperScript = 'C:\Tools\Scripts\codex-request-restart.ps1'

function Add-WatchLog {
    param([string]$Message)
    Add-Content -Path $logPath -Value ((Get-Date -Format 'HH:mm:ss') + ' ' + $Message)
}

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output
}

if (Test-Path $logPath) {
    Remove-Item -Path $logPath -Force
}

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
$psi.Arguments = @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', ('"{0}"' -f $controllerScript),
    '-TestId', $TestId,
    '-RegistryPath', ('"{0}"' -f $RegistryPath),
    '-ValueName', $ValueName,
    '-ValueType', $ValueType,
    '-CandidateValue', $CandidateValue,
    '-BenchmarkCommand', ('"{0}"' -f $BenchmarkCommand),
    '-RestartMode', $RestartMode,
    '-WarmupRuns', $WarmupRuns,
    '-MeasuredRuns', $MeasuredRuns,
    '-PollSeconds', $PollSeconds,
    '-PhaseTimeoutMinutes', $PhaseTimeoutMinutes,
    '-GuestUser', $GuestUser,
    '-GuestPassword', ('"{0}"' -f $GuestPassword)
) -join ' '
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($psi)
Add-WatchLog ("controller-pid={0}" -f $process.Id)

$handledRestartPhases = @{}
$deadline = (Get-Date).AddMinutes($PhaseTimeoutMinutes + 5)

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $PollSeconds

    if (-not (Test-Path $statusPath)) {
        continue
    }

    $status = Get-Content -Path $statusPath -Raw | ConvertFrom-Json
    Add-WatchLog ("phase={0}" -f $status.phase)

    if ($status.phase -like 'RESTART_*' -and -not $handledRestartPhases.ContainsKey($status.phase)) {
        Add-WatchLog ("manual-helper={0}" -f $status.phase)
        Invoke-Vmrun -Arguments @(
            '-T', 'ws',
            '-gu', $GuestUser,
            '-gp', $GuestPassword,
            'runProgramInGuest',
            $VmPath,
            'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
            '-NoProfile',
            '-ExecutionPolicy', 'Bypass',
            '-File', $helperScript
        ) | Out-Null
        $handledRestartPhases[$status.phase] = $true
    }

    if ($status.phase -eq 'COMPLETE') {
        Add-WatchLog 'complete'
        break
    }

    if ($status.phase -eq 'ERROR') {
        Add-WatchLog 'error'
        break
    }
}

$process.WaitForExit(30000) | Out-Null
Add-WatchLog ("controller-exit={0}" -f $process.ExitCode)

if (Test-Path $statusPath) {
    Add-WatchLog 'status-json-final'
    Add-Content -Path $logPath -Value (Get-Content -Path $statusPath -Raw)
}

Get-Content -Path $logPath -Raw
