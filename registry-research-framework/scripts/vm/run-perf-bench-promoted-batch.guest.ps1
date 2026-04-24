[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = $(if ($env:REGPROBE_VM_REPO_ROOT) { $env:REGPROBE_VM_REPO_ROOT } else { Join-Path $env:USERPROFILE 'RegProbe-codex-legacy-dirty-main-20260407' })
$runner = @(
    Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*PERF*.PS1' |
        Where-Object { $_.Name -notmatch 'BATCH' } |
        Select-Object -First 1
)[0]
if (-not $runner) {
    throw "Perf bench guest runner not found under $PSScriptRoot"
}

Set-Location $repoRoot

$cases = @(
    @{
        CandidateId = 'power.control.class1-initial-unpark-count'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Power'
        ValueNames = @('Class1InitialUnparkCount')
        ApplyValues = @(63)
        ApplyTypes = @('REG_DWORD')
        RollbackMethod = 'restore-baseline'
        MeasurementProfile = 'power'
    },
    @{
        CandidateId = 'power.control.hiber-file-size-percent'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Power'
        ValueNames = @('HiberFileSizePercent')
        ApplyValues = @(1)
        ApplyTypes = @('REG_DWORD')
        RollbackMethod = 'restore-baseline'
        MeasurementProfile = 'power'
    },
    @{
        CandidateId = 'power.control.hibernate-enabled'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Power'
        ValueNames = @('HibernateEnabled')
        ApplyValues = @(1)
        ApplyTypes = @('REG_DWORD')
        RollbackMethod = 'restore-baseline'
        MeasurementProfile = 'power'
    },
    @{
        CandidateId = 'power.control.lid-reliability-state'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Power'
        ValueNames = @('LidReliabilityState')
        ApplyValues = @(0)
        ApplyTypes = @('REG_DWORD')
        RollbackMethod = 'restore-baseline'
        MeasurementProfile = 'power'
    },
    @{
        CandidateId = 'power.control.mf-buffering-threshold'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Power'
        ValueNames = @('MfBufferingThreshold')
        ApplyValues = @(1)
        ApplyTypes = @('REG_DWORD')
        RollbackMethod = 'restore-baseline'
        MeasurementProfile = 'power'
    },
    @{
        CandidateId = 'power.control.perf-calculate-actual-utilization'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Power'
        ValueNames = @('PerfCalculateActualUtilization')
        ApplyValues = @(0)
        ApplyTypes = @('REG_DWORD')
        RollbackMethod = 'restore-baseline'
        MeasurementProfile = 'power'
    },
    @{
        CandidateId = 'system.executive-additional-worker-threads'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Executive'
        ValueNames = @('AdditionalCriticalWorkerThreads', 'AdditionalDelayedWorkerThreads')
        ApplyValues = @(1, 1)
        ApplyTypes = @('REG_DWORD', 'REG_DWORD')
        RollbackMethod = 'restore-baseline'
        MeasurementProfile = 'system'
    },
    @{
        CandidateId = 'system.kernel.disable-exception-chain-validation'
        RegistrySubKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Kernel'
        ValueNames = @('DisableExceptionChainValidation')
        ApplyValues = @(1)
        ApplyTypes = @('REG_DWORD')
        RollbackMethod = 'delete'
        MeasurementProfile = 'system'
    }
)

$summary = [ordered]@{
    bench_tier = 'vm'
    bench_profile = 'performance'
    bench_measurement_reliability = 'relative'
    bench_bare_metal_pending = $true
    candidate_count = $cases.Count
    executed_at = $null
    results = @()
}

foreach ($case in $cases) {
    $outputPath = 'registry-research-framework\bench-results\{0}-vm-performance.json' -f $case.CandidateId
    & $runner.FullName `
        -CandidateId $case.CandidateId `
        -RegistrySubKey $case.RegistrySubKey `
        -ValueNames $case.ValueNames `
        -ApplyValues $case.ApplyValues `
        -ApplyTypes $case.ApplyTypes `
        -RollbackMethod $case.RollbackMethod `
        -MeasurementProfile $case.MeasurementProfile `
        -BenchTier 'vm' `
        -BenchProfile 'performance' `
        -BenchEnvironment 'windows-11-25h2-vm' `
        -BenchMeasurementReliability 'relative' `
        -OutputPath $outputPath | Out-Null

    $result = Get-Content -Raw -LiteralPath (Join-Path $repoRoot $outputPath) | ConvertFrom-Json
    $summary.results += [ordered]@{
        candidate_id = $result.candidate_id
        baseline_median_ms = $result.baseline_median_ms
        applied_median_ms = $result.applied_median_ms
        delta_ms = $result.delta_ms
        delta_pct = $result.delta_pct
        rollback_verified = [bool]$result.rollback_verified
        output_file = $outputPath
    }
}

$summary.executed_at = (Get-Date).ToString('o')
$summaryPath = Join-Path $repoRoot 'registry-research-framework\bench-results\promoted-vm-performance-summary.json'
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

foreach ($item in $summary.results) {
    '{0} | baseline={1}ms | applied={2}ms | delta={3}ms | pct={4}% | rollback={5}' -f `
        $item.candidate_id, `
        $item.baseline_median_ms, `
        $item.applied_median_ms, `
        $item.delta_ms, `
        $item.delta_pct, `
        $item.rollback_verified
}

$summary | ConvertTo-Json -Depth 8
