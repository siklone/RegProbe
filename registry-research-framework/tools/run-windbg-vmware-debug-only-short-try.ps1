[CmdletBinding()]
param(
    [string]$TargetVmPath = '',
    [string]$TargetVmName = 'Win25H2DebugOnly',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',
    [string]$DebugSnapshotName = 'RegProbe-Debug-VMwareOnly-Baseline-20260403',
    [string]$PipeName = '\\.\pipe\regprobe_debug_vmware_debugonly',
    [ValidateSet('server', 'client')]
    [string]$PipeEndpoint = 'server',
    [ValidateSet('TRUE', 'FALSE')]
    [string]$TryNoRxLoss = 'FALSE',
    [switch]$ProvisionVm,
    [switch]$ReplaceExisting,
    [switch]$PlanOnly,
    [string]$AuditDate = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1')
$planScript = Join-Path $repoRoot 'scripts\vm\new-vmware-debug-only-baseline-plan.ps1'
$provisionScript = Join-Path $repoRoot 'scripts\vm\new-vmware-debug-only-vm.ps1'
$bundleScript = Join-Path $repoRoot 'registry-research-framework\tools\run-windbg-boot-registry-trace.ps1'
$executeScript = Join-Path $repoRoot 'registry-research-framework\tools\execute-windbg-boot-registry-trace.ps1'
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$notesRoot = Join-Path $repoRoot 'research\notes'
$resolvedAuditDate = if ([string]::IsNullOrWhiteSpace($AuditDate)) { Get-Date -Format 'yyyyMMdd' } else { $AuditDate }
$sessionRoot = Join-Path $repoRoot ("evidence\files\vm-tooling-staging\windbg-vmware-debug-only-short-try-{0}" -f $resolvedAuditDate)
New-Item -ItemType Directory -Path $sessionRoot -Force | Out-Null
New-Item -ItemType Directory -Path $auditRoot -Force | Out-Null
New-Item -ItemType Directory -Path $notesRoot -Force | Out-Null

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-SummaryBlocker {
    param([Parameter(Mandatory = $true)][object]$Summary)

    $errorText = [string]$Summary.error
    if ($errorText -match 'Failed to write breakin packet|HOST cannot communicate with the TARGET') {
        return 'breakin-packet-error'
    }

    if ([string]$Summary.windbg_transport_state -eq 'transport_error' -or [string]$Summary.status -eq 'trace-error') {
        return 'transport-error'
    }

    if ([string]$Summary.windbg_transport_state -in @('attach_ok_command_not_executed', 'attach_ok_breakin_failed') -or [string]$Summary.status -eq 'script-not-executed') {
        return 'command-execution-unreliable'
    }

    return $null
}

function New-ProfileSpec {
    param(
        [string]$Id,
        [string]$Phase,
        [string]$TraceProfile,
        [string]$BootMode
    )

    return [ordered]@{
        id = $Id
        phase = $Phase
        trace_profile = $TraceProfile
        boot_mode = $BootMode
    }
}

function Resolve-DefaultTargetVmPath {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$TargetName
    )

    $sourceVmDirectory = Split-Path -Parent ([System.IO.Path]::GetFullPath($SourcePath))
    $sourceRoot = Split-Path -Parent $sourceVmDirectory
    $targetDirectory = Join-Path $sourceRoot $TargetName
    return (Join-Path $targetDirectory ("{0}.vmx" -f $TargetName))
}

$profiles = @(
    (New-ProfileSpec -Id 'transport-smoke' -Phase 'transport-smoke' -TraceProfile 'minimal' -BootMode 'guest-restart'),
    (New-ProfileSpec -Id 'minimal-attach-after-shell' -Phase 'minimal-attach-matrix' -TraceProfile 'minimal' -BootMode 'attach-after-shell'),
    (New-ProfileSpec -Id 'minimal-cold-boot' -Phase 'minimal-attach-matrix' -TraceProfile 'minimal' -BootMode 'cold-boot'),
    (New-ProfileSpec -Id 'breakin-smoke' -Phase 'breakin-smoke' -TraceProfile 'breakin-once' -BootMode 'guest-restart')
)

$planOutputPath = Join-Path $sessionRoot 'vmware-debug-only-plan.json'
$plan = & $planScript -OutputPath $planOutputPath | ConvertFrom-Json

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-vmware-debug-only-short-try'
    status = if ($PlanOnly) { 'planned' } else { 'ready' }
    debug_environment = 'vmware-debug-only'
    debug_vm_name = if ([string]::IsNullOrWhiteSpace($TargetVmName)) { [string]$plan.debug_vm_name } else { $TargetVmName }
    debug_snapshot_name = $DebugSnapshotName
    frozen_lane_return_allowed = $false
    plan_ref = ("evidence/files/vm-tooling-staging/windbg-vmware-debug-only-short-try-{0}/vmware-debug-only-plan.json" -f $resolvedAuditDate)
    provision_requested = [bool]$ProvisionVm
    selected_profiles = @($profiles)
    stop_rules = @(
        'same transport blocker repeats',
        'same breakin packet error repeats',
        'command execution remains unreliable'
    )
    branch_close_condition = 'repeat-current-transport-blocker'
    branch_status = 'pending'
    next_environment_if_closed = 'hyperv-prerequisites'
    entries = @()
}

