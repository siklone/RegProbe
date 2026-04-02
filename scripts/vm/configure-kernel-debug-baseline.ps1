[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$PipeName = '\\.\pipe\regprobe_debug',
    [int]$DebugPort = 1,
    [int]$BaudRate = 115200,
    [string]$CreateSnapshotName = '',
    [string]$OutputPath = '',
    [switch]$SkipVmxUpdate,
    [switch]$SkipGuestBootConfig
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $PSScriptRoot '_vmrun-common.ps1')
$guestCredential = Resolve-RegProbeVmCredential -GuestUser $GuestUser -GuestPassword $GuestPassword
$guestAuthArgs = Get-RegProbeVmrunAuthArguments -Credential $guestCredential
$resolverPath = Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1'
if (Test-Path -LiteralPath $resolverPath) {
    . $resolverPath
    if ([string]::IsNullOrWhiteSpace($VmPath)) {
        $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile
    }
}

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx'
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$InputObject,
        [int]$Depth = 8
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    return Invoke-RegProbeVmrun -VmrunPath $VmrunPath -Arguments $Arguments -IgnoreExitCode:$IgnoreExitCode
}

function Test-VmRunning {
    $listOutput = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') -IgnoreExitCode
    return ($listOutput -match [regex]::Escape($VmPath))
}

function Set-VmxKeyValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Key,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $lines = if (Test-Path -LiteralPath $Path) {
        [System.Collections.Generic.List[string]]::new()
        (Get-Content -LiteralPath $Path) | ForEach-Object { [void]$lines.Add([string]$_) }
        $lines
    }
    else {
        throw "VMX file not found: $Path"
    }

    $pattern = '^{0}\s*=' -f [regex]::Escape($Key)
    $replacement = '{0} = "{1}"' -f $Key, $Value
    $updated = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $pattern) {
            $lines[$i] = $replacement
            $updated = $true
            break
        }
    }

    if (-not $updated) {
        [void]$lines.Add($replacement)
    }

    Set-Content -LiteralPath $Path -Value $lines -Encoding ASCII
}

function Invoke-GuestCmd {
    param([string]$Command)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws'
    ) + $guestAuthArgs + @(
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\cmd.exe',
        '/c', $Command
    )) | Out-Null
}

$vmxPath = [System.IO.Path]::GetFullPath($VmPath)
$resolvedVmProfile = if ([string]::IsNullOrWhiteSpace($VmProfile)) { 'primary' } else { $VmProfile }
$resolvedSnapshotName = if ([string]::IsNullOrWhiteSpace($CreateSnapshotName)) { $null } else { $CreateSnapshotName }

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_profile = $resolvedVmProfile
    vm_path = $vmxPath
    vm_running = $false
    pipe_name = $PipeName
    debug_port = $DebugPort
    baud_rate = $BaudRate
    vmx_updated = $false
    guest_boot_debug_updated = $false
    snapshot_created = $false
    snapshot_name = $resolvedSnapshotName
    steps = @()
    status = 'staged'
}

try {
    $result.vm_running = Test-VmRunning

    if (-not $SkipVmxUpdate) {
        Set-VmxKeyValue -Path $vmxPath -Key 'serial0.present' -Value 'TRUE'
        Set-VmxKeyValue -Path $vmxPath -Key 'serial0.fileType' -Value 'pipe'
        Set-VmxKeyValue -Path $vmxPath -Key 'serial0.fileName' -Value $PipeName
        Set-VmxKeyValue -Path $vmxPath -Key 'serial0.tryNoRxLoss' -Value 'TRUE'
        Set-VmxKeyValue -Path $vmxPath -Key 'serial0.pipe.endPoint' -Value 'server'
        $result.vmx_updated = $true
        $result.steps += 'vmx-serial-pipe-configured'
    }

    if (-not $SkipGuestBootConfig) {
        if (-not $result.vm_running) {
            $result.steps += 'guest-debug-skipped-vm-not-running'
        }
        else {
            Invoke-GuestCmd -Command 'bcdedit /debug on'
            Invoke-GuestCmd -Command ("bcdedit /dbgsettings serial debugport:{0} baudrate:{1}" -f $DebugPort, $BaudRate)
            $result.guest_boot_debug_updated = $true
            $result.steps += 'guest-bcdedit-debug-enabled'
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($CreateSnapshotName)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'snapshot', $vmxPath, $CreateSnapshotName) | Out-Null
        $result.snapshot_created = $true
        $result.steps += 'snapshot-created'
    }

    $result.status = if ($result.vmx_updated -or $result.guest_boot_debug_updated -or $result.snapshot_created) {
        'configured'
    }
    else {
        'staged'
    }
}
catch {
    $result.status = 'error'
    $result.error = $_.Exception.Message
}

if ($OutputPath) {
    Write-JsonFile -Path $OutputPath -InputObject $result
}

$result | ConvertTo-Json -Depth 8

