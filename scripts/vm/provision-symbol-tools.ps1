[CmdletBinding()]
param(
    [string]$VmProfile = 'primary',
    [string]$ConfigPath = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$GuestInstallRoot = 'C:\Tools\SymbolTools',
    [string]$GuestWorkRoot = 'C:\RegProbe-Diag\SymbolTools',
    [bool]$EnableGuestWingetProvisioning = $false,
    [bool]$EnableGuestBootstrapperFallback = $false,
    [bool]$EnableGuestSharedWinDbgFallback = $false,
    [string]$HostSdkRoot = '',
    [string]$HostSdkInstallerPath = '',
    [string]$HostSdkLayoutRoot = '',
    [string]$HostSharedFolderPath = 'H:\Temp\vm-tooling-staging',
    [string]$SharedFolderGuestName = 'vm-tooling-staging',
    [string]$HostWinDbgPackagePath = '',
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
    $OutputFile = Join-Path $repoRoot ("registry-research-framework\audit\symbol-tools-provisioning-$stamp.json")
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

function Get-HostSdkCandidate {
    param([string]$ExplicitRoot)

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        $candidates.Add($ExplicitRoot)
    }

    foreach ($path in @(
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Debugging Tools for Windows (x64)',
        'C:\Program Files\Debugging Tools for Windows'
    )) {
        if (-not $candidates.Contains($path)) {
            $candidates.Add($path)
        }
    }

    foreach ($candidate in $candidates) {
        if (-not (Test-Path $candidate)) {
            continue
        }

        $symchk = Join-Path $candidate 'symchk.exe'
        if (-not (Test-Path $symchk)) {
            continue
        }

        $dbghelp = Join-Path $candidate 'dbghelp.dll'
        $symsrv = Join-Path $candidate 'symsrv.dll'

        return [pscustomobject]@{
            root = $candidate
            symchk = $symchk
            dbghelp = if (Test-Path $dbghelp) { $dbghelp } else { $null }
            symsrv = if (Test-Path $symsrv) { $symsrv } else { $null }
        }
    }

    return $null
}

function Get-HostSdkInstallerCandidate {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath) -and (Test-Path $ExplicitPath)) {
        return (Get-Item $ExplicitPath).FullName
    }

    foreach ($root in @(
        'H:\Temp\winget-downloads',
        'C:\Users\Deniz\Downloads',
        'C:\Users\Deniz\Desktop',
        'H:\Temp'
    )) {
        if (-not (Test-Path $root)) {
            continue
        }

        $match = Get-ChildItem -Path $root -Recurse -ErrorAction SilentlyContinue -Include 'winsdksetup.exe','Windows SDK*_exe_*.exe' |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($match) {
            return $match.FullName
        }
    }

    return $null
}

function Get-HostWinDbgPackageCandidate {
    param(
        [string]$ExplicitPath,
        [string]$SharedFolderPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath) -and (Test-Path $ExplicitPath)) {
        return (Get-Item $ExplicitPath).FullName
    }

    foreach ($root in @(
        $SharedFolderPath,
        'H:\Temp\winget-downloads\windbg',
        'H:\Temp\winget-downloads',
        'C:\Users\Deniz\Downloads',
        'H:\Temp'
    )) {
        if ([string]::IsNullOrWhiteSpace($root) -or -not (Test-Path $root)) {
            continue
        }

        $match = Get-ChildItem -Path $root -Recurse -ErrorAction SilentlyContinue -Include 'WinDbg*.msix','WinDbg*.msixbundle' |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($match) {
            return $match.FullName
        }
    }

    return $null
}

