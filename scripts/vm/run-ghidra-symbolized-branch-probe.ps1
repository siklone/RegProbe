[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetBinary,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [Parameter(Mandatory = $true)]
    [string[]]$Patterns,

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging\ghidra-v32-probes',
    [string]$HostSdkRoot = 'H:\Temp\winsdk-debuggers-x64\Windows Kits\10\Debuggers\x64',
    [string]$HostSharedFolderPath = 'H:\Temp\vm-tooling-staging',
    [string]$SharedFolderGuestName = 'vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\GhidraV32Probes',
    [string]$GuestProjectRoot = 'C:\Tools\GhidraProjects',
    [string]$GuestSymbolRoot = 'C:\Tools\Symbols',
    [switch]$NoAnalysis
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$branchScriptSource = Join-Path $repoRoot 'scripts\vm\ghidra\ExportBranchAnalysis.java'
$pdbScriptSource = Join-Path $repoRoot 'scripts\vm\ghidra\SetPdbSymbolRepository.java'
$compactorScript = Join-Path $repoRoot 'scripts\compact_ghidra_branch_output.py'
foreach ($source in @($branchScriptSource, $pdbScriptSource)) {
    if (-not (Test-Path $source)) {
        throw "Missing Ghidra v3.2 script: $source"
    }
}
if (-not (Test-Path $compactorScript)) {
    throw "Missing Ghidra compactor script: $compactorScript"
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$guestScriptsRoot = Join-Path $guestRoot 'scripts'
$hostRunner = Join-Path $hostRoot 'run-ghidra-symbolized-branch-probe-inner.ps1'
$guestRunner = Join-Path $guestRoot 'run-ghidra-symbolized-branch-probe-inner.ps1'
$hostBranchScript = Join-Path $hostRoot 'ExportBranchAnalysis.java'
$guestBranchScript = Join-Path $guestScriptsRoot 'ExportBranchAnalysis.java'
$hostPdbScript = Join-Path $hostRoot 'SetPdbSymbolRepository.java'
$guestPdbScript = Join-Path $guestScriptsRoot 'SetPdbSymbolRepository.java'
$hostMarkdown = Join-Path $hostRoot 'ghidra-matches.md'
$guestMarkdown = Join-Path $guestRoot 'ghidra-matches.md'
$hostEvidence = Join-Path $hostRoot 'evidence.json'
$guestEvidence = Join-Path $guestRoot 'evidence.json'
$hostRunLog = Join-Path $hostRoot 'ghidra-run.log'
$guestRunLog = Join-Path $guestRoot 'ghidra-run.log'
$hostStdout = Join-Path $hostRoot 'ghidra-stdout.txt'
$guestStdout = Join-Path $guestRoot 'ghidra-stdout.txt'
$hostStderr = Join-Path $hostRoot 'ghidra-stderr.txt'
$guestStderr = Join-Path $guestRoot 'ghidra-stderr.txt'
$hostTargetBinary = Join-Path $hostRoot ([IO.Path]::GetFileName($TargetBinary))
$hostSymbolCacheRoot = Join-Path $hostRoot 'symbol-cache'
$hostSymbolCacheZip = Join-Path $hostRoot 'symbol-cache.zip'
$hostSymchkLog = Join-Path $hostRoot 'symchk-host.log'
$patternPayload = ($Patterns | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join '|||'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostSymbolCacheRoot -Force | Out-Null
Copy-Item -Path $branchScriptSource -Destination $hostBranchScript -Force
Copy-Item -Path $pdbScriptSource -Destination $hostPdbScript -Force

$innerScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetBinary,

    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,

    [Parameter(Mandatory = $true)]
    [string]$ProjectName,

    [Parameter(Mandatory = $true)]
    [string]$PdbScriptPath,

    [Parameter(Mandatory = $true)]
    [string]$BranchScriptPath,

    [Parameter(Mandatory = $true)]
    [string]$MarkdownPath,

    [Parameter(Mandatory = $true)]
    [string]$EvidencePath,

    [Parameter(Mandatory = $true)]
    [string]$RunLogPath,

    [Parameter(Mandatory = $true)]
    [string]$StdoutPath,

    [Parameter(Mandatory = $true)]
    [string]$StderrPath,

    [Parameter(Mandatory = $true)]
    [string]$ProbeName,

    [Parameter(Mandatory = $true)]
    [string]$PatternPayload,

    [Parameter(Mandatory = $true)]
    [string]$SymbolRoot,

    [switch]$NoAnalysis
)

$Patterns = @($PatternPayload -split '\|\|\|' | Where-Object { $_ })
$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path (Split-Path -Parent $MarkdownPath) -Force | Out-Null
New-Item -ItemType Directory -Path $ProjectRoot -Force | Out-Null
New-Item -ItemType Directory -Path $SymbolRoot -Force | Out-Null
$tempRoot = Join-Path (Split-Path -Parent $MarkdownPath) 'temp'
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
$env:TEMP = $tempRoot
$env:TMP = $tempRoot

function Write-Evidence {
    param(
        [string]$Status,
        [bool]$PdbLoaded,
        [string]$PdbSource,
        [string]$Failure
    )

    $payload = [ordered]@{
        binary = [System.IO.Path]::GetFileName($TargetBinary)
        probe = $ProbeName
        timestamp = [DateTime]::UtcNow.ToString('o')
        pdb_source = $PdbSource
        pdb_loaded = $PdbLoaded
        status = $Status
        failure = $Failure
        matches = @()
    }
    $payload | ConvertTo-Json -Depth 8 | Set-Content -Path $EvidencePath -Encoding UTF8
}

Write-Evidence -Status 'started' -PdbLoaded $false -PdbSource $SymbolRoot -Failure $null

$analyzeHeadless = Get-ChildItem -Path 'C:\Tools\Ghidra' -Recurse -Filter 'analyzeHeadless.bat' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $analyzeHeadless) {
    Write-Evidence -Status 'blocked-ghidra-missing' -PdbLoaded $false -PdbSource $SymbolRoot -Failure 'analyzeHeadless.bat not found'
    exit 0
}

function Test-LocalPdbCache {
    param([string]$Root)

    if (-not (Test-Path $Root)) {
        return $false
    }

    $candidate = Get-ChildItem -Path $Root -Recurse -Include '*.pdb','*.pd_' -ErrorAction SilentlyContinue | Select-Object -First 1
    return [bool]$candidate
}

if (-not (Test-LocalPdbCache -Root $SymbolRoot)) {
    Write-Evidence -Status 'blocked-pdb-missing' -PdbLoaded $false -PdbSource $SymbolRoot -Failure 'No local PDB cache was staged into the guest symbol root.'
    exit 0
}

$args = @(
    $ProjectRoot,
    $ProjectName,
    '-import', $TargetBinary,
    '-overwrite',
    '-scriptPath', (Split-Path -Parent $PdbScriptPath),
    '-preScript', (Split-Path -Leaf $PdbScriptPath), $SymbolRoot
)

if ($NoAnalysis) {
    $args += '-noanalysis'
}

$args += @(
    '-postScript', (Split-Path -Leaf $BranchScriptPath), $MarkdownPath, $EvidencePath, $ProbeName, $SymbolRoot
) + $Patterns + @('-deleteProject')

$proc = Start-Process -FilePath $analyzeHeadless.FullName `
    -ArgumentList $args `
    -PassThru -Wait -WindowStyle Hidden `
    -RedirectStandardOutput $StdoutPath `
    -RedirectStandardError $StderrPath

$logParts = @()
if (Test-Path $StdoutPath) { $logParts += Get-Content -Path $StdoutPath -Raw }
if (Test-Path $StderrPath) { $logParts += Get-Content -Path $StderrPath -Raw }
Set-Content -Path $RunLogPath -Value (($logParts -join [Environment]::NewLine).Trim()) -Encoding UTF8

if (-not (Test-Path $EvidencePath)) {
    Write-Evidence -Status 'failed' -PdbLoaded $false -PdbSource $SymbolRoot -Failure "Ghidra exited $($proc.ExitCode) without producing evidence."
    exit 0
}

$payload = Get-Content -Path $EvidencePath -Raw | ConvertFrom-Json
$payload | Add-Member -NotePropertyName exit_code -NotePropertyValue $proc.ExitCode -Force
$payload | Add-Member -NotePropertyName markdown_exists -NotePropertyValue ([bool](Test-Path $MarkdownPath)) -Force
$payload | Add-Member -NotePropertyName run_log_exists -NotePropertyValue ([bool](Test-Path $RunLogPath)) -Force
if ($proc.ExitCode -ne 0) {
    $payload | Add-Member -NotePropertyName status -NotePropertyValue 'failed' -Force
    if (-not $payload.failure) {
        $payload | Add-Member -NotePropertyName failure -NotePropertyValue "analyzeHeadless exited $($proc.ExitCode)" -Force
    }
}
$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $EvidencePath -Encoding UTF8
'@

Set-Content -Path $hostRunner -Value $innerScript -Encoding UTF8

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

function Invoke-VmrunWithToolsRetry {
    param(
        [string[]]$Arguments,
        [int]$TimeoutSeconds = 900,
        [int]$RetryDelaySeconds = 15,
        [switch]$IgnoreExitCode
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $output = & $VmrunPath @Arguments 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
        if ($IgnoreExitCode -or $exitCode -eq 0) {
            return $output.Trim()
        }

        $trimmed = $output.Trim()
        $isToolsTransient = $trimmed -match 'VMware Tools are not running' -or
            $trimmed -match 'VIX_E_TOOLS_NOT_RUNNING' -or
            $trimmed -match 'VIX_E_TIMEOUT_WAITING_FOR_TOOLS'

        if (-not $isToolsTransient -or (Get-Date) -ge $deadline) {
            throw "vmrun failed ($exitCode): $trimmed"
        }

        Start-Sleep -Seconds $RetryDelaySeconds
    } while ((Get-Date) -lt $deadline)

    throw "vmrun timed out waiting for VMware Tools recovery."
}

function Resolve-SharedGuestPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostPath,
        [Parameter(Mandatory = $true)]
        [string]$SharedRoot,
        [Parameter(Mandatory = $true)]
        [string]$GuestShareName
    )

    $resolvedHostPath = (Resolve-Path $HostPath).Path
    $resolvedSharedRoot = (Resolve-Path $SharedRoot).Path
    if (-not $resolvedHostPath.StartsWith($resolvedSharedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path '$resolvedHostPath' is outside shared root '$resolvedSharedRoot'."
    }

    $relative = $resolvedHostPath.Substring($resolvedSharedRoot.Length).TrimStart('\')
    if ([string]::IsNullOrWhiteSpace($relative)) {
        return "\\vmware-host\Shared Folders\$GuestShareName"
    }

    return "\\vmware-host\Shared Folders\$GuestShareName\$relative"
}

function Find-HostSymchk {
    param([string]$ExplicitRoot)

    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($path in @(
        $ExplicitRoot,
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Debugging Tools for Windows (x64)',
        'C:\Program Files\Debugging Tools for Windows'
    )) {
        if (-not [string]::IsNullOrWhiteSpace($path) -and -not $candidates.Contains($path)) {
            $candidates.Add($path)
        }
    }

    foreach ($candidate in $candidates) {
        if (-not (Test-Path $candidate)) { continue }
        $symchk = Join-Path $candidate 'symchk.exe'
        if (Test-Path $symchk) {
            return $symchk
        }
    }

    return $null
}

function Normalize-TextArtifact {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    $raw = Get-Content -Path $Path -Raw
    $raw = $raw -replace "`r`n", "`n"
    [System.IO.File]::WriteAllText($Path, $raw, [System.Text.UTF8Encoding]::new($false))
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestScriptsRoot) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $TargetBinary, $hostTargetBinary) | Out-Null

