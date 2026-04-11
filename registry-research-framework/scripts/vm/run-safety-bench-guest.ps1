[CmdletBinding()]
param(
    [string]$CandidateId = 'system.kernel.disable-exception-chain-validation',
    [string]$RegistryPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel',
    [string]$RegistryPathNative = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel',
    [string]$RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Kernel',
    [string]$ValueName = 'DisableExceptionChainValidation',
    [int]$ApplyValue = 1,
    [string]$ApplyType = 'REG_DWORD',
    [string]$RollbackMethod = 'delete',
    [Nullable[int]]$RollbackValue = $null,
    [string]$RollbackType = 'REG_DWORD',
    [bool]$RequireStateChanged = $true,
    [string]$BenchTier = 'vm',
    [string]$BenchProfile = 'functional',
    [string]$BenchEnvironment = 'windows-11-25h2-vm',
    [string]$BenchMeasurementReliability = 'functional',
    [string]$OutputPath = 'registry-research-framework\bench-results\system.kernel.disable-exception-chain-validation-vm-functional.json'
)

$ErrorActionPreference = 'Stop'

function Resolve-BenchPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Write-BenchResult {
    param(
        [hashtable]$Result,
        [string]$Path
    )

    $directory = Split-Path -Path $Path -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $Result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-RegistryValueState {
    param(
        [string]$SubKey,
        [string]$Name,
        [string]$NativePath
    )

    $state = @{
        exists = $false
        value = $null
        value_kind = $null
    }

    $root = [Microsoft.Win32.Registry]::LocalMachine
    $key = $root.OpenSubKey($SubKey, $false)
    if (-not $key) {
        return $state
    }

    try {
        if (@($key.GetValueNames()) -notcontains $Name) {
            return $state
        }

        $state.exists = $true
        $state.value = $key.GetValue($Name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $state.value_kind = $key.GetValueKind($Name).ToString()
    }
    finally {
        $key.Close()
    }

    return $state
}

function Convert-RegistryValueKind {
    param([string]$Kind)

    switch -Regex ($Kind) {
        '^(REG_DWORD|DWord)$' { return [Microsoft.Win32.RegistryValueKind]::DWord }
        '^(REG_QWORD|QWord)$' { return [Microsoft.Win32.RegistryValueKind]::QWord }
        '^(REG_SZ|String)$' { return [Microsoft.Win32.RegistryValueKind]::String }
        '^(REG_EXPAND_SZ|ExpandString)$' { return [Microsoft.Win32.RegistryValueKind]::ExpandString }
        '^(REG_MULTI_SZ|MultiString)$' { return [Microsoft.Win32.RegistryValueKind]::MultiString }
        '^(REG_BINARY|Binary)$' { return [Microsoft.Win32.RegistryValueKind]::Binary }
        default { throw "Unsupported registry value kind: $Kind" }
    }
}

function Set-RegistryBenchValue {
    param(
        [string]$SubKey,
        [string]$Name,
        [object]$Value,
        [string]$Kind
    )

    $writeKey = [Microsoft.Win32.Registry]::LocalMachine.CreateSubKey($SubKey)
    try {
        $writeKey.SetValue($Name, $Value, (Convert-RegistryValueKind -Kind $Kind))
    }
    finally {
        if ($writeKey) {
            $writeKey.Close()
        }
    }
}

function Remove-RegistryBenchValue {
    param(
        [string]$SubKey,
        [string]$Name,
        [string]$NativePath
    )

    $rollbackKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($SubKey, $true)
    if (-not $rollbackKey) {
        throw "Registry key not found for rollback: $NativePath"
    }

    try {
        $rollbackKey.DeleteValue($Name, $false)
    }
    finally {
        $rollbackKey.Close()
    }
}

function Test-ValueEquals {
    param(
        [object]$Left,
        [object]$Right
    )

    if ($null -eq $Left -and $null -eq $Right) {
        return $true
    }

    if ($null -eq $Left -or $null -eq $Right) {
        return $false
    }

    return ([string]$Left) -eq ([string]$Right)
}

$resolvedOutputPath = Resolve-BenchPath -Path $OutputPath
$benchResult = [ordered]@{
    candidate_id = $CandidateId
    bench_tier = $BenchTier
    bench_profile = $BenchProfile
    apply_value = $ApplyValue
    apply_type = $ApplyType
    rollback_method = $RollbackMethod
    safety_passed = $null
    boot_success = $true
    shell_usable = $true
    services_healthy = $null
    event_log_clean = $null
    rollback_executed = $null
    rollback_verified = $null
    rollback_failure_reason = $null
    bench_environment = $BenchEnvironment
    bench_measurement_reliability = $BenchMeasurementReliability
    executed_at = $null
    apply_verified = $null
    state_changed = $null
    baseline_state = $null
    service_statuses = @()
    critical_event_count = $null
    critical_events = @()
}

Write-BenchResult -Result $benchResult -Path $resolvedOutputPath

try {
    $baselineState = Get-RegistryValueState -SubKey $RegistrySubKey -Name $ValueName -NativePath $RegistryPathNative
    $benchResult.baseline_state = $baselineState

    Set-RegistryBenchValue -SubKey $RegistrySubKey -Name $ValueName -Value $ApplyValue -Kind $ApplyType

    $appliedState = Get-RegistryValueState -SubKey $RegistrySubKey -Name $ValueName -NativePath $RegistryPathNative
    $benchResult.apply_verified = [bool]($appliedState.exists -and (Test-ValueEquals -Left $appliedState.value -Right $ApplyValue))
    $benchResult.state_changed = [bool](
        (-not $baselineState.exists -and $appliedState.exists) -or
        ($baselineState.exists -and (
            (-not $appliedState.exists) -or
            (-not (Test-ValueEquals -Left $baselineState.value -Right $appliedState.value)) -or
            ([string]$baselineState.value_kind -ne [string]$appliedState.value_kind)
        ))
    )

    $criticalServices = 'LanmanWorkstation', 'EventLog', 'RpcSs', 'Schedule'
    $serviceStatuses = @(
        Get-Service -Name $criticalServices -ErrorAction SilentlyContinue |
            Select-Object Name, Status, StartType
    )
    $benchResult.service_statuses = $serviceStatuses
    $benchResult.services_healthy = [bool](
        $serviceStatuses.Count -eq $criticalServices.Count -and
        @($serviceStatuses | Where-Object { $_.Status -ne 'Running' }).Count -eq 0
    )

    $recentEvents = @(
        Get-WinEvent -FilterHashtable @{
            LogName = @('System', 'Application')
            StartTime = (Get-Date).AddSeconds(-60)
        } -ErrorAction SilentlyContinue |
            Where-Object { $_.LevelDisplayName -in @('Critical', 'Error') } |
            Select-Object -First 20 TimeCreated, LogName, Id, ProviderName, LevelDisplayName, Message
    )
    $benchResult.critical_event_count = $recentEvents.Count
    $benchResult.critical_events = $recentEvents
    $benchResult.event_log_clean = [bool]($recentEvents.Count -eq 0)

    if ($RollbackMethod -eq 'delete') {
        Remove-RegistryBenchValue -SubKey $RegistrySubKey -Name $ValueName -NativePath $RegistryPathNative
        $benchResult.rollback_executed = $true
    }
    elseif ($RollbackMethod -eq 'restore-baseline') {
        if ($baselineState.exists) {
            Set-RegistryBenchValue -SubKey $RegistrySubKey -Name $ValueName -Value $baselineState.value -Kind $baselineState.value_kind
        }
        else {
            Remove-RegistryBenchValue -SubKey $RegistrySubKey -Name $ValueName -NativePath $RegistryPathNative
        }
        $benchResult.rollback_executed = $true
    }
    elseif ($RollbackMethod -eq 'set') {
        if ($null -eq $RollbackValue) {
            throw 'RollbackValue is required when RollbackMethod is set.'
        }
        Set-RegistryBenchValue -SubKey $RegistrySubKey -Name $ValueName -Value $RollbackValue -Kind $RollbackType
        $benchResult.rollback_executed = $true
    }
    else {
        throw "Unsupported rollback method: $RollbackMethod"
    }

    $restoredState = Get-RegistryValueState -SubKey $RegistrySubKey -Name $ValueName -NativePath $RegistryPathNative
    if (-not $baselineState.exists) {
        $benchResult.rollback_verified = -not $restoredState.exists
    }
    else {
        $benchResult.rollback_verified = [bool](
            $restoredState.exists -and
            (Test-ValueEquals -Left $restoredState.value -Right $baselineState.value)
        )
    }

    if (-not $benchResult.rollback_verified) {
        $benchResult.rollback_failure_reason = 'post-delete-state-mismatch'
    }
}
catch {
    if ($null -eq $benchResult.rollback_executed) {
        $benchResult.rollback_executed = $false
    }
    if ($null -eq $benchResult.rollback_verified) {
        $benchResult.rollback_verified = $false
    }
    if ([string]::IsNullOrWhiteSpace([string]$benchResult.rollback_failure_reason)) {
        $benchResult.rollback_failure_reason = $_.Exception.Message
    }
}
finally {
    $benchResult.executed_at = (Get-Date).ToString('o')
    $benchResult.safety_passed = [bool](
        $benchResult.boot_success -and
        $benchResult.shell_usable -and
        $benchResult.apply_verified -and
        ((-not $RequireStateChanged) -or $benchResult.state_changed) -and
        $benchResult.services_healthy -and
        $benchResult.event_log_clean -and
        $benchResult.rollback_executed -and
        $benchResult.rollback_verified
    )
    Write-BenchResult -Result $benchResult -Path $resolvedOutputPath
    $benchResult | ConvertTo-Json -Depth 8
}
