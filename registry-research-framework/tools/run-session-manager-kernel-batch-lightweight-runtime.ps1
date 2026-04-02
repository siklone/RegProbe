[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$SnapshotName = '',
    [int]$PostBootSettleSeconds = 25
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$baselineResolver = Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1'
$vmProfileTag = 'primary'
$repoEvidenceBase = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$hostStagingBase = Join-Path ([System.IO.Path]::GetTempPath()) 'vm-tooling-staging-primary'
$guestScriptRoot = 'C:\Tools\Scripts'
$guestDiagBase = 'C:\RegProbe-Diag'
if (Test-Path $baselineResolver) {
    . $baselineResolver
    $vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
    if ([string]::IsNullOrWhiteSpace($VmPath)) {
        $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile
    }
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
        $SnapshotName = Resolve-DefaultVmSnapshotName -VmProfile $VmProfile
    }
    $repoEvidenceBase = Resolve-TrackedVmOutputRoot -VmProfile $VmProfile -Fallback $repoEvidenceBase
    $hostStagingBase = Resolve-HostStagingRoot -VmProfile $VmProfile -Fallback $hostStagingBase
    $guestScriptRoot = Resolve-GuestScriptRoot -VmProfile $VmProfile -Fallback $guestScriptRoot
    $guestDiagBase = Resolve-GuestDiagRoot -VmProfile $VmProfile -Fallback $guestDiagBase
}

if ([string]::IsNullOrWhiteSpace($VmPath)) {
    $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx'
}

if ([string]::IsNullOrWhiteSpace($SnapshotName)) {
    $SnapshotName = 'RegProbe-Baseline-ToolsHardened-20260330'
}

$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$phase0Path = Join-Path $repoRoot 'registry-research-framework\audit\kernel-power-96-phase0-candidates-20260329.json'
$hitQueuePath = Join-Path $repoRoot 'registry-research-framework\audit\kernel-power-96-broad-targeted-string-hit-queue-20260331.json'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "session-manager-kernel-batch-lightweight-runtime-$vmProfileTag-$stamp"
$repoOutputRoot = Join-Path $repoEvidenceBase $probeName
$hostWorkRoot = Join-Path $hostStagingBase $probeName
$guestBatchRoot = Join-Path $guestDiagBase $probeName
$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'
$repoManifestPath = Join-Path $repoOutputRoot 'manifest.json'
$repoSessionPath = Join-Path $repoOutputRoot 'session.json'
$hostManifestPath = Join-Path $hostWorkRoot 'manifest.json'
$hostPayloadPath = Join-Path $hostWorkRoot 'run-session-manager-kernel-batch-lightweight-runtime.guest.ps1'
$guestPayloadPath = Join-Path $guestScriptRoot 'run-session-manager-kernel-batch-lightweight-runtime.guest.ps1'
$guestManifestPath = Join-Path $guestBatchRoot 'manifest.json'
$guestSummaryPath = Join-Path $guestBatchRoot 'summary.json'
$guestResultsPath = Join-Path $guestBatchRoot 'results.json'
$guestStatePath = Join-Path $guestBatchRoot 'state.json'
$guestEtlPath = Join-Path $guestBatchRoot 'session-manager-kernel-batch.etl'
$hostSummaryPath = Join-Path $hostWorkRoot 'summary.json'
$hostResultsPath = Join-Path $hostWorkRoot 'results.json'
$hostStatePath = Join-Path $hostWorkRoot 'state.json'
$hostEtlPath = Join-Path $hostWorkRoot 'session-manager-kernel-batch.etl'
$hostCsvPath = Join-Path $hostWorkRoot 'session-manager-kernel-batch.csv'
$repoStatePath = Join-Path $repoOutputRoot 'state.json'

$phase0 = Get-Content -LiteralPath $phase0Path -Raw | ConvertFrom-Json
$hitQueue = Get-Content -LiteralPath $hitQueuePath -Raw | ConvertFrom-Json
$kernelGroup = @($hitQueue.hit_groups | Where-Object { $_.family -eq 'session-manager-kernel' })
if (@($kernelGroup).Count -ne 1) {
    throw 'Could not resolve the session-manager-kernel hit group from the broad hit queue.'
}

