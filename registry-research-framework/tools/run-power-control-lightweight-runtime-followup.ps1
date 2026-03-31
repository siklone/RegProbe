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
    $SnapshotName = 'RegProbe-Baseline-Clean-20260329'
}

$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "power-control-lightweight-runtime-$vmProfileTag-$stamp"
$repoOutputRoot = Join-Path $repoEvidenceBase $probeName
$hostWorkRoot = Join-Path $hostStagingBase $probeName
$guestBatchRoot = Join-Path $guestDiagBase $probeName

$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'

$candidates = @(
    [ordered]@{
        candidate_id = 'power.control.class1-initial-unpark-count'
        value_name = 'Class1InitialUnparkCount'
        short_trigger_profile = 'processor-current-scheme-refresh-short'
        split_trigger_profile = 'processor-current-scheme-refresh-only'
    },
    [ordered]@{
        candidate_id = 'power.control.perf-calculate-actual-utilization'
        value_name = 'PerfCalculateActualUtilization'
        short_trigger_profile = 'perf-current-scheme-refresh-short'
        split_trigger_profile = 'perf-current-scheme-refresh-only'
    },
    [ordered]@{
        candidate_id = 'power.control.hiber-file-size-percent'
        value_name = 'HiberFileSizePercent'
        short_trigger_profile = 'hiber-file-size-multi-trigger-short'
        split_trigger_profile = 'hiber-file-size-multi-trigger-only'
    },
    [ordered]@{
        candidate_id = 'power.control.mf-buffering-threshold'
        value_name = 'MfBufferingThreshold'
        short_trigger_profile = 'mf-io-burst-short'
        split_trigger_profile = 'mf-io-burst-only'
    },
    [ordered]@{
        candidate_id = 'power.control.timer-rebase-threshold-on-drips-exit'
        value_name = 'TimerRebaseThresholdOnDripsExit'
        capability_check_profile = 'modern-standby-capability-check'
        short_trigger_profile = 'drips-exit-short'
        split_trigger_profile = 'drips-exit-only'
    }
)

if (@($CandidateIds).Count -gt 0) {
    $wanted = @($CandidateIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $candidates = @($candidates | Where-Object { $wanted -contains $_.candidate_id })
    if (@($candidates).Count -eq 0) {
        throw "No lightweight runtime candidates matched the requested ids: $($wanted -join ', ')"
    }
}

New-Item -ItemType Directory -Path $repoOutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $hostWorkRoot -Force | Out-Null

$guestPayload = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,
    [Parameter(Mandatory = $true)]
    [string]$ValueName,
    [Parameter(Mandatory = $true)]
    [string]$Phase,
    [Parameter(Mandatory = $true)]
    [string]$TriggerProfile,
    [Parameter(Mandatory = $true)]
    [string]$ResultVariableName
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

function Set-GuestInfoValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $vmtoolsd = 'C:\Program Files\VMware\VMware Tools\vmtoolsd.exe'
    if (-not (Test-Path -LiteralPath $vmtoolsd)) {
        throw 'vmtoolsd.exe was not found in the guest.'
    }

    $result = Invoke-CmdCapture -FilePath $vmtoolsd -Arguments @('--cmd', "info-set guestinfo.$Name $Value")
    if ($result.exit_code -ne 0) {
        throw "vmtoolsd info-set failed: $($result.stderr)"
    }
}

function Publish-CompactResult {
    param([hashtable]$Payload)

    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(($Payload | ConvertTo-Json -Depth 6 -Compress)))
    Set-GuestInfoValue -Name $ResultVariableName -Value $encoded
}

function Publish-CompactResultBestEffort {
    param([hashtable]$Payload)

    try {
        Publish-CompactResult -Payload $Payload
        return $null
    }
    catch {
        return $_.Exception.Message
    }
}

function Get-ModernStandbyCapability {
    $powercfg = Invoke-CmdCapture -FilePath 'C:\Windows\System32\cmd.exe' -Arguments @('/c', 'powercfg /a')
    $raw = if (-not [string]::IsNullOrWhiteSpace($powercfg.stdout)) { $powercfg.stdout } else { $powercfg.stderr }
    $availableSection = $raw
    $marker = 'The following sleep states are not available on this system:'
    if ($availableSection -like "*$marker*") {
        $availableSection = $availableSection.Split(@($marker), 2, [System.StringSplitOptions]::None)[0]
    }

    $supported = ($availableSection -match 'Standby \(S0 Low Power Idle\)' -or $availableSection -match 'Modern Standby')
    return [ordered]@{
        modern_standby_supported = [bool]$supported
        powercfg_exit_code = $powercfg.exit_code
        powercfg_output = (($raw -split "(`r`n|`n|`r)") | Select-Object -First 24) -join [Environment]::NewLine
    }
}

