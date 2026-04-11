[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = 'C:\Users\rai\RegProbe-codex-legacy-dirty-main-20260407'
$runner = @(
    Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*SAFETY*BENCH*GUEST*.PS1' |
        Where-Object { $_.Name -notmatch 'HIBER|BATCH' } |
        Select-Object -First 1
)[0]
if (-not $runner) {
    throw "Safety bench guest runner not found under $PSScriptRoot"
}

Set-Location $repoRoot

$cases = @(
    @{
        CandidateId = 'power.control.mf-buffering-threshold'
        ValueName = 'MfBufferingThreshold'
        ApplyValue = 1
    },
    @{
        CandidateId = 'power.control.lid-reliability-state'
        ValueName = 'LidReliabilityState'
        ApplyValue = 0
    },
    @{
        CandidateId = 'power.control.perf-calculate-actual-utilization'
        ValueName = 'PerfCalculateActualUtilization'
        ApplyValue = 0
    }
)

$summary = [ordered]@{
    bench_tier = 'vm'
    bench_profile = 'functional'
    bench_environment = 'windows-11-25h2-vm'
    candidate_count = $cases.Count
    passed_count = 0
    failed_count = 0
    executed_at = $null
    results = @()
}

foreach ($case in $cases) {
    $outputPath = 'registry-research-framework\bench-results\{0}-vm-functional.json' -f $case.CandidateId
    & $runner.FullName `
        -CandidateId $case.CandidateId `
        -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' `
        -RegistryPathNative 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
        -RegistrySubKey 'SYSTEM\CurrentControlSet\Control\Power' `
        -ValueName $case.ValueName `
        -ApplyValue $case.ApplyValue `
        -ApplyType 'REG_DWORD' `
        -RollbackMethod 'restore-baseline' `
        -BenchTier 'vm' `
        -BenchProfile 'functional' `
        -BenchEnvironment 'windows-11-25h2-vm' `
        -BenchMeasurementReliability 'functional' `
        -OutputPath $outputPath | Out-Null

    $result = Get-Content -Raw -LiteralPath (Join-Path $repoRoot $outputPath) | ConvertFrom-Json
    if ($result.safety_passed) {
        $summary.passed_count++
    }
    else {
        $summary.failed_count++
    }
    $summary.results += [ordered]@{
        candidate_id = $result.candidate_id
        safety_passed = [bool]$result.safety_passed
        state_changed = [bool]$result.state_changed
        rollback_verified = [bool]$result.rollback_verified
        rollback_failure_reason = $result.rollback_failure_reason
        output_file = $outputPath
        executed_at = $result.executed_at
    }
}

$summary.executed_at = (Get-Date).ToString('o')
$summaryPath = Join-Path $repoRoot 'registry-research-framework\bench-results\power-control-low-risk-batch-vm-functional-summary.json'
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8
