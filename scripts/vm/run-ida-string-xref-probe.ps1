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
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
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
$hostStateProbe = Join-Path $hostRoot 'probe-ida-state.ps1'
$guestStateProbe = Join-Path $guestRoot 'probe-ida-state.ps1'
$hostRunner = Join-Path $hostRoot 'run-ida-probe.ps1'
$guestRunner = Join-Path $guestRoot 'run-ida-probe.ps1'
$hostState = Join-Path $hostRoot 'ida-state.json'
$guestState = Join-Path $guestRoot 'ida-state.json'
$hostRunState = Join-Path $hostRoot 'ida-run-state.json'
$guestRunState = Join-Path $guestRoot 'ida-run-state.json'
$hostEvidence = Join-Path $hostRoot 'evidence.json'
$guestEvidence = Join-Path $guestRoot 'evidence.json'
$hostLog = Join-Path $hostRoot 'ida.log'
$guestLog = Join-Path $guestRoot 'ida.log'
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
    param(
        [string]$Status,
        [string]$Reason,
        [hashtable]$ExtraFields
    )
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
    if ($ExtraFields) {
        foreach ($key in @($ExtraFields.Keys)) {
            $payload[$key] = $ExtraFields[$key]
        }
    }
    $payload | ConvertTo-Json -Depth 6 | Set-Content -Path $hostEvidence -Encoding UTF8
    return $payload
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null

