[CmdletBinding(DefaultParameterSetName = 'modern')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'modern')]
    [string]$TargetBinary,

    [Parameter(Mandatory = $true, ParameterSetName = 'modern')]
    [string]$OutputName,

    [Parameter(Mandatory = $true, ParameterSetName = 'modern')]
    [string[]]$Patterns,

    [Parameter(Mandatory = $true, ParameterSetName = 'legacy')]
    [Alias('BinaryPath')]
    [string]$LegacyBinaryPath,

    [Parameter(Mandatory = $true, ParameterSetName = 'legacy')]
    [Alias('SearchTerm')]
    [string]$LegacySearchTerm,

    [switch]$NoAnalysis
)

$script = Join-Path (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "scripts\vm") "run-ghidra-string-xref-probe.ps1"
if (-not (Test-Path $script)) {
    throw "Missing existing Ghidra probe wrapper: $script"
}

$effectiveBinary = if ($PSCmdlet.ParameterSetName -eq 'legacy') { $LegacyBinaryPath } else { $TargetBinary }
$effectiveOutput = if ($PSCmdlet.ParameterSetName -eq 'legacy') {
    [System.IO.Path]::GetFileNameWithoutExtension($effectiveBinary) + "-xref"
}
else {
    $OutputName
}
$effectivePatterns = if ($PSCmdlet.ParameterSetName -eq 'legacy') {
    @($LegacySearchTerm)
}
else {
    @($Patterns)
}

$args = @(
    '-TargetBinary', $effectiveBinary,
    '-OutputName', $effectiveOutput
)

foreach ($pattern in $effectivePatterns) {
    $args += @('-Patterns', $pattern)
}

if ($NoAnalysis.IsPresent) {
    $args += '-NoAnalysis'
}

& $script @args
exit $LASTEXITCODE
