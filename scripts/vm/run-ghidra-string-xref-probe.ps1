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
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging\ghidra-probes',
    [string]$GuestOutputRoot = 'C:\Tools\GhidraProbes',
    [string]$GuestProjectRoot = 'C:\Tools\GhidraProjects',
    [switch]$NoAnalysis
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptSource = Join-Path $repoRoot 'scripts\vm\ghidra\ExportStringXrefs.java'
if (-not (Test-Path $scriptSource)) {
    throw "Missing Ghidra script: $scriptSource"
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$hostRunner = Join-Path $hostRoot 'run-ghidra-probe-inner.ps1'
$guestRunner = Join-Path $guestRoot 'run-ghidra-probe-inner.ps1'
$hostScript = Join-Path $hostRoot 'ExportStringXrefs.java'
$guestScript = Join-Path $guestRoot 'ExportStringXrefs.java'
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
$patternPayload = ($Patterns | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join '|||'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
Copy-Item -Path $scriptSource -Destination $hostScript -Force

$innerScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetBinary,

    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,

    [Parameter(Mandatory = $true)]
    [string]$ProjectName,

    [Parameter(Mandatory = $true)]
    [string]$ScriptPath,

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

    [switch]$NoAnalysis
)

$Patterns = @($PatternPayload -split '\|\|\|' | Where-Object { $_ })
$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path (Split-Path -Parent $MarkdownPath) -Force | Out-Null
New-Item -ItemType Directory -Path $ProjectRoot -Force | Out-Null

$proc = $null
$failure = $null

$initialEvidence = [ordered]@{
    binary = [System.IO.Path]::GetFileName($TargetBinary)
    probe = $ProbeName
    timestamp = [DateTime]::UtcNow.ToString('o')
    ghidra_no_function_fallback = $false
    matches = @()
    exit_code = $null
    status = 'started'
    failure = $null
}
$initialEvidence | ConvertTo-Json -Depth 6 | Set-Content -Path $EvidencePath -Encoding UTF8

try {
    $analyzeHeadless = Get-ChildItem -Path 'C:\Tools\Ghidra' -Recurse -Filter 'analyzeHeadless.bat' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $analyzeHeadless) {
        throw 'analyzeHeadless.bat not found'
    }

    $args = @(
        $ProjectRoot,
        $ProjectName,
        '-import', $TargetBinary,
        '-overwrite'
    )

    if ($NoAnalysis) {
        $args += '-noanalysis'
    }

    $args += @(
        '-scriptPath', (Split-Path -Parent $ScriptPath),
        '-postScript', (Split-Path -Leaf $ScriptPath), $MarkdownPath, $EvidencePath, $ProbeName
    ) + $Patterns + @('-deleteProject')

    $proc = Start-Process -FilePath $analyzeHeadless.FullName `
        -ArgumentList $args `
        -PassThru -Wait -WindowStyle Hidden `
        -RedirectStandardOutput $StdoutPath `
        -RedirectStandardError $StderrPath
}
catch {
    $failure = $_.Exception.Message
}

$exitCode = if ($proc) { $proc.ExitCode } else { 1 }
$logParts = @()
if (Test-Path $StdoutPath) {
    $logParts += Get-Content -Path $StdoutPath -Raw
}
if (Test-Path $StderrPath) {
    $stderrText = Get-Content -Path $StderrPath -Raw
    if (-not [string]::IsNullOrWhiteSpace($stderrText)) {
        $logParts += $stderrText
    }
}
$logText = ($logParts -join [Environment]::NewLine).Trim()
Set-Content -Path $RunLogPath -Value $logText -Encoding UTF8

$evidence = if (Test-Path $EvidencePath) {
    Get-Content -Path $EvidencePath -Raw | ConvertFrom-Json
}
else {
    [pscustomobject]@{
        binary = [System.IO.Path]::GetFileName($TargetBinary)
        probe = $ProbeName
        timestamp = [DateTime]::UtcNow.ToString('o')
        ghidra_no_function_fallback = $false
        matches = @()
    }
}

$evidence | Add-Member -NotePropertyName exit_code -NotePropertyValue $exitCode -Force
$evidence | Add-Member -NotePropertyName status -NotePropertyValue (if ($exitCode -eq 0) { 'completed' } else { 'failed' }) -Force
$evidence | Add-Member -NotePropertyName failure -NotePropertyValue $failure -Force
$evidence | Add-Member -NotePropertyName markdown_exists -NotePropertyValue ([bool](Test-Path $MarkdownPath)) -Force
$evidence | Add-Member -NotePropertyName run_log_exists -NotePropertyValue ([bool](Test-Path $RunLogPath)) -Force
$evidence | ConvertTo-Json -Depth 8 | Set-Content -Path $EvidencePath -Encoding UTF8

