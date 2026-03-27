# Publish script to create a single distribution folder (/publish_final)

Param(
    [string]$Runtime = 'win-x64'
)

Write-Output "Cleaning solution and creating Release publish for runtime: $Runtime"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot 'app\app.csproj'
if (-not (Test-Path $projectPath)) {
    throw "Project not found at $projectPath"
}

dotnet clean $projectPath

# Use a deterministic publish folder under repo root
$out = Join-Path -Path $repoRoot -ChildPath 'publish_final'
if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force -ErrorAction SilentlyContinue }

dotnet publish $projectPath -c Release -r $Runtime -o $out --self-contained false

Write-Output "Published to: $out"
