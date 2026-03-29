[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestRoot = 'C:\RegProbe-Diag',
    [string]$SnapshotName = '',
    [string]$TrackedOutputRoot = ''
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
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "vm-tooling-minimal-diagnostic-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$defaultTrackedRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$trackedRootBase =
    if ([string]::IsNullOrWhiteSpace($TrackedOutputRoot)) {
        $defaultTrackedRoot
    }
    else {
        $TrackedOutputRoot
    }
$trackedRootOut = Join-Path $trackedRootBase $probeName
$hostPayloadPath = Join-Path $hostRoot 'vm-tooling-minimal-payload.ps1'
$guestPayloadPath = Join-Path $GuestRoot 'vm-tooling-minimal-payload.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$trackedSummaryPath = Join-Path $trackedRootOut 'summary.json'
$guestWriteTestPath = Join-Path $GuestRoot 'write-test.txt'
$guestEnvPath = Join-Path $GuestRoot 'environment.json'
$guestScriptResultPath = Join-Path $GuestRoot 'script-result.json'
$hostWriteTestPath = Join-Path $hostRoot 'write-test.txt'
$hostEnvPath = Join-Path $hostRoot 'environment.json'
$hostScriptResultPath = Join-Path $hostRoot 'script-result.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $trackedRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$WriteTestPath,

    [Parameter(Mandatory = $true)]
    [string]$EnvironmentPath,

    [Parameter(Mandatory = $true)]
    [string]$ScriptResultPath
)

$ErrorActionPreference = 'Continue'
$procmonCandidates = @(
    'C:\Tools\Sysinternals\Procmon64.exe',
    'C:\Tools\Procmon.exe',
    'C:\Tools\SysinternalsSuite\Procmon64.exe'
)

try {
    $dir = Split-Path -Parent $WriteTestPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    "DIAGNOSTIC_OK $([DateTime]::UtcNow.ToString('o'))" | Set-Content -Path $WriteTestPath -Encoding UTF8

    $mpPref = $null
    try {
        $mpPref = Get-MpPreference | Select-Object DisableRealtimeMonitoring
    }
    catch {
        $mpPref = [ordered]@{ error = $_.Exception.Message }
    }

    $drive = Get-PSDrive C | Select-Object Used, Free
    $payload = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        write_test_exists = [bool](Test-Path $WriteTestPath)
        execution_policy = Get-ExecutionPolicy
        ps_version = $PSVersionTable.PSVersion.ToString()
        user_name = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        procmon_paths = @(
            $procmonCandidates | ForEach-Object {
                [ordered]@{
                    path = $_
                    exists = [bool](Test-Path $_)
                }
            }
        )
        defender = $mpPref
        drive_c = $drive
    }
    $payload | ConvertTo-Json -Depth 8 | Set-Content -Path $EnvironmentPath -Encoding UTF8

    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        status = 'ok'
        write_test_path = $WriteTestPath
        environment_path = $EnvironmentPath
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $ScriptResultPath -Encoding UTF8
}
catch {
    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        status = 'error'
        error = $_.Exception.Message
        at = $_.InvocationInfo.PositionMessage
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $ScriptResultPath -Encoding UTF8
    exit 1
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

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath
    ) | Out-Null
}

function Copy-FromGuestBestEffort {
    param([string]$GuestPath, [string]$HostPath)

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
            ) | Out-Null

            if (Test-Path $HostPath) {
                return $true
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    }

    return $false
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

Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
Wait-GuestReady
Ensure-GuestDirectory -GuestPath $GuestRoot
Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    tracked_output_root = $trackedRootOut
    guest_root = $GuestRoot
    run_program_exit = $null
    copied = [ordered]@{
        write_test = $false
        environment = $false
        script_result = $false
    }
    script_result_status = $null
    script_result_error = $null
    write_test_exists = $false
    execution_policy = $null
    procmon_paths = @()
    defender = $null
    drive_c = $null
    status = 'started'
    errors = @()
}

try {
    $runOutput = Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-WriteTestPath', $guestWriteTestPath,
        '-EnvironmentPath', $guestEnvPath,
        '-ScriptResultPath', $guestScriptResultPath
    )
    $summary.run_program_exit = $runOutput
}
catch {
    $summary.errors += $_.Exception.Message
}

$summary.copied.write_test = Copy-FromGuestBestEffort -GuestPath $guestWriteTestPath -HostPath $hostWriteTestPath
$summary.copied.environment = Copy-FromGuestBestEffort -GuestPath $guestEnvPath -HostPath $hostEnvPath
$summary.copied.script_result = Copy-FromGuestBestEffort -GuestPath $guestScriptResultPath -HostPath $hostScriptResultPath

if ($summary.copied.script_result) {
    Copy-Item -Path $hostScriptResultPath -Destination (Join-Path $trackedRootOut 'script-result.json') -Force
    $scriptResult = Get-Content -Path $hostScriptResultPath -Raw | ConvertFrom-Json
    $summary.script_result_status = $scriptResult.status
    if ($scriptResult.PSObject.Properties.Name -contains 'error') {
        $summary.script_result_error = $scriptResult.error
    }
}

if ($summary.copied.write_test) {
    Copy-Item -Path $hostWriteTestPath -Destination (Join-Path $trackedRootOut 'write-test.txt') -Force
}

if ($summary.copied.environment) {
    Copy-Item -Path $hostEnvPath -Destination (Join-Path $trackedRootOut 'environment.json') -Force
    $envPayload = Get-Content -Path $hostEnvPath -Raw | ConvertFrom-Json
    $summary.write_test_exists = [bool]$envPayload.write_test_exists
    $summary.execution_policy = $envPayload.execution_policy
    $summary.procmon_paths = @($envPayload.procmon_paths)
    $summary.defender = $envPayload.defender
    $summary.drive_c = $envPayload.drive_c
}

$summary.status =
    if ($summary.copied.write_test -and $summary.copied.environment -and $summary.copied.script_result -and $summary.script_result_status -eq 'ok') {
        'ok'
    }
    else {
        'failed'
    }

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $trackedSummaryPath -Encoding UTF8

Write-Output $trackedSummaryPath
if ($summary.status -ne 'ok') {
    exit 1
}
