[CmdletBinding()]
param(
    [string]$VmProfile = '',
    [string]$VmPath = '',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$SnapshotName = ''
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
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "executive-worker-threads-lightweight-runtime-$vmProfileTag-$stamp"
$repoOutputRoot = Join-Path $repoEvidenceBase $probeName
$hostWorkRoot = Join-Path $hostStagingBase $probeName
$guestBatchRoot = Join-Path $guestDiagBase $probeName
$candidateId = 'system.executive-additional-worker-threads'
$valueNames = @('AdditionalCriticalWorkerThreads', 'AdditionalDelayedWorkerThreads')

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

function Stop-ExistingSession {
    param([string]$Name)

    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $Name, '-ets') | Out-Null
    Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $Name) | Out-Null
}

function Set-ExecutivePair {
    param(
        [int]$CriticalValue,
        [int]$DelayedValue
    )

    $failures = @()
    foreach ($valueSpec in @(
        @{ Name = 'AdditionalCriticalWorkerThreads'; Value = $CriticalValue },
        @{ Name = 'AdditionalDelayedWorkerThreads'; Value = $DelayedValue }
    )) {
        $result = Invoke-CmdCapture -FilePath 'C:\Windows\System32\reg.exe' -Arguments @(
            'add',
            '"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive"',
            '/v', $valueSpec.Name,
            '/t', 'REG_DWORD',
            '/d', [string]$valueSpec.Value,
            '/f'
        )
        if ($result.exit_code -ne 0 -or -not [string]::IsNullOrWhiteSpace($result.stderr)) {
            $failures += [ordered]@{
                action = 'reg-add'
                target = $valueSpec.Name
                exit_code = $result.exit_code
                stdout = $result.stdout
                stderr = $result.stderr
            }
        }
    }
    return @($failures)
}

