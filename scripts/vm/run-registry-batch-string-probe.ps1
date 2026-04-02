[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = '',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\batch-string-search',
    [string]$SnapshotName = '',
    [string]$ProbePrefix = 'registry-batch-string',
    [string]$IncidentLogPath = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_resolve-vm-baseline.ps1')

$vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = Resolve-DefaultVmSnapshotName -VmProfile $VmProfile
}

if ([string]::IsNullOrWhiteSpace($HostOutputRoot)) {
    $HostOutputRoot = Resolve-HostStagingRoot -VmProfile $VmProfile
}

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Resolve-TrackedVmOutputRoot -VmProfile $VmProfile -Fallback (Join-Path $repoRoot 'evidence\files\vm-tooling-staging')
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "$ProbePrefix-$vmProfileTag-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot "$ProbePrefix-payload.ps1"
$hostManifestCopyPath = Join-Path $hostRoot 'manifest.json'
$guestPayloadPath = Join-Path $guestRoot "$ProbePrefix-payload.ps1"
$guestManifestPath = Join-Path $guestRoot 'manifest.json'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$hostResultsPath = Join-Path $hostRoot 'results.json'
$hostResultsCsvPath = Join-Path $hostRoot 'results.csv'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'
$repoResultsPath = Join-Path $repoRootOut 'results.json'
$repoResultsCsvPath = Join-Path $repoRootOut 'results.csv'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null
Copy-Item -Path $ManifestPath -Destination $hostManifestCopyPath -Force
Copy-Item -Path $ManifestPath -Destination (Join-Path $repoRootOut 'manifest.json') -Force

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$ResultsPath,

    [Parameter(Mandatory = $true)]
    [string]$ResultsCsvPath,

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

    $exists = Test-Path $Candidate
    $bytes = if ($exists) { [System.IO.File]::ReadAllBytes($Candidate) } else { @() }
    $ascii = [System.Text.Encoding]::ASCII.GetBytes($Needle)
    $unicode = [System.Text.Encoding]::Unicode.GetBytes($Needle)

    return [ordered]@{
        path = $Candidate
        exists = $exists
        size = if ($exists) { $bytes.Length } else { 0 }
        ascii_match = if ($exists) { Test-BytePattern -Data $bytes -Pattern $ascii } else { $false }
        unicode_match = if ($exists) { Test-BytePattern -Data $bytes -Pattern $unicode } else { $false }
    }
}

New-Item -ItemType Directory -Path (Split-Path -Parent $ResultsPath) -Force | Out-Null

$manifest = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json
$results = New-Object System.Collections.Generic.List[object]

foreach ($entry in $manifest.candidates) {
    $hits = New-Object System.Collections.Generic.List[object]
    foreach ($candidate in $entry.binaries) {
        $hits.Add([pscustomobject](Get-BinaryHit -Needle $entry.needle -Candidate $candidate)) | Out-Null
    }

    $results.Add([pscustomobject]@{
        candidate_id = $entry.candidate_id
        family = $entry.family
        needle = $entry.needle
        hit_count = @($hits | Where-Object { $_.ascii_match -or $_.unicode_match }).Count
        binaries = $hits
    }) | Out-Null
}

$results | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultsPath -Encoding UTF8

$flatRows = foreach ($entry in $results) {
    foreach ($binary in $entry.binaries) {
        [pscustomobject]@{
            candidate_id = $entry.candidate_id
            family = $entry.family
            needle = $entry.needle
            binary_path = $binary.path
            exists = $binary.exists
            size = $binary.size
            ascii_match = $binary.ascii_match
            unicode_match = $binary.unicode_match
        }
    }
}
$flatRows | Export-Csv -Path $ResultsCsvPath -NoTypeInformation -Encoding UTF8

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    total_candidates = $results.Count
    candidates_with_hits = @($results | Where-Object { $_.hit_count -gt 0 }).Count
    candidates_without_hits = @($results | Where-Object { $_.hit_count -eq 0 }).Count
    by_family = @(
        $results |
            Group-Object -Property family |
            Sort-Object Name |
            ForEach-Object {
                [ordered]@{
                    family = $_.Name
                    total = $_.Count
                    with_hits = @($_.Group | Where-Object { $_.hit_count -gt 0 }).Count
                }
            }
    )
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $SummaryPath -Encoding UTF8
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

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 600)

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

        Start-Sleep -Seconds 5
    }

    throw 'Guest did not return to a running VMware Tools state in time.'
}