$candidateIds = @($kernelGroup[0].candidate_ids)
$candidates = @(
    foreach ($candidateId in $candidateIds) {
        $candidate = @($phase0.candidates | Where-Object { $_.candidate_id -eq $candidateId }) | Select-Object -First 1
        if ($null -eq $candidate) {
            throw "Candidate metadata missing from phase0 manifest: $candidateId"
        }

        [ordered]@{
            candidate_id = [string]$candidate.candidate_id
            family = [string]$candidate.family
            route_bucket = [string]$candidate.route_bucket
            registry_path = [string]$candidate.registry_path
            value_name = [string]$candidate.value_name
            probe_value = 1
        }
    }
)

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

$manifest = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    family = 'session-manager-kernel'
    candidate_count = @($candidates).Count
    candidate_ids = @($candidates | ForEach-Object { $_.candidate_id })
    candidates = $candidates
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $hostManifestPath -Encoding UTF8
Copy-Item -LiteralPath $hostManifestPath -Destination $repoManifestPath -Force

$guestPayload = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('arm','stop')]
    [string]$Phase,

    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,

    [Parameter(Mandatory = $true)]
    [string]$GuestEtlPath,

    [Parameter(Mandatory = $true)]
    [string]$StatePath,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

    [Parameter(Mandatory = $true)]
    [string]$ResultsPath
)

$ErrorActionPreference = 'Stop'
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'

function Read-TextOrEmpty {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    $raw = Get-Content -LiteralPath $Path -Raw -ErrorAction SilentlyContinue
    if ($null -eq $raw) {
        return ''
    }

    return ([string]$raw).Trim()
}

