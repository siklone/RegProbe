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

$shellBefore = Wait-HealthyShell
Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
$shellReady = Wait-HealthyShell

$hostIda = Resolve-HostIdaRoot -ExplicitRoot $HostInstallerRoot
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
    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($path in @(
        (Join-Path $GuestInstallRoot 'idat64.exe'),
        'C:\Program Files\IDA Pro\idat64.exe',
        'C:\Program Files\IDA Professional\idat64.exe',
        'C:\Program Files\Hex-Rays\idat64.exe'
    )) {
        if (Test-Path $path) {
            $candidates.Add($path)
        }
    }

    if ($candidates.Count -eq 0) {
        $search = Get-ChildItem -Path 'C:\Program Files', 'C:\Tools' -Recurse -Filter 'idat64.exe' -ErrorAction SilentlyContinue | Select-Object -First 5
        foreach ($item in @($search)) {
            if ($item -and -not $candidates.Contains($item.FullName)) {
                $candidates.Add($item.FullName)
            }
        }
    }

    if ($candidates.Count -eq 0) {
        return $null
    }

    return $candidates[0]
}

function Find-LicenseArtifacts {
    param([string]$IdaExePath)

    if ([string]::IsNullOrWhiteSpace($IdaExePath)) {
        return @()
    }

    $root = Split-Path -Parent $IdaExePath
    $paths = @(
        (Join-Path $root 'ida.key'),
        (Join-Path $root 'license\ida.key'),
        (Join-Path $root 'cfg\ida.reg'),
        (Join-Path $root 'cfg\ida.key')
    )

    return @($paths | Where-Object { Test-Path $_ })
}

$idaPath = Find-GuestIda
$licenses = Find-LicenseArtifacts -IdaExePath $idaPath

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    guest_idat64_path = $idaPath
    guest_license_artifacts = @($licenses)
    license_present = (@($licenses).Count -gt 0)
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
$guestIdaPath = $before.guest_idat64_path
$licensePresent = [bool]$before.license_present

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
        $guestIdaPath = $afterProbe.guest_idat64_path
        $licensePresent = [bool]$afterProbe.license_present
    }
    else {
        $status = 'blocked-installer-missing'
        $notes.Add('No guest IDA installation was found and no host portable installer root was discovered.')
    }
}

if ($guestIdaPath -and -not $licensePresent) {
    $status = 'blocked-license-missing'
    $notes.Add('IDA binary path exists, but no local license artifact was detected under the resolved guest root.')
}
elseif ($guestIdaPath -and $status -eq 'ready' -and $copiedFromHost) {
    $status = 'provisioned'
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    vm_profile = $VmProfile
    guest_install_root = $GuestInstallRoot
    guest_idat64_path = $guestIdaPath
    status = $status
    license_present = $licensePresent
    host_installer_root = if ($hostIda) { $hostIda.root } else { $null }
    host_installer_source = if ($hostIda) { $hostIda.source } else { $null }
    copied_from_host = $copiedFromHost
    shell_health_before = $shellBefore
    shell_health_ready = $shellReady
    notes = @($notes)
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFile) | Out-Null
$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputFile -Encoding UTF8
$payload | ConvertTo-Json -Depth 8
