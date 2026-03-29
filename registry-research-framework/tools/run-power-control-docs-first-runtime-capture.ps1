[CmdletBinding()]
param(
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SnapshotName = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$baselineResolver = Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1'
if (Test-Path $baselineResolver) {
    . $baselineResolver
    if ([string]::IsNullOrWhiteSpace($VmPath)) {
        $VmPath = Resolve-CanonicalVmPath
    }
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        $SnapshotName = Resolve-DefaultVmSnapshotName
    }
}

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx'
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = 'RegProbe-Baseline-Clean-20260329'
}

$sourceManifestPath = Join-Path $repoRoot 'registry-research-framework\audit\power-control-docs-first-value-exists-static-triage-20260329.json'
$probeScriptSource = Join-Path $repoRoot 'scripts\vm\registry-policy-probe.ps1'
$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "power-control-docs-first-runtime-$stamp"
$repoOutputRoot = Join-Path $repoRoot ("evidence\files\vm-tooling-staging\{0}" -f $probeName)
$hostWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) $probeName
$guestScriptRoot = 'C:\Tools\Scripts'
$guestOutputRoot = "C:\RegProbe-Diag\$probeName"
$guestProbeScriptPath = Join-Path $guestScriptRoot 'registry-policy-probe.ps1'
$guestDriverPath = Join-Path $guestScriptRoot 'power-control-docs-first-runtime-driver.ps1'
$hostDriverPath = Join-Path $hostWorkRoot 'power-control-docs-first-runtime-driver.ps1'
$driverLogPath = Join-Path $repoOutputRoot 'driver-run.log'
$summaryPath = Join-Path $repoOutputRoot 'summary.json'
$resultsPath = Join-Path $repoOutputRoot 'results.json'

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

if (-not (Test-Path $sourceManifestPath)) {
    throw "Source manifest was not found: $sourceManifestPath"
}

if (-not (Test-Path $probeScriptSource)) {
    throw "Probe script was not found: $probeScriptSource"
}

$manifest = Get-Content $sourceManifestPath -Raw | ConvertFrom-Json
$candidates = @($manifest.candidates)
if (@($candidates).Count -eq 0) {
    throw 'No candidates were found in the docs-first manifest.'
}

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

    throw 'Guest is not ready for vmrun operations.'
}

function Ensure-GuestDirectory {
    param([string]$GuestPath)

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CreateDirectoryInGuest', $VmPath, $GuestPath
        ) | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'already exists') {
            throw
        }
    }
}

function Copy-FromGuestBestEffort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPath,
        [Parameter(Mandatory = $true)]
        [string]$HostPath
    )

    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
        ) | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Get-ShellHealthObject {
    return (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript `
        -VmPath $VmPath `
        -VmrunPath $VmrunPath `
        -GuestUser $GuestUser `
        -GuestPassword $GuestPassword | ConvertFrom-Json)
}

$guestTrigger = @'
$ErrorActionPreference = "SilentlyContinue"
& cmd.exe /c "powercfg /a >nul 2>&1"
& cmd.exe /c "powercfg /list >nul 2>&1"
& cmd.exe /c "powercfg /query >nul 2>&1"
& cmd.exe /c "powercfg /requests >nul 2>&1"
& cmd.exe /c "sc query Power >nul 2>&1"
Get-CimInstance -Namespace root\cimv2\power -ClassName Win32_PowerPlan | Out-Null
Get-WinEvent -LogName System -MaxEvents 40 | Out-Null
Start-Sleep -Seconds 3
'@

$candidateLiterals = @()
foreach ($candidate in $candidates) {
    $candidateLiterals += @(
        "[ordered]@{",
        "    candidate_id = '$($candidate.candidate_id)'",
        "    value_name = '$($candidate.value_name)'",
        "},"
    )
}
$candidateLiterals[-1] = '}'

$candidateBlock = ($candidateLiterals -join "`r`n")
$driverTemplate = @"
`$ErrorActionPreference = 'Continue'

`$candidates = @(
$candidateBlock
)

