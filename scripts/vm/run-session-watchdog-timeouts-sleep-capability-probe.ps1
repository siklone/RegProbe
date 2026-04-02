[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\watchdog-sleep-capability',
    [string]$SnapshotName = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "watchdog-sleep-capability-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName

$hostPayloadPath = Join-Path $hostRoot 'watchdog-sleep-capability-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'watchdog-sleep-capability-payload.ps1'
$guestSummaryPath = Join-Path $guestRoot 'summary.json'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'
$guestPowerCfgPath = Join-Path $guestRoot 'powercfg-a.txt'
$hostPowerCfgPath = Join-Path $hostRoot 'powercfg-a.txt'
$repoPowerCfgPath = Join-Path $repoRootOut 'powercfg-a.txt'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null

$cmd = 'powercfg /a > "{0}" 2>&1' -f $OutputPath
& cmd.exe /c $cmd | Out-Null
$exitCode = $LASTEXITCODE

$text = if (Test-Path $OutputPath) {
    [System.IO.File]::ReadAllText($OutputPath)
} else {
    ''
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    lane_label = 'power.session-watchdog-timeouts'
    probe = 'sleep-capability'
    powercfg_exit_code = $exitCode
    output_path = $OutputPath
    output_exists = [bool](Test-Path $OutputPath)
    sleep_states_available = ($text -match 'The following sleep states are available')
    sleep_states_unavailable = ($text -match 'The following sleep states are not available')
    output_excerpt = if ($text.Length -gt 600) { $text.Substring(0, 600) } else { $text }
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $SummaryPath -Encoding UTF8
exit 0
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
    param([int]$TimeoutSeconds = 300)

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

        Start-Sleep -Seconds 3
    }

    throw 'Guest did not reach a running VMware Tools state in time.'
}

function Ensure-VmRunning {
    $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
    if ($running -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) | Out-Null
    }
}

Ensure-VmRunning
Wait-GuestReady

$shellBefore = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1')

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostPayloadPath, $guestPayloadPath) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $guestPayloadPath,
    '-OutputPath', $guestPowerCfgPath,
    '-SummaryPath', $guestSummaryPath
) | Out-Null

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestSummaryPath, $hostSummaryPath) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestPowerCfgPath, $hostPowerCfgPath) | Out-Null

Copy-Item -Path $hostSummaryPath -Destination $repoSummaryPath -Force
Copy-Item -Path $hostPowerCfgPath -Destination $repoPowerCfgPath -Force

$summary = Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json
$shellAfter = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1')

$result = [ordered]@{
    probe_name = $probeName
    host_output_root = $hostRoot
    repo_output_root = $repoRootOut
    shell_before = $shellBefore
    shell_after = $shellAfter
    powercfg = $summary
}

$result | ConvertTo-Json -Depth 8