function Invoke-HostSdkLayoutProvision {
    param(
        [string]$InstallerPath,
        [string]$LayoutRoot
    )

    if ([string]::IsNullOrWhiteSpace($InstallerPath) -or -not (Test-Path $InstallerPath)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($LayoutRoot)) {
        $LayoutRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'RegProbe-WinSdk-Layout'
    }

    New-Item -ItemType Directory -Path $LayoutRoot -Force | Out-Null

    $proc = Start-Process -FilePath $InstallerPath -ArgumentList @('/layout', $LayoutRoot, '/q') -Wait -PassThru -WindowStyle Hidden
    $symchk = Get-ChildItem -Path $LayoutRoot -Recurse -Filter 'symchk.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $symchk) {
        return [pscustomobject]@{
            layout_root = $LayoutRoot
            exit_code = $proc.ExitCode
            symchk = $null
            dbghelp = $null
            symsrv = $null
        }
    }

    $root = $symchk.Directory.FullName
    $dbghelp = Join-Path $root 'dbghelp.dll'
    $symsrv = Join-Path $root 'symsrv.dll'

    return [pscustomobject]@{
        layout_root = $LayoutRoot
        exit_code = $proc.ExitCode
        symchk = $symchk.FullName
        dbghelp = if (Test-Path $dbghelp) { $dbghelp } else { $null }
        symsrv = if (Test-Path $symsrv) { $symsrv } else { $null }
    }
}

$shellBefore = Wait-HealthyShell
Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
$shellReady = Wait-HealthyShell

$hostCandidate = Get-HostSdkCandidate -ExplicitRoot $HostSdkRoot
$hostInstaller = Get-HostSdkInstallerCandidate -ExplicitPath $HostSdkInstallerPath
$hostLayoutAttempt = $null
if (-not $hostCandidate -and $hostInstaller) {
    $hostLayoutAttempt = Invoke-HostSdkLayoutProvision -InstallerPath $hostInstaller -LayoutRoot $HostSdkLayoutRoot
    if ($hostLayoutAttempt -and $hostLayoutAttempt.symchk) {
        $hostCandidate = [pscustomobject]@{
            root = (Split-Path -Parent $hostLayoutAttempt.symchk)
            symchk = $hostLayoutAttempt.symchk
            dbghelp = $hostLayoutAttempt.dbghelp
            symsrv = $hostLayoutAttempt.symsrv
        }
    }
}
$hostWinDbgPackage = Get-HostWinDbgPackageCandidate -ExplicitPath $HostWinDbgPackagePath -SharedFolderPath $HostSharedFolderPath
$guestSharedWinDbgPackage = $null
$sharedPackagePrepared = $false
$sharedPackageSource = $null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("symbol-tools-provisioning-$stamp")
$guestRoot = Join-Path $GuestWorkRoot ("symbol-tools-provisioning-$stamp")
$hostScript = Join-Path $hostRoot 'provision-symbol-tools-inner.ps1'
$guestScript = Join-Path $guestRoot 'provision-symbol-tools-inner.ps1'
$hostResult = Join-Path $hostRoot 'result.json'
$guestResult = Join-Path $guestRoot 'result.json'
$guestInstaller = Join-Path $guestRoot 'winsdksetup.exe'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$innerScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$GuestInstallRoot,
    [bool]$EnableGuestWingetProvisioning = $false,
    [bool]$EnableSharedWinDbgFallback = $false,
    [string]$SharedWinDbgPackagePath = '',
    [Parameter(Mandatory = $true)]
    [string]$ResultPath
)

$ErrorActionPreference = 'Stop'

function Get-WingetCommand {
    Get-Command winget.exe -ErrorAction SilentlyContinue
}

function Find-Symchk {
    $roots = New-Object System.Collections.Generic.List[string]
    foreach ($path in @(
        $GuestInstallRoot,
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Debugging Tools for Windows (x64)',
        'C:\Program Files\Debugging Tools for Windows'
    )) {
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path) -and -not $roots.Contains($path)) {
            $roots.Add($path)
        }
    }

    foreach ($pkg in @(Get-AppxPackage -Name Microsoft.WinDbg* -ErrorAction SilentlyContinue)) {
        if ($pkg.InstallLocation -and (Test-Path $pkg.InstallLocation) -and -not $roots.Contains($pkg.InstallLocation)) {
            $roots.Add($pkg.InstallLocation)
        }
    }

    foreach ($root in $roots) {
        $candidate = Get-ChildItem -Path $root -Recurse -Filter 'symchk.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    return $null
}

