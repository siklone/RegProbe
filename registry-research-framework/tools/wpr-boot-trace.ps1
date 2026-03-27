[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputFile,
    [string]$TweakId
)

$resolver = Join-Path $PSScriptRoot '_resolve-tweak-runner.ps1'
$runner = $null
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

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

function Sanitize-RunnerOutput {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ""
    }

    $sanitized = $Text
    $repo = [System.IO.Path]::GetFullPath($repoRoot)
    $repoPattern = [regex]::Escape($repo)
    $sanitized = [regex]::Replace($sanitized, $repoPattern, "")
    $sanitized = [regex]::Replace($sanitized, '(?im)(?<![A-Za-z0-9])[A-Z]:\\[^\r\n]+', '<local-path>')
    return $sanitized.Trim()
}

function Get-RunnerResultRef {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    $lines = $Text -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    [array]::Reverse($lines)
    foreach ($line in $lines) {
        $normalized = $line.TrimStart('\', '/') -replace '\\', '/'
        if ($normalized -match '^(evidence|research|registry-research-framework)/') {
            return $normalized
        }
    }

    return $null
}

if ($TweakId) {
    $resolved = & $resolver -Lane behavior -TweakId $TweakId
    if ($resolved) {
        $runner = $resolved | ConvertFrom-Json
    }
}

$payload = [pscustomobject]@{
    tool = "WPR"
    trace_kind = "boot"
    tweak_id = $TweakId
    output_file = Get-RepoDisplayPath -Path $OutputFile
    status = if ($runner) { "invoked-runner" } else { "staged" }
    runner = if ($runner) {
        [pscustomobject]@{
            script = $runner.script
            args = @($runner.args)
        }
    }
    else {
        $null
    }
    note = if ($runner) {
        "Invoked the mapped VM behavior runner for this tweak. Check the runner output root for concrete boot trace files."
    }
    else {
        "No tweak-specific WPR runner is mapped yet. This wrapper currently reserves the output contract."
    }
}

if ($runner) {
    if (-not (Test-Path $runner.script_path)) {
        throw "Mapped behavior runner is missing: $($runner.script_path)"
    }

    $logPath = [System.IO.Path]::ChangeExtension($OutputFile, '.log')
    $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $runner.script_path @($runner.args) 2>&1 | Out-String
    $payload.status = if ($LASTEXITCODE -eq 0) { "runner-ok" } else { "runner-failed" }
    $payload | Add-Member -NotePropertyName exit_code -NotePropertyValue $LASTEXITCODE
    $payload | Add-Member -NotePropertyName log_file -NotePropertyValue (Get-RepoDisplayPath -Path $logPath)
    $resultRef = Get-RunnerResultRef -Text $output
    if ($resultRef) {
        $payload | Add-Member -NotePropertyName result_ref -NotePropertyValue $resultRef
    }
    $sanitizedOutput = Sanitize-RunnerOutput -Text $output
    $sanitizedOutput | Set-Content -Path $logPath -Encoding utf8
}

$payload | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputFile -Encoding utf8
