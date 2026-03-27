[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestOutputRoot = 'C:\Tools\ValidationController\jpeg-import-quality-runtime',
    [string]$RecordId = 'system.disable-jpeg-reduction',
    [string]$SnapshotName = 'baseline-20260327-regprobe-visible-shell-stable',
    [string]$IncidentLogPath = '',
    [string]$SourceJpeg = '',
    [int]$ValueData = 100,
    [int]$SettleSeconds = 8
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentLogPath)) {
    $IncidentLogPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$repoEvidenceRoot = Join-Path $repoRoot 'evidence\files\vm-tooling-staging'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$probeName = "jpeg-import-quality-runtime-$stamp"
$hostRoot = Join-Path $HostOutputRoot $probeName
$guestRoot = Join-Path $GuestOutputRoot $probeName
$repoRootOut = Join-Path $repoEvidenceRoot $probeName
$hostPayloadPath = Join-Path $hostRoot 'jpeg-import-quality-runtime-payload.ps1'
$guestPayloadPath = Join-Path $guestRoot 'jpeg-import-quality-runtime-payload.ps1'
$hostSummaryPath = Join-Path $hostRoot 'summary.json'
$repoSummaryPath = Join-Path $repoRootOut 'summary.json'

New-Item -ItemType Directory -Path $hostRoot -Force | Out-Null
New-Item -ItemType Directory -Path $repoRootOut -Force | Out-Null

$guestPayload = @'
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$EtlPath,

    [Parameter(Mandatory = $true)]
    [int]$ValueData,

    [string]$SourceJpeg = '',

    [int]$SettleSeconds = 8
)

$ErrorActionPreference = 'Stop'
$registryPath = 'HKCU:\Control Panel\Desktop'
$registryPathNative = 'HKCU\Control Panel\Desktop'
$valueName = 'JPEGImportQuality'
$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$wallpaperCandidates = @(
    'C:\Windows\Web\Wallpaper\Windows\img0.jpg',
    'C:\Windows\Web\Wallpaper\Windows\img19.jpg',
    'C:\Windows\Web\4K\Wallpaper\Windows\img0_1920x1200.jpg'
)

function Get-ValueState {
    $pathExists = Test-Path $registryPath
    try {
        $item = Get-ItemProperty -Path $registryPath -Name $valueName -ErrorAction Stop
        return [ordered]@{
            path_exists = $pathExists
            value_exists = $true
            value = [int]$item.$valueName
        }
    }
    catch {
        return [ordered]@{
            path_exists = $pathExists
            value_exists = $false
            value = $null
        }
    }
}

function Restore-ValueState {
    param([hashtable]$Original)

    if ($Original.value_exists) {
        & reg.exe add $registryPathNative /v $valueName /t REG_DWORD /d ([int]$Original.value) /f | Out-Null
        return
    }

    if (Test-Path $registryPath) {
        & reg.exe delete $registryPathNative /v $valueName /f | Out-Null
    }
}

function Get-ShellState {
    $names = @('explorer', 'sihost', 'ShellHost', 'ctfmon')
    $result = [ordered]@{}
    foreach ($name in $names) {
        $result[$name] = [bool](Get-Process -Name $name -ErrorAction SilentlyContinue)
    }
    return $result
}

function Get-WallpaperPath {
    try {
        return (Get-ItemProperty -Path $registryPath -Name 'WallPaper' -ErrorAction Stop).WallPaper
    }
    catch {
        return $null
    }
}

function Get-DisplayGuestPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $display = $Path
    $display = $display -replace '^[A-Za-z]:\\Windows', '%SystemRoot%'
    $display = $display -replace '^[A-Za-z]:\\Tools\\ValidationController', '%ValidationController%'
    return ($display -replace '\\', '/')
}

function Resolve-SourceJpegPath {
    param([string]$Candidate)

    if (-not [string]::IsNullOrWhiteSpace($Candidate) -and (Test-Path $Candidate)) {
        return $Candidate
    }

    foreach ($path in $wallpaperCandidates) {
        if (Test-Path $path) {
            return $path
        }
    }

    throw 'Could not find a JPEG source file for the wallpaper runtime probe.'
}

