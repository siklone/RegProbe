[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$SnapshotName = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repoRoot 'scripts\vm\_resolve-vm-baseline.ps1')

$vmProfileTag = Resolve-VmProfileTag -VmProfile $VmProfile
$repoEvidenceBase = Resolve-TrackedVmOutputRoot -VmProfile $VmProfile -Fallback (Join-Path $repoRoot 'evidence\files\vm-tooling-staging')
$hostStagingBase = Resolve-HostStagingRoot -VmProfile $VmProfile
$guestScriptRoot = Resolve-GuestScriptRoot -VmProfile $VmProfile
$guestDiagBase = Resolve-GuestDiagRoot -VmProfile $VmProfile

if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = Resolve-CanonicalVmPath -VmProfile $VmProfile }
if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = Resolve-DefaultVmSnapshotName -VmProfile $VmProfile }
if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx' }
if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = 'RegProbe-Baseline-ToolsHardened-20260330' }

$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "watchdog-lightweight-runtime-$vmProfileTag-$stamp"
$repoOutputRoot = Join-Path $repoEvidenceBase $probeName
$hostWorkRoot = Join-Path $hostStagingBase $probeName
$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'
$guestRoot = Join-Path $guestDiagBase $probeName
$candidateId = 'power.session-watchdog-timeouts'
$candidateLabel = 'power-session-watchdog-timeouts'

New-Item -ItemType Directory -Path $repoOutputRoot, $hostWorkRoot -Force | Out-Null

$guestPayload = @'
param([string]$GuestRoot,[string]$Phase)
$ErrorActionPreference = 'Stop'
$pairNames = @('WatchdogResumeTimeout','WatchdogSleepTimeout')
$adjacentNames = @('Win32CalloutWatchdogBugcheckEnabled')
$traceBase = if ($Phase -like 'split-*') { 'split-trace' } else { $Phase }
$etl = Join-Path $GuestRoot "$traceBase.etl"
$csv = Join-Path $GuestRoot "$traceBase.csv"
$sum = Join-Path $GuestRoot "$Phase-summary.json"
$helper = Join-Path $GuestRoot 's1-helper.ps1'
$lastwake = Join-Path $GuestRoot "$traceBase-lastwake.txt"
$kernel = Join-Path $GuestRoot "$traceBase-kernelpower.txt"
$session = if ($Phase -like 'split-*') { 'RegProbeWatchdogLightweightSplit' } else { 'RegProbeWatchdogLightweight_' + ($Phase -replace '[^A-Za-z0-9]','') }
$pathPattern = 'SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power'

function Cap {
  $pc = (& cmd /c "powercfg /a" 2>&1 | Out-String)
  $available = $pc -replace '(?s)The following sleep states are not available on this system:.*$', ''
  [ordered]@{
    s1_supported = [bool]($available -match 'Standby \(S1\)')
    modern_standby_supported = [bool]($available -match 'Standby \(S0 Low Power Idle\)' -or $available -match 'Modern Standby')
    powercfg_output = (($pc -split "(`r`n|`n|`r)") | Select-Object -First 24) -join [Environment]::NewLine
  }
}

function Parse-Csv([string]$CsvPath) {
  $stats = [ordered]@{
    csv_exists = [bool](Test-Path -LiteralPath $CsvPath)
    csv_line_count = 0
    path_line_count = 0
    exact_line_count = 0
    exact_query_hits = 0
    adjacent_query_hits = 0
    per_value_line_counts = [ordered]@{}
    per_value_query_counts = [ordered]@{}
  }
  foreach ($n in ($pairNames + $adjacentNames)) {
    $stats.per_value_line_counts[$n] = 0
    $stats.per_value_query_counts[$n] = 0
  }
  if (-not $stats.csv_exists) { return $stats }
  $reader = [System.IO.File]::OpenText($CsvPath)
  try {
    while (($line = $reader.ReadLine()) -ne $null) {
      $stats.csv_line_count++
      if ($line -match [regex]::Escape($pathPattern)) { $stats.path_line_count++ }
      foreach ($n in ($pairNames + $adjacentNames)) {
        if ($line -match [regex]::Escape($n)) {
          $stats.per_value_line_counts[$n]++
          if ($n -in $pairNames) { $stats.exact_line_count++ }
          if ($line -match 'RegQueryValue|QueryValue') {
            $stats.per_value_query_counts[$n]++
            if ($n -in $pairNames) { $stats.exact_query_hits++ } else { $stats.adjacent_query_hits++ }
          }
        }
      }
    }
  } finally { $reader.Close() }
  return $stats
}

