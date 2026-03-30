[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SourceSnapshotName = '',
    [string]$TargetSnapshotName = 'RegProbe-Baseline-ToolsHardened-20260330',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\RegProbe-Diag\vmtools-hardening',
    [string]$AuditLabel = 'vm-baseline-tools-hardened-20260330'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

if ([string]::IsNullOrWhiteSpace($SourceSnapshotName)) {
    $SourceSnapshotName = Resolve-DefaultVmSnapshotName
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$auditSessionRoot = Join-Path $auditRoot $AuditLabel
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("regprobe-tools-hardened-baseline-$stamp")
$hardeningHostScript = Join-Path $PSScriptRoot 'apply-vmtools-hardening.ps1'
$hardeningGuestScript = Join-Path $GuestScriptRoot 'apply-vmtools-hardening.ps1'
$toolingDiagnosticHostScript = Join-Path $PSScriptRoot 'run-vm-tooling-minimal-diagnostic.ps1'
$artifactAuditHostScript = Join-Path $PSScriptRoot 'run-guest-app-artifact-audit.ps1'
$shellHealthScript = Join-Path $PSScriptRoot 'get-vm-shell-health.ps1'

$guestApplyJson = Join-Path $GuestOutputRoot 'apply.json'
$guestReadJson = Join-Path $GuestOutputRoot 'read.json'
$applyJsonPath = Join-Path $auditSessionRoot 'vmtools-hardening-apply.json'
$readJsonPath = Join-Path $auditSessionRoot 'vmtools-hardening-read.json'
$shellBeforeSnapshotPath = Join-Path $auditSessionRoot 'shell-before-snapshot.json'
$shellAfterValidationPath = Join-Path $auditSessionRoot 'shell-after-validation.json'
$baselineAuditPath = Join-Path $auditSessionRoot 'baseline-app-artifacts-audit.json'
$validationAuditPath = Join-Path $auditSessionRoot 'validation-app-artifacts-audit.json'
$snapshotsBeforePath = Join-Path $auditSessionRoot 'snapshots-before.txt'
$snapshotsAfterPath = Join-Path $auditSessionRoot 'snapshots-after.txt'
$auditPath = Join-Path $auditRoot 'regprobe-baseline-tools-hardened-20260330.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $auditRoot -Force | Out-Null
New-Item -ItemType Directory -Path $auditSessionRoot -Force | Out-Null

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Get-SnapshotNames {
    $raw = Invoke-Vmrun -Arguments @('-T', 'ws', 'listSnapshots', $VmPath)
    $snapshots = @(
        $raw -split "`r?`n" |
        Where-Object { $_ -and $_ -notmatch '^Total snapshots:' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
    )

    return [pscustomobject]@{
        raw = $raw
        names = $snapshots
    }
}

function Save-SnapshotList {
    param([string]$Path, [string[]]$Snapshots)
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
    param([string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'createDirectoryInGuest', $VmPath, $GuestPath
    ) -IgnoreExitCode | Out-Null
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

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

function Revert-AndStartVm {
    param([string]$SnapshotName)

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') -IgnoreExitCode | Out-Null
    Wait-GuestReady
}

function Get-ShellHealthJson {
    $raw = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword

    return $raw | ConvertFrom-Json
}

function Remove-SnapshotIfExists {
    param([string]$SnapshotName)

    $snapshots = (Get-SnapshotNames).names
    if ($snapshots -contains $SnapshotName) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'deleteSnapshot', $VmPath, $SnapshotName) | Out-Null
        return $true
    }

    return $false
}

function Invoke-AppArtifactAudit {
    param([string]$Mode, [string]$OutputPath, [switch]$RequireClean, [string]$SnapshotName = '')

    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $artifactAuditHostScript,
        '-VmPath', $VmPath,
        '-VmrunPath', $VmrunPath,
        '-GuestUser', $GuestUser,
        '-GuestPassword', $GuestPassword,
        '-Mode', $Mode,
        '-OutputPath', $OutputPath
    )
    if ($RequireClean) {
        $arguments += '-RequireClean'
    }
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        $arguments += @('-SnapshotName', $SnapshotName)
    }

    & powershell.exe @arguments | Out-Null
    return Get-Content -Path $OutputPath -Raw | ConvertFrom-Json
}

function Invoke-ToolingDiagnostic {
    param([string]$SnapshotName = '')

    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $toolingDiagnosticHostScript,
        '-VmPath', $VmPath,
        '-VmrunPath', $VmrunPath,
        '-GuestUser', $GuestUser,
        '-GuestPassword', $GuestPassword,
        '-TrackedOutputRoot', $auditSessionRoot
    )
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        $arguments += @('-SnapshotName', $SnapshotName)
    }

    $trackedSummary = (& powershell.exe @arguments).Trim()
    return [pscustomobject]@{
        summary_path = $trackedSummary
        summary = (Get-Content -Path $trackedSummary -Raw | ConvertFrom-Json)
    }
}

