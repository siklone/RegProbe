[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$SourceSnapshotName = 'RegProbe-Baseline-20260328',
    [string]$TargetSnapshotName = 'RegProbe-Baseline-Clean-20260329',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Temp\baseline-output',
    [string]$AuditLabel = 'vm-baseline-clean-20260329'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$auditSessionRoot = Join-Path $auditRoot $AuditLabel
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("regprobe-clean-baseline-$stamp")

$hostDefenderScript = Join-Path $PSScriptRoot 'apply-defender-tooling-exclusions.ps1'
$guestDefenderScript = Join-Path $GuestScriptRoot 'apply-defender-tooling-exclusions.ps1'
$artifactAuditHostScript = Join-Path $PSScriptRoot 'run-guest-app-artifact-audit.ps1'
$toolingDiagnosticHostScript = Join-Path $PSScriptRoot 'run-vm-tooling-minimal-diagnostic.ps1'
$appSmokeHostScript = Join-Path $PSScriptRoot 'run-app-launch-smoke-host.ps1'

$guestApplyJson = Join-Path $GuestOutputRoot 'tooling-defender-exclusions-apply.json'
$guestReadJson = Join-Path $GuestOutputRoot 'tooling-defender-exclusions-read.json'

$applyJsonPath = Join-Path $auditSessionRoot 'tooling-defender-exclusions-apply.json'
$readJsonPath = Join-Path $auditSessionRoot 'tooling-defender-exclusions-read.json'
$finalReadJsonPath = Join-Path $auditSessionRoot 'tooling-defender-exclusions-final-read.json'
$sourceCleanupPath = Join-Path $auditSessionRoot 'source-app-artifacts-cleanup.json'
$baselineAuditPath = Join-Path $auditSessionRoot 'baseline-app-artifacts-audit.json'
$afterSmokeAuditPath = Join-Path $auditSessionRoot 'after-smoke-app-artifacts-audit.json'
$finalRevertAuditPath = Join-Path $auditSessionRoot 'final-revert-app-artifacts-audit.json'
$appSmokePath = Join-Path $auditSessionRoot 'app-launch-smoke.json'
$appSmokePreCleanupPath = Join-Path $auditSessionRoot 'app-launch-smoke-pre-cleanup.json'
$appSmokePostCleanupPath = Join-Path $auditSessionRoot 'app-launch-smoke-post-cleanup.json'
$shellBeforeSnapshotPath = Join-Path $auditSessionRoot 'shell-before-snapshot.json'
$shellAfterSmokePath = Join-Path $auditSessionRoot 'shell-after-smoke.json'
$shellAfterFinalRevertPath = Join-Path $auditSessionRoot 'shell-after-final-revert.json'
$snapshotsBeforePath = Join-Path $auditSessionRoot 'snapshots-before.txt'
$snapshotsAfterPath = Join-Path $auditSessionRoot 'snapshots-after.txt'
$auditPath = Join-Path $auditRoot 'regprobe-baseline-clean-20260329.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $auditRoot -Force | Out-Null
New-Item -ItemType Directory -Path $auditSessionRoot -Force | Out-Null

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

    $snapshots = (Get-SnapshotNames).names
    if ($snapshots -contains $SnapshotName) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'deleteSnapshot', $VmPath, $SnapshotName) | Out-Null
        return $true
    }

    return $false
}

function Remove-GuestPath {
    param([Parameter(Mandatory = $true)][string]$GuestPath)

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command', "if (Test-Path -LiteralPath '$GuestPath') { Remove-Item -LiteralPath '$GuestPath' -Recurse -Force -ErrorAction SilentlyContinue }"
    )
}

function Invoke-AppArtifactAudit {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('audit', 'cleanup')][string]$Mode,
        [Parameter(Mandatory = $true)][string]$OutputPath,
        [switch]$RequireClean,
        [string]$GuestOutputRootOverride = ''
    )

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

    if (-not [string]::IsNullOrWhiteSpace($GuestOutputRootOverride)) {
        $arguments += @('-GuestOutputRoot', $GuestOutputRootOverride)
    }

    & powershell.exe @arguments | Out-Null
    return Get-Content -Path $OutputPath -Raw | ConvertFrom-Json
}

