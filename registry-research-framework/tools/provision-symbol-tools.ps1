[CmdletBinding()]
param(
    [string]$VmProfile = 'primary',
    [string]$OutputFile = ''
)

$script = Join-Path (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts\vm') 'provision-symbol-tools.ps1'
if (-not (Test-Path $script)) {
    throw "Missing symbol provisioning wrapper: $script"
}

$args = @('-VmProfile', $VmProfile)
if (-not [string]::IsNullOrWhiteSpace($OutputFile)) {
    $args += @('-OutputFile', $OutputFile)
}

& $script @args
exit $LASTEXITCODE
