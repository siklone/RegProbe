[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SnapshotName = '',
    [string[]]$CandidateIds = @()
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1')

$vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
$hostStagingBase = Resolve-HostStagingRoot -VmProfile $VmProfile
$guestDiagBase = Resolve-GuestDiagRoot -VmProfile $VmProfile

if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile }
if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = Resolve-DefaultVmSnapshotName -VmProfile $VmProfile }
if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx' }
if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = 'RegProbe-Baseline-ToolsHardened-20260330' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "path-aware-static-$vmProfileTag-$stamp"
$repoOutputRoot = Join-Path $repoRoot "evidence\files\path-aware\$vmProfileTag\$probeName"
$hostWorkRoot = Join-Path $hostStagingBase $probeName
$guestProbeRoot = Join-Path $guestDiagBase $probeName
$ghidraProbeRoot = Join-Path $repoRoot 'evidence\files\ghidra'
$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'
$hostSummaryPath = Join-Path $hostWorkRoot 'summary.json'
$hostResultsPath = Join-Path $hostWorkRoot 'results.json'
$hostPythonPath = Join-Path $hostWorkRoot 'exact-string-scan.py'
$ghidraWrapper = Join-Path $repoRoot 'scripts\vm\run-ghidra-string-xref-probe.ps1'

New-Item -ItemType Directory -Path $repoOutputRoot, $hostWorkRoot -Force | Out-Null

$binaryDefinitions = [ordered]@{
    'ntoskrnl.exe' = 'C:\Windows\System32\ntoskrnl.exe'
    'ci.dll' = 'C:\Windows\System32\ci.dll'
    'winload.exe' = 'C:\Windows\System32\winload.exe'
    'kernelbase.dll' = 'C:\Windows\System32\KernelBase.dll'
    'advapi32.dll' = 'C:\Windows\System32\advapi32.dll'
    'sechost.dll' = 'C:\Windows\System32\sechost.dll'
    'services.exe' = 'C:\Windows\System32\services.exe'
    'lsass.exe' = 'C:\Windows\System32\lsass.exe'
}

$candidates = @(
    [ordered]@{
        candidate_id = 'policy.system.enable-virtualization'
        family = 'policy-system'
        registry_path = 'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM'
        value_name = 'EnableVirtualization'
        binaries = @('ntoskrnl.exe', 'ci.dll', 'winload.exe', 'kernelbase.dll', 'advapi32.dll', 'sechost.dll', 'services.exe', 'lsass.exe')
        context_needles = @(
            'EnableVirtualization',
            'SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System',
            'EnableLUA',
            'EnableInstallerDetection',
            'EnableVirtualizationBasedSecurity'
        )
        ghidra_patterns = @('EnableVirtualization', 'EnableLUA', 'EnableInstallerDetection')
        collision_needles = @('EnableVirtualizationBasedSecurity')
    },
    [ordered]@{
        candidate_id = 'system.io-allow-remote-dasd'
        family = 'session-manager-io'
        registry_path = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System'
        value_name = 'AllowRemoteDASD'
        binaries = @('ntoskrnl.exe', 'ci.dll', 'winload.exe', 'kernelbase.dll', 'advapi32.dll', 'sechost.dll', 'services.exe', 'lsass.exe')
        context_needles = @(
            'AllowRemoteDASD',
            'SYSTEM\CurrentControlSet\Control\Session Manager\I/O System',
            'SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices\AllowRemoteDASD',
            'RemovableStorageDevices'
        )
        ghidra_patterns = @('AllowRemoteDASD', 'RemovableStorageDevices')
        collision_needles = @('SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices\AllowRemoteDASD', 'RemovableStorageDevices')
    }
)

