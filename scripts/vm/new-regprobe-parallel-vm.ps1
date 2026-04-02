[CmdletBinding()]
param(
    [string]$SourceVmProfile = 'primary',
    [string]$TargetVmProfile = 'secondary',
    [string]$SourceVmPath = '',
    [string]$TargetVmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$SourceSnapshotName = '',
    [string]$TargetSnapshotName = '',
    [ValidateSet('full', 'linked')]
    [string]$CloneType = 'full',
    [switch]$ReplaceExisting,
    [switch]$RunAppSmoke,
    [string]$AuditLabel = 'parallel-vm-bootstrap-20260331'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

$sourceProfileTag = Resolve-VmProfileTag -VmProfile $SourceVmProfile
$targetProfileTag = Resolve-VmProfileTag -VmProfile $TargetVmProfile
$sourceConfig = Get-VmProfileConfig -VmProfile $SourceVmProfile
$targetConfig = Get-VmProfileConfig -VmProfile $TargetVmProfile

if ([string]::IsNullOrWhiteSpace($SourceVmPath)) {
    $SourceVmPath = Resolve-CanonicalVmPath -VmProfile $SourceVmProfile
}

if ([string]::IsNullOrWhiteSpace($TargetVmPath)) {
    $TargetVmPath = Resolve-CanonicalVmPath -VmProfile $TargetVmProfile
}

if ([string]::IsNullOrWhiteSpace($SourceSnapshotName)) {
    $SourceSnapshotName = Resolve-SeedVmSnapshotName -VmProfile $SourceVmProfile
}

if ([string]::IsNullOrWhiteSpace($TargetSnapshotName)) {
    $TargetSnapshotName = Resolve-DefaultVmSnapshotName -VmProfile $TargetVmProfile
}

$sourceVmName = Resolve-CanonicalVmName -VmProfile $SourceVmProfile
$targetVmName = Resolve-CanonicalVmName -VmProfile $TargetVmProfile
$targetVmRoot = Split-Path -Parent $TargetVmPath
$targetVmxName = Split-Path -Leaf $TargetVmPath
$targetHostStagingRoot = Resolve-HostStagingRoot -VmProfile $TargetVmProfile
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$auditRoot = Join-Path $repoRoot 'registry-research-framework\audit'
$auditSessionRoot = Join-Path $auditRoot $AuditLabel
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostAuditRoot = Join-Path $auditSessionRoot ("{0}-bootstrap-{1}" -f $targetProfileTag, $stamp)
$cloneAuditPath = Join-Path $auditRoot ("regprobe-parallel-vm-{0}.json" -f $targetProfileTag)
$publicAuditSessionRoot = "<REPO_ROOT>\\registry-research-framework\\audit\\{0}\\{1}" -f $AuditLabel, (Split-Path -Leaf $hostAuditRoot)
$appSmokeScript = Join-Path $PSScriptRoot 'run-app-launch-smoke-host.ps1'
$validationGuestRoot = Resolve-GuestDiagRoot -VmProfile $TargetVmProfile
$validationHostScriptPath = Join-Path $hostAuditRoot 'secondary-validation-payload.ps1'
$validationGuestScriptPath = Join-Path $validationGuestRoot 'secondary-validation-payload.ps1'
$validationGuestWriteTestPath = Join-Path $validationGuestRoot 'write-test.txt'
$validationGuestEnvironmentPath = Join-Path $validationGuestRoot 'environment.json'
$validationGuestResultPath = Join-Path $validationGuestRoot 'script-result.json'
$validationHostWriteTestPath = Join-Path $hostAuditRoot 'write-test.txt'
$validationHostEnvironmentPath = Join-Path $hostAuditRoot 'environment.json'
$validationHostResultPath = Join-Path $hostAuditRoot 'script-result.json'
$validationHostSummaryPath = Join-Path $hostAuditRoot 'summary.json'
$publicValidationWriteTestPath = "$publicAuditSessionRoot\\write-test.txt"
$publicValidationEnvironmentPath = "$publicAuditSessionRoot\\environment.json"
$publicValidationResultPath = "$publicAuditSessionRoot\\script-result.json"
$publicValidationSummaryPath = "$publicAuditSessionRoot\\summary.json"

New-Item -ItemType Directory -Path $auditRoot -Force | Out-Null
New-Item -ItemType Directory -Path $auditSessionRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostAuditRoot -Force | Out-Null
New-Item -ItemType Directory -Path $targetHostStagingRoot -Force | Out-Null

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

function Get-RunningVmList {
    $raw = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
    return @(
        $raw -split "`r?`n" |
        Where-Object { $_ -and $_ -notmatch '^Total running VMs:' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
    )
}

function Test-VmRunning {
    param([string]$VmPathToCheck)

    return (Get-RunningVmList) -contains $VmPathToCheck
}

function Wait-VmStopped {
    param(
        [string]$VmPathToCheck,
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
        [string]$VmPathToCheck,
        [int]$TimeoutSeconds = 600
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPathToCheck)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    throw "Guest did not reach a ready VMware Tools state in time: $VmPathToCheck"
}

function Get-SnapshotNames {
    param([string]$VmPathToCheck)

    $raw = Invoke-Vmrun -Arguments @('-T', 'ws', 'listSnapshots', $VmPathToCheck) -IgnoreExitCode
    if ($LASTEXITCODE -ne 0) {
        return @()
    }

    return @(
        $raw -split "`r?`n" |
        Where-Object { $_ -and $_ -notmatch '^Total snapshots:' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
    )
}

function Remove-ExistingTargetVm {
    param([string]$TargetVmPathToRemove)

    if (-not (Test-Path $TargetVmPathToRemove)) {
        return $false
    }

    if (Test-VmRunning -VmPathToCheck $TargetVmPathToRemove) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $TargetVmPathToRemove, 'soft') -IgnoreExitCode | Out-Null
        Wait-VmStopped -VmPathToCheck $TargetVmPathToRemove
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'deleteVM', $TargetVmPathToRemove) -IgnoreExitCode | Out-Null

    $targetRootToRemove = Split-Path -Parent $TargetVmPathToRemove
    if (Test-Path $targetRootToRemove) {
        Remove-Item -Path $targetRootToRemove -Recurse -Force
    }

    return $true
}

function Ensure-GuestDirectory {
    param([string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'createDirectoryInGuest', $TargetVmPath, $GuestPath
    ) -IgnoreExitCode | Out-Null
}

function Copy-ToGuest {
    param(
        [string]$HostPath,
        [string]$GuestPath
    )

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $TargetVmPath, $HostPath, $GuestPath
    ) | Out-Null
}

function Copy-FromGuest {
    param(
        [string]$GuestPath,
        [string]$HostPath
    )

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromGuestToHost', $TargetVmPath, $GuestPath, $HostPath
    ) | Out-Null
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $TargetVmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

$audit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    source = [ordered]@{
        profile = $sourceProfileTag
        vm_name = $sourceVmName
        vm_path = [string]$sourceConfig.vm_path
        snapshot = $SourceSnapshotName
    }
    target = [ordered]@{
        profile = $targetProfileTag
        vm_name = $targetVmName
        vm_path = [string]$targetConfig.vm_path
        snapshot = $TargetSnapshotName
        host_staging_root = [string]$targetConfig.host_staging_root
    }
    clone_type = $CloneType
    target_exists_before = [bool](Test-Path $TargetVmPath)
    replaced_existing = $false
    clone_performed = $false
    source_running_at_start = $false
    source_restarted_after_clone = $false
    validation = [ordered]@{
        tooling_diagnostic_summary = $null
        write_test_path = $null
        environment_path = $null
        script_result_path = $null
        app_smoke_output = $null
    }
    clone_strategy = $null
    status = 'started'
    errors = @()
}

