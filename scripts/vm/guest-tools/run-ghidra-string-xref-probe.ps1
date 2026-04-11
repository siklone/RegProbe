[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [Parameter(Mandatory = $true)]
    [string[]]$Patterns,

    [string]$ScriptsRoot = 'C:\Tools\Scripts',
    [string]$ProjectRoot = 'C:\Tools\GhidraProjects',
    [string]$SymbolRoot = '',
    [string]$OutputRoot = '',
    [string]$UploadBaseUrl = '',
    [switch]$SkipSymchk,
    [switch]$NoAnalysis
)

$ErrorActionPreference = 'Stop'

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Get-DebuggerToolRoots {
    $roots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($path in @(
        'C:\Tools\SymbolTools',
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Debugging Tools for Windows (x64)',
        'C:\Program Files\Debugging Tools for Windows'
    )) {
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path) -and -not $roots.Contains($path)) {
            $roots.Add($path)
        }
    }

    foreach ($pkg in @(Get-AppxPackage -Name Microsoft.WinDbg* -ErrorAction SilentlyContinue)) {
        if ($pkg.InstallLocation -and (Test-Path $pkg.InstallLocation) -and -not $roots.Contains($pkg.InstallLocation)) {
            $roots.Add($pkg.InstallLocation)
        }
    }

    return @($roots)
}

function Find-FirstDebuggerTool {
    param([Parameter(Mandatory = $true)][string]$ToolName)

    $command = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    foreach ($root in Get-DebuggerToolRoots) {
        $candidate = Get-ChildItem -Path $root -Recurse -Filter $ToolName -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    return $null
}

function Invoke-ArtifactUpload {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path $Path)) {
        return $null
    }

    $targetUri = '{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName
    Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
    return [ordered]@{
        path = $Path
        uri = $targetUri
    }
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\ghidra' $OutputName
}

$ghidraScriptRoot = Join-Path $ScriptsRoot 'ghidra'
$ghidraLauncher = Join-Path $ScriptsRoot 'ghidra-headless.cmd'
$symchkPath = Find-FirstDebuggerTool -ToolName 'symchk.exe'
$projectName = 'Probe_{0}' -f (($OutputName -replace '[^A-Za-z0-9_]', '_'))
$evidencePath = Join-Path $OutputRoot 'evidence.json'
$markdownPath = Join-Path $OutputRoot 'ghidra-matches.md'
$summaryPath = Join-Path $OutputRoot 'run-summary.json'
$symchkLogPath = Join-Path $OutputRoot 'symchk.txt'
$stdoutPath = Join-Path $OutputRoot 'ghidra-stdout.txt'
$stderrPath = Join-Path $OutputRoot 'ghidra-stderr.txt'
$runLogPath = Join-Path $OutputRoot 'ghidra-run.log'
$stagedPdbCount = 0
$stagedPdbSample = @()

if ([string]::IsNullOrWhiteSpace($SymbolRoot)) {
    $SymbolRoot = Join-Path 'C:\Tools\Symbols' $OutputName
}

Ensure-Directory -Path $ProjectRoot
Ensure-Directory -Path $OutputRoot
Ensure-Directory -Path $SymbolRoot

if (-not (Test-Path $BinaryPath)) {
    throw "Binary path not found: $BinaryPath"
}

if (-not (Test-Path $ghidraLauncher)) {
    throw "ghidra-headless.cmd not found at $ghidraLauncher"
}

if (-not (Test-Path $ghidraScriptRoot)) {
    throw "Ghidra script root not found at $ghidraScriptRoot"
}

$symchkExit = $null
if (-not $SkipSymchk) {
    if (-not $symchkPath) {
        throw 'symchk.exe was not found in this guest.'
    }

    $symbolStore = "SRV*$SymbolRoot*https://msdl.microsoft.com/download/symbols"
    $symchkArgs = @(
        '/r',
        $BinaryPath,
        '/s', $symbolStore,
        '/v',
        '/om', $symchkLogPath
    )

    $symchkProc = Start-Process -FilePath $symchkPath -ArgumentList $symchkArgs -PassThru -Wait -WindowStyle Hidden
    $symchkExit = $symchkProc.ExitCode

    $stagedPdbs = @(Get-ChildItem -Path $SymbolRoot -Recurse -Include '*.pdb', '*.pd_' -ErrorAction SilentlyContinue)
    $stagedPdbCount = $stagedPdbs.Count
    $stagedPdbSample = @($stagedPdbs | Select-Object -First 5 -ExpandProperty FullName)
    if ($stagedPdbs.Count -eq 0) {
        throw "symchk.exe did not stage any PDBs under $SymbolRoot"
    }
}

$ghidraArgs = @(
    $ProjectRoot,
    $projectName,
    '-import', $BinaryPath,
    '-overwrite',
    '-scriptPath', $ghidraScriptRoot,
    '-preScript', 'SetPdbSymbolRepository.java', $SymbolRoot
)

if ($NoAnalysis) {
    $ghidraArgs += '-noanalysis'
}

$ghidraArgs += @(
    '-postScript', 'ExportStringXrefs.java', $markdownPath, $evidencePath, $OutputName
) + $Patterns + @(
    '-deleteProject'
)

$proc = Start-Process -FilePath $ghidraLauncher `
    -ArgumentList $ghidraArgs `
    -PassThru -Wait -WindowStyle Hidden `
    -RedirectStandardOutput $stdoutPath `
    -RedirectStandardError $stderrPath

$ghidraExit = $proc.ExitCode
$logParts = @()
if (Test-Path $stdoutPath) {
    $logParts += Get-Content -Path $stdoutPath -Raw
}
if (Test-Path $stderrPath) {
    $stderrText = Get-Content -Path $stderrPath -Raw
    if (-not [string]::IsNullOrWhiteSpace($stderrText)) {
        $logParts += $stderrText
    }
}
($logParts -join [Environment]::NewLine).Trim() | Set-Content -Path $runLogPath -Encoding UTF8

if ($ghidraExit -ne 0) {
    throw "Ghidra string/xref run failed with exit code $ghidraExit"
}

if (-not (Test-Path $evidencePath)) {
    throw "Ghidra did not produce evidence.json at $evidencePath"
}

$evidence = Get-Content -Path $evidencePath -Raw | ConvertFrom-Json
$uploads = [ordered]@{}
foreach ($entry in @(
    @{ key = 'evidence'; path = $evidencePath; name = ('{0}-evidence.json' -f $OutputName) },
    @{ key = 'markdown'; path = $markdownPath; name = ('{0}-ghidra-matches.md' -f $OutputName) },
    @{ key = 'symchk_log'; path = $symchkLogPath; name = ('{0}-symchk.txt' -f $OutputName) },
    @{ key = 'run_log'; path = $runLogPath; name = ('{0}-ghidra-run.log' -f $OutputName) }
)) {
    $upload = Invoke-ArtifactUpload -Path $entry.path -RemoteName $entry.name
    if ($upload) {
        $uploads[$entry.key] = $upload
    }
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    binary_path = $BinaryPath
    output_name = $OutputName
    project_name = $projectName
    symbol_root = $SymbolRoot
    output_root = $OutputRoot
    symchk_path = $symchkPath
    symchk_exit_code = $symchkExit
    staged_pdb_count = $stagedPdbCount
    staged_pdb_sample = $stagedPdbSample
    ghidra_exit_code = $ghidraExit
    no_analysis = [bool]$NoAnalysis
    match_count = @($evidence.matches).Count
    uploads = $uploads
}

$result | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
Write-Output $summaryPath
