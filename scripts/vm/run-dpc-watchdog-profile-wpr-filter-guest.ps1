[CmdletBinding()]
param(
    [string]$CsvPath = 'C:\RegProbe-Diag\wpr-boot-registry\kernel-timing-wpr-boot-registry-20260412\kernel-timing-wpr-boot-registry-20260412.manual.csv',
    [string]$OutputName = 'dpc-watchdog-profile-wpr-filter-20260412a',
    [string]$UploadBaseUrl = $(if ($env:REGPROBE_VM_BRIDGE_BASE_URL) { $env:REGPROBE_VM_BRIDGE_BASE_URL } else { 'http://10.0.2.2:8766' })
)

$ErrorActionPreference = 'Stop'

$patterns = @(
    'DpcWatchdogProfileBufferSizeBytes',
    'DpcWatchdogProfileCumulativeDpcThreshold',
    'DpcWatchdogProfileOffset',
    'DpcWatchdogProfileSingleDpcThreshold'
)

$outputRoot = Join-Path 'C:\RegProbe-Diag\wpr-filter' $OutputName
$summaryPath = Join-Path $outputRoot ($OutputName + '-summary.json')
$hitsPath = Join-Path $outputRoot ($OutputName + '.hits.txt')

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )

    $Payload | ConvertTo-Json -Depth 12 | Set-Content -Path $Path -Encoding UTF8
}

function Invoke-ArtifactUpload {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path $Path -PathType Leaf)) {
        return $null
    }

    $targetUri = '{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName
    Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
    return [ordered]@{
        path = $Path
        uri = $targetUri
    }
}

$summary = [ordered]@{
    output_name = $OutputName
    status = 'started'
    source_csv_path = $CsvPath
    source_csv_exists = $false
    source_csv_size_bytes = 0
    patterns = $patterns
    hits_path = $hitsPath
    hits_exists = $false
    hit_line_count = 0
    pattern_hit_counts = [ordered]@{}
    any_target_hits = $false
    findstr_exit_code = $null
    uploads = [ordered]@{}
    error = $null
    collected_utc = $null
}

foreach ($pattern in $patterns) {
    $summary.pattern_hit_counts[$pattern] = 0
}

try {
    if (-not (Test-Path $CsvPath -PathType Leaf)) {
        $summary.status = 'source-missing'
    }
    else {
        $csvItem = Get-Item $CsvPath
        $summary.source_csv_exists = $true
        $summary.source_csv_size_bytes = $csvItem.Length

        $findArgs = @('/I')
        foreach ($pattern in $patterns) {
            $findArgs += "/C:$pattern"
        }
        $findArgs += $CsvPath

        $hits = & findstr.exe @findArgs 2>$null
        $summary.findstr_exit_code = $LASTEXITCODE

        if ($null -eq $hits) {
            New-Item -ItemType File -Path $hitsPath -Force | Out-Null
        }
        else {
            @($hits) | Set-Content -Path $hitsPath -Encoding UTF8
        }

        $hitLines = if (Test-Path $hitsPath) { @(Get-Content -Path $hitsPath -ErrorAction SilentlyContinue) } else { @() }
        $summary.hits_exists = [bool](Test-Path $hitsPath)
        $summary.hit_line_count = @($hitLines).Count
        foreach ($line in $hitLines) {
            foreach ($pattern in $patterns) {
                if ($line -like "*$pattern*") {
                    $summary.pattern_hit_counts[$pattern]++
                }
            }
        }
        $summary.any_target_hits = ($summary.hit_line_count -gt 0)
        $summary.status = 'completed'
    }
}
catch {
    $summary.status = 'error'
    $summary.error = $_.Exception.Message
}
finally {
    $summary.collected_utc = (Get-Date).ToUniversalTime().ToString('o')
    Write-JsonFile -Path $summaryPath -Payload $summary
    foreach ($artifact in @(
        @{ key = 'hits'; path = $hitsPath; name = ($OutputName + '.hits.txt') }
    )) {
        try {
            $upload = Invoke-ArtifactUpload -Path $artifact.path -RemoteName $artifact.name
            if ($upload) {
                $summary.uploads[$artifact.key] = $upload
            }
        }
        catch {
            $summary.error = $_.Exception.Message
        }
    }
    Write-JsonFile -Path $summaryPath -Payload $summary
    Invoke-ArtifactUpload -Path $summaryPath -RemoteName ($OutputName + '-summary.json') | Out-Null
    Write-Output $summaryPath
}
