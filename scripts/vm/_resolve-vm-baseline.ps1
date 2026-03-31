Set-StrictMode -Version Latest

function Test-VmConfigValueUsable {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    if ($Value -match '<[A-Z0-9_:-]+>') {
        return $false
    }

    return $true
}

function Get-VmBaselineConfig {
    param(
        [string]$ConfigPath = ''
    )

    if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
        $ConfigPath = Join-Path $repoRoot 'registry-research-framework\config\vm-baselines.json'
    }

    if (-not (Test-Path $ConfigPath)) {
        throw "VM baseline config not found at $ConfigPath"
    }

    return Get-Content -Path $ConfigPath -Raw | ConvertFrom-Json
}

function Resolve-VmProfileName {
    param(
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $config = Get-VmBaselineConfig -ConfigPath $ConfigPath
        $requestedProfile = if ([string]::IsNullOrWhiteSpace($VmProfile)) {
            if (-not [string]::IsNullOrWhiteSpace($config.default_profile)) {
                [string]$config.default_profile
            }
            elseif (-not [string]::IsNullOrWhiteSpace($config.canonical_profile)) {
                [string]$config.canonical_profile
            }
            else {
                'primary'
            }
        }
        else {
            $VmProfile
        }

        if ($config.profiles -and $config.profiles.PSObject.Properties.Name -contains $requestedProfile) {
            return $requestedProfile
        }
    }
    catch {
    }

    return (if ([string]::IsNullOrWhiteSpace($VmProfile)) { 'primary' } else { $VmProfile })
}

function Resolve-VmProfileTag {
    param(
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    $resolvedProfile = Resolve-VmProfileName -VmProfile $VmProfile -ConfigPath $ConfigPath
    if ([string]::IsNullOrWhiteSpace($resolvedProfile)) {
        return 'primary'
    }

    return ($resolvedProfile -replace '[^A-Za-z0-9_-]', '-').ToLowerInvariant()
}

function Resolve-CanonicalVmName {
    param(
        [string]$Fallback = '',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.vm_name)) {
            return [string]$profile.vm_name
        }
    }
    catch {
    }

    if (-not [string]::IsNullOrWhiteSpace($Fallback)) {
        return $Fallback
    }

    switch (Resolve-VmProfileName -VmProfile $VmProfile -ConfigPath $ConfigPath) {
        'secondary' { return 'Win25H2Clean-B' }
        default { return 'Win25H2Clean' }
    }
}

function Resolve-CanonicalVmPathFallback {
    param(
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    switch (Resolve-VmProfileName -VmProfile $VmProfile -ConfigPath $ConfigPath) {
        'secondary' { return 'H:\Yedek\VMs\Win25H2Clean-B\Win25H2Clean-B.vmx' }
        default { return 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx' }
    }
}

function Get-VmProfileConfig {
    param(
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    $config = Get-VmBaselineConfig -ConfigPath $ConfigPath
    $resolvedProfile = Resolve-VmProfileName -VmProfile $VmProfile -ConfigPath $ConfigPath

    if ($config.profiles -and $config.profiles.PSObject.Properties.Name -contains $resolvedProfile) {
        return $config.profiles.$resolvedProfile
    }

    return [pscustomobject]@{
        vm_name = $config.canonical_vm
        vm_path = $config.vm_path
        default_snapshot = $config.default_snapshot
        seed_snapshot = $config.seed_snapshot
        host_staging_root = $config.host_staging_root
        tracked_output_root = $config.tracked_output_root
        guest_diag_root = $config.guest_diag_root
        guest_script_root = $config.guest_script_root
    }
}

function Resolve-CanonicalVmPath {
    param(
        [string]$Fallback = '',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.vm_path)) {
            return $profile.vm_path
        }
    }
    catch {
    }

    if ([string]::IsNullOrWhiteSpace($Fallback)) {
        $Fallback = Resolve-CanonicalVmPathFallback -VmProfile $VmProfile -ConfigPath $ConfigPath
    }

    return $Fallback
}

function Resolve-DefaultVmSnapshotName {
    param(
        [string]$Fallback = 'RegProbe-Baseline-Clean-20260329',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.default_snapshot)) {
            return $profile.default_snapshot
        }
    }
    catch {
    }

    return $Fallback
}

function Resolve-SeedVmSnapshotName {
    param(
        [string]$Fallback = 'RegProbe-Baseline-ToolsHardened-20260330',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.seed_snapshot)) {
            return $profile.seed_snapshot
        }
    }
    catch {
    }

    return $Fallback
}

function Resolve-HostStagingRoot {
    param(
        [string]$Fallback = '',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.host_staging_root)) {
            return $profile.host_staging_root
        }
    }
    catch {
    }

    if (-not [string]::IsNullOrWhiteSpace($Fallback)) {
        return $Fallback
    }

    $resolvedProfile = Resolve-VmProfileName -VmProfile $VmProfile -ConfigPath $ConfigPath
    return (Join-Path ([System.IO.Path]::GetTempPath()) ("vm-tooling-staging-{0}" -f $resolvedProfile))
}

function Resolve-TrackedVmOutputRoot {
    param(
        [string]$Fallback = '',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.tracked_output_root)) {
            return $profile.tracked_output_root
        }
    }
    catch {
    }

    if (-not [string]::IsNullOrWhiteSpace($Fallback)) {
        return $Fallback
    }

    $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
    $resolvedProfile = Resolve-VmProfileName -VmProfile $VmProfile -ConfigPath $ConfigPath
    return (Join-Path $repoRoot ("evidence\files\vm-tooling-staging\{0}" -f $resolvedProfile))
}

function Resolve-GuestDiagRoot {
    param(
        [string]$Fallback = 'C:\RegProbe-Diag',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.guest_diag_root)) {
            return $profile.guest_diag_root
        }
    }
    catch {
    }

    return $Fallback
}

function Resolve-GuestScriptRoot {
    param(
        [string]$Fallback = 'C:\Tools\Scripts',
        [string]$VmProfile = '',
        [string]$ConfigPath = ''
    )

    try {
        $profile = Get-VmProfileConfig -VmProfile $VmProfile -ConfigPath $ConfigPath
        if (Test-VmConfigValueUsable -Value ([string]$profile.guest_script_root)) {
            return $profile.guest_script_root
        }
    }
    catch {
    }

    return $Fallback
}

function Get-LegacyVmSnapshotNames {
    param(
        [string[]]$Fallback = @(
            'baseline-20260324-high-risk-lane',
            'baseline-20260325-defender-on',
            'baseline-20260325-shell-stable',
            'baseline-20260327-shell-stable',
            'baseline-20260327-regprobe-visible-shell-stable',
            'RegProbe-Baseline-20260328'
        ),
        [string]$ConfigPath = ''
    )

    try {
        $config = Get-VmBaselineConfig -ConfigPath $ConfigPath
        if ($config.legacy_snapshots -and $config.legacy_snapshots.Count -gt 0) {
            return @($config.legacy_snapshots)
        }
    }
    catch {
    }

    return $Fallback
}
