[CmdletBinding()]
param(
    [string]$VmProfile = 'secondary',
    [string]$VmPath = '',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [string]$DebugSnapshotName = 'RegProbe-Baseline-Debug-20260402',
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',
    [string]$AuditDate = '',
    [switch]$PrepareBaseline
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$runBundleScript = Join-Path $PSScriptRoot 'run-windbg-boot-registry-trace.ps1'
$executeScript = Join-Path $PSScriptRoot 'execute-windbg-boot-registry-trace.ps1'
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$notesRoot = Join-Path $repoRoot 'research\notes'
$resolvedAuditDate = if ([string]::IsNullOrWhiteSpace($AuditDate)) { Get-Date -Format 'yyyyMMdd' } else { $AuditDate }

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$InputObject,
        [int]$Depth = 10
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

function Get-ProfileResultSummaryRef {
    param([object]$Summary)

    $commandScript = [string]$Summary.command_script
    if ([string]::IsNullOrWhiteSpace($commandScript)) {
        return $null
    }

    $directory = Split-Path -Parent $commandScript
    if ([string]::IsNullOrWhiteSpace($directory)) {
        return $null
    }

    return ($directory.TrimEnd('/') + '/summary.json')
}

$profiles = @(
    [ordered]@{
        id = 'minimal-cold-boot'
        phase = 'minimal-matrix'
        trace_profile = 'minimal'
        boot_mode = 'cold-boot'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'minimal-attach-after-shell'
        phase = 'minimal-matrix'
        trace_profile = 'minimal'
        boot_mode = 'attach-after-shell'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'symbols-guest-restart'
        phase = 'minimal-matrix'
        trace_profile = 'symbols'
        boot_mode = 'guest-restart'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'attach-only-attach-after-shell'
        phase = 'breakin-matrix'
        trace_profile = 'attach-only'
        boot_mode = 'attach-after-shell'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'breakin-once-attach-after-shell'
        phase = 'breakin-matrix'
        trace_profile = 'breakin-once'
        boot_mode = 'attach-after-shell'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'breakin-twice-attach-after-shell'
        phase = 'breakin-matrix'
        trace_profile = 'breakin-twice'
        boot_mode = 'attach-after-shell'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'breakin-delayed-10-attach-after-shell'
        phase = 'breakin-matrix'
        trace_profile = 'breakin-delayed-10'
        boot_mode = 'attach-after-shell'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'breakin-delayed-30-attach-after-shell'
        phase = 'breakin-matrix'
        trace_profile = 'breakin-delayed-30'
        boot_mode = 'attach-after-shell'
        target_key = $null
        semantic_ready = $false
    },
    [ordered]@{
        id = 'singlekey-smoke-cold-boot'
        phase = 'singlekey-smoke'
        trace_profile = 'singlekey-smoke'
        boot_mode = 'cold-boot'
        target_key = 'AllowSystemRequiredPowerRequests'
        semantic_ready = $true
    }
)

$configVariantsPath = Join-Path $auditRoot ("windbg-transport-config-variants-{0}.json" -f $resolvedAuditDate)
$executionPath = Join-Path $auditRoot ("windbg-transport-execution-{0}.json" -f $resolvedAuditDate)
$matrixPath = Join-Path $auditRoot ("windbg-transport-matrix-{0}.json" -f $resolvedAuditDate)
$recoveryPath = Join-Path $auditRoot ("windbg-transport-recovery-{0}.json" -f $resolvedAuditDate)
$notePath = Join-Path $notesRoot ("windbg-transport-matrix-{0}.md" -f $resolvedAuditDate)

$executionEntries = New-Object System.Collections.ArrayList
$recoveryEntries = New-Object System.Collections.ArrayList

for ($index = 0; $index -lt $profiles.Count; $index++) {
    $profile = $profiles[$index]
    $bundlePath = Join-Path $auditRoot ("windbg-transport-bundle-{0}-{1}.json" -f $profile.id, $resolvedAuditDate)
    $bundleArgs = @{
        OutputFile = $bundlePath
        VmProfile = $VmProfile
        CollectionMode = $CollectionMode
        TraceProfile = $profile.trace_profile
        BootMode = $profile.boot_mode
        GuestUser = $GuestUser
        DebugSnapshotName = $DebugSnapshotName
    }
    if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
        $bundleArgs.GuestPassword = $GuestPassword
    }
    if (-not [string]::IsNullOrWhiteSpace($VmPath)) {
        $bundleArgs.VmPath = $VmPath
    }
    if (-not [string]::IsNullOrWhiteSpace($CredentialFilePath)) {
        $bundleArgs.CredentialFilePath = $CredentialFilePath
    }
    if ($PrepareBaseline -and $index -eq 0) {
        $bundleArgs.PrepareBaseline = $true
    }
    if ($profile.target_key) {
        $bundleArgs.TargetKey = $profile.target_key
    }

    try {
        & $runBundleScript @bundleArgs | Out-Null
        $executeArgs = @{
            BundlePath = $bundlePath
            TraceProfile = $profile.trace_profile
            CollectionMode = $CollectionMode
            GuestUser = $GuestUser
        }
        if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
            $executeArgs.GuestPassword = $GuestPassword
        }
        if (-not [string]::IsNullOrWhiteSpace($CredentialFilePath)) {
            $executeArgs.CredentialFilePath = $CredentialFilePath
        }

        $summary = & $executeScript @executeArgs
        $summaryRef = Get-ProfileResultSummaryRef -Summary $summary

        $entry = [ordered]@{
            id = $profile.id
            phase = $profile.phase
            trace_profile = $profile.trace_profile
            boot_mode = $profile.boot_mode
            target_key = $profile.target_key
            semantic_ready = $profile.semantic_ready
            bundle = $bundlePath.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/')
            summary_ref = $summaryRef
            status = [string]$summary.status
            windbg_transport_state = [string]$summary.windbg_transport_state
            transport_score = [int]$summary.transport_score
            kernel_connected = [bool]$summary.kernel_connected
            transport_error = [bool]$summary.transport_error
            script_execution_observed = [bool]$summary.script_execution_observed
            shell_recovered = [bool]$summary.shell_recovered
            breakin_attempted = [bool]$summary.breakin_attempted
            breakin_request_count = [int]$summary.breakin_request_count
            breakin_success_count = [int]$summary.breakin_success_count
            breakpoint_unresolved = [bool]$summary.breakpoint_unresolved
            parser_invalid = [bool]$summary.parser_invalid
        }
        [void]$executionEntries.Add($entry)

        if ($summary.recovery) {
            [void]$recoveryEntries.Add([ordered]@{
                id = $profile.id
                summary_ref = $summaryRef
                recovery = $summary.recovery
            })
        }
    }
    catch {
        [void]$executionEntries.Add([ordered]@{
            id = $profile.id
            phase = $profile.phase
            trace_profile = $profile.trace_profile
            boot_mode = $profile.boot_mode
            target_key = $profile.target_key
            semantic_ready = $profile.semantic_ready
            bundle = $bundlePath.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/')
            summary_ref = $null
            status = 'matrix-run-error'
            windbg_transport_state = 'transport_error'
            transport_score = 0
            kernel_connected = $false
            transport_error = $true
            script_execution_observed = $false
            shell_recovered = $false
            breakin_attempted = $false
            breakin_request_count = 0
            breakin_success_count = 0
            breakpoint_unresolved = $false
            parser_invalid = $false
            error = $_.Exception.Message
        })
    }
}