function Invoke-CmdCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $stdout = [System.IO.Path]::GetTempFileName()
    $stderr = [System.IO.Path]::GetTempFileName()
    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $Arguments -Wait -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        return [ordered]@{
            exit_code = $proc.ExitCode
            stdout = Read-TextOrEmpty -Path $stdout
            stderr = Read-TextOrEmpty -Path $stderr
        }
    }
    finally {
        Remove-Item -Path $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

function Stop-ExistingBootTrace {
    & $wpr -cancelboot | Out-Null
    & $wpr -cancel | Out-Null
}

function Get-BaselineValues {
    param([object[]]$Candidates)

    $values = [ordered]@{}
    foreach ($candidate in $Candidates) {
        $path = [string]$candidate.registry_path -replace '^HKLM', 'Registry::HKEY_LOCAL_MACHINE'
        $name = [string]$candidate.value_name
        try {
            $item = Get-ItemProperty -Path $path -Name $name -ErrorAction Stop
            $values[$name] = $item.$name
        }
        catch {
            $values[$name] = $null
        }
    }

    return $values
}

function Apply-CandidateValues {
    param([object[]]$Candidates)

    $failures = @()
    foreach ($candidate in $Candidates) {
        $providerPath = ([string]$candidate.registry_path -replace '^HKLM\\', 'HKLM:\')
        $valueName = [string]$candidate.value_name
        $probeValue = [int]$candidate.probe_value
        try {
            New-ItemProperty -Path $providerPath -Name $valueName -PropertyType DWord -Value $probeValue -Force | Out-Null
        }
        catch {
            $failures += [ordered]@{
                candidate_id = $candidate.candidate_id
                value_name = $valueName
                error = $_.Exception.Message
            }
        }
    }

    return @($failures)
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$candidates = @($manifest.candidates)

try {
    if ($Phase -eq 'arm') {
        foreach ($path in @($GuestEtlPath, $SummaryPath, $ResultsPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            }
        }

        $baselineValues = Get-BaselineValues -Candidates $candidates
        $applyFailures = Apply-CandidateValues -Candidates $candidates
        Stop-ExistingBootTrace
        & $wpr -addboot Registry -filemode -recordtempto $GuestRoot | Out-Null

        $state = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            phase = 'armed'
            baseline_values = $baselineValues
            candidate_values = (Get-BaselineValues -Candidates $candidates)
            apply_failures = @($applyFailures)
            guest_etl_path = $GuestEtlPath
        }
        $state | ConvertTo-Json -Depth 8 | Set-Content -Path $StatePath -Encoding UTF8
        [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            phase = 'arm'
            status = if (@($applyFailures).Count -eq 0) { 'armed' } else { 'armed-with-errors' }
            apply_failure_count = @($applyFailures).Count
            baseline_missing_count = @($baselineValues.Values | Where-Object { $null -eq $_ }).Count
        } | ConvertTo-Json -Depth 6 | Set-Content -Path $SummaryPath -Encoding UTF8
        exit 0
    }

    $state = if (Test-Path -LiteralPath $StatePath) {
        Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
    }
    else {
        [pscustomobject]@{
            baseline_values = [ordered]@{}
            candidate_values = [ordered]@{}
            apply_failures = @()
        }
    }

    & $wpr -stopboot $GuestEtlPath 'session-manager-kernel broad batch lightweight runtime' | Out-Null

    $csvPath = Join-Path $GuestRoot 'session-manager-kernel-batch.csv'
    if (Test-Path -LiteralPath $csvPath) {
        Remove-Item -LiteralPath $csvPath -Force -ErrorAction SilentlyContinue
    }

    $etlExists = [bool](Test-Path -LiteralPath $GuestEtlPath)
    $etlLength = if ($etlExists) { (Get-Item -LiteralPath $GuestEtlPath).Length } else { 0 }
    if ($etlExists) {
        $tracerpt = Invoke-CmdCapture -FilePath 'C:\Windows\System32\tracerpt.exe' -Arguments @($GuestEtlPath, '-o', $csvPath, '-of', 'CSV')
        if ($tracerpt.exit_code -ne 0) {
            throw "tracerpt failed: $($tracerpt.stderr)"
        }
    }

    $csvExists = [bool](Test-Path -LiteralPath $csvPath)
    $csvLineCount = 0
    $pathLineCount = 0
    $candidateResults = @(
        foreach ($candidate in $candidates) {
            [ordered]@{
                candidate_id = [string]$candidate.candidate_id
                value_name = [string]$candidate.value_name
                exact_line_count = 0
                exact_query_hits = 0
                status = 'no-hit'
            }
        }
    )

    if ($csvExists) {
        $pathPattern = [regex]::Escape('SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel')
        $reader = [System.IO.File]::OpenText($csvPath)
        try {
            while (($line = $reader.ReadLine()) -ne $null) {
                $csvLineCount++
                if ($line -match $pathPattern) {
                    $pathLineCount++
                }

                foreach ($result in $candidateResults) {
                    $valuePattern = [regex]::Escape([string]$result.value_name)
                    if ($line -match $valuePattern) {
                        $result.exact_line_count++
                        if ($line -match 'RegQueryValue|QueryValue') {
                            $result.exact_query_hits++
                        }
                    }
                }
            }
        }
        finally {
            $reader.Close()
        }
    }

    foreach ($result in $candidateResults) {
        $result.status = if ($result.exact_query_hits -gt 0) {
            'exact-hit'
        }
        elseif ($result.exact_line_count -gt 0) {
            'exact-line-no-query'
        }
        elseif ($pathLineCount -gt 0) {
            'path-only-hit'
        }
        else {
            'no-hit'
        }
    }

    $resultsPayload = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'session-manager-kernel'
        total_candidates = @($candidates).Count
        csv_exists = $csvExists
        csv_line_count = $csvLineCount
        path_line_count = $pathLineCount
        exact_hit_count = @($candidateResults | Where-Object { $_.status -eq 'exact-hit' }).Count
        exact_line_only_count = @($candidateResults | Where-Object { $_.status -eq 'exact-line-no-query' }).Count
        path_only_count = @($candidateResults | Where-Object { $_.status -eq 'path-only-hit' }).Count
        no_hit_count = @($candidateResults | Where-Object { $_.status -eq 'no-hit' }).Count
        candidates = @($candidateResults)
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'session-manager-kernel'
        total_candidates = @($candidates).Count
        etl_exists = $etlExists
        etl_length = $etlLength
        csv_exists = $csvExists
        csv_line_count = $csvLineCount
        path_line_count = $pathLineCount
        exact_hit_count = $resultsPayload.exact_hit_count
        exact_line_only_count = $resultsPayload.exact_line_only_count
        path_only_count = $resultsPayload.path_only_count
        no_hit_count = $resultsPayload.no_hit_count
        baseline_missing_count = @($state.baseline_values.PSObject.Properties | Where-Object { $null -eq $_.Value }).Count
        apply_failure_count = @($state.apply_failures).Count
        status = if ($resultsPayload.exact_hit_count -gt 0) { 'exact-hit' } elseif ($resultsPayload.exact_line_only_count -gt 0) { 'exact-line-no-query' } elseif ($resultsPayload.path_only_count -gt 0) { 'path-only-hit' } elseif ($etlExists) { 'no-hit' } else { 'etl-missing' }
    }

    $resultsPayload | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultsPath -Encoding UTF8
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $SummaryPath -Encoding UTF8
}
catch {
    [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        family = 'session-manager-kernel'
        status = 'error'
        error = $_.Exception.Message
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $SummaryPath -Encoding UTF8
    throw
}
'@

Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)
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
            $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
            if ($running -notmatch [regex]::Escape($VmPath)) {
                Start-Sleep -Seconds 3
                continue
            }

            $state = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
            if ($state -match 'running|installed') {
                return
            }
        }
        catch {
        }
        Start-Sleep -Seconds 5
    }
    throw 'Guest is not ready for vmrun guest operations.'
}

