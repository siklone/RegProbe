[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = $(if ($env:REGPROBE_VM_REPO_ROOT) { $env:REGPROBE_VM_REPO_ROOT } else { Join-Path $env:USERPROFILE 'RegProbe-codex-legacy-dirty-main-20260407' })
$registrySubKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Executive'
$registryPathNative = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive'
$outputPath = Join-Path $repoRoot 'registry-research-framework\bench-results\system.executive-additional-worker-threads-vm-functional.json'
$values = @(
    @{
        Name = 'AdditionalCriticalWorkerThreads'
        ApplyValue = 1
        ApplyType = 'REG_DWORD'
    },
    @{
        Name = 'AdditionalDelayedWorkerThreads'
        ApplyValue = 1
        ApplyType = 'REG_DWORD'
    }
)

Set-Location $repoRoot

function Write-BenchResult {
    param(
        [hashtable]$Result,
        [string]$Path
    )

    $directory = Split-Path -Path $Path -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $Result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-RegistryValueState {
    param(
        [string]$SubKey,
        [string]$Name
    )

    $state = [ordered]@{
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
        [string]$Name
    )

    $rollbackKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($SubKey, $true)
    if (-not $rollbackKey) {
        throw "Registry key not found for rollback: $registryPathNative"
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

function Test-StateEquals {
    param(
        [object]$Left,
        [object]$Right
    )

    if ([bool]$Left.exists -ne [bool]$Right.exists) {
        return $false
    }

    if (-not [bool]$Left.exists) {
        return $true
    }

    return (
        (Test-ValueEquals -Left $Left.value -Right $Right.value) -and
        ([string]$Left.value_kind -eq [string]$Right.value_kind)
    )
}

$benchResult = [ordered]@{
    candidate_id = 'system.executive-additional-worker-threads'
    bench_tier = 'vm'
    bench_profile = 'functional'
    apply_value = [ordered]@{
        AdditionalCriticalWorkerThreads = 1
        AdditionalDelayedWorkerThreads = 1
    }
    apply_type = 'REG_DWORD pair'
    rollback_method = 'restore-baseline'
    safety_passed = $null
    boot_success = $true
    shell_usable = $true
    services_healthy = $null
    event_log_clean = $null
    rollback_executed = $null
    rollback_verified = $null
    rollback_failure_reason = $null
    bench_environment = 'windows-11-25h2-vm'
    bench_measurement_reliability = 'functional'
    executed_at = $null
    apply_verified = $null
    state_changed = $null
    baseline_state = [ordered]@{}
    applied_state = [ordered]@{}
    restored_state = [ordered]@{}
    value_results = @()
    service_statuses = @()
    critical_event_count = $null
    critical_events = @()
}

Write-BenchResult -Result $benchResult -Path $outputPath

try {
    foreach ($value in $values) {
        $benchResult.baseline_state[$value.Name] = Get-RegistryValueState -SubKey $registrySubKey -Name $value.Name
    }

    foreach ($value in $values) {
        Set-RegistryBenchValue -SubKey $registrySubKey -Name $value.Name -Value $value.ApplyValue -Kind $value.ApplyType
    }

    $allApplied = $true
    $anyChanged = $false
    $valueResults = @()
    foreach ($value in $values) {
        $applied = Get-RegistryValueState -SubKey $registrySubKey -Name $value.Name
        $benchResult.applied_state[$value.Name] = $applied
        $baseline = $benchResult.baseline_state[$value.Name]
        $appliedMatches = [bool]($applied.exists -and (Test-ValueEquals -Left $applied.value -Right $value.ApplyValue))
        $changed = -not (Test-StateEquals -Left $baseline -Right $applied)
        $allApplied = [bool]($allApplied -and $appliedMatches)
        $anyChanged = [bool]($anyChanged -or $changed)
        $valueResults += [ordered]@{
            value_name = $value.Name
            apply_value = $value.ApplyValue
            apply_verified = $appliedMatches
            state_changed = $changed
        }
    }
    $benchResult.value_results = $valueResults
    $benchResult.apply_verified = $allApplied
    $benchResult.state_changed = $anyChanged

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
}
catch {
    if ([string]::IsNullOrWhiteSpace([string]$benchResult.rollback_failure_reason)) {
        $benchResult.rollback_failure_reason = $_.Exception.Message
    }
}
finally {
    try {
        if ($benchResult.baseline_state.Count -gt 0) {
            foreach ($value in $values) {
                $baseline = $benchResult.baseline_state[$value.Name]
                if ($baseline.exists) {
                    Set-RegistryBenchValue -SubKey $registrySubKey -Name $value.Name -Value $baseline.value -Kind $baseline.value_kind
                }
                else {
                    Remove-RegistryBenchValue -SubKey $registrySubKey -Name $value.Name
                }
                $benchResult.restored_state[$value.Name] = Get-RegistryValueState -SubKey $registrySubKey -Name $value.Name
            }
            $benchResult.rollback_executed = $true

            $allRestored = $true
            foreach ($value in $values) {
                $allRestored = [bool](
                    $allRestored -and
                    (Test-StateEquals -Left $benchResult.baseline_state[$value.Name] -Right $benchResult.restored_state[$value.Name])
                )
            }
            $benchResult.rollback_verified = $allRestored
            if (-not $allRestored -and [string]::IsNullOrWhiteSpace([string]$benchResult.rollback_failure_reason)) {
                $benchResult.rollback_failure_reason = 'post-restore-state-mismatch'
            }
        }
        else {
            $benchResult.rollback_executed = $false
            $benchResult.rollback_verified = $false
        }
    }
    catch {
        $benchResult.rollback_executed = $false
        $benchResult.rollback_verified = $false
        if ([string]::IsNullOrWhiteSpace([string]$benchResult.rollback_failure_reason)) {
            $benchResult.rollback_failure_reason = $_.Exception.Message
        }
    }

    $benchResult.executed_at = (Get-Date).ToString('o')
    $benchResult.safety_passed = [bool](
        $benchResult.boot_success -and
        $benchResult.shell_usable -and
        $benchResult.apply_verified -and
        $benchResult.state_changed -and
        $benchResult.services_healthy -and
        $benchResult.event_log_clean -and
        $benchResult.rollback_executed -and
        $benchResult.rollback_verified
    )

    Write-BenchResult -Result $benchResult -Path $outputPath
    $benchResult | ConvertTo-Json -Depth 10
}
