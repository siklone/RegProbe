[CmdletBinding()]
param(
    [string]$PayloadRoot = $PSScriptRoot,
    [string]$ToolRoot = 'C:\Tools',
    [string]$DiagRoot = 'C:\RegProbe-Diag',
    [string]$DotNetSdkVersion = '8.0.416',
    [string]$JavaVersion = '21.0.10+7',
    [string]$GhidraTag = 'Ghidra_12.0.4_build',
    [string]$WindowsSdkInstallerUrl = 'https://go.microsoft.com/fwlink/?linkid=2357925',
    [string]$StatusWebhook,
    [switch]$SkipWindowsPerformanceToolkit,
    [switch]$SkipGhidra,
    [switch]$SkipDiskSpd
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    Write-Host ("[bootstrap] {0}" -f $Name)
    try {
        $payload = & $Action
        if ($null -eq $payload) {
            return [ordered]@{ success = $true }
        }

        if ($payload -is [System.Collections.IDictionary]) {
            $step = [ordered]@{ success = $true }
            foreach ($key in $payload.Keys) {
                $step[$key] = $payload[$key]
            }
            return $step
        }

        if ($payload -is [psobject]) {
            $step = [ordered]@{ success = $true }
            foreach ($property in $payload.PSObject.Properties) {
                $step[$property.Name] = $property.Value
            }
            return $step
        }

        return [ordered]@{
            success = $true
            detail = [string]$payload
        }
    }
    catch {
        return [ordered]@{
            success = $false
            error = $_.Exception.Message
        }
    }
}

function Download-File {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$DestinationPath,
        [hashtable]$Headers = @{}
    )

    Ensure-Directory -Path (Split-Path -Parent $DestinationPath)
    $attempt = 0
    do {
        $attempt++
        try {
            Invoke-WebRequest -Uri $Uri -OutFile $DestinationPath -UseBasicParsing -Headers $Headers
            if (Test-Path $DestinationPath) {
                return $DestinationPath
            }
        }
        catch {
            if ($attempt -ge 3) {
                throw
            }

            Start-Sleep -Seconds (3 * $attempt)
        }
    } while ($attempt -lt 3)

    throw "Failed to download $Uri"
}

function Expand-ZipToDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    Ensure-Directory -Path $DestinationPath
    Expand-Archive -Path $ZipPath -DestinationPath $DestinationPath -Force
}

function Copy-DirectoryContent {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    Ensure-Directory -Path $DestinationPath
    Copy-Item -Path (Join-Path $SourcePath '*') -Destination $DestinationPath -Recurse -Force
}

function Add-MachinePathEntries {
    param([string[]]$Entries)

    $machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $parts = New-Object 'System.Collections.Generic.List[string]'
    foreach ($part in ($machinePath -split ';')) {
        if (-not [string]::IsNullOrWhiteSpace($part) -and $parts -notcontains $part) {
            $parts.Add($part)
        }
    }

    foreach ($entry in $Entries) {
        if (-not [string]::IsNullOrWhiteSpace($entry) -and (Test-Path $entry) -and $parts -notcontains $entry) {
            $parts.Add($entry)
        }
    }

    $newPath = ($parts -join ';')
    [Environment]::SetEnvironmentVariable('Path', $newPath, 'Machine')
    $env:Path = $newPath
}

function Set-MachineEnvironmentVariable {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    [Environment]::SetEnvironmentVariable($Name, $Value, 'Machine')
    Set-Item -Path ("Env:{0}" -f $Name) -Value $Value
}

function Get-GitHubJson {
    param([Parameter(Mandatory = $true)][string]$Uri)

    Invoke-RestMethod -Uri $Uri -Headers @{ 'User-Agent' = 'RegProbe-KVM-Bootstrap' }
}

function Send-StatusWebhook {
    param(
        [Parameter(Mandatory = $true)][string]$Phase,
        [object]$Payload
    )

    if ([string]::IsNullOrWhiteSpace($StatusWebhook)) {
        return
    }

    try {
        $body = [ordered]@{
            phase = $Phase
            generated_utc = [DateTime]::UtcNow.ToString('o')
            machine = $env:COMPUTERNAME
            user = $env:USERNAME
            is_admin = $isAdministrator
            payload = $Payload
        } | ConvertTo-Json -Depth 12

        Invoke-RestMethod -Method Post -Uri $StatusWebhook -ContentType 'application/json' -Body $body | Out-Null
    }
    catch {
        Write-Warning ("Status webhook failed: {0}" -f $_.Exception.Message)
    }
}

