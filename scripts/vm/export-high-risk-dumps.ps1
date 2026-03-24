[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging\registry-dumps'
)

$ErrorActionPreference = 'Stop'

$targets = @(
    @{ Name = 'defender-policy-root'; RegistryPath = 'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender' },
    @{ Name = 'defender-policy-manager'; RegistryPath = 'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager' },
    @{ Name = 'power-control-root'; RegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Control\Power' },
    @{ Name = 'stornvme-parameters'; RegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters' },
    @{ Name = 'stornvme-device'; RegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters\Device' },
    @{ Name = 'usbhub3-service'; RegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Services\USBHUB3' },
    @{ Name = 'usbhub3-parameters'; RegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Services\USBHUB3\Parameters' },
    @{ Name = 'usbhub3-wdf'; RegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Services\USBHUB3\Parameters\Wdf' },
    @{ Name = 'windows-nt-currentversion-windows'; RegistryPath = 'HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows' }
)

$results = @()
foreach ($target in $targets) {
    Write-Host "Exporting $($target.RegistryPath)"
    $resultJson = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'export-registry-key.ps1') `
        -RegistryPath $target.RegistryPath `
        -OutputName $target.Name `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword `
        -HostOutputRoot $HostOutputRoot

    $results += ($resultJson | ConvertFrom-Json)
}

$summaryPath = Join-Path $HostOutputRoot 'high-risk-dumps-summary.json'
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
Write-Output $summaryPath