function Invoke-VmtoolsHardeningMode {
    param([ValidateSet('apply', 'read')][string]$Mode, [string]$GuestJsonPath, [string]$HostJsonPath)

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $hardeningGuestScript,
        '-Mode', $Mode,
        '-OutputPath', $GuestJsonPath
    )

    Copy-FromGuest -GuestPath $GuestJsonPath -HostPath $HostJsonPath
    $payload = Get-Content -Path $HostJsonPath -Raw | ConvertFrom-Json
    if ($payload.status -ne 'ok') {
        throw "VMware Tools hardening $Mode failed: $((@($payload.errors) -join '; '))"
    }

    return $payload
}

$beforeSnapshots = Get-SnapshotNames
Save-SnapshotList -Path $snapshotsBeforePath -Snapshots $beforeSnapshots.names

$audit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    source_snapshot = $SourceSnapshotName
    target_snapshot = $TargetSnapshotName
    audit_session_root = $auditSessionRoot
    policy = [ordered]@{
        baseline = 'tooling-first, app-free, VMware Tools hardened'
        app_smoke = 'ephemeral deploy -> validate -> cleanup only'
        defender = 'keep tooling exclusions intact; do not disable Defender'
    }
    validation = [ordered]@{}
    snapshot_lists = [ordered]@{
        before = @($beforeSnapshots.names)
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
    Copy-ToGuest -HostPath $hardeningHostScript -GuestPath $hardeningGuestScript

    $shellBefore = Get-ShellHealthJson
    $shellBefore | ConvertTo-Json -Depth 5 | Set-Content -Path $shellBeforeSnapshotPath -Encoding UTF8
    $audit.validation.shell_before_snapshot = [ordered]@{
        path = $shellBeforeSnapshotPath
        shell_healthy = [bool]$shellBefore.shell_healthy
    }

    $baselineAudit = Invoke-AppArtifactAudit -Mode 'audit' -OutputPath $baselineAuditPath -RequireClean
    $audit.validation.baseline_app_artifacts = [ordered]@{
        path = $baselineAuditPath
        status = $baselineAudit.status
        policy_compliant = [bool]$baselineAudit.policy_compliant
    }

    $applyResult = Invoke-VmtoolsHardeningMode -Mode 'apply' -GuestJsonPath $guestApplyJson -HostJsonPath $applyJsonPath
    $audit.validation.vmtools_hardening_apply = [ordered]@{
        path = $applyJsonPath
        status = $applyResult.status
        service_name = $applyResult.after.service_name
        vmtoolsd_priority = $applyResult.after.vmtoolsd_priority
    }

    $readResult = Invoke-VmtoolsHardeningMode -Mode 'read' -GuestJsonPath $guestReadJson -HostJsonPath $readJsonPath
    $audit.validation.vmtools_hardening_read = [ordered]@{
        path = $readJsonPath
        status = $readResult.status
        service_name = $readResult.after.service_name
        vmtoolsd_priority = $readResult.after.vmtoolsd_priority
    }

    $tooling = Invoke-ToolingDiagnostic
    $audit.validation.tooling_minimal = [ordered]@{
        path = $tooling.summary_path
        status = $tooling.summary.status
        write_test_exists = [bool]$tooling.summary.write_test_exists
    }

    Remove-SnapshotIfExists -SnapshotName $TargetSnapshotName | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'snapshot', $VmPath, $TargetSnapshotName) | Out-Null

    $validationAudit = Invoke-AppArtifactAudit -Mode 'audit' -OutputPath $validationAuditPath -RequireClean -SnapshotName $TargetSnapshotName
    $audit.validation.validation_app_artifacts = [ordered]@{
        path = $validationAuditPath
        status = $validationAudit.status
        policy_compliant = [bool]$validationAudit.policy_compliant
    }

    $validationTooling = Invoke-ToolingDiagnostic -SnapshotName $TargetSnapshotName
    $audit.validation.validation_tooling_minimal = [ordered]@{
        path = $validationTooling.summary_path
        status = $validationTooling.summary.status
        write_test_exists = [bool]$validationTooling.summary.write_test_exists
    }

    Revert-AndStartVm -SnapshotName $TargetSnapshotName
    $shellAfter = Get-ShellHealthJson
    $shellAfter | ConvertTo-Json -Depth 5 | Set-Content -Path $shellAfterValidationPath -Encoding UTF8
    $audit.validation.shell_after_validation = [ordered]@{
        path = $shellAfterValidationPath
        shell_healthy = [bool]$shellAfter.shell_healthy
    }

    if ($SourceSnapshotName -ne $TargetSnapshotName) {
        if (Remove-SnapshotIfExists -SnapshotName $SourceSnapshotName) {
            $audit.deleted_snapshots += $SourceSnapshotName
        }
    }

    $afterSnapshots = Get-SnapshotNames
    Save-SnapshotList -Path $snapshotsAfterPath -Snapshots $afterSnapshots.names
    $audit.snapshot_lists.after = @($afterSnapshots.names)
    $audit.status = 'ok'
}
catch {
    $audit.status = 'error'
    $audit.errors += $_.Exception.Message
    throw
}
finally {
    $audit | ConvertTo-Json -Depth 8 | Set-Content -Path $auditPath -Encoding UTF8
}

Write-Output $auditPath