function Invoke-SetDesktopWallpaper {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $folderPath = Split-Path -Parent $resolved
    $leaf = Split-Path -Leaf $resolved

    try {
        $shell = New-Object -ComObject Shell.Application
        $folder = $shell.Namespace($folderPath)
        if (-not $folder) {
            throw "Shell namespace not available for $folderPath"
        }

        $item = $folder.ParseName($leaf)
        if (-not $item) {
            throw "Shell item not available for $resolved"
        }

        $item.InvokeVerb('setdesktopwallpaper')
    }
    catch {
        $proc = Start-Process -FilePath $resolved -Verb setdesktopwallpaper -PassThru
        if ($proc) {
            Wait-Process -Id $proc.Id -Timeout 30 -ErrorAction SilentlyContinue
        }
    }
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $EtlPath) -Force | Out-Null

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    registry_path = $registryPath
    value_name = $valueName
    value_data = $ValueData
    before = $null
    applied = $null
    restored = $null
    shell_after = $null
    wallpaper_apply = [ordered]@{
        source_jpeg = $null
        working_copy = $null
        original_wallpaper = $null
        current_wallpaper = $null
        final_wallpaper = $null
        candidate_invoked = $false
        restore_invoked = $false
        restored_original_exists = $false
    }
    etl_exists = $false
    etl_path = 'jpeg-import-quality-runtime.etl'
    errors = @()
}

$before = Get-ValueState
$summary.before = $before
$traceStarted = $false
$originalWallpaper = Get-WallpaperPath
$summary.wallpaper_apply.original_wallpaper = Get-DisplayGuestPath -Path $originalWallpaper

try {
    $sourcePath = Resolve-SourceJpegPath -Candidate $SourceJpeg
    $workingCopy = Join-Path (Split-Path -Parent $OutputPath) 'wallpaper-probe.jpg'
    Copy-Item -Path $sourcePath -Destination $workingCopy -Force
    $summary.wallpaper_apply.source_jpeg = Get-DisplayGuestPath -Path $sourcePath
    $summary.wallpaper_apply.working_copy = Get-DisplayGuestPath -Path $workingCopy

    & reg.exe add $registryPathNative /v $valueName /t REG_DWORD /d $ValueData /f | Out-Null
    $summary.applied = Get-ValueState

    & $wpr -cancel | Out-Null
    & $wpr -start GeneralProfile -filemode | Out-Null
    $traceStarted = $true

    Invoke-SetDesktopWallpaper -Path $workingCopy
    $summary.wallpaper_apply.candidate_invoked = $true
    Start-Sleep -Seconds $SettleSeconds
    $summary.wallpaper_apply.current_wallpaper = Get-DisplayGuestPath -Path (Get-WallpaperPath)
    $summary.shell_after = Get-ShellState
}
catch {
    $summary.errors += $_.Exception.Message
}
finally {
    if ($traceStarted) {
        try {
            & $wpr -stop $EtlPath | Out-Null
            $summary.etl_exists = [bool](Test-Path $EtlPath)
        }
        catch {
            $summary.errors += $_.Exception.Message
        }
    }

    Restore-ValueState -Original $before
    $summary.restored = Get-ValueState

    try {
        if (-not [string]::IsNullOrWhiteSpace($originalWallpaper) -and (Test-Path $originalWallpaper)) {
            $summary.wallpaper_apply.restored_original_exists = $true
            Invoke-SetDesktopWallpaper -Path $originalWallpaper
            $summary.wallpaper_apply.restore_invoked = $true
            Start-Sleep -Seconds $SettleSeconds
        }
    }
    catch {
        $summary.errors += $_.Exception.Message
    }

    $summary.wallpaper_apply.final_wallpaper = Get-DisplayGuestPath -Path (Get-WallpaperPath)
    $summary.generated_utc = [DateTime]::UtcNow.ToString('o')
    $summary | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath -Encoding UTF8
}

