[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$runner = @(
    Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*SAFETY*BENCH*GUEST*.PS1' |
        Where-Object { $_.Name -notmatch 'HIBER' } |
        Select-Object -First 1
)[0]
if (-not $runner) {
    throw "Safety bench guest runner not found under $PSScriptRoot"
}
Set-Location 'C:\Users\rai\RegProbe-codex-legacy-dirty-main-20260407'

& $runner.FullName `
    -CandidateId 'power.control.hiber-file-size-percent' `
    -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' `
    -RegistryPathNative 'HKLM\SYSTEM\CurrentControlSet\Control\Power' `
    -RegistrySubKey 'SYSTEM\CurrentControlSet\Control\Power' `
    -ValueName 'HiberFileSizePercent' `
    -ApplyValue 1 `
    -ApplyType 'REG_DWORD' `
    -RollbackMethod 'restore-baseline' `
    -BenchTier 'vm' `
    -BenchProfile 'functional' `
    -BenchEnvironment 'windows-11-25h2-vm' `
    -BenchMeasurementReliability 'functional' `
    -OutputPath 'registry-research-framework\bench-results\power.control.hiber-file-size-percent-vm-functional.json'