if (@($CandidateIds).Count -gt 0) {
    $wanted = @($CandidateIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $candidates = @($candidates | Where-Object { $wanted -contains $_.candidate_id })
    if (@($candidates).Count -eq 0) {
        throw "No path-aware candidates matched the requested ids: $($wanted -join ', ')"
    }
}

$pythonScript = @'
import json
import pathlib
import re
import sys

binary_path = pathlib.Path(sys.argv[1])
output_path = pathlib.Path(sys.argv[2])
needles_path = pathlib.Path(sys.argv[3])
needles = json.loads(needles_path.read_text(encoding="utf-8-sig"))

def extract_ascii_strings(data: bytes, minimum: int = 4):
    text = data.decode("latin1", errors="ignore")
    return re.findall(r"[\x20-\x7e]{%d,}" % minimum, text)

def extract_utf16le_strings(data: bytes, minimum: int = 4):
    text = data.decode("utf-16le", errors="ignore")
    return re.findall(r"[\x20-\x7e]{%d,}" % minimum, text)

raw = binary_path.read_bytes()
ascii_strings = extract_ascii_strings(raw)
wide_strings = extract_utf16le_strings(raw)

result = {
    "binary": str(binary_path),
    "size": len(raw),
    "minimum_length": 4,
    "ascii_total": len(ascii_strings),
    "wide_total": len(wide_strings),
    "needles": [],
}

for needle in needles:
    ascii_hits = [item for item in ascii_strings if item == needle]
    wide_hits = [item for item in wide_strings if item == needle]
    result["needles"].append(
        {
            "needle": needle,
            "ascii_hits": len(ascii_hits),
            "wide_hits": len(wide_hits),
            "ascii_samples": ascii_hits[:5],
            "wide_samples": wide_hits[:5],
        }
    )

output_path.write_text(json.dumps(result, indent=2), encoding="utf-8")
'@

Set-Content -Path $hostPythonPath -Value $pythonScript -Encoding UTF8

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
    if ($running -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') -IgnoreExitCode | Out-Null
    }
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 600)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $tools = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($tools -match 'running|installed') {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
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

function Copy-FromGuest {
    param([string]$GuestPath, [string]$HostPath)

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath) | Out-Null
}

function Invoke-ProcessCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [int]$TimeoutSeconds = 0
    )

    $stdout = [System.IO.Path]::GetTempFileName()
    $stderr = [System.IO.Path]::GetTempFileName()
    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $timedOut = $false
        if ($TimeoutSeconds -gt 0) {
            Wait-Process -Id $proc.Id -Timeout $TimeoutSeconds -ErrorAction SilentlyContinue
            if (-not $proc.HasExited) {
                $timedOut = $true
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                $proc.WaitForExit()
            }
        }
        else {
            $proc.WaitForExit()
        }
        return [ordered]@{
            exit_code = $proc.ExitCode
            timed_out = $timedOut
            stdout = if (Test-Path -Path $stdout) { (Get-Content -Path $stdout -Raw -ErrorAction SilentlyContinue) } else { '' }
            stderr = if (Test-Path -Path $stderr) { (Get-Content -Path $stderr -Raw -ErrorAction SilentlyContinue) } else { '' }
        }
    }
    finally {
        Remove-Item -Path $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-GuestPowerShellScript {
    param([string]$ScriptText)

    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($ScriptText))
    Invoke-Vmrun -Arguments @(
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
    ) | Out-Null
}