$hostSymchk = Find-HostSymchk -ExplicitRoot $HostSdkRoot
if (-not $hostSymchk) {
    throw "Host symchk.exe was not found. Expected under '$HostSdkRoot' or a standard debugger root."
}

& $hostSymchk /r $hostTargetBinary /s "SRV*$hostSymbolCacheRoot*https://msdl.microsoft.com/download/symbols" *> $hostSymchkLog
$hostSymchkExit = $LASTEXITCODE
if ($hostSymchkExit -ne 0) {
    throw "Host symchk failed with exit code $hostSymchkExit. See $hostSymchkLog"
}

$guestSymbolRoot = Join-Path $GuestSymbolRoot $OutputName
$guestSharedSymbolCache = Resolve-SharedGuestPath -HostPath $hostSymbolCacheRoot -SharedRoot $HostSharedFolderPath -GuestShareName $SharedFolderGuestName
$guestSymbolCacheZip = Join-Path $guestRoot 'symbol-cache.zip'
$hostStageScript = Join-Path $hostRoot 'stage-symbol-cache.ps1'
$guestStageScript = Join-Path $guestRoot 'stage-symbol-cache.ps1'
$hostArchiveStageScript = Join-Path $hostRoot 'stage-symbol-cache-archive.ps1'
$guestArchiveStageScript = Join-Path $guestRoot 'stage-symbol-cache-archive.ps1'
$hostStageResult = Join-Path $hostRoot 'stage-symbol-cache.json'
$guestStageResult = Join-Path $guestRoot 'stage-symbol-cache.json'
$hostArchiveStageResult = Join-Path $hostRoot 'stage-symbol-cache-archive.json'
$guestArchiveStageResult = Join-Path $guestRoot 'stage-symbol-cache-archive.json'

