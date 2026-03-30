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
    if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = Resolve-CanonicalVmPath }
    if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = Resolve-DefaultVmSnapshotName }
}
if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx' }
if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = 'RegProbe-Baseline-ToolsHardened-20260330' }

$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "executive-uuid-sequence-number-lightweight-runtime-$stamp"
$repoOutputRoot = Join-Path $repoRoot "evidence\files\vm-tooling-staging\$probeName"
$hostWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) $probeName
$guestScriptRoot = 'C:\Tools\Scripts'
$guestRoot = "C:\RegProbe-Diag\$probeName"
$candidateId = 'system.executive-uuid-sequence-number'
$candidateLabel = 'system-executive-uuid-sequence-number'
$valueName = 'UuidSequenceNumber'
$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

$guestPayload = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,
    [Parameter(Mandatory = $true)]
    [string]$Phase,
    [Parameter(Mandatory = $true)]
    [string]$TriggerProfile
)

$ErrorActionPreference = 'Stop'
$valueName = 'UuidSequenceNumber'
$registryPathFragment = 'SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Executive'
$traceBaseName = if ($Phase -like 'split-*') { 'split-trace' } else { $Phase }
$etlPath = Join-Path $GuestRoot ("$traceBaseName.etl")
$csvPath = Join-Path $GuestRoot ("$traceBaseName.csv")
$summaryPath = Join-Path $GuestRoot ("$Phase-summary.json")
$sessionName = if ($Phase -like 'split-*') { 'RegProbeExecutiveUuidSplit' } else { 'RegProbeExecutiveUuid_' + ($Phase -replace '[^A-Za-z0-9]', '') }

function Read-TextOrEmpty {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $raw = Get-Content -LiteralPath $Path -Raw -ErrorAction SilentlyContinue
    if ($null -eq $raw) { return '' }
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

function Stop-ExistingSession {
    param([string]$Name)
    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $Name, '-ets') | Out-Null
    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $Name) | Out-Null
}

function Invoke-UuidRpcComBurst {
    param([int]$OuterLoops = 8)
    $burstScript = [string]::Join("`n", @(
        '$ErrorActionPreference = ''SilentlyContinue''',
        '1..96 | ForEach-Object { [guid]::NewGuid() | Out-Null }',
        '1..24 | ForEach-Object {',
        '    try { [System.Runtime.InteropServices.Marshal]::GenerateGuidForType([string]) | Out-Null } catch {}',
        '}',
        '1..12 | ForEach-Object {',
        '    try { New-Object -ComObject WScript.Shell | Out-Null } catch {}',
        '    try { New-Object -ComObject Shell.Application | Out-Null } catch {}',
        '    try { New-Object -ComObject Scripting.Dictionary | Out-Null } catch {}',
        '    try { Get-CimInstance Win32_OperatingSystem | Out-Null } catch {}',
        '    try { Get-CimInstance Win32_ComputerSystemProduct | Out-Null } catch {}',
        '    try { Get-CimInstance Win32_Process | Select-Object -First 10 | Out-Null } catch {}',
        '}',
        'if (Get-Command wmic.exe -ErrorAction SilentlyContinue) {',
        '    & wmic.exe path Win32_OperatingSystem get BuildNumber | Out-Null',
        '    & wmic.exe path Win32_ComputerSystemProduct get UUID | Out-Null',
        '}'
    ))
    for ($i = 0; $i -lt $OuterLoops; $i++) {
        $result = Invoke-CmdCapture -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -Arguments @('-NoProfile','-ExecutionPolicy','Bypass','-Command',$burstScript)
        if ($result.exit_code -ne 0 -or -not [string]::IsNullOrWhiteSpace($result.stderr)) {
            return [ordered]@{ failed = $true; exit_code = $result.exit_code; stdout = $result.stdout; stderr = $result.stderr }
        }
        Start-Sleep -Milliseconds 250
    }
    return [ordered]@{ failed = $false; exit_code = 0; stdout = ''; stderr = '' }
}

