[CmdletBinding()]
param(
    [string]$SourceVmProfile = 'primary',
    [string]$SourceVmPath = '',
    [string]$TargetVmName = 'Win25H2DebugOnly',
    [string]$TargetVmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [string]$SourceSnapshotName = '',
    [string]$DebugSnapshotName = 'RegProbe-Debug-VMwareOnly-Baseline-20260403',
    [string]$PipeName = '\\.\pipe\regprobe_debug_vmware_debugonly',
    [ValidateSet('server', 'client')]
    [string]$PipeEndpoint = 'server',
    [ValidateSet('TRUE', 'FALSE')]
    [string]$TryNoRxLoss = 'FALSE',
    [ValidateSet('full', 'linked')]
    [string]$CloneType = 'full',
    [switch]$ReplaceExisting,
    [switch]$PlanOnly,
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')
. (Join-Path $PSScriptRoot '_vmrun-common.ps1')

$storageHealthScript = Join-Path $PSScriptRoot 'test-vm-storage-health.ps1'
$configureScript = Join-Path $PSScriptRoot 'configure-kernel-debug-baseline.ps1'

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Resolve-DefaultTargetVmPath {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$TargetName
    )

    $sourceVmDirectory = Split-Path -Parent ([System.IO.Path]::GetFullPath($SourcePath))
    $sourceRoot = Split-Path -Parent $sourceVmDirectory
    $targetDirectory = Join-Path $sourceRoot $TargetName
    return (Join-Path $targetDirectory ("{0}.vmx" -f $TargetName))
}

function Test-SafeTargetPath {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$TargetPath,
        [Parameter(Mandatory = $true)][string]$TargetName
    )

    $sourceFull = [System.IO.Path]::GetFullPath($SourcePath)
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)
    $targetDir = Split-Path -Parent $targetFull
    $sourceDir = Split-Path -Parent $sourceFull
    $sourceRoot = Split-Path -Parent $sourceDir

    if ($sourceFull.Equals($targetFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Target VMware debug-only VM path must not equal the source runtime VM path.'
    }

    if (-not $targetDir.StartsWith($sourceRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside the source VM root family: $targetDir"
    }

    if ((Split-Path -Leaf $targetDir) -ne $TargetName) {
        throw "Refusing to operate on target directory with unexpected leaf name: $targetDir"
    }
}

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    return Invoke-RegProbeVmrun -VmrunPath $VmrunPath -Arguments $Arguments -IgnoreExitCode:$IgnoreExitCode
}

function Test-VmRunning {
    param([string]$VmPathToCheck)

    $listOutput = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') -IgnoreExitCode
    return ($listOutput -match [regex]::Escape($VmPathToCheck))
}

function Wait-VmStopped {
    param(
        [Parameter(Mandatory = $true)][string]$VmPathToCheck,
        [int]$TimeoutSeconds = 300
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (-not (Test-VmRunning -VmPathToCheck $VmPathToCheck)) {
            return
        }

        Start-Sleep -Seconds 3
    }

    throw "VM did not stop in time: $VmPathToCheck"
}

function Wait-GuestReady {
    param(
        [Parameter(Mandatory = $true)][string]$VmPathToCheck,
        [int]$TimeoutSeconds = 600
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPathToCheck)
            if ($state -match 'running|installed') {
                return [string]$state
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw "Guest did not reach a ready VMware Tools state in time: $VmPathToCheck"
}

function Remove-ExistingTargetVm {
    param(
        [Parameter(Mandatory = $true)][string]$TargetVmPathToRemove,
        [Parameter(Mandatory = $true)][string]$SourceVmPathToProtect,
        [Parameter(Mandatory = $true)][string]$TargetName
    )

    Test-SafeTargetPath -SourcePath $SourceVmPathToProtect -TargetPath $TargetVmPathToRemove -TargetName $TargetName

    if (Test-VmRunning -VmPathToCheck $TargetVmPathToRemove) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $TargetVmPathToRemove, 'soft') -IgnoreExitCode | Out-Null
        Wait-VmStopped -VmPathToCheck $TargetVmPathToRemove
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'deleteVM', $TargetVmPathToRemove) -IgnoreExitCode | Out-Null

    $targetRootToRemove = Split-Path -Parent $TargetVmPathToRemove
    if (Test-Path -LiteralPath $targetRootToRemove) {
        Remove-Item -LiteralPath $targetRootToRemove -Recurse -Force
    }
}

if ([string]::IsNullOrWhiteSpace($SourceVmPath)) {
    $SourceVmPath = Resolve-CanonicalVmPath -VmProfile $SourceVmProfile
}
if ([string]::IsNullOrWhiteSpace($SourceSnapshotName)) {
    $SourceSnapshotName = Resolve-SeedVmSnapshotName -VmProfile $SourceVmProfile
}
if ([string]::IsNullOrWhiteSpace($TargetVmPath)) {
    $TargetVmPath = Resolve-DefaultTargetVmPath -SourcePath $SourceVmPath -TargetName $TargetVmName
}

$resolvedSourceVmPath = [System.IO.Path]::GetFullPath($SourceVmPath)
$resolvedTargetVmPath = [System.IO.Path]::GetFullPath($TargetVmPath)
$resolvedTargetVmDir = Split-Path -Parent $resolvedTargetVmPath
$resolvedTargetVmName = if ([string]::IsNullOrWhiteSpace($TargetVmName)) { Split-Path -Leaf $resolvedTargetVmDir } else { $TargetVmName }

