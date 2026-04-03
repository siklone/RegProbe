[CmdletBinding()]
param(
    [string]$VmProfile = 'secondary',
    [string]$VmPath = '',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [string]$PipeName = '\\.\pipe\regprobe_debug',
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',
    [string]$AuditDate = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$configureScript = Join-Path $repoRoot 'scripts\vm\configure-kernel-debug-baseline.ps1'
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
        id = 'guest-restart-kd-bonc-rxloss-true'
        debugger_frontend = 'kd'
        trace_profile = 'symbols'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'bonc'
        pipe_endpoint = 'server'
        try_no_rx_loss = 'TRUE'
    },
    [ordered]@{
        id = 'guest-restart-kd-none-rxloss-true'
        debugger_frontend = 'kd'
        trace_profile = 'symbols'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'none'
        pipe_endpoint = 'server'
        try_no_rx_loss = 'TRUE'
    },
    [ordered]@{
        id = 'guest-restart-kd-bonc-rxloss-false'
        debugger_frontend = 'kd'
        trace_profile = 'symbols'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'bonc'
        pipe_endpoint = 'server'
        try_no_rx_loss = 'FALSE'
    },
    [ordered]@{
        id = 'guest-restart-kd-none-rxloss-false'
        debugger_frontend = 'kd'
        trace_profile = 'symbols'
        boot_mode = 'guest-restart'
        break_on_connect_mode = 'none'
        pipe_endpoint = 'server'
        try_no_rx_loss = 'FALSE'
    }
)

$variantsPath = Join-Path $auditRoot ("windbg-serial-config-variants-{0}.json" -f $resolvedAuditDate)
$executionPath = Join-Path $auditRoot ("windbg-serial-config-execution-{0}.json" -f $resolvedAuditDate)
$matrixPath = Join-Path $auditRoot ("windbg-serial-config-matrix-{0}.json" -f $resolvedAuditDate)
$recoveryPath = Join-Path $auditRoot ("windbg-serial-config-recovery-{0}.json" -f $resolvedAuditDate)
$notePath = Join-Path $notesRoot ("windbg-serial-config-matrix-{0}.md" -f $resolvedAuditDate)

$executionEntries = New-Object System.Collections.ArrayList
$recoveryEntries = New-Object System.Collections.ArrayList
$prepareEntries = New-Object System.Collections.ArrayList

foreach ($profile in $profiles) {
    $variantSnapshotName = "RegProbe-Debug-Serial-$($profile.id)"
    $preparePath = Join-Path $auditRoot ("windbg-serial-config-prepare-{0}-{1}.json" -f $profile.id, $resolvedAuditDate)
    $bundlePath = Join-Path $auditRoot ("windbg-serial-config-bundle-{0}-{1}.json" -f $profile.id, $resolvedAuditDate)

    try {
        $prepareArgs = @(
            '-VmrunPath', 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
            '-GuestUser', $GuestUser,
            '-PipeName', $PipeName,
            '-PipeEndpoint', $profile.pipe_endpoint,
            '-TryNoRxLoss', $profile.try_no_rx_loss,
            '-CreateSnapshotName', $variantSnapshotName,
            '-OutputPath', $preparePath
        )
        if (-not [string]::IsNullOrWhiteSpace($VmProfile)) {
            $prepareArgs += @('-VmProfile', $VmProfile)
        }
        if (-not [string]::IsNullOrWhiteSpace($VmPath)) {
            $prepareArgs += @('-VmPath', $VmPath)
        }
        if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
            $prepareArgs += @('-GuestPassword', $GuestPassword)
        }
        if (-not [string]::IsNullOrWhiteSpace($CredentialFilePath)) {
            $prepareArgs += @('-CredentialFilePath', $CredentialFilePath)
        }

        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $configureScript @prepareArgs | Out-Null
        $prep = if (Test-Path -LiteralPath $preparePath) {
            Get-Content -LiteralPath $preparePath -Raw | ConvertFrom-Json
        }
        else {
            $null
        }

        [void]$prepareEntries.Add([ordered]@{
            id = $profile.id
            snapshot_name = $variantSnapshotName
            pipe_endpoint = $profile.pipe_endpoint
            try_no_rx_loss = $profile.try_no_rx_loss
            prepare_ref = if ($prep) { $preparePath.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/') } else { $null }
            prepare_status = if ($prep) { [string]$prep.status } else { 'missing' }
        })

        if (-not $prep -or [string]$prep.status -eq 'error') {
            throw "Variant prepare failed for $($profile.id)."
        }

        $bundleArgs = @{
            OutputFile = $bundlePath
            VmProfile = $VmProfile
            CollectionMode = $CollectionMode
            TraceProfile = $profile.trace_profile
            BootMode = $profile.boot_mode
            DebuggerFrontend = $profile.debugger_frontend
            BreakOnConnectMode = $profile.break_on_connect_mode
            GuestUser = $GuestUser
            DebugSnapshotName = $variantSnapshotName
            PipeName = $PipeName
            PipeEndpoint = $profile.pipe_endpoint
            TryNoRxLoss = $profile.try_no_rx_loss
        }
        if (-not [string]::IsNullOrWhiteSpace($VmPath)) {
            $bundleArgs.VmPath = $VmPath
        }
        if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
            $bundleArgs.GuestPassword = $GuestPassword
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
            trace_profile = $profile.trace_profile
            boot_mode = $profile.boot_mode
            break_on_connect_mode = $profile.break_on_connect_mode
            pipe_endpoint = $profile.pipe_endpoint
            try_no_rx_loss = $profile.try_no_rx_loss
            snapshot_name = $variantSnapshotName
            bundle = $bundlePath.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/')
            summary_ref = $summaryRef
            status = [string]$summary.status
            windbg_transport_state = [string]$summary.windbg_transport_state
            transport_score = [int]$summary.transport_score
            kernel_connected = [bool]$summary.kernel_connected
            transport_error = [bool]$summary.transport_error
            no_debuggee_waiting = [bool]$summary.no_debuggee_waiting
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
            debugger_frontend = $profile.debugger_frontend
            trace_profile = $profile.trace_profile
            boot_mode = $profile.boot_mode
            break_on_connect_mode = $profile.break_on_connect_mode
            pipe_endpoint = $profile.pipe_endpoint
            try_no_rx_loss = $profile.try_no_rx_loss
            snapshot_name = $variantSnapshotName
            bundle = $bundlePath.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/')
            summary_ref = $null
            status = 'serial-matrix-error'
            windbg_transport_state = 'transport_error'
            transport_score = 0
            kernel_connected = $false
            transport_error = $true
            no_debuggee_waiting = $false
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

$kernelConnected = @($executionEntries | Where-Object { $_.kernel_connected })
$transportErrors = @($executionEntries | Where-Object { $_.transport_error })
$noDebuggee = @($executionEntries | Where-Object { $_.no_debuggee_waiting })

$variantsAudit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    phase = 'serial-config-matrix'
    audit_date = $resolvedAuditDate
    variants = @($profiles)
}

$executionAudit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    phase = 'serial-config-matrix'
    audit_date = $resolvedAuditDate
    prepare_entries = @($prepareEntries)
    entries = @($executionEntries)
}