$sourceRunningAtStart = $false
$sourceStoppedForClone = $false

try {
    $sourceRunningAtStart = Test-VmRunning -VmPathToCheck $SourceVmPath
    $audit.source_running_at_start = $sourceRunningAtStart

    if ((Test-Path $TargetVmPath) -and -not $ReplaceExisting) {
        Write-Host "Target VM already exists at $TargetVmPath; reusing it."
    }
    elseif (Test-Path $TargetVmPath) {
        $audit.replaced_existing = Remove-ExistingTargetVm -TargetVmPathToRemove $TargetVmPath
    }

    if (-not (Test-Path $TargetVmPath)) {
        New-Item -ItemType Directory -Path $targetVmRoot -Force | Out-Null

        try {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws',
                'clone',
                $SourceVmPath,
                $TargetVmPath,
                $CloneType,
                "-snapshot=$SourceSnapshotName",
                "-cloneName=$targetVmName"
            ) | Out-Null
            $audit.clone_strategy = 'snapshot-clone'
        }
        catch {
            if ($sourceRunningAtStart) {
                Write-Host "Clone failed while source VM was running; retrying after a soft stop."
                Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $SourceVmPath, 'soft') -IgnoreExitCode | Out-Null
                Wait-VmStopped -VmPathToCheck $SourceVmPath
                $sourceStoppedForClone = $true
                Start-Sleep -Seconds 8

                $cloneSucceeded = $false
                for ($attempt = 1; $attempt -le 3 -and -not $cloneSucceeded; $attempt++) {
                    try {
                        Invoke-Vmrun -Arguments @(
                            '-T', 'ws',
                            'clone',
                            $SourceVmPath,
                            $TargetVmPath,
                            $CloneType,
                            "-snapshot=$SourceSnapshotName",
                            "-cloneName=$targetVmName"
                        ) | Out-Null
                        $audit.clone_strategy = 'snapshot-clone-after-stop'
                        $cloneSucceeded = $true
                    }
                    catch {
                        if ($attempt -ge 3) {
                            $cloneSucceeded = $false
                        }
                        else {
                            Start-Sleep -Seconds 5
                        }
                    }
                }

                if ($cloneSucceeded) {
                    $audit.clone_performed = $true
                }
            }

            if (-not (Test-Path $TargetVmPath)) {
                Write-Host "Snapshot-based clone stayed brittle; falling back to a current-state full clone from the powered-off source."
                try {
                    Invoke-Vmrun -Arguments @(
                        '-T', 'ws',
                        'clone',
                        $SourceVmPath,
                        $TargetVmPath,
                        $CloneType,
                        "-cloneName=$targetVmName"
                    ) | Out-Null
                    $audit.clone_strategy = 'current-state-clone'
                }
                catch {
                    throw
                }
            }
        }

        $audit.clone_performed = $true
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $TargetVmPath, 'gui') -IgnoreExitCode | Out-Null
    Wait-GuestReady -VmPathToCheck $TargetVmPath

    $targetSnapshots = Get-SnapshotNames -VmPathToCheck $TargetVmPath
    if ($targetSnapshots -notcontains $TargetSnapshotName) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $TargetVmPath, 'soft') -IgnoreExitCode | Out-Null
        Wait-VmStopped -VmPathToCheck $TargetVmPath
        Invoke-Vmrun -Arguments @('-T', 'ws', 'snapshot', $TargetVmPath, $TargetSnapshotName) | Out-Null
    }

    @'
