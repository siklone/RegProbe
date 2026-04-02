[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Needle,

    [Parameter(Mandatory = $true)]
    [string[]]$Candidates,

    [Parameter(Mandatory = $true)]
    [string]$OutputName,

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging\string-search',
    [string]$GuestOutputRoot = 'C:\Tools\StringSearch'
)

$ErrorActionPreference = 'Stop'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$hostRoot = Join-Path $HostOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$guestRoot = Join-Path $GuestOutputRoot ("{0}-{1}" -f $OutputName, $stamp)
$hostRunner = Join-Path $hostRoot 'search-guest-binary-string-inner.ps1'
$guestRunner = Join-Path $guestRoot 'search-guest-binary-string-inner.ps1'
$hostJson = Join-Path $hostRoot "$OutputName.json"
$guestJson = Join-Path $guestRoot "$OutputName.json"
$candidatePayload = ($Candidates | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join '|||'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null

$innerScript = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$Needle,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$CandidatePayload
)

$ErrorActionPreference = 'Stop'
$Candidates = @($CandidatePayload -split '\|\|\|' | Where-Object { $_ })

function Test-BytePattern {
    param(
        [byte[]]$Data,
        [byte[]]$Pattern
    )

    if (-not $Data -or -not $Pattern -or $Pattern.Length -eq 0 -or $Data.Length -lt $Pattern.Length) {
        return $false
    }

    for ($i = 0; $i -le ($Data.Length - $Pattern.Length); $i++) {
        $matched = $true
        for ($j = 0; $j -lt $Pattern.Length; $j++) {
            if ($Data[$i + $j] -ne $Pattern[$j]) {
                $matched = $false
                break
            }
        }

        if ($matched) {
            return $true
        }
    }

    return $false
}

$ascii = [System.Text.Encoding]::ASCII.GetBytes($Needle)
$unicode = [System.Text.Encoding]::Unicode.GetBytes($Needle)

$results = foreach ($candidate in $Candidates) {
    $exists = Test-Path $candidate
    $bytes = if ($exists) { [System.IO.File]::ReadAllBytes($candidate) } else { @() }
    [ordered]@{
        path = $candidate
        exists = $exists
        size = if ($exists) { $bytes.Length } else { 0 }
        ascii_match = if ($exists) { Test-BytePattern -Data $bytes -Pattern $ascii } else { $false }
        unicode_match = if ($exists) { Test-BytePattern -Data $bytes -Pattern $unicode } else { $false }
    }
}

[ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    needle = $Needle
    candidates = $results
} | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8
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

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostRunner, $guestRunner) | Out-Null

$vmrunArgs = @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $guestRunner,
    '-Needle', $Needle,
    '-OutputPath', $guestJson,
    '-CandidatePayload', $candidatePayload
)

Invoke-Vmrun -Arguments $vmrunArgs | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestJson, $hostJson) | Out-Null

[ordered]@{
    needle = $Needle
    host_output_root = $hostRoot
    json = $hostJson
    candidates = $Candidates
} | ConvertTo-Json -Depth 6

