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
. (Join-Path $PSScriptRoot '_lane-manifest-lib.ps1')

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
$commandScriptRef = Get-RepoDisplayPath -Path $commandScriptPath
$portableCommandScript = ('<REPO_ROOT>\{0}' -f ($commandScriptRef -replace '/', '\'))
$logPath = 'C:\RegProbe-Diag\windbg-registry-trace.log'
$generatorOutput = & $generatorScript -KeyNames $Keys -OutputPath $commandScriptPath -LogPath $logPath | ConvertFrom-Json

function New-ArtifactRef {
    param([string]$Path)

    $display = Get-RepoDisplayPath -Path $Path
    $full = [System.IO.Path]::GetFullPath($Path)
    $item = if (Test-Path -LiteralPath $full) { Get-Item -LiteralPath $full } else { $null }

    return [ordered]@{
        path = $display
        sha256 = if ($item) { (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
        size = if ($item) { [int64]$item.Length } else { $null }
        collected_utc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        exists = [bool]$item
    }
}

function Resolve-DebuggerCandidate {
    param([string[]]$CandidatePaths)

    foreach ($candidate in $CandidatePaths) {
        if ($candidate.Contains('*')) {
            $matches = Get-ChildItem -Path $candidate -ErrorAction SilentlyContinue | Sort-Object FullName -Descending
            foreach ($match in $matches) {
                if ($match.PSIsContainer) {
                    foreach ($leaf in @('windbg.exe', 'kd.exe', 'cdb.exe')) {
                        $leafPath = Join-Path $match.FullName $leaf
                        if (Test-Path -LiteralPath $leafPath) {
                            return $leafPath
                        }
                    }
                }
                elseif (Test-Path -LiteralPath $match.FullName) {
                    return $match.FullName
                }
            }
            continue
        }

        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

$commonWinDbgPaths = @(
    'C:\Program Files\WindowsApps\Microsoft.WinDbg_*',
    'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\windbg.exe',
    'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\kd.exe',
    'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe',
    'C:\Program Files\WindowsApps\Microsoft.WinDbg_1.0.0.0_x64__8wekyb3d8bbwe\windbg.exe'
)
$resolvedWinDbg = Resolve-DebuggerCandidate -CandidatePaths $commonWinDbgPaths

$baselinePrep = $null
$baselinePrepPath = Join-Path $outputRoot 'configure-kernel-debug-baseline.json'
if ($PrepareBaseline) {
    $prepArgs = @('-VmPath', $VmPath, '-VmrunPath', $VmrunPath, '-PipeName', $PipeName, '-CreateSnapshotName', $DebugSnapshotName, '-OutputPath', $baselinePrepPath)
    if (-not [string]::IsNullOrWhiteSpace($VmProfile)) {
        $prepArgs += @('-VmProfile', $VmProfile)
    }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $configureScript @prepArgs | Out-Null
    if (Test-Path -LiteralPath $baselinePrepPath) {
        $baselinePrep = Get-Content -LiteralPath $baselinePrepPath -Raw | ConvertFrom-Json
    }
}

$resolvedVmProfile = if ([string]::IsNullOrWhiteSpace($VmProfile)) { 'primary' } else { $VmProfile }
$resolvedWindbgCommand = if ($resolvedWinDbg) {
    ('"{0}" -k com:pipe,port={1},resets=0,reconnect -c "$$< {2}"' -f $resolvedWinDbg, $PipeName, $portableCommandScript)
}
else {
    ('windbg -k com:pipe,port={0},resets=0,reconnect -c "$$< {1}"' -f $PipeName, $portableCommandScript)
}
$baselinePrepStatus = if ($PrepareBaseline) {
    if (-not $baselinePrep) { 'missing' }
    else { [string]$baselinePrep.status }
}
else {
    'not-requested'
}
$resolvedStatus = if ($PrepareBaseline -and $baselinePrepStatus -eq 'error') {
    'baseline-prepare-failed'
}
elseif ($resolvedWinDbg) {
    'ready-to-attach'
}
else {
    'blocked-windbg-missing'
}
$supportArtifacts = @(
    (New-ArtifactRef -Path $commandScriptPath)
)
if (Test-Path -LiteralPath $baselinePrepPath) {
    $supportArtifacts += @(New-ArtifactRef -Path $baselinePrepPath)
}
$captureStatus = if ($resolvedStatus -eq 'baseline-prepare-failed') { 'missing-capture' } else { 'staged' }

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    tool = 'WinDbg'
    vm_profile = $resolvedVmProfile
    vm_path = $VmPath
    pipe_name = $PipeName
    debug_snapshot_name = $DebugSnapshotName
    collection_mode = $CollectionMode
    rollback_pending = ($CollectionMode -eq 'evidence')
    runner_required = $true
    key_count = @($Keys).Count
    keys = @($Keys | Sort-Object -Unique)
    command_script = $commandScriptRef
    log_path = $logPath
    attach_mode = 'manual-kernel-debug'
    breakpoint_mode = $generatorOutput.mode
    windbg_path = $resolvedWinDbg
    windbg_command = $resolvedWindbgCommand
    status = $resolvedStatus
    capture_status = $captureStatus
    capture_artifacts = @()
    support_artifacts = $supportArtifacts
    host_debugger_status = if ($resolvedWinDbg) { 'installed' } else { 'missing' }
    host_debugger_candidates = @($commonWinDbgPaths)
    baseline_prepare_status = $baselinePrepStatus
    prep = $baselinePrep
    notes = @(
        'Use this lane only after ETW mega-trigger leaves a no-hit hold queue.',
        'This command script logs all CmpQueryValueKey reads; post-filter the resulting log for the requested key names.',
        'WinDbg acts as the dead-flag final arbiter: ETW no-hit + WinDbg no-hit is the strongest dead-flag signal.'
    )
}

$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputFile -Encoding UTF8
$OutputFile
