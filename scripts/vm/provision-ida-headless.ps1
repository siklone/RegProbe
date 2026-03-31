[CmdletBinding()]
param(
    [string]$VmProfile = 'primary',
    [string]$ConfigPath = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$GuestInstallRoot = 'C:\Tools\IDA',
    [string]$GuestWorkRoot = 'C:\RegProbe-Diag\IDA',
    [string]$HostInstallerRoot = '',
    [string]$HostInstallerPath = '',
    [string]$HostSharedFolderPath = 'H:\Temp\vm-tooling-staging',
    [string]$SharedFolderGuestName = 'vm-tooling-staging',
    [string]$OutputFile = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile -ConfigPath $ConfigPath
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'

if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputFile = Join-Path $repoRoot ("registry-research-framework\audit\ida-provisioning-$stamp.json")
}

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }
    return $output.Trim()
}

function Wait-HealthyShell {
    param([int]$TimeoutSeconds = 240)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $json = & powershell -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript | Out-String
        if (-not [string]::IsNullOrWhiteSpace($json)) {
            try {
                $state = $json | ConvertFrom-Json
                if ($state.vm_running -and $state.tools_state -eq 'running' -and $state.shell_healthy) {
                    return $state
                }
            }
            catch {
            }
        }
        Start-Sleep -Seconds 5
    } while ((Get-Date) -lt $deadline)

    throw "VM shell did not become healthy within $TimeoutSeconds seconds."
}

function Resolve-HostIdaRoot {
    param([string]$ExplicitRoot)

    $candidateDirs = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        $candidateDirs.Add($ExplicitRoot)
    }

    foreach ($path in @(
        'C:\Program Files\IDA Pro',
        'C:\Program Files\IDA Professional',
        'C:\Program Files\Hex-Rays',
        'C:\Users\Deniz\Downloads',
        'C:\Users\Deniz\Desktop',
        'H:\Temp'
    )) {
        if (-not $candidateDirs.Contains($path)) {
            $candidateDirs.Add($path)
        }
    }

    foreach ($candidate in $candidateDirs) {
        if (-not (Test-Path $candidate)) {
            continue
        }

        if (Test-Path (Join-Path $candidate 'idat64.exe')) {
            return [pscustomobject]@{
                root = $candidate
                path = (Join-Path $candidate 'idat64.exe')
                source = 'host-directory'
            }
        }

        $nested = Get-ChildItem -Path $candidate -Recurse -Filter 'idat64.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($nested) {
            return [pscustomobject]@{
                root = $nested.Directory.FullName
                path = $nested.FullName
                source = 'host-nested-search'
            }
        }
    }

    return $null
}

function Resolve-HostIdaInstaller {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath) -and (Test-Path $ExplicitPath)) {
        return (Get-Item $ExplicitPath).FullName
    }

    foreach ($root in @(
        'H:\Temp\idafree',
        'C:\Users\Deniz\Downloads',
        'C:\Users\Deniz\Desktop',
        'H:\Temp'
    )) {
        if (-not (Test-Path $root)) {
            continue
        }

        $match = Get-ChildItem -Path $root -Recurse -ErrorAction SilentlyContinue -Include 'IDA Freeware*_exe_*.exe','idafree*_windows.exe' |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($match) {
            return $match.FullName
        }
    }

    return $null
}

$shellBefore = Wait-HealthyShell
Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
$shellReady = Wait-HealthyShell

$hostIda = Resolve-HostIdaRoot -ExplicitRoot $HostInstallerRoot
$hostIdaInstaller = Resolve-HostIdaInstaller -ExplicitPath $HostInstallerPath
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ida-provisioning-$stamp")
$guestRoot = Join-Path $GuestWorkRoot ("ida-provisioning-$stamp")
$hostScript = Join-Path $hostRoot 'probe-ida-state.ps1'
$guestScript = Join-Path $guestRoot 'probe-ida-state.ps1'
$hostResult = Join-Path $hostRoot 'result.json'
$guestResult = Join-Path $guestRoot 'result.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$innerScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$GuestInstallRoot,
    [Parameter(Mandatory = $true)]
    [string]$ResultPath
)

