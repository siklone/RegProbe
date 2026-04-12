[CmdletBinding()]
param(
    [string]$GuestRoot = ".",
    [string]$OutputPath = "C:\regprobe-dpc-timer-etw",
    [int]$TraceSeconds = 45,
    [string]$UploadBaseUrl = "",
    [string]$UploadPrefix = "dpc-timer-etw"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

$traceName = "regprobe-dpc-timer-registry"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$etlPath = Join-Path $OutputPath "dpc-timer-registry-$stamp.etl"
$xmlPath = Join-Path $OutputPath "dpc-timer-registry-$stamp.xml"
$hitsPath = Join-Path $OutputPath "target-hits-$stamp.txt"
$summaryPath = Join-Path $OutputPath "trace-summary.json"

function Join-UploadUri {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseUrl,
        [Parameter(Mandatory = $true)]
        [string]$RemoteName
    )

    return ("{0}/{1}" -f $BaseUrl.TrimEnd("/"), $RemoteName.TrimStart("/"))
}

function Invoke-ArtifactUpload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path $Path)) {
        return $null
    }

    $uri = Join-UploadUri -BaseUrl $UploadBaseUrl -RemoteName $RemoteName
    try {
        Invoke-WebRequest -Method Put -Uri $uri -InFile $Path -UseBasicParsing | Out-Null
        $item = Get-Item $Path
        return @{
            path = $Path
            remote_name = $RemoteName
            uri = $uri
            size_bytes = $item.Length
            status = "uploaded"
        }
    }
    catch {
        return @{
            path = $Path
            remote_name = $RemoteName
            uri = $uri
            size_bytes = 0
            status = "upload-error"
            error = $_.Exception.Message
        }
    }
}

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
    target_hits_path = $hitsPath
    target_hits_exists = $false
    target_hits_count = 0
    logman_create_exit_code = $null
    logman_update_results = @()
    logman_start_exit_code = $null
    logman_stop_exit_code = $null
    tracerpt_exit_code = $null
    trace_seconds = $TraceSeconds
    uploads = [ordered]@{}
    collected_utc = $null
    status = "started"
    error = $null
}

try {
    Invoke-Logman -Arguments @("stop", $traceName) -IgnoreFailure | Out-Null
    Invoke-Logman -Arguments @("delete", $traceName) -IgnoreFailure | Out-Null

    Get-ChildItem -Path $OutputPath -File -Filter "dpc-timer-registry*.etl" -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path $OutputPath -File -Filter "dpc-timer-registry*.xml" -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path $OutputPath -File -Filter "target-hits*.txt" -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue

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

    Write-Host "Trace started, waiting $TraceSeconds seconds..."
    Start-Sleep -Seconds $TraceSeconds

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

            $targetPattern = "TimerCheckFlags|LongDpc|ForceBugcheck|DpcWatchdog|DpcWatchdogProfile|DpcWatchdogPeriod|KeTimerCheckFlags"
            $hits = @(Select-String -Path $xmlPath -Pattern $targetPattern -AllMatches | Select-Object -First 200)
            if ($hits.Count -gt 0) {
                $hits | ForEach-Object { "{0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line.Trim() } |
                    Set-Content -Path $hitsPath -Encoding UTF8
                $summary.target_hits_exists = $true
                $summary.target_hits_count = $hits.Count
                Write-Host "Target hits produced: $hitsPath ($($hits.Count) lines)"
            }
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

    if (-not [string]::IsNullOrWhiteSpace($UploadBaseUrl)) {
        $summary.uploads["summary"] = Invoke-ArtifactUpload -Path $summaryPath -RemoteName "$UploadPrefix/trace-summary.json"
        $summary.uploads["target_hits"] = Invoke-ArtifactUpload -Path $hitsPath -RemoteName "$UploadPrefix/target-hits.txt"
        $summary.uploads["etl"] = Invoke-ArtifactUpload -Path $summary.etl_path -RemoteName "$UploadPrefix/dpc-timer-registry.etl"
        $summary.uploads["xml"] = Invoke-ArtifactUpload -Path $xmlPath -RemoteName "$UploadPrefix/dpc-timer-registry.xml"
        $summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
        $summary.uploads["summary"] = Invoke-ArtifactUpload -Path $summaryPath -RemoteName "$UploadPrefix/trace-summary.json"
    }

    Write-Host "Summary: $summaryPath"
}
