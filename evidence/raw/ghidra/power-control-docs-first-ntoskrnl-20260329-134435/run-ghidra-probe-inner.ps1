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