if ($exitCode -ne 0) {
    exit $exitCode
}
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

function Normalize-TextArtifact {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return
    }

    $raw = Get-Content -Path $Path -Raw
    $raw = $raw -replace "`r`n", "`n"
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($raw -split "`n", -1)) {
        $lines.Add($line.TrimEnd())
    }

    while ($lines.Count -gt 0 -and [string]::IsNullOrEmpty($lines[$lines.Count - 1])) {
        $lines.RemoveAt($lines.Count - 1)
    }

    $normalized = if ($lines.Count -gt 0) {
        ($lines -join "`n") + "`n"
    }
    else {
        ""
    }

    $normalized = [regex]::Replace($normalized, '(?im)(?<![A-Za-z0-9])[A-Z]:\\[^\r\n]+', '<local-path>')

    [System.IO.File]::WriteAllText($Path, $normalized, [System.Text.UTF8Encoding]::new($false))
}

function Ensure-VmRunning {
    $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
    if ($running -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) | Out-Null
    }
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 3
    }

    throw 'Guest is not ready for vmrun guest operations.'
}

Ensure-VmRunning
Wait-GuestReady

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScript, $guestScript) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostRunner, $guestRunner) | Out-Null

$vmrunArgs = @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NonInteractive',
    '-NoProfile',
    '-WindowStyle',
    'Hidden',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $guestRunner,
    '-TargetBinary',
    $TargetBinary,
    '-ProjectRoot',
    $GuestProjectRoot,
    '-ProjectName',
    ("Probe_{0}" -f $OutputName.Replace('-', '_')),
    '-ScriptPath',
    $guestScript,
    '-MarkdownPath',
    $guestMarkdown,
    '-EvidencePath',
    $guestEvidence,
    '-RunLogPath',
    $guestRunLog,
    '-StdoutPath',
    $guestStdout,
    '-StderrPath',
    $guestStderr,
    '-ProbeName',
    $OutputName,
    '-PatternPayload',
    $patternPayload
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
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) -IgnoreExitCode | Out-Null
}

$legacyMarkdown = Join-Path $hostRoot ("{0}.md" -f $OutputName)
if (Test-Path $hostMarkdown) {
    Copy-Item -Path $hostMarkdown -Destination $legacyMarkdown -Force
}

Normalize-TextArtifact -Path $hostMarkdown
Normalize-TextArtifact -Path $legacyMarkdown
Normalize-TextArtifact -Path $hostRunLog
Normalize-TextArtifact -Path $hostStdout
Normalize-TextArtifact -Path $hostStderr

$evidence = if (Test-Path $hostEvidence) {
    Get-Content -Path $hostEvidence -Raw | ConvertFrom-Json
}
else {
    $null
}

if (-not $evidence) {
    throw "Guest Ghidra probe did not complete cleanly. See $hostRoot"
}

if ($null -eq $evidence.exit_code) {
    $evidence | Add-Member -NotePropertyName exit_code -NotePropertyValue 0 -Force
    $evidence | Add-Member -NotePropertyName status -NotePropertyValue 'completed' -Force
    $evidence | Add-Member -NotePropertyName failure -NotePropertyValue $null -Force
    $evidence | Add-Member -NotePropertyName markdown_exists -NotePropertyValue ([bool](Test-Path $hostMarkdown)) -Force
    $evidence | Add-Member -NotePropertyName run_log_exists -NotePropertyValue ([bool](Test-Path $hostRunLog)) -Force
    $evidence | ConvertTo-Json -Depth 8 | Set-Content -Path $hostEvidence -Encoding UTF8
}

[ordered]@{
    target_binary = $TargetBinary
    output_name = $OutputName
    host_output_root = $hostRoot
    markdown = if (Test-Path $hostMarkdown) { $hostMarkdown } else { $null }
    evidence = $hostEvidence
    run_log = if (Test-Path $hostRunLog) { $hostRunLog } else { $null }
    stdout = if (Test-Path $hostStdout) { $hostStdout } else { $null }
    stderr = if (Test-Path $hostStderr) { $hostStderr } else { $null }
    patterns = $Patterns
} | ConvertTo-Json -Depth 6

if ($evidence -and $evidence.exit_code -ne 0) {
    throw "Guest Ghidra probe failed with exit code $($evidence.exit_code). See $hostRunLog."
}