$recoveryAudit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    phase = 'serial-config-matrix'
    audit_date = $resolvedAuditDate
    recovery_count = $recoveryEntries.Count
    entries = @($recoveryEntries)
}

$matrix = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    epic = 'windbg-transport-hardening'
    phase = 'serial-config-matrix'
    audit_date = $resolvedAuditDate
    profile_count = $profiles.Count
    kernel_connected_count = $kernelConnected.Count
    transport_error_count = $transportErrors.Count
    no_debuggee_count = $noDebuggee.Count
    executed_profiles = @($executionEntries)
    current_status = if ($kernelConnected.Count -gt 0 -and $transportErrors.Count -lt $profiles.Count) { 'partial' } else { 'transport-blocked' }
    recommended_next_actions = @(
        'Prefer variants that preserve kernel_connected without transport_error before returning to single-key arbitration.',
        'If -bonc is consistently worse than none, keep break-on-connect disabled for the next round.',
        'If TRUE/FALSE tryNoRxLoss does not materially change the result, move next to pipe endpoint and start-order experiments.'
    )
}

$noteLines = @(
    '# WinDbg Serial Config Matrix',
    '',
    ('Date: `{0}`' -f (Get-Date -Format 'yyyy-MM-dd')),
    '',
    '## Summary',
    '',
    ('- executed profiles: `{0}`' -f $profiles.Count),
    ('- kernel-connected profiles: `{0}`' -f $kernelConnected.Count),
    ('- transport-error profiles: `{0}`' -f $transportErrors.Count),
    ('- no-debuggee profiles: `{0}`' -f $noDebuggee.Count),
    '',
    '## Profiles',
    ''
)
foreach ($entry in $executionEntries) {
    $noteLines += ('- `{0}` -> status `{1}`, transport `{2}`, score `{3}`, kernel_connected `{4}`, rxloss `{5}`, bonc `{6}`' -f $entry.id, $entry.status, $entry.windbg_transport_state, $entry.transport_score, $entry.kernel_connected, $entry.try_no_rx_loss, $entry.break_on_connect_mode)
}
$noteLines += @(
    '',
    '## Follow-Up',
    '',
    '- keep this phase focused on transport only',
    '- do not widen to more keys until a guest-restart or cold-boot serial variant is reproducibly usable',
    '- if these four variants stay weak, move next to pipe endpoint and attach/start-order combinations'
)

Write-JsonFile -Path $variantsPath -InputObject $variantsAudit
Write-JsonFile -Path $executionPath -InputObject $executionAudit
Write-JsonFile -Path $recoveryPath -InputObject $recoveryAudit
Write-JsonFile -Path $matrixPath -InputObject $matrix
Set-Content -LiteralPath $notePath -Value $noteLines -Encoding UTF8

$matrix
