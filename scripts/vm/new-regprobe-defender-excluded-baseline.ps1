[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SourceSnapshotName = 'baseline-20260327-regprobe-visible-shell-stable',
    [string]$TargetSnapshotName = 'RegProbe-Baseline-20260328',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\RegProbe-Diag\baseline-output'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$sessionName = "regprobe-baseline-setup-$stamp"
$hostRoot = Join-Path $HostOutputRoot $sessionName
$repoRootOut = Join-Path $repoEvidenceRoot $sessionName
$hostGuestScript = Join-Path $PSScriptRoot 'apply-defender-tooling-exclusions.ps1'
$guestGuestScript = Join-Path $GuestScriptRoot 'apply-defender-tooling-exclusions.ps1'
$guestApplyJson = Join-Path $GuestOutputRoot 'tooling-defender-exclusions-apply.json'
$guestReadJson = Join-Path $GuestOutputRoot 'tooling-defender-exclusions-read.json'
$repoApplyJson = Join-Path $repoRootOut 'tooling-defender-exclusions-apply.json'
$repoReadJson = Join-Path $repoRootOut 'tooling-defender-exclusions-read.json'
$repoShellHealthJson = Join-Path $repoRootOut 'shell-health.json'
$repoSnapshotListBefore = Join-Path $repoRootOut 'snapshots-before.txt'
$repoSnapshotListAfter = Join-Path $repoRootOut 'snapshots-after.txt'
$auditPath = Join-Path $auditRoot 'regprobe-baseline-defender-exclusions-20260328.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null
New-Item -ItemType Directory -Path $auditRoot -Force | Out-Null

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

function Get-SnapshotNames {
    $raw = Invoke-Vmrun -Arguments @('-T', 'ws', 'listSnapshots', $VmPath)
    $raw | Set-Content -Path $repoSnapshotListBefore -Encoding UTF8
    return @(
        $raw -split "`r?`n" |
        Where-Object { $_ -and $_ -notmatch '^Total snapshots:' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
    )
}

function Save-SnapshotList {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string[]]$Snapshots
    )

    ($Snapshots -join [Environment]::NewLine) | Set-Content -Path $Path -Encoding UTF8
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
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'runProgramInGuest',
        $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

function Revert-AndStartVm {
    param([Parameter(Mandatory = $true)][string]$SnapshotName)

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') -IgnoreExitCode | Out-Null
    Wait-GuestReady
}

function Get-ShellHealthJson {
    $raw = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'get-vm-shell-health.ps1') `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword
    return $raw | ConvertFrom-Json
}

function Remove-SnapshotIfExists {
    param([Parameter(Mandatory = $true)][string]$SnapshotName)

    $snapshots = @(
        Invoke-Vmrun -Arguments @('-T', 'ws', 'listSnapshots', $VmPath) -IgnoreExitCode -split "`r?`n" |
        Where-Object { $_ -and $_ -notmatch '^Total snapshots:' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
    )

    if ($snapshots -contains $SnapshotName) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'deleteSnapshot', $VmPath, $SnapshotName) | Out-Null
    }
}

$beforeSnapshots = Get-SnapshotNames
$deletedSnapshots = New-Object 'System.Collections.Generic.List[string]'
$audit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    source_snapshot = $SourceSnapshotName
    target_snapshot = $TargetSnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$sessionName"
    validation = [ordered]@{
        tooling_minimal = $null
        app_launch_smoke = $null
        shell_health = $null
    }
    defender_exclusions = [ordered]@{
        apply = $null
        readback = $null
    }
    snapshot_lists = [ordered]@{
        before = $beforeSnapshots
        after = @()
    }
    deleted_snapshots = @()
    status = 'started'
    errors = @()
}

try {
    Revert-AndStartVm -SnapshotName $SourceSnapshotName
    Ensure-GuestDirectory -GuestPath $GuestScriptRoot
    Ensure-GuestDirectory -GuestPath $GuestOutputRoot
    Copy-ToGuest -HostPath $hostGuestScript -GuestPath $guestGuestScript

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestGuestScript,
        '-Mode', 'apply',
        '-OutputPath', $guestApplyJson
    )
    Copy-FromGuest -GuestPath $guestApplyJson -HostPath $repoApplyJson
    $audit.defender_exclusions.apply = Get-Content -Path $repoApplyJson -Raw | ConvertFrom-Json
    if ($audit.defender_exclusions.apply.status -ne 'ok') {
        throw ("Defender exclusion apply failed: {0}" -f (($audit.defender_exclusions.apply.errors | Where-Object { $_ }) -join '; '))
    }

    $preSnapshotShell = Get-ShellHealthJson
    if (-not $preSnapshotShell.shell_healthy) {
        throw 'Shell health check failed before creating the Defender-excluded baseline snapshot.'
    }

    Remove-SnapshotIfExists -SnapshotName $TargetSnapshotName
    Invoke-Vmrun -Arguments @('-T', 'ws', 'snapshot', $VmPath, $TargetSnapshotName) | Out-Null

    Revert-AndStartVm -SnapshotName $TargetSnapshotName

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestGuestScript,
        '-Mode', 'read',
        '-OutputPath', $guestReadJson
    )
    Copy-FromGuest -GuestPath $guestReadJson -HostPath $repoReadJson
    $audit.defender_exclusions.readback = Get-Content -Path $repoReadJson -Raw | ConvertFrom-Json
    if ($audit.defender_exclusions.readback.status -ne 'ok') {
        throw ("Defender exclusion readback failed: {0}" -f (($audit.defender_exclusions.readback.errors | Where-Object { $_ }) -join '; '))
    }

    $toolingSummaryPath = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'run-vm-tooling-minimal-diagnostic.ps1') `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword `
        -SnapshotName $TargetSnapshotName
    $audit.validation.tooling_minimal = $toolingSummaryPath.Trim()

    Revert-AndStartVm -SnapshotName $TargetSnapshotName
    $appSmokePath = Join-Path $repoRootOut 'app-launch-smoke.json'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'run-app-launch-smoke-host.ps1') `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword `
        -OutputPath $appSmokePath | Out-Null
    $audit.validation.app_launch_smoke = "evidence/files/vm-tooling-staging/$sessionName/app-launch-smoke.json"

    $shellHealth = Get-ShellHealthJson
    $shellHealth | ConvertTo-Json -Depth 6 | Set-Content -Path $repoShellHealthJson -Encoding UTF8
    $audit.validation.shell_health = "evidence/files/vm-tooling-staging/$sessionName/shell-health.json"
    if (-not $shellHealth.shell_healthy) {
        throw 'Shell health check failed after validating the Defender-excluded baseline.'
    }

    foreach ($snapshot in (Get-LegacyVmSnapshotNames)) {
        if ($snapshot -eq $TargetSnapshotName) {
            continue
        }

        Invoke-Vmrun -Arguments @('-T', 'ws', 'deleteSnapshot', $VmPath, $snapshot) | Out-Null
        $deletedSnapshots.Add($snapshot)
    }

    $afterSnapshots = @(
        Invoke-Vmrun -Arguments @('-T', 'ws', 'listSnapshots', $VmPath) -split "`r?`n" |
        Where-Object { $_ -and $_ -notmatch '^Total snapshots:' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
    )
    Save-SnapshotList -Path $repoSnapshotListAfter -Snapshots $afterSnapshots
    $audit.snapshot_lists.after = $afterSnapshots
    $audit.deleted_snapshots = @($deletedSnapshots)
    $audit.status = 'ok'
}
catch {
    $audit.errors += $_.Exception.Message
    $audit.deleted_snapshots = @($deletedSnapshots)
    $audit.status = 'failed'
}

$audit | ConvertTo-Json -Depth 10 | Set-Content -Path $auditPath -Encoding UTF8

if ($audit.status -ne 'ok') {
    $errorList = @($audit.errors | Where-Object { $_ })
    if ($errorList.Count -eq 0) {
        throw 'Defender-excluded baseline creation failed for an unknown reason.'
    }

    throw ($errorList -join '; ')
}
