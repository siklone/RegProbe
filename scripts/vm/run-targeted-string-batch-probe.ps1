[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$ProbePrefix = 'targeted-string-batch'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

if (-not (Test-Path -LiteralPath $ManifestPath)) {
    throw "Manifest not found: $ManifestPath"
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if (-not $manifest.candidates -or @($manifest.candidates).Count -eq 0) {
    throw "Manifest does not contain any candidates: $ManifestPath"
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
$repoEvidenceBase = Resolve-TrackedVmOutputRoot -VmProfile $VmProfile -Fallback (Join-Path $repoRoot 'evidence\files\vm-tooling-staging')
$hostStagingBase = Resolve-HostStagingRoot -VmProfile $VmProfile
$guestScriptRoot = Resolve-GuestScriptRoot -VmProfile $VmProfile
$guestDiagBase = Resolve-GuestDiagRoot -VmProfile $VmProfile

if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile }
if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
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
Copy-Item -LiteralPath $ManifestPath -Destination $hostManifestPath -Force
Copy-Item -LiteralPath $ManifestPath -Destination (Join-Path $repoOutputRoot 'manifest.json') -Force

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

function Get-StringsCommandPath {
    $command = Get-Command strings.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    foreach ($candidate in @(
        'C:\Tools\strings.exe',
        'C:\Tools\Sysinternals\strings.exe',
        'C:\Windows\System32\strings.exe'
    )) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

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

function New-StringSet {
    return [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
}

function Get-ExtractedStringSets {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StringsExe,
        [Parameter(Mandatory = $true)]
        [string]$BinaryPath
    )

    $asciiSet = New-StringSet
    $wideSet = New-StringSet

    $asciiOutput = & $StringsExe -nobanner -accepteula -e a $BinaryPath 2>$null
    foreach ($line in @($asciiOutput)) {
        $text = [string]$line
        if (-not [string]::IsNullOrWhiteSpace($text)) {
            [void]$asciiSet.Add($text.Trim())
        }
    }

    $wideOutput = & $StringsExe -nobanner -accepteula -e l $BinaryPath 2>$null
    foreach ($line in @($wideOutput)) {
        $text = [string]$line
        if (-not [string]::IsNullOrWhiteSpace($text)) {
            [void]$wideSet.Add($text.Trim())
        }
    }

    return [pscustomobject]@{
        ascii = $asciiSet
        wide = $wideSet
    }
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$stringsExe = Get-StringsCommandPath

$binaryCache = @{}
$uniqueBinaries = @(
    $manifest.candidates |
        ForEach-Object { @($_.binaries) } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
)

foreach ($binary in $uniqueBinaries) {
    $binaryPath = [string]$binary
    if (-not (Test-Path -LiteralPath $binaryPath)) {
        $binaryCache[$binaryPath] = [pscustomobject]@{
            path = $binaryPath
            exists = $false
            size = 0
            match_backend = 'missing'
            strings_error = 'missing'
            ascii = $null
            wide = $null
            bytes = $null
        }
        continue
    }

    try {
        $info = Get-Item -LiteralPath $binaryPath
        $backend = 'byte-scan'
        $sets = $null
        $bytes = $null

        if ($stringsExe) {
            $sets = Get-ExtractedStringSets -StringsExe $stringsExe -BinaryPath $binaryPath
            $backend = 'strings'
        }
        else {
            $bytes = [System.IO.File]::ReadAllBytes($binaryPath)
        }

        $binaryCache[$binaryPath] = [pscustomobject]@{
            path = $binaryPath
            exists = $true
            size = [int64]$info.Length
            match_backend = $backend
            strings_error = $null
            ascii = if ($sets) { $sets.ascii } else { $null }
            wide = if ($sets) { $sets.wide } else { $null }
            bytes = $bytes
        }
    }
    catch {
        $message = if ($_.Exception -and $_.Exception.Message) { $_.Exception.Message } else { [string]$_ }
        $binaryCache[$binaryPath] = [pscustomobject]@{
            path = $binaryPath
            exists = $true
            size = 0
            match_backend = 'error'
            strings_error = $message
            ascii = $null
            wide = $null
            bytes = $null
        }
    }
}

$candidateResults = @(
    foreach ($candidate in @($manifest.candidates)) {
        $needle = [string]$candidate.needle
        $binaryResults = @(
            foreach ($binaryPath in @($candidate.binaries)) {
                $meta = $binaryCache[[string]$binaryPath]
                $asciiMatch = $false
                $wideMatch = $false

                if ($meta.exists -and -not $meta.strings_error) {
                    if ($meta.match_backend -eq 'strings') {
                        $asciiMatch = $meta.ascii.Contains($needle)
                        $wideMatch = $meta.wide.Contains($needle)
                    }
                    else {
                        $asciiBytes = [System.Text.Encoding]::ASCII.GetBytes($needle)
                        $wideBytes = [System.Text.Encoding]::Unicode.GetBytes($needle)
                        $asciiMatch = Test-BytePattern -Data $meta.bytes -Pattern $asciiBytes
                        $wideMatch = Test-BytePattern -Data $meta.bytes -Pattern $wideBytes
                    }
                }

                [pscustomobject]@{
                    path = [string]$binaryPath
                    exists = [bool]$meta.exists
                    size = [int64]$meta.size
                    match_backend = $meta.match_backend
                    ascii_match = [bool]$asciiMatch
                    unicode_match = [bool]$wideMatch
                    strings_error = $meta.strings_error
                }
            }
        )

        $hitBinaries = @(
            $binaryResults |
                Where-Object { $_.ascii_match -or $_.unicode_match } |
                ForEach-Object { $_.path }
        )

        [pscustomobject]@{
            candidate_id = [string]$candidate.candidate_id
            family = [string]$candidate.family
            route_bucket = [string]$candidate.route_bucket
            registry_path = [string]$candidate.registry_path
            needle = $needle
            total_binaries = @($binaryResults).Count
            hit_count = @($hitBinaries).Count
            hit_binaries = $hitBinaries
            status = if (@($hitBinaries).Count -gt 0) { 'hit' } else { 'no-hit' }
            binaries = $binaryResults
        }
    }
)

$familySummary = @(
    $candidateResults |
        Group-Object family |
        Sort-Object Name |
        ForEach-Object {
            [pscustomobject]@{
                family = $_.Name
                total = $_.Count
                hit_count = @($_.Group | Where-Object { $_.status -eq 'hit' }).Count
                no_hit_count = @($_.Group | Where-Object { $_.status -eq 'no-hit' }).Count
            }
        }
)

$results = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    strings_exe = $stringsExe
    total_candidates = @($candidateResults).Count
    candidates = $candidateResults
    families = $familySummary
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    strings_exe = $stringsExe
    total_candidates = @($candidateResults).Count
    hit_count = @($candidateResults | Where-Object { $_.status -eq 'hit' }).Count
    no_hit_count = @($candidateResults | Where-Object { $_.status -eq 'no-hit' }).Count
    hit_candidates = @($candidateResults | Where-Object { $_.status -eq 'hit' } | ForEach-Object { $_.candidate_id })
    no_hit_candidates = @($candidateResults | Where-Object { $_.status -eq 'no-hit' } | ForEach-Object { $_.candidate_id })
    families = $familySummary
}

$results | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ResultsPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $SummaryPath -Encoding UTF8
'@

Set-Content -LiteralPath $hostPayloadPath -Value $guestPayload -Encoding UTF8

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
    Set-Content -LiteralPath '$guestErrorPath' -Value `$message -Encoding UTF8
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
        $guestError = Get-Content -LiteralPath $hostErrorPath -Raw -ErrorAction SilentlyContinue
    }
    catch {
        $guestError = $null
    }
    $detail = if (-not [string]::IsNullOrWhiteSpace($guestError)) { $guestError.Trim() } else { $runOutput }
    throw "Targeted string batch guest run failed: $detail"
}

Copy-FromGuest $guestResultsPath $hostResultsPath
Copy-FromGuest $guestSummaryPath $hostSummaryPath
Copy-Item -LiteralPath $hostResultsPath -Destination $repoResultsPath -Force
Copy-Item -LiteralPath $hostSummaryPath -Destination $repoSummaryPath -Force

Write-Output $repoSummaryPath
