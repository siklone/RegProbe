[CmdletBinding()]
param(
    [string]$OutputName = 'local-kd-smoke',
    [string]$OutputRoot = '',
    [string]$SymbolRoot = '',
    [string]$UploadBaseUrl = '',
    [int]$TimeoutSeconds = 180,
    [string]$QuerySymbol = 'nt!CmQueryValueKey'
)

$ErrorActionPreference = 'Stop'

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Invoke-ArtifactUpload {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path -Path $Path -PathType Leaf)) {
        return $null
    }

    $targetUri = '{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName
    Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
    return [ordered]@{
        path = $Path
        uri = $targetUri
    }
}

function Find-FirstDebuggerTool {
    param([Parameter(Mandatory = $true)][string]$ToolName)

    $candidates = @(
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64'
    )

    foreach ($candidate in $candidates) {
        $path = Join-Path $candidate $ToolName
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\debugger-smoke' $OutputName
}

if ([string]::IsNullOrWhiteSpace($SymbolRoot)) {
    $SymbolRoot = Join-Path 'C:\Tools\Symbols' $OutputName
}

Ensure-Directory -Path $OutputRoot
Ensure-Directory -Path $SymbolRoot

$commandFile = Join-Path $OutputRoot 'local-kd.txt'
$logPath = Join-Path $OutputRoot 'local-kd.log'
$summaryPath = Join-Path $OutputRoot 'summary.json'
$stdoutPath = Join-Path $OutputRoot 'stdout.txt'
$stderrPath = Join-Path $OutputRoot 'stderr.txt'
$symchkLog = Join-Path $OutputRoot 'symchk.txt'

$kdPath = Find-FirstDebuggerTool -ToolName 'kd.exe'
$symchkPath = Find-FirstDebuggerTool -ToolName 'symchk.exe'

if (-not $kdPath) {
    throw 'kd.exe not found in expected debugger roots.'
}

if (-not $symchkPath) {
    throw 'symchk.exe not found in expected debugger roots.'
}

$symchkArgs = @(
    'C:\Windows\System32\ntoskrnl.exe',
    '/s', ('SRV*{0}*https://msdl.microsoft.com/download/symbols' -f $SymbolRoot),
    '/ocx', $symchkLog
)
$symchkProc = Start-Process -FilePath $symchkPath -ArgumentList $symchkArgs -PassThru -Wait -WindowStyle Hidden

@(
    ('.sympath {0}' -f $SymbolRoot)
    '.reload /f nt'
    '.echo REGPROBE_LOCALKD_BEGIN'
    ('x {0}' -f $QuerySymbol)
    '.echo REGPROBE_LOCALKD_END'
    'q'
) | Set-Content -Path $commandFile -Encoding ASCII

$kdArgs = @('-kl', '-cf', $commandFile, '-logo', $logPath)
$proc = Start-Process `
    -FilePath $kdPath `
    -ArgumentList $kdArgs `
    -PassThru `
    -RedirectStandardOutput $stdoutPath `
    -RedirectStandardError $stderrPath `
    -WindowStyle Hidden

$completed = $proc.WaitForExit($TimeoutSeconds * 1000)
$proc.Refresh()
if (-not $completed) {
    try {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    catch {
    }
}

$stdoutText = if (Test-Path $stdoutPath) { Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue } else { '' }
$logText = if (Test-Path $logPath) { Get-Content -Path $logPath -Raw -ErrorAction SilentlyContinue } else { '' }
$combinedText = '{0}{1}{2}' -f $logText, [Environment]::NewLine, $stdoutText

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    kd_path = $kdPath
    symchk_path = $symchkPath
    symchk_exit_code = $symchkProc.ExitCode
    symbol_root = $SymbolRoot
    query_symbol = $QuerySymbol
    completed = $completed
    exit_code = if ($completed) { $proc.ExitCode } else { $null }
    attached = ($combinedText -match 'Connected to Windows')
    local_kernel_disabled = ($combinedText -match 'Local kernel debugging is disabled by default')
    symbol_reload_started = ($combinedText -match 'Loading Kernel Symbols')
    query_symbol_seen = ($combinedText -match [regex]::Escape($QuerySymbol))
    command_file_exists = [bool](Test-Path $commandFile)
    log_exists = [bool](Test-Path $logPath)
    stdout_exists = [bool](Test-Path $stdoutPath)
    stderr_exists = [bool](Test-Path $stderrPath)
    symchk_log_exists = [bool](Test-Path -Path $symchkLog -PathType Leaf)
    symchk_log_is_directory = [bool](Test-Path -Path $symchkLog -PathType Container)
    uploads = [ordered]@{}
}

foreach ($entry in @(
    @{ key = 'command_file'; path = $commandFile; name = ('{0}.txt' -f $OutputName) },
    @{ key = 'log'; path = $logPath; name = ('{0}.log' -f $OutputName) },
    @{ key = 'stdout'; path = $stdoutPath; name = ('{0}.stdout.txt' -f $OutputName) },
    @{ key = 'stderr'; path = $stderrPath; name = ('{0}.stderr.txt' -f $OutputName) },
    @{ key = 'symchk_log'; path = $symchkLog; name = ('{0}-symchk.txt' -f $OutputName) }
)) {
    $upload = Invoke-ArtifactUpload -Path $entry.path -RemoteName $entry.name
    if ($upload) {
        $summary.uploads[$entry.key] = $upload
    }
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
Invoke-ArtifactUpload -Path $summaryPath -RemoteName ('{0}-summary.json' -f $OutputName) | Out-Null
Write-Output $summaryPath
