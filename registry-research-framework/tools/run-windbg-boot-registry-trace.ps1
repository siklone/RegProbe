[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputFile,
    [string[]]$Keys = @(),
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$PipeName = '\\.\pipe\regprobe_debug',
    [string]$DebugSnapshotName = 'RegProbe-Baseline-Debug-20260402',
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',
    [switch]$PrepareBaseline
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$resolverPath = Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1'
$configureScript = Join-Path $repoRoot 'scripts\vm\configure-kernel-debug-baseline.ps1'
$generatorScript = Join-Path $repoRoot 'scripts\vm\new-windbg-registry-watch-script.ps1'

if (Test-Path -LiteralPath $resolverPath) {
    . $resolverPath
    if ([string]::IsNullOrWhiteSpace($VmPath)) {
        $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile
    }
}

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx'
}

function Get-RepoDisplayPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $full = [System.IO.Path]::GetFullPath($Path)
    if ($full.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($repoRoot.Length).TrimStart('\').Replace('\', '/')
    }

    return $full
}

$outputRoot = Split-Path -Parent ([System.IO.Path]::GetFullPath($OutputFile))
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$commandScriptPath = Join-Path $outputRoot 'windbg-registry-watch.txt'
$logPath = 'C:\RegProbe-Diag\windbg-registry-trace.log'
$generatorOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $generatorScript -KeyNames $Keys -OutputPath $commandScriptPath -LogPath $logPath | ConvertFrom-Json

$commonWinDbgPaths = @(
    'C:\Program Files\WindowsApps\Microsoft.WinDbg_*',
    'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\windbg.exe',
    'C:\Program Files\WindowsApps\Microsoft.WinDbg_1.0.0.0_x64__8wekyb3d8bbwe\windbg.exe'
)
$resolvedWinDbg = $null
foreach ($candidate in $commonWinDbgPaths) {
    if ($candidate.Contains('*')) {
        $match = Get-ChildItem -Path $candidate -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
        if ($match) {
            $windbgPath = Join-Path $match.FullName 'windbg.exe'
            if (Test-Path -LiteralPath $windbgPath) {
                $resolvedWinDbg = $windbgPath
                break
            }
        }
    }
    elseif (Test-Path -LiteralPath $candidate) {
        $resolvedWinDbg = $candidate
        break
    }
}

$baselinePrep = $null
if ($PrepareBaseline) {
    $prepPath = Join-Path $outputRoot 'configure-kernel-debug-baseline.json'
    $prepArgs = @('-VmPath', $VmPath, '-VmrunPath', $VmrunPath, '-PipeName', $PipeName, '-CreateSnapshotName', $DebugSnapshotName, '-OutputPath', $prepPath)
    if (-not [string]::IsNullOrWhiteSpace($VmProfile)) {
        $prepArgs += @('-VmProfile', $VmProfile)
    }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $configureScript @prepArgs | Out-Null
    if (Test-Path -LiteralPath $prepPath) {
        $baselinePrep = Get-Content -LiteralPath $prepPath -Raw | ConvertFrom-Json
    }
}

$resolvedVmProfile = if ([string]::IsNullOrWhiteSpace($VmProfile)) { 'primary' } else { $VmProfile }
$resolvedWindbgCommand = if ($resolvedWinDbg) {
    ('"{0}" -k com:pipe,port={1},resets=0,reconnect -c "$$< {2}"' -f $resolvedWinDbg, $PipeName, $commandScriptPath)
}
else {
    ('windbg -k com:pipe,port={0},resets=0,reconnect -c "$$< {1}"' -f $PipeName, $commandScriptPath)
}
$resolvedStatus = if ($resolvedWinDbg) { 'ready-to-attach' } else { 'blocked-windbg-missing' }

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    tool = 'WinDbg'
    vm_profile = $resolvedVmProfile
    vm_path = $VmPath
    pipe_name = $PipeName
    debug_snapshot_name = $DebugSnapshotName
    collection_mode = $CollectionMode
    rollback_pending = ($CollectionMode -eq 'evidence')
    key_count = @($Keys).Count
    keys = @($Keys | Sort-Object -Unique)
    command_script = Get-RepoDisplayPath -Path $commandScriptPath
    log_path = $logPath
    attach_mode = 'manual-kernel-debug'
    breakpoint_mode = $generatorOutput.mode
    windbg_path = $resolvedWinDbg
    windbg_command = $resolvedWindbgCommand
    status = $resolvedStatus
    prep = $baselinePrep
    notes = @(
        'Use this lane only after ETW mega-trigger leaves a no-hit hold queue.',
        'This command script logs all CmpQueryValueKey reads; post-filter the resulting log for the requested key names.',
        'WinDbg acts as the dead-flag final arbiter: ETW no-hit + WinDbg no-hit is the strongest dead-flag signal.'
    )
}

$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputFile -Encoding UTF8
$OutputFile