function Get-GuestWingetCommand {
    Get-Command winget.exe -ErrorAction SilentlyContinue
}

function Get-DebuggerToolRoots {
    $roots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($path in @(
        $symbolToolsRoot,
        'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Windows Kits\10\Debuggers\x64',
        'C:\Program Files\Debugging Tools for Windows (x64)',
        'C:\Program Files\Debugging Tools for Windows'
    )) {
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path) -and -not $roots.Contains($path)) {
            $roots.Add($path)
        }
    }

    foreach ($pkg in @(Get-AppxPackage -Name Microsoft.WinDbg* -ErrorAction SilentlyContinue)) {
        if ($pkg.InstallLocation -and (Test-Path $pkg.InstallLocation) -and -not $roots.Contains($pkg.InstallLocation)) {
            $roots.Add($pkg.InstallLocation)
        }
    }

    return @($roots)
}

function Find-FirstDebuggerTool {
    param([Parameter(Mandatory = $true)][string]$ToolName)

    foreach ($root in Get-DebuggerToolRoots) {
        $candidate = Get-ChildItem -Path $root -Recurse -Filter $ToolName -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    return $null
}

function Invoke-GuestWingetInstall {
    param([Parameter(Mandatory = $true)][string]$PackageId)

    $winget = Get-GuestWingetCommand
    if ($null -eq $winget) {
        return [ordered]@{
            package_id = $PackageId
            executed = $false
            exit_code = $null
            status = 'blocked-winget-missing'
        }
    }

    $args = @(
        'install',
        '--id', $PackageId,
        '--exact',
        '--source', 'winget',
        '--accept-package-agreements',
        '--accept-source-agreements',
        '--disable-interactivity'
    )

    $proc = Start-Process -FilePath $winget.Source -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
    return [ordered]@{
        package_id = $PackageId
        executed = $true
        exit_code = $proc.ExitCode
        status = if ($proc.ExitCode -eq 0) { 'ok' } else { 'failed' }
    }
}

function Install-WindowsDesktopDebuggersFromSdk {
    param(
        [Parameter(Mandatory = $true)][string]$DestinationRoot,
        [Parameter(Mandatory = $true)][string]$LogRoot
    )

    $installerPath = Join-Path $DestinationRoot 'winsdksetup.exe'
    $stagedInstaller = Join-Path $PayloadRoot 'winsdksetup.exe'
    if (Test-Path $stagedInstaller) {
        Copy-Item -Path $stagedInstaller -Destination $installerPath -Force
        $installerSource = $stagedInstaller
    }
    else {
        Download-File -Uri $WindowsSdkInstallerUrl -DestinationPath $installerPath | Out-Null
        $installerSource = $WindowsSdkInstallerUrl
    }

    $logPath = Join-Path $LogRoot 'winsdksetup-debuggers.log'
    $proc = Start-Process -FilePath $installerPath -ArgumentList @(
        '/quiet',
        '/norestart',
        '/ceip', 'off',
        '/features', 'OptionId.WindowsDesktopDebuggers',
        '/log', $logPath
    ) -PassThru -Wait

    if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne 3010) {
        throw "winsdksetup.exe exited with code $($proc.ExitCode)"
    }

    return [ordered]@{
        installer = $installerPath
        log = $logPath
        exit_code = $proc.ExitCode
        source = $installerSource
    }
}

$scriptsRoot = Join-Path $ToolRoot 'Scripts'
$perfRoot = Join-Path $ToolRoot 'Perf'
$sysinternalsRoot = Join-Path $ToolRoot 'Sysinternals'
$inboundRoot = Join-Path $ToolRoot 'Inbound'
$symbolToolsRoot = Join-Path $ToolRoot 'SymbolTools'
$javaRoot = Join-Path $ToolRoot 'Java'
$ghidraRoot = Join-Path $ToolRoot 'Ghidra'
$dotnetRoot = Join-Path $ToolRoot ("DotNetSDK\{0}" -f $DotNetSdkVersion)
$validationRoot = Join-Path $ToolRoot 'ValidationController\smoke'
$bootstrapRoot = Join-Path $DiagRoot 'bootstrap'
$guestToolsPayload = Join-Path $PayloadRoot 'guest-tools'
$repoScriptsPayload = Join-Path $PayloadRoot 'repo-scripts'
$resultPath = Join-Path $bootstrapRoot 'summary.json'
$isAdministrator = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

