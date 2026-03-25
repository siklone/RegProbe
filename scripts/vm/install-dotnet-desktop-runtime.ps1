[CmdletBinding()]
param(
    [string]$InstallerPath = 'C:\Tools\Inbound\windowsdesktop-runtime-8.0.23-win-x64.exe',
    [string]$OutputRoot = 'C:\Tools\ValidationController\smoke',
    [string]$AppExePath = 'C:\Tools\AppSmoke\WindowsOptimizer.App.exe'
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    installer_path = $InstallerPath
    app_exe_path = $AppExePath
    installer_exists = [bool](Test-Path $InstallerPath)
}

$logPath = Join-Path $OutputRoot 'dotnet-desktop-runtime-install.log'
$resultPath = Join-Path $OutputRoot 'dotnet-desktop-runtime-install.json'

try {
    if (-not $result.installer_exists) {
        throw "Installer not found at $InstallerPath"
    }

    $beforeDesktop = @()
    $desktopRoot = 'C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App'
    if (Test-Path $desktopRoot) {
        $beforeDesktop = @(Get-ChildItem -Path $desktopRoot -Name | Sort-Object)
    }

    $proc = Start-Process -FilePath $InstallerPath -ArgumentList @('/install', '/quiet', '/norestart', '/log', $logPath) -PassThru -Wait

    $afterDesktop = @()
    if (Test-Path $desktopRoot) {
        $afterDesktop = @(Get-ChildItem -Path $desktopRoot -Name | Sort-Object)
    }

    $result.exit_code = $proc.ExitCode
    $result.log_path = $logPath
    $result.desktop_runtime_root = $desktopRoot
    $result.desktop_runtime_before = $beforeDesktop
    $result.desktop_runtime_after = $afterDesktop
    $result.desktop_runtime_installed = [bool](Test-Path $desktopRoot)
    $result.app_exe_exists = [bool](Test-Path $AppExePath)
    $result.success = ($proc.ExitCode -eq 0 -or $proc.ExitCode -eq 3010)
}
catch {
    $result.success = $false
    $result.error = $_.Exception.Message
    if (Test-Path $desktopRoot) {
        $result.desktop_runtime_after = @(Get-ChildItem -Path $desktopRoot -Name | Sort-Object)
        $result.desktop_runtime_installed = $true
    }
}

$result | ConvertTo-Json -Depth 6 | Set-Content -Path $resultPath -Encoding UTF8
Write-Output $resultPath
