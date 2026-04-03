[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputFile,
    [string[]]$Keys = @(),
    [string]$TargetKey = '',
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$CredentialFilePath = '',
    [string]$PipeName = '\\.\pipe\regprobe_debug',
    [ValidateSet('auto', 'kd', 'cdb', 'windbg')]
    [string]$DebuggerFrontend = 'auto',
    [string]$DebugSnapshotName = 'RegProbe-Baseline-Debug-20260402',
    [ValidateSet('evidence', 'operational')]
    [string]$CollectionMode = 'evidence',
    [ValidateSet('multi-postfilter', 'minimal', 'symbols', 'attach-only', 'breakin-once', 'breakin-twice', 'breakin-delayed-10', 'breakin-delayed-30', 'singlekey-smoke', 'singlekey-firsthit', 'singlekey-rawbounded')]
    [string]$TraceProfile = 'multi-postfilter',
    [ValidateSet('guest-restart', 'cold-boot', 'attach-after-shell')]
    [string]$BootMode = '',
    [int]$NoiseBudgetBytes = 262144,
    [int]$RawHitLimit = 100,
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
$resolvedKeys = @($Keys | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
if (-not [string]::IsNullOrWhiteSpace($TargetKey)) {
    $resolvedKeys = @($TargetKey)
}
if ($TraceProfile -like 'singlekey-*' -and $resolvedKeys.Count -ne 1) {
    throw "TraceProfile '$TraceProfile' requires exactly one target key."
}
$resolvedTargetKey = if ($resolvedKeys.Count -eq 1) { [string]$resolvedKeys[0] } else { $null }
$resolvedBootMode = if (-not [string]::IsNullOrWhiteSpace($BootMode)) {
    $BootMode
}
elseif ($TraceProfile -like 'singlekey-*') {
    'cold-boot'
}
else {
    'guest-restart'
}
$generatorArgs = @{
    OutputPath = $commandScriptPath
    LogPath = $logPath
    TraceProfile = $TraceProfile
    RawHitLimit = $RawHitLimit
}
if ($resolvedTargetKey) {
    $generatorArgs.TargetKey = $resolvedTargetKey
}
elseif ($resolvedKeys.Count -gt 0) {
    $generatorArgs.KeyNames = $resolvedKeys
}
$generatorOutput = & $generatorScript @generatorArgs | ConvertFrom-Json

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
    param(
        [string[]]$CandidatePaths,
        [string]$PreferredFrontend = 'auto'
    )

    $leafOrder = switch ($PreferredFrontend) {
        'kd' { @('kd.exe', 'cdb.exe', 'windbg.exe') }
        'cdb' { @('cdb.exe', 'kd.exe', 'windbg.exe') }
        'windbg' { @('windbg.exe', 'kd.exe', 'cdb.exe') }
        default { @('windbg.exe', 'kd.exe', 'cdb.exe') }
    }

    foreach ($candidate in $CandidatePaths) {
        if ($candidate.Contains('*')) {
            $matches = Get-ChildItem -Path $candidate -ErrorAction SilentlyContinue | Sort-Object FullName -Descending
            foreach ($match in $matches) {
                if ($match.PSIsContainer) {
                    foreach ($searchRoot in @(
                            $match.FullName,
                            (Join-Path $match.FullName 'amd64'),
                            (Join-Path $match.FullName 'x64'),
                            (Join-Path $match.FullName 'x86'),
                            (Join-Path $match.FullName 'arm64')
                        )) {
                        foreach ($leaf in $leafOrder) {
                            $leafPath = Join-Path $searchRoot $leaf
                            if (Test-Path -LiteralPath $leafPath) {
                                return $leafPath
                            }
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
$resolvedWinDbg = Resolve-DebuggerCandidate -CandidatePaths $commonWinDbgPaths -PreferredFrontend $DebuggerFrontend

$baselinePrep = $null
$baselinePrepPath = Join-Path $outputRoot 'configure-kernel-debug-baseline.json'
if ($PrepareBaseline) {
    $prepArgs = @('-VmPath', $VmPath, '-VmrunPath', $VmrunPath, '-GuestUser', $GuestUser, '-PipeName', $PipeName, '-CreateSnapshotName', $DebugSnapshotName, '-OutputPath', $baselinePrepPath)
    if (-not [string]::IsNullOrWhiteSpace($VmProfile)) {
        $prepArgs += @('-VmProfile', $VmProfile)
    }
    if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
        $prepArgs += @('-GuestPassword', $GuestPassword)
    }
    if (-not [string]::IsNullOrWhiteSpace($CredentialFilePath)) {
        $prepArgs += @('-CredentialFilePath', $CredentialFilePath)
    }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $configureScript @prepArgs | Out-Null
    if (Test-Path -LiteralPath $baselinePrepPath) {
        $baselinePrep = Get-Content -LiteralPath $baselinePrepPath -Raw | ConvertFrom-Json
    }
}

$resolvedVmProfile = if ([string]::IsNullOrWhiteSpace($VmProfile)) { 'primary' } else { $VmProfile }
$resolvedWindbgCommand = if ($resolvedWinDbg) {
    ('"{0}" -k com:pipe,port={1},resets=0,reconnect -cfr {2}' -f $resolvedWinDbg, $PipeName, $portableCommandScript)
}
else {
    ('windbg -k com:pipe,port={0},resets=0,reconnect -cfr {1}' -f $PipeName, $portableCommandScript)
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
    key_count = @($resolvedKeys).Count
    keys = @($resolvedKeys)
    target_key = $resolvedTargetKey
    trace_profile = $TraceProfile
    boot_mode = $resolvedBootMode
    noise_budget_bytes = $NoiseBudgetBytes
    raw_hit_limit = $RawHitLimit
    command_script = $commandScriptRef
    log_path = $logPath
    attach_mode = 'manual-kernel-debug'
    debugger_frontend = if ($resolvedWinDbg) { [IO.Path]::GetFileNameWithoutExtension($resolvedWinDbg).ToLowerInvariant() } else { $DebuggerFrontend }
    breakpoint_mode = $generatorOutput.mode
    runner_output_policy = 'raw+sanitized'
    windbg_semantic_ready = ($TraceProfile -like 'singlekey-*')
    windbg_transport_state = 'staged'
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
        'This command script targets nt!CmQueryValueKey with parser-safe bu + bs 0 commands.',
        'WinDbg acts as the dead-flag final arbiter: ETW no-hit + WinDbg no-hit is the strongest dead-flag signal.'
    )
}

$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputFile -Encoding UTF8
$OutputFile