function Invoke-DefenderExclusionMode {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('apply', 'read')][string]$Mode,
        [Parameter(Mandatory = $true)][string]$GuestJsonPath,
        [Parameter(Mandatory = $true)][string]$HostJsonPath
    )

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestDefenderScript,
        '-Mode', $Mode,
        '-OutputPath', $GuestJsonPath
    )

    Copy-FromGuest -GuestPath $GuestJsonPath -HostPath $HostJsonPath
    $payload = Get-Content -Path $HostJsonPath -Raw | ConvertFrom-Json
    if ($payload.status -ne 'ok') {
        throw ("Defender exclusion {0} failed: {1}" -f $Mode, (($payload.errors | Where-Object { $_ }) -join '; '))
    }

    return $payload
}

function Convert-AppArtifactSummary {
    param(
        [Parameter(Mandatory = $true)]$Result,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $state =
        if ($Result.mode -eq 'cleanup') {
            $Result.post_cleanup
        }
        else {
            $Result.pre_cleanup
        }

    return [ordered]@{
        path = $Path
        mode = $Result.mode
        status = $Result.status
        policy_compliant = [bool]$Result.policy_compliant
        stale_binary_count = if ($state) { [int]$state.stale_binary_count } else { -1 }
        residual_item_count = if ($state) { [int]$state.residual_item_count } else { -1 }
    }
}

function Convert-DefenderSummary {
    param(
        [Parameter(Mandatory = $true)]$Result,
        [Parameter(Mandatory = $true)][string]$Path
    )

    return [ordered]@{
        path = $Path
        status = $Result.status
        exclusion_paths = @($Result.after.exclusion_paths)
        exclusion_processes = @($Result.after.exclusion_processes)
        execution_policy = $Result.after.execution_policy
    }
}

function Convert-ShellSummary {
    param(
        [Parameter(Mandatory = $true)]$ShellHealth,
        [Parameter(Mandatory = $true)][string]$Path
    )

    return [ordered]@{
        path = $Path
        shell_healthy = [bool]$ShellHealth.shell_healthy
        explorer = [bool]$ShellHealth.checks.explorer
        sihost = [bool]$ShellHealth.checks.sihost
        shellhost = [bool]$ShellHealth.checks.shellhost
        ctfmon = [bool]$ShellHealth.checks.ctfmon
    }
}

function Convert-ToolingSummary {
    param(
        [Parameter(Mandatory = $true)]$Summary,
        [Parameter(Mandatory = $true)][string]$Path
    )

    return [ordered]@{
        path = $Path
        status = $Summary.status
        write_test_exists = [bool]$Summary.write_test_exists
        procmon_paths = @($Summary.procmon_paths)
        defender = $Summary.defender
    }
}

function Convert-AppSmokeSummary {
    param(
        [Parameter(Mandatory = $true)]$Result,
        [Parameter(Mandatory = $true)][string]$Path
    )

    return [ordered]@{
        path = $Path
        process_started = [bool]$Result.process_started
        process_alive_after_12s = [bool]$Result.process_alive_after_12s
        cleanup_verified = [bool]$Result.cleanup_verified
        executable = $Result.executable
    }
}

$beforeSnapshotInfo = Get-SnapshotNames
$beforeSnapshotInfo.raw | Set-Content -Path $snapshotsBeforePath -Encoding UTF8
$deletedSnapshots = New-Object 'System.Collections.Generic.List[string]'

$audit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    source_snapshot = $SourceSnapshotName
    target_snapshot = $TargetSnapshotName
    audit_session_root = $auditSessionRoot
    policy = [ordered]@{
        baseline = 'tooling-first and app-free'
        app_smoke = 'ephemeral deploy -> validate -> cleanup only'
        defender = 'keep tooling exclusions intact; do not disable Defender'
    }
    validation = [ordered]@{
        source_cleanup = $null
        baseline_audit = $null
        tooling_minimal = $null
        app_launch_smoke = $null
        app_launch_smoke_pre_cleanup = $null
        app_launch_smoke_post_cleanup = $null
        after_smoke_audit = $null
        final_revert_audit = $null
        shell_before_snapshot = $null
        shell_after_smoke = $null
        shell_after_final_revert = $null
    }
    defender_exclusions = [ordered]@{
        apply = $null
        readback = $null
        final_readback = $null
    }
    snapshot_lists = [ordered]@{
        before = @($beforeSnapshotInfo.names)
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
    Copy-ToGuest -HostPath $hostDefenderScript -GuestPath $guestDefenderScript

    $sourceCleanup = Invoke-AppArtifactAudit -Mode cleanup -OutputPath $sourceCleanupPath -RequireClean -GuestOutputRootOverride 'C:\Tools\Temp\baseline-source-artifact-audit'
    $audit.validation.source_cleanup = Convert-AppArtifactSummary -Result $sourceCleanup -Path $sourceCleanupPath
    Remove-GuestPath -GuestPath 'C:\Tools\Temp\baseline-source-artifact-audit'

    $applyPayload = Invoke-DefenderExclusionMode -Mode apply -GuestJsonPath $guestApplyJson -HostJsonPath $applyJsonPath
    $audit.defender_exclusions.apply = Convert-DefenderSummary -Result $applyPayload -Path $applyJsonPath
    Remove-GuestPath -GuestPath $GuestOutputRoot

    $shellBeforeSnapshot = Get-ShellHealthJson
    $shellBeforeSnapshot | ConvertTo-Json -Depth 6 | Set-Content -Path $shellBeforeSnapshotPath -Encoding UTF8
    $audit.validation.shell_before_snapshot = Convert-ShellSummary -ShellHealth $shellBeforeSnapshot -Path $shellBeforeSnapshotPath
    if (-not $shellBeforeSnapshot.shell_healthy) {
        throw 'Shell health check failed before creating the clean baseline snapshot.'
    }

    Remove-SnapshotIfExists -SnapshotName $TargetSnapshotName | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'snapshot', $VmPath, $TargetSnapshotName) | Out-Null

    Revert-AndStartVm -SnapshotName $TargetSnapshotName
    $baselineAudit = Invoke-AppArtifactAudit -Mode audit -OutputPath $baselineAuditPath -RequireClean -GuestOutputRootOverride 'C:\Tools\Temp\baseline-audit'
    $audit.validation.baseline_audit = Convert-AppArtifactSummary -Result $baselineAudit -Path $baselineAuditPath
    Remove-GuestPath -GuestPath 'C:\Tools\Temp\baseline-audit'

    $readPayload = Invoke-DefenderExclusionMode -Mode read -GuestJsonPath $guestReadJson -HostJsonPath $readJsonPath
    $audit.defender_exclusions.readback = Convert-DefenderSummary -Result $readPayload -Path $readJsonPath
    Remove-GuestPath -GuestPath $GuestOutputRoot

    $toolingSummaryPath = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $toolingDiagnosticHostScript `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword `
        -SnapshotName $TargetSnapshotName `
        -TrackedOutputRoot $auditSessionRoot).Trim()
    $toolingSummary = Get-Content -Path $toolingSummaryPath -Raw | ConvertFrom-Json
    if ($toolingSummary.status -ne 'ok') {
        throw 'Minimal tooling diagnostic did not complete successfully.'
    }
    $audit.validation.tooling_minimal = Convert-ToolingSummary -Summary $toolingSummary -Path $toolingSummaryPath

    Revert-AndStartVm -SnapshotName $TargetSnapshotName
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $appSmokeHostScript `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword `
        -OutputPath $appSmokePath `
        -PreCleanupOutputPath $appSmokePreCleanupPath `
        -PostCleanupOutputPath $appSmokePostCleanupPath | Out-Null

    $appSmoke = Get-Content -Path $appSmokePath -Raw | ConvertFrom-Json
    $appSmokePreCleanup = Get-Content -Path $appSmokePreCleanupPath -Raw | ConvertFrom-Json
    $appSmokePostCleanup = Get-Content -Path $appSmokePostCleanupPath -Raw | ConvertFrom-Json
    $audit.validation.app_launch_smoke = Convert-AppSmokeSummary -Result $appSmoke -Path $appSmokePath
    $audit.validation.app_launch_smoke_pre_cleanup = Convert-AppArtifactSummary -Result $appSmokePreCleanup -Path $appSmokePreCleanupPath
    $audit.validation.app_launch_smoke_post_cleanup = Convert-AppArtifactSummary -Result $appSmokePostCleanup -Path $appSmokePostCleanupPath

    $shellAfterSmoke = Get-ShellHealthJson
    $shellAfterSmoke | ConvertTo-Json -Depth 6 | Set-Content -Path $shellAfterSmokePath -Encoding UTF8
    $audit.validation.shell_after_smoke = Convert-ShellSummary -ShellHealth $shellAfterSmoke -Path $shellAfterSmokePath
    if (-not $shellAfterSmoke.shell_healthy) {
        throw 'Shell health check failed after the app smoke lane.'
    }

    $afterSmokeAudit = Invoke-AppArtifactAudit -Mode audit -OutputPath $afterSmokeAuditPath -RequireClean -GuestOutputRootOverride 'C:\Tools\Temp\after-smoke-audit'
    $audit.validation.after_smoke_audit = Convert-AppArtifactSummary -Result $afterSmokeAudit -Path $afterSmokeAuditPath
    Remove-GuestPath -GuestPath 'C:\Tools\Temp\after-smoke-audit'

    Revert-AndStartVm -SnapshotName $TargetSnapshotName
    $finalReadPayload = Invoke-DefenderExclusionMode -Mode read -GuestJsonPath $guestReadJson -HostJsonPath $finalReadJsonPath
    $audit.defender_exclusions.final_readback = Convert-DefenderSummary -Result $finalReadPayload -Path $finalReadJsonPath
    Remove-GuestPath -GuestPath $GuestOutputRoot

    $finalRevertAudit = Invoke-AppArtifactAudit -Mode audit -OutputPath $finalRevertAuditPath -RequireClean -GuestOutputRootOverride 'C:\Tools\Temp\final-revert-audit'
    $audit.validation.final_revert_audit = Convert-AppArtifactSummary -Result $finalRevertAudit -Path $finalRevertAuditPath
    Remove-GuestPath -GuestPath 'C:\Tools\Temp\final-revert-audit'

    $shellAfterFinalRevert = Get-ShellHealthJson
    $shellAfterFinalRevert | ConvertTo-Json -Depth 6 | Set-Content -Path $shellAfterFinalRevertPath -Encoding UTF8
    $audit.validation.shell_after_final_revert = Convert-ShellSummary -ShellHealth $shellAfterFinalRevert -Path $shellAfterFinalRevertPath
    if (-not $shellAfterFinalRevert.shell_healthy) {
        throw 'Shell health check failed after the final clean-baseline revert.'
    }

    if ($SourceSnapshotName -ne $TargetSnapshotName) {
        if (Remove-SnapshotIfExists -SnapshotName $SourceSnapshotName) {
            $deletedSnapshots.Add($SourceSnapshotName)
        }
    }

    $afterSnapshotInfo = Get-SnapshotNames
    Save-SnapshotList -Path $snapshotsAfterPath -Snapshots $afterSnapshotInfo.names
    $audit.snapshot_lists.after = @($afterSnapshotInfo.names)
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
        throw 'Clean baseline creation failed for an unknown reason.'
    }

    throw ($errorList -join '; ')
}

Write-Output $auditPath