if ($summary.errors.Count -gt 0) {
    exit 1
}
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
        [string]$ValueState,
        [Parameter(Mandatory = $true)]
        [string]$Symptom,
        [bool]$ShellRecovered = $false,
        [bool]$NeededSnapshotRevert = $false,
        [string]$Notes = ''
    )

    & (Join-Path $PSScriptRoot 'log-vm-incident.ps1') `
        -RecordId $RecordId `
        -TweakId $RecordId `
        -TestId $TestId `
        -Family 'jpeg-import-quality-runtime' `
        -SnapshotName $SnapshotName `
        -RegistryPath 'HKCU\Control Panel\Desktop' `
        -ValueName 'JPEGImportQuality' `
        -ValueState $ValueState `
        -Symptom $Symptom `
        -ShellRecovered:$ShellRecovered `
        -NeededSnapshotRevert:$NeededSnapshotRevert `
        -Notes $Notes `
        -IncidentPath $IncidentLogPath | Out-Null
}

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    probe_name = $probeName
    record_id = $RecordId
    snapshot_name = $SnapshotName
    host_output_root = "evidence/files/vm-tooling-staging/$probeName"
    status = 'started'
    value_name = 'JPEGImportQuality'
    value_data = $ValueData
    shell_after = $null
    wallpaper_apply = $null
    wpr = [ordered]@{
        started = $false
        stopped = $false
        profile = 'GeneralProfile'
        guest_etl = 'jpeg-import-quality-runtime.etl'
        repo_etl_placeholder = "evidence/files/vm-tooling-staging/$probeName/jpeg-import-quality-runtime.etl.md"
    }
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
    $initialShell = Get-ShellHealth
    if (-not ($initialShell.explorer -and $initialShell.sihost -and $initialShell.shellhost)) {
        throw 'Shell health check failed before the JPEG import quality runtime probe started.'
    }

    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'createDirectoryInGuest', $VmPath, $guestRoot) -IgnoreExitCode | Out-Null
    Copy-ToGuest -HostPath $hostPayloadPath -GuestPath $guestPayloadPath

    $guestSummaryPath = Join-Path $guestRoot 'summary.json'
    $guestEtlPath = Join-Path $guestRoot 'jpeg-import-quality-runtime.etl'
    $guestInvocationError = $null
    $guestSummary = $null
    try {
        $guestArgs = @(
            '-NoProfile',
            '-ExecutionPolicy', 'Bypass',
            '-File', $guestPayloadPath,
            '-OutputPath', $guestSummaryPath,
            '-EtlPath', $guestEtlPath,
            '-ValueData', "$ValueData",
            '-SettleSeconds', "$SettleSeconds"
        )
        if (-not [string]::IsNullOrWhiteSpace($SourceJpeg)) {
            $guestArgs += @('-SourceJpeg', $SourceJpeg)
        }
        Invoke-GuestPowerShell -ArgumentList $guestArgs
    }
    catch {
        $guestInvocationError = $_.Exception.Message
    }

    try {
        Copy-FromGuest -GuestPath $guestSummaryPath -HostPath $hostSummaryPath
        $guestSummary = Get-Content -Path $hostSummaryPath -Raw | ConvertFrom-Json
        $summary.summary = $guestSummary
        $summary.wallpaper_apply = $guestSummary.wallpaper_apply
        $summary.wpr.started = $true
        $summary.wpr.stopped = [bool]$guestSummary.etl_exists
    }
    catch {
    }

    if ($guestInvocationError) {
        throw $guestInvocationError
    }

    $summary.shell_after = Get-ShellHealth

    if (-not ($summary.shell_after.explorer -and $summary.shell_after.sihost -and $summary.shell_after.shellhost)) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += 'Shell health was degraded after the JPEG import quality runtime probe.'
        Log-Incident -TestId $probeName -ValueState "JPEGImportQuality=$ValueData" -Symptom 'Shell health was degraded after the JPEG import quality runtime probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Recovered by snapshot revert after the wallpaper runtime lane.'
    }
    elseif ($guestSummary.errors.Count -gt 0) {
        $probeFailed = $true
        $needsRecovery = $true
        $summary.errors += @($guestSummary.errors)
        Log-Incident -TestId $probeName -ValueState "JPEGImportQuality=$ValueData" -Symptom 'Guest payload reported errors during the JPEG import quality runtime probe.' -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes (($guestSummary.errors -join '; '))
    }
}
catch {
    $probeFailed = $true
    $needsRecovery = $true
    $summary.errors += $_.Exception.Message
    Log-Incident -TestId $probeName -ValueState "JPEGImportQuality=$ValueData" -Symptom $_.Exception.Message -ShellRecovered:$false -NeededSnapshotRevert:$true -Notes 'Runtime lane failed before the JPEG summary completed. Recovered by snapshot revert.'
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

$etlPlaceholder = @(
    '# External Evidence Placeholder',
    '',
    'Title: JPEG import quality runtime WPR trace',
    '',
    'The raw ETL is kept off-git. Use the summary JSON in the same folder and the runtime lane manifest in evidence/records for the machine-readable result.'
) -join "`n"
Set-Content -Path (Join-Path $repoRootOut 'jpeg-import-quality-runtime.etl.md') -Value $etlPlaceholder -Encoding UTF8

Write-Output $repoSummaryPath
if ($probeFailed) {
    exit 1
}