if ($PlanOnly) {
    $result.branch_status = 'planned'
}
else {
    try {
        if (-not [string]::IsNullOrWhiteSpace($GuestPassword) -or -not [string]::IsNullOrWhiteSpace($CredentialFilePath) -or -not [string]::IsNullOrWhiteSpace($env:REGPROBE_VM_GUEST_PASSWORD) -or -not [string]::IsNullOrWhiteSpace($env:REGPROBE_VM_CREDENTIAL_FILE)) {
            $credentialAvailable = $true
        }
        else {
            $credentialAvailable = $false
        }

        if (-not $credentialAvailable) {
            $result.status = 'blocked-missing-credentials'
            $result.branch_status = 'blocked'
        }
        else {
            $resolvedTargetVmPath = if (-not [string]::IsNullOrWhiteSpace($TargetVmPath)) {
                $TargetVmPath
            }
            else {
                Resolve-DefaultTargetVmPath -SourcePath (Resolve-CanonicalVmPath -VmProfile 'primary') -TargetName $result.debug_vm_name
            }
            if ($ProvisionVm) {
                $provisionAuditPath = Join-Path $sessionRoot 'vmware-debug-only-provision.json'
                $provisionArgs = @(
                    '-TargetVmName', $result.debug_vm_name,
                    '-VmrunPath', $VmrunPath,
                    '-GuestUser', $GuestUser,
                    '-DebugSnapshotName', $DebugSnapshotName,
                    '-PipeName', $PipeName,
                    '-PipeEndpoint', $PipeEndpoint,
                    '-TryNoRxLoss', $TryNoRxLoss,
                    '-OutputPath', $provisionAuditPath
                )
                if (-not [string]::IsNullOrWhiteSpace($TargetVmPath)) {
                    $provisionArgs += @('-TargetVmPath', $TargetVmPath)
                }
                if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
                    $provisionArgs += @('-GuestPassword', $GuestPassword)
                }
                if (-not [string]::IsNullOrWhiteSpace($CredentialFilePath)) {
                    $provisionArgs += @('-CredentialFilePath', $CredentialFilePath)
                }
                if ($ReplaceExisting) {
                    $provisionArgs += '-ReplaceExisting'
                }

                & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $provisionScript @provisionArgs | Out-Null
                $provisionAudit = Get-Content -LiteralPath $provisionAuditPath -Raw | ConvertFrom-Json
                $result.provision_ref = ("evidence/files/vm-tooling-staging/windbg-vmware-debug-only-short-try-{0}/vmware-debug-only-provision.json" -f $resolvedAuditDate)
                $result.provision_status = [string]$provisionAudit.status
                if ([string]$provisionAudit.status -ne 'provisioned') {
                    $result.status = [string]$provisionAudit.status
                    $result.branch_status = 'blocked'
                }
            }

            if ($result.status -eq 'ready') {
                $blockerCounts = @{}
                foreach ($profile in $profiles) {
                    $bundlePath = Join-Path $sessionRoot ("{0}-bundle.json" -f $profile.id)
                    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $bundleScript `
                        -OutputFile $bundlePath `
                        -VmPath $resolvedTargetVmPath `
                        -VmrunPath $VmrunPath `
                        -GuestUser $GuestUser `
                        -PipeName $PipeName `
                        -PipeEndpoint $PipeEndpoint `
                        -TryNoRxLoss $TryNoRxLoss `
                        -DebugSnapshotName $DebugSnapshotName `
                        -CollectionMode $CollectionMode `
                        -TraceProfile $profile.trace_profile `
                        -BootMode $profile.boot_mode | Out-Null
                    if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
                        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $bundleScript `
                            -OutputFile $bundlePath `
                            -VmPath $resolvedTargetVmPath `
                            -VmrunPath $VmrunPath `
                            -GuestUser $GuestUser `
                            -GuestPassword $GuestPassword `
                            -CredentialFilePath $CredentialFilePath `
                            -PipeName $PipeName `
                            -PipeEndpoint $PipeEndpoint `
                            -TryNoRxLoss $TryNoRxLoss `
                            -DebugSnapshotName $DebugSnapshotName `
                            -CollectionMode $CollectionMode `
                            -TraceProfile $profile.trace_profile `
                            -BootMode $profile.boot_mode | Out-Null
                    }
                    elseif (-not [string]::IsNullOrWhiteSpace($CredentialFilePath)) {
                        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $bundleScript `
                            -OutputFile $bundlePath `
                            -VmPath $resolvedTargetVmPath `
                            -VmrunPath $VmrunPath `
                            -GuestUser $GuestUser `
                            -CredentialFilePath $CredentialFilePath `
                            -PipeName $PipeName `
                            -PipeEndpoint $PipeEndpoint `
                            -TryNoRxLoss $TryNoRxLoss `
                            -DebugSnapshotName $DebugSnapshotName `
                            -CollectionMode $CollectionMode `
                            -TraceProfile $profile.trace_profile `
                            -BootMode $profile.boot_mode | Out-Null
                    }
                    else {
                        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $bundleScript `
                            -OutputFile $bundlePath `
                            -VmPath $resolvedTargetVmPath `
                            -VmrunPath $VmrunPath `
                            -GuestUser $GuestUser `
                            -PipeName $PipeName `
                            -PipeEndpoint $PipeEndpoint `
                            -TryNoRxLoss $TryNoRxLoss `
                            -DebugSnapshotName $DebugSnapshotName `
                            -CollectionMode $CollectionMode `
                            -TraceProfile $profile.trace_profile `
                            -BootMode $profile.boot_mode | Out-Null
                    }

                    $executeArgs = @(
                        '-BundlePath', $bundlePath,
                        '-CollectionMode', $CollectionMode,
                        '-GuestUser', $GuestUser,
                        '-CredentialFilePath', $CredentialFilePath,
                        '-TraceProfile', $profile.trace_profile,
                        '-VmrunPath', $VmrunPath
                    )
                    if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
                        $executeArgs += @('-GuestPassword', $GuestPassword)
                    }

                    $summary = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $executeScript @executeArgs | ConvertFrom-Json
                    $blocker = Get-SummaryBlocker -Summary $summary
                    if (-not [string]::IsNullOrWhiteSpace($blocker)) {
                        if (-not $blockerCounts.ContainsKey($blocker)) {
                            $blockerCounts[$blocker] = 0
                        }
                        $blockerCounts[$blocker]++
                    }

                    $entry = [ordered]@{
                        id = $profile.id
                        phase = $profile.phase
                        trace_profile = $profile.trace_profile
                        boot_mode = $profile.boot_mode
                        status = [string]$summary.status
                        windbg_transport_state = [string]$summary.windbg_transport_state
                        transport_score = [int]$summary.transport_score
                        blocker = $blocker
                        bundle_ref = ("evidence/files/vm-tooling-staging/windbg-vmware-debug-only-short-try-{0}/{1}-bundle.json" -f $resolvedAuditDate, $profile.id)
                    }
                    $result.entries += $entry

                    $sameTransportBlockerRepeated = ($blocker -and $blockerCounts[$blocker] -ge 2)
                    $breakinPacketRepeated = (($blockerCounts['breakin-packet-error']) -ge 2)
                    $commandExecutionStillUnreliable = ($profile.id -eq 'breakin-smoke' -and $blocker -eq 'command-execution-unreliable')

                    if ($sameTransportBlockerRepeated -or $breakinPacketRepeated -or $commandExecutionStillUnreliable) {
                        $result.status = 'branch-closed'
                        $result.branch_status = 'closed'
                        $result.close_reason = if ($breakinPacketRepeated) {
                            'same-breakin-packet-error-repeated'
                        }
                        elseif ($commandExecutionStillUnreliable) {
                            'command-execution-unreliable-again'
                        }
                        else {
                            'same-transport-blocker-repeated'
                        }
                        break
                    }
                }

                if ($result.status -eq 'ready') {
                    $result.status = 'short-try-complete'
                    $result.branch_status = 'open-for-review'
                }
            }
        }
    }
    catch {
        $result.status = 'error'
        $result.branch_status = 'blocked'
        $result.error = $_.Exception.Message
    }
}

$auditPath = Join-Path $auditRoot ("windbg-vmware-debug-only-short-try-{0}.json" -f $resolvedAuditDate)
$notePath = Join-Path $notesRoot ("windbg-vmware-debug-only-short-try-{0}.md" -f $resolvedAuditDate)

Write-JsonFile -Path $auditPath -InputObject $result

$noteLines = @(
    '# VMware Debug-Only Short Try',
    '',
    "- Date: ``$resolvedAuditDate``",
    "- Status: ``$($result.status)``",
    "- Branch status: ``$($result.branch_status)``",
    "- Frozen lane return allowed: ``$($result.frozen_lane_return_allowed)``",
    '',
    '## Sequence',
    '- debugger-first fresh provision',
    '- transport-first smoke',
    '- minimal attach matrix',
    '- breakin smoke',
    '',
    '## Stop Rules',
    '- same transport blocker repeats',
    '- same breakin packet error repeats',
    '- command execution remains unreliable'
)
$closeReason = if ($result.Contains('close_reason')) { [string]$result['close_reason'] } else { '' }
if (-not [string]::IsNullOrWhiteSpace($closeReason)) {
    $noteLines += ''
    $noteLines += '## Close Reason'
    $noteLines += ("- ``{0}``" -f $closeReason)
}
Set-Content -LiteralPath $notePath -Value (($noteLines -join "`n") + "`n") -Encoding UTF8

$result | ConvertTo-Json -Depth 10
