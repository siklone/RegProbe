[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SnapshotName = '',
    [int]$PostBootSettleSeconds = 20
)

$ErrorActionPreference = 'Stop'

$stepwiseScript = Join-Path $PSScriptRoot 'run-power-control-docs-first-stepwise-runtime-capture.ps1'
if (-not (Test-Path $stepwiseScript)) {
    throw "Stepwise runtime capture script was not found: $stepwiseScript"
}

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $stepwiseScript `
    -VmPath $VmPath `
    -VmrunPath $VmrunPath `
    -GuestUser $GuestUser `
    -GuestPassword $GuestPassword `
    -SnapshotName $SnapshotName `
    -PostBootSettleSeconds $PostBootSettleSeconds

exit $LASTEXITCODE
