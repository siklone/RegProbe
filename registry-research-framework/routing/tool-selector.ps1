[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath
)

$router = Join-Path $PSScriptRoot "key-type-router.ps1"
$route = (& $router -RegistryPath $RegistryPath) | ConvertFrom-Json

$runtime = @("etw", "procmon")
$static = @("bingrep", "floss", "capa", "ghidra_pdb")
$behavior = @("typeperf", "registry_sideeffects")

switch ($route.suspected_layer) {
    "boot" {
        $runtime = @("etw", "wpr_boot")
        $static = @("bingrep", "floss", "capa", "ghidra_pdb")
        $behavior = @("wpr_boot", "registry_sideeffects")
    }
    "kernel" {
        $runtime = @("etw", "dtrace", "procmon")
        $behavior = @("typeperf", "wpr_boot", "registry_sideeffects")
    }
    "driver" {
        $runtime = @("etw", "dtrace", "procmon")
        $behavior = @("typeperf", "wpr_boot", "registry_sideeffects")
    }
}

[pscustomobject]@{
    registry_path = $RegistryPath
    suspected_layer = $route.suspected_layer
    runtime = $runtime
    static = $static
    behavior = $behavior
} | ConvertTo-Json -Depth 4