function Invoke-FlossJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BinaryPath,
        [Parameter(Mandatory = $true)]
        [string[]]$Needles,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $capture = Invoke-ProcessCapture -FilePath 'python' -Arguments @('-m', 'floss', '-q', '--only', 'static', '-j', $BinaryPath) -TimeoutSeconds 90
    $raw = [string]$capture.stdout
    if ($capture.timed_out -or $capture.exit_code -ne 0 -or [string]::IsNullOrWhiteSpace($raw)) {
        return [ordered]@{
            executed = $false
            exact_hits = @()
            total_hits = 0
            output_file = $null
            error = if ($capture.timed_out) { 'floss timeout' } elseif (-not [string]::IsNullOrWhiteSpace($capture.stderr)) { $capture.stderr.Trim() } else { 'floss returned no JSON output' }
        }
    }

    $parsed = $null
    try {
        $parsed = $raw | ConvertFrom-Json
    }
    catch {
        return [ordered]@{
            executed = $false
            exact_hits = @()
            total_hits = 0
            output_file = $null
            error = 'failed to parse floss JSON output'
        }
    }

    $allStrings = New-Object System.Collections.Generic.List[string]
    if ($parsed.strings) {
        if ($parsed.strings.static_strings) {
            foreach ($entry in $parsed.strings.static_strings) {
                if ($entry.string) {
                    $allStrings.Add([string]$entry.string) | Out-Null
                }
            }
        }
        elseif ($parsed.strings -is [System.Collections.IEnumerable]) {
            foreach ($entry in $parsed.strings) {
                if ($entry.string) {
                    $allStrings.Add([string]$entry.string) | Out-Null
                }
            }
        }
    }
    else {
        foreach ($bucket in @('static_strings', 'stack_strings', 'tight_strings', 'decoded_strings')) {
            if ($parsed.$bucket) {
                foreach ($entry in $parsed.$bucket) {
                    if ($entry.string) {
                        $allStrings.Add([string]$entry.string) | Out-Null
                    }
                }
            }
        }
    }

    $hits = @()
    foreach ($needle in $Needles) {
        $matched = @($allStrings | Where-Object { $_ -eq $needle })
        if ($matched.Count -gt 0) {
            $hits += [pscustomobject]@{
                needle = $needle
                count = $matched.Count
                samples = @($matched | Select-Object -First 5)
            }
        }
    }

    [pscustomobject]@{
        hits = $hits
    } | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8

    $totalHits = 0
    if (@($hits).Count -gt 0) {
        $totalHits = [int](($hits | Measure-Object -Property count -Sum).Sum)
    }

    return [ordered]@{
        executed = $true
        exact_hits = @($hits)
        total_hits = $totalHits
        output_file = $OutputPath
        error = $null
    }
}

Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
Ensure-VmStarted
Wait-GuestReady
Ensure-GuestDirectory $guestProbeRoot

$guestBinaryMap = @{}
foreach ($pair in $binaryDefinitions.GetEnumerator()) {
    $guestBinaryMap[$pair.Key] = $pair.Value
}

$uniqueBinaryNames = $candidates.binaries | Sort-Object -Unique
$copiedBinaries = @{}
foreach ($binaryName in $uniqueBinaryNames) {
    $guestBinaryPath = [string]($guestBinaryMap[$binaryName])
    if ([string]::IsNullOrWhiteSpace($guestBinaryPath)) {
        continue
    }

    $hostBinaryPath = Join-Path $hostWorkRoot $binaryName
    Copy-FromGuest -GuestPath $guestBinaryPath -HostPath $hostBinaryPath
    $copiedBinaries[$binaryName] = [ordered]@{
        guest_path = $guestBinaryPath
        host_path = $hostBinaryPath
        exists = [bool](Test-Path -Path $hostBinaryPath)
    }
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/path-aware/$probeName"
    total_candidates = @($candidates).Count
    candidates_with_static_hits = 0
    candidates_with_runtime_hits = 0
    copied_binary_count = @($copiedBinaries.Keys).Count
    candidate_summary_files = @()
    errors = @()
}

$results = New-Object System.Collections.Generic.List[object]

