[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$GuestInstallRoot = 'C:\Tools\IDA',
    [string]$HostInstallerRoot = '',
    [string]$OutputFile = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputFile = "H:\D\Dev\RegProbe\registry-research-framework\audit\ida-provisioning-$stamp.json"
}

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)
    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }
    return $output.Trim()
}

function Invoke-GuestPowerShell {
    param([string]$InlineCommand, [switch]$IgnoreExitCode)
    $args = @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command', $InlineCommand
    )
    return Invoke-Vmrun -Arguments $args -IgnoreExitCode:$IgnoreExitCode
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null

$guestKnownPaths = @(
    (Join-Path $GuestInstallRoot 'idat64.exe'),
    'C:\Program Files\IDA Pro\idat64.exe',
    'C:\Program Files\IDA Professional\idat64.exe'
)

$existingGuestPath = $null
foreach ($candidate in $guestKnownPaths) {
    $result = Invoke-GuestPowerShell -InlineCommand "[Console]::Out.Write((Test-Path '$candidate').ToString().ToLowerInvariant())" -IgnoreExitCode
    if ($result -match 'true') {
        $existingGuestPath = $candidate
        break
    }
}

$status = 'ready'
$notes = @()

if (-not $existingGuestPath) {
    if (-not [string]::IsNullOrWhiteSpace($HostInstallerRoot) -and (Test-Path (Join-Path $HostInstallerRoot 'idat64.exe'))) {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $GuestInstallRoot) -IgnoreExitCode | Out-Null
        Get-ChildItem -Path $HostInstallerRoot -File | ForEach-Object {
            Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $_.FullName, (Join-Path $GuestInstallRoot $_.Name)) | Out-Null
        }
        $existingGuestPath = Join-Path $GuestInstallRoot 'idat64.exe'
        $status = 'provisioned'
        $notes += 'Portable IDA root copied from host installer root.'
    }
    else {
        $status = 'blocked-installer-missing'
        $notes += 'No guest IDA installation was found and no host portable installer root was provided.'
    }
}

$licensePresent = $false
if ($existingGuestPath) {
    $licenseCheck = Invoke-GuestPowerShell -InlineCommand @"
$paths = @(
  '$GuestInstallRoot\ida.key',
  '$GuestInstallRoot\license\ida.key',
  '$GuestInstallRoot\cfg\ida.reg'
)
[Console]::Out.Write((@($paths | Where-Object { Test-Path $_ }).Count -gt 0).ToString().ToLowerInvariant())
"@ -IgnoreExitCode
    $licensePresent = $licenseCheck -match 'true'
    if (-not $licensePresent) {
        $status = 'blocked-license-missing'
        $notes += 'IDA binary path exists, but no local license artifact was detected under the canonical guest root.'
    }
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    guest_install_root = $GuestInstallRoot
    guest_idat64_path = $existingGuestPath
    status = $status
    license_present = $licensePresent
    notes = @($notes)
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFile) | Out-Null
$payload | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputFile -Encoding UTF8
$payload | ConvertTo-Json -Depth 5
