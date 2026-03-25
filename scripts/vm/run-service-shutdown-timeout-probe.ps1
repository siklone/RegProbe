[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestScriptRoot = 'C:\Tools\Scripts',
    [string]$GuestOutputRoot = 'C:\Tools\Perf\Procmon'
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("wait-to-kill-service-timeout-probe-{0}" -f $stamp)
New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$hostScriptPath = Join-Path $hostRoot 'service-shutdown-timeout-probe.ps1'
$guestScriptPath = Join-Path $GuestScriptRoot 'service-shutdown-timeout-probe.ps1'
$guestJsonPath = Join-Path $GuestOutputRoot 'wait-to-kill-service-timeout-probe.json'
$guestTxtPath = Join-Path $GuestOutputRoot 'wait-to-kill-service-timeout-probe.txt'
$hostJsonPath = Join-Path $hostRoot 'wait-to-kill-service-timeout-probe.json'
$hostTxtPath = Join-Path $hostRoot 'wait-to-kill-service-timeout-probe.txt'

$guestScript = @'
param(
    [string]$OutputJson,
    [string]$OutputText
)

$ErrorActionPreference = 'Stop'
$path = 'HKLM:\SYSTEM\CurrentControlSet\Control'
$name = 'WaitToKillServiceTimeout'
$candidate = '2500'

function Get-State {
    try {
        $item = Get-ItemProperty -Path $path -Name $name -ErrorAction Stop
        return [ordered]@{
            path_exists = $true
            value_exists = $true
            value = [string]$item.$name
        }
    }
    catch {
        return [ordered]@{
            path_exists = [bool](Test-Path $path)
            value_exists = $false
            value = $null
        }
    }
}

$summary = [ordered]@{
    registry_path = 'HKLM\SYSTEM\CurrentControlSet\Control'
    value_name = $name
    candidate = $candidate
    original = $null
    after = $null
    restored = $null
    error = $null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('REGISTRY_PATH=HKLM\SYSTEM\CurrentControlSet\Control')
$lines.Add('VALUE_NAME=WaitToKillServiceTimeout')
$lines.Add('CANDIDATE=' + $candidate)

try {
    $original = Get-State
    $summary.original = $original
    $lines.Add('ORIGINAL=' + ($original | ConvertTo-Json -Compress))

    if (-not (Test-Path $path)) {
        New-Item -Path $path -Force | Out-Null
    }
    New-ItemProperty -Path $path -Name $name -PropertyType String -Value $candidate -Force | Out-Null
    $after = Get-State
    $summary.after = $after
    $lines.Add('AFTER=' + ($after | ConvertTo-Json -Compress))

    if ($original.value_exists) {
        New-ItemProperty -Path $path -Name $name -PropertyType String -Value ([string]$original.value) -Force | Out-Null
    }
    else {
        Remove-ItemProperty -Path $path -Name $name -ErrorAction SilentlyContinue
    }

    $restored = Get-State
    $summary.restored = $restored
    $lines.Add('RESTORED=' + ($restored | ConvertTo-Json -Compress))
}
catch {
    $summary.error = $_.Exception.Message
    $lines.Add('ERROR=' + $_.Exception.Message)
    if ($_.InvocationInfo) {
        $lines.Add('AT=' + $_.InvocationInfo.PositionMessage)
    }
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputJson) | Out-Null
$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputJson -Encoding UTF8
$lines | Set-Content -Path $OutputText -Encoding UTF8
'@

Set-Content -Path $hostScriptPath -Value $guestScript -Encoding UTF8

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Ensure-VmRunning {
    $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
    if ($running -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) | Out-Null
    }
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

Ensure-VmRunning
Wait-GuestReady

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScriptPath, $guestScriptPath) | Out-Null
try {
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        $guestScriptPath,
        '-OutputJson',
        $guestJsonPath,
        '-OutputText',
        $guestTxtPath
    ) | Out-Null
}
catch {
}

foreach ($pair in @(
    @{ Guest = $guestJsonPath; Host = $hostJsonPath },
    @{ Guest = $guestTxtPath; Host = $hostTxtPath }
)) {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $pair.Guest, $pair.Host) | Out-Null
}

Write-Output $hostRoot
