param(
    [Parameter(Mandatory = $true)]
    [string]$WriteTestPath,
    [Parameter(Mandatory = $true)]
    [string]$EnvironmentPath,
    [Parameter(Mandatory = $true)]
    [string]$ScriptResultPath
)

$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $WriteTestPath
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

"SECONDARY_VM_OK $([DateTime]::UtcNow.ToString('o'))" | Set-Content -Path $WriteTestPath -Encoding UTF8

$environment = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    write_test_exists = [bool](Test-Path $WriteTestPath)
    execution_policy = (Get-ExecutionPolicy).ToString()
    ps_version = $PSVersionTable.PSVersion.ToString()
    user_is_administrator = ([bool]([System.Security.Principal.WindowsPrincipal] [System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator))
    procmon_present = [bool](Test-Path 'C:\Tools\Sysinternals\Procmon64.exe')
    defender_disable_realtime = (Get-MpPreference).DisableRealtimeMonitoring
}
$environment | ConvertTo-Json -Depth 6 | Set-Content -Path $EnvironmentPath -Encoding UTF8

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = 'ok'
    write_test_path = $WriteTestPath
    environment_path = $EnvironmentPath
} | ConvertTo-Json -Depth 6 | Set-Content -Path $ScriptResultPath -Encoding UTF8