param(
    [Parameter(Mandatory = $true)]
    [string]$WriteTestPath,
    [Parameter(Mandatory = $true)]
    [string]$EnvironmentPath,
    [Parameter(Mandatory = $true)]
    [string]$ScriptResultPath
)

$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $WriteTestPath
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

"SECONDARY_VM_OK $([DateTime]::UtcNow.ToString('o'))" | Set-Content -Path $WriteTestPath -Encoding UTF8

$environment = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    write_test_exists = [bool](Test-Path $WriteTestPath)
    execution_policy = (Get-ExecutionPolicy).ToString()
    ps_version = $PSVersionTable.PSVersion.ToString()
    user_is_administrator = ([bool]([System.Security.Principal.WindowsPrincipal] [System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator))
    procmon_present = [bool](Test-Path 'C:\Tools\Sysinternals\Procmon64.exe')
    defender_disable_realtime = (Get-MpPreference).DisableRealtimeMonitoring
}
$environment | ConvertTo-Json -Depth 6 | Set-Content -Path $EnvironmentPath -Encoding UTF8

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = 'ok'
    write_test_path = $WriteTestPath
    environment_path = $EnvironmentPath
} | ConvertTo-Json -Depth 6 | Set-Content -Path $ScriptResultPath -Encoding UTF8
'@ | Set-Content -Path $validationHostScriptPath -Encoding UTF8

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $TargetVmPath, 'gui') -IgnoreExitCode | Out-Null
    Wait-GuestReady -VmPathToCheck $TargetVmPath
    Ensure-GuestDirectory -GuestPath $validationGuestRoot
    Copy-ToGuest -HostPath $validationHostScriptPath -GuestPath $validationGuestScriptPath
    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $validationGuestScriptPath,
        '-WriteTestPath', $validationGuestWriteTestPath,
        '-EnvironmentPath', $validationGuestEnvironmentPath,
        '-ScriptResultPath', $validationGuestResultPath
    )

    Copy-FromGuest -GuestPath $validationGuestWriteTestPath -HostPath $validationHostWriteTestPath
    Copy-FromGuest -GuestPath $validationGuestEnvironmentPath -HostPath $validationHostEnvironmentPath
    Copy-FromGuest -GuestPath $validationGuestResultPath -HostPath $validationHostResultPath

    $validationSummary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        profile = $targetProfileTag
        vm_name = $targetVmName
        snapshot_name = $TargetSnapshotName
        write_test_path = $publicValidationWriteTestPath
        environment_path = $publicValidationEnvironmentPath
        script_result_path = $publicValidationResultPath
        script_result = (Get-Content -Path $validationHostResultPath -Raw | ConvertFrom-Json)
        environment = (Get-Content -Path $validationHostEnvironmentPath -Raw | ConvertFrom-Json)
        status = 'ok'
    }
    $validationSummary | ConvertTo-Json -Depth 8 | Set-Content -Path $validationHostSummaryPath -Encoding UTF8

    $audit.validation.tooling_diagnostic_summary = $publicValidationSummaryPath
    $audit.validation.write_test_path = $publicValidationWriteTestPath
    $audit.validation.environment_path = $publicValidationEnvironmentPath
    $audit.validation.script_result_path = $publicValidationResultPath

    if ($RunAppSmoke) {
        $appSmokeOutputPath = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $appSmokeScript `
            -VmProfile $TargetVmProfile `
            -VmPath $TargetVmPath `
            -RefreshPackage:$false).Trim()
        $audit.validation.app_smoke_output = $appSmokeOutputPath
    }

    if ($sourceRunningAtStart -and $sourceStoppedForClone -and -not (Test-VmRunning -VmPathToCheck $SourceVmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $SourceVmPath, 'gui') -IgnoreExitCode | Out-Null
        $audit.source_restarted_after_clone = $true
    }

    $audit.status = 'ok'
}
catch {
    $audit.status = 'error'
    $audit.errors += $_.Exception.Message
}
finally {
    if ($sourceRunningAtStart -and $sourceStoppedForClone -and -not (Test-VmRunning -VmPathToCheck $SourceVmPath)) {
        try {
            Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $SourceVmPath, 'gui') -IgnoreExitCode | Out-Null
            $audit.source_restarted_after_clone = $true
        }
        catch {
            $audit.errors += "Failed to restart source VM after clone attempt: $($_.Exception.Message)"
        }
    }
}

$audit | ConvertTo-Json -Depth 8 | Set-Content -Path $cloneAuditPath -Encoding UTF8
Get-Content -Path $cloneAuditPath