function Invoke-ExecutiveConcurrentBurst {
    param([int]$DurationSeconds = 6)

    $root = 'C:\RegProbe-Diag\executive-burst'
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $jobs = @()
    try {
        foreach ($id in 1..4) {
            $jobs += Start-Job -ArgumentList $root, $id, $DurationSeconds -ScriptBlock {
                param($RootPath, $JobId, $RunSeconds)

                $jobRoot = Join-Path $RootPath ("job" + $JobId)
                New-Item -ItemType Directory -Path $jobRoot -Force | Out-Null
                $deadline = (Get-Date).AddSeconds($RunSeconds)
                while ((Get-Date) -lt $deadline) {
                    foreach ($i in 1..6) {
                        $filePath = Join-Path $jobRoot ([Guid]::NewGuid().ToString() + '.bin')
                        $data = New-Object byte[] 262144
                        [System.IO.File]::WriteAllBytes($filePath, $data)
                    }

                    foreach ($commandSpec in @(
                        @{ File = 'cmd.exe'; Args = '/c ver' },
                        @{ File = 'cmd.exe'; Args = '/c whoami >nul' },
                        @{ File = 'cmd.exe'; Args = '/c set path >nul' }
                    )) {
                        $proc = Start-Process -FilePath $commandSpec.File -ArgumentList $commandSpec.Args -WindowStyle Hidden -PassThru
                        $proc.WaitForExit()
                    }

                    Get-Process | Select-Object -First 40 | Out-Null
                    Get-CimInstance Win32_Process | Select-Object -First 25 | Out-Null
                    Get-ChildItem -Path $jobRoot -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
                    Start-Sleep -Milliseconds 200
                }
            }
        }

        Wait-Job -Job $jobs | Out-Null
        Receive-Job -Job $jobs -ErrorAction SilentlyContinue | Out-Null
    }
    finally {
        if ($jobs) {
            Remove-Job -Job $jobs -Force -ErrorAction SilentlyContinue
        }
        Remove-Item -Path $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-TriggerProfile {
    param([string]$Profile)

    $failures = @()

    switch ($Profile) {
        'executive-concurrent-burst-short' {
            $failures += Set-ExecutivePair -CriticalValue 4 -DelayedValue 4
            Start-Sleep -Seconds 1
            Invoke-ExecutiveConcurrentBurst -DurationSeconds 6
            Start-Sleep -Seconds 2
        }
        'executive-concurrent-burst-only' {
            $failures += Set-ExecutivePair -CriticalValue 4 -DelayedValue 4
            Start-Sleep -Seconds 1
            Invoke-ExecutiveConcurrentBurst -DurationSeconds 6
        }
        default {
            throw "Unknown trigger profile: $Profile"
        }
    }

    return @($failures)
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
        [hashtable]$PerValueLineCounts,
        [hashtable]$PerValueQueryCounts,
        [string[]]$Errors
    )

    return [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
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
        per_value_line_counts = $PerValueLineCounts
        per_value_query_counts = $PerValueQueryCounts
        errors = @($Errors | Select-Object -First 4)
    }
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

$phaseSummaryPath = Join-Path $GuestRoot ("$Phase-summary.json")
$traceBaseName = if ($Phase -like 'split-*') { 'split-trace' } else { $Phase }
$etlPath = Join-Path $GuestRoot ("$traceBaseName.etl")
$csvPath = Join-Path $GuestRoot ("$traceBaseName.csv")
$sessionName = if ($Phase -like 'split-*') { 'RegProbeExecutiveRegistrySplit' } else { 'RegProbeExecutiveRegistry_' + ($Phase -replace '[^A-Za-z0-9]', '') }
$registryPathFragment = 'SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Executive'
$valueNames = @('AdditionalCriticalWorkerThreads', 'AdditionalDelayedWorkerThreads')

try {
    if ($Phase -eq 'short-trigger-etw') {
        foreach ($path in @($etlPath, $csvPath, $phaseSummaryPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            }
        }

        Stop-ExistingSession -Name $sessionName
        $create = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('create', 'trace', $sessionName, '-o', $etlPath, '-p', 'Microsoft-Windows-Kernel-Registry', '0xFFFF', '5', '-ets')
        if ($create.exit_code -ne 0) {
            throw "logman create trace failed: $($create.stderr)"
        }

        Start-Sleep -Seconds 1
        $triggerFailures = Invoke-TriggerProfile -Profile $TriggerProfile
        $stop = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $sessionName, '-ets')
        if ($stop.exit_code -ne 0) {
            throw "logman stop failed: $($stop.stderr)"
        }
        Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $sessionName) | Out-Null

        $etlExists = [bool](Test-Path -LiteralPath $etlPath)
        $etlLength = if ($etlExists) { (Get-Item -LiteralPath $etlPath).Length } else { 0 }
        $tracerpt = Invoke-CmdCapture -FilePath 'C:\Windows\System32\tracerpt.exe' -Arguments @($etlPath, '-o', $csvPath, '-of', 'CSV')
        if ($tracerpt.exit_code -ne 0) {
            throw "tracerpt failed: $($tracerpt.stderr)"
        }

        $csvExists = [bool](Test-Path -LiteralPath $csvPath)
        $csvLineCount = 0
        $pathLineCount = 0
        $exactLineCount = 0
        $exactQueryHits = 0
        $perValueLineCounts = [ordered]@{}
        $perValueQueryCounts = [ordered]@{}
        foreach ($name in $valueNames) {
            $perValueLineCounts[$name] = 0
            $perValueQueryCounts[$name] = 0
        }

        if ($csvExists) {
            $pathPattern = [regex]::Escape($registryPathFragment)
            $reader = [System.IO.File]::OpenText($csvPath)
            try {
                while (($line = $reader.ReadLine()) -ne $null) {
                    $csvLineCount++
                    if ($line -match $pathPattern) {
                        $pathLineCount++
                    }
                    foreach ($name in $valueNames) {
                        $valuePattern = [regex]::Escape($name)
                        if ($line -match $valuePattern) {
                            $exactLineCount++
                            $perValueLineCounts[$name]++
                            if ($line -match 'RegQueryValue|QueryValue') {
                                $exactQueryHits++
                                $perValueQueryCounts[$name]++
                            }
                        }
                    }
                }
            }
            finally {
                $reader.Close()
            }
        }

        $status = if ($exactQueryHits -gt 0) { 'exact-hit' } elseif ($exactLineCount -gt 0) { 'exact-line-no-query' } elseif ($pathLineCount -gt 0) { 'path-only-hit' } else { 'no-hit' }
        $payload = Build-CompactSummary -Status $status -EtlExists $etlExists -CsvExists $csvExists -EtlLength $etlLength -CsvLineCount $csvLineCount -ExactLineCount $exactLineCount -ExactQueryHits $exactQueryHits -PathLineCount $pathLineCount -TriggerFailureCount @($triggerFailures).Count -TriggerFailures @($triggerFailures) -PerValueLineCounts $perValueLineCounts -PerValueQueryCounts $perValueQueryCounts -Errors @()
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    elseif ($Phase -eq 'split-trace-start') {
        foreach ($path in @($etlPath, $csvPath, $phaseSummaryPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            }
        }

        Stop-ExistingSession -Name $sessionName
        $create = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('create', 'trace', $sessionName, '-o', $etlPath, '-p', 'Microsoft-Windows-Kernel-Registry', '0xFFFF', '5', '-bs', '64', '-nb', '32', '64', '-ets')
        if ($create.exit_code -ne 0) {
            throw "logman create trace failed: $($create.stderr)"
        }

        Start-Sleep -Seconds 1
        $payload = Build-CompactSummary -Status 'trace-started' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount 0 -TriggerFailures @() -PerValueLineCounts ([ordered]@{}) -PerValueQueryCounts ([ordered]@{}) -Errors @()
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    elseif ($Phase -eq 'split-trigger') {
        $triggerFailures = Invoke-TriggerProfile -Profile $TriggerProfile
        $payload = Build-CompactSummary -Status 'trigger-complete' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount @($triggerFailures).Count -TriggerFailures @($triggerFailures) -PerValueLineCounts ([ordered]@{}) -PerValueQueryCounts ([ordered]@{}) -Errors @()
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    elseif ($Phase -eq 'split-trace-stop') {
        $stop = Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('stop', $sessionName, '-ets')
        if ($stop.exit_code -ne 0) {
            throw "logman stop failed: $($stop.stderr)"
        }
        Invoke-CmdCapture -FilePath 'C:\Windows\System32\logman.exe' -Arguments @('delete', $sessionName) | Out-Null

        $etlExists = [bool](Test-Path -LiteralPath $etlPath)
        $etlLength = if ($etlExists) { (Get-Item -LiteralPath $etlPath).Length } else { 0 }
        $tracerpt = Invoke-CmdCapture -FilePath 'C:\Windows\System32\tracerpt.exe' -Arguments @($etlPath, '-o', $csvPath, '-of', 'CSV')
        if ($tracerpt.exit_code -ne 0) {
            throw "tracerpt failed: $($tracerpt.stderr)"
        }

        $csvExists = [bool](Test-Path -LiteralPath $csvPath)
        $csvLineCount = 0
        $pathLineCount = 0
        $exactLineCount = 0
        $exactQueryHits = 0
        $perValueLineCounts = [ordered]@{}
        $perValueQueryCounts = [ordered]@{}
        foreach ($name in $valueNames) {
            $perValueLineCounts[$name] = 0
            $perValueQueryCounts[$name] = 0
        }

        if ($csvExists) {
            $pathPattern = [regex]::Escape($registryPathFragment)
            $reader = [System.IO.File]::OpenText($csvPath)
            try {
                while (($line = $reader.ReadLine()) -ne $null) {
                    $csvLineCount++
                    if ($line -match $pathPattern) {
                        $pathLineCount++
                    }
                    foreach ($name in $valueNames) {
                        $valuePattern = [regex]::Escape($name)
                        if ($line -match $valuePattern) {
                            $exactLineCount++
                            $perValueLineCounts[$name]++
                            if ($line -match 'RegQueryValue|QueryValue') {
                                $exactQueryHits++
                                $perValueQueryCounts[$name]++
                            }
                        }
                    }
                }
            }
            finally {
                $reader.Close()
            }
        }

        $status = if ($exactQueryHits -gt 0) { 'exact-hit' } elseif ($exactLineCount -gt 0) { 'exact-line-no-query' } elseif ($pathLineCount -gt 0) { 'path-only-hit' } else { 'no-hit' }
        $payload = Build-CompactSummary -Status $status -EtlExists $etlExists -CsvExists $csvExists -EtlLength $etlLength -CsvLineCount $csvLineCount -ExactLineCount $exactLineCount -ExactQueryHits $exactQueryHits -PathLineCount $pathLineCount -TriggerFailureCount 0 -TriggerFailures @() -PerValueLineCounts $perValueLineCounts -PerValueQueryCounts $perValueQueryCounts -Errors @()
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    else {
        throw "Unsupported phase: $Phase"
    }
}
catch {
    $payload = Build-CompactSummary -Status 'error' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount 0 -TriggerFailures @() -PerValueLineCounts ([ordered]@{}) -PerValueQueryCounts ([ordered]@{}) -Errors @($_.Exception.Message)
    try {
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    catch {
    }
}

exit 0
'@

$hostPayloadPath = Join-Path $hostWorkRoot 'run-executive-worker-threads-lightweight-runtime-followup.guest.ps1'
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
    param([int]$TimeoutSeconds = 300)

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

        Start-Sleep -Seconds 3
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
                '-NoProfile',
                '-ExecutionPolicy',
                'Bypass',
                '-Command',
                'exit 0'
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
            Invoke-Vmrun -Arguments @(
                '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
                'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath
            ) | Out-Null

            if (Test-Path -LiteralPath $HostPath) {
                return $true
            }
        }
        catch {
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    return $false
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath
    ) | Out-Null
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

    if ($null -eq $last) {
        return [pscustomobject]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            vm_path = $VmPath
            vm_running = $false
            tools_state = 'unknown'
            process_query_error = 'shell-health-timeout'
            shell_healthy = $false
            checks = [ordered]@{
                explorer = $false
                sihost = $false
                shellhost = $false
                ctfmon = $false
                app = $false
            }
        }
    }

    return $last
}

function Invoke-GuestPhase {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPayloadPath,
        [Parameter(Mandatory = $true)]
        [string]$GuestRoot,
        [Parameter(Mandatory = $true)]
        [string]$Phase,
        [Parameter(Mandatory = $true)]
        [string]$TriggerProfile
    )

    $script = @"
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path '$GuestRoot' -Force | Out-Null
& '$GuestPayloadPath' -GuestRoot '$GuestRoot' -Phase '$Phase' -TriggerProfile '$TriggerProfile'
exit 0
"@

    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script))
    Invoke-Vmrun -Arguments @(
        '-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword,
        'runProgramInGuest', $VmPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-EncodedCommand',
        $encoded
    ) | Out-Null
}

