[CmdletBinding()]
param(
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

if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = Resolve-CanonicalVmPath }
if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = Resolve-DefaultVmSnapshotName }
if ([string]::IsNullOrWhiteSpace($VmPath)) { $VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx' }
if ([string]::IsNullOrWhiteSpace($SnapshotName)) { $SnapshotName = 'RegProbe-Baseline-ToolsHardened-20260330' }

$shellHealthScript = Join-Path $repoRoot 'scripts\vm\get-vm-shell-health.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "path-aware-runtime-$stamp"
$repoOutputRoot = Join-Path $repoRoot "evidence\files\path-aware\$probeName"
$hostWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) $probeName
$guestScriptRoot = 'C:\Tools\Scripts'
$guestRoot = "C:\RegProbe-Diag\$probeName"
$repoSummaryPath = Join-Path $repoOutputRoot 'summary.json'
$repoResultsPath = Join-Path $repoOutputRoot 'results.json'

$candidates = @(
    [ordered]@{
        candidate_id = 'policy.system.enable-virtualization'
        label = 'policy-system-enable-virtualization'
        family = 'policy-system'
        registry_path_fragment = 'SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System'
        value_name = 'EnableVirtualization'
        collision_path_fragment = 'SOFTWARE\\Policies\\Microsoft\\Windows\\DeviceGuard'
        short_trigger_profile = 'uac-policy-surface-short'
        split_trigger_profile = 'uac-policy-surface-only'
    },
    [ordered]@{
        candidate_id = 'system.io-allow-remote-dasd'
        label = 'system-io-allow-remote-dasd'
        family = 'session-manager-io'
        registry_path_fragment = 'SYSTEM\\CurrentControlSet\\Control\\Session Manager\\I/O System'
        value_name = 'AllowRemoteDASD'
        collision_path_fragment = 'SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices\\AllowRemoteDASD'
        short_trigger_profile = 'session-manager-io-raw-io-short'
        split_trigger_profile = 'session-manager-io-raw-io-only'
    }
)

