[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\batch-existence',
    [string]$SnapshotName = 'baseline-20260327-regprobe-visible-shell-stable',
    [string]$ProbePrefix = 'registry-batch-existence',
    [string]$IncidentLogPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "$ProbePrefix-$stamp"
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

function Resolve-RegistryRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RegistryPath
    )

    if ($RegistryPath -notmatch '^(HKLM|HKEY_LOCAL_MACHINE|HKCU|HKEY_CURRENT_USER|HKCR|HKEY_CLASSES_ROOT|HKU|HKEY_USERS|HKCC|HKEY_CURRENT_CONFIG)\\?(.*)$') {
        throw "Unsupported registry path: $RegistryPath"
    }

    $hive = $matches[1].ToUpperInvariant()
    $subKey = $matches[2]

    switch ($hive) {
        'HKLM' { $root = [Microsoft.Win32.Registry]::LocalMachine }
        'HKEY_LOCAL_MACHINE' { $root = [Microsoft.Win32.Registry]::LocalMachine }
        'HKCU' { $root = [Microsoft.Win32.Registry]::CurrentUser }
        'HKEY_CURRENT_USER' { $root = [Microsoft.Win32.Registry]::CurrentUser }
        'HKCR' { $root = [Microsoft.Win32.Registry]::ClassesRoot }
        'HKEY_CLASSES_ROOT' { $root = [Microsoft.Win32.Registry]::ClassesRoot }
        'HKU' { $root = [Microsoft.Win32.Registry]::Users }
        'HKEY_USERS' { $root = [Microsoft.Win32.Registry]::Users }
        'HKCC' { $root = [Microsoft.Win32.Registry]::CurrentConfig }
        'HKEY_CURRENT_CONFIG' { $root = [Microsoft.Win32.Registry]::CurrentConfig }
        default { throw "Unsupported hive: $hive" }
    }

    return [ordered]@{
        root = $root
        subkey = $subKey
    }
}

function Convert-RegistryValuePreview {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [byte[]]) {
        $slice = $Value | Select-Object -First 16
        return ($slice | ForEach-Object { $_.ToString('X2') }) -join ' '
    }

    if ($Value -is [string[]]) {
        return ($Value -join '; ')
    }

    return [string]$Value
}

function Get-ValueLength {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return 0
    }

    if ($Value -is [Array]) {
        return $Value.Length
    }

    return ([string]$Value).Length
}

function Get-EntryState {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Entry
    )

    $resolved = Resolve-RegistryRoot -RegistryPath $Entry.registry_path
    $pathExists = $false
    $valueExists = $false
    $valueKind = $null
    $valueData = $null
    $readError = $null

    $key = $null
    try {
        $key = $resolved.root.OpenSubKey($resolved.subkey, $false)
        $pathExists = $null -ne $key
        if ($pathExists) {
            $names = @($key.GetValueNames())
            $valueExists = $names -contains $Entry.value_name
            if ($valueExists) {
                $valueKind = [string]$key.GetValueKind($Entry.value_name)
                $valueData = $key.GetValue($Entry.value_name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            }
        }
    }
    catch {
        $readError = $_.Exception.Message
    }
    finally {
        if ($null -ne $key) {
            $key.Dispose()
        }
    }

    return [ordered]@{
        candidate_id = $Entry.candidate_id
        family = $Entry.family
        suspected_layer = $Entry.suspected_layer
        boot_phase_relevant = [bool]$Entry.boot_phase_relevant
        frida_allowed = [bool]$Entry.frida_allowed
        registry_path = $Entry.registry_path
        value_name = $Entry.value_name
        path_exists = $pathExists
        value_exists = $valueExists
        value_kind = $valueKind
        value_preview = Convert-RegistryValuePreview -Value $valueData
        value_length = Get-ValueLength -Value $valueData
        read_error = $readError
    }
}

New-Item -ItemType Directory -Path (Split-Path -Parent $ResultsPath) -Force | Out-Null

$manifest = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json
$results = New-Object System.Collections.Generic.List[object]

foreach ($entry in $manifest.candidates) {
    $results.Add([pscustomobject](Get-EntryState -Entry $entry)) | Out-Null
}

$results | ConvertTo-Json -Depth 8 | Set-Content -Path $ResultsPath -Encoding UTF8
$results | Export-Csv -Path $ResultsCsvPath -NoTypeInformation -Encoding UTF8

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    route = $manifest.route
    source_note = $manifest.source_note
    total_candidates = $results.Count
    path_exists_count = @($results | Where-Object { $_.path_exists }).Count
    value_exists_count = @($results | Where-Object { $_.value_exists }).Count
    read_error_count = @($results | Where-Object { $_.read_error }).Count
    by_family = @(
        $results |
            Group-Object -Property family |
            Sort-Object Name |
            ForEach-Object {
                [ordered]@{
                    family = $_.Name
                    total = $_.Count
                    path_exists_count = @($_.Group | Where-Object { $_.path_exists }).Count
                    value_exists_count = @($_.Group | Where-Object { $_.value_exists }).Count
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
    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
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
        -RecordId 'research.batch-existence-probe' `
        -TweakId 'research.batch-existence-probe' `
        -TestId $TestId `
        -Family 'batch-existence-probe' `
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

    Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath) -IgnoreExitCode | Out-Null
    Wait-GuestReady

    $summary.shell_before = Get-ShellHealth
    if (-not ($summary.shell_before.explorer -and $summary.shell_before.sihost -and $summary.shell_before.shellhost)) {
        throw 'Shell health check failed before the batch existence probe started.'
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
        $summary.errors += 'Shell health was degraded after the batch existence probe.'
        Log-Incident -TestId $probeName -Symptom 'Shell health was degraded after the batch existence probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Recovered by snapshot revert after the read-only batch existence lane.'
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Batch existence probe failed before results could be captured.'
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