Test-SafeTargetPath -SourcePath $resolvedSourceVmPath -TargetPath $resolvedTargetVmPath -TargetName $resolvedTargetVmName

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = 'planned'
    debug_environment = 'vmware-debug-only'
    vm_role = 'debug_arbiter_only'
    source_vm_profile = $SourceVmProfile
    source_vm_path = "<resolve-from-vm-baselines:$SourceVmProfile>"
    source_snapshot = $SourceSnapshotName
    target_vm_name = $resolvedTargetVmName
    target_vm_path = '<derived-next-to-source-runtime-root>/Win25H2DebugOnly/Win25H2DebugOnly.vmx'
    target_vm_path_resolution = 'derived-from-source-runtime-root'
    debug_snapshot_name = $DebugSnapshotName
    pipe_name = $PipeName
    pipe_endpoint = $PipeEndpoint
    try_no_rx_loss = $TryNoRxLoss
    clone_type = $CloneType
    replace_existing = [bool]$ReplaceExisting
    fresh_provision = $true
    frozen_lane_return_allowed = $false
    storage_preflight = $null
    configure_baseline_ref = $null
    steps = @(
        'storage-preflight',
        'fresh-clone-from-runtime-baseline',
        'guest-ready-check',
        'configure-kernel-debug-baseline',
        'debug-snapshot-created'
    )
}

if ($PlanOnly) {
    if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
        Write-JsonFile -Path $OutputPath -InputObject $result
    }

    $result | ConvertTo-Json -Depth 10
    exit 0
}

$guestCredential = Resolve-RegProbeVmCredential -GuestUser $GuestUser -GuestPassword $GuestPassword -CredentialFilePath $CredentialFilePath
$guestAuthArgs = Get-RegProbeVmrunAuthArguments -Credential $guestCredential

$storagePreflightPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $env:TEMP ("regprobe-vmware-debug-only-storage-{0}.json" -f ([guid]::NewGuid().ToString('N')))
}
else {
    Join-Path (Split-Path -Parent ([System.IO.Path]::GetFullPath($OutputPath))) 'vmware-debug-only-storage-preflight.json'
}

try {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $storageHealthScript -VmPath $resolvedSourceVmPath -OutputPath $storagePreflightPath | Out-Null
    if (Test-Path -LiteralPath $storagePreflightPath) {
        $storagePreflight = Get-Content -LiteralPath $storagePreflightPath -Raw | ConvertFrom-Json
        $result.storage_preflight = [ordered]@{
            status = [string]$storagePreflight.status
            findings = @($storagePreflight.findings)
        }
        if ([string]$storagePreflight.status -eq 'unsafe') {
            $result.status = 'storage-unsafe'
            if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
                Write-JsonFile -Path $OutputPath -InputObject $result
            }
            $result | ConvertTo-Json -Depth 10
            exit 0
        }
    }

    if ((Test-Path -LiteralPath $resolvedTargetVmPath) -and -not $ReplaceExisting) {
        $result.status = 'target-exists-replace-required'
        if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
            Write-JsonFile -Path $OutputPath -InputObject $result
        }
        $result | ConvertTo-Json -Depth 10
        exit 0
    }

    if (Test-Path -LiteralPath $resolvedTargetVmPath) {
        Remove-ExistingTargetVm -TargetVmPathToRemove $resolvedTargetVmPath -SourceVmPathToProtect $resolvedSourceVmPath -TargetName $resolvedTargetVmName
    }

    New-Item -ItemType Directory -Path $resolvedTargetVmDir -Force | Out-Null
    Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        'clone',
        $resolvedSourceVmPath,
        $resolvedTargetVmPath,
        $CloneType,
        "-snapshot=$SourceSnapshotName",
        "-cloneName=$resolvedTargetVmName"
    ) | Out-Null

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $resolvedTargetVmPath, 'gui') -IgnoreExitCode | Out-Null
    $toolsState = Wait-GuestReady -VmPathToCheck $resolvedTargetVmPath
    $result.vmtools_state_after_clone = $toolsState

    $configureBaselinePath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        Join-Path $env:TEMP ("regprobe-vmware-debug-only-configure-{0}.json" -f ([guid]::NewGuid().ToString('N')))
    }
    else {
        Join-Path (Split-Path -Parent ([System.IO.Path]::GetFullPath($OutputPath))) 'vmware-debug-only-configure-kernel-debug-baseline.json'
    }

    $configureArgs = @(
        '-VmPath', $resolvedTargetVmPath,
        '-VmrunPath', $VmrunPath,
        '-GuestUser', $guestCredential.UserName,
        '-CredentialFilePath', $CredentialFilePath,
        '-PipeName', $PipeName,
        '-PipeEndpoint', $PipeEndpoint,
        '-TryNoRxLoss', $TryNoRxLoss,
        '-CreateSnapshotName', $DebugSnapshotName,
        '-OutputPath', $configureBaselinePath
    )
    if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
        $configureArgs += @('-GuestPassword', $GuestPassword)
    }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $configureScript @configureArgs | Out-Null

    $result.configure_baseline_ref = if (Test-Path -LiteralPath $configureBaselinePath) {
        $configureBaselinePath
    }
    else {
        $null
    }
    $result.status = 'provisioned'
}
catch {
    $result.status = 'error'
    $result.error = $_.Exception.Message
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-JsonFile -Path $OutputPath -InputObject $result
}

$result | ConvertTo-Json -Depth 10