`$processNames = @('powercfg.exe', 'System', 'svchost.exe', 'WmiPrvSE.exe', 'rundll32.exe', 'SystemSettings.exe')
`$registryPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Power'
`$probeScript = '$guestProbeScriptPath'
`$guestOutputRoot = '$guestOutputRoot'
`$trigger = @'
$guestTrigger
'@

foreach (`$candidate in `$candidates) {
    try {
        `$candidateRoot = Join-Path `$guestOutputRoot `$candidate.candidate_id
        if (-not (Test-Path `$candidateRoot)) {
            New-Item -ItemType Directory -Path `$candidateRoot -Force | Out-Null
        }

        & 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -File `$probeScript -Mode capture -RegistryPath `$registryPath -ValueName `$candidate.value_name -Prefix `$candidate.candidate_id -OutputDirectory `$candidateRoot -PowerShellCommand `$trigger -ProcessNames `$processNames
    }
    catch {
        @(
            ('ERROR=' + `$_.Exception.GetType().FullName + ': ' + `$_.Exception.Message),
            ('AT=' + `$_.InvocationInfo.PositionMessage)
        ) | Set-Content -Path (Join-Path (Join-Path `$guestOutputRoot `$candidate.candidate_id) 'wrapper-error.txt') -Encoding UTF8
    }
}
"@

$driverTemplate | Set-Content -Path $hostDriverPath -Encoding UTF8

Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
Ensure-VmRunning
Wait-GuestReady

$shellBefore = Get-ShellHealthObject

Ensure-GuestDirectory -GuestPath $guestScriptRoot
Ensure-GuestDirectory -GuestPath $guestOutputRoot
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $probeScriptSource, $guestProbeScriptPath
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'CopyFileFromHostToGuest', $VmPath, $hostDriverPath, $guestDriverPath
) | Out-Null
Invoke-Vmrun -Arguments @(
    '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
    'runProgramInGuest', $VmPath, '-interactive',
    'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $guestDriverPath
) | Set-Content -Path $driverLogPath -Encoding UTF8