@'
$ErrorActionPreference = 'Stop'
[string]$SourceRoot = '__SOURCE_ROOT__'
[string]$SourceArchive = '__SOURCE_ARCHIVE__'
[string]$DestinationRoot = '__DESTINATION_ROOT__'
[string]$ResultPath = '__RESULT_PATH__'
New-Item -ItemType Directory -Path (Split-Path -Parent $ResultPath) -Force | Out-Null
$payload = [ordered]@{
    source_root = $SourceRoot
    source_archive = $SourceArchive
    destination_root = $DestinationRoot
    source_visible = (Test-Path $SourceRoot)
    archive_visible = if ([string]::IsNullOrWhiteSpace($SourceArchive)) { $false } else { Test-Path $SourceArchive }
    status = 'not-run'
    pdb_count = 0
    error = $null
}
try {
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    if ($payload.source_visible) {
        Copy-Item -Path (Join-Path $SourceRoot '*') -Destination $DestinationRoot -Recurse -Force
    }
    elseif ($payload.archive_visible) {
        Expand-Archive -Path $SourceArchive -DestinationPath $DestinationRoot -Force
    }
    else {
        throw 'shared symbol cache is not visible in the guest and no direct archive fallback was provided'
    }
    $payload.pdb_count = @(
        Get-ChildItem -Path $DestinationRoot -Recurse -Include '*.pdb','*.pd_' -ErrorAction SilentlyContinue
    ).Count
    $payload.status = if ($payload.pdb_count -gt 0) { 'staged' } else { 'failed-no-pdb' }
}
catch {
    $payload.status = 'failed'
    $payload.error = $_.Exception.Message
}
$payload | ConvertTo-Json -Depth 6 | Set-Content -Path $ResultPath -Encoding UTF8
'@.Replace('__SOURCE_ROOT__', $guestSharedSymbolCache).
    Replace('__SOURCE_ARCHIVE__', '').
    Replace('__DESTINATION_ROOT__', $guestSymbolRoot).
    Replace('__RESULT_PATH__', $guestStageResult) | Set-Content -Path $hostStageScript -Encoding UTF8

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostStageScript, $guestStageScript) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NonInteractive',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $guestStageScript
) -IgnoreExitCode | Out-Null
Remove-Item -Path $hostStageResult -Force -ErrorAction SilentlyContinue
Invoke-VmrunWithToolsRetry -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestStageResult, $hostStageResult) | Out-Null