function Invoke-WingetInstall {
    param([Parameter(Mandatory = $true)][string]$PackageId)

    $winget = Get-WingetCommand
    if ($null -eq $winget) {
        return [ordered]@{
            package_id = $PackageId
            executed = $false
            exit_code = $null
            status = 'blocked-winget-missing'
        }
    }

    $args = @(
        'install',
        '--id', $PackageId,
        '--exact',
        '--source', 'winget',
        '--accept-package-agreements',
        '--accept-source-agreements',
        '--disable-interactivity'
    )

    $proc = Start-Process -FilePath $winget.Source -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
    return [ordered]@{
        package_id = $PackageId
        executed = $true
        exit_code = $proc.ExitCode
        status = if ($proc.ExitCode -eq 0) { 'ok' } else { 'failed' }
    }
}

function Invoke-SharedWinDbgInstall {
    param([string]$PackagePath)

    if ([string]::IsNullOrWhiteSpace($PackagePath)) {
        return [ordered]@{
            package_id = 'SharedWinDbgPackage'
            executed = $false
            exit_code = $null
            status = 'skipped-no-path'
        }
    }

    if (-not (Test-Path $PackagePath)) {
        return [ordered]@{
            package_id = 'SharedWinDbgPackage'
            executed = $false
            exit_code = $null
            status = 'blocked-shared-path-missing'
            package_path = $PackagePath
        }
    }

    try {
        $localStageRoot = Join-Path $env:TEMP 'RegProbe-SymbolTools'
        New-Item -ItemType Directory -Path $localStageRoot -Force | Out-Null
        $localPackagePath = Join-Path $localStageRoot ([IO.Path]::GetFileName($PackagePath))
        Copy-Item -LiteralPath $PackagePath -Destination $localPackagePath -Force
        Add-AppxPackage -Path $localPackagePath -ErrorAction Stop
        return [ordered]@{
            package_id = 'SharedWinDbgPackage'
            executed = $true
            exit_code = 0
            status = 'ok'
            package_path = $PackagePath
            local_package_path = $localPackagePath
        }
    }
    catch {
        return [ordered]@{
            package_id = 'SharedWinDbgPackage'
            executed = $true
            exit_code = $null
            status = 'failed'
            package_path = $PackagePath
            local_package_path = if (Test-Path variable:localPackagePath) { $localPackagePath } else { $null }
            error = $_.Exception.Message
        }
    }
}

New-Item -ItemType Directory -Force -Path $GuestInstallRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ResultPath) | Out-Null

$before = [ordered]@{
    winget_available = [bool](Get-WingetCommand)
    symchk_path = Find-Symchk
}

$attempts = @()
$notes = New-Object System.Collections.Generic.List[string]
$status = 'ready'

if (-not $EnableGuestWingetProvisioning) {
    $notes.Add('Guest winget provisioning is disabled by default because the VM is treated as offline for this lane.')
}
if (-not $EnableSharedWinDbgFallback) {
    $notes.Add('Guest shared-package fallback is disabled for symbol provisioning because the current WinDbg package does not provide symchk.exe.')
}

if (-not $before.symchk_path -and $EnableGuestWingetProvisioning) {
    foreach ($packageId in @('Microsoft.WinDbg', 'Microsoft.WindowsSDK.10.0.18362', 'Microsoft.WindowsSDK.10.0.17134')) {
        $result = Invoke-WingetInstall -PackageId $packageId
        $attempts += $result
        Start-Sleep -Seconds 3
        $path = Find-Symchk
        if ($path) {
            $after = [ordered]@{
                winget_available = [bool](Get-WingetCommand)
                symchk_path = $path
            }
            $payload = [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                status = 'ready'
                before = $before
                after = $after
                attempts = $attempts
                notes = @('symchk.exe was discovered after provisioning.')
            }
            $payload | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultPath -Encoding UTF8
            exit 0
        }
    }
}