function Invoke-TriggerProfile {
    param([string]$Profile)

    $commands = switch ($Profile) {
        'modern-standby-capability-check' {
            @()
        }
        'processor-current-scheme-refresh-short' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 64 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFEPP 100',
                'timeout /t 1 /nobreak',
                'powercfg /setactive SCHEME_CURRENT',
                'timeout /t 2 /nobreak'
            )
        }
        'processor-current-scheme-refresh-only' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 64 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFEPP 100',
                'timeout /t 1 /nobreak',
                'powercfg /setactive SCHEME_CURRENT'
            )
        }
        'perf-current-scheme-refresh-short' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 1 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFEPP 80',
                'timeout /t 1 /nobreak',
                'powercfg /setactive SCHEME_CURRENT',
                'timeout /t 1 /nobreak',
                'powershell -NoProfile -Command "$end=(Get-Date).AddSeconds(3); while((Get-Date) -lt $end){ 1..8000 | ForEach-Object { [Math]::Sqrt($_) | Out-Null } }"',
                'timeout /t 1 /nobreak'
            )
        }
        'perf-current-scheme-refresh-only' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 1 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFEPP 80',
                'timeout /t 1 /nobreak',
                'powercfg /setactive SCHEME_CURRENT',
                'timeout /t 1 /nobreak',
                'powershell -NoProfile -Command "$end=(Get-Date).AddSeconds(3); while((Get-Date) -lt $end){ 1..8000 | ForEach-Object { [Math]::Sqrt($_) | Out-Null } }"'
            )
        }
        'mf-io-burst-short' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 0 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powershell -NoProfile -Command "$path=''C:\RegProbe-Diag\io-burst''; New-Item -ItemType Directory -Path $path -Force | Out-Null; 1..100 | ForEach-Object { [byte[]]$data = New-Object byte[] 1048576; [System.IO.File]::WriteAllBytes((Join-Path $path (''file'' + $_ + ''.bin'')), $data) }; Remove-Item -Path $path -Recurse -Force"',
                'timeout /t 1 /nobreak'
            )
        }
        'mf-io-burst-only' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 0 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powershell -NoProfile -Command "$path=''C:\RegProbe-Diag\io-burst''; New-Item -ItemType Directory -Path $path -Force | Out-Null; 1..100 | ForEach-Object { [byte[]]$data = New-Object byte[] 1048576; [System.IO.File]::WriteAllBytes((Join-Path $path (''file'' + $_ + ''.bin'')), $data) }; Remove-Item -Path $path -Recurse -Force"'
            )
        }
        'hiber-file-size-multi-trigger-short' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 0 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /hibernate on',
                'timeout /t 1 /nobreak',
                'powercfg /hibernate off',
                'timeout /t 1 /nobreak',
                'powercfg /a',
                'timeout /t 1 /nobreak',
                'powershell -NoProfile -Command "$path=''C:\RegProbe-Diag\hiber-io-burst''; New-Item -ItemType Directory -Path $path -Force | Out-Null; 1..48 | ForEach-Object { [byte[]]$data = New-Object byte[] 2097152; [System.IO.File]::WriteAllBytes((Join-Path $path (''hiber'' + $_ + ''.bin'')), $data) }; Remove-Item -Path $path -Recurse -Force"',
                'timeout /t 1 /nobreak'
            )
        }
        'hiber-file-size-multi-trigger-only' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 0 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /hibernate on',
                'timeout /t 1 /nobreak',
                'powercfg /hibernate off',
                'timeout /t 1 /nobreak',
                'powercfg /a',
                'timeout /t 1 /nobreak',
                'powershell -NoProfile -Command "$path=''C:\RegProbe-Diag\hiber-io-burst''; New-Item -ItemType Directory -Path $path -Force | Out-Null; 1..48 | ForEach-Object { [byte[]]$data = New-Object byte[] 2097152; [System.IO.File]::WriteAllBytes((Join-Path $path (''hiber'' + $_ + ''.bin'')), $data) }; Remove-Item -Path $path -Recurse -Force"'
            )
        }
        'drips-exit-short' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 60 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /sleepstudy',
                'timeout /t 1 /nobreak'
            )
        }
        'drips-exit-only' {
            @(
                ('reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power" /v "{0}" /t REG_DWORD /d 60 /f' -f $ValueName),
                'timeout /t 1 /nobreak',
                'powercfg /sleepstudy'
            )
        }
        default {
            throw "Unknown trigger profile: $Profile"
        }
    }

    $failures = @()
    foreach ($command in $commands) {
        $result = Invoke-CmdCapture -FilePath 'C:\Windows\System32\cmd.exe' -Arguments @('/c', $command)
        if ($result.exit_code -ne 0 -or -not [string]::IsNullOrWhiteSpace($result.stderr)) {
            $failures += [ordered]@{
                command = $command
                exit_code = $result.exit_code
                stdout = $result.stdout
                stderr = $result.stderr
            }
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
        [string[]]$Errors
    )

    return [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        value_name = $ValueName
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
        errors = @($Errors | Select-Object -First 3)
    }
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

$phaseSummaryPath = Join-Path $GuestRoot ("$Phase-summary.json")
$etlPath = Join-Path $GuestRoot ("$Phase.etl")
$csvPath = Join-Path $GuestRoot ("$Phase.csv")
$sessionName = ('RegProbeRegistry_' + ($ValueName -replace '[^A-Za-z0-9]', '') + '_' + ($Phase -replace '[^A-Za-z0-9]', ''))
$registryPathFragment = 'SYSTEM\\CurrentControlSet\\Control\\Power'

try {
    if ($Phase -eq 'capability-check') {
        $capability = Get-ModernStandbyCapability
        $status = if ($capability.modern_standby_supported) { 'modern-standby-supported' } else { 'vm-standby-limitation' }
        $payload = Build-CompactSummary -Status $status -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount 0 -Errors @()
        $payload['modern_standby_supported'] = [bool]$capability.modern_standby_supported
        $payload['powercfg_exit_code'] = $capability.powercfg_exit_code
        $payload['powercfg_output'] = $capability.powercfg_output
        $publishError = Publish-CompactResultBestEffort -Payload $payload
        if (-not [string]::IsNullOrWhiteSpace($publishError)) {
            $payload.errors = @($payload.errors + "guestvar publish: $publishError")
        }
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    elseif ($Phase -eq 'short-trigger-etw') {
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
        if ($csvExists) {
            $pathPattern = [regex]::Escape($registryPathFragment)
            $valuePattern = [regex]::Escape($ValueName)
            $reader = [System.IO.File]::OpenText($csvPath)
            try {
                while (($line = $reader.ReadLine()) -ne $null) {
                    $csvLineCount++
                    if ($line -match $pathPattern) {
                        $pathLineCount++
                    }
                    if ($line -match $valuePattern) {
                        $exactLineCount++
                        if ($line -match 'RegQueryValue|QueryValue') {
                            $exactQueryHits++
                        }
                    }
                }
            }
            finally {
                $reader.Close()
            }
        }

        $status = if ($exactQueryHits -gt 0) { 'exact-hit' } elseif ($exactLineCount -gt 0) { 'exact-line-no-query' } elseif ($pathLineCount -gt 0) { 'path-only-hit' } else { 'no-hit' }
        $payload = Build-CompactSummary -Status $status -EtlExists $etlExists -CsvExists $csvExists -EtlLength $etlLength -CsvLineCount $csvLineCount -ExactLineCount $exactLineCount -ExactQueryHits $exactQueryHits -PathLineCount $pathLineCount -TriggerFailureCount @($triggerFailures).Count -Errors @()
        $publishError = Publish-CompactResultBestEffort -Payload $payload
        if (-not [string]::IsNullOrWhiteSpace($publishError)) {
            $payload.errors = @($payload.errors + "guestvar publish: $publishError")
        }
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
        $payload = Build-CompactSummary -Status 'trace-started' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount 0 -Errors @()
        $publishError = Publish-CompactResultBestEffort -Payload $payload
        if (-not [string]::IsNullOrWhiteSpace($publishError)) {
            $payload.errors = @($payload.errors + "guestvar publish: $publishError")
        }
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    elseif ($Phase -eq 'split-trigger') {
        $triggerFailures = Invoke-TriggerProfile -Profile $TriggerProfile
        $payload = Build-CompactSummary -Status 'trigger-complete' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount @($triggerFailures).Count -Errors @()
        $publishError = Publish-CompactResultBestEffort -Payload $payload
        if (-not [string]::IsNullOrWhiteSpace($publishError)) {
            $payload.errors = @($payload.errors + "guestvar publish: $publishError")
        }
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
        if ($csvExists) {
            $pathPattern = [regex]::Escape($registryPathFragment)
            $valuePattern = [regex]::Escape($ValueName)
            $reader = [System.IO.File]::OpenText($csvPath)
            try {
                while (($line = $reader.ReadLine()) -ne $null) {
                    $csvLineCount++
                    if ($line -match $pathPattern) {
                        $pathLineCount++
                    }
                    if ($line -match $valuePattern) {
                        $exactLineCount++
                        if ($line -match 'RegQueryValue|QueryValue') {
                            $exactQueryHits++
                        }
                    }
                }
            }
            finally {
                $reader.Close()
            }
        }

        $status = if ($exactQueryHits -gt 0) { 'exact-hit' } elseif ($exactLineCount -gt 0) { 'exact-line-no-query' } elseif ($pathLineCount -gt 0) { 'path-only-hit' } else { 'no-hit' }
        $payload = Build-CompactSummary -Status $status -EtlExists $etlExists -CsvExists $csvExists -EtlLength $etlLength -CsvLineCount $csvLineCount -ExactLineCount $exactLineCount -ExactQueryHits $exactQueryHits -PathLineCount $pathLineCount -TriggerFailureCount 0 -Errors @()
        $publishError = Publish-CompactResultBestEffort -Payload $payload
        if (-not [string]::IsNullOrWhiteSpace($publishError)) {
            $payload.errors = @($payload.errors + "guestvar publish: $publishError")
        }
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    else {
        throw "Unsupported phase: $Phase"
    }
}
catch {
    $payload = Build-CompactSummary -Status 'error' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -TriggerFailureCount 0 -Errors @($_.Exception.Message)
    try {
        $publishError = Publish-CompactResultBestEffort -Payload $payload
        if (-not [string]::IsNullOrWhiteSpace($publishError)) {
            $payload.errors = @($payload.errors + "guestvar publish: $publishError")
        }
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    catch {
    }
}

exit 0
'@

$hostPayloadPath = Join-Path $hostWorkRoot 'run-power-control-lightweight-runtime-followup.guest.ps1'
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

function Read-GuestVariableBestEffort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [int]$Attempts = 12,
        [int]$DelaySeconds = 2
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $value = Invoke-Vmrun -Arguments @('-T', 'ws', 'readVariable', $VmPath, 'guestVar', $Name)
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                return $value.Trim()
            }
        }
        catch {
        }
        Start-Sleep -Seconds $DelaySeconds
    }

    return $null
}

function Convert-GuestVariableToObject {
    param([string]$Encoded)

    if ([string]::IsNullOrWhiteSpace($Encoded)) {
        return $null
    }

    $json = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Encoded))
    return $json | ConvertFrom-Json
}

