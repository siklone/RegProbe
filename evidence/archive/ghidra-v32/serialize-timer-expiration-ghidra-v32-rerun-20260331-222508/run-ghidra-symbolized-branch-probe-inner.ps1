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

function Find-Symchk {
    $direct = Get-Command symchk.exe -ErrorAction SilentlyContinue
    if ($direct) {
        return $direct.Source
    }

    $roots = New-Object System.Collections.Generic.List[string]
    foreach ($path in @(
        'C:\Tools\SymbolTools',
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Debugging Tools for Windows (x64)',
        'C:\Program Files\Debugging Tools for Windows'
    )) {
        if ((Test-Path $path) -and -not $roots.Contains($path)) {
            $roots.Add($path)
        }
    }

    foreach ($pkg in @(Get-AppxPackage -Name Microsoft.WinDbg* -ErrorAction SilentlyContinue)) {
        if ($pkg.InstallLocation -and (Test-Path $pkg.InstallLocation) -and -not $roots.Contains($pkg.InstallLocation)) {
            $roots.Add($pkg.InstallLocation)
        }
    }

    foreach ($root in $roots) {
        $candidate = Get-ChildItem -Path $root -Recurse -Filter 'symchk.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    return $null
}

$symchkPath = Find-Symchk
if ($symchkPath) {
    $symchk = @{ Source = $symchkPath }
}
else {
    $symchk = $null
}
if (-not $symchk) {
    Write-Evidence -Status 'blocked-pdb-missing' -PdbLoaded $false -PdbSource $SymbolRoot -Failure 'symchk.exe is not installed in the guest environment.'
    exit 0
}

$symbolLog = Join-Path (Split-Path -Parent $RunLogPath) 'symchk.log'
& $symchk.Source /r $TargetBinary /s "SRV*$SymbolRoot*https://msdl.microsoft.com/download/symbols" *> $symbolLog
$symchkExit = $LASTEXITCODE
if ($symchkExit -ne 0) {
    Write-Evidence -Status 'blocked-pdb-missing' -PdbLoaded $false -PdbSource $SymbolRoot -Failure "symchk failed with exit code $symchkExit"
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
