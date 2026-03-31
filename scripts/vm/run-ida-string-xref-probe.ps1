[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetBinary,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [Parameter(Mandatory = $true)]
    [string[]]$Patterns,

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging\ida-probes',
    [string]$GuestOutputRoot = 'C:\Tools\IdaProbes',
    [string]$GuestIdaRoot = 'C:\Tools\IDA'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptSource = Join-Path $repoRoot 'scripts\vm\ida\export_branch_analysis.py'
if (-not (Test-Path $scriptSource)) {
    throw "Missing IDA export script: $scriptSource"
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$hostScript = Join-Path $hostRoot 'export_branch_analysis.py'
$guestScript = Join-Path $guestRoot 'export_branch_analysis.py'
$hostState = Join-Path $hostRoot 'ida-state.json'
$guestState = Join-Path $guestRoot 'ida-state.json'
$hostEvidence = Join-Path $hostRoot 'evidence.json'
$guestEvidence = Join-Path $guestRoot 'evidence.json'
$patternPayload = ($Patterns | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join '|||'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
Copy-Item -Path $scriptSource -Destination $hostScript -Force

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)
    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }
    return $output.Trim()
}

function Write-BlockedEvidence {
    param([string]$Status, [string]$Reason)
    $payload = [ordered]@{
        binary = [IO.Path]::GetFileName($TargetBinary)
        probe = $OutputName
        timestamp = [DateTime]::UtcNow.ToString('o')
        pdb_source = $null
        pdb_loaded = $false
        status = $Status
        failure = $Reason
        matches = @()
    }
    $payload | ConvertTo-Json -Depth 6 | Set-Content -Path $hostEvidence -Encoding UTF8
    return $payload
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null

$stateCommand = @"
$paths = @(
  (Join-Path '$GuestIdaRoot' 'idat64.exe'),
  'C:\Program Files\IDA Pro\idat64.exe',
  'C:\Program Files\IDA Professional\idat64.exe',
  'C:\Program Files\Hex-Rays\idat64.exe'
)
$resolved = $paths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $resolved) {
  $resolved = (Get-ChildItem -Path 'C:\Program Files', 'C:\Tools' -Recurse -Filter 'idat64.exe' -ErrorAction SilentlyContinue | Select-Object -First 1 | ForEach-Object { $_.FullName })
}
[ordered]@{ guest_idat64_path = $resolved } | ConvertTo-Json -Depth 4 | Set-Content -Path '$guestState' -Encoding UTF8
"@

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $stateCommand
) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestState, $hostState) -IgnoreExitCode | Out-Null

$idaGuestPath = $null
if (Test-Path $hostState) {
    try {
        $state = Get-Content -Path $hostState -Raw | ConvertFrom-Json
        $idaGuestPath = [string]$state.guest_idat64_path
    }
    catch {
    }
}

if ([string]::IsNullOrWhiteSpace($idaGuestPath)) {
    Write-BlockedEvidence -Status 'blocked-ida-missing' -Reason 'idat64.exe was not found in the canonical guest IDA root.' | Out-Null
    Get-Content -Path $hostEvidence -Raw
    exit 0
}

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScript, $guestScript) | Out-Null

$guestCommand = @"
$output = '$guestEvidence'
$probe = '$OutputName'
$pdbSource = '$GuestIdaRoot'
$patterns = '$patternPayload' -split '\|\|\|' | Where-Object { $_ }
$args = @('-A', '-S""' + $guestScript + ' ' + $output + ' ' + $probe + ' ' + $pdbSource + ' ' + ($patterns -join ' ') + '""', '$TargetBinary')
$proc = Start-Process -FilePath '$idaGuestPath' -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
exit $proc.ExitCode
"@

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $guestCommand
) -IgnoreExitCode | Out-Null

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestEvidence, $hostEvidence) -IgnoreExitCode | Out-Null

if (-not (Test-Path $hostEvidence)) {
    Write-BlockedEvidence -Status 'blocked-ida-run-failed' -Reason 'IDA run did not produce evidence.json in the guest output root.' | Out-Null
}

Get-Content -Path $hostEvidence -Raw