function Emit([hashtable]$h) { $h | ConvertTo-Json -Depth 8 | Set-Content -Path $sum -Encoding UTF8 }

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

if ($Phase -eq 'capability-check') {
  $cap = Cap
  Emit ([ordered]@{ generated_utc=[DateTime]::UtcNow.ToString('o'); phase=$Phase; status=($(if($cap.s1_supported){'s1-supported'}else{'no-s1'})); s1_supported=$cap.s1_supported; modern_standby_supported=$cap.modern_standby_supported; powercfg_output=$cap.powercfg_output })
  exit 0
}

foreach ($p in @($etl,$csv,$sum,$helper,$lastwake,$kernel)) {
  if (($Phase -eq 'short-trigger-etw' -or $Phase -eq 'split-trace-start') -and (Test-Path -LiteralPath $p)) {
    Remove-Item -LiteralPath $p -Force -ErrorAction SilentlyContinue
  }
}

[System.IO.File]::WriteAllText($helper, "Start-Sleep -Seconds 5`nStart-Process -FilePath `"$env:SystemRoot\System32\rundll32.exe`" -ArgumentList 'powrprof.dll,SetSuspendState 0,1,0' -WindowStyle Hidden | Out-Null`n", [System.Text.Encoding]::UTF8)

if ($Phase -eq 'short-trigger-etw') {
  & logman stop $session -ets *> $null
  & logman delete $session *> $null
  & logman create trace $session -o $etl -p Microsoft-Windows-Kernel-Registry 0xFFFF 5 -ets | Out-Null
  Start-Sleep -Seconds 1
  Start-Process -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',$helper) -WindowStyle Hidden | Out-Null
  Start-Sleep -Seconds 45
  & logman stop $session -ets | Out-Null
  & logman delete $session | Out-Null
  & cmd /c "powercfg /lastwake > ""$lastwake"" 2>&1" | Out-Null
  & cmd /c "wevtutil qe System /q:""*[System[(Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=1 or EventID=42 or EventID=107 or EventID=506))]]"" /c:20 /rd:true /f:text > ""$kernel"" 2>&1" | Out-Null
  & tracerpt $etl -o $csv -of CSV | Out-Null
  $s = Parse-Csv $csv
  $status = if ($s.exact_query_hits -gt 0) { 'exact-hit' } elseif ($s.exact_line_count -gt 0) { 'exact-line-no-query' } elseif ($s.adjacent_query_hits -gt 0) { 'adjacent-query-only' } elseif ($s.path_line_count -gt 0) { 'path-only-hit' } else { 'no-hit' }
  Emit ($s + [ordered]@{ generated_utc=[DateTime]::UtcNow.ToString('o'); phase=$Phase; status=$status; etl_exists=[bool](Test-Path $etl); etl_length=$(if(Test-Path $etl){(Get-Item $etl).Length}else{0}); lastwake_exists=[bool](Test-Path $lastwake); kernelpower_exists=[bool](Test-Path $kernel) })
  exit 0
}

if ($Phase -eq 'split-trace-start') {
  & logman stop $session -ets *> $null
  & logman delete $session *> $null
  & logman create trace $session -o $etl -p Microsoft-Windows-Kernel-Registry 0xFFFF 5 -bs 64 -nb 32 64 -ets | Out-Null
  Emit ([ordered]@{ generated_utc=[DateTime]::UtcNow.ToString('o'); phase=$Phase; status='trace-started' })
  exit 0
}

if ($Phase -eq 'split-trigger') {
  Start-Process -FilePath 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',$helper) -WindowStyle Hidden | Out-Null
  Emit ([ordered]@{ generated_utc=[DateTime]::UtcNow.ToString('o'); phase=$Phase; status='trigger-started' })
  exit 0
}

if ($Phase -eq 'split-trace-stop') {
  & logman stop $session -ets | Out-Null
  & logman delete $session | Out-Null
  & cmd /c "powercfg /lastwake > ""$lastwake"" 2>&1" | Out-Null
  & cmd /c "wevtutil qe System /q:""*[System[(Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=1 or EventID=42 or EventID=107 or EventID=506))]]"" /c:20 /rd:true /f:text > ""$kernel"" 2>&1" | Out-Null
  & tracerpt $etl -o $csv -of CSV | Out-Null
  $s = Parse-Csv $csv
  $status = if ($s.exact_query_hits -gt 0) { 'exact-hit' } elseif ($s.exact_line_count -gt 0) { 'exact-line-no-query' } elseif ($s.adjacent_query_hits -gt 0) { 'adjacent-query-only' } elseif ($s.path_line_count -gt 0) { 'path-only-hit' } else { 'no-hit' }
  Emit ($s + [ordered]@{ generated_utc=[DateTime]::UtcNow.ToString('o'); phase=$Phase; status=$status; etl_exists=[bool](Test-Path $etl); etl_length=$(if(Test-Path $etl){(Get-Item $etl).Length}else{0}); lastwake_exists=[bool](Test-Path $lastwake); kernelpower_exists=[bool](Test-Path $kernel) })
  exit 0
}

throw "Unsupported phase: $Phase"
'@

$hostPayloadPath = Join-Path $hostWorkRoot 'run-watchdog-lightweight-runtime-followup.guest.ps1'
Set-Content -Path $hostPayloadPath -Value $guestPayload -Encoding UTF8

function Invoke-Vmrun([string[]]$Arguments,[switch]$IgnoreExitCode) {
    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) { throw "vmrun failed ($LASTEXITCODE): $($output.Trim())" }
    $output.Trim()
}

