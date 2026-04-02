[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [ValidateSet('audit', 'cleanup')]
    [string]$Mode = 'audit',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\RegProbe-Diag\app-artifact-audit',
    [string]$SnapshotName = '',
    [switch]$RequireClean,
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$sessionName = "guest-app-artifact-$Mode-$stamp"
$hostRoot = Join-Path $HostOutputRoot $sessionName
$guestScriptPath = Join-Path $GuestScriptRoot 'guest-app-artifact-audit.ps1'
$guestResultPath = Join-Path $GuestOutputRoot "guest-app-artifact-$Mode.json"
$hostResultPath =
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        Join-Path $hostRoot "guest-app-artifact-$Mode.json"
    }
    else {
        $OutputPath
    }
$hostScriptPath = Join-Path $PSScriptRoot 'guest-app-artifact-audit.ps1'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $hostResultPath) -Force | Out-Null

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

function Ensure-GuestDirectory {
    param([Parameter(Mandatory = $true)][string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'createDirectoryInGuest', $VmPath, $GuestPath
    ) -IgnoreExitCode | Out-Null
}

function Copy-ToGuest {
    param(
        [Parameter(Mandatory = $true)][string]$HostPath,
        [Parameter(Mandatory = $true)][string]$GuestPath
    )

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath
    ) | Out-Null
}

function Copy-FromGuest {
    param(
        [Parameter(Mandatory = $true)][string]$GuestPath,
        [Parameter(Mandatory = $true)][string]$HostPath
    )

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
    ) | Out-Null
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList)
}

if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
}

$running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
if ($running -notmatch [regex]::Escape($VmPath)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
}

Wait-GuestReady
Ensure-GuestDirectory -GuestPath $GuestScriptRoot
Ensure-GuestDirectory -GuestPath $GuestOutputRoot
Copy-ToGuest -HostPath $hostScriptPath -GuestPath $guestScriptPath

Invoke-GuestPowerShell -ArgumentList @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $guestScriptPath,
    '-Mode', $Mode,
    '-OutputPath', $guestResultPath
) | Out-Null

Copy-FromGuest -GuestPath $guestResultPath -HostPath $hostResultPath

if ($RequireClean) {
    $result = Get-Content -Path $hostResultPath -Raw | ConvertFrom-Json
    if (-not $result.policy_compliant) {
        throw "Guest app artifact audit reported a non-compliant state in mode '$Mode'."
    }

    if ($result.status -eq 'error') {
        throw "Guest app artifact audit returned status '$($result.status)'."
    }
}

Write-Output $hostResultPath

