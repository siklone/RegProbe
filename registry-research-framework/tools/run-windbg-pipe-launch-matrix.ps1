[CmdletBinding()]
param(
    [string]$VmProfile = 'secondary',
    [string]$VmPath = '',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',
    [string]$AuditDate = ''
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
        id = 'guest-restart-breakin-server-quiet'
        debugger_frontend = 'kd'
        debugger_launch_mode = 'quiet'
        trace_profile = 'breakin-once'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'bonc'
        debugger_attach_lead_seconds = 10
        shell_health_timeout_seconds = 240
        pipe_endpoint = 'server'
        try_no_rx_loss = 'FALSE'
        debug_snapshot_name = 'RegProbe-Debug-PipeLaunch-server-quiet-20260403'
    },
    [ordered]@{
        id = 'guest-restart-breakin-server-standard'
        debugger_frontend = 'kd'
        debugger_launch_mode = 'standard'
        trace_profile = 'breakin-once'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'bonc'
        debugger_attach_lead_seconds = 10
        shell_health_timeout_seconds = 240
        pipe_endpoint = 'server'
        try_no_rx_loss = 'FALSE'
        debug_snapshot_name = 'RegProbe-Debug-PipeLaunch-server-standard-20260403'
    },
    [ordered]@{
        id = 'guest-restart-breakin-client-quiet'
        debugger_frontend = 'kd'
        debugger_launch_mode = 'quiet'
        trace_profile = 'breakin-once'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'bonc'
        debugger_attach_lead_seconds = 10
        shell_health_timeout_seconds = 240
        pipe_endpoint = 'client'
        try_no_rx_loss = 'FALSE'
        debug_snapshot_name = 'RegProbe-Debug-PipeLaunch-client-quiet-20260403'
    },
    [ordered]@{
        id = 'guest-restart-breakin-client-standard'
        debugger_frontend = 'kd'
        debugger_launch_mode = 'standard'
        trace_profile = 'breakin-once'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'bonc'
        debugger_attach_lead_seconds = 10
        shell_health_timeout_seconds = 240
        pipe_endpoint = 'client'
        try_no_rx_loss = 'FALSE'
        debug_snapshot_name = 'RegProbe-Debug-PipeLaunch-client-standard-20260403'
    }
)

$variantsPath = Join-Path $auditRoot ("windbg-pipe-launch-variants-{0}.json" -f $resolvedAuditDate)
$executionPath = Join-Path $auditRoot ("windbg-pipe-launch-execution-{0}.json" -f $resolvedAuditDate)
$matrixPath = Join-Path $auditRoot ("windbg-pipe-launch-matrix-{0}.json" -f $resolvedAuditDate)
$recoveryPath = Join-Path $auditRoot ("windbg-pipe-launch-recovery-{0}.json" -f $resolvedAuditDate)
$notePath = Join-Path $notesRoot ("windbg-pipe-launch-matrix-{0}.md" -f $resolvedAuditDate)

$executionEntries = New-Object System.Collections.ArrayList
$recoveryEntries = New-Object System.Collections.ArrayList