$results = New-Object System.Collections.Generic.List[object]
foreach ($candidate in $candidates) {
    $candidateId = [string]$candidate.candidate_id
    $valueName = [string]$candidate.value_name
    $candidateRoot = Join-Path $repoOutputRoot $candidateId
    New-Item -ItemType Directory -Path $candidateRoot -Force | Out-Null

    $guestCandidateRoot = Join-Path $guestOutputRoot $candidateId
    $guestTxt = Join-Path $guestCandidateRoot "$candidateId.txt"
    $guestCsv = Join-Path $guestCandidateRoot "$candidateId.csv"
    $guestHitsCsv = Join-Path $guestCandidateRoot "$candidateId.hits.csv"
    $guestWrapperError = Join-Path $guestCandidateRoot 'wrapper-error.txt'

    $hostTxt = Join-Path $candidateRoot "$candidateId.txt"
    $hostCsv = Join-Path $candidateRoot "$candidateId.csv"
    $hostHitsCsv = Join-Path $candidateRoot "$candidateId.hits.csv"
    $hostWrapperError = Join-Path $candidateRoot 'wrapper-error.txt'
    $hostPmlPlaceholder = Join-Path $candidateRoot "$candidateId.pml.md"

    $copied = [ordered]@{
        txt = Copy-FromGuestBestEffort -GuestPath $guestTxt -HostPath $hostTxt
        csv = Copy-FromGuestBestEffort -GuestPath $guestCsv -HostPath $hostCsv
        hits_csv = Copy-FromGuestBestEffort -GuestPath $guestHitsCsv -HostPath $hostHitsCsv
        wrapper_error = Copy-FromGuestBestEffort -GuestPath $guestWrapperError -HostPath $hostWrapperError
    }

    if ($copied.csv -or $copied.hits_csv -or $copied.txt) {
        @(
            '# External Evidence Placeholder',
            '',
            "Title: $candidateId Procmon capture",
            '',
            'The raw Procmon PML for this lane is not committed. Use the companion TXT, CSV, HITS CSV, and summary JSON in this folder.'
        ) -join "`n" | Set-Content -Path $hostPmlPlaceholder -Encoding UTF8
    }

    $matchCount = 0
    $errorPresent = $false
    if (Test-Path $hostTxt) {
        $txtLines = Get-Content -Path $hostTxt
        foreach ($line in $txtLines) {
            if ($line -match '^MATCH_COUNT=(\d+)$') {
                $matchCount = [int]$Matches[1]
            }
            elseif ($line -match '^ERROR=') {
                $errorPresent = $true
            }
        }
    }

    $exactHitCount = 0
    $pathHitCount = 0
    $hitProcesses = @()
    if (Test-Path $hostHitsCsv) {
        $rows = @(Import-Csv -Path $hostHitsCsv)
        $pathHitCount = $rows.Count
        $exactRows = @($rows | Where-Object { $_.Path -like "*\$valueName" })
        $exactHitCount = $exactRows.Count
        $hitProcesses = @($rows | Select-Object -ExpandProperty 'Process Name' -Unique)
    }

    $results.Add([ordered]@{
        candidate_id = $candidateId
        value_name = $valueName
        registry_path = 'HKLM\SYSTEM\CurrentControlSet\Control\Power'
        trigger = 'powercfg /a + powercfg /list + powercfg /query + powercfg /requests + sc query Power + Win32_PowerPlan CIM query + System event read'
        txt = "evidence/files/vm-tooling-staging/$probeName/$candidateId/$candidateId.txt"
        csv = if (Test-Path $hostCsv) { "evidence/files/vm-tooling-staging/$probeName/$candidateId/$candidateId.csv" } else { $null }
        hits_csv = if (Test-Path $hostHitsCsv) { "evidence/files/vm-tooling-staging/$probeName/$candidateId/$candidateId.hits.csv" } else { $null }
        pml_placeholder = if (Test-Path $hostPmlPlaceholder) { "evidence/files/vm-tooling-staging/$probeName/$candidateId/$candidateId.pml.md" } else { $null }
        wrapper_error = if (Test-Path $hostWrapperError) { "evidence/files/vm-tooling-staging/$probeName/$candidateId/wrapper-error.txt" } else { $null }
        copied = $copied
        match_count = $matchCount
        path_hit_count = $pathHitCount
        exact_value_hit_count = $exactHitCount
        exact_value_hit = ($exactHitCount -gt 0)
        hit_processes = $hitProcesses
        error_present = $errorPresent
        status = if ($errorPresent -or (Test-Path $hostWrapperError)) { 'error' } elseif ($exactHitCount -gt 0) { 'exact-hit' } elseif ($pathHitCount -gt 0) { 'path-only-hit' } elseif ($copied.txt) { 'no-hit' } else { 'copy-incomplete' }
    })
}

$shellAfter = Get-ShellHealthObject
$copyIncompleteCount = @($results | Where-Object { $_.status -eq 'copy-incomplete' }).Count
$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    manifest = 'registry-research-framework/audit/power-control-docs-first-value-exists-static-triage-20260329.json'
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = if (@($results | Where-Object { $_.status -eq 'error' }).Count -gt 0) { 'completed-with-errors' } elseif ($copyIncompleteCount -gt 0) { 'copy-incomplete' } else { 'ok' }
    shell_before = $shellBefore
    shell_after = $shellAfter
    total_candidates = $results.Count
    exact_hit_candidates = @($results | Where-Object { $_.exact_value_hit }).Count
    path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
    no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    copy_incomplete_candidates = $copyIncompleteCount
    error_candidates = @($results | Where-Object { $_.status -eq 'error' }).Count
    driver_log = "evidence/files/vm-tooling-staging/$probeName/driver-run.log"
    results_path = "evidence/files/vm-tooling-staging/$probeName/results.json"
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath -Encoding UTF8
$results | ConvertTo-Json -Depth 8 | Set-Content -Path $resultsPath -Encoding UTF8
Write-Output $summaryPath
