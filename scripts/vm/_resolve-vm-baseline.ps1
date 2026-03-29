Set-StrictMode -Version Latest

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

function Resolve-CanonicalVmPath {
    param(
        [string]$Fallback = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
        [string]$ConfigPath = ''
    )

    try {
        $config = Get-VmBaselineConfig -ConfigPath $ConfigPath
        if (-not [string]::IsNullOrWhiteSpace($config.vm_path)) {
            return $config.vm_path
        }
    }
    catch {
    }

    return $Fallback
}

function Resolve-DefaultVmSnapshotName {
    param(
        [string]$Fallback = 'RegProbe-Baseline-Clean-20260329',
        [string]$ConfigPath = ''
    )

    try {
        $config = Get-VmBaselineConfig -ConfigPath $ConfigPath
        if (-not [string]::IsNullOrWhiteSpace($config.default_snapshot)) {
            return $config.default_snapshot
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