Ensure-Directory -Path $ToolRoot
Ensure-Directory -Path $DiagRoot
Ensure-Directory -Path $scriptsRoot
Ensure-Directory -Path $perfRoot
Ensure-Directory -Path $sysinternalsRoot
Ensure-Directory -Path $inboundRoot
Ensure-Directory -Path $symbolToolsRoot
Ensure-Directory -Path $javaRoot
Ensure-Directory -Path $ghidraRoot
Ensure-Directory -Path $validationRoot
Ensure-Directory -Path $bootstrapRoot
Ensure-Directory -Path (Join-Path $ToolRoot 'DiskSpd\work')
Ensure-Directory -Path (Join-Path $ToolRoot 'GhidraProjects')

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    payload_root = $PayloadRoot
    tool_root = $ToolRoot
    diag_root = $DiagRoot
    is_admin = $isAdministrator
    steps = [ordered]@{}
}

Send-StatusWebhook -Phase 'started' -Payload @{
    payload_root = $PayloadRoot
    tool_root = $ToolRoot
    diag_root = $DiagRoot
}

$result.steps['payload_copy'] = Invoke-Step -Name 'Copy guest tooling payload' -Action {
    $requiredScripts = @(
        'apply-defender-tooling-exclusions.ps1',
        'ghidra-headless.cmd',
        'procmon-safe.ps1',
        'run-ghidra-string-xref-probe.ps1',
        'run-ghidra-symbolized-probe.ps1',
        'tool-health-smoke.ps1',
        'wpa.cmd',
        'wpr.cmd',
        'wpr-start-general.cmd',
        'wpr-stop.cmd',
        'xperf.cmd'
    )

    $copyWarnings = New-Object 'System.Collections.Generic.List[string]'

    if (-not (Test-Path $guestToolsPayload)) {
        $copyWarnings.Add("Guest tools payload not found at $guestToolsPayload")
    }
    else {
        try {
            Copy-DirectoryContent -SourcePath $guestToolsPayload -DestinationPath $scriptsRoot
        }
        catch {
            $copyWarnings.Add($_.Exception.Message)
        }
    }

    if (-not (Test-Path $repoScriptsPayload)) {
        $copyWarnings.Add("Repo scripts payload not found at $repoScriptsPayload")
    }
    else {
        try {
            Copy-DirectoryContent -SourcePath $repoScriptsPayload -DestinationPath $scriptsRoot
        }
        catch {
            $copyWarnings.Add($_.Exception.Message)
        }
    }

    $availableScripts = @(Get-ChildItem -Path $scriptsRoot -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name | Sort-Object)
    $missingScripts = @($requiredScripts | Where-Object { $availableScripts -notcontains $_ })
    if ($missingScripts.Count -gt 0) {
        throw "Required guest tooling is still missing after payload staging: $($missingScripts -join ', ')"
    }

    @{
        guest_tools = $availableScripts
        reused_existing_payload = [bool]($copyWarnings.Count -gt 0)
        warnings = @($copyWarnings)
    }
}

$result.steps['defender'] = Invoke-Step -Name 'Apply bounded Defender exclusions' -Action {
    $scriptPath = Join-Path $scriptsRoot 'apply-defender-tooling-exclusions.ps1'
    if (-not (Test-Path $scriptPath)) {
        throw "apply-defender-tooling-exclusions.ps1 not found at $scriptPath"
    }

    $outputPath = Join-Path $bootstrapRoot 'tooling-defender-exclusions.json'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $scriptPath -Mode apply -OutputPath $outputPath | Out-Null
    if (-not (Test-Path $outputPath)) {
        throw 'Defender exclusion output was not created.'
    }

    Get-Content -Path $outputPath -Raw | ConvertFrom-Json
}

