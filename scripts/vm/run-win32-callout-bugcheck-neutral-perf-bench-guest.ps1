[CmdletBinding()]
param(
    [string]$OutputName = 'win32-callout-bugcheck-neutral-perf-20260412b',
    [string]$UploadBaseUrl = $(if ($env:REGPROBE_VM_BRIDGE_BASE_URL) { $env:REGPROBE_VM_BRIDGE_BASE_URL } else { 'http://10.0.2.2:8766' })
)

$ErrorActionPreference = 'Stop'

$candidateId = 'power.session-win32-callout-watchdog-bugcheck-enabled'
$registrySubKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Power'
$valueName = 'Win32CalloutWatchdogBugcheckEnabled'
$outputRoot = Join-Path 'C:\RegProbe-Diag\bench' $OutputName
$benchScriptPath = Join-Path $outputRoot 'run-perf-bench-guest.ps1'
$benchResultPath = Join-Path $outputRoot ($OutputName + '.json')
$resultPath = Join-Path $outputRoot ($OutputName + '.txt')
$summaryPath = Join-Path $outputRoot ($OutputName + '-summary.json')
$scriptUri = 'http://10.0.2.2:8766/registry-research-framework/scripts/vm/run-perf-bench-guest.ps1'

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

function Upload-Artifact {
    param(
        [string]$Path,
        [string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path $Path)) {
        return $null
    }

    $uri = ('{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName)
    Invoke-WebRequest -Method Put -Uri $uri -InFile $Path -UseBasicParsing | Out-Null
    return $uri
}

$lines = New-Object System.Collections.Generic.List[string]
$summary = [ordered]@{
    generated_utc = (Get-Date).ToUniversalTime().ToString('o')
    output_name = $OutputName
    candidate_id = $candidateId
    registry_sub_key = $registrySubKey
    value_name = $valueName
    apply_value = 0
    apply_type = 'REG_DWORD'
    rollback_method = 'restore-baseline'
    status = 'starting'
    bench_result_path = $benchResultPath
    bench_result_exists = $false
    rollback_verified = $null
    error = $null
    uploads = [ordered]@{}
}

try {
    $lines.Add('OUTPUT_NAME=' + $OutputName)
    $lines.Add('CANDIDATE_ID=' + $candidateId)
    $lines.Add('REGISTRY_SUB_KEY=' + $registrySubKey)
    $lines.Add('VALUE_NAME=' + $valueName)
    $lines.Add('APPLY_VALUE=0')
    $lines.Add('ROLLBACK_METHOD=restore-baseline')

    Invoke-WebRequest -UseBasicParsing -Uri $scriptUri -OutFile $benchScriptPath

    $benchJson = & $benchScriptPath `
        -CandidateId $candidateId `
        -RegistrySubKey $registrySubKey `
        -ValueNames @($valueName) `
        -ApplyValues @(0) `
        -ApplyTypes @('REG_DWORD') `
        -RollbackMethod 'restore-baseline' `
        -MeasurementProfile 'system' `
        -SampleCount 3 `
        -RegistryReadIterations 400 `
        -BenchTier 'vm' `
        -BenchProfile 'performance' `
        -BenchEnvironment 'windows-11-25h2-kvm' `
        -BenchMeasurementReliability 'relative-neutral-value' `
        -OutputPath $benchResultPath

    $bench = Get-Content -Path $benchResultPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    $summary.status = if ($bench.rollback_verified -and $bench.apply_verified) { 'ok' } else { 'bench-warning' }
    $summary.bench_result_exists = Test-Path $benchResultPath
    $summary.apply_verified = [bool]$bench.apply_verified
    $summary.rollback_verified = [bool]$bench.rollback_verified
    $summary.baseline_median_ms = $bench.baseline_median_ms
    $summary.applied_median_ms = $bench.applied_median_ms
    $summary.delta_ms = $bench.delta_ms
    $summary.delta_pct = $bench.delta_pct
    $summary.executed_at = $bench.executed_at
    $summary.bench_measurement_reliability = $bench.bench_measurement_reliability

    $lines.Add('BENCH_RESULT=' + (($benchJson | ForEach-Object { [string]$_ }) -join ' '))
}
catch {
    $summary.status = 'error'
    $summary.error = $_.Exception.Message
    $lines.Add('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message)
}
finally {
    $lines | Set-Content -Path $resultPath -Encoding UTF8
    $summary | ConvertTo-Json -Depth 10 | Set-Content -Path $summaryPath -Encoding UTF8

    foreach ($artifact in @(
        @{ path = $benchResultPath; name = ($OutputName + '.json') },
        @{ path = $summaryPath; name = ($OutputName + '-summary.json') },
        @{ path = $resultPath; name = ($OutputName + '.txt') }
    )) {
        try {
            $uploaded = Upload-Artifact -Path $artifact.path -RemoteName $artifact.name
            if ($uploaded) {
                $summary.uploads[$artifact.name] = $uploaded
            }
        }
        catch {
            $lines.Add('UPLOAD_ERROR=' + $artifact.name + ': ' + $_.Exception.Message)
        }
    }

    $summary | ConvertTo-Json -Depth 10 | Set-Content -Path $summaryPath -Encoding UTF8
}

if ($summary.status -eq 'error') {
    exit 1
}