function Wait-GuestCommandReady {
    param([int]$TimeoutSeconds = 180)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'runProgramInGuest', $VmPath,
                'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
                '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', 'exit 0'
            ) | Out-Null
            return
        }
        catch {
        }
        Start-Sleep -Seconds 3
    }
    throw 'Guest command execution did not become ready in time.'
}

function Ensure-VmStarted {
    $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
    if ($running -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    }
}

function Start-VmNonBlocking {
    $arguments = @('-T', 'ws', 'start', $VmPath, 'gui')
    Start-Process -FilePath $VmrunPath -ArgumentList $arguments -WindowStyle Hidden | Out-Null
}

function Ensure-GuestDirectory {
    param([string]$GuestPath)
    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'createDirectoryInGuest', $VmPath, $GuestPath
        ) | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'already exists') {
            throw
        }
    }
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath
    ) | Out-Null
}

function Copy-FromGuestBestEffort {
    param([string]$GuestPath, [string]$HostPath, [string]$RepoPath = '')
    try {
        Invoke-Vmrun -Arguments @(
            '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
            'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
        ) | Out-Null
        if ($RepoPath) {
            Copy-Item -Path $HostPath -Destination $RepoPath -Force
        }
        return $true
    }
    catch {
        return $false
    }
}

function Copy-FromGuestBounded {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPath,
        [Parameter(Mandatory = $true)]
        [string]$HostPath,
        [int]$TimeoutSeconds = 600
    )

    $stdoutPath = Join-Path $hostWorkRoot ("copy-{0}.stdout.txt" -f ([IO.Path]::GetFileName($HostPath)))
    $stderrPath = Join-Path $hostWorkRoot ("copy-{0}.stderr.txt" -f ([IO.Path]::GetFileName($HostPath)))
    if (Test-Path $stdoutPath) { Remove-Item -Path $stdoutPath -Force }
    if (Test-Path $stderrPath) { Remove-Item -Path $stderrPath -Force }
    if (Test-Path $HostPath) { Remove-Item -Path $HostPath -Force }

    $argumentList = @(
        '-T', 'ws',
        '-gu', $GuestUser,
        '-gp', $GuestPassword,
        'CopyFileFromGuestToHost',
        $VmPath,
        $GuestPath,
        $HostPath
    )

    $process = Start-Process -FilePath $VmrunPath -ArgumentList $argumentList -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try { $process.Kill() } catch {}
        throw "vmrun copy timed out after $TimeoutSeconds seconds for $GuestPath"
    }

    if ($process.ExitCode -ne 0) {
        $stderr = if (Test-Path $stderrPath) { (Get-Content -Path $stderrPath -Raw).Trim() } else { '' }
        $stdout = if (Test-Path $stdoutPath) { (Get-Content -Path $stdoutPath -Raw).Trim() } else { '' }
        $detail = ($stderr, $stdout | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' '
        throw "vmrun copy failed with exit code $($process.ExitCode) for $GuestPath. $detail".Trim()
    }

    if (-not (Test-Path $HostPath)) {
        throw "vmrun copy reported success but host file was missing for $GuestPath"
    }
}

function Invoke-GuestPowerShell {
    param([string[]]$ArgumentList)
    Invoke-Vmrun -Arguments (@(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
    ) + $ArgumentList) | Out-Null
}

