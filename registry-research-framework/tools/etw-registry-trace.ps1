[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputFile,
    [string]$TweakId,
    [string]$SessionName = "OpenTraceRegistry",
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence'
)

$resolver = Join-Path $PSScriptRoot '_resolve-tweak-runner.ps1'
$runner = $null
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot '_lane-manifest-lib.ps1')
$coverage = Get-RunnerCoverageRequirement -TweakId $TweakId

if ($TweakId) {
    $resolved = & $resolver -Lane runtime -TweakId $TweakId
    if ($resolved) {
        $runner = $resolved | ConvertFrom-Json
    }
}

$payload = [pscustomobject]@{
    tool = "ETW"
    session_name = $SessionName
    tweak_id = $TweakId
    output_file = Get-RepoDisplayPath -Path $OutputFile
    status = if ($runner) { "invoked-runner" } else { "staged" }
    collection_mode = $CollectionMode
    rollback_pending = ($CollectionMode -eq 'evidence')
    runner_required = [bool]$coverage.runner_required
    capture_status = if ($runner) { 'missing-capture' } else { 'staged' }
    capture_artifacts = @()
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
        "Invoked the mapped VM runtime runner for this tweak. Check the runner output root for concrete capture files."
    }
    else {
        "No tweak-specific ETW runner is mapped yet. Wire this wrapper to an existing VM capture script before using it for live validation."
    }
    suspected_layer = $coverage.suspected_layer
    boot_phase_relevant = [bool]$coverage.boot_phase_relevant
}

if ($runner) {
    if (-not (Test-Path $runner.script_path)) {
        throw "Mapped runtime runner is missing: $($runner.script_path)"
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
    $payload.capture_artifacts = @(Get-CaptureArtifactsFromPayload -ResultRef $resultRef -LogRef $payload.log_file)
    $payload.capture_status = Get-CaptureStatus -Status $payload.status -CaptureArtifacts $payload.capture_artifacts
}

$payload | ConvertTo-Json -Depth 4 | Set-Content -Path $OutputFile -Encoding utf8
Write-Host "Wrote ETW wrapper manifest to $OutputFile"