function Ensure-VmStarted {
    $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') -IgnoreExitCode
    if ($running -notmatch [Regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') -IgnoreExitCode | Out-Null
    }
}

function Get-ShellHealth {
    $processes = Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'listProcessesInGuest',
        $VmPath
    )

    return [ordered]@{
        explorer = [bool]($processes -match '\bexplorer\.exe\b')
        sihost = [bool]($processes -match '\bsihost\.exe\b')
        shellhost = [bool]($processes -match '\bShellHost\.exe\b')
        ctfmon = [bool]($processes -match '\bctfmon\.exe\b')
        process_dump = $processes
    }
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromHostToGuest',
        $VmPath,
        $HostPath,
        $GuestPath
    ) | Out-Null
}

function Copy-FromGuest {
    param([string]$GuestPath, [string]$HostPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromGuestToHost',
        $VmPath,
        $GuestPath,
        $HostPath
    ) | Out-Null
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)

    Invoke-Vmrun -Arguments (@(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'runProgramInGuest',
        $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

function Restore-HealthySnapshot {
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        return
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Ensure-VmStarted
    Wait-GuestReady
}

function Log-Incident {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestId,
        [Parameter(Mandatory = $true)]
        [string]$Symptom,
        [bool]$ShellRecovered = $false,
        [bool]$NeededSnapshotRevert = $false,
        [string]$Notes = ''
    )

    & (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId 'research.batch-string-probe' `
        -TweakId 'research.batch-string-probe' `
        -TestId $TestId `
        -Family 'batch-string-probe' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'multiple' `
        -ValueName 'multiple' `
        -ValueState 'read-only' `
        -Symptom $Symptom `
        -ShellRecovered:$ShellRecovered `
        -NeededSnapshotRevert:$NeededSnapshotRevert `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    manifest = "registry-research-framework/audit/$(Split-Path -Leaf $ManifestPath)"
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = 'started'
    shell_before = $null
    shell_after = $null
    results = $null
    recovery = [ordered]@{
        performed = $false
        shell_healthy_after_recovery = $false
    }
    errors = @()
}

$probeFailed = $false
$needsRecovery = $false

try {
    if (-not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    }

    Ensure-VmStarted
    Wait-GuestReady

    $summary.shell_before = Get-ShellHealth
    if (-not ($summary.shell_before.explorer -and $summary.shell_before.sihost -and $summary.shell_before.shellhost)) {
        throw 'Shell health check failed before the batch string probe started.'
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath
    Copy-ToGuest -HostPath $hostManifestCopyPath -GuestPath $guestManifestPath

    $guestResultsPath = Join-Path $guestRoot 'results.json'
    $guestResultsCsvPath = Join-Path $guestRoot 'results.csv'
    $guestSummaryPath = Join-Path $guestRoot 'summary.json'

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-ManifestPath', $guestManifestPath,
        '-ResultsPath', $guestResultsPath,
        '-ResultsCsvPath', $guestResultsCsvPath,
        '-SummaryPath', $guestSummaryPath
    )

    Copy-FromGuest -GuestPath $guestResultsPath -HostPath $hostResultsPath
    Copy-FromGuest -GuestPath $guestResultsCsvPath -HostPath $hostResultsCsvPath
    Copy-FromGuest -GuestPath $guestSummaryPath -HostPath $hostSummaryPath

    Copy-Item -Path $hostResultsPath -Destination $repoResultsPath -Force
    Copy-Item -Path $hostResultsCsvPath -Destination $repoResultsCsvPath -Force
    Copy-Item -Path $hostSummaryPath -Destination $repoSummaryPath -Force

    $summary.results = Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json
    $summary.shell_after = Get-ShellHealth
    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.shellhost)) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += 'Shell health was degraded after the batch string probe.'
        Log-Incident -TestId $probeName -Symptom 'Shell health was degraded after the batch string probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Recovered by snapshot revert after the read-only batch string lane.'
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Batch string probe failed before results could be captured.'
}
finally {
    if ($needsRecovery -and -not [string]::IsNullOrWhiteSpace($SnapshotName)) {
        try {
            Restore-HealthySnapshot
            $recoveredShell = Get-ShellHealth
            $summary.recovery.performed = $true
            $summary.recovery.shell_healthy_after_recovery = [bool]($recoveredShell.explorer -and $recoveredShell.sihost -and $recoveredShell.shellhost)
        }
        catch {
            $summary.errors += "Recovery failed: $($_.Exception.Message)"
        }
    }
}

$summary.status = if ($probeFailed) { 'failed' } else { 'ok' }
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $hostSummaryPath -Encoding UTF8
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $repoSummaryPath -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}