if (-not $before.symchk_path -and $EnableSharedWinDbgFallback) {
    $sharedInstall = Invoke-SharedWinDbgInstall -PackagePath $SharedWinDbgPackagePath
    if ($sharedInstall.executed -or $sharedInstall.status -ne 'skipped-no-path') {
        $attempts += $sharedInstall
        Start-Sleep -Seconds 3
        $path = Find-Symchk
        if ($path) {
            $after = [ordered]@{
                winget_available = [bool](Get-WingetCommand)
                symchk_path = $path
            }
            $payload = [ordered]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                status = 'ready'
                before = $before
                after = $after
                attempts = $attempts
                notes = @('symchk.exe was discovered after shared-package provisioning.')
            }
            $payload | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultPath -Encoding UTF8
            exit 0
        }
    }
}

$after = [ordered]@{
    winget_available = [bool](Get-WingetCommand)
    symchk_path = Find-Symchk
}

if (-not $after.symchk_path) {
    $status = if ($after.winget_available) { 'blocked-symchk-still-missing' } else { 'blocked-winget-missing' }
    $notes.Add('No guest symchk.exe was found after the provisioning attempts.')
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = $status
    before = $before
    after = $after
    attempts = $attempts
    notes = @($notes)
}
$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultPath -Encoding UTF8
'@

Set-Content -Path $hostScript -Value $innerScript -Encoding UTF8

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $GuestInstallRoot) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScript, $guestScript) | Out-Null

$hostFallbackApplied = $false
if ($hostCandidate) {
    foreach ($file in @($hostCandidate.symchk, $hostCandidate.dbghelp, $hostCandidate.symsrv)) {
        if ([string]::IsNullOrWhiteSpace($file) -or -not (Test-Path $file)) {
            continue
        }
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CopyFileFromHostToGuest', $VmPath, $file, (Join-Path $GuestInstallRoot ([IO.Path]::GetFileName($file)))
        ) | Out-Null
        $hostFallbackApplied = $true
    }
}

function Invoke-GuestProvisionProbe {
    $guestArgs = @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', $guestScript,
        '-GuestInstallRoot', $GuestInstallRoot,
        '-ResultPath', $guestResult
    )

    if ($EnableGuestSharedWinDbgFallback -and $guestSharedWinDbgPackage) {
        $guestArgs += @('-EnableSharedWinDbgFallback', 'true', '-SharedWinDbgPackagePath', $guestSharedWinDbgPackage)
    }

    if ($EnableGuestWingetProvisioning) {
        $guestArgs += @('-EnableGuestWingetProvisioning', 'true')
    }

    Invoke-Vmrun -Arguments $guestArgs -IgnoreExitCode | Out-Null

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromGuestToHost', $VmPath, $guestResult, $hostResult
    ) -IgnoreExitCode | Out-Null

    if (-not (Test-Path $hostResult)) {
        throw "Guest symbol tool provisioning did not produce result.json."
    }

    return (Get-Content -Path $hostResult -Raw | ConvertFrom-Json)
}