function Invoke-TriggerProfile {
    param([string]$Profile)
    switch ($Profile) {
        'uuid-rpc-com-burst-short' { return ,(Invoke-UuidRpcComBurst -OuterLoops 10) }
        'uuid-rpc-com-burst-only' { return ,(Invoke-UuidRpcComBurst -OuterLoops 10) }
        default { throw "Unknown trigger profile: $Profile" }
    }
}

function Build-CompactSummary {
    param(
        [string]$Status,
        [bool]$EtlExists,
        [bool]$CsvExists,
        [long]$EtlLength,
        [int]$CsvLineCount,
        [int]$ExactLineCount,
        [int]$ExactQueryHits,
        [int]$PathLineCount,
        [int]$TriggerFailureCount,
        [object[]]$TriggerFailures,
        [string[]]$Errors
    )
    return [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        value_name = $valueName
        phase = $Phase
        trigger_profile = $TriggerProfile
        status = $Status
        etl_exists = $EtlExists
        csv_exists = $CsvExists
        etl_length = $EtlLength
        csv_line_count = $CsvLineCount
        exact_runtime_read = ($ExactQueryHits -gt 0)
        exact_query_hits = $ExactQueryHits
        exact_line_count = $ExactLineCount
        path_line_count = $PathLineCount
        trigger_failure_count = $TriggerFailureCount
        trigger_failures = @($TriggerFailures | Select-Object -First 6)
        errors = @($Errors | Select-Object -First 4)
    }
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

try {
    if ($Phase -in @('short-trigger-etw', 'split-trace-start')) {
        foreach ($path in @($etlPath, $csvPath, $summaryPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            }
        }
    }

    if ($Phase -eq 'short-trigger-etw') {
        Stop-ExistingSession -Name $sessionName
        $create = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('create','trace',$sessionName,'-o',$etlPath,'-p','Microsoft-Windows-Kernel-Registry','0xFFFF','5','-ets')
        if ($create.exit_code -ne 0) { throw "logman create trace failed: $($create.stderr)" }
        Start-Sleep -Seconds 1
        $triggerFailures = @()
        $triggerResult = Invoke-TriggerProfile -Profile $TriggerProfile
        if ($triggerResult.failed) { $triggerFailures += $triggerResult }
        $stop = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop',$sessionName,'-ets')
        if ($stop.exit_code -ne 0) { throw "logman stop failed: $($stop.stderr)" }
        Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $sessionName) | Out-Null
    }
    elseif ($Phase -eq 'split-trace-start') {
        Stop-ExistingSession -Name $sessionName
        $create = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('create','trace',$sessionName,'-o',$etlPath,'-p','Microsoft-Windows-Kernel-Registry','0xFFFF','5','-bs','64','-nb','32','64','-ets')
        if ($create.exit_code -ne 0) { throw "logman create trace failed: $($create.stderr)" }
        $payload = Build-CompactSummary -Status 'trace-started' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount 0 -TriggerFailures @() -Errors @()
        $payload | ConvertTo-Json -Depth 6 -Compress | Set-Content -Path $summaryPath -Encoding UTF8
        exit 0
    }
    elseif ($Phase -eq 'split-trigger') {
        $triggerFailures = @()
        $triggerResult = Invoke-TriggerProfile -Profile $TriggerProfile
        if ($triggerResult.failed) { $triggerFailures += $triggerResult }
        $payload = Build-CompactSummary -Status 'trigger-complete' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount @($triggerFailures).Count -TriggerFailures @($triggerFailures) -Errors @()
        $payload | ConvertTo-Json -Depth 6 -Compress | Set-Content -Path $summaryPath -Encoding UTF8
        exit 0
    }
    elseif ($Phase -eq 'split-trace-stop') {
        $stop = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop',$sessionName,'-ets')
        if ($stop.exit_code -ne 0) { throw "logman stop failed: $($stop.stderr)" }
        Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $sessionName) | Out-Null
    }
    else {
        throw "Unsupported phase: $Phase"
    }

    $etlExists = [bool](Test-Path -LiteralPath $etlPath)
    $etlLength = if ($etlExists) { (Get-Item -LiteralPath $etlPath).Length } else { 0 }
    $tracerpt = Invoke-CmdCapture -FilePath 'C:\Windows\System32\tracerpt.exe' -Arguments @($etlPath,'-o',$csvPath,'-of','CSV')
    if ($tracerpt.exit_code -ne 0) { throw "tracerpt failed: $($tracerpt.stderr)" }

    $csvExists = [bool](Test-Path -LiteralPath $csvPath)
    $csvLineCount = 0
    $pathLineCount = 0
    $exactLineCount = 0
    $exactQueryHits = 0
    if ($csvExists) {
        $pathPattern = [regex]::Escape($registryPathFragment)
        $valuePattern = [regex]::Escape($valueName)
        $reader = [System.IO.File]::OpenText($csvPath)
        try {
            while (($line = $reader.ReadLine()) -ne $null) {
                $csvLineCount++
                if ($line -match $pathPattern) { $pathLineCount++ }
                if ($line -match $valuePattern) {
                    $exactLineCount++
                    if ($line -match 'RegQueryValue|QueryValue') { $exactQueryHits++ }
                }
            }
        }
        finally {
            $reader.Close()
        }
    }

    $status = if ($exactQueryHits -gt 0) { 'exact-hit' } elseif ($exactLineCount -gt 0) { 'exact-line-no-query' } elseif ($pathLineCount -gt 0) { 'path-only-hit' } else { 'no-hit' }
    $payload = Build-CompactSummary -Status $status -EtlExists $etlExists -CsvExists $csvExists -EtlLength $etlLength -CsvLineCount $csvLineCount -ExactLineCount $exactLineCount -ExactQueryHits $exactQueryHits -PathLineCount $pathLineCount -TriggerFailureCount 0 -TriggerFailures @() -Errors @()
    $payload | ConvertTo-Json -Depth 6 -Compress | Set-Content -Path $summaryPath -Encoding UTF8
}
catch {
    $payload = Build-CompactSummary -Status 'error' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount 0 -TriggerFailures @() -Errors @($_.Exception.Message)
    try {
        $payload | ConvertTo-Json -Depth 6 -Compress | Set-Content -Path $summaryPath -Encoding UTF8
    }
    catch {
    }
}