function Wait-GuestReady([int]$TimeoutSeconds = 300) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            if ((Invoke-Vmrun @('-T','ws','list')) -notmatch [regex]::Escape($VmPath)) { Start-Sleep 3; continue }
            if ((Invoke-Vmrun @('-T','ws','checkToolsState',$VmPath)) -match 'running|installed') { return }
        } catch {}
        Start-Sleep -Seconds 3
    }
    throw 'Guest is not ready for vmrun guest operations.'
}

function Wait-GuestCommandReady([int]$TimeoutSeconds = 180) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-Vmrun @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'runProgramInGuest',$VmPath,'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-Command','exit 0') | Out-Null
            return
        } catch {}
        Start-Sleep -Seconds 3
    }
    throw 'Guest command execution did not become ready in time.'
}

function Ensure-VmStarted {
    if ((Invoke-Vmrun @('-T','ws','list')) -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun @('-T','ws','start',$VmPath) -IgnoreExitCode | Out-Null
    }
}

function Ensure-GuestDirectory([string]$GuestPath) {
    try { Invoke-Vmrun @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'createDirectoryInGuest',$VmPath,$GuestPath) | Out-Null } catch { if ($_.Exception.Message -notmatch 'already exists') { throw } }
}

function Copy-FromGuestBestEffort([string]$GuestPath,[string]$HostPath) {
    try {
        Invoke-Vmrun @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'CopyFileFromGuestToHost',$VmPath,$GuestPath,$HostPath) | Out-Null
        $true
    } catch { $false }
}

function Copy-ToGuest([string]$HostPath,[string]$GuestPath) {
    Invoke-Vmrun @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'CopyFileFromHostToGuest',$VmPath,$HostPath,$GuestPath) | Out-Null
}

function Get-ShellHealthBestEffort([int]$TimeoutSeconds = 180) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $last = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json
            if ($last.shell_healthy) { return $last }
        } catch {}
        Start-Sleep -Seconds 5
    }
    if ($null -ne $last) { return $last }
    [pscustomobject]@{ generated_utc=[DateTime]::UtcNow.ToString('o'); shell_healthy=$false; tools_state='unknown'; checks=[ordered]@{ explorer=$false; sihost=$false; shellhost=$false; ctfmon=$false; app=$false } }
}

function Invoke-GuestPhase([string]$GuestPayloadPath,[string]$Phase) {
    $script = @"
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path '$guestRoot' -Force | Out-Null
& '$GuestPayloadPath' -GuestRoot '$guestRoot' -Phase '$Phase'
exit 0
"@
    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script))
    Invoke-Vmrun @('-T','ws','-gu',$GuestUser,'-gp',$GuestPassword,'runProgramInGuest',$VmPath,'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-EncodedCommand',$encoded) | Out-Null
}

function New-PhaseErrorSummary([string]$PhaseName,[string]$Message) {
    [pscustomobject]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        phase = $PhaseName
        status = 'error'
        exact_runtime_read = $false
        exact_query_hits = 0
        errors = @($Message)
    }
}

function Write-Json([object]$Payload,[string]$HostPath,[string]$RepoPath='') {
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
    adjacent_only_candidates = 0
    path_only_candidates = 0
    no_hit_candidates = 0
    error_candidates = 0
    candidate_summary_files = @()
    errors = @()
}

$results = New-Object System.Collections.Generic.List[object]

