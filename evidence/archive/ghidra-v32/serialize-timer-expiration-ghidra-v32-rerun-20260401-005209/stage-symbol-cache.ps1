param(
    [string]$SourceRoot,
    [string]$DestinationRoot,
    [string]$ResultPath
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path (Split-Path -Parent $ResultPath) -Force | Out-Null
$payload = [ordered]@{
    source_root = $SourceRoot
    destination_root = $DestinationRoot
    source_visible = (Test-Path $SourceRoot)
    status = 'not-run'
    pdb_count = 0
    error = $null
}
try {
    if (-not $payload.source_visible) { throw 'shared symbol cache is not visible in the guest' }
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $SourceRoot '*') -Destination $DestinationRoot -Recurse -Force
    $payload.pdb_count = @(
        Get-ChildItem -Path $DestinationRoot -Recurse -Include '*.pdb','*.pd_' -ErrorAction SilentlyContinue
    ).Count
    $payload.status = if ($payload.pdb_count -gt 0) { 'staged' } else { 'failed-no-pdb' }
}
catch {
    $payload.status = 'failed'
    $payload.error = $_.Exception.Message
}
$payload | ConvertTo-Json -Depth 6 | Set-Content -Path $ResultPath -Encoding UTF8
