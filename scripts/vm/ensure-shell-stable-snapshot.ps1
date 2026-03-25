[CmdletBinding()]
param(
    [string]$SnapshotName = ("baseline-{0}-shell-stable" -f (Get-Date -Format 'yyyyMMdd')),
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

$shellScript = Join-Path $PSScriptRoot 'get-vm-shell-health.ps1'
$shellHealth = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json

if (-not $shellHealth.vm_running) {
    throw 'The VM is not running. Start the guest before taking a shell-stable snapshot.'
}

if (-not $shellHealth.shell_healthy) {
    throw 'The guest shell is not healthy enough for a shell-stable snapshot.'
}

$existingSnapshots = & $VmrunPath -T ws listSnapshots $VmPath 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    throw "listSnapshots failed: $($existingSnapshots.Trim())"
}

$created = $false
if ($existingSnapshots -notmatch [regex]::Escape($SnapshotName)) {
    $snapshotOutput = & $VmrunPath -T ws snapshot $VmPath $SnapshotName 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "snapshot failed: $($snapshotOutput.Trim())"
    }
    $created = $true
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    snapshot_name = $SnapshotName
    created = $created
    shell_healthy = $shellHealth.shell_healthy
    vm_path = $VmPath
}

if ($OutputPath) {
    $result | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputPath -Encoding UTF8
}

$result | ConvertTo-Json -Depth 5