try {
    $guestPayloadPath = Join-Path $guestScriptRoot 'run-watchdog-lightweight-runtime-followup.guest.ps1'
    $hostCandidateRoot = Join-Path $hostWorkRoot $candidateLabel
    $repoCandidateRoot = Join-Path $repoOutputRoot $candidateLabel
    New-Item -ItemType Directory -Path $hostCandidateRoot, $repoCandidateRoot -Force | Out-Null

    Invoke-Vmrun @('-T','ws','revertToSnapshot',$VmPath,$SnapshotName) | Out-Null
    Invoke-Vmrun @('-T','ws','start',$VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-GuestCommandReady
    Ensure-GuestDirectory $guestScriptRoot
    Ensure-GuestDirectory $guestRoot
    Copy-ToGuest $hostPayloadPath $guestPayloadPath

    $shellBefore = Get-ShellHealthBestEffort
    $phaseResults = [ordered]@{}
    foreach ($phaseName in @('capability-check','short-trigger-etw','split-trace-start','split-trigger','split-trace-stop')) {
        $phaseSummary = $null
        try {
            Wait-GuestReady
            Wait-GuestCommandReady
            Invoke-GuestPhase $guestPayloadPath $phaseName
            if ($phaseName -eq 'split-trigger') {
                Start-Sleep -Seconds 55
                Wait-GuestReady -TimeoutSeconds 420
                Wait-GuestCommandReady -TimeoutSeconds 240
            } elseif ($phaseName -eq 'short-trigger-etw') {
                Wait-GuestReady -TimeoutSeconds 420
                Wait-GuestCommandReady -TimeoutSeconds 240
            } elseif ($phaseName -eq 'split-trace-start') {
                Start-Sleep -Seconds 5
            }
            $guestPhaseSummary = Join-Path $guestRoot "$phaseName-summary.json"
            $hostPhaseSummary = Join-Path $hostCandidateRoot "$phaseName-summary.json"
            if (Copy-FromGuestBestEffort $guestPhaseSummary $hostPhaseSummary) {
                try { $phaseSummary = Get-Content -LiteralPath $hostPhaseSummary -Raw | ConvertFrom-Json } catch {}
            }
        }
        catch {
            $phaseSummary = New-PhaseErrorSummary -PhaseName $phaseName -Message $_.Exception.Message
        }
        if ($null -eq $phaseSummary) { $phaseSummary = [pscustomobject]@{ generated_utc=[DateTime]::UtcNow.ToString('o'); phase=$phaseName; status='copy-incomplete'; exact_runtime_read=$false; exact_query_hits=0 } }
        $phaseResults[$phaseName] = $phaseSummary
    }

    $shellAfter = Get-ShellHealthBestEffort
    $best = $phaseResults['short-trigger-etw']
    if ($phaseResults['split-trace-stop'].exact_runtime_read) { $best = $phaseResults['split-trace-stop'] }
    elseif ($best.status -eq 'copy-incomplete' -and $phaseResults['split-trace-stop'].status -ne 'copy-incomplete') { $best = $phaseResults['split-trace-stop'] }

    $candidateResult = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        candidate_id = $candidateId
        value_names = @('WatchdogResumeTimeout','WatchdogSleepTimeout')
        adjacent_value_names = @('Win32CalloutWatchdogBugcheckEnabled')
        snapshot_name = $SnapshotName
        shell_before = $shellBefore
        shell_after = $shellAfter
        best_phase = $best.phase
        status = $best.status
        exact_query_hits = $best.exact_query_hits
        exact_runtime_read = [bool]$best.exact_runtime_read
        s1_supported = $phaseResults['capability-check'].s1_supported
        modern_standby_supported = $phaseResults['capability-check'].modern_standby_supported
        phase_results = $phaseResults
        artifacts = [ordered]@{ summary = "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json" }
    }

    Write-Json $candidateResult (Join-Path $hostCandidateRoot 'summary.json') (Join-Path $repoCandidateRoot 'summary.json')
    $summary.candidate_summary_files += "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
    $results.Add([pscustomobject]$candidateResult) | Out-Null
    $summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
    $summary.exact_line_only_candidates = @($results | Where-Object { $_.status -eq 'exact-line-no-query' }).Count
    $summary.adjacent_only_candidates = @($results | Where-Object { $_.status -eq 'adjacent-query-only' }).Count
    $summary.path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
    $summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    $summary.error_candidates = @($results | Where-Object { $_.status -in @('error','copy-incomplete') }).Count
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.exact_line_only_candidates -gt 0) { 'exact-line-no-query' } elseif ($summary.adjacent_only_candidates -gt 0) { 'adjacent-query-only' } elseif ($summary.path_only_candidates -gt 0) { 'path-only-hit' } elseif ($summary.error_candidates -gt 0) { 'error' } else { 'no-hit' }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}

Write-Json $summary (Join-Path $hostWorkRoot 'summary.json') $repoSummaryPath
Write-Json @($results.ToArray()) (Join-Path $hostWorkRoot 'results.json') $repoResultsPath
Write-Output $repoSummaryPath