$stableProfiles = @($executionEntries | Where-Object { $_.windbg_transport_state -eq 'transport_ok' })
$unstableProfiles = @($executionEntries | Where-Object { $_.windbg_transport_state -ne 'transport_ok' })

$matrix = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    audit_date = $resolvedAuditDate
    vm_profile = $VmProfile
    vm_path = if ([string]::IsNullOrWhiteSpace($VmPath)) { $null } else { $VmPath }
    debug_snapshot_name = $DebugSnapshotName
    profile_count = $profiles.Count
    stable_profile_count = $stableProfiles.Count
    unstable_profile_count = $unstableProfiles.Count
    breakin_profile_count = @($executionEntries | Where-Object { $_.trace_profile -like 'breakin-*' }).Count
    breakin_success_profile_count = @($executionEntries | Where-Object { $_.breakin_success_count -gt 0 }).Count
    reproducibility_target = 2
    current_status = if ($stableProfiles.Count -gt 0) { 'partial' } else { 'transport-blocked' }
    executed_profiles = @($executionEntries)
    recommended_next_actions = @(
        'Keep transport testing separate from registry semantics until one attach profile is reproducibly stable.',
        'Prefer attach-after-shell vs cold-boot comparisons before adding new breakpoint complexity.',
        'Do not widen single-key arbitration beyond AllowSystemRequiredPowerRequests until one transport profile is stable twice.'
    )
}

$configVariants = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    audit_date = $resolvedAuditDate
    variants = @($profiles)
}

$recoveryAudit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    audit_date = $resolvedAuditDate
    recovery_count = $recoveryEntries.Count
    entries = @($recoveryEntries)
}

$executionAudit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    audit_date = $resolvedAuditDate
    entries = @($executionEntries)
}

$noteLines = @(
    '# WinDbg Transport Matrix',
    '',
    ('Date: `{0}`' -f (Get-Date -Format 'yyyy-MM-dd')),
    '',
    '## Summary',
    '',
    ('- executed profiles: `{0}`' -f $profiles.Count),
    ('- stable profiles: `{0}`' -f $stableProfiles.Count),
    ('- unstable profiles: `{0}`' -f $unstableProfiles.Count),
    ('- break-in profiles: `{0}`' -f (@($executionEntries | Where-Object { $_.trace_profile -like 'breakin-*' }).Count)),
    ('- break-in success profiles: `{0}`' -f (@($executionEntries | Where-Object { $_.breakin_success_count -gt 0 }).Count)),
    '',
    '## Profiles',
    ''
)
foreach ($entry in $executionEntries) {
    $suffix = if ($entry.trace_profile -like 'breakin-*') {
        ', breakin {0}/{1}' -f $entry.breakin_success_count, $entry.breakin_request_count
    }
    else {
        ''
    }
    $noteLines += ('- `{0}` [{1}] -> status `{2}`, transport `{3}`, score `{4}`{5}' -f $entry.id, $entry.phase, $entry.status, $entry.windbg_transport_state, $entry.transport_score, $suffix)
}
$noteLines += @(
    '',
    '## Follow-Up',
    '',
    '- keep transport engineering isolated from registry classification',
    '- repeat any potentially stable profile twice before using it as the arbiter base',
    '- only resume single-key semantic arbitration after a transport-stable profile exists'
)

Write-JsonFile -Path $configVariantsPath -InputObject $configVariants
Write-JsonFile -Path $executionPath -InputObject $executionAudit
Write-JsonFile -Path $matrixPath -InputObject $matrix
Write-JsonFile -Path $recoveryPath -InputObject $recoveryAudit
Set-Content -LiteralPath $notePath -Value $noteLines -Encoding UTF8

$matrix