function Invoke-GuestPhase {
    param(
        [Parameter(Mandatory = $true)]
        [string]$GuestPayloadPath,
        [Parameter(Mandatory = $true)]
        [string]$GuestRoot,
        [Parameter(Mandatory = $true)]
        [string]$ValueName,
        [Parameter(Mandatory = $true)]
        [string]$Phase,
        [Parameter(Mandatory = $true)]
        [string]$TriggerProfile,
        [Parameter(Mandatory = $true)]
        [string]$ResultVariableName
    )

    $script = @"
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path '$GuestRoot' -Force | Out-Null
& '$GuestPayloadPath' -GuestRoot '$GuestRoot' -ValueName '$ValueName' -Phase '$Phase' -TriggerProfile '$TriggerProfile' -ResultVariableName '$ResultVariableName'
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
        [string]$ValueName,
        [Parameter(Mandatory = $true)]
        [string]$Phase,
        [Parameter(Mandatory = $true)]
        [string]$TriggerProfile,
        [Parameter(Mandatory = $true)]
        [string]$ResultVariableName,
        [int]$Attempts = 3
    )

    $lastError = $null
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Ensure-VmStarted
            Wait-GuestReady
            Wait-GuestCommandReady
            Invoke-GuestPhase -GuestPayloadPath $GuestPayloadPath -GuestRoot $GuestRoot -ValueName $ValueName -Phase $Phase -TriggerProfile $TriggerProfile -ResultVariableName $ResultVariableName
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
    total_candidates = @($candidates).Count
    exact_hit_candidates = 0
    exact_line_only_candidates = 0
    path_only_candidates = 0
    no_hit_candidates = 0
    limitation_candidates = 0
    error_candidates = 0
    candidate_summary_files = @()
    errors = @()
}

