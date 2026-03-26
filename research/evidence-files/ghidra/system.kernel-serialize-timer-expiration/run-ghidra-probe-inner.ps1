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
    [string]$PatternPayload
)

$Patterns = @($PatternPayload -split '\|\|\|' | Where-Object { $_ })

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path (Split-Path -Parent $MarkdownPath) -Force | Out-Null
New-Item -ItemType Directory -Path $ProjectRoot -Force | Out-Null
$proc = $null
$failure = $null

try {
    $analyzeHeadless = Get-ChildItem -Path 'C:\Tools\Ghidra' -Recurse -Filter 'analyzeHeadless.bat' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $analyzeHeadless) {
        throw 'analyzeHeadless.bat not found'
    }

    $args = @(
        $ProjectRoot,
        $ProjectName,
        '-import', $TargetBinary,
        '-overwrite',
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
$meta = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    target_binary = $TargetBinary
    project_root = $ProjectRoot
    project_name = $ProjectName
    script_path = $ScriptPath
    markdown_path = $MarkdownPath
    stdout_path = $StdoutPath
    stderr_path = $StderrPath
    exit_code = $exitCode
    markdown_exists = [bool](Test-Path $MarkdownPath)
    stdout_exists = [bool](Test-Path $StdoutPath)
    stderr_exists = [bool](Test-Path $StderrPath)
    patterns = $Patterns
    failure = $failure
}

$meta | ConvertTo-Json -Depth 6 | Set-Content -Path $MetadataPath -Encoding UTF8
if ($exitCode -ne 0) {
    exit $exitCode
}