$ErrorActionPreference = 'Stop'

function Find-GuestIda {
    $candidates = New-Object System.Collections.Generic.List[object]
    foreach ($path in @(
        (Join-Path $GuestInstallRoot 'idat64.exe'),
        (Join-Path $GuestInstallRoot 'ida64.exe'),
        (Join-Path (Join-Path $GuestInstallRoot 'Freeware') 'idat64.exe'),
        (Join-Path (Join-Path $GuestInstallRoot 'Freeware') 'ida64.exe'),
        'C:\Program Files\IDA Pro\idat64.exe',
        'C:\Program Files\IDA Pro\ida64.exe',
        'C:\Program Files\IDA Professional\idat64.exe',
        'C:\Program Files\IDA Professional\ida64.exe',
        'C:\Program Files\Hex-Rays\idat64.exe',
        'C:\Program Files\Hex-Rays\ida64.exe'
    )) {
        if (Test-Path $path) {
            $candidates.Add([pscustomobject]@{
                path = $path
                headless = [bool]($path -match 'idat64\.exe$')
            })
        }
    }

    if ($candidates.Count -eq 0) {
        $search = Get-ChildItem -Path 'C:\Program Files', 'C:\Tools' -Recurse -Include 'idat64.exe','ida64.exe' -ErrorAction SilentlyContinue | Select-Object -First 10
        foreach ($item in @($search)) {
            if ($item) {
                $candidates.Add([pscustomobject]@{
                    path = $item.FullName
                    headless = [bool]($item.FullName -match 'idat64\.exe$')
                })
            }
        }
    }

    if ($candidates.Count -eq 0) {
        return $null
    }

    return ($candidates | Sort-Object @{ Expression = 'headless'; Descending = $true }, path | Select-Object -First 1)
}

function Find-LicenseArtifacts {
    param([string]$IdaExePath)

    if ([string]::IsNullOrWhiteSpace($IdaExePath)) {
        return @()
    }

    $root = Split-Path -Parent $IdaExePath
    $paths = @(
        (Join-Path $root 'ida.hexlic'),
        (Join-Path $root '*.hexlic'),
        (Join-Path $root 'ida.key'),
        (Join-Path $root 'license\*.hexlic'),
        (Join-Path $root 'license\ida.key'),
        (Join-Path $root 'cfg\*.hexlic'),
        (Join-Path $root 'cfg\ida.reg'),
        (Join-Path $root 'cfg\ida.key'),
        (Join-Path $env:APPDATA 'Hex-Rays\IDA Pro\*.hexlic'),
        (Join-Path $env:APPDATA 'Hex-Rays\IDA Pro\ida.key')
    )

    $matches = foreach ($path in $paths) {
        Get-ChildItem -Path $path -Force -ErrorAction SilentlyContinue
    }

    return @($matches | Select-Object -ExpandProperty FullName -Unique)
}

$idaInfo = Find-GuestIda
$idaPath = if ($idaInfo) { $idaInfo.path } else { $null }
$licenses = Find-LicenseArtifacts -IdaExePath $idaPath
$productName = $null
$edition = 'unknown'
$hexRaysDecompiler = $false
$headlessAvailable = [bool]($idaInfo -and $idaInfo.headless)

if ($idaPath -and (Test-Path $idaPath)) {
    try {
        $productName = (Get-Item $idaPath).VersionInfo.ProductName
    }
    catch {
    }

    if ($productName -match 'Freeware' -or $idaPath -match '\\Freeware\\') {
        $edition = 'freeware'
    }
    elseif ($licenses.Count -gt 0) {
        $edition = 'licensed'
    }

    $root = Split-Path -Parent $idaPath
    if ($edition -eq 'freeware') {
        $hexRaysDecompiler = $false
    }
    else {
        $hexRaysDecompiler = [bool](Get-ChildItem -Path $root -Recurse -Filter 'hexx64.dll' -ErrorAction SilentlyContinue | Select-Object -First 1)
    }
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    guest_ida_path = $idaPath
    guest_idat64_path = if ($headlessAvailable) { $idaPath } else { $null }
    guest_license_artifacts = @($licenses)
    license_present = (@($licenses).Count -gt 0)
    product_name = $productName
    edition = $edition
    hex_rays_decompiler = $hexRaysDecompiler
    headless_available = $headlessAvailable
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ResultPath) | Out-Null
$payload | ConvertTo-Json -Depth 6 | Set-Content -Path $ResultPath -Encoding UTF8
'@

