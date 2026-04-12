[CmdletBinding()]
param(
    [string]$GuestRoot = ".",
    [string]$OutputPath = "C:\regprobe-dpc-timer-etw"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

$traceName = "regprobe-dpc-timer-registry"
$etlPath = Join-Path $OutputPath "dpc-timer-registry.etl"
$xmlPath = Join-Path $OutputPath "dpc-timer-registry.xml"
$summaryPath = Join-Path $OutputPath "trace-summary.json"

function Invoke-Logman {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$IgnoreFailure
    )

    $output = & logman @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if (($exitCode -ne 0) -and (-not $IgnoreFailure)) {
        throw "logman $($Arguments -join ' ') failed with exit code ${exitCode}: $output"
    }
    return @{
        exit_code = $exitCode
        output = @($output)
    }
}

$summary = [ordered]@{
    trace_name = $traceName
    etl_path = $etlPath
    etl_exists = $false
    etl_size_bytes = 0
    xml_path = $xmlPath
    xml_exists = $false
    xml_size_bytes = 0
    logman_create_exit_code = $null
    logman_update_results = @()
    logman_start_exit_code = $null
    logman_stop_exit_code = $null
    tracerpt_exit_code = $null
    collected_utc = $null
    status = "started"
    error = $null
}

try {
    Invoke-Logman -Arguments @("stop", $traceName) -IgnoreFailure | Out-Null
    Invoke-Logman -Arguments @("delete", $traceName) -IgnoreFailure | Out-Null

    if (Test-Path $etlPath) {
        Remove-Item -Force $etlPath
    }
    if (Test-Path $xmlPath) {
        Remove-Item -Force $xmlPath
    }

    $create = Invoke-Logman -Arguments @(
        "create", "trace", $traceName,
        "-o", $etlPath,
        "-f", "bincirc",
        "-max", "64",
        "-nb", "16", "512"
    )
    $summary.logman_create_exit_code = $create.exit_code

    $providers = @(
        "Microsoft-Windows-Kernel-Registry",
        "Microsoft-Windows-Kernel-General",
        "Microsoft-Windows-Kernel-Power",
        "Microsoft-Windows-Kernel-Processor-Power"
    )
    foreach ($provider in $providers) {
        $update = Invoke-Logman -Arguments @(
            "update", "trace", $traceName,
            "-p", $provider, "0xFFFF"
        )
        $summary.logman_update_results += @{
            provider = $provider
            exit_code = $update.exit_code
        }
    }

    $start = Invoke-Logman -Arguments @("start", $traceName)
    $summary.logman_start_exit_code = $start.exit_code

    Write-Host "Trace started, waiting 45 seconds..."
    Start-Sleep -Seconds 45

    $stop = Invoke-Logman -Arguments @("stop", $traceName)
    $summary.logman_stop_exit_code = $stop.exit_code

    $actualEtlPath = $etlPath
    if (-not (Test-Path $actualEtlPath)) {
        $etlPrefix = [System.IO.Path]::GetFileNameWithoutExtension($etlPath)
        $candidate = Get-ChildItem -Path $OutputPath -File -Filter "$etlPrefix*.etl" |
            Sort-Object Length -Descending |
            Select-Object -First 1
        if ($candidate) {
            $actualEtlPath = $candidate.FullName
        }
    }

    if (Test-Path $actualEtlPath) {
        $etlItem = Get-Item $actualEtlPath
        $summary.etl_path = $etlItem.FullName
        $summary.etl_exists = $true
        $summary.etl_size_bytes = $etlItem.Length
        Write-Host "ETL produced: $($etlItem.FullName) ($($etlItem.Length) bytes)"

        $tracerptOutput = & tracerpt $etlItem.FullName -o $xmlPath -of XML -lr 2>&1
        $summary.tracerpt_exit_code = $LASTEXITCODE
        if ($LASTEXITCODE -ne 0) {
            Write-Host "tracerpt failed with exit code $LASTEXITCODE"
            Write-Host ($tracerptOutput -join [Environment]::NewLine)
        }

        if (Test-Path $xmlPath) {
            $xmlItem = Get-Item $xmlPath
            $summary.xml_exists = $true
            $summary.xml_size_bytes = $xmlItem.Length
            Write-Host "XML produced: $xmlPath ($($xmlItem.Length) bytes)"
        }
    } else {
        Write-Host "ETL_MISSING"
    }

    $summary.status = if ($summary.etl_exists) { "completed" } else { "etl-missing" }
}
catch {
    $summary.status = "error"
    $summary.error = $_.Exception.Message
    Write-Host "ERROR=$($summary.error)"
}
finally {
    $summary.collected_utc = (Get-Date).ToUniversalTime().ToString("o")
    $summary | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8
    Write-Host "Summary: $summaryPath"
}
