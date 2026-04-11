[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = 'C:\Users\rai\RegProbe-codex-legacy-dirty-main-20260407'
$outputPath = 'registry-research-framework\bench-results\power.control.hibernate-enabled-vm-functional.json'
$resolvedOutputPath = Join-Path $repoRoot $outputPath

function Get-HiberfileState {
    $path = 'C:\hiberfil.sys'
    $item = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    if (-not $item) {
        return [ordered]@{
            exists = $false
            length = $null
            last_write_time_utc = $null
        }
    }

    return [ordered]@{
        exists = $true
        length = $item.Length
        last_write_time_utc = $item.LastWriteTimeUtc.ToString('o')
    }
}

function Test-HiberfileStateEquals {
    param(
        [object]$Left,
        [object]$Right
    )

    if ([bool]$Left.exists -ne [bool]$Right.exists) {
        return $false
    }

    if (-not [bool]$Left.exists) {
        return $true
    }

    return (
        ([string]$Left.length -eq [string]$Right.length) -and
        ([string]$Left.last_write_time_utc -eq [string]$Right.last_write_time_utc)
    )
}

$runner = @(
    Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*SAFETY*BENCH*GUEST*.PS1' |
        Where-Object { $_.Name -notmatch 'HIBER|HIBEN|BATCH|CLASS1|EXEC' } |
        Select-Object -First 1
)[0]
if (-not $runner) {
    throw "Safety bench guest runner not found under $PSScriptRoot"
}

Set-Location $repoRoot

$hiberfileBefore = Get-HiberfileState

& $runner.FullName `
    -CandidateId 'power.control.hibernate-enabled' `
    -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' `
    -RegistryPathNative 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
    -RegistrySubKey 'SYSTEM\CurrentControlSet\Control\Power' `
    -ValueName 'HibernateEnabled' `
    -ApplyValue 1 `
    -ApplyType 'REG_DWORD' `
    -RollbackMethod 'restore-baseline' `
    -BenchTier 'vm' `
    -BenchProfile 'functional' `
    -BenchEnvironment 'windows-11-25h2-vm' `
    -BenchMeasurementReliability 'functional' `
    -OutputPath $outputPath | Out-Null

$result = Get-Content -Raw -LiteralPath $resolvedOutputPath | ConvertFrom-Json
$hiberfileAfter = Get-HiberfileState
$sideeffectsClean = Test-HiberfileStateEquals -Left $hiberfileBefore -Right $hiberfileAfter

$result | Add-Member -NotePropertyName 'hiberfile_state_before' -NotePropertyValue $hiberfileBefore -Force
$result | Add-Member -NotePropertyName 'hiberfile_state_after' -NotePropertyValue $hiberfileAfter -Force
$result | Add-Member -NotePropertyName 'hiberfile_sideeffects_clean' -NotePropertyValue ([bool]$sideeffectsClean) -Force

if (-not $sideeffectsClean) {
    $result.safety_passed = $false
    if ([string]::IsNullOrWhiteSpace([string]$result.rollback_failure_reason)) {
        $result.rollback_failure_reason = 'hiberfile-sideeffect-detected'
    }
}

$result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8
$result | ConvertTo-Json -Depth 10