if (-not (Test-Path $hostStageResult)) {
    throw "Guest symbol-cache staging did not produce stage-symbol-cache.json."
}

$stagePayload = Get-Content -Path $hostStageResult -Raw | ConvertFrom-Json
if ($stagePayload.status -ne 'staged') {
    if (Test-Path $hostSymbolCacheZip) {
        Remove-Item -Path $hostSymbolCacheZip -Force
    }
    Compress-Archive -Path (Join-Path $hostSymbolCacheRoot '*') -DestinationPath $hostSymbolCacheZip -Force
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostSymbolCacheZip, $guestSymbolCacheZip) | Out-Null
    @'
$ErrorActionPreference = 'Stop'
[string]$SourceArchive = '__SOURCE_ARCHIVE__'
[string]$DestinationRoot = '__DESTINATION_ROOT__'
[string]$ResultPath = '__RESULT_PATH__'
try {
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    Expand-Archive -Path $SourceArchive -DestinationPath $DestinationRoot -Force
    [ordered]@{
        source_root = ''
        source_archive = $SourceArchive
        destination_root = $DestinationRoot
        source_visible = $false
        archive_visible = (Test-Path $SourceArchive)
        status = if (@(Get-ChildItem -Path $DestinationRoot -Recurse -Include '*.pdb','*.pd_' -ErrorAction SilentlyContinue).Count -gt 0) { 'staged' } else { 'failed-no-pdb' }
        pdb_count = @(Get-ChildItem -Path $DestinationRoot -Recurse -Include '*.pdb','*.pd_' -ErrorAction SilentlyContinue).Count
        error = $null
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $ResultPath -Encoding UTF8
}
catch {
    [ordered]@{
        source_root = ''
        source_archive = $SourceArchive
        destination_root = $DestinationRoot
        source_visible = $false
        archive_visible = (Test-Path $SourceArchive)
        status = 'failed'
        pdb_count = 0
        error = $_.Exception.Message
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $ResultPath -Encoding UTF8
}
'@.Replace('__SOURCE_ARCHIVE__', $guestSymbolCacheZip).
        Replace('__DESTINATION_ROOT__', $guestSymbolRoot).
        Replace('__RESULT_PATH__', $guestArchiveStageResult) | Set-Content -Path $hostArchiveStageScript -Encoding UTF8
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostArchiveStageScript, $guestArchiveStageScript) | Out-Null
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NonInteractive',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestArchiveStageScript
    ) -IgnoreExitCode | Out-Null
    Remove-Item -Path $hostArchiveStageResult -Force -ErrorAction SilentlyContinue
    Invoke-VmrunWithToolsRetry -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestArchiveStageResult, $hostArchiveStageResult) | Out-Null
    if (-not (Test-Path $hostArchiveStageResult)) {
        throw "Guest symbol-cache archive fallback did not produce stage-symbol-cache-archive.json."
    }
    $stagePayload = Get-Content -Path $hostArchiveStageResult -Raw | ConvertFrom-Json
}
if ($stagePayload.status -ne 'staged') {
    throw "Guest symbol-cache staging failed: $($stagePayload.status) $($stagePayload.error)"
}

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostRunner, $guestRunner) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostBranchScript, $guestBranchScript) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostPdbScript, $guestPdbScript) | Out-Null