$results = New-Object System.Collections.Generic.List[object]

try {
    $guestPayloadPath = Join-Path $guestScriptRoot 'run-power-control-lightweight-runtime-followup.guest.ps1'

    foreach ($candidate in $candidates) {
        $candidateLabel = ($candidate.candidate_id -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
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
        $skipRuntimePhases = $false

        if ($candidate.Contains('capability_check_profile')) {
            $capabilityVariableName = ("rpl.$stamp." + $candidateLabel.Substring(0, [Math]::Min(12, $candidateLabel.Length)) + '.capabilitycheck')
            $capabilityError = Invoke-GuestPhaseSafe -GuestPayloadPath $guestPayloadPath -GuestRoot $guestCandidateRoot -ValueName $candidate.value_name -Phase 'capability-check' -TriggerProfile $candidate.capability_check_profile -ResultVariableName $capabilityVariableName
            $encodedCapability = Read-GuestVariableBestEffort -Name $capabilityVariableName
            $capabilitySummary = Convert-GuestVariableToObject -Encoded $encodedCapability
            $guestCapabilityPath = Join-Path $guestCandidateRoot 'capability-check-summary.json'
            $hostCapabilityPath = Join-Path $hostCandidateRoot 'capability-check-summary.json'
            if ($null -eq $capabilitySummary) {
                $copied = Copy-FromGuestBestEffort -GuestPath $guestCapabilityPath -HostPath $hostCapabilityPath
                if ($copied) {
                    try {
                        $capabilitySummary = Get-Content -LiteralPath $hostCapabilityPath -Raw | ConvertFrom-Json
                    }
                    catch {
                    }
                }
            }
            if ($null -eq $capabilitySummary) {
                $capabilitySummary = [pscustomobject]@{
                    generated_utc = [DateTime]::UtcNow.ToString('o')
                    value_name = $candidate.value_name
                    phase = 'capability-check'
                    trigger_profile = $candidate.capability_check_profile
                    status = 'no-guestvar'
                    modern_standby_supported = $false
                    errors = @('Guest capability summary was not returned through guestVar.')
                }
            }
            if ($capabilityError) {
                $capabilitySummary | Add-Member -NotePropertyName host_phase_error -NotePropertyValue $capabilityError -Force
            }
            $phaseResults['capability-check'] = $capabilitySummary
            if (-not [bool]$capabilitySummary.modern_standby_supported) {
                $skipRuntimePhases = $true
            }
        }

        if (-not $skipRuntimePhases) {
            foreach ($phaseSpec in @(
                [ordered]@{ phase = 'short-trigger-etw'; trigger = $candidate.short_trigger_profile },
                [ordered]@{ phase = 'split-trace-start'; trigger = $candidate.split_trigger_profile },
                [ordered]@{ phase = 'split-trigger'; trigger = $candidate.split_trigger_profile },
                [ordered]@{ phase = 'split-trace-stop'; trigger = $candidate.split_trigger_profile }
            )) {
                $phaseName = $phaseSpec.phase
                $variableName = ("rpl.$stamp." + $candidateLabel.Substring(0, [Math]::Min(12, $candidateLabel.Length)) + '.' + ($phaseName -replace '[^A-Za-z0-9]', '').ToLowerInvariant())
                $phaseError = $null
                $phaseError = Invoke-GuestPhaseSafe -GuestPayloadPath $guestPayloadPath -GuestRoot $guestCandidateRoot -ValueName $candidate.value_name -Phase $phaseName -TriggerProfile $phaseSpec.trigger -ResultVariableName $variableName

                if ($phaseName -in @('split-trace-start', 'split-trigger')) {
                    Start-Sleep -Seconds 10
                    Wait-GuestReady
                    Wait-GuestCommandReady
                }

                $encodedSummary = Read-GuestVariableBestEffort -Name $variableName
                $phaseSummary = Convert-GuestVariableToObject -Encoded $encodedSummary
                $guestPhaseSummaryPath = Join-Path $guestCandidateRoot ("$phaseName-summary.json")
                $hostPhaseSummaryPath = Join-Path $hostCandidateRoot ("$phaseName-summary.json")
                if ($null -eq $phaseSummary) {
                    $copied = Copy-FromGuestBestEffort -GuestPath $guestPhaseSummaryPath -HostPath $hostPhaseSummaryPath
                    if ($copied) {
                        try {
                            $phaseSummary = Get-Content -LiteralPath $hostPhaseSummaryPath -Raw | ConvertFrom-Json
                        }
                        catch {
                        }
                    }
                }
                if ($null -eq $phaseSummary) {
                    $phaseSummary = [pscustomobject]@{
                        generated_utc = [DateTime]::UtcNow.ToString('o')
                        value_name = $candidate.value_name
                        phase = $phaseName
                        trigger_profile = $phaseSpec.trigger
                        status = 'no-guestvar'
                        exact_runtime_read = $false
                        exact_query_hits = 0
                        errors = @('Guest summary was not returned through guestVar.')
                    }
                }
                if ($phaseError) {
                    $phaseSummary | Add-Member -NotePropertyName host_phase_error -NotePropertyValue $phaseError -Force
                }

                $phaseResults[$phaseName] = $phaseSummary
            }
        }

        $shellAfter = Get-ShellHealthBestEffort

        $best = if ($phaseResults.Contains('short-trigger-etw')) { $phaseResults['short-trigger-etw'] } else { $phaseResults['capability-check'] }
        if (-not $skipRuntimePhases) {
            foreach ($phaseName in @('split-trace-stop', 'split-trigger', 'split-trace-start')) {
                $candidatePhase = $phaseResults[$phaseName]
                if ($candidatePhase.exact_runtime_read) {
                    $best = $candidatePhase
                    break
                }
                if ($best.status -eq 'no-guestvar' -and $candidatePhase.status -ne 'no-guestvar') {
                    $best = $candidatePhase
                }
            }
        }

        $candidateResult = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            candidate_id = $candidate.candidate_id
            value_name = $candidate.value_name
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
        if ($skipRuntimePhases) {
            $candidateResult['limitation_reason'] = 'vm_standby_limitation'
        }

        Write-Json -Payload $candidateResult -HostPath (Join-Path $hostCandidateRoot 'summary.json') -RepoPath (Join-Path $repoCandidateRoot 'summary.json')
        $summary.candidate_summary_files += "evidence/files/vm-tooling-staging/$probeName/$candidateLabel/summary.json"
        $results.Add([pscustomobject]$candidateResult) | Out-Null
    }

    $summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
    $summary.exact_line_only_candidates = @($results | Where-Object { $_.status -eq 'exact-line-no-query' }).Count
    $summary.path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
    $summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
    $summary.limitation_candidates = @($results | Where-Object { $_.status -eq 'vm-standby-limitation' }).Count
    $summary.error_candidates = @($results | Where-Object { $_.status -in @('error', 'no-guestvar') }).Count
    $summary.handle_check_available = $false
    $summary.handle_check_reason = 'C:\Tools\handle.exe is not present on the current clean baseline.'
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.exact_line_only_candidates -gt 0) { 'exact-line-no-query' } elseif ($summary.path_only_candidates -gt 0) { 'path-only-hit' } elseif ($summary.limitation_candidates -gt 0) { 'vm-standby-limitation' } elseif ($summary.error_candidates -gt 0) { 'error' } else { 'no-hit' }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}

Write-Json -Payload $summary -HostPath (Join-Path $hostWorkRoot 'summary.json') -RepoPath $repoSummaryPath
Write-Json -Payload @($results.ToArray()) -HostPath (Join-Path $hostWorkRoot 'results.json') -RepoPath $repoResultsPath
Write-Output $repoSummaryPath