$result.steps['procmon'] = Invoke-Step -Name 'Install Procmon' -Action {
    $zipPath = Join-Path $inboundRoot 'ProcessMonitor.zip'
    Download-File -Uri 'https://download.sysinternals.com/files/ProcessMonitor.zip' -DestinationPath $zipPath | Out-Null
    $extractRoot = Join-Path $bootstrapRoot 'procmon-extract'
    if (Test-Path $extractRoot) {
        Remove-Item -Path $extractRoot -Recurse -Force
    }

    Expand-ZipToDirectory -ZipPath $zipPath -DestinationPath $extractRoot
    $procmonExe = Get-ChildItem -Path $extractRoot -Recurse -Filter 'Procmon64.exe' | Select-Object -First 1
    if (-not $procmonExe) {
        throw 'Procmon64.exe was not present in the downloaded archive.'
    }

    Copy-Item -Path $procmonExe.FullName -Destination (Join-Path $sysinternalsRoot 'Procmon64.exe') -Force
    @{
        procmon_path = (Join-Path $sysinternalsRoot 'Procmon64.exe')
        archive = $zipPath
    }
}

$result.steps['dotnet_sdk'] = Invoke-Step -Name 'Install .NET SDK' -Action {
    $dotnetExe = Join-Path $dotnetRoot 'dotnet.exe'
    if (-not (Test-Path $dotnetExe)) {
        $dotnetInstallScript = Join-Path $inboundRoot 'dotnet-install.ps1'
        Download-File -Uri 'https://dot.net/v1/dotnet-install.ps1' -DestinationPath $dotnetInstallScript | Out-Null
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $dotnetInstallScript -Version $DotNetSdkVersion -InstallDir $dotnetRoot -Architecture x64 -NoPath | Out-Null
    }

    if (-not (Test-Path $dotnetExe)) {
        throw "dotnet.exe was not installed at $dotnetExe"
    }

    @{
        dotnet_path = $dotnetExe
        version = (& $dotnetExe --version | Select-Object -First 1)
    }
}

$result.steps['diskspd'] = Invoke-Step -Name 'Install DiskSpd' -Action {
    if ($SkipDiskSpd) {
        return @{ skipped = $true }
    }

    $release = Get-GitHubJson -Uri 'https://api.github.com/repos/microsoft/diskspd/releases/latest'
    $asset = $release.assets | Where-Object { $_.name -match 'diskspd.*\.zip$' } | Select-Object -First 1
    if (-not $asset) {
        throw 'DiskSpd release asset was not found.'
    }

    $zipPath = Join-Path $inboundRoot $asset.name
    Download-File -Uri $asset.browser_download_url -DestinationPath $zipPath -Headers @{ 'User-Agent' = 'RegProbe-KVM-Bootstrap' } | Out-Null
    $extractRoot = Join-Path $bootstrapRoot 'diskspd-extract'
    if (Test-Path $extractRoot) {
        Remove-Item -Path $extractRoot -Recurse -Force
    }

    Expand-ZipToDirectory -ZipPath $zipPath -DestinationPath $extractRoot
    $diskspdExe = Get-ChildItem -Path $extractRoot -Recurse -Filter 'diskspd.exe' | Select-Object -First 1
    if (-not $diskspdExe) {
        throw 'diskspd.exe was not present in the downloaded archive.'
    }

    $target = Join-Path $perfRoot 'diskspd.exe'
    Copy-Item -Path $diskspdExe.FullName -Destination $target -Force
    @{
        diskspd_path = $target
        archive = $zipPath
    }
}

$result.steps['java'] = Invoke-Step -Name 'Install Java 21' -Action {
    $escapedVersion = [Uri]::EscapeDataString("jdk-$JavaVersion")
    $zipPath = Join-Path $inboundRoot 'temurin-jdk.zip'
    $javaApi = "https://api.adoptium.net/v3/binary/version/$escapedVersion/windows/x64/jdk/hotspot/normal/eclipse"
    Download-File -Uri $javaApi -DestinationPath $zipPath | Out-Null

    $extractRoot = Join-Path $bootstrapRoot 'java-extract'
    if (Test-Path $extractRoot) {
        Remove-Item -Path $extractRoot -Recurse -Force
    }

    Expand-ZipToDirectory -ZipPath $zipPath -DestinationPath $extractRoot
    $jdkRoot = Get-ChildItem -Path $extractRoot -Directory | Select-Object -First 1
    if (-not $jdkRoot) {
        throw 'The Java archive did not expand to a JDK directory.'
    }

    $targetRoot = Join-Path $javaRoot $jdkRoot.Name
    if (Test-Path $targetRoot) {
        Remove-Item -Path $targetRoot -Recurse -Force
    }

    Copy-Item -Path $jdkRoot.FullName -Destination $targetRoot -Recurse -Force
    Set-MachineEnvironmentVariable -Name 'JAVA_HOME' -Value $targetRoot
    @{
        java_home = $targetRoot
        archive = $zipPath
    }
}