exit 0
'@

$hostPayloadPath = Join-Path $hostWorkRoot 'run-executive-uuid-sequence-number-lightweight-runtime-followup.guest.ps1'
Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

function Invoke-Vmrun {
    param([string[]]$Arguments, [switch]$IgnoreExitCode)
    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) { throw "vmrun failed ($LASTEXITCODE): $($output.Trim())" }
    return $output.Trim()
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 300)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $running = Invoke-Vmrun -Arguments @('-T','ws','list')
            if ($running -notmatch [regex]::Escape($VmPath)) { Start-Sleep -Seconds 3; continue }
            $state = Invoke-Vmrun -Arguments @('-T','ws','checkToolsState',$VmPath)
            if ($state -match 'running|installed') { return }
        }
        catch {}
        Start-Sleep -Seconds 3
    }
    throw 'Guest is not ready for vmrun guest operations.'
}

function Wait-GuestCommandReady {
    param([int]$TimeoutSeconds = 180)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-Vmrun -Arguments @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'runProgramInGuest',$VmPath,'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-Command','exit 0') | Out-Null
            return
        }
        catch {}
        Start-Sleep -Seconds 3
    }
    throw 'Guest command execution did not become ready in time.'
}

function Ensure-GuestDirectory {
    param([string]$GuestPath)
    try {
        Invoke-Vmrun -Arguments @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'createDirectoryInGuest',$VmPath,$GuestPath) | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch 'already exists') { throw }
    }
}