function Wait-VmPoweredOff {
    param([int]$TimeoutSeconds = 300)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
            if ($running -notmatch [regex]::Escape($VmPath)) {
                return
            }
        }
        catch {
        }
        Start-Sleep -Seconds 5
    }
    throw "VM did not power off within $TimeoutSeconds seconds."
}

function Restart-GuestCycle {
    $stopMode = 'soft'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'soft') -IgnoreExitCode | Out-Null
    try {
        Wait-VmPoweredOff -TimeoutSeconds 240
    }
    catch {
        $stopMode = 'hard'
        Invoke-Vmrun -Arguments @('-T', 'ws', 'stop', $VmPath, 'hard') -IgnoreExitCode | Out-Null
        Wait-VmPoweredOff -TimeoutSeconds 90
    }
    Start-VmNonBlocking
    Wait-GuestCommandReady -TimeoutSeconds 600
    Start-Sleep -Seconds 10
    return $stopMode
}

function Get-ShellHealthObject {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json
}

function Get-ShellHealthBestEffort {
    param([int]$TimeoutSeconds = 120)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $last = Get-ShellHealthObject
            if ($last.shell_healthy) {
                return $last
            }
        }
        catch {
        }
        Start-Sleep -Seconds 5
    }
    return $last
}

function Restore-HealthySnapshot {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Start-VmNonBlocking
    Wait-GuestCommandReady -TimeoutSeconds 600
}

function Get-CandidateResultsFromCsv {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Candidates,
        [Parameter(Mandatory = $true)]
        [string]$CsvPath
    )

    $pathLineCount = 0
    $csvLineCount = 0
    $candidateMaps = [ordered]@{}
    foreach ($candidate in $Candidates) {
        $candidateMaps[$candidate.candidate_id] = [ordered]@{
            candidate_id = [string]$candidate.candidate_id
            registry_path = [string]$candidate.registry_path
            value_name = [string]$candidate.value_name
            exact_line_count = 0
            exact_query_hits = 0
            status = 'no-hit'
        }
    }

    if (-not (Test-Path -LiteralPath $CsvPath)) {
        return [ordered]@{
            csv_line_count = 0
            path_line_count = 0
            candidates = @($candidateMaps.Values)
        }
    }

    $pathPattern = [regex]::Escape('SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel')
    $reader = [System.IO.File]::OpenText($CsvPath)
    try {
        while (($line = $reader.ReadLine()) -ne $null) {
            $csvLineCount++
            if ($line -match $pathPattern) {
                $pathLineCount++
            }

            foreach ($candidate in $Candidates) {
                $valuePattern = [regex]::Escape([string]$candidate.value_name)
                if ($line -match $valuePattern) {
                    $candidateMap = $candidateMaps[$candidate.candidate_id]
                    $candidateMap.exact_line_count++
                    if ($line -match 'RegQueryValue|QueryValue') {
                        $candidateMap.exact_query_hits++
                    }
                }
            }
        }
    }
    finally {
        $reader.Close()
    }

    foreach ($candidateMap in $candidateMaps.Values) {
        $candidateMap.status = if ($candidateMap.exact_query_hits -gt 0) {
            'exact-hit'
        }
        elseif ($candidateMap.exact_line_count -gt 0) {
            'exact-line-no-query'
        }
        elseif ($pathLineCount -gt 0) {
            'path-only-hit'
        }
        else {
            'no-hit'
        }
    }

    return [ordered]@{
        csv_line_count = $csvLineCount
        path_line_count = $pathLineCount
        candidates = @($candidateMaps.Values)
    }
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    family = 'session-manager-kernel'
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    total_candidates = @($candidates).Count
    candidate_ids = @($candidates | ForEach-Object { $_.candidate_id })
    shell_before = $null
    shell_after = $null
    reboot_mode = $null
    status = 'started'
    exact_hit_candidates = @()
    exact_line_only_candidates = @()
    path_only_candidates = @()
    no_hit_candidates = @()
    errors = @()
}

