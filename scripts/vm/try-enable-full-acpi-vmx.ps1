[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$BaseSnapshotName = '',
    [string]$TargetSnapshotName = 'RegProbe-Baseline-FullACPI-20260330',
    [string]$AuditPath = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = Resolve-CanonicalVmPath }
if ([string]::IsNullOrWhiteSpace($BaseSnapshotName)) { $BaseSnapshotName = Resolve-DefaultVmSnapshotName }

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($AuditPath)) {
    $AuditPath = Join-Path $repoRoot 'registry-research-framework\audit\regprobe-full-acpi-vmx-20260330.json'
}
$auditDir = Split-Path -Parent $AuditPath
$beforePowercfgPath = Join-Path $auditDir 'regprobe-full-acpi-vmx-20260330.before.txt'
$afterPowercfgPath = Join-Path $auditDir 'regprobe-full-acpi-vmx-20260330.after.txt'

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)
    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }
    return $output.Trim()
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 420)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
            if ($running -notmatch [regex]::Escape($VmPath)) { Start-Sleep -Seconds 3; continue }
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') { return }
        }
        catch {}
        Start-Sleep -Seconds 3
    }
    throw 'Guest did not reach a ready tools state in time.'
}

function Wait-GuestCommandReady {
    param([int]$TimeoutSeconds = 180)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'runProgramInGuest', $VmPath,
                'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
                '-NoProfile',
                '-ExecutionPolicy',
                'Bypass',
                '-Command',
                'exit 0'
            ) | Out-Null
            return
        }
        catch {}
        Start-Sleep -Seconds 3
    }
    throw 'Guest command execution did not become ready in time.'
}

function Get-PowercfgAOutput {
    $hostPath = Join-Path ([System.IO.Path]::GetTempPath()) ("regprobe-acpi-" + [guid]::NewGuid().ToString() + '.txt')
    $guestPath = 'C:\RegProbe-Diag\powercfg-a.txt'
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-Command',
        "New-Item -ItemType Directory -Path 'C:\RegProbe-Diag' -Force | Out-Null; powercfg /a | Out-File -FilePath '$guestPath' -Encoding utf8"
    ) | Out-Null
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromGuestToHost', $VmPath, $guestPath, $hostPath
    ) | Out-Null
    $text = Get-Content -Raw $hostPath
    Remove-Item -Force $hostPath -ErrorAction SilentlyContinue
    return $text
}

function Test-AvailableSleepState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PowercfgText,
        [Parameter(Mandatory = $true)]
        [string]$StateLabel
    )
    $parts = $PowercfgText -split 'The following sleep states are not available on this system:'
    $availableBlock = if ($parts.Count -gt 0) { $parts[0] } else { $PowercfgText }
    return [bool]($availableBlock -match ("(?m)^\\s+" + [regex]::Escape($StateLabel) + "\\s*$"))
}

function Set-VmxKeyValue {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$Lines,
        [Parameter(Mandatory = $true)]
        [string]$Key,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )
    $pattern = '^\s*' + [regex]::Escape($Key) + '\s*='
    $replacement = "$Key = `"$Value`""
    $updated = $false
    $sourceLines = @()
    foreach ($entry in @($Lines)) {
        if ($null -eq $entry) { continue }
        $entryText = [string]$entry
        if ([string]::IsNullOrEmpty($entryText)) {
            $sourceLines += ''
            continue
        }
        $sourceLines += ($entryText -split "`r?`n")
    }
    $output = foreach ($line in $sourceLines) {
        if (($line -as [string]) -match $pattern) {
            $updated = $true
            $replacement
        }
        else {
            [string]$line
        }
    }
    if (-not $updated) { $output += $replacement }
    return @($output)
}

function Get-SnapshotNames {
    $raw = Invoke-Vmrun -Arguments @('-T', 'ws', 'listSnapshots', $VmPath)
    return @(
        $raw -split "`r?`n" |
        Where-Object { $_ -and $_ -notmatch '^Total snapshots:' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
    )
}

$vmxPath = $VmPath
$audit = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    base_snapshot = $BaseSnapshotName
    target_snapshot = $TargetSnapshotName
    vmx_path = $vmxPath
    vmx_changes = [ordered]@{}
    before = [ordered]@{
        powercfg_a_path = $beforePowercfgPath
    }
    after = [ordered]@{
        powercfg_a_path = $afterPowercfgPath
    }
    status = 'started'
    errors = @()
}

try {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $BaseSnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-GuestCommandReady
    $beforePowercfg = Get-PowercfgAOutput
    Set-Content -Path $beforePowercfgPath -Value $beforePowercfg -Encoding UTF8

    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'soft') -IgnoreExitCode | Out-Null
    Start-Sleep -Seconds 10

    $vmxLines = Get-Content -Path $vmxPath
    $desired = [ordered]@{
        'firmware' = 'efi'
        'monitor.virtual_exec' = 'hardware'
        'isolation.tools.hibernate.disable' = 'FALSE'
        'acpi.smbiosVersion' = '2.4'
        'gui.runVMWorkstation' = 'TRUE'
    }

    foreach ($key in $desired.Keys) {
        $currentLine = @($vmxLines | Where-Object { $_ -match ('^\s*' + [regex]::Escape($key) + '\s*=') } | Select-Object -Last 1)
        $audit.vmx_changes[$key] = [ordered]@{
            before = if ($currentLine) { ($currentLine -join '') } else { $null }
            after = "$key = `"$($desired[$key])`""
        }
        $vmxLines = Set-VmxKeyValue -Lines $vmxLines -Key $key -Value $desired[$key]
    }

    Set-Content -Path $vmxPath -Value $vmxLines -Encoding UTF8

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-GuestCommandReady
    $afterPowercfg = Get-PowercfgAOutput
    Set-Content -Path $afterPowercfgPath -Value $afterPowercfg -Encoding UTF8
    $audit.after.hibernate_visible = Test-AvailableSleepState -PowercfgText $afterPowercfg -StateLabel 'Hibernate'
    $audit.after.s3_visible = Test-AvailableSleepState -PowercfgText $afterPowercfg -StateLabel 'Standby (S3)'

    if ($audit.after.hibernate_visible -or $audit.after.s3_visible) {
        $snapshots = Get-SnapshotNames
        if ($snapshots -contains $TargetSnapshotName) {
            Invoke-Vmrun -Arguments @('-T', 'ws', 'deleteSnapshot', $VmPath, $TargetSnapshotName) | Out-Null
        }
        Invoke-Vmrun -Arguments @('-T', 'ws', 'snapshot', $VmPath, $TargetSnapshotName) | Out-Null
        $audit.after.snapshot_created = $true
    }
    else {
        $audit.after.snapshot_created = $false
    }

    $audit.status = 'ok'
}
catch {
    $audit.status = 'error'
    $audit.errors += $_.Exception.Message
}
finally {
    $audit | ConvertTo-Json -Depth 8 | Set-Content -Path $AuditPath -Encoding UTF8
}

Write-Output $AuditPath
