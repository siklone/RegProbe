[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('runtime', 'behavior')]
    [string]$Lane,
    [Parameter(Mandatory = $true)]
    [string]$TweakId
)

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$configPath = Join-Path $repoRoot 'registry-research-framework\config\tweak-vm-runners.json'

if (-not (Test-Path $configPath)) {
    throw "Missing tweak VM runner config: $configPath"
}

$config = Get-Content -Path $configPath -Raw | ConvertFrom-Json
$laneConfig = $config.$Lane
if ($null -eq $laneConfig) {
    return
}

$entry = $laneConfig.PSObject.Properties[$TweakId].Value
if ($null -eq $entry) {
    return
}

$scriptPath = Join-Path $repoRoot $entry.script

[pscustomobject]@{
    lane = $Lane
    tweak_id = $TweakId
    script = $entry.script
    script_path = $scriptPath
    args = @($entry.args)
} | ConvertTo-Json -Depth 4
