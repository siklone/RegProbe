[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$parserTargets = @(
    'scripts\vm\_vmrun-common.ps1',
    'scripts\sanitize_public_artifacts.ps1',
    'registry-research-framework\tools\_lane-manifest-lib.ps1',
    'registry-research-framework\tools\etw-registry-trace.ps1',
    'registry-research-framework\tools\procmon-registry-trace.ps1',
    'registry-research-framework\tools\wpr-boot-trace.ps1',
    'registry-research-framework\tools\run-power-control-batch-mega-trigger-runtime.ps1',
    'registry-research-framework\tools\run-power-control-batch-mega-trigger-runtime-safe.ps1',
    'registry-research-framework\tools\run-path-aware-runtime-probe.ps1',
    'scripts\vm\get-vm-shell-health.ps1',
    'scripts\vm\configure-kernel-debug-baseline.ps1',
    'scripts\vm\new-windbg-registry-watch-script.ps1',
    'registry-research-framework\tools\run-windbg-boot-registry-trace.ps1',
    'registry-research-framework\tools\run-windbg-transport-matrix.ps1',
    'registry-research-framework\tools\run-windbg-serial-config-matrix.ps1',
    'registry-research-framework\tools\run-windbg-start-order-matrix.ps1',
    'registry-research-framework\tools\run-windbg-reconnect-command-matrix.ps1',
    'registry-research-framework\tools\run-windbg-pipe-launch-matrix.ps1',
    'registry-research-framework\tools\execute-windbg-boot-registry-trace.ps1',
    'scripts\vm-hyperv\test-hyperv-debug-feasibility.ps1',
    'scripts\vm-hyperv\new-hyperv-debug-baseline-plan.ps1',
    'registry-research-framework\tools\windbg-hyperv\run-debug-environment-selection.ps1',
    'tests\Invoke-StagingInventorySmoke.ps1'
)

foreach ($relative in $parserTargets) {
    $full = Join-Path $repoRoot $relative
    [scriptblock]::Create((Get-Content -LiteralPath $full -Raw)) | Out-Null
}

. (Join-Path $repoRoot 'scripts\vm\_vmrun-common.ps1')

$oldGuestUser = $env:REGPROBE_VM_GUEST_USER
$oldGuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD
$oldCredentialFile = $env:REGPROBE_VM_CREDENTIAL_FILE

try {
    $env:REGPROBE_VM_GUEST_USER = 'env-user'
    $env:REGPROBE_VM_GUEST_PASSWORD = 'env-pass'
    $env:REGPROBE_VM_CREDENTIAL_FILE = $null

    $envCredential = Resolve-RegProbeVmCredential -GuestUser 'fallback-user' -GuestPassword ''
    if ($envCredential.UserName -ne 'env-user' -or $envCredential.GetNetworkCredential().Password -ne 'env-pass') {
        throw 'Environment credential precedence failed.'
    }

    $credFile = Join-Path $env:TEMP ("regprobe-vm-cred-{0}.clixml" -f ([guid]::NewGuid().ToString('N')))
    New-RegProbePlaintextCredential -UserName 'file-user' -Password 'file-pass' | Export-Clixml -LiteralPath $credFile
    $env:REGPROBE_VM_GUEST_PASSWORD = $null
    $env:REGPROBE_VM_CREDENTIAL_FILE = $credFile
    $fileCredential = Resolve-RegProbeVmCredential -GuestUser 'fallback-user' -GuestPassword ''
    if ($fileCredential.UserName -ne 'file-user' -or $fileCredential.GetNetworkCredential().Password -ne 'file-pass') {
        throw 'Credential file fallback failed.'
    }

    $masked = Format-RegProbeVmrunArgumentsForLog -Arguments @('-T', 'ws', '-gu', 'Administrator', '-gp', 'secret-value', 'list')
    if ($masked -match 'secret-value') {
        throw 'Secret masking failed for vmrun argv.'
    }

    $tempRepoRoot = Join-Path $repoRoot ("tests\tmp-quality-smoke-{0}" -f ([guid]::NewGuid().ToString('N')))
    New-Item -ItemType Directory -Path $tempRepoRoot -Force | Out-Null
    try {
        $manifestPath = Join-Path $tempRepoRoot 'runtime-lane.json'
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'registry-research-framework\tools\etw-registry-trace.ps1') -OutputFile $manifestPath -TweakId 'nonexistent.tweak' -CollectionMode evidence | Out-Null
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if ($manifest.collection_mode -ne 'evidence' -or -not $manifest.rollback_pending -or $manifest.capture_status -ne 'staged') {
            throw 'ETW wrapper manifest contract failed.'
        }

        $sampleDir = Join-Path $tempRepoRoot 'sanitize-sample'
        New-Item -ItemType Directory -Path $sampleDir -Force | Out-Null
        $sampleFile = Join-Path $sampleDir 'sample.json'
        Set-Content -LiteralPath $sampleFile -Value 'C:\Users\TestUser\AppData\Local\Temp\artifact.bin' -Encoding UTF8

        $configPath = Join-Path $tempRepoRoot 'sanitize-config.json'
        $tempRepoRelative = ("tests/{0}" -f (Split-Path -Leaf $tempRepoRoot))
        @"
{
  "schema_version": "1.0",
  "targets": [
    "$tempRepoRelative/sanitize-sample"
  ],
  "rules": [
    {
      "id": "temp",
      "type": "regex",
      "pattern": "C:\\\\Users\\\\[^\\\\]+\\\\AppData\\\\Local\\\\Temp\\\\",
      "replacement": "%LOCALAPPDATA%\\\\Temp\\\\",
      "targets": [
        "$tempRepoRelative/sanitize-sample"
      ],
      "enabled": true
    }
  ]
}
"@ | Set-Content -LiteralPath $configPath -Encoding UTF8

        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\sanitize_public_artifacts.ps1') -RulesConfig $configPath | Out-Null
        $manifests = Get-ChildItem -Path (Join-Path $repoRoot 'registry-research-framework\audit') -Filter 'sanitize-public-artifacts-*.json' | Sort-Object LastWriteTimeUtc -Descending
        if (-not $manifests) {
            throw 'Sanitization dry-run manifest was not produced.'
        }
        $latest = Get-Content -LiteralPath $manifests[0].FullName -Raw | ConvertFrom-Json
        if ([int]$latest.changed_file_count -lt 1) {
            throw 'Sanitization config-driven dry-run did not detect expected change.'
        }

        $privateRunnerRoot = Join-Path $tempRepoRoot 'private-runner-output'
        $oldPrivateRunnerRoot = $env:REGPROBE_PRIVATE_RUNNER_OUTPUT_ROOT
        try {
            $env:REGPROBE_PRIVATE_RUNNER_OUTPUT_ROOT = $privateRunnerRoot
            . (Join-Path $repoRoot 'registry-research-framework\tools\_lane-manifest-lib.ps1')
            $rawRunnerLog = Join-Path $tempRepoRoot 'runner.log'
            Set-Content -LiteralPath $rawRunnerLog -Value 'vmrun -gp secret-value C:\Users\TestUser\AppData\Local\Temp\trace.log' -Encoding UTF8
            $published = Publish-RunnerOutputArtifacts -Label 'smoke' -RawPath $rawRunnerLog -SanitizedOutputPath (Join-Path $tempRepoRoot 'runner.public.log')
            if ($published.private_storage_status -ne 'copied' -or -not $published.public_sanitized_ref.exists) {
                throw 'Runner output dual-publish failed.'
            }
            $sanitizedText = Get-Content -LiteralPath (Join-Path $tempRepoRoot 'runner.public.log') -Raw
            if ($sanitizedText -match 'secret-value' -or $sanitizedText -match 'TestUser') {
                throw 'Runner output sanitization failed.'
            }
        }
        finally {
            $env:REGPROBE_PRIVATE_RUNNER_OUTPUT_ROOT = $oldPrivateRunnerRoot
        }

        $baselineGuardPath = Join-Path $tempRepoRoot 'baseline-guard.json'
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\vm\configure-kernel-debug-baseline.ps1') -VmPath 'C:\Fake\Win25H2.vmx' -OutputPath $baselineGuardPath -GuestPassword 'unused' | Out-Null
        $baselineGuard = Get-Content -LiteralPath $baselineGuardPath -Raw | ConvertFrom-Json
        if ($baselineGuard.status -ne 'blocked-snapshot-gate') {
            throw 'Kernel debug baseline snapshot gate did not block unsafe mutation.'
        }

        $windbgScriptPath = Join-Path $tempRepoRoot 'windbg-singlekey.txt'
        $windbgGeneratorOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\vm\new-windbg-registry-watch-script.ps1') -TargetKey 'AllowSystemRequiredPowerRequests' -TraceProfile 'singlekey-rawbounded' -OutputPath $windbgScriptPath | ConvertFrom-Json
        if ($windbgGeneratorOutput.mode -ne 'singlekey-rawbounded' -or $windbgGeneratorOutput.target_key -ne 'AllowSystemRequiredPowerRequests') {
            throw 'Single-key WinDbg generator contract failed.'
        }
        $windbgScriptText = Get-Content -LiteralPath $windbgScriptPath -Raw
        if ($windbgScriptText -notmatch 'bu nt!CmQueryValueKey' -or $windbgScriptText -notmatch 'REGPROBE_TARGET\|AllowSystemRequiredPowerRequests') {
            throw 'Single-key WinDbg script contents are incomplete.'
        }

        $inventorySmokePath = Join-Path $repoRoot 'tests\Invoke-StagingInventorySmoke.ps1'
        $inventoryResult = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $inventorySmokePath
        if (-not $inventoryResult) {
            throw 'Staging inventory smoke did not return a result.'
        }
        $inventoryJson = $inventoryResult | ConvertFrom-Json
        if ([int]$inventoryJson.directory_count -le 0 -or [int]$inventoryJson.file_count -le 0) {
            throw 'Staging inventory smoke produced invalid counts.'
        }

        $hypervFeasibilityPath = Join-Path $tempRepoRoot 'hyperv-feasibility.json'
        $hypervFeasibility = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\vm-hyperv\test-hyperv-debug-feasibility.ps1') -OutputPath $hypervFeasibilityPath | ConvertFrom-Json
        if ($hypervFeasibility.selected -ne 'Hyper-V' -or @($hypervFeasibility.debug_environment_candidates).Count -lt 2) {
            throw 'Hyper-V feasibility contract failed.'
        }

        $hypervPlanPath = Join-Path $tempRepoRoot 'hyperv-plan.json'
        $hypervPlan = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\vm-hyperv\new-hyperv-debug-baseline-plan.ps1') -FeasibilityPath $hypervFeasibilityPath -OutputPath $hypervPlanPath | ConvertFrom-Json
        if ($hypervPlan.debug_environment -ne 'hyperv' -or $hypervPlan.vm_role -ne 'debug_arbiter_only') {
            throw 'Hyper-V baseline plan contract failed.'
        }
    }
    finally {
        Remove-Item -LiteralPath $tempRepoRoot -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $credFile -Force -ErrorAction SilentlyContinue
    }
}
finally {
    $env:REGPROBE_VM_GUEST_USER = $oldGuestUser
    $env:REGPROBE_VM_GUEST_PASSWORD = $oldGuestPassword
    $env:REGPROBE_VM_CREDENTIAL_FILE = $oldCredentialFile
}

Write-Host 'Pipeline quality smoke checks passed.'