$vmrunArgs = @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NonInteractive',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $guestRunner,
    '-TargetBinary', $TargetBinary,
    '-ProjectRoot', $GuestProjectRoot,
    '-ProjectName', ("Probe_{0}" -f $OutputName.Replace('-', '_')),
    '-PdbScriptPath', $guestPdbScript,
    '-BranchScriptPath', $guestBranchScript,
    '-MarkdownPath', $guestMarkdown,
    '-EvidencePath', $guestEvidence,
    '-RunLogPath', $guestRunLog,
    '-StdoutPath', $guestStdout,
    '-StderrPath', $guestStderr,
    '-ProbeName', $OutputName,
    '-PatternPayload', $patternPayload,
    '-SymbolRoot', $guestSymbolRoot
)
if ($NoAnalysis) {
    $vmrunArgs += '-NoAnalysis'
}

Invoke-Vmrun -Arguments $vmrunArgs -IgnoreExitCode | Out-Null

foreach ($pair in @(
    @{ Guest = $guestEvidence; Host = $hostEvidence },
    @{ Guest = $guestRunLog; Host = $hostRunLog },
    @{ Guest = $guestMarkdown; Host = $hostMarkdown },
    @{ Guest = $guestStdout; Host = $hostStdout },
    @{ Guest = $guestStderr; Host = $hostStderr }
)) {
    Invoke-VmrunWithToolsRetry -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) | Out-Null
}

foreach ($path in @($hostMarkdown, $hostRunLog, $hostStdout, $hostStderr)) {
    Normalize-TextArtifact -Path $path
}

if (-not (Test-Path $hostEvidence)) {
    throw "Guest Ghidra v3.2 probe did not produce evidence.json. See $hostRoot"
}

& python $compactorScript --evidence $hostEvidence --markdown $hostMarkdown | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Ghidra compactor failed for $hostRoot"
}
Normalize-TextArtifact -Path $hostMarkdown

$evidencePayload = Get-Content -Path $hostEvidence -Raw | ConvertFrom-Json
if (Test-Path $hostRunLog) {
    $runLog = Get-Content -Path $hostRunLog -Raw
    $pdbLoadedFromLog = $runLog -match 'PDB analyzer parsing file:' -and $runLog -match 'PDB Types and Main Symbols Processing Terminated Normally'
    if ($pdbLoadedFromLog) {
        $evidencePayload | Add-Member -NotePropertyName pdb_loaded -NotePropertyValue $true -Force
        $evidencePayload | Add-Member -NotePropertyName pdb_verification_source -NotePropertyValue 'ghidra-run-log' -Force
    }
}

$evidencePayload | ConvertTo-Json -Depth 10 | Set-Content -Path $hostEvidence -Encoding UTF8
$evidencePayload | ConvertTo-Json -Depth 10