if (@($CandidateIds).Count -gt 0) {
    $wanted = @($CandidateIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $candidates = @($candidates | Where-Object { $wanted -contains $_.candidate_id })
    if (@($candidates).Count -eq 0) {
        throw "No path-aware runtime candidates matched the requested ids: $($wanted -join ', ')"
    }
}

New-Item -ItemType Directory -Path $repoOutputRoot, $hostWorkRoot -Force | Out-Null

$guestPayload = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GuestRoot,
    [Parameter(Mandatory = $true)]
    [string]$Phase,
    [Parameter(Mandatory = $true)]
    [string]$TriggerProfile,
    [Parameter(Mandatory = $true)]
    [string]$RegistryPathFragment,
    [Parameter(Mandatory = $true)]
    [string]$ValueName,
    [string]$CollisionPathFragment = ''
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

function Invoke-UacSurfaceBurst {
    $failures = @()

    foreach ($target in @("$env:SystemRoot\System32\ComputerDefaults.exe", "$env:SystemRoot\System32\fodhelper.exe")) {
        try {
            $proc = Start-Process -FilePath $target -PassThru -ErrorAction Stop
            Start-Sleep -Seconds 4
            if ($proc -and -not $proc.HasExited) {
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
            $failures += [ordered]@{
                command = "start-burst:$target"
                exit_code = 1
                stdout = ''
                stderr = $_.Exception.Message
            }
        }
    }

    $policyRefresh = Invoke-CmdCapture -FilePath 'C:\Windows\System32\gpupdate.exe' -Arguments @('/target:computer', '/force')
    if ($policyRefresh.exit_code -ne 0 -or -not [string]::IsNullOrWhiteSpace($policyRefresh.stderr)) {
        $failures += [ordered]@{
            command = 'gpupdate-machine-policy'
            exit_code = $policyRefresh.exit_code
            stdout = $policyRefresh.stdout
            stderr = $policyRefresh.stderr
        }
    }

    return @($failures)
}

function Invoke-SessionManagerIoBurst {
    $failures = @()
    try {
        $path = 'C:\RegProbe-Diag\io-session-manager'
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        1..24 | ForEach-Object {
            $filePath = Join-Path $path ("io" + $_ + '.bin')
            $data = New-Object byte[] 1048576
            [System.IO.File]::WriteAllBytes($filePath, $data)
        }
        try {
            $stream = [System.IO.File]::Open('\\.\C:', [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
            $buffer = New-Object byte[] 4096
            $null = $stream.Read($buffer, 0, $buffer.Length)
            $stream.Close()
        }
        catch {
        }
        cmd /c 'fsutil fsinfo ntfsInfo C:' | Out-Null
        cmd /c 'fltmc volumes' | Out-Null
        cmd /c 'mountvol' | Out-Null
        Get-Volume | Out-Null
        Get-Disk | Out-Null
    }
    catch {
        $failures += [ordered]@{
            command = 'session-manager-io-raw-burst'
            exit_code = 1
            stdout = ''
            stderr = $_.Exception.Message
        }
    }
    finally {
        Remove-Item -Path 'C:\RegProbe-Diag\io-session-manager' -Recurse -Force -ErrorAction SilentlyContinue
    }

    return @($failures)
}

function Invoke-TriggerProfile {
    param([string]$Profile)

    switch ($Profile) {
        'uac-policy-surface-short' { return @(Invoke-UacSurfaceBurst) }
        'uac-policy-surface-only' { return @(Invoke-UacSurfaceBurst) }
        'session-manager-io-raw-io-short' { return @(Invoke-SessionManagerIoBurst) }
        'session-manager-io-raw-io-only' { return @(Invoke-SessionManagerIoBurst) }
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
        [int]$CollisionPathHits,
        [int]$TriggerFailureCount,
        [object[]]$TriggerFailures,
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
        collision_path_hits = $CollisionPathHits
        trigger_failure_count = $TriggerFailureCount
        trigger_failures = @($TriggerFailures | Select-Object -First 6)
        errors = @($Errors | Select-Object -First 4)
    }
}

New-Item -ItemType Directory -Path $GuestRoot -Force | Out-Null

$phaseSummaryPath = Join-Path $GuestRoot ("$Phase-summary.json")
$traceBaseName = if ($Phase -like 'split-*') { 'split-trace' } else { $Phase }
$etlPath = Join-Path $GuestRoot ("$traceBaseName.etl")
$csvPath = Join-Path $GuestRoot ("$traceBaseName.csv")
$sessionName = if ($Phase -like 'split-*') { 'RegProbePathAwareSplit' } else { 'RegProbePathAware_' + ($Phase -replace '[^A-Za-z0-9]', '') }

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
        Start-Sleep -Seconds 2
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
        $collisionPathHits = 0
        $exactLineCount = 0
        $exactQueryHits = 0

        if ($csvExists) {
            $pathPattern = [regex]::Escape($RegistryPathFragment)
            $collisionPattern = if ([string]::IsNullOrWhiteSpace($CollisionPathFragment)) { $null } else { [regex]::Escape($CollisionPathFragment) }
            $valuePattern = [regex]::Escape($ValueName)
            $reader = [System.IO.File]::OpenText($csvPath)
            try {
                while (($line = $reader.ReadLine()) -ne $null) {
                    $csvLineCount++
                    if ($line -match $pathPattern) {
                        $pathLineCount++
                    }
                    if ($collisionPattern -and $line -match $collisionPattern) {
                        $collisionPathHits++
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
        $payload = Build-CompactSummary -Status $status -EtlExists $etlExists -CsvExists $csvExists -EtlLength $etlLength -CsvLineCount $csvLineCount -ExactLineCount $exactLineCount -ExactQueryHits $exactQueryHits -PathLineCount $pathLineCount -CollisionPathHits $collisionPathHits -TriggerFailureCount @($triggerFailures).Count -TriggerFailures @($triggerFailures) -Errors @()
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

        $payload = Build-CompactSummary -Status 'trace-started' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -CollisionPathHits 0 -TriggerFailureCount 0 -TriggerFailures @() -Errors @()
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    elseif ($Phase -eq 'split-trigger') {
        $triggerFailures = Invoke-TriggerProfile -Profile $TriggerProfile
        $payload = Build-CompactSummary -Status 'trigger-complete' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -CollisionPathHits 0 -TriggerFailureCount @($triggerFailures).Count -TriggerFailures @($triggerFailures) -Errors @()
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
        $collisionPathHits = 0
        $exactLineCount = 0
        $exactQueryHits = 0

        if ($csvExists) {
            $pathPattern = [regex]::Escape($RegistryPathFragment)
            $collisionPattern = if ([string]::IsNullOrWhiteSpace($CollisionPathFragment)) { $null } else { [regex]::Escape($CollisionPathFragment) }
            $valuePattern = [regex]::Escape($ValueName)
            $reader = [System.IO.File]::OpenText($csvPath)
            try {
                while (($line = $reader.ReadLine()) -ne $null) {
                    $csvLineCount++
                    if ($line -match $pathPattern) {
                        $pathLineCount++
                    }
                    if ($collisionPattern -and $line -match $collisionPattern) {
                        $collisionPathHits++
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
        $payload = Build-CompactSummary -Status $status -EtlExists $etlExists -CsvExists $csvExists -EtlLength $etlLength -CsvLineCount $csvLineCount -ExactLineCount $exactLineCount -ExactQueryHits $exactQueryHits -PathLineCount $pathLineCount -CollisionPathHits $collisionPathHits -TriggerFailureCount 0 -TriggerFailures @() -Errors @()
        $json = $payload | ConvertTo-Json -Depth 6 -Compress
        [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    }
    else {
        throw "Unsupported phase: $Phase"
    }
}
catch {
    $payload = Build-CompactSummary -Status 'error' -EtlExists $false -CsvExists $false -EtlLength 0 -CsvLineCount 0 -ExactLineCount 0 -ExactQueryHits 0 -PathLineCount 0 -CollisionPathHits 0 -TriggerFailureCount 0 -TriggerFailures @() -Errors @($_.Exception.Message)
    $json = $payload | ConvertTo-Json -Depth 6 -Compress
    [System.IO.File]::WriteAllText($phaseSummaryPath, $json, [System.Text.Encoding]::UTF8)
    exit 1
}
'@

$hostPayloadPath = Join-Path $hostWorkRoot 'run-path-aware-runtime-probe.guest.ps1'
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
    param([int]$TimeoutSeconds = 300)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            if ((Invoke-Vmrun -Arguments @('-T', 'ws', 'list')) -notmatch [regex]::Escape($VmPath)) {
                Start-Sleep -Seconds 3
                continue
            }
            if ((Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)) -match 'running|installed') {
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
                '-T', 'ws',
                '-gu', $GuestUser,
                '-gp', $GuestPassword,
                'runProgramInGuest',
                $VmPath,
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
    if ((Invoke-Vmrun -Arguments @('-T', 'ws', 'list')) -notmatch [regex]::Escape($VmPath)) {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'gui') -IgnoreExitCode | Out-Null
    }
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

function Copy-FromGuestBestEffort {
    param([string]$GuestPath, [string]$HostPath)

    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromGuestToHost', $VmPath, $GuestPath, $HostPath) | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Copy-ToGuest {
    param([string]$HostPath, [string]$GuestPath)

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'CopyFileFromHostToGuest', $VmPath, $HostPath, $GuestPath) | Out-Null
}

function Get-ShellHealthBestEffort {
    param([int]$TimeoutSeconds = 180)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $last = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $shellHealthScript -VmPath $VmPath -VmrunPath $VmrunPath -GuestUser $GuestUser -GuestPassword $GuestPassword | ConvertFrom-Json
            if ($last.shell_healthy) {
                return $last
            }
        }
        catch {
        }

        Start-Sleep -Seconds 5
    }

    if ($null -ne $last) { return $last }
    return [pscustomobject]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        shell_healthy = $false
        tools_state = 'unknown'
        checks = [ordered]@{
            explorer = $false
            sihost = $false
            shellhost = $false
            ctfmon = $false
            app = $false
        }
    }
}

function Invoke-GuestPhase {
    param(
        [string]$GuestPayloadPath,
        [hashtable]$Candidate,
        [string]$Phase
    )

    $triggerProfile = if ($Phase -like 'split-*') { $Candidate.split_trigger_profile } else { $Candidate.short_trigger_profile }
    $guestLauncherLog = Join-Path $guestRoot "$Phase-launcher.log"
    $script = @"
`$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path '$guestRoot' -Force | Out-Null
Remove-Item -LiteralPath '$guestLauncherLog' -Force -ErrorAction SilentlyContinue
try {
    'BEGIN_PHASE=$Phase' | Out-File -FilePath '$guestLauncherLog' -Encoding utf8
    & '$GuestPayloadPath' -GuestRoot '$guestRoot' -Phase '$Phase' -TriggerProfile '$triggerProfile' -RegistryPathFragment '$($Candidate.registry_path_fragment)' -ValueName '$($Candidate.value_name)' -CollisionPathFragment '$($Candidate.collision_path_fragment)' *>> '$guestLauncherLog'
    'END_PHASE=$Phase' | Add-Content -LiteralPath '$guestLauncherLog' -Encoding utf8
    exit 0
}
catch {
    ('ERROR_PHASE=$Phase' + [Environment]::NewLine + (`$_ | Out-String)) | Add-Content -LiteralPath '$guestLauncherLog' -Encoding utf8
    exit 1
}
"@
    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script))
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

function New-PhaseErrorSummary {
    param([string]$PhaseName, [string]$Message)

    return [pscustomobject]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        phase = $PhaseName
        status = 'error'
        exact_runtime_read = $false
        exact_query_hits = 0
        exact_line_count = 0
        path_line_count = 0
        collision_path_hits = 0
        errors = @($Message)
    }
}

function Write-Json {
    param(
        [object]$Payload,
        [string]$HostPath,
        [string]$RepoPath = ''
    )

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
    host_output_root = "evidence/files/path-aware/$probeName"
    status = ''
    total_candidates = @($candidates).Count
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
    $guestPayloadPath = Join-Path $guestScriptRoot 'run-path-aware-runtime-probe.guest.ps1'
    Invoke-Vmrun -Arguments @('-T', 'ws', 'revertToSnapshot', $VmPath, $SnapshotName) | Out-Null
    Ensure-VmStarted
    Wait-GuestReady
    Wait-GuestCommandReady
    Ensure-GuestDirectory $guestScriptRoot
    Ensure-GuestDirectory $guestRoot
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    foreach ($candidate in $candidates) {
        $hostCandidateRoot = Join-Path $hostWorkRoot $candidate.label
        $repoCandidateRoot = Join-Path $repoOutputRoot $candidate.label
        New-Item -ItemType Directory -Path $hostCandidateRoot, $repoCandidateRoot -Force | Out-Null

        $shellBefore = Get-ShellHealthBestEffort
        $phaseResults = [ordered]@{}
        foreach ($phaseName in @('short-trigger-etw', 'split-trace-start', 'split-trigger', 'split-trace-stop')) {
            $phaseSummary = $null
            try {
                Wait-GuestReady
                Wait-GuestCommandReady
                Invoke-GuestPhase -GuestPayloadPath $guestPayloadPath -Candidate $candidate -Phase $phaseName
                if ($phaseName -eq 'split-trigger') {
                    Start-Sleep -Seconds 10
                    Wait-GuestReady -TimeoutSeconds 240
                    Wait-GuestCommandReady -TimeoutSeconds 180
                } elseif ($phaseName -eq 'short-trigger-etw') {
                    Wait-GuestReady -TimeoutSeconds 240
                    Wait-GuestCommandReady -TimeoutSeconds 180
                } elseif ($phaseName -eq 'split-trace-start') {
                    Start-Sleep -Seconds 2
                }

                $guestPhaseSummary = Join-Path $guestRoot "$phaseName-summary.json"
                $hostPhaseSummary = Join-Path $hostCandidateRoot "$phaseName-summary.json"
                if (Copy-FromGuestBestEffort -GuestPath $guestPhaseSummary -HostPath $hostPhaseSummary) {
                    try {
                        $phaseSummary = Get-Content -Path $hostPhaseSummary -Raw | ConvertFrom-Json
                        Copy-Item -Path $hostPhaseSummary -Destination (Join-Path $repoCandidateRoot "$phaseName-summary.json") -Force
                    }
                    catch {
                    }
                }
            }
            catch {
                $phaseSummary = New-PhaseErrorSummary -PhaseName $phaseName -Message $_.Exception.Message
            }

            $guestPhaseLog = Join-Path $guestRoot "$phaseName-launcher.log"
            $hostPhaseLog = Join-Path $hostCandidateRoot "$phaseName-launcher.log"
            if (Copy-FromGuestBestEffort -GuestPath $guestPhaseLog -HostPath $hostPhaseLog) {
                Copy-Item -Path $hostPhaseLog -Destination (Join-Path $repoCandidateRoot "$phaseName-launcher.log") -Force
            }

            if ($null -eq $phaseSummary) {
                $phaseSummary = [pscustomobject]@{
                    generated_utc = [DateTime]::UtcNow.ToString('o')
                    phase = $phaseName
                    status = 'copy-incomplete'
                    exact_runtime_read = $false
                    exact_query_hits = 0
                    exact_line_count = 0
                    path_line_count = 0
                    collision_path_hits = 0
                    errors = @('phase summary copy-back did not complete')
                }
            }
            $phaseResults[$phaseName] = $phaseSummary
        }

        $shellAfter = Get-ShellHealthBestEffort
        $best = $phaseResults['short-trigger-etw']
        if ($phaseResults['split-trace-stop'].exact_runtime_read) {
            $best = $phaseResults['split-trace-stop']
        }
        elseif ($best.status -eq 'copy-incomplete' -and $phaseResults['split-trace-stop'].status -ne 'copy-incomplete') {
            $best = $phaseResults['split-trace-stop']
        }

        $candidateResult = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            candidate_id = $candidate.candidate_id
            family = $candidate.family
            snapshot_name = $SnapshotName
            registry_path_fragment = $candidate.registry_path_fragment
            value_name = $candidate.value_name
            shell_before = $shellBefore
            shell_after = $shellAfter
            best_phase = $best.phase
            status = $best.status
            exact_query_hits = $best.exact_query_hits
            exact_runtime_read = [bool]$best.exact_runtime_read
            exact_line_count = if ($null -ne $best.PSObject.Properties['exact_line_count']) { [int]$best.exact_line_count } else { 0 }
            path_line_count = if ($null -ne $best.PSObject.Properties['path_line_count']) { [int]$best.path_line_count } else { 0 }
            collision_path_hits = if ($null -ne $best.PSObject.Properties['collision_path_hits']) { [int]$best.collision_path_hits } else { 0 }
            phase_results = $phaseResults
            artifacts = [ordered]@{
                summary = "evidence/files/path-aware/$probeName/$($candidate.label)/summary.json"
            }
        }

        Write-Json -Payload $candidateResult -HostPath (Join-Path $hostCandidateRoot 'summary.json') -RepoPath (Join-Path $repoCandidateRoot 'summary.json')
        $summary.candidate_summary_files += "evidence/files/path-aware/$probeName/$($candidate.label)/summary.json"
        $results.Add([pscustomobject]$candidateResult) | Out-Null
    }
}
catch {
    $summary.status = 'error'
    $summary.errors = @($summary.errors) + $_.Exception.Message
}

$summary.exact_hit_candidates = @($results | Where-Object { $_.exact_runtime_read }).Count
$summary.exact_line_only_candidates = @($results | Where-Object { $_.status -eq 'exact-line-no-query' }).Count
$summary.path_only_candidates = @($results | Where-Object { $_.status -eq 'path-only-hit' }).Count
$summary.no_hit_candidates = @($results | Where-Object { $_.status -eq 'no-hit' }).Count
$summary.error_candidates = @($results | Where-Object { $_.status -in @('error', 'copy-incomplete') }).Count
if (-not $summary.status) {
    $summary.status = if ($summary.exact_hit_candidates -gt 0) { 'exact-hit' } elseif ($summary.exact_line_only_candidates -gt 0) { 'exact-line-no-query' } elseif ($summary.path_only_candidates -gt 0) { 'path-only-hit' } elseif ($summary.error_candidates -gt 0) { 'error' } else { 'no-hit' }
}

Write-Json -Payload $summary -HostPath (Join-Path $hostWorkRoot 'summary.json') -RepoPath $repoSummaryPath
Write-Json -Payload @($results.ToArray()) -HostPath (Join-Path $hostWorkRoot 'results.json') -RepoPath $repoResultsPath

Write-Output $repoSummaryPath