function Invoke-GuestPhaseSafe {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPayloadPath,
        [Parameter(Mandatory = $true)]
        [string]$GuestRoot,
        [Parameter(Mandatory = $true)]
        [string]$Phase,
        [Parameter(Mandatory = $true)]
        [string]$TriggerProfile,
        [int]$Attempts = 3
    )

    $lastError = $null
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Ensure-VmStarted
            Wait-GuestReady
            Wait-GuestCommandReady
            Invoke-GuestPhase -GuestPayloadPath $GuestPayloadPath -GuestRoot $GuestRoot -Phase $Phase -TriggerProfile $TriggerProfile
            return $null
        }
        catch {
            $lastError = $_.Exception.Message
            Start-Sleep -Seconds (5 * $attempt)
        }
    }

    return $lastError
}

function Write-Json {
    param([object]$Payload, [string]$HostPath, [string]$RepoPath = '')

    $json = ConvertTo-Json -InputObject $Payload -Depth 8
    Set-Content -Path $HostPath -Value $json -Encoding UTF8
    if ($RepoPath) {
        Copy-Item -Path $HostPath -Destination $RepoPath -Force
    }
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
    $guestPayloadPath = Join-Path $guestScriptRoot 'run-executive-worker-threads-lightweight-runtime-followup.guest.ps1'
    $candidateLabel = ($candidateId -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
    $hostCandidateRoot = Join-Path $hostWorkRoot $candidateLabel
    $repoCandidateRoot = Join-Path $repoOutputRoot $candidateLabel
    $guestCandidateRoot = Join-Path $guestBatchRoot $candidateLabel

    New-Item -ItemType Directory -Path $hostCandidateRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $repoCandidateRoot -Force | Out-Null

    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady
    Wait-GuestCommandReady
    Ensure-GuestDirectory -GuestPath $guestScriptRoot
    Ensure-GuestDirectory -GuestPath $guestCandidateRoot
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    $shellBefore = Get-ShellHealthBestEffort
    $phaseResults = [ordered]@{}

    foreach ($phaseSpec in @(
        [ordered]@{ phase = 'short-trigger-etw'; trigger = 'executive-concurrent-burst-short' },
        [ordered]@{ phase = 'split-trace-start'; trigger = 'executive-concurrent-burst-only' },
        [ordered]@{ phase = 'split-trigger'; trigger = 'executive-concurrent-burst-only' },
        [ordered]@{ phase = 'split-trace-stop'; trigger = 'executive-concurrent-burst-only' }
    )) {
        $phaseName = $phaseSpec.phase
        $phaseError = Invoke-GuestPhaseSafe -GuestPayloadPath $guestPayloadPath -GuestRoot $guestCandidateRoot -Phase $phaseName -TriggerProfile $phaseSpec.trigger

        if ($phaseName -in @('split-trace-start', 'split-trigger')) {
            Start-Sleep -Seconds 10
            Wait-GuestReady
            Wait-GuestCommandReady
        }

        $guestPhaseSummaryPath = Join-Path $guestCandidateRoot ("$phaseName-summary.json")
        $hostPhaseSummaryPath = Join-Path $hostCandidateRoot ("$phaseName-summary.json")
        $phaseSummary = $null
        $copied = Copy-FromGuestBestEffort -GuestPath $guestPhaseSummaryPath -HostPath $hostPhaseSummaryPath
        if ($copied) {
            try {
                $phaseSummary = Get-Content -LiteralPath $hostPhaseSummaryPath -Raw | ConvertFrom-Json
            }
            catch {
            }
        }

        if ($null -eq $phaseSummary) {
            $phaseSummary = [pscustomobject]@{
                generated_utc = [DateTime]::UtcNow.ToString('o')
                phase = $phaseName
                trigger_profile = $phaseSpec.trigger
                status = 'copy-incomplete'
                exact_runtime_read = $false
                exact_query_hits = 0
                errors = @('Guest summary could not be copied back to the host.')
            }
        }
        if ($phaseError) {
            $phaseSummary | Add-Member -NotePropertyName host_phase_error -NotePropertyValue $phaseError -Force
        }

        $phaseResults[$phaseName] = $phaseSummary
    }

    $shellAfter = Get-ShellHealthBestEffort

    $best = $phaseResults['short-trigger-etw']
    foreach ($phaseName in @('split-trace-stop', 'split-trigger', 'split-trace-start')) {
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
        value_names = $valueNames
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
    $summary.error_candidates = @($results | Where-Object { $_.status -in @('error', 'copy-incomplete') }).Count
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.exact_line_only_candidates -gt 0) { 'exact-line-no-query' } elseif ($summary.path_only_candidates -gt 0) { 'path-only-hit' } elseif ($summary.error_candidates -gt 0) { 'error' } else { 'no-hit' }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}

Write-Json -Payload $summary -HostPath (Join-Path $hostWorkRoot 'summary.json') -RepoPath $repoSummaryPath
Write-Json -Payload @($results.ToArray()) -HostPath (Join-Path $hostWorkRoot 'results.json') -RepoPath $repoResultsPath
Write-Output $repoSummaryPath
