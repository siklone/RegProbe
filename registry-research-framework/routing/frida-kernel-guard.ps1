[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,
    [string]$Layer
)

$configPath = Join-Path (Split-Path -Parent $PSScriptRoot) "config\frida-kernel-guard-rules.json"
$config = Get-Content -Raw $configPath | ConvertFrom-Json
$normalized = $RegistryPath.ToLowerInvariant()
$resolvedLayer = if ($Layer) { $Layer } else { $null }
if (-not $resolvedLayer) {
    $router = Join-Path $PSScriptRoot "key-type-router.ps1"
    $resolvedLayer = ((& $router -RegistryPath $RegistryPath) | ConvertFrom-Json).suspected_layer
}

$blocked = $false
foreach ($prefix in $config.blocked_prefixes) {
    if ($normalized.StartsWith($prefix.ToLowerInvariant())) {
        $blocked = $true
    }
}
foreach ($fragment in $config.blocked_substrings) {
    if ($normalized.Contains($fragment.ToLowerInvariant())) {
        $blocked = $true
    }
}
if ($resolvedLayer -in @($config.blocked_layers)) {
    $blocked = $true
}

[pscustomobject]@{
    registry_path = $RegistryPath
    suspected_layer = $resolvedLayer
    frida_allowed = -not $blocked
    guard_reason = if ($blocked) { "KERNEL_GUARD" } else { "user-mode lane" }
} | ConvertTo-Json -Depth 4