$result.steps['ghidra'] = Invoke-Step -Name 'Install Ghidra' -Action {
    if ($SkipGhidra) {
        return @{ skipped = $true }
    }

    $release = Get-GitHubJson -Uri ("https://api.github.com/repos/NationalSecurityAgency/ghidra/releases/tags/{0}" -f $GhidraTag)
    $asset = $release.assets | Where-Object { $_.name -match '^ghidra_.*_PUBLIC_.*\.zip$' } | Select-Object -First 1
    if (-not $asset) {
        throw "No Ghidra zip asset was found for tag $GhidraTag"
    }

    $zipPath = Join-Path $inboundRoot $asset.name
    Download-File -Uri $asset.browser_download_url -DestinationPath $zipPath -Headers @{ 'User-Agent' = 'RegProbe-KVM-Bootstrap' } | Out-Null
    $extractRoot = Join-Path $bootstrapRoot 'ghidra-extract'
    if (Test-Path $extractRoot) {
        Remove-Item -Path $extractRoot -Recurse -Force
    }

    Expand-ZipToDirectory -ZipPath $zipPath -DestinationPath $extractRoot
    $ghidraFolder = Get-ChildItem -Path $extractRoot -Directory | Select-Object -First 1
    if (-not $ghidraFolder) {
        throw 'The Ghidra archive did not expand to a directory.'
    }

    $targetRoot = Join-Path $ghidraRoot $ghidraFolder.Name
    if (Test-Path $targetRoot) {
        Remove-Item -Path $targetRoot -Recurse -Force
    }

    Copy-Item -Path $ghidraFolder.FullName -Destination $targetRoot -Recurse -Force
    Set-MachineEnvironmentVariable -Name 'GHIDRA_HOME' -Value $targetRoot
    @{
        ghidra_home = $targetRoot
        archive = $zipPath
    }
}

$result.steps['wpt'] = Invoke-Step -Name 'Install Windows Performance Toolkit' -Action {
    if ($SkipWindowsPerformanceToolkit) {
        return @{ skipped = $true }
    }

    $wptRoot = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit'
    $wpr = Join-Path $wptRoot 'wpr.exe'
    $wpa = Join-Path $wptRoot 'wpa.exe'
    $xperf = Join-Path $wptRoot 'xperf.exe'
    if (-not ((Test-Path $wpr) -and (Test-Path $wpa) -and (Test-Path $xperf))) {
        $adkSetup = Join-Path $inboundRoot 'adksetup.exe'
        Download-File -Uri 'https://go.microsoft.com/fwlink/?linkid=2289980' -DestinationPath $adkSetup | Out-Null
        $logPath = Join-Path $bootstrapRoot 'adksetup.log'
        $proc = Start-Process -FilePath $adkSetup -ArgumentList @('/quiet', '/norestart', '/ceip', 'off', '/features', 'OptionId.WindowsPerformanceToolkit', '/log', $logPath) -PassThru -Wait
        if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne 3010) {
            throw "adksetup.exe exited with code $($proc.ExitCode)"
        }
    }

    if (-not ((Test-Path $wpr) -and (Test-Path $wpa) -and (Test-Path $xperf))) {
        throw 'Windows Performance Toolkit was not found after installation.'
    }

    @{
        wpr = $wpr
        wpa = $wpa
        xperf = $xperf
    }
}