if (-not [string]::IsNullOrWhiteSpace($hostWinDbgPackage) -and -not [string]::IsNullOrWhiteSpace($HostSharedFolderPath) -and -not [string]::IsNullOrWhiteSpace($SharedFolderGuestName)) {
    New-Item -ItemType Directory -Path $HostSharedFolderPath -Force | Out-Null
    $sharedTarget = Join-Path $HostSharedFolderPath ([IO.Path]::GetFileName($hostWinDbgPackage))
    $resolvedSource = (Resolve-Path $hostWinDbgPackage).Path
    $resolvedTarget = [IO.Path]::Combine((Resolve-Path (Split-Path -Parent $sharedTarget)).Path, ([IO.Path]::GetFileName($sharedTarget)))
    if ($resolvedSource -ne $resolvedTarget) {
        Copy-Item -LiteralPath $hostWinDbgPackage -Destination $sharedTarget -Force
    }
    elseif (-not (Test-Path $sharedTarget)) {
        Copy-Item -LiteralPath $hostWinDbgPackage -Destination $sharedTarget -Force
    }
    $sharedPackagePrepared = Test-Path $sharedTarget
    if ($sharedPackagePrepared) {
        $sharedPackageSource = $sharedTarget
        $guestSharedWinDbgPackage = "\\vmware-host\Shared Folders\$SharedFolderGuestName\$([IO.Path]::GetFileName($sharedTarget))"
    }
}

$payload = Invoke-GuestProvisionProbe

$offlineInstallerApplied = $false
$offlineInstallerExitCode = $null
if ($payload.status -eq 'blocked-symchk-still-missing' -and $hostInstaller -and $EnableGuestBootstrapperFallback) {
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $hostInstaller, $guestInstaller
    ) | Out-Null

    $installerOutput = Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        $guestInstaller,
        '/features', 'OptionId.WindowsDesktopDebuggers',
        '/quiet',
        '/norestart',
        '/ceip', 'off'
    ) -IgnoreExitCode

    $offlineInstallerApplied = $true
    if ($installerOutput -match 'Guest program exited with non-zero exit code:\s*(-?\d+)') {
        $offlineInstallerExitCode = [int]$matches[1]
    }

    Start-Sleep -Seconds 10
    $payload = Invoke-GuestProvisionProbe
}
$payload | Add-Member -NotePropertyName vm_path -NotePropertyValue $VmPath -Force
$payload | Add-Member -NotePropertyName vm_profile -NotePropertyValue $VmProfile -Force
$payload | Add-Member -NotePropertyName guest_install_root -NotePropertyValue $GuestInstallRoot -Force
$payload | Add-Member -NotePropertyName host_fallback_root -NotePropertyValue $(if ($hostCandidate) { $hostCandidate.root } else { $null }) -Force
$payload | Add-Member -NotePropertyName host_fallback_applied -NotePropertyValue $hostFallbackApplied -Force
$payload | Add-Member -NotePropertyName host_offline_installer_path -NotePropertyValue $hostInstaller -Force
$payload | Add-Member -NotePropertyName host_offline_installer_applied -NotePropertyValue $offlineInstallerApplied -Force
$payload | Add-Member -NotePropertyName host_offline_installer_exit_code -NotePropertyValue $offlineInstallerExitCode -Force
$payload | Add-Member -NotePropertyName host_layout_root -NotePropertyValue $(if ($hostLayoutAttempt) { $hostLayoutAttempt.layout_root } else { $null }) -Force
$payload | Add-Member -NotePropertyName host_layout_exit_code -NotePropertyValue $(if ($hostLayoutAttempt) { $hostLayoutAttempt.exit_code } else { $null }) -Force
$payload | Add-Member -NotePropertyName host_windbg_package_path -NotePropertyValue $hostWinDbgPackage -Force
$payload | Add-Member -NotePropertyName shared_package_prepared -NotePropertyValue $sharedPackagePrepared -Force
$payload | Add-Member -NotePropertyName shared_package_source -NotePropertyValue $sharedPackageSource -Force
$payload | Add-Member -NotePropertyName guest_shared_package_path -NotePropertyValue $guestSharedWinDbgPackage -Force
$payload | Add-Member -NotePropertyName shell_health_before -NotePropertyValue $shellBefore -Force
$payload | Add-Member -NotePropertyName shell_health_ready -NotePropertyValue $shellReady -Force

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFile) | Out-Null
$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputFile -Encoding UTF8
$payload | ConvertTo-Json -Depth 10
