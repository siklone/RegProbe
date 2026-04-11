[CmdletBinding()]
param(
    [string]$CandidateId,
    [string]$RegistrySubKey,
    [string[]]$ValueNames,
    [int[]]$ApplyValues,
    [string[]]$ApplyTypes,
    [string]$RollbackMethod = 'restore-baseline',
    [string]$MeasurementProfile = 'system',
    [int]$SampleCount = 3,
    [int]$RegistryReadIterations = 400,
    [string]$BenchTier = 'vm',
    [string]$BenchProfile = 'performance',
    [string]$BenchEnvironment = 'windows-11-25h2-vm',
    [string]$BenchMeasurementReliability = 'relative',
    [string]$OutputPath
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

    $Result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Convert-RegistryValueKind {
    param([string]$Kind)

    switch -Regex ($Kind) {
        '^(REG_DWORD|DWord)$' { return [Microsoft.Win32.RegistryValueKind]::DWord }
        default { throw "Unsupported registry value kind: $Kind" }
    }
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
        return
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

function Get-Median {
    param([double[]]$Values)

    if (-not $Values -or $Values.Count -eq 0) {
        return $null
    }

    $sorted = @($Values | Sort-Object)
    $mid = [int]($sorted.Count / 2)
    if (($sorted.Count % 2) -eq 1) {
        return [Math]::Round([double]$sorted[$mid], 3)
    }

    return [Math]::Round((([double]$sorted[$mid - 1]) + ([double]$sorted[$mid])) / 2.0, 3)
}

function Measure-ServiceQueryMs {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    Get-Service -Name @('LanmanWorkstation', 'EventLog', 'RpcSs', 'Schedule') -ErrorAction SilentlyContinue | Out-Null
    $stopwatch.Stop()
    return [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 3)
}

function Measure-BootEventQuery {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $events = @(
        Get-WinEvent -FilterHashtable @{
            LogName = 'System'
            Id = @(12, 13, 6005, 6006)
        } -MaxEvents 8 -ErrorAction SilentlyContinue |
            Select-Object -First 4 TimeCreated, Id, ProviderName
    )
    $stopwatch.Stop()
    return [ordered]@{
        latency_ms = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 3)
        events = $events
    }
}

function Measure-RegistryQueryMs {
    param(
        [string]$SubKey,
        [string[]]$Names,
        [int]$Iterations
    )

    $root = [Microsoft.Win32.Registry]::LocalMachine
    $key = $root.OpenSubKey($SubKey, $false)
    if (-not $key) {
        throw "Registry key not found during measurement: $SubKey"
    }

    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        for ($i = 0; $i -lt $Iterations; $i++) {
            foreach ($name in $Names) {
                $null = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            }
        }
        $stopwatch.Stop()
        return [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 3)
    }
    finally {
        $key.Close()
    }
}

function Measure-PerfSample {
    param(
        [string]$Profile,
        [string]$SubKey,
        [string[]]$Names,
        [int]$Iterations
    )

    $serviceMs = Measure-ServiceQueryMs
    if ($Profile -eq 'power') {
        $bootEvent = Measure-BootEventQuery
        return [ordered]@{
            service_query_ms = $serviceMs
            boot_event_query_ms = $bootEvent.latency_ms
            total_ms = [Math]::Round($serviceMs + [double]$bootEvent.latency_ms, 3)
            boot_reference_events = $bootEvent.events
            measurement_components = @('service-query-ms', 'boot-event-query-ms')
        }
    }

    $registryMs = Measure-RegistryQueryMs -SubKey $SubKey -Names $Names -Iterations $Iterations
    return [ordered]@{
        service_query_ms = $serviceMs
        registry_query_ms = $registryMs
        total_ms = [Math]::Round($serviceMs + $registryMs, 3)
        measurement_components = @('service-query-ms', 'registry-query-ms')
    }
}

if (-not $ValueNames -or $ValueNames.Count -eq 0) {
    throw 'ValueNames is required.'
}
if ($ValueNames.Count -ne $ApplyValues.Count -or $ValueNames.Count -ne $ApplyTypes.Count) {
    throw 'ValueNames, ApplyValues, and ApplyTypes must have the same length.'
}

$resolvedOutputPath = Resolve-BenchPath -Path $OutputPath
$benchResult = [ordered]@{
    candidate_id = $CandidateId
    bench_tier = $BenchTier
    bench_profile = $BenchProfile
    bench_measurement_reliability = $BenchMeasurementReliability
    measurement_profile = $MeasurementProfile
    measurement_components = @()
    sample_count = $SampleCount
    registry_read_iterations = $RegistryReadIterations
    apply_value = [ordered]@{}
    apply_type = if ($ValueNames.Count -eq 1) { $ApplyTypes[0] } else { 'REG_DWORD pair' }
    rollback_method = $RollbackMethod
    bench_environment = $BenchEnvironment
    bench_bare_metal_pending = $true
    baseline_state = [ordered]@{}
    restored_state = [ordered]@{}
    baseline_samples = @()
    applied_samples = @()
    baseline_median_ms = $null
    applied_median_ms = $null
    delta_ms = $null
    delta_pct = $null
    apply_verified = $null
    rollback_verified = $null
    rollback_failure_reason = $null
    executed_at = $null
}