foreach ($profile in $profiles) {
    $bundlePath = Join-Path $auditRoot ("windbg-pipe-launch-bundle-{0}-{1}.json" -f $profile.id, $resolvedAuditDate)
    $bundleArgs = @{
        OutputFile = $bundlePath
        VmProfile = $VmProfile
        CollectionMode = $CollectionMode
        TraceProfile = $profile.trace_profile
        BootMode = $profile.boot_mode
        DebuggerFrontend = $profile.debugger_frontend
        DebuggerLaunchMode = $profile.debugger_launch_mode
        BreakOnConnectMode = $profile.break_on_connect_mode
        PipeEndpoint = $profile.pipe_endpoint
        TryNoRxLoss = $profile.try_no_rx_loss
        GuestUser = $GuestUser
        DebugSnapshotName = $profile.debug_snapshot_name
        PrepareBaseline = $true
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

    & $runBundleScript @bundleArgs | Out-Null

    $executeArgs = @{
        BundlePath = $bundlePath
        TraceProfile = $profile.trace_profile
        CollectionMode = $CollectionMode
        GuestUser = $GuestUser
        DebuggerAttachLeadSeconds = [int]$profile.debugger_attach_lead_seconds
        ShellHealthTimeoutSeconds = [int]$profile.shell_health_timeout_seconds
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
        debugger_frontend = $profile.debugger_frontend
        debugger_launch_mode = $profile.debugger_launch_mode
        trace_profile = $profile.trace_profile
        boot_mode = $profile.boot_mode
        break_on_connect_mode = $profile.break_on_connect_mode
        debugger_attach_lead_seconds = [int]$profile.debugger_attach_lead_seconds
        shell_health_timeout_seconds = [int]$profile.shell_health_timeout_seconds
        pipe_endpoint = $profile.pipe_endpoint
        try_no_rx_loss = $profile.try_no_rx_loss
        debug_snapshot_name = $profile.debug_snapshot_name
        bundle = ($bundlePath.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/'))
        summary_ref = $summaryRef
        status = [string]$summary.status
        windbg_transport_state = [string]$summary.windbg_transport_state
        transport_score = [int]$summary.transport_score
        kernel_connected = [bool]$summary.kernel_connected
        transport_error = [bool]$summary.transport_error
        fatal_system_error_observed = [bool]$summary.fatal_system_error_observed
        shell_recovered = [bool]$summary.shell_recovered
        breakin_attempted = [bool]$summary.breakin_attempted
        breakin_dispatched_count = [int]$summary.breakin_dispatched_count
        breakin_request_count = [int]$summary.breakin_request_count
        breakin_success_count = [int]$summary.breakin_success_count
    }
    [void]$executionEntries.Add($entry)

    if ($summary.recovery) {
        [void]$recoveryEntries.Add([ordered]@{
            id = $profile.id
            recovered = [bool]$summary.recovery.recovered
            steps = @($summary.recovery.steps)
            shell_checks = $summary.recovery.shell_checks
        })
    }
}

$healthyBreakinCount = @($executionEntries | Where-Object { $_.breakin_success_count -gt 0 -and $_.shell_recovered -and -not $_.fatal_system_error_observed }).Count
$fatalBreakCount = @($executionEntries | Where-Object { $_.fatal_system_error_observed }).Count
$attachOkCommandMissingCount = @($executionEntries | Where-Object { $_.status -eq 'attach-ok-command-not-executed' }).Count
$transportErrorCount = @($executionEntries | Where-Object { $_.transport_error }).Count

$matrix = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    phase = 'pipe-launch-matrix'
    audit_date = $resolvedAuditDate
    profile_count = $profiles.Count
    healthy_breakin_count = $healthyBreakinCount
    fatal_break_count = $fatalBreakCount
    attach_ok_command_not_executed_count = $attachOkCommandMissingCount
    transport_error_count = $transportErrorCount
    executed_profiles = @($executionEntries)
    current_status = if ($healthyBreakinCount -gt 0) { 'partial' } elseif ($attachOkCommandMissingCount -gt 0) { 'attach-ok-command-not-executed' } else { 'transport-blocked' }
    recommended_next_actions = @(
        'Prefer any pipe/launch variant that reaches breakin_success_count>0 without fatal_system_error_observed before widening to semantics.',
        'If every pipe/launch variant stays attach-ok-command-not-executed, move next to debugger frontend or serial transport alternatives outside the current VMware named-pipe contract.',
        'Keep WinDbg single-key arbitration paused until one pipe/launch variant yields a healthy post-restart command roundtrip.'
    )
}

$noteLines = @(
    '# WinDbg Pipe And Launch Matrix',
    '',
    ('Date: `{0}`' -f (Get-Date -Format 'yyyy-MM-dd')),
    '',
    '## Summary',
    '',
    ('- executed profiles: `{0}`' -f $profiles.Count),
    ('- healthy-breakin profiles: `{0}`' -f $healthyBreakinCount),
    ('- fatal-break profiles: `{0}`' -f $fatalBreakCount),
    ('- attach-ok-command-not-executed profiles: `{0}`' -f $attachOkCommandMissingCount),
    ('- transport-error profiles: `{0}`' -f $transportErrorCount),
    '',
    '## Profiles',
    ''
)
foreach ($entry in $executionEntries) {
    $noteLines += ('- `{0}` -> status `{1}`, transport `{2}`, endpoint `{3}`, launch `{4}`, breakin_attempted `{5}`, breakin_success_count `{6}`, shell_recovered `{7}`, fatal `{8}`' -f $entry.id, $entry.status, $entry.windbg_transport_state, $entry.pipe_endpoint, $entry.debugger_launch_mode, $entry.breakin_attempted, $entry.breakin_success_count, $entry.shell_recovered, $entry.fatal_system_error_observed)
}
$noteLines += @(
    '',
    '## Follow-Up',
    '',
    '- keep this phase focused on pipe endpoint and debugger launch mode only',
    '- do not widen to key-specific arbitration until one variant reaches a healthy post-restart prompt',
    '- if all variants stay attach-ok-command-not-executed, the next step is likely outside the current named-pipe contract'
)

Write-JsonFile -Path $variantsPath -InputObject @{ generated_utc = [DateTime]::UtcNow.ToString('o'); profiles = @($profiles) }
Write-JsonFile -Path $executionPath -InputObject @{ generated_utc = [DateTime]::UtcNow.ToString('o'); entries = @($executionEntries) }
Write-JsonFile -Path $matrixPath -InputObject $matrix
Write-JsonFile -Path $recoveryPath -InputObject @{ generated_utc = [DateTime]::UtcNow.ToString('o'); entries = @($recoveryEntries) }
Set-Content -Path $notePath -Value $noteLines -Encoding UTF8

$matrix
