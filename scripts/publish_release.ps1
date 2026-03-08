# Publish script to create a single distribution folder (/publish_final)

Param(
    [string]$Runtime = 'win-x64'
)

Write-Output "Cleaning solution and creating Release publish for runtime: $Runtime"

dotnet clean

# Use a deterministic publish folder under repo root
$out = Join-Path -Path (Get-Location) -ChildPath 'publish_final'
if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force -ErrorAction SilentlyContinue }

dotnet publish -c Release -r $Runtime -o $out --self-contained false

Write-Output "Published to: $out"
