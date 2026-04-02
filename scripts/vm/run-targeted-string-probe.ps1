[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CandidateId,

    [Parameter(Mandatory = $true)]
    [string]$Needle,

    [Parameter(Mandatory = $true)]
    [string[]]$Binaries,

    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$ProbePrefix = 'targeted-string-probe'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
$repoEvidenceBase = Resolve-TrackedVmOutputRoot -VmProfile $VmProfile -Fallback (Join-Path $repoRoot 'evidence\files\vm-tooling-staging')
$hostStagingBase = Resolve-HostStagingRoot -VmProfile $VmProfile
$guestScriptRoot = Resolve-GuestScriptRoot -VmProfile $VmProfile
$guestDiagBase = Resolve-GuestDiagRoot -VmProfile $VmProfile

if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile }
if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$safeCandidate = ($CandidateId -replace '[^a-zA-Z0-9\-\.]', '-')
$probeName = "$ProbePrefix-$vmProfileTag-$stamp"
$repoOutputRoot = Join-Path $repoEvidenceBase $probeName
$hostWorkRoot = Join-Path $hostStagingBase $probeName
$guestRoot = Join-Path $guestDiagBase $probeName
$guestPayloadPath = Join-Path $guestScriptRoot "$ProbePrefix.guest.ps1"
$hostPayloadPath = Join-Path $hostWorkRoot "$ProbePrefix.guest.ps1"
$hostManifestPath = Join-Path $hostWorkRoot 'manifest.json'
$guestManifestPath = Join-Path $guestRoot 'manifest.json'
$hostSummaryPath = Join-Path $hostWorkRoot 'summary.json'
$hostResultsPath = Join-Path $hostWorkRoot 'results.json'
$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'

New-Item -ItemType Directory -Path $repoOutputRoot, $hostWorkRoot -Force | Out-Null

$manifest = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    candidate_id = $CandidateId
    needle = $Needle
    binaries = @($Binaries)
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $hostManifestPath -Encoding UTF8
Copy-Item -Path $hostManifestPath -Destination (Join-Path $repoOutputRoot 'manifest.json') -Force

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,
    [Parameter(Mandatory = $true)]
    [string]$ResultsPath,
    [Parameter(Mandatory = $true)]
    [string]$SummaryPath
)

$ErrorActionPreference = 'Stop'

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

function Get-BinaryHit {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Needle,
        [Parameter(Mandatory = $true)]
        [string]$Candidate
    )

    $exists = Test-Path -LiteralPath $Candidate
    if (-not $exists) {
        return [ordered]@{
            path = $Candidate
            exists = $false
            size = 0
            ascii_match = $false
            unicode_match = $false
        }
    }

    $bytes = [System.IO.File]::ReadAllBytes($Candidate)
    $ascii = [System.Text.Encoding]::ASCII.GetBytes($Needle)
    $unicode = [System.Text.Encoding]::Unicode.GetBytes($Needle)

    return [ordered]@{
        path = $Candidate
        exists = $true
        size = $bytes.Length
        ascii_match = Test-BytePattern -Data $bytes -Pattern $ascii
        unicode_match = Test-BytePattern -Data $bytes -Pattern $unicode
    }
}

$manifest = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json
$hits = @(
    foreach ($binary in @($manifest.binaries)) {
        [pscustomobject](Get-BinaryHit -Needle ([string]$manifest.needle) -Candidate ([string]$binary))
    }
)

$results = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    candidate_id = $manifest.candidate_id
    needle = $manifest.needle
    hit_count = @($hits | Where-Object { $_.ascii_match -or $_.unicode_match }).Count
    binaries = $hits
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    candidate_id = $manifest.candidate_id
    needle = $manifest.needle
    total_binaries = @($hits).Count
    hit_count = $results.hit_count
    hit_binaries = @($hits | Where-Object { $_.ascii_match -or $_.unicode_match } | ForEach-Object { $_.path })
    status = if ($results.hit_count -gt 0) { 'hit' } else { 'no-hit' }
}

$results | ConvertTo-Json -Depth 6 | Set-Content -Path $ResultsPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $SummaryPath -Encoding UTF8
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

function Ensure-VmStarted {
    $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') -IgnoreExitCode
    if ($running -notmatch [Regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') -IgnoreExitCode | Out-Null
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

    throw 'Guest did not return to a running VMware Tools state in time.'
}

function Ensure-GuestDirectory {
    param([string]$GuestPath)

    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $GuestPath) | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'already exists') {
            throw
        }
    }
}

function Copy-ToGuest([string]$HostPath, [string]$GuestPath) {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath) | Out-Null
}

function Copy-FromGuest([string]$GuestPath, [string]$HostPath) {
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath) | Out-Null
}

Ensure-VmStarted
Wait-GuestReady
Ensure-GuestDirectory $guestScriptRoot
Ensure-GuestDirectory $guestRoot
Copy-ToGuest $hostPayloadPath $guestPayloadPath
Copy-ToGuest $hostManifestPath $guestManifestPath

$guestResultsPath = Join-Path $guestRoot 'results.json'
$guestSummaryPath = Join-Path $guestRoot 'summary.json'
$guestErrorPath = Join-Path $guestRoot 'error.txt'
$hostErrorPath = Join-Path $hostWorkRoot 'error.txt'
$encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes(@"
try {
    Remove-Item -LiteralPath '$guestErrorPath' -Force -ErrorAction SilentlyContinue
    & '$guestPayloadPath' -ManifestPath '$guestManifestPath' -ResultsPath '$guestResultsPath' -SummaryPath '$guestSummaryPath'
    exit 0
}
catch {
    `$message = if (`$_.Exception -and `$_.Exception.Message) { `$_.Exception.Message } else { [string]`$_ }
    Set-Content -Path '$guestErrorPath' -Value `$message -Encoding UTF8
    exit 1
}
"@))

$runOutput = Invoke-Vmrun -Arguments @(
    '-T', 'ws',
    '-gu', $GuestUser,
    '-gp', $GuestPassword,
    'runProgramInGuest',
    $VmPath,
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-EncodedCommand',
    $encoded
) -IgnoreExitCode

if ($LASTEXITCODE -ne 0) {
    try {
        Copy-FromGuest $guestErrorPath $hostErrorPath
        $guestError = Get-Content -Path $hostErrorPath -Raw -ErrorAction SilentlyContinue
    }
    catch {
        $guestError = $null
    }
    $detail = if (-not [string]::IsNullOrWhiteSpace($guestError)) { $guestError.Trim() } else { $runOutput }
    throw "Targeted string probe guest run failed: $detail"
}

Copy-FromGuest $guestResultsPath $hostResultsPath
Copy-FromGuest $guestSummaryPath $hostSummaryPath
Copy-Item -Path $hostResultsPath -Destination $repoResultsPath -Force
Copy-Item -Path $hostSummaryPath -Destination $repoSummaryPath -Force

Write-Output $repoSummaryPath