$result.steps['symbol_tools'] = Invoke-Step -Name 'Install symbol tools' -Action {
    $attempts = @()
    $wingetAvailable = [bool](Get-GuestWingetCommand)
    $symchk = Find-FirstDebuggerTool -ToolName 'symchk.exe'
    $dbghelp = Find-FirstDebuggerTool -ToolName 'dbghelp.dll'
    $windbg = Find-FirstDebuggerTool -ToolName 'windbg.exe'

    if (-not $symchk -and $wingetAvailable) {
        foreach ($packageId in @('Microsoft.WindowsSDK.10.0.18362', 'Microsoft.WindowsSDK.10.0.17134')) {
            $attempts += Invoke-GuestWingetInstall -PackageId $PackageId
            Start-Sleep -Seconds 3
            $symchk = Find-FirstDebuggerTool -ToolName 'symchk.exe'
            $dbghelp = Find-FirstDebuggerTool -ToolName 'dbghelp.dll'
            if ($symchk -and $dbghelp) {
                break
            }
        }
    }

    if (-not $windbg -and $wingetAvailable) {
        $attempts += Invoke-GuestWingetInstall -PackageId 'Microsoft.WinDbg'
        Start-Sleep -Seconds 3
        $windbg = Find-FirstDebuggerTool -ToolName 'windbg.exe'
    }

    if (-not $symchk -or -not $dbghelp) {
        $attempts += Install-WindowsDesktopDebuggersFromSdk -DestinationRoot $inboundRoot -LogRoot $bootstrapRoot
        Start-Sleep -Seconds 3
        $symchk = Find-FirstDebuggerTool -ToolName 'symchk.exe'
        $dbghelp = Find-FirstDebuggerTool -ToolName 'dbghelp.dll'
        $windbg = Find-FirstDebuggerTool -ToolName 'windbg.exe'
    }

    $symchk = Find-FirstDebuggerTool -ToolName 'symchk.exe'
    $dbghelp = Find-FirstDebuggerTool -ToolName 'dbghelp.dll'
    $windbg = Find-FirstDebuggerTool -ToolName 'windbg.exe'

    if (-not $symchk) {
        throw 'symchk.exe was not found after symbol-tool provisioning.'
    }

    if (-not $dbghelp) {
        throw 'dbghelp.dll was not found after symbol-tool provisioning.'
    }

    @{
        winget_available = $wingetAvailable
        attempts = @($attempts)
        symchk = $symchk
        dbghelp = $dbghelp
        windbg = $windbg
    }
}

$result.steps['environment'] = Invoke-Step -Name 'Finalize machine environment' -Action {
    $pathEntries = @(
        $scriptsRoot,
        $sysinternalsRoot,
        $perfRoot,
        'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit',
        (Join-Path $javaRoot ("jdk-{0}\bin" -f $JavaVersion)),
        (Join-Path $javaRoot ("jdk-{0}" -f $JavaVersion)),
        $dotnetRoot,
        'C:\Program Files\dotnet'
    )

    $symchkPath = Find-FirstDebuggerTool -ToolName 'symchk.exe'
    if ($symchkPath) {
        $pathEntries += (Split-Path -Parent $symchkPath)
    }

    Add-MachinePathEntries -Entries $pathEntries

    @{
        path_entries = @(
            [Environment]::GetEnvironmentVariable('Path', 'Machine') -split ';' |
            Where-Object { $_ -like 'C:\Tools*' -or $_ -like 'C:\Program Files*Windows Performance Toolkit*' -or $_ -eq 'C:\Program Files\dotnet' }
        )
        java_home = [Environment]::GetEnvironmentVariable('JAVA_HOME', 'Machine')
        ghidra_home = [Environment]::GetEnvironmentVariable('GHIDRA_HOME', 'Machine')
    }
}

$result.steps['tool_health'] = Invoke-Step -Name 'Run tool health smoke' -Action {
    $toolHealthScript = Join-Path $scriptsRoot 'tool-health-smoke.ps1'
    if (-not (Test-Path $toolHealthScript)) {
        throw "tool-health-smoke.ps1 not found at $toolHealthScript"
    }

    $toolHealthArgs = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $toolHealthScript,
        '-OutputRoot', $validationRoot
    )
    if ($SkipGhidra) {
        $toolHealthArgs += '-SkipGhidraSmoke'
    }

    $outputPath = & powershell.exe @toolHealthArgs
    if (-not $outputPath -or -not (Test-Path $outputPath.Trim())) {
        throw 'tool-health-smoke.ps1 did not produce an output file.'
    }

    $summary = Get-Content -Path $outputPath.Trim() -Raw | ConvertFrom-Json
    @{
        output_path = $outputPath.Trim()
        summary = $summary
    }
}

$failedSteps = @(
    $result.steps.GetEnumerator() |
    Where-Object { -not $_.Value.success } |
    Select-Object -ExpandProperty Key
)

$result.status = if ($failedSteps.Count -eq 0) { 'ok' } else { 'partial' }
$result.failed_steps = @($failedSteps)
$result | ConvertTo-Json -Depth 10 | Set-Content -Path $resultPath -Encoding UTF8
Send-StatusWebhook -Phase 'completed' -Payload @{
    summary_path = $resultPath
    result = $result
}
Write-Output $resultPath