function Copy-FromGuestBestEffort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPath,
        [Parameter(Mandatory = $true)]
        [string]$HostPath,
        [int]$Attempts = 5,
        [int]$DelaySeconds = 2
    )
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Invoke-Vmrun -Arguments @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'CopyFileFromGuestToHost',$VmPath,$GuestPath,$HostPath) | Out-Null
            if (Test-Path -LiteralPath $HostPath) { return $true }
        }
        catch {}
        Start-Sleep -Seconds $DelaySeconds
    }
    return $false
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)
    Invoke-Vmrun -Arguments @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'CopyFileFromHostToGuest',$VmPath,$HostPath,$GuestPath) | Out-Null
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
            if ($last.shell_healthy) { return $last }
        }
        catch {}
        Start-Sleep -Seconds 5
    }
    if ($null -eq $last) {
        return [pscustomobject]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            vm_path = $VmPath
            vm_running = $false
            tools_state = 'unknown'
            process_query_error = 'shell-health-timeout'
            shell_healthy = $false
            checks = [ordered]@{ explorer = $false; sihost = $false; shellhost = $false; ctfmon = $false; app = $false }
        }
    }
    return $last
}

function Invoke-GuestPhase {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPayloadPath,
        [Parameter(Mandatory = $true)]
        [string]$Phase,
        [Parameter(Mandatory = $true)]
        [string]$TriggerProfile
    )
    $script = @"
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path '$guestRoot' -Force | Out-Null
& '$GuestPayloadPath' -GuestRoot '$guestRoot' -Phase '$Phase' -TriggerProfile '$TriggerProfile'
exit 0
"@
    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script))
    Invoke-Vmrun -Arguments @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'runProgramInGuest',$VmPath,'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-EncodedCommand',$encoded) | Out-Null
}

function New-PhaseErrorSummary {
    param([string]$PhaseName, [string]$Message)
    return [pscustomobject]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        phase = $PhaseName
        status = 'error'
        exact_runtime_read = $false
        exact_query_hits = 0
        errors = @($Message)
    }
}

function Write-Json {
    param([object]$Payload, [string]$HostPath, [string]$RepoPath = '')
    $json = ConvertTo-Json -InputObject $Payload -Depth 8
    Set-Content -Path $HostPath -Value $json -Encoding UTF8
    if ($RepoPath) { Copy-Item -Path $HostPath -Destination $RepoPath -Force }
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    total_candidates = 1
    exact_hit_candidates = 0
    exact_line_only_candidates = 0
    path_only_candidates = 0
    no_hit_candidates = 0
    error_candidates = 0
    candidate_summary_files = @()
    errors = @()
}

$results = New-Object System.Collections.Generic.List[object]

