param(
    [switch]$Apply
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

$targetSpecs = @(
    "evidence",
    "registry-research-framework\audit",
    "research\notes",
    "research\vm-incidents.json",
    "registry-research-framework\config\vm-baselines.json"
)

$rules = @(
    @{
        Pattern = 'C:\\Users\\Deniz\\AppData\\Local\\Temp\\'
        Replacement = '%LOCALAPPDATA%\\Temp\\'
    },
    @{
        Pattern = 'H:\\Yedek\\VMs\\Win25H2Clean\\Win25H2.vmx'
        Replacement = '<LOCAL_VM_ROOT>\\Win25H2Clean\\Win25H2.vmx'
    },
    @{
        Pattern = 'H:\\Temp\\vm-tooling-staging\\'
        Replacement = '<HOST_STAGING_ROOT>\\'
    },
    @{
        Pattern = 'H:\\D\\Dev\\RegProbe\\'
        Replacement = '<REPO_ROOT>\\'
    },
    @{
        Pattern = 'H:\\D\\Dev\\WPF-Windows-optimizer-with-safe-reversible-tweaks\\'
        Replacement = '<LEGACY_REPO_ROOT>\\'
    },
    @{
        Pattern = 'C:\\Users\\Administrator\\AppData\\Local\\'
        Replacement = '%GUEST_USERPROFILE%\\AppData\\Local\\'
    },
    @{
        Pattern = 'C:\\Users\\Administrator\\AppData\\Roaming\\'
        Replacement = '%GUEST_USERPROFILE%\\AppData\\Roaming\\'
    },
    @{
        Pattern = 'C:\\Users\\Administrator'
        Replacement = '%GUEST_USERPROFILE%'
    }
)

$files = New-Object System.Collections.Generic.List[string]
foreach ($spec in $targetSpecs) {
    $fullPath = Join-Path $repoRoot $spec
    if (-not (Test-Path $fullPath)) {
        continue
    }

    $item = Get-Item $fullPath
    if ($item.PSIsContainer) {
        Get-ChildItem -Path $fullPath -Recurse -File |
            Where-Object { $_.Extension -in ".json", ".md", ".txt" } |
            ForEach-Object { [void]$files.Add($_.FullName) }
    }
    else {
        [void]$files.Add($item.FullName)
    }
}

$changed = New-Object System.Collections.Generic.List[string]
foreach ($file in $files | Sort-Object -Unique) {
    $content = Get-Content -LiteralPath $file -Raw
    if ($null -eq $content) {
        $content = ""
    }
    $updated = $content

    foreach ($rule in $rules) {
        $updated = $updated.Replace($rule.Pattern, $rule.Replacement)
    }

    if ($updated -ne $content) {
        [void]$changed.Add($file)
        if ($Apply) {
            Set-Content -LiteralPath $file -Value $updated
        }
    }
}

if ($Apply) {
    Write-Host "Sanitized $($changed.Count) file(s)."
}
else {
    Write-Host "Would sanitize $($changed.Count) file(s):"
    $changed | ForEach-Object { Write-Host $_ }
}