foreach ($candidate in $candidates) {
    $candidateSlug = $candidate.candidate_id -replace '\.', '-'
    $candidateRepoRoot = Join-Path $repoOutputRoot $candidateSlug
    $candidateHostRoot = Join-Path $hostWorkRoot $candidateSlug
    New-Item -ItemType Directory -Path $candidateRepoRoot, $candidateHostRoot -Force | Out-Null

    $binaryResults = New-Object System.Collections.Generic.List[object]
    $ghidraArtifacts = New-Object System.Collections.Generic.List[object]
    $valueHitBinaryNames = New-Object System.Collections.Generic.List[string]
    $contextHitBinaryNames = New-Object System.Collections.Generic.List[string]
    $collisionBinaryNames = New-Object System.Collections.Generic.List[string]

    foreach ($binaryName in $candidate.binaries) {
        $binaryMeta = $copiedBinaries[$binaryName]
        if ($null -eq $binaryMeta) {
        $binaryResults.Add([pscustomobject]@{
            binary_name = $binaryName
            guest_path = [string]($binaryDefinitions[$binaryName])
            exists = $false
            exact_scan = $null
            floss = [ordered]@{ executed = $false; total_hits = 0; exact_hits = @(); output_file = $null; error = 'binary-not-copied' }
        }) | Out-Null
            continue
        }

        $needlesPath = Join-Path $candidateHostRoot ("$binaryName.needles.json")
        ($candidate.context_needles | ConvertTo-Json -Depth 4) | Set-Content -Path $needlesPath -Encoding UTF8
        $exactOutputPath = Join-Path $candidateHostRoot ("$binaryName.exact.json")
        & python $hostPythonPath $binaryMeta.host_path $exactOutputPath $needlesPath | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Exact string scan failed for $binaryName"
        }

        $exactPayload = Get-Content -Path $exactOutputPath -Raw | ConvertFrom-Json
        $valueNeedle = @($exactPayload.needles | Where-Object { $_.needle -eq $candidate.value_name })
        $contextNeedles = @($exactPayload.needles | Where-Object { $_.needle -ne $candidate.value_name })
        $valueHit = @($valueNeedle | Where-Object { $_.ascii_hits -gt 0 -or $_.wide_hits -gt 0 }).Count -gt 0
        $contextHit = @($contextNeedles | Where-Object { $_.ascii_hits -gt 0 -or $_.wide_hits -gt 0 }).Count -gt 0
        $collisionHit = @($exactPayload.needles | Where-Object { $candidate.collision_needles -contains $_.needle -and ($_.ascii_hits -gt 0 -or $_.wide_hits -gt 0) }).Count -gt 0
        if ($valueHit) { $valueHitBinaryNames.Add($binaryName) | Out-Null }
        if ($contextHit) { $contextHitBinaryNames.Add($binaryName) | Out-Null }
        if ($collisionHit) { $collisionBinaryNames.Add($binaryName) | Out-Null }

        $flossSummary = [ordered]@{
            executed = $false
            total_hits = 0
            exact_hits = @()
            output_file = $null
            error = 'skipped-no-exact-hit'
        }

        if ($valueHit -or $contextHit) {
            $flossOutputPath = Join-Path $candidateHostRoot ("$binaryName.floss.json")
            $flossSummary = Invoke-FlossJson -BinaryPath $binaryMeta.host_path -Needles $candidate.context_needles -OutputPath $flossOutputPath
            if ($flossSummary.output_file) {
                $repoFlossPath = Join-Path $candidateRepoRoot ("$binaryName.floss.json")
                Copy-Item -Path $flossSummary.output_file -Destination $repoFlossPath -Force
                $flossSummary.output_file = "evidence/files/path-aware/$probeName/$candidateSlug/$binaryName.floss.json"
            }
        }

        $repoExactPath = Join-Path $candidateRepoRoot ("$binaryName.exact.json")
        Copy-Item -Path $exactOutputPath -Destination $repoExactPath -Force

        $binaryResults.Add([pscustomobject]@{
            binary_name = $binaryName
            guest_path = $binaryMeta.guest_path
            exists = $true
            exact_scan = $exactPayload
            floss = $flossSummary
        }) | Out-Null
    }

    $ghidraTargetBinaries = @($valueHitBinaryNames | Sort-Object -Unique)
    foreach ($binaryName in $ghidraTargetBinaries) {
        $binaryMeta = $copiedBinaries[$binaryName]
        if ($null -eq $binaryMeta) { continue }

        $ghidraProbeName = "$candidateSlug-$($binaryName -replace '\.', '-')-path-aware"
        try {
            & $ghidraWrapper -TargetBinary $binaryMeta.guest_path -OutputName $ghidraProbeName -Patterns @($candidate.ghidra_patterns) | Out-Null
        }
        catch {
        }

        $latestGhidraRoot = Get-ChildItem -Path 'H:\Temp\vm-tooling-staging\ghidra-probes' -Directory -Filter "$ghidraProbeName-*" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1

        if ($latestGhidraRoot) {
            $repoGhidraRoot = Join-Path $ghidraProbeRoot ("$candidateSlug-$($binaryName -replace '\.', '-')-path-aware-$stamp")
            New-Item -ItemType Directory -Path $repoGhidraRoot -Force | Out-Null
            foreach ($name in @('ghidra-matches.md', 'evidence.json', 'ghidra-run.log')) {
                $src = Join-Path $latestGhidraRoot.FullName $name
                if (Test-Path -Path $src) {
                    Copy-Item -Path $src -Destination (Join-Path $repoGhidraRoot $name) -Force
                }
            }
            $ghidraArtifacts.Add([pscustomobject]@{
                binary_name = $binaryName
                output_root = "evidence/files/ghidra/$($candidateSlug)-$($binaryName -replace '\.', '-')-path-aware-$stamp"
                guest_target = $binaryMeta.guest_path
            }) | Out-Null
        }
    }

    $candidateResult = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        candidate_id = $candidate.candidate_id
        family = $candidate.family
        registry_path = $candidate.registry_path
        value_name = $candidate.value_name
        snapshot_name = $SnapshotName
        binaries = @($binaryResults.ToArray())
        static_value_hit = (@($valueHitBinaryNames | Sort-Object -Unique).Count -gt 0)
        static_context_hit = (@($contextHitBinaryNames | Sort-Object -Unique).Count -gt 0)
        static_collision_hit = (@($collisionBinaryNames | Sort-Object -Unique).Count -gt 0)
        value_hit_binaries = @($valueHitBinaryNames | Sort-Object -Unique)
        context_hit_binaries = @($contextHitBinaryNames | Sort-Object -Unique)
        collision_binaries = @($collisionBinaryNames | Sort-Object -Unique)
        ghidra_artifacts = @($ghidraArtifacts.ToArray())
        summary_artifact = "evidence/files/path-aware/$probeName/$candidateSlug/summary.json"
    }

    $candidateRepoSummary = Join-Path $candidateRepoRoot 'summary.json'
    $candidateJson = $candidateResult | ConvertTo-Json -Depth 10
    Set-Content -Path $candidateRepoSummary -Value $candidateJson -Encoding UTF8
    Set-Content -Path (Join-Path $candidateHostRoot 'summary.json') -Value $candidateJson -Encoding UTF8

    if ($candidateResult.static_value_hit) {
        $summary.candidates_with_static_hits++
    }
    $summary.candidate_summary_files += "evidence/files/path-aware/$probeName/$candidateSlug/summary.json"
    $results.Add([pscustomobject]$candidateResult) | Out-Null
}

$summary.status = if ($summary.candidates_with_static_hits -gt 0) { 'static-hit' } else { 'no-static-hit' }
($summary | ConvertTo-Json -Depth 8) | Set-Content -Path $hostSummaryPath -Encoding UTF8
($results.ToArray() | ConvertTo-Json -Depth 10) | Set-Content -Path $hostResultsPath -Encoding UTF8
Copy-Item -Path $hostSummaryPath -Destination $repoSummaryPath -Force
Copy-Item -Path $hostResultsPath -Destination $repoResultsPath -Force

Write-Output $repoSummaryPath
