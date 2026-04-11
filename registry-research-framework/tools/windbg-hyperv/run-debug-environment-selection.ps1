[CmdletBinding()]
param(
    [string]$OutputRoot = '',
    [string]$DateToken = (Get-Date -Format 'yyyyMMdd')
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$noteRoot = Join-Path $repoRoot 'research\notes'
$toolOutputRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    Join-Path $repoRoot "evidence\files\vm-tooling-staging\windbg-debug-environment-selection-$DateToken"
}
else {
    Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) "windbg-debug-environment-selection-$DateToken"
}
New-Item -ItemType Directory -Path $toolOutputRoot -Force | Out-Null

$feasibilityScript = Join-Path $repoRoot 'scripts\vm-hyperv\test-hyperv-debug-feasibility.ps1'
$planScript = Join-Path $repoRoot 'scripts\vm-hyperv\new-hyperv-debug-baseline-plan.ps1'
$vmwareDebugOnlyPlanScript = Join-Path $repoRoot 'scripts\vm\new-vmware-debug-only-baseline-plan.ps1'
$transportFindingsPath = Join-Path $auditRoot 'windbg-transport-findings-20260403.json'
$freezeAuditPath = Join-Path $auditRoot ("windbg-vmware-freeze-{0}.json" -f $DateToken)
$freezeNotePath = Join-Path $noteRoot ("windbg-vmware-freeze-{0}.md" -f $DateToken)
$selectionAuditPath = Join-Path $auditRoot ("windbg-debug-environment-selection-{0}.json" -f $DateToken)
$selectionNotePath = Join-Path $noteRoot ("windbg-debug-environment-selection-{0}.md" -f $DateToken)
$setupAuditPath = Join-Path $auditRoot ("windbg-hyperv-setup-{0}.json" -f $DateToken)
$setupNotePath = Join-Path $noteRoot ("windbg-hyperv-setup-{0}.md" -f $DateToken)
$vmwareSetupAuditPath = Join-Path $auditRoot ("windbg-vmware-debug-only-setup-{0}.json" -f $DateToken)
$vmwareSetupNotePath = Join-Path $noteRoot ("windbg-vmware-debug-only-setup-{0}.md" -f $DateToken)

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

$feasibilityOutputPath = Join-Path $toolOutputRoot 'hyperv-feasibility.json'
$planOutputPath = Join-Path $toolOutputRoot 'hyperv-baseline-plan.json'
$vmwarePlanOutputPath = Join-Path $toolOutputRoot 'vmware-debug-only-plan.json'

$feasibility = & $feasibilityScript -OutputPath $feasibilityOutputPath | ConvertFrom-Json
$plan = & $planScript -FeasibilityPath $feasibilityOutputPath -OutputPath $planOutputPath | ConvertFrom-Json
$vmwareDebugOnlyPlan = & $vmwareDebugOnlyPlanScript -OutputPath $vmwarePlanOutputPath | ConvertFrom-Json
$freezeAudit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    environment = 'vmware'
    lane = 'windbg'
    status = 'known-blocked-frozen'
    semantic_ready = $true
    parser_ready = $true
    public_registry_symbol = 'nt!CmQueryValueKey'
    value_name_argument = '@rdx'
    transport_contract = 'unreliable'
    freeze_reason = 'The current VMware named-pipe contract cannot deliver classification-grade command/breakin roundtrips.'
    transport_findings_ref = 'registry-research-framework/audit/windbg-transport-findings-20260403.json'
    next_environment = 'hyperv'
    immediate_fallback_environment = 'vmware-debug-only'
    next_phase = 'debug-environment-selection'
}
Write-JsonFile -Path $freezeAuditPath -InputObject $freezeAudit

$freezeNote = @(
    '# VMware WinDbg Freeze',
    '',
    "- Date: ``$DateToken``",
    '- Parser and public-symbol work are preserved, not discarded.',
    '- Confirmed public symbol: ``nt!CmQueryValueKey``',
    '- Confirmed value-name argument: ``@rdx``',
    '- Freeze reason: the current VMware named-pipe transport contract remains unreliable for classification-grade arbiter work.',
    '- Future work moves to a debugger-first environment instead of re-spending cycles on the same VMware transport envelope.',
    '',
    '## References',
    '- `registry-research-framework/audit/windbg-transport-findings-20260403.json`',
    '- `registry-research-framework/audit/windbg-pipe-launch-matrix-20260403.json`'
) -join "`n"
Set-Content -LiteralPath $freezeNotePath -Value ($freezeNote + "`n") -Encoding UTF8

