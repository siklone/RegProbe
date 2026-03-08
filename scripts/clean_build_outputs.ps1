# Cleans build output folders across the repository.
# Only removes auto-generated build outputs (bin/ obj/ publish/). Safe to run.

Param(
    [switch]$WhatIfMode = $true
)

Write-Output "Cleaning build outputs (safe): bin/, obj/, publish/"

$paths = Get-ChildItem -Path . -Recurse -Directory -Force | Where-Object {
    $_.Name -in @('bin','obj','publish')
}

foreach ($p in $paths) {
    $full = $p.FullName
    if ($WhatIfMode) {
        Write-Output "Would remove: $full"
    }
    else {
        Write-Output "Removing: $full"
        Remove-Item -LiteralPath $full -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Output "Done. Re-run with -WhatIfMode:$false to actually delete."
