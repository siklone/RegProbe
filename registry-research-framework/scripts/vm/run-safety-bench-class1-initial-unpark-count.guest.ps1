[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = 'C:\Users\rai\RegProbe-codex-legacy-dirty-main-20260407'
$runner = @(
    Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*SAFETY*BENCH*GUEST*.PS1' |
        Where-Object { $_.Name -notmatch 'CLASS1|HIBER|BATCH' } |
        Select-Object -First 1
)[0]
if (-not $runner) {
    throw "Safety bench guest runner not found under $PSScriptRoot"
}

Set-Location $repoRoot

& $runner.FullName `
    -CandidateId 'power.control.class1-initial-unpark-count' `
    -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' `
    -RegistryPathNative 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
    -RegistrySubKey 'SYSTEM\CurrentControlSet\Control\Power' `
    -ValueName 'Class1InitialUnparkCount' `
    -ApplyValue 63 `
    -ApplyType 'REG_DWORD' `
    -RollbackMethod 'restore-baseline' `
    -BenchTier 'vm' `
    -BenchProfile 'functional' `
    -BenchEnvironment 'windows-11-25h2-vm' `
    -BenchMeasurementReliability 'functional' `
    -OutputPath 'registry-research-framework\bench-results\power.control.class1-initial-unpark-count-vm-functional.json'