$selectionAudit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-new-debug-environment'
    freeze_ref = "registry-research-framework/audit/$(Split-Path -Leaf $freezeAuditPath)"
    feasibility_ref = "evidence/files/vm-tooling-staging/$(Split-Path -Leaf $toolOutputRoot)/hyperv-feasibility.json"
    hyperv_setup_ref = "registry-research-framework/audit/$(Split-Path -Leaf $setupAuditPath)"
    vmware_debug_only_setup_ref = "registry-research-framework/audit/$(Split-Path -Leaf $vmwareSetupAuditPath)"
    debug_environment_candidates = @($feasibility.debug_environment_candidates)
    selected = [string]$feasibility.selected
    selected_status = [string]$feasibility.selected_status
    selected_long_term = [string]$feasibility.selected_long_term
    selected_long_term_status = [string]$feasibility.selected_long_term_status
    selected_immediate = [string]$feasibility.selected_immediate
    selected_immediate_status = [string]$feasibility.selected_immediate_status
    selected_immediate_reason = [string]$feasibility.selected_immediate_reason
    fallback_if_blocked = [string]$feasibility.fallback_if_blocked
    frozen_lane_return_allowed = [bool]$feasibility.frozen_lane_return_allowed
}
Write-JsonFile -Path $selectionAuditPath -InputObject $selectionAudit

$selectionNote = @(
    '# WinDbg Debug Environment Selection',
    '',
    "- Date: ``$DateToken``",
    "- Selected long-term target: ``$($feasibility.selected_long_term)``",
    "- Long-term status: ``$($feasibility.selected_long_term_status)``",
    "- Immediate environment: ``$($feasibility.selected_immediate)``",
    "- Immediate environment status: ``$($feasibility.selected_immediate_status)``",
    "- Fallback if blocked: ``$($feasibility.fallback_if_blocked)``",
    '',
    '## Host Signals',
    "- HyperVisorPresent: ``$($feasibility.host.hypervisor_present)``",
    "- Hyper-V feature state: ``$((@($feasibility.hyperv.features | Where-Object { $_.name -eq 'Microsoft-Hyper-V' }) | Select-Object -First 1).state)``",
    "- Hyper-V PowerShell feature state: ``$((@($feasibility.hyperv.features | Where-Object { $_.name -eq 'Microsoft-Hyper-V-Management-PowerShell' }) | Select-Object -First 1).state)``",
    "- VirtualMachinePlatform state: ``$((@($feasibility.hyperv.features | Where-Object { $_.name -eq 'VirtualMachinePlatform' }) | Select-Object -First 1).state)``",
    '',
    '## Decision',
    '- Freeze the current VMware WinDbg lane as known blocked.',
    '- Treat Hyper-V as the preferred debugger-first target environment.',
    '- If Hyper-V prerequisites are still blocked, allow one short fresh VMware debug-only try instead of returning to the frozen lane.',
    '- If that short VMware debug-only try reproduces the same transport blocker, stop and move directly to Hyper-V prerequisites.'
) -join "`n"
Set-Content -LiteralPath $selectionNotePath -Value ($selectionNote + "`n") -Encoding UTF8

Write-JsonFile -Path $setupAuditPath -InputObject $plan

$setupNote = @(
    '# Hyper-V Debug Setup Plan',
    '',
    "- Status: ``$($plan.status)``",
    "- Debug VM name: ``$($plan.debug_vm_name)``",
    "- Baseline: ``$($plan.debug_baseline)``",
    "- VM role: ``$($plan.vm_role)``",
    "- Transport candidates: ``$((@($plan.debug_transport_candidates) -join ', '))``",
    '',
    '## Role Split',
    '- `VMware`: runtime research lanes',
    '- `Hyper-V`: debug arbiter only',
    '',
    '## Provisioning Steps'
)
$setupNote += @($plan.provisioning_steps | ForEach-Object { "- $_" })
Set-Content -LiteralPath $setupNotePath -Value (($setupNote -join "`n") + "`n") -Encoding UTF8

Write-JsonFile -Path $vmwareSetupAuditPath -InputObject $vmwareDebugOnlyPlan

$vmwareSetupNote = @(
    '# VMware Debug-Only Fallback Plan',
    '',
    "- Status: ``$($vmwareDebugOnlyPlan.status)``",
    "- Debug VM name: ``$($vmwareDebugOnlyPlan.debug_vm_name)``",
    "- Baseline: ``$($vmwareDebugOnlyPlan.debug_baseline)``",
    "- VM role: ``$($vmwareDebugOnlyPlan.vm_role)``",
    "- Source runtime profile: ``$($vmwareDebugOnlyPlan.source_runtime_profile)``",
    "- Source snapshot: ``$($vmwareDebugOnlyPlan.source_snapshot)``",
    "- Trial policy: ``$($vmwareDebugOnlyPlan.trial_policy)``",
    '',
    '## Decision Rules',
    '- This is a fresh debugger-first fallback VM, not the frozen VMware WinDbg lane.',
    '- Run only a short transport-first try here.',
    '- If the current transport blocker reproduces, stop and move directly to Hyper-V prerequisites.'
)
Set-Content -LiteralPath $vmwareSetupNotePath -Value (($vmwareSetupNote -join "`n") + "`n") -Encoding UTF8

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    freeze_audit = $freezeAuditPath
    selection_audit = $selectionAuditPath
    hyperv_setup_audit = $setupAuditPath
    vmware_debug_only_setup_audit = $vmwareSetupAuditPath
    feasibility_artifact = $feasibilityOutputPath
    hyperv_plan_artifact = $planOutputPath
    vmware_debug_only_plan_artifact = $vmwarePlanOutputPath
} | ConvertTo-Json -Depth 8