try {
    $guestPayloadPath = Join-Path $guestScriptRoot 'run-executive-uuid-sequence-number-lightweight-runtime-followup.guest.ps1'
    $hostCandidateRoot = Join-Path $hostWorkRoot $candidateLabel
    $repoCandidateRoot = Join-Path $repoOutputRoot $candidateLabel
    New-Item -ItemType Directory -Path $hostCandidateRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $repoCandidateRoot -Force | Out-Null

    Invoke-Vmrun -Arguments @('-T','ws','revertToSnapshot',$VmPath,$SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T','ws','start',$VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-GuestCommandReady
    Ensure-GuestDirectory -GuestPath $guestScriptRoot
    Ensure-GuestDirectory -GuestPath $guestRoot
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    $shellBefore = Get-ShellHealthBestEffort
    $phaseResults = [ordered]@{}
    foreach ($phaseSpec in @(
        [ordered]@{ phase = 'short-trigger-etw'; trigger = 'uuid-rpc-com-burst-short' },
        [ordered]@{ phase = 'split-trace-start'; trigger = 'uuid-rpc-com-burst-only' },
        [ordered]@{ phase = 'split-trigger'; trigger = 'uuid-rpc-com-burst-only' },
        [ordered]@{ phase = 'split-trace-stop'; trigger = 'uuid-rpc-com-burst-only' }
    )) {
        $phaseSummary = $null
        try {
            Wait-GuestReady
            Wait-GuestCommandReady
            Invoke-GuestPhase -GuestPayloadPath $guestPayloadPath -Phase $phaseSpec.phase -TriggerProfile $phaseSpec.trigger
            if ($phaseSpec.phase -eq 'split-trigger') {
                Start-Sleep -Seconds 15
            }
            elseif ($phaseSpec.phase -eq 'short-trigger-etw') {
                Start-Sleep -Seconds 5
            }
            elseif ($phaseSpec.phase -eq 'split-trace-start') {
                Start-Sleep -Seconds 2
            }
            $guestPhaseSummary = Join-Path $guestRoot "$($phaseSpec.phase)-summary.json"
            $hostPhaseSummary = Join-Path $hostCandidateRoot "$($phaseSpec.phase)-summary.json"
            if (Copy-FromGuestBestEffort -GuestPath $guestPhaseSummary -HostPath $hostPhaseSummary) {
                try { $phaseSummary = Get-Content -LiteralPath $hostPhaseSummary -Raw | ConvertFrom-Json } catch {}
            }
        }
        catch {
            $phaseSummary = New-PhaseErrorSummary -PhaseName $phaseSpec.phase -Message $_.Exception.Message
        }
        if ($null -eq $phaseSummary) {
            $phaseSummary = [pscustomobject]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                phase = $phaseSpec.phase
                status = 'copy-incomplete'
                exact_runtime_read = $false
                exact_query_hits = 0
            }
        }
        $phaseResults[$phaseSpec.phase] = $phaseSummary
    }

    $shellAfter = Get-ShellHealthBestEffort
    $best = $phaseResults['short-trigger-etw']
    foreach ($phaseName in @('split-trace-stop','split-trigger','split-trace-start')) {
        $candidatePhase = $phaseResults[$phaseName]
        if ($candidatePhase.exact_runtime_read) {
            $best = $candidatePhase
            break
        }
        if ($best.status -eq 'copy-incomplete' -and $candidatePhase.status -ne 'copy-incomplete') {
            $best = $candidatePhase
        }
    }

    $candidateResult = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        candidate_id = $candidateId
        value_name = $valueName
        snapshot_name = $SnapshotName
        shell_before = $shellBefore
        shell_after = $shellAfter
        best_phase = $best.phase
        status = $best.status
        exact_query_hits = $best.exact_query_hits
        exact_runtime_read = [bool]$best.exact_runtime_read
        phase_results = $phaseResults
        artifacts = [ordered]@{
            summary = "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
        }
    }

    Write-Json -Payload $candidateResult -HostPath (Join-Path $hostCandidateRoot 'summary.json') -RepoPath (Join-Path $repoCandidateRoot 'summary.json')
    $summary.candidate_summary_files += "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
    $results.Add([pscustomobject]$candidateResult) | Out-Null
    $summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
    $summary.exact_line_only_candidates = @($results | Where-Object { $_.status -eq 'exact-line-no-query' }).Count
    $summary.path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
    $summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    $summary.error_candidates = @($results | Where-Object { $_.status -in @('error','copy-incomplete') }).Count
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.exact_line_only_candidates -gt 0) { 'exact-line-no-query' } elseif ($summary.path_only_candidates -gt 0) { 'path-only-hit' } elseif ($summary.error_candidates -gt 0) { 'error' } else { 'no-hit' }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}

Write-Json -Payload $summary -HostPath (Join-Path $hostWorkRoot 'summary.json') -RepoPath $repoSummaryPath
Write-Json -Payload @($results.ToArray()) -HostPath (Join-Path $hostWorkRoot 'results.json') -RepoPath $repoResultsPath
Write-Output $repoSummaryPath
