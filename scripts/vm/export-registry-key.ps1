[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging\registry-dumps',
    [string]$GuestOutputRoot = 'C:\Tools\RegistryDumps'
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$hostRunner = Join-Path $hostRoot 'export-registry-key-inner.ps1'
$guestRunner = Join-Path $guestRoot 'export-registry-key-inner.ps1'
$hostQuery = Join-Path $hostRoot "$OutputName.txt"
$guestQuery = Join-Path $guestRoot "$OutputName.txt"
$hostReg = Join-Path $hostRoot "$OutputName.reg"
$guestReg = Join-Path $guestRoot "$OutputName.reg"
$hostMeta = Join-Path $hostRoot 'metadata.json'
$guestMeta = Join-Path $guestRoot 'metadata.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$innerScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$RegistryPath,

    [Parameter(Mandatory = $true)]
    [string]$QueryOutputPath,

    [Parameter(Mandatory = $true)]
    [string]$RegOutputPath,

    [Parameter(Mandatory = $true)]
    [string]$MetadataPath
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path (Split-Path -Parent $QueryOutputPath) -Force | Out-Null

$queryOutput = & reg.exe query $RegistryPath /s 2>&1 | Out-String
$queryExitCode = $LASTEXITCODE
$queryOutput | Set-Content -Path $QueryOutputPath -Encoding UTF8

$exportOutput = & reg.exe export $RegistryPath $RegOutputPath /y 2>&1 | Out-String
$exportExitCode = $LASTEXITCODE

$lastBoot = (Get-CimInstance -ClassName Win32_OperatingSystem).LastBootUpTime.ToString('o')
$metadata = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    registry_path = $RegistryPath
    query_exit_code = $queryExitCode
    export_exit_code = $exportExitCode
    query_output_path = $QueryOutputPath
    reg_output_path = $RegOutputPath
    reg_output_exists = [bool](Test-Path $RegOutputPath)
    guest_last_boot = $lastBoot
    export_stdout = $exportOutput.Trim()
}

$metadata | ConvertTo-Json -Depth 6 | Set-Content -Path $MetadataPath -Encoding UTF8
'@

Set-Content -Path $hostRunner -Value $innerScript -Encoding UTF8

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 3
    }

    throw 'Guest is not ready for vmrun guest operations.'
}

Wait-GuestReady

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostRunner, $guestRunner) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $guestRunner,
    '-RegistryPath',
    $RegistryPath,
    '-QueryOutputPath',
    $guestQuery,
    '-RegOutputPath',
    $guestReg,
    '-MetadataPath',
    $guestMeta
) | Out-Null

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestQuery, $hostQuery) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestMeta, $hostMeta) | Out-Null

$meta = Get-Content -Path $hostMeta -Raw | ConvertFrom-Json
if ($meta.reg_output_exists) {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestReg, $hostReg) | Out-Null
}

$summary = [ordered]@{
    registry_path = $RegistryPath
    host_output_root = $hostRoot
    query_output = $hostQuery
    reg_output = if (Test-Path $hostReg) { $hostReg } else { $null }
    metadata = $hostMeta
}

$summary | ConvertTo-Json -Depth 6
