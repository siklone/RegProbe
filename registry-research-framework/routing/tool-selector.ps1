[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,
    [string]$TweakId
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

$runtimeRunnerJson = $null
$behaviorRunnerJson = $null
if ($TweakId) {
    $resolver = Join-Path (Split-Path -Parent (Split-Path -Parent $router)) "tools\_resolve-tweak-runner.ps1"
    $runtimeRunnerJson = & $resolver -Lane runtime -TweakId $TweakId
    $behaviorRunnerJson = & $resolver -Lane behavior -TweakId $TweakId
}

[pscustomobject]@{
    registry_path = $RegistryPath
    tweak_id = $TweakId
    suspected_layer = $route.suspected_layer
    runtime = $runtime
    static = $static
    behavior = $behavior
    vm_runners = if ($TweakId) {
        [pscustomobject]@{
            runtime = if ($runtimeRunnerJson) { $runtimeRunnerJson | ConvertFrom-Json } else { $null }
            behavior = if ($behaviorRunnerJson) { $behaviorRunnerJson | ConvertFrom-Json } else { $null }
        }
    }
    else {
        $null
    }
} | ConvertTo-Json -Depth 4