try {
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Start-VmNonBlocking
    Wait-GuestCommandReady
    Ensure-GuestDirectory -GuestPath $guestScriptRoot
    Ensure-GuestDirectory -GuestPath $guestBatchRoot
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath
    Copy-ToGuest -HostPath $hostManifestPath -GuestPath $guestManifestPath

    $summary.shell_before = Get-ShellHealthBestEffort

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'arm',
        '-ManifestPath', $guestManifestPath,
        '-GuestRoot', $guestBatchRoot,
        '-GuestEtlPath', $guestEtlPath,
        '-StatePath', $guestStatePath,
        '-SummaryPath', $guestSummaryPath,
        '-ResultsPath', $guestResultsPath
    )

    Copy-FromGuestBestEffort -GuestPath $guestSummaryPath -HostPath $hostSummaryPath -RepoPath $repoSummaryPath | Out-Null
    Copy-FromGuestBestEffort -GuestPath $guestStatePath -HostPath $hostStatePath -RepoPath $repoStatePath | Out-Null
    if (Test-Path -LiteralPath $hostSummaryPath) {
        $armSummary = Get-Content -LiteralPath $hostSummaryPath -Raw | ConvertFrom-Json
        if ([int]$armSummary.apply_failure_count -gt 0) {
            throw "Guest arm phase failed to apply $($armSummary.apply_failure_count) candidate values."
        }
    }

    $summary.reboot_mode = Restart-GuestCycle
    Start-Sleep -Seconds $PostBootSettleSeconds
    Wait-GuestCommandReady

    Invoke-GuestPowerShell -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', $guestPayloadPath,
        '-Phase', 'stop',
        '-ManifestPath', $guestManifestPath,
        '-GuestRoot', $guestBatchRoot,
        '-GuestEtlPath', $guestEtlPath,
        '-StatePath', $guestStatePath,
        '-SummaryPath', $guestSummaryPath,
        '-ResultsPath', $guestResultsPath
    )

    if (-not (Copy-FromGuestBestEffort -GuestPath $guestSummaryPath -HostPath $hostSummaryPath -RepoPath $repoSummaryPath)) {
        throw 'Guest summary copy failed.'
    }
    if (-not (Copy-FromGuestBestEffort -GuestPath $guestResultsPath -HostPath $hostResultsPath -RepoPath $repoResultsPath)) {
        throw 'Guest results copy failed.'
    }
    Copy-FromGuestBestEffort -GuestPath $guestStatePath -HostPath $hostStatePath -RepoPath $repoStatePath | Out-Null

    $resultSummary = Get-Content -LiteralPath $hostSummaryPath -Raw | ConvertFrom-Json
    $resultDetails = Get-Content -LiteralPath $hostResultsPath -Raw | ConvertFrom-Json

    $summary.status = [string]$resultSummary.status
    $summary.exact_hit_candidates = @($resultDetails.candidates | Where-Object { $_.status -eq 'exact-hit' } | ForEach-Object { $_.candidate_id })
    $summary.exact_line_only_candidates = @($resultDetails.candidates | Where-Object { $_.status -eq 'exact-line-no-query' } | ForEach-Object { $_.candidate_id })
    $summary.path_only_candidates = @($resultDetails.candidates | Where-Object { $_.status -eq 'path-only-hit' } | ForEach-Object { $_.candidate_id })
    $summary.no_hit_candidates = @($resultDetails.candidates | Where-Object { $_.status -eq 'no-hit' } | ForEach-Object { $_.candidate_id })
    $summary.shell_after = Get-ShellHealthBestEffort
}
catch {
    $summary.status = 'error'
    $summary.errors += $_.Exception.Message
}
finally {
    try {
        Restore-HealthySnapshot
    }
    catch {
        $summary.errors += "Recovery failed: $($_.Exception.Message)"
    }

    $session = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        probe_name = $probeName
        snapshot_name = $SnapshotName
        status = $summary.status
        summary_file = "evidence/files/vm-tooling-staging/$probeName/summary.json"
        results_file = "evidence/files/vm-tooling-staging/$probeName/results.json"
        manifest_file = "evidence/files/vm-tooling-staging/$probeName/manifest.json"
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $hostSummaryPath -Encoding UTF8
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSummaryPath -Encoding UTF8
    $session | ConvertTo-Json -Depth 8 | Set-Content -Path $repoSessionPath -Encoding UTF8
}

Write-Output $repoSummaryPath
if ($summary.status -eq 'error') {
    exit 1
}

