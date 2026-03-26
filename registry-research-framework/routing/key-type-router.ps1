[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath
)

$normalized = $RegistryPath.ToLowerInvariant()
$layer = "user-mode"

if ($normalized.StartsWith("hklm\system\setup\")) {
    $layer = "boot"
} elseif ($normalized.Contains("\services\") -or $normalized.Contains("\parameters\")) {
    $layer = "driver"
} elseif ($normalized.StartsWith("hklm\system\currentcontrolset\") -or $normalized.StartsWith("hklm\system\controlset001\")) {
    $layer = "kernel"
}

[pscustomobject]@{
    registry_path = $RegistryPath
    suspected_layer = $layer
    boot_phase_relevant = $layer -in @("boot", "kernel", "driver")
} | ConvertTo-Json -Depth 4
