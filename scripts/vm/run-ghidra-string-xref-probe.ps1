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
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging\ghidra-probes',
    [string]$GuestOutputRoot = 'C:\Tools\GhidraProbes',
    [string]$GuestProjectRoot = 'C:\Tools\GhidraProjects',
    [string]$GuestGhidraRoot = 'C:\Tools\Ghidra',
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
$hostMarkdown = Join-Path $hostRoot "$OutputName.md"
$guestMarkdown = Join-Path $guestRoot "$OutputName.md"
$hostStdout = Join-Path $hostRoot "$OutputName.stdout.txt"
$guestStdout = Join-Path $guestRoot "$OutputName.stdout.txt"
$hostStderr = Join-Path $hostRoot "$OutputName.stderr.txt"
$guestStderr = Join-Path $guestRoot "$OutputName.stderr.txt"
$hostMeta = Join-Path $hostRoot 'metadata.json'
$guestMeta = Join-Path $guestRoot 'metadata.json'
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
    [string]$StdoutPath,

    [Parameter(Mandatory = $true)]
    [string]$StderrPath,

    [Parameter(Mandatory = $true)]
    [string]$MetadataPath,

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
$meta = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    target_binary = $TargetBinary
    project_root = $ProjectRoot
    project_name = $ProjectName
    script_path = $ScriptPath
    markdown_path = $MarkdownPath
    stdout_path = $StdoutPath
    stderr_path = $StderrPath
    exit_code = $null
    markdown_exists = [bool](Test-Path $MarkdownPath)
    stdout_exists = [bool](Test-Path $StdoutPath)
    stderr_exists = [bool](Test-Path $StderrPath)
    patterns = $Patterns
    failure = $null
    status = 'started'
}

$meta | ConvertTo-Json -Depth 6 | Set-Content -Path $MetadataPath -Encoding UTF8

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
        '-postScript', (Split-Path -Leaf $ScriptPath), $MarkdownPath
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
$meta.generated_utc = [DateTime]::UtcNow.ToString('o')
$meta.exit_code = $exitCode
$meta.markdown_exists = [bool](Test-Path $MarkdownPath)
$meta.stdout_exists = [bool](Test-Path $StdoutPath)
$meta.stderr_exists = [bool](Test-Path $StderrPath)
$meta.failure = $failure
$meta.status = if ($exitCode -eq 0) { 'completed' } else { 'failed' }

$meta | ConvertTo-Json -Depth 6 | Set-Content -Path $MetadataPath -Encoding UTF8
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
    '-NoProfile',
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
    '-StdoutPath',
    $guestStdout,
    '-StderrPath',
    $guestStderr,
    '-MetadataPath',
    $guestMeta,
    '-PatternPayload',
    $patternPayload
)

if ($NoAnalysis) {
    $vmrunArgs += '-NoAnalysis'
}

Invoke-Vmrun -Arguments $vmrunArgs -IgnoreExitCode | Out-Null

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestMeta, $hostMeta) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestStdout, $hostStdout) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestStderr, $hostStderr) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestMarkdown, $hostMarkdown) -IgnoreExitCode | Out-Null

$meta = if (Test-Path $hostMeta) {
    Get-Content -Path $hostMeta -Raw | ConvertFrom-Json
}
else {
    $null
}

if (-not $meta -or $null -eq $meta.exit_code) {
    throw "Guest Ghidra probe did not complete cleanly. See $hostRoot"
}

[ordered]@{
    target_binary = $TargetBinary
    output_name = $OutputName
    host_output_root = $hostRoot
    markdown = if (Test-Path $hostMarkdown) { $hostMarkdown } else { $null }
    stdout = if (Test-Path $hostStdout) { $hostStdout } else { $null }
    stderr = if (Test-Path $hostStderr) { $hostStderr } else { $null }
    metadata = $hostMeta
    patterns = $Patterns
} | ConvertTo-Json -Depth 6

if ($meta -and $meta.exit_code -ne 0) {
    throw "Guest Ghidra probe failed with exit code $($meta.exit_code). See $hostStdout and $hostStderr."
}
