[CmdletBinding()]
param(
    [ValidateSet('arm', 'collect')]
    [string]$Stage = 'arm',

    [string]$OutputName = 'kernel-timing-seeded-registry-boot-20260412',
    [string]$OutputRoot = '',
    [int]$TracerptTimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'

$registryPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel'
$displayRegistryPath = 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel'
$wprPath = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$tracerptPath = 'C:\Windows\System32\tracerpt.exe'

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\wpr-boot-registry' $OutputName
}

$statePath = Join-Path $OutputRoot 'state.json'
$armSummaryPath = Join-Path $OutputRoot 'summary-arm.json'
$collectSummaryPath = Join-Path $OutputRoot 'summary.json'
$etlPath = Join-Path $OutputRoot ($OutputName + '.etl')
$csvPath = Join-Path $OutputRoot ($OutputName + '.csv')
$hitsPath = Join-Path $OutputRoot ($OutputName + '.target-hits.txt')
$contextPath = Join-Path $OutputRoot ($OutputName + '.session-manager-kernel-context.txt')

$targets = @(
    [pscustomobject]@{ Name = 'TimerCheckFlags'; Value = 1; Type = 'DWord' },
    [pscustomobject]@{ Name = 'ForceBugcheckForDpcWatchdog'; Value = 0; Type = 'DWord' },
    [pscustomobject]@{ Name = 'LongDpcQueueThreshold'; Value = 3; Type = 'DWord' },
    [pscustomobject]@{ Name = 'LongDpcRuntimeThreshold'; Value = 100; Type = 'DWord' }
)

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )

    $Payload | ConvertTo-Json -Depth 12 | Set-Content -Path $Path -Encoding UTF8
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [int]$TimeoutSeconds = 0,
        [switch]$IgnoreExitCode
    )

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-seeded-wpr-' + [Guid]::NewGuid().ToString('N') + '.stdout.txt')
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-seeded-wpr-' + [Guid]::NewGuid().ToString('N') + '.stderr.txt')

    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $timedOut = $false
        if ($TimeoutSeconds -gt 0) {
            if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
                $timedOut = $true
                try {
                    $proc.Kill()
                }
                catch {
                }
                $proc.WaitForExit()
            }
        }
        else {
            $proc.WaitForExit()
        }

        $stdout = if (Test-Path $stdoutPath) { (Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        $stderr = if (Test-Path $stderrPath) { (Get-Content -Path $stderrPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        $exitCode = if ($timedOut) { -1 } else { $proc.ExitCode }

        if ($timedOut -and -not $IgnoreExitCode) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) timed out after $TimeoutSeconds second(s)"
        }
        if ((-not $IgnoreExitCode) -and $null -ne $exitCode -and $exitCode -ne 0) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $exitCode"
        }

        return [ordered]@{
            exit_code = $exitCode
            timed_out = $timedOut
            stdout = ('{0}' -f $stdout).Trim()
            stderr = ('{0}' -f $stderr).Trim()
        }
    }
    finally {
        Remove-Item -Path $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Ensure-RegistryKey {
    if (-not (Test-Path $registryPath)) {
        New-Item -Path $registryPath -Force | Out-Null
    }
}

function Get-TargetBackup {
    Ensure-RegistryKey
    $key = Get-Item -Path $registryPath
    $backup = @()

    foreach ($target in $targets) {
        $targetName = [string]$target.Name
        $present = $false
        $value = $null
        $kind = $null
        try {
            $kind = [string]$key.GetValueKind($targetName)
            $value = Get-ItemPropertyValue -Path $registryPath -Name $targetName -ErrorAction Stop
            $present = $true
        }
        catch {
            $present = $false
        }

        $backup += [pscustomobject]@{
            name = $targetName
            was_present = $present
            kind = $kind
            value = $value
        }
    }

    return $backup
}

function Set-SeedValues {
    Ensure-RegistryKey
    foreach ($target in $targets) {
        New-ItemProperty -Path $registryPath -Name ([string]$target.Name) -PropertyType ([string]$target.Type) -Value ([int]$target.Value) -Force | Out-Null
    }
}

function Restore-SeedValues {
    param([object[]]$Backup)

    $results = @()
    foreach ($entry in $Backup) {
        try {
            if ($entry.was_present) {
                if ($entry.kind -eq 'DWord') {
                    New-ItemProperty -Path $registryPath -Name $entry.name -PropertyType DWord -Value ([int]$entry.value) -Force | Out-Null
                }
                else {
                    $results += [pscustomobject]@{
                        name = $entry.name
                        status = 'skipped-unsupported-original-kind'
                        original_kind = $entry.kind
                    }
                    continue
                }
            }
            else {
                Remove-ItemProperty -Path $registryPath -Name $entry.name -ErrorAction SilentlyContinue
            }

            $results += [pscustomobject]@{
                name = $entry.name
                status = 'restored'
                original_kind = $entry.kind
                was_present = $entry.was_present
            }
        }
        catch {
            $results += [pscustomobject]@{
                name = $entry.name
                status = 'error'
                error = $_.Exception.Message
            }
        }
    }

    return $results
}

function Copy-ToWritableDrive {
    param([Parameter(Mandatory = $true)][string[]]$Paths)

    foreach ($letter in @('E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z')) {
        $drive = $letter + ':'
        if (-not (Test-Path ($drive + '\'))) {
            continue
        }

        try {
            $probe = Join-Path ($drive + '\') 'regprobe-write-test.tmp'
            Set-Content -Path $probe -Value 'test' -ErrorAction Stop
            $target = Join-Path ($drive + '\') $OutputName
            New-Item -ItemType Directory -Force -Path $target | Out-Null

            foreach ($path in $Paths) {
                if (Test-Path $path) {
                    Copy-Item -Force -Path $path -Destination $target
                }
            }

            Remove-Item -Force $probe -ErrorAction SilentlyContinue
            return $target
        }
        catch {
            Write-Host "Drive $drive not writable: $($_.Exception.Message)"
        }
    }

    return $null
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

if (-not (Test-Path $wprPath)) {
    throw "Missing wpr.exe: $wprPath"
}
if (-not (Test-Path $tracerptPath)) {
    throw "Missing tracerpt.exe: $tracerptPath"
}

if ($Stage -eq 'arm') {
    $summary = [ordered]@{
        stage = 'arm'
        output_name = $OutputName
        output_root = $OutputRoot
        registry_path = $displayRegistryPath
        targets = $targets
        status = 'started'
        seed_backup = @()
        cancelboot = $null
        cancel = $null
        addboot = $null
        error = $null
        generated_utc = (Get-Date).ToUniversalTime().ToString('o')
    }

    try {
        $backup = Get-TargetBackup
        Set-SeedValues

        $summary.seed_backup = @($backup)
        $state = [ordered]@{
            generated_utc = (Get-Date).ToUniversalTime().ToString('o')
            output_name = $OutputName
            output_root = $OutputRoot
            registry_path = $displayRegistryPath
            targets = $targets
            seed_backup = @($backup)
            etl_path = $etlPath
            csv_path = $csvPath
            hits_path = $hitsPath
            before_boot_time_utc = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToUniversalTime().ToString('o')
        }
        Write-JsonFile -Path $statePath -Payload $state

        $summary.cancelboot = Invoke-Native -FilePath $wprPath -Arguments @('-cancelboot') -IgnoreExitCode
        $summary.cancel = Invoke-Native -FilePath $wprPath -Arguments @('-cancel') -IgnoreExitCode
        $summary.addboot = Invoke-Native -FilePath $wprPath -Arguments @('-addboot', 'Registry', '-filemode', '-recordtempto', $OutputRoot) -IgnoreExitCode

        if ($summary.addboot.timed_out) {
            $summary.status = 'error'
            $summary.error = 'wpr-addboot-timeout'
        }
        elseif ($null -ne $summary.addboot.exit_code -and $summary.addboot.exit_code -ne 0) {
            $summary.status = 'error'
            $summary.error = "wpr-addboot-nonzero-exit: $($summary.addboot.exit_code)"
        }
        else {
            $summary.status = 'armed'
        }
    }
    catch {
        $summary.status = 'error'
        $summary.error = $_.Exception.Message
        $summary.error_type = $_.Exception.GetType().FullName
        $summary.error_script_stack = $_.ScriptStackTrace
    }

    Write-JsonFile -Path $armSummaryPath -Payload $summary
    Write-Host "Arm summary: $armSummaryPath"
    Get-Content -Path $armSummaryPath -Raw
    exit 0
}

if (-not (Test-Path $statePath)) {
    throw "Missing state file: $statePath"
}

$state = Get-Content -Path $statePath -Raw | ConvertFrom-Json
$summary = [ordered]@{
    stage = 'collect'
    output_name = $OutputName
    output_root = $OutputRoot
    registry_path = $displayRegistryPath
    targets = $targets
    status = 'started'
    stopboot = $null
    tracerpt = $null
    etl_path = $etlPath
    etl_exists = $false
    etl_size_bytes = 0
    csv_path = $csvPath
    csv_exists = $false
    csv_size_bytes = 0
    hits_path = $hitsPath
    hits_exists = $false
    hit_count = 0
    target_hit_counts = [ordered]@{}
    context_path = $contextPath
    restore_results = @()
    copied_to = $null
    error = $null
    generated_utc = (Get-Date).ToUniversalTime().ToString('o')
}

try {
    $summary.stopboot = Invoke-Native -FilePath $wprPath -Arguments @('-stopboot', $etlPath) -IgnoreExitCode
    $summary.etl_exists = [bool](Test-Path $etlPath)
    if ($summary.etl_exists) {
        $etlItem = Get-Item $etlPath
        $summary.etl_size_bytes = $etlItem.Length
    }

    if ($summary.etl_exists) {
        $summary.tracerpt = Invoke-Native -FilePath $tracerptPath -Arguments @($etlPath, '-o', $csvPath, '-of', 'CSV') -TimeoutSeconds $TracerptTimeoutSeconds -IgnoreExitCode
        $summary.csv_exists = [bool](Test-Path $csvPath)
        if ($summary.csv_exists) {
            $csvItem = Get-Item $csvPath
            $summary.csv_size_bytes = $csvItem.Length

            $findstrArgs = @('/I')
            foreach ($target in $targets) {
                $targetName = [string]$target.Name
                $findstrArgs += "/C:$targetName"
                $summary.target_hit_counts[$targetName] = 0
            }
            $findstrArgs += $csvPath

            $targetHits = & findstr.exe @findstrArgs 2>$null
            if ($null -eq $targetHits) {
                New-Item -ItemType File -Force -Path $hitsPath | Out-Null
            }
            else {
                @($targetHits) | Set-Content -Path $hitsPath -Encoding UTF8
            }
            $summary.hits_exists = [bool](Test-Path $hitsPath)

            $hitLines = if ($summary.hits_exists) { Get-Content -Path $hitsPath -ErrorAction SilentlyContinue } else { @() }
            $summary.hit_count = @($hitLines).Count
            foreach ($line in @($hitLines)) {
                foreach ($target in $targets) {
                    $targetName = [string]$target.Name
                    if ($line -like "*$targetName*") {
                        $summary.target_hit_counts[$targetName]++
                    }
                }
            }

            $contextHits = & findstr.exe /I /C:'Session Manager\Kernel' $csvPath 2>$null | Select-Object -First 120
            @($contextHits) | Set-Content -Path $contextPath -Encoding UTF8
        }
    }

    $summary.restore_results = Restore-SeedValues -Backup @($state.seed_backup)
    $summary.status = 'completed'
}
catch {
    $summary.status = 'error'
    $summary.error = $_.Exception.Message
    $summary.error_type = $_.Exception.GetType().FullName
    $summary.error_script_stack = $_.ScriptStackTrace
    try {
        $summary.restore_results = Restore-SeedValues -Backup @($state.seed_backup)
    }
    catch {
        $summary.restore_results = @([ordered]@{ status = 'restore-exception'; error = $_.Exception.Message })
    }
}

Write-JsonFile -Path $collectSummaryPath -Payload $summary
$summary.copied_to = Copy-ToWritableDrive -Paths @($armSummaryPath, $collectSummaryPath, $statePath, $hitsPath, $contextPath)
Write-JsonFile -Path $collectSummaryPath -Payload $summary
if ($summary.copied_to -and (Test-Path $summary.copied_to)) {
    Copy-Item -Force -Path $collectSummaryPath -Destination $summary.copied_to
}

Write-Host "Collect summary: $collectSummaryPath"
Get-Content -Path $collectSummaryPath -Raw