@"
`$paths = @(
  (Join-Path '$GuestIdaRoot' 'idat64.exe'),
  (Join-Path '$GuestIdaRoot' 'ida64.exe'),
  (Join-Path (Join-Path '$GuestIdaRoot' 'Freeware') 'idat64.exe'),
  (Join-Path (Join-Path '$GuestIdaRoot' 'Freeware') 'ida64.exe'),
  'C:\Program Files\IDA Pro\idat64.exe',
  'C:\Program Files\IDA Pro\ida64.exe',
  'C:\Program Files\IDA Professional\idat64.exe',
  'C:\Program Files\IDA Professional\ida64.exe',
  'C:\Program Files\Hex-Rays\idat64.exe',
  'C:\Program Files\Hex-Rays\ida64.exe'
)
`$resolved = `$paths | Where-Object { Test-Path `$_ } | Select-Object -First 1
if (-not `$resolved) {
  `$resolved = (Get-ChildItem -Path 'C:\Program Files', 'C:\Tools' -Recurse -Include 'idat64.exe','ida64.exe' -ErrorAction SilentlyContinue | Sort-Object FullName | Select-Object -First 1 | ForEach-Object { `$_.FullName })
}
`$licenses = @()
if (`$resolved) {
  `$root = Split-Path -Parent `$resolved
  foreach (`$path in @(
    (Join-Path `$root 'ida.hexlic'),
    (Join-Path `$root '*.hexlic'),
    (Join-Path `$env:APPDATA 'Hex-Rays\IDA Pro\ida.hexlic'),
    (Join-Path `$env:APPDATA 'Hex-Rays\IDA Pro\*.hexlic'),
    (Join-Path `$root 'ida.key')
  )) {
    `$licenses += @(Get-ChildItem -Path `$path -Force -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
  }
}
`$licensePath = `$null
if (@(`$licenses).Count -gt 0) {
  `$licensePath = @(`$licenses | Select-Object -Unique | Select-Object -First 1)[0]
}
[ordered]@{
  guest_ida_path = `$resolved
  guest_idat64_path = if (`$resolved -and `$resolved -match 'idat64\.exe$') { `$resolved } else { `$null }
  license_path = `$licensePath
  execution_mode = if (`$resolved -and `$resolved -match 'idat64\.exe$') { 'idat64' } elseif (`$resolved) { 'ida64-fallback' } else { `$null }
} | ConvertTo-Json -Depth 4 | Set-Content -Path '$guestState' -Encoding UTF8
"@ | Set-Content -Path $hostStateProbe -Encoding UTF8

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $hostStateProbe, $guestStateProbe
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $guestStateProbe
) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestState, $hostState) -IgnoreExitCode | Out-Null

$idaGuestPath = $null
$licenseGuestPath = $null
$executionMode = $null
if (Test-Path $hostState) {
    try {
        $state = Get-Content -Path $hostState -Raw | ConvertFrom-Json
        $idaGuestPath = [string]$state.guest_ida_path
        $licenseGuestPath = [string]$state.license_path
        $executionMode = [string]$state.execution_mode
    }
    catch {
    }
}

if ([string]::IsNullOrWhiteSpace($idaGuestPath)) {
    Write-BlockedEvidence -Status 'blocked-ida-missing' -Reason 'Neither idat64.exe nor ida64.exe was found in the canonical guest IDA root.' | Out-Null
    Get-Content -Path $hostEvidence -Raw
    exit 0
}

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostScript, $guestScript) | Out-Null
@"
param(
    [Parameter(Mandatory = `$true)]
    [string]`$IdaPath,
    [Parameter(Mandatory = `$true)]
    [string]`$GuestScript,
    [Parameter(Mandatory = `$true)]
    [string]`$OutputFile,
    [Parameter(Mandatory = `$true)]
    [string]`$RunStateFile,
    [Parameter(Mandatory = `$true)]
    [string]`$LogFile,
    [Parameter(Mandatory = `$true)]
    [string]`$ProbeName,
    [Parameter(Mandatory = `$true)]
    [string]`$PdbSource,
    [Parameter(Mandatory = `$true)]
    [string]`$PatternPayload,
    [Parameter(Mandatory = `$true)]
    [string]`$TargetBinary,
    [string]`$LicensePath = ''
)

`$ErrorActionPreference = 'Stop'
if (Test-Path `$OutputFile) { Remove-Item `$OutputFile -Force -ErrorAction SilentlyContinue }
if (Test-Path `$LogFile) { Remove-Item `$LogFile -Force -ErrorAction SilentlyContinue }
if (Test-Path `$RunStateFile) { Remove-Item `$RunStateFile -Force -ErrorAction SilentlyContinue }

`$payload = [ordered]@{
    ida_path = `$IdaPath
    execution_mode = if (`$IdaPath -match 'idat64\.exe$') { 'idat64' } else { 'ida64-fallback' }
    license_path = if ([string]::IsNullOrWhiteSpace(`$LicensePath)) { `$null } else { `$LicensePath }
    exit_code = `$null
    evidence_exists = `$false
    log_exists = `$false
    log_tail = @()
    status = 'not-run'
    error = `$null
}

try {
    `$patterns = `$PatternPayload -split '\|\|\|' | Where-Object { `$_ }
    `$scriptArgs = @(`$GuestScript, `$OutputFile, `$ProbeName, `$PdbSource) + `$patterns
    `$scriptPayload = (`$scriptArgs | ForEach-Object { '"' + (`$_ -replace '"', '\"') + '"' }) -join ' '
    `$args = @('-A', ('-L' + `$LogFile), ('-S' + `$scriptPayload), `$TargetBinary)
    if (-not [string]::IsNullOrWhiteSpace(`$LicensePath)) {
        `$normalizedLicense = `$LicensePath.Replace('\', '/')
        `$args = @('-A', ('-Olicense:keyfile=' + `$normalizedLicense + ':setpref'), ('-L' + `$LogFile), ('-S' + `$scriptPayload), `$TargetBinary)
        `$env:IDA_LICENSE = 'keyfile=' + `$normalizedLicense + ':setpref'
    }

    `$proc = Start-Process -FilePath `$IdaPath -ArgumentList `$args -Wait -PassThru -WindowStyle Hidden
    `$payload.exit_code = `$proc.ExitCode
    `$payload.evidence_exists = (Test-Path `$OutputFile)
    `$payload.log_exists = (Test-Path `$LogFile)
    if (`$payload.log_exists) {
        `$payload.log_tail = [string[]](Get-Content -Path `$LogFile -Tail 40)
    }

    `$payload.status = 'ok'
    if (-not `$payload.evidence_exists) {
        `$payload.status = 'failed'
        if ((`$payload.log_tail -join [Environment]::NewLine) -match 'License not yet accepted, cannot run in batch mode') {
            `$payload.status = 'blocked-license-not-accepted'
        }
    }
}
catch {
    `$payload.status = 'failed'
    `$payload.error = `$_.Exception.Message
    `$payload.evidence_exists = (Test-Path `$OutputFile)
    `$payload.log_exists = (Test-Path `$LogFile)
    if (`$payload.log_exists) {
        `$payload.log_tail = [string[]](Get-Content -Path `$LogFile -Tail 40)
    }
}

`$payload | ConvertTo-Json -Depth 6 | Set-Content -Path `$RunStateFile -Encoding UTF8
"@ | Set-Content -Path $hostRunner -Encoding UTF8

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $hostRunner, $guestRunner) | Out-Null

Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $guestRunner,
    '-IdaPath', $idaGuestPath,
    '-GuestScript', $guestScript,
    '-OutputFile', $guestEvidence,
    '-RunStateFile', $guestRunState,
    '-LogFile', $guestLog,
    '-ProbeName', $OutputName,
    '-PdbSource', $GuestIdaRoot,
    '-PatternPayload', $patternPayload,
    '-TargetBinary', $TargetBinary,
    '-LicensePath', $licenseGuestPath
) -IgnoreExitCode | Out-Null

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestRunState, $hostRunState) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestLog, $hostLog) -IgnoreExitCode | Out-Null
Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $guestEvidence, $hostEvidence) -IgnoreExitCode | Out-Null

if (-not (Test-Path $hostEvidence)) {
    $runState = $null
    if (Test-Path $hostRunState) {
        try {
            $runState = Get-Content -Path $hostRunState -Raw | ConvertFrom-Json
        }
        catch {
        }
    }

    $reason = 'IDA run did not produce evidence.json in the guest output root.'
    $status = 'blocked-ida-run-failed'
    $extra = @{
        ida_path = $idaGuestPath
        execution_mode = $executionMode
        license_path = $licenseGuestPath
    }
    if ($runState) {
        $extra.ida_exit_code = $runState.exit_code
        if ($runState.log_tail) {
            $extra.log_tail = @($runState.log_tail)
        }
        if ($runState.status -eq 'blocked-license-not-accepted') {
            $status = 'blocked-license-not-accepted'
            $reason = 'ida64.exe is installed and licensed, but the guest has not accepted the IDA license/EULA state required for batch mode.'
        }
        elseif ($runState.status -eq 'failed') {
            $reason = "IDA exited with code $($runState.exit_code) without producing evidence.json."
        }
    }
    Write-BlockedEvidence -Status $status -Reason $reason -ExtraFields $extra | Out-Null
}

Get-Content -Path $hostEvidence -Raw