for ($i = 0; $i -lt $ValueNames.Count; $i++) {
    $benchResult.apply_value[$ValueNames[$i]] = $ApplyValues[$i]
}

Write-BenchResult -Result $benchResult -Path $resolvedOutputPath

$valuesApplied = $false

try {
    foreach ($name in $ValueNames) {
        $benchResult.baseline_state[$name] = Get-RegistryValueState -SubKey $RegistrySubKey -Name $name
    }

    $baselineSamples = @()
    for ($sampleIndex = 0; $sampleIndex -lt $SampleCount; $sampleIndex++) {
        $sample = Measure-PerfSample -Profile $MeasurementProfile -SubKey $RegistrySubKey -Names $ValueNames -Iterations $RegistryReadIterations
        $baselineSamples += $sample
    }
    $benchResult.baseline_samples = $baselineSamples
    $benchResult.measurement_components = @($baselineSamples[0].measurement_components)
    $benchResult.baseline_median_ms = Get-Median -Values @($baselineSamples | ForEach-Object { [double]$_.total_ms })

    for ($i = 0; $i -lt $ValueNames.Count; $i++) {
        Set-RegistryBenchValue -SubKey $RegistrySubKey -Name $ValueNames[$i] -Value $ApplyValues[$i] -Kind $ApplyTypes[$i]
    }
    $valuesApplied = $true

    $applyVerified = $true
    for ($i = 0; $i -lt $ValueNames.Count; $i++) {
        $appliedState = Get-RegistryValueState -SubKey $RegistrySubKey -Name $ValueNames[$i]
        $applyVerified = [bool](
            $applyVerified -and
            $appliedState.exists -and
            (Test-ValueEquals -Left $appliedState.value -Right $ApplyValues[$i])
        )
    }
    $benchResult.apply_verified = $applyVerified

    $appliedSamples = @()
    for ($sampleIndex = 0; $sampleIndex -lt $SampleCount; $sampleIndex++) {
        $sample = Measure-PerfSample -Profile $MeasurementProfile -SubKey $RegistrySubKey -Names $ValueNames -Iterations $RegistryReadIterations
        $appliedSamples += $sample
    }
    $benchResult.applied_samples = $appliedSamples
    $benchResult.applied_median_ms = Get-Median -Values @($appliedSamples | ForEach-Object { [double]$_.total_ms })
    $benchResult.delta_ms = [Math]::Round(([double]$benchResult.applied_median_ms - [double]$benchResult.baseline_median_ms), 3)
    if ([double]$benchResult.baseline_median_ms -ne 0) {
        $benchResult.delta_pct = [Math]::Round((([double]$benchResult.delta_ms / [double]$benchResult.baseline_median_ms) * 100.0), 3)
    }
}
catch {
    if ([string]::IsNullOrWhiteSpace([string]$benchResult.rollback_failure_reason)) {
        $benchResult.rollback_failure_reason = $_.Exception.Message
    }
    if ($null -eq $benchResult.rollback_verified) {
        $benchResult.rollback_verified = $false
    }
}
finally {
    if ($valuesApplied) {
        try {
            if ($RollbackMethod -eq 'delete') {
                foreach ($name in $ValueNames) {
                    Remove-RegistryBenchValue -SubKey $RegistrySubKey -Name $name
                }
            }
            elseif ($RollbackMethod -eq 'restore-baseline') {
                foreach ($name in $ValueNames) {
                    $baseline = $benchResult.baseline_state[$name]
                    if ($baseline.exists) {
                        Set-RegistryBenchValue -SubKey $RegistrySubKey -Name $name -Value $baseline.value -Kind $baseline.value_kind
                    }
                    else {
                        Remove-RegistryBenchValue -SubKey $RegistrySubKey -Name $name
                    }
                }
            }
            else {
                throw "Unsupported rollback method: $RollbackMethod"
            }

            $rollbackVerified = $true
            foreach ($name in $ValueNames) {
                $restored = Get-RegistryValueState -SubKey $RegistrySubKey -Name $name
                $benchResult.restored_state[$name] = $restored
                $rollbackVerified = [bool]($rollbackVerified -and (Test-StateEquals -Left $benchResult.baseline_state[$name] -Right $restored))
            }
            $benchResult.rollback_verified = $rollbackVerified
            if (-not $rollbackVerified -and [string]::IsNullOrWhiteSpace([string]$benchResult.rollback_failure_reason)) {
                $benchResult.rollback_failure_reason = 'post-restore-state-mismatch'
            }
        }
        catch {
            $benchResult.rollback_verified = $false
            if ([string]::IsNullOrWhiteSpace([string]$benchResult.rollback_failure_reason)) {
                $benchResult.rollback_failure_reason = $_.Exception.Message
            }
        }
    }

    $benchResult.executed_at = (Get-Date).ToString('o')
    Write-BenchResult -Result $benchResult -Path $resolvedOutputPath
    $benchResult | ConvertTo-Json -Depth 10
}
