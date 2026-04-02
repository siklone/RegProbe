param(
    [switch]$Apply,
    [string]$RulesConfig = ''
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

if ([string]::IsNullOrWhiteSpace($RulesConfig)) {
    $RulesConfig = Join-Path $repoRoot 'registry-research-framework\config\artifact-sanitization-rules.json'
}

if (-not (Test-Path -LiteralPath $RulesConfig)) {
    throw "Missing sanitization rules config: $RulesConfig"
}

$config = Get-Content -LiteralPath $RulesConfig -Raw | ConvertFrom-Json
$targetSpecs = @($config.targets)
$rules = @($config.rules | Where-Object { $_.enabled -ne $false })
$rulesConfigDirectory = Split-Path -Parent (Resolve-Path -LiteralPath $RulesConfig).Path

function Get-NormalizedRelativePath {
    param([string]$Path)

    return ($Path.Substring($repoRoot.Length).TrimStart('\')).Replace('\', '/')
}

function Test-PathTargetMatch {
    param(
        [string]$RelativePath,
        [object[]]$Targets
    )

    if (-not $Targets -or $Targets.Count -eq 0) {
        return $true
    }

    foreach ($target in $Targets) {
        $normalizedTarget = ([string]$target).Trim().Replace('\', '/').Trim('/')
        if ([string]::IsNullOrWhiteSpace($normalizedTarget)) {
            continue
        }

        if ($normalizedTarget -eq '*') {
            return $true
        }

        if ($RelativePath.Equals($normalizedTarget, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }

        if ($RelativePath.StartsWith("$normalizedTarget/", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-EligibleFiles {
    param([string]$TargetSpec)

    if ([System.IO.Path]::IsPathRooted($TargetSpec)) {
        $fullPath = $TargetSpec
    }
    else {
        $fullPath = Join-Path $repoRoot $TargetSpec
        if (-not (Test-Path -LiteralPath $fullPath)) {
            $configRelativePath = Join-Path $rulesConfigDirectory $TargetSpec
            if (Test-Path -LiteralPath $configRelativePath) {
                $fullPath = $configRelativePath
            }
        }
    }

    if (-not (Test-Path -LiteralPath $fullPath)) {
        return @()
    }

    $item = Get-Item -LiteralPath $fullPath
    if ($item.PSIsContainer) {
        return @(Get-ChildItem -Path $fullPath -Recurse -File |
            Where-Object { $_.Extension -in ".json", ".md", ".txt" })
    }

    return @($item)
}

function Invoke-SanitizationRule {
    param(
        [string]$Content,
        [object]$Rule
    )

    $updated = $Content
    switch ([string]$Rule.type) {
        'literal' {
            $updated = $updated.Replace([string]$Rule.pattern, [string]$Rule.replacement)
        }
        'regex' {
            $updated = [regex]::Replace($updated, [string]$Rule.pattern, [string]$Rule.replacement)
        }
        default {
            throw "Unsupported sanitization rule type: $($Rule.type)"
        }
    }

    return $updated
}

$files = New-Object System.Collections.Generic.List[string]
foreach ($spec in $targetSpecs) {
    foreach ($file in @(Get-EligibleFiles -TargetSpec $spec)) {
        [void]$files.Add($file.FullName)
    }
}

$changes = New-Object System.Collections.Generic.List[object]
foreach ($file in $files | Sort-Object -Unique) {
    $content = Get-Content -LiteralPath $file -Raw
    if ($null -eq $content) {
        $content = ""
    }

    $updated = $content
    $relativeFile = Get-NormalizedRelativePath -Path $file
    $matchedRuleIds = New-Object System.Collections.Generic.List[string]
    foreach ($rule in $rules) {
        if (-not (Test-PathTargetMatch -RelativePath $relativeFile -Targets @($rule.targets))) {
            continue
        }

        $candidate = Invoke-SanitizationRule -Content $updated -Rule $rule
        if ($candidate -ne $updated) {
            $updated = $candidate
            [void]$matchedRuleIds.Add([string]$rule.id)
        }
    }

    if ($updated -ne $content) {
        $backupPath = "$file.bak"
        $changes.Add([ordered]@{
            file = $relativeFile
            backup_path = (($backupPath.Substring($repoRoot.Length).TrimStart('\')).Replace('\', '/'))
            rules = @($matchedRuleIds | Select-Object -Unique)
        }) | Out-Null

        if ($Apply) {
            Copy-Item -LiteralPath $file -Destination $backupPath -Force
            Set-Content -LiteralPath $file -Value $updated -Encoding UTF8
        }
    }
}

$manifestPath = Join-Path $repoRoot ("registry-research-framework\audit\sanitize-public-artifacts-{0}.json" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
$rulesConfigDisplay = if ($RulesConfig.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    ($RulesConfig.Substring($repoRoot.Length).TrimStart('\')).Replace('\', '/')
}
else {
    $RulesConfig
}
$changeArray = @($changes.ToArray())
$manifest = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    apply = [bool]$Apply
    rules_config = $rulesConfigDisplay
    changed_file_count = $changeArray.Count
    changes = $changeArray
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

if ($Apply) {
    Write-Host "Sanitized $($changes.Count) file(s). Manifest: $manifestPath"
}
else {
    Write-Host "Would sanitize $($changes.Count) file(s). Manifest: $manifestPath"
    $changes | ForEach-Object { Write-Host $_.file }
}
