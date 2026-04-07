[CmdletBinding()]
param()

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$coveragePolicyPath = Join-Path $repoRoot 'registry-research-framework\config\runner-coverage-policy.json'
$auditPath = Join-Path $repoRoot 'research\evidence-audit.json'

function Get-RepoDisplayPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $full = [System.IO.Path]::GetFullPath($Path)
    $repo = [System.IO.Path]::GetFullPath($repoRoot)
    if ($full.StartsWith($repo, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($repo.Length).TrimStart('\').Replace('\', '/')
    }

    return $Path
}

function Get-RepoRefFromValue {
    param($Value)

    if ($null -eq $Value) {
        return $null
    }

    $text = [string]$Value
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    $trimmed = $text.Trim()
    $normalized = $trimmed.TrimStart('\', '/') -replace '\\', '/'
    if ($normalized -match '^(evidence|research|registry-research-framework)/') {
        return $normalized
    }

    try {
        $display = Get-RepoDisplayPath -Path $trimmed
        if (-not [string]::IsNullOrWhiteSpace($display)) {
            $normalizedDisplay = $display.TrimStart('\', '/') -replace '\\', '/'
            if ($normalizedDisplay -match '^(evidence|research|registry-research-framework)/') {
                return $normalizedDisplay
            }
        }
    }
    catch {
    }

    return $null
}

function Sanitize-RunnerOutput {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ""
    }

    $sanitized = $Text
    $repo = [System.IO.Path]::GetFullPath($repoRoot)
    $repoPattern = [regex]::Escape($repo)
    $sanitized = [regex]::Replace($sanitized, $repoPattern, "")
    $sanitized = [regex]::Replace($sanitized, '(?im)(-gp\s+)(\S+)', '$1<redacted>')
    $sanitized = [regex]::Replace($sanitized, '(?im)(?<![A-Za-z0-9])[A-Z]:\\[^\r\n]+', '<local-path>')
    return $sanitized.Trim()
}

function New-LaneArtifactRef {
    param(
        [string]$Path,
        [hashtable]$Extra = @{}
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $full = [System.IO.Path]::GetFullPath($Path)
    $item = if (Test-Path -LiteralPath $full) { Get-Item -LiteralPath $full } else { $null }
    $display = Get-RepoDisplayPath -Path $full

    $payload = [ordered]@{
        path = $display
        sha256 = if ($item) { (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
        size = if ($item) { [int64]$item.Length } else { $null }
        collected_utc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        exists = [bool]$item
    }

    foreach ($key in $Extra.Keys) {
        $payload[$key] = $Extra[$key]
    }

    return $payload
}

function Publish-RunnerOutputArtifacts {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,
        [string]$RawPath,
        [string]$SanitizedOutputPath,
        [string]$SanitizedText = '',
        [string]$PrivateRoot = $env:REGPROBE_PRIVATE_RUNNER_OUTPUT_ROOT
    )

    $rawFullPath = if ([string]::IsNullOrWhiteSpace($RawPath)) { $null } else { [System.IO.Path]::GetFullPath($RawPath) }
    $rawExists = -not [string]::IsNullOrWhiteSpace($rawFullPath) -and (Test-Path -LiteralPath $rawFullPath)
    $resolvedSanitizedText = $SanitizedText
    if ([string]::IsNullOrWhiteSpace($resolvedSanitizedText) -and $rawExists) {
        $resolvedSanitizedText = Sanitize-RunnerOutput -Text (Get-Content -LiteralPath $rawFullPath -Raw -ErrorAction SilentlyContinue)
    }

    $sanitizedRef = $null
    if (-not [string]::IsNullOrWhiteSpace($SanitizedOutputPath) -and -not [string]::IsNullOrWhiteSpace($resolvedSanitizedText)) {
        $sanitizedParent = Split-Path -Parent $SanitizedOutputPath
        if (-not [string]::IsNullOrWhiteSpace($sanitizedParent)) {
            New-Item -ItemType Directory -Path $sanitizedParent -Force | Out-Null
        }
        Set-Content -LiteralPath $SanitizedOutputPath -Value $resolvedSanitizedText -Encoding UTF8
        $sanitizedRef = New-LaneArtifactRef -Path $SanitizedOutputPath -Extra @{ visibility = 'public-sanitized' }
    }

    $privateStorageStatus = if ($rawExists) { 'not-configured' } else { 'source-missing' }
    $privateRawRef = $null
    if ($rawExists -and -not [string]::IsNullOrWhiteSpace($PrivateRoot)) {
        try {
            $privateRootFull = [System.IO.Path]::GetFullPath($PrivateRoot)
            $privateLabel = ($Label -replace '[^A-Za-z0-9._-]+', '-').Trim('-')
            if ([string]::IsNullOrWhiteSpace($privateLabel)) {
                $privateLabel = 'runner-output'
            }
            $privateDir = Join-Path $privateRootFull (Get-Date -Format 'yyyyMMdd')
            New-Item -ItemType Directory -Path $privateDir -Force | Out-Null
            $privateLeaf = '{0}-{1}{2}' -f $privateLabel, ([guid]::NewGuid().ToString('N')), [System.IO.Path]::GetExtension($rawFullPath)
            $privatePath = Join-Path $privateDir $privateLeaf
            Copy-Item -LiteralPath $rawFullPath -Destination $privatePath -Force
            $privateRawRef = New-LaneArtifactRef -Path $privatePath -Extra @{ visibility = 'private-raw' }
            $privateStorageStatus = 'copied'
        }
        catch {
            $privateStorageStatus = 'copy-failed'
        }
    }

    return [ordered]@{
        label = $Label
        raw_source_path = $rawFullPath
        private_storage_status = $privateStorageStatus
        raw_private_ref = $privateRawRef
        public_sanitized_ref = $sanitizedRef
    }
}

function Get-RunnerResultRef {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    $repo = [System.IO.Path]::GetFullPath($repoRoot)
    $repoPattern = [regex]::Escape($repo)
    $normalizedText = [regex]::Replace($Text, $repoPattern, "")
    $lines = $normalizedText -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    [array]::Reverse($lines)
    foreach ($line in $lines) {
        $normalized = $line.TrimStart('\', '/') -replace '\\', '/'
        if ($normalized -match '^(evidence|research|registry-research-framework)/') {
            return $normalized
        }
    }

    foreach ($propertyName in @('normalized_result_ref', 'result_ref', 'summary_path')) {
        $match = [regex]::Match($normalizedText, ('"{0}"\s*:\s*"([^"]+)"' -f [regex]::Escape($propertyName)))
        if ($match.Success) {
            $candidate = Get-RepoRefFromValue -Value $match.Groups[1].Value
            if ($candidate) {
                return $candidate
            }
        }
    }

    return $null
}

function Get-RunnerSummaryPayload {
    param([string]$ResultRef)

    $repoRef = Get-RepoRefFromValue -Value $ResultRef
    if ([string]::IsNullOrWhiteSpace($repoRef)) {
        return $null
    }

    $resultPath = Join-Path $repoRoot ($repoRef -replace '/', '\')
    if (-not (Test-Path -LiteralPath $resultPath)) {
        return $null
    }

    try {
        return Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        return $null
    }
}

function Get-RunnerNormalizationMetadata {
    param([string]$ResultRef)

    $metadata = [ordered]@{
        normalized_result_ref = $null
        normalization_status = $null
        normalizer_name = $null
        normalization_errors = @()
        error_kind = $null
        recovery_action = $null
        transport_blocker = $null
        guest_health = $null
    }

    $summary = Get-RunnerSummaryPayload -ResultRef $ResultRef
    if (-not $summary) {
        return $metadata
    }

    $metadata.normalized_result_ref = Get-RepoRefFromValue -Value $summary.normalized_result_ref
    $metadata.normalizer_name = if ($summary.PSObject.Properties.Name -contains 'normalizer_name') { [string]$summary.normalizer_name } else { $null }
    $metadata.normalization_status = if ($summary.PSObject.Properties.Name -contains 'normalization_status') { [string]$summary.normalization_status } else { $null }
    $metadata.error_kind = if ($summary.PSObject.Properties.Name -contains 'error_kind') { [string]$summary.error_kind } else { $null }
    $metadata.recovery_action = if ($summary.PSObject.Properties.Name -contains 'recovery_action') { [string]$summary.recovery_action } else { $null }
    $metadata.transport_blocker = if ($summary.PSObject.Properties.Name -contains 'transport_blocker') { [string]$summary.transport_blocker } else { $null }
    $metadata.guest_health = if ($summary.PSObject.Properties.Name -contains 'guest_health') { [string]$summary.guest_health } else { $null }

    if ($summary.PSObject.Properties.Name -contains 'normalization_errors' -and $summary.normalization_errors) {
        $metadata.normalization_errors = @($summary.normalization_errors | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
    elseif ($summary.PSObject.Properties.Name -contains 'errors' -and $summary.errors) {
        $metadata.normalization_errors = @($summary.errors | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
    elseif ($summary.PSObject.Properties.Name -contains 'error' -and -not [string]::IsNullOrWhiteSpace([string]$summary.error)) {
        $metadata.normalization_errors = @([string]$summary.error)
    }

    return $metadata
}

function Get-RunnerCoverageRequirement {
    param([string]$TweakId)

    $default = [ordered]@{
        runner_required = $false
        suspected_layer = $null
        boot_phase_relevant = $false
    }

    if ([string]::IsNullOrWhiteSpace($TweakId) -or -not (Test-Path -LiteralPath $auditPath) -or -not (Test-Path -LiteralPath $coveragePolicyPath)) {
        return $default
    }

    $audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
    $entry = @($audit.entries | Where-Object { $_.tweak_id -eq $TweakId } | Select-Object -First 1)
    if (-not $entry) {
        return $default
    }

    $policy = Get-Content -LiteralPath $coveragePolicyPath -Raw | ConvertFrom-Json
    $suspectedLayer = [string]$entry[0].suspected_layer
    $bootRelevant = [bool]$entry[0].boot_phase_relevant
    $runnerRequired = $policy.required_layers -contains $suspectedLayer
    if (-not $runnerRequired -and $policy.required_when_boot_phase_relevant) {
        $runnerRequired = $bootRelevant
    }

    return [ordered]@{
        runner_required = [bool]$runnerRequired
        suspected_layer = $suspectedLayer
        boot_phase_relevant = $bootRelevant
    }
}

function Get-CaptureArtifactsFromPayload {
    param(
        [string]$ResultRef,
        [string]$LogRef
    )

    $artifactMap = [ordered]@{}

    foreach ($candidate in @($ResultRef, $LogRef)) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        $normalized = $candidate.TrimStart('\', '/') -replace '\\', '/'
        $full = Join-Path $repoRoot $normalized
        $artifactMap[$normalized] = [ordered]@{
            path = $normalized
            exists = (Test-Path -LiteralPath $full)
            placeholder = ($normalized -like '*.md')
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ResultRef)) {
        $normalizedResult = $ResultRef.TrimStart('\', '/') -replace '\\', '/'
        $resultPath = Join-Path $repoRoot $normalizedResult
        if (Test-Path -LiteralPath $resultPath) {
            try {
                $payload = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
                $json = $payload | ConvertTo-Json -Depth 16
                foreach ($match in ([regex]::Matches($json, '(evidence|research|registry-research-framework)[\\/][^"''\s]+'))) {
                    $path = $match.Value -replace '\\', '/'
                    if ($artifactMap.Contains($path)) {
                        continue
                    }
                    $full = Join-Path $repoRoot $path
                    $artifactMap[$path] = [ordered]@{
                        path = $path
                        exists = (Test-Path -LiteralPath $full)
                        placeholder = ($path -like '*.md')
                    }
                }
            }
            catch {
            }
        }
    }

    return @($artifactMap.Values)
}

function Get-CaptureStatus {
    param(
        [string]$Status,
        [object[]]$CaptureArtifacts,
        [string]$NormalizationStatus,
        [string]$NormalizedResultRef
    )

    $normalizedStatus = [string]$Status
    if ($normalizedStatus -eq 'staged') {
        return 'staged'
    }

    $normalizedContractStatus = [string]$NormalizationStatus
    if ($normalizedContractStatus -in @('error', 'failed', 'parse-error')) {
        return 'normalization-error'
    }

    $normalizedResultPath = $null
    $normalizedRepoRef = Get-RepoRefFromValue -Value $NormalizedResultRef
    if (-not [string]::IsNullOrWhiteSpace($normalizedRepoRef)) {
        $normalizedResultPath = Join-Path $repoRoot ($normalizedRepoRef -replace '/', '\')
    }

    if ($normalizedResultPath -and (Test-Path -LiteralPath $normalizedResultPath)) {
        return 'captured'
    }

    $physicalArtifacts = @(
        $CaptureArtifacts |
            Where-Object { $_.exists -and -not $_.placeholder }
    )

    if ($physicalArtifacts.Count -gt 0) {
        if (-not [string]::IsNullOrWhiteSpace($normalizedContractStatus)) {
            return 'captured-without-normalization'
        }

        return 'captured'
    }

    if ($normalizedStatus -eq 'runner-ok') {
        return 'missing-capture'
    }

    if ([string]::IsNullOrWhiteSpace($normalizedStatus)) {
        return 'missing-capture'
    }

    return $normalizedStatus
}