Set-Content -Path $hostScript -Value $innerScript -Encoding UTF8

function Invoke-GuestProbe {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScript, $guestScript) | Out-Null
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', $guestScript,
        '-GuestInstallRoot', $GuestInstallRoot,
        '-ResultPath', $guestResult
    ) -IgnoreExitCode | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestResult, $hostResult) -IgnoreExitCode | Out-Null
    if (-not (Test-Path $hostResult)) {
        throw 'Guest IDA probe did not produce result.json.'
    }
    return (Get-Content -Path $hostResult -Raw | ConvertFrom-Json)
}

$before = Invoke-GuestProbe
$notes = New-Object System.Collections.Generic.List[string]
$status = 'ready'
$copiedFromHost = $false
$installedFromSharedInstaller = $false
$guestIdaPath = $before.guest_ida_path
$licensePresent = [bool]$before.license_present
$edition = $before.edition
$productName = $before.product_name
$hexRaysDecompiler = [bool]$before.hex_rays_decompiler
$headlessAvailable = [bool]$before.headless_available

if (-not $guestIdaPath) {
    if ($hostIda) {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $GuestInstallRoot) -IgnoreExitCode | Out-Null
        Get-ChildItem -Path $hostIda.root -File | ForEach-Object {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'CopyFileFromHostToGuest', $VmPath, $_.FullName, (Join-Path $GuestInstallRoot $_.Name)
            ) | Out-Null
        }
        $copiedFromHost = $true
        $notes.Add("Portable IDA root copied from host: $($hostIda.root)")
        $afterProbe = Invoke-GuestProbe
        $guestIdaPath = $afterProbe.guest_ida_path
        $licensePresent = [bool]$afterProbe.license_present
        $edition = $afterProbe.edition
        $productName = $afterProbe.product_name
        $hexRaysDecompiler = [bool]$afterProbe.hex_rays_decompiler
        $headlessAvailable = [bool]$afterProbe.headless_available
    }
    elseif ($hostIdaInstaller) {
        New-Item -ItemType Directory -Path $HostSharedFolderPath -Force | Out-Null
        $sharedInstaller = Join-Path $HostSharedFolderPath ([IO.Path]::GetFileName($hostIdaInstaller))
        if (((Resolve-Path $hostIdaInstaller).Path) -ne ([IO.Path]::Combine((Resolve-Path (Split-Path -Parent $sharedInstaller)).Path, ([IO.Path]::GetFileName($sharedInstaller))))) {
            Copy-Item -LiteralPath $hostIdaInstaller -Destination $sharedInstaller -Force
        }
        elseif (-not (Test-Path $sharedInstaller)) {
            Copy-Item -LiteralPath $hostIdaInstaller -Destination $sharedInstaller -Force
        }

        $guestSharedInstaller = "\\vmware-host\Shared Folders\$SharedFolderGuestName\$([IO.Path]::GetFileName($sharedInstaller))"
        $guestInstallerScript = Join-Path $guestRoot 'install-ida-freeware.ps1'
        $hostInstallerScript = Join-Path $hostRoot 'install-ida-freeware.ps1'
        $guestInstallResult = Join-Path $guestRoot 'install-result.json'
        $hostInstallResult = Join-Path $hostRoot 'install-result.json'
        $guestTargetRoot = Join-Path $GuestInstallRoot 'Freeware'

        @'
param(
    [string]$InstallerPath,
    [string]$InstallRoot,
    [string]$ResultPath
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path (Split-Path -Parent $ResultPath) -Force | Out-Null
$payload = [ordered]@{
    installer_path = $InstallerPath
    install_root = $InstallRoot
    installer_visible = (Test-Path $InstallerPath)
    exit_code = $null
    status = 'not-run'
    error = $null
}
try {
    if (-not $payload.installer_visible) { throw 'shared installer not visible' }
    $proc = Start-Process -FilePath $InstallerPath -ArgumentList @('--mode','unattended','--unattendedmodeui','none','--prefix',$InstallRoot) -Wait -PassThru
    $payload.exit_code = $proc.ExitCode
    $payload.status = if ($proc.ExitCode -eq 0) { 'installed' } else { 'failed' }
}
catch {
    $payload.status = 'failed'
    $payload.error = $_.Exception.Message
}
$payload | ConvertTo-Json -Depth 6 | Set-Content -Path $ResultPath -Encoding UTF8
'@ | Set-Content -Path $hostInstallerScript -Encoding UTF8

        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostInstallerScript, $guestInstallerScript) | Out-Null
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'runProgramInGuest', $VmPath,
            'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
            '-NoProfile', '-ExecutionPolicy', 'Bypass',
            '-File', $guestInstallerScript,
            '-InstallerPath', $guestSharedInstaller,
            '-InstallRoot', $guestTargetRoot,
            '-ResultPath', $guestInstallResult
        ) -IgnoreExitCode | Out-Null

        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestInstallResult, $hostInstallResult) -IgnoreExitCode | Out-Null
        if (Test-Path $hostInstallResult) {
            $installResult = Get-Content -Path $hostInstallResult -Raw | ConvertFrom-Json
            $notes.Add("Shared installer status: $($installResult.status)")
            if ($installResult.error) {
                $notes.Add("Shared installer error: $($installResult.error)")
            }
        }

        $installedFromSharedInstaller = $true
        $afterProbe = Invoke-GuestProbe
        $guestIdaPath = $afterProbe.guest_ida_path
        $licensePresent = [bool]$afterProbe.license_present
        $edition = $afterProbe.edition
        $productName = $afterProbe.product_name
        $hexRaysDecompiler = [bool]$afterProbe.hex_rays_decompiler
        $headlessAvailable = [bool]$afterProbe.headless_available
    }
    else {
        $status = 'blocked-installer-missing'
        $notes.Add('No guest IDA installation was found and no host portable installer root was discovered.')
    }
}

