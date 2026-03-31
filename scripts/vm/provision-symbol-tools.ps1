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
    [string]$HostSdkRoot = '',
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

$shellBefore = Wait-HealthyShell
Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
$shellReady = Wait-HealthyShell

$hostCandidate = Get-HostSdkCandidate -ExplicitRoot $HostSdkRoot
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("symbol-tools-provisioning-$stamp")
$guestRoot = Join-Path $GuestWorkRoot ("symbol-tools-provisioning-$stamp")
$hostScript = Join-Path $hostRoot 'provision-symbol-tools-inner.ps1'
$guestScript = Join-Path $guestRoot 'provision-symbol-tools-inner.ps1'
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

New-Item -ItemType Directory -Force -Path $GuestInstallRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ResultPath) | Out-Null

$before = [ordered]@{
    winget_available = [bool](Get-WingetCommand)
    symchk_path = Find-Symchk
}

$attempts = @()
$notes = New-Object System.Collections.Generic.List[string]
$status = 'ready'

if (-not $before.symchk_path) {
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

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile', '-ExecutionPolicy', 'Bypass',
    '-File', $guestScript,
    '-GuestInstallRoot', $GuestInstallRoot,
    '-ResultPath', $guestResult
) -IgnoreExitCode | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromGuestToHost', $VmPath, $guestResult, $hostResult
) -IgnoreExitCode | Out-Null

if (-not (Test-Path $hostResult)) {
    throw "Guest symbol tool provisioning did not produce result.json."
}

$payload = Get-Content -Path $hostResult -Raw | ConvertFrom-Json
$payload | Add-Member -NotePropertyName vm_path -NotePropertyValue $VmPath -Force
$payload | Add-Member -NotePropertyName vm_profile -NotePropertyValue $VmProfile -Force
$payload | Add-Member -NotePropertyName guest_install_root -NotePropertyValue $GuestInstallRoot -Force
$payload | Add-Member -NotePropertyName host_fallback_root -NotePropertyValue $(if ($hostCandidate) { $hostCandidate.root } else { $null }) -Force
$payload | Add-Member -NotePropertyName host_fallback_applied -NotePropertyValue $hostFallbackApplied -Force
$payload | Add-Member -NotePropertyName shell_health_before -NotePropertyValue $shellBefore -Force
$payload | Add-Member -NotePropertyName shell_health_ready -NotePropertyValue $shellReady -Force

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFile) | Out-Null
$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputFile -Encoding UTF8
$payload | ConvertTo-Json -Depth 10