if ($guestIdaPath -and $edition -eq 'freeware') {
    $status = if ($headlessAvailable) { 'provisioned-freeware' } else { 'provisioned-freeware-gui-only' }
    $notes.Add('IDA Freeware is installed in the guest. This is enough for string/xref/disassembly work, but Hex-Rays decompiler features remain unavailable.')
    if (-not $headlessAvailable) {
        $notes.Add('The freeware install exposes ida64.exe but not idat64.exe, so headless IDAPython automation is still blocked.')
    }
}
elseif ($guestIdaPath -and -not $licensePresent) {
    $status = 'blocked-license-missing'
    $notes.Add('IDA binary path exists, but no local license artifact was detected under the resolved guest root.')
}
elseif ($guestIdaPath -and $status -eq 'ready' -and ($copiedFromHost -or $installedFromSharedInstaller)) {
    $status = 'provisioned'
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    vm_profile = $VmProfile
    guest_install_root = $GuestInstallRoot
    guest_ida_path = $guestIdaPath
    guest_idat64_path = if ($headlessAvailable) { $guestIdaPath } else { $null }
    status = $status
    license_present = $licensePresent
    edition = $edition
    product_name = $productName
    hex_rays_decompiler = $hexRaysDecompiler
    headless_available = $headlessAvailable
    host_installer_root = if ($hostIda) { $hostIda.root } else { $null }
    host_installer_source = if ($hostIda) { $hostIda.source } else { $null }
    host_installer_path = $hostIdaInstaller
    copied_from_host = $copiedFromHost
    installed_from_shared_installer = $installedFromSharedInstaller
    shell_health_before = $shellBefore
    shell_health_ready = $shellReady
    notes = @($notes)
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFile) | Out-Null
$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputFile -Encoding UTF8
$payload | ConvertTo-Json -Depth 8
