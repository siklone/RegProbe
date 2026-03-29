[CmdletBinding()]
param(
    [ValidateSet('audit', 'cleanup')]
    [string]$Mode = 'audit',

    [string]$OutputPath = 'C:\RegProbe-Diag\guest-app-artifact-audit.json',

    [string]$UserProfileRoot = 'C:\Users\Administrator'
)

$ErrorActionPreference = 'Stop'

$identityTokens = @(
    'RegProbe',
    'OpenTrace',
    'OpenTraceProject',
    'WindowsOptimizer'
)

$binaryPatterns = @(
    'RegProbe*.exe',
    'OpenTrace*.exe',
    'OpenTraceProject*.exe',
    'WindowsOptimizer*.exe'
)

$processNames = @(
    'RegProbe.App',
    'RegProbe.ElevatedHost',
    'OpenTraceProject.App',
    'OpenTraceProject.ElevatedHost',
    'WindowsOptimizer.App',
    'WindowsOptimizer.ElevatedHost'
)

$scheduledTaskAllowlist = @(
    'RegProbeValidationAgent'
)

$commandAllowlistFragments = @(
    'C:\RegProbe-Diag',
    'guest-validation-agent.ps1',
    'RegProbeValidationAgent'
)

$userStartupFolder = Join-Path $UserProfileRoot 'AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup'
$machineStartupFolder = 'C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup'
$appDataRoots = @(
    (Join-Path $UserProfileRoot 'AppData\Local'),
    (Join-Path $UserProfileRoot 'AppData\LocalLow'),
    (Join-Path $UserProfileRoot 'AppData\Roaming')
)
$binarySearchRoots = @(
    'C:\Tools',
    (Join-Path $UserProfileRoot 'Desktop'),
    (Join-Path $UserProfileRoot 'Downloads'),
    'C:\Users\Public\Desktop'
)
$startupRegistryPaths = @(
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run',
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\RunOnce',
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Run',
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\RunOnce',
    'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run',
    'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce'
)

function Test-TextHasTargetToken {
    param([AllowNull()][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $false
    }

    foreach ($token in $identityTokens) {
        if ($Text -like "*$token*") {
            return $true
        }
    }

    return $false
}

function Test-CommandAllowlisted {
    param([AllowNull()][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $false
    }

    foreach ($fragment in $commandAllowlistFragments) {
        if ($Text -like "*$fragment*") {
            return $true
        }
    }

    return $false
}

function Test-NameMatchesPattern {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if ($Name -like $pattern) {
            return $true
        }
    }

    return $false
}

function ConvertTo-SafeString {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    if ($Value -is [System.Array]) {
        return (($Value | ForEach-Object { [string]$_ }) -join '; ')
    }

    return [string]$Value
}

function New-ResultObject {
    param([Parameter(Mandatory = $true)][hashtable]$Fields)

    return [pscustomobject]$Fields
}

function Get-RootDeployTargets {
    $items = New-Object 'System.Collections.Generic.List[object]'
    $patterns = @('C:\RegProbe*', 'C:\OpenTraceProject*', 'C:\WindowsOptimizer*')

    foreach ($pattern in $patterns) {
        foreach ($item in @(Get-Item -Path $pattern -ErrorAction SilentlyContinue)) {
            if ($null -eq $item) {
                continue
            }

            if ($item.FullName -ieq 'C:\RegProbe-Diag') {
                continue
            }

            $items.Add((New-ResultObject @{
                path = $item.FullName
                kind = if ($item.PSIsContainer) { 'directory' } else { 'file' }
            }))
        }
    }

    return @(
        $items |
        Group-Object -Property path |
        ForEach-Object { $_.Group[0] } |
        Sort-Object path
    )
}

function Get-DeployDirectories {
    $items = New-Object 'System.Collections.Generic.List[object]'

    if (Test-Path 'C:\Tools\AppSmoke') {
        $items.Add((New-ResultObject @{
            path = 'C:\Tools\AppSmoke'
            kind = 'directory'
        }))
    }

    foreach ($item in Get-RootDeployTargets | Where-Object { $_.kind -eq 'directory' }) {
        $items.Add($item)
    }

    return @(
        $items |
        Group-Object -Property path |
        ForEach-Object { $_.Group[0] } |
        Sort-Object path
    )
}

function Get-InboundPayloads {
    $items = New-Object 'System.Collections.Generic.List[object]'
    $inboundRoot = 'C:\Tools\Inbound'

    if (-not (Test-Path $inboundRoot)) {
        return @()
    }

    foreach ($item in Get-ChildItem -Path $inboundRoot -Force -ErrorAction SilentlyContinue) {
        $isNamedPayload =
            ($item.Name -ieq 'app-publish.zip') -or
            (Test-TextHasTargetToken -Text $item.Name)

        if (-not $isNamedPayload) {
            continue
        }

        $items.Add((New-ResultObject @{
            path = $item.FullName
            kind = if ($item.PSIsContainer) { 'directory' } else { 'file' }
        }))
    }

    return @(
        $items |
        Group-Object -Property path |
        ForEach-Object { $_.Group[0] } |
        Sort-Object path
    )
}

function Get-AppDataArtifactRoots {
    $items = New-Object 'System.Collections.Generic.List[object]'

    foreach ($root in $appDataRoots) {
        if (-not (Test-Path $root)) {
            continue
        }

        foreach ($item in Get-ChildItem -Path $root -Force -ErrorAction SilentlyContinue) {
            if (-not (Test-TextHasTargetToken -Text $item.Name)) {
                continue
            }

            $items.Add((New-ResultObject @{
                path = $item.FullName
                kind = if ($item.PSIsContainer) { 'directory' } else { 'file' }
            }))
        }
    }

    return @(
        $items |
        Group-Object -Property path |
        ForEach-Object { $_.Group[0] } |
        Sort-Object path
    )
}

function Get-StaleProcesses {
    return @(
        Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $processNames -contains $_.ProcessName } |
        Sort-Object ProcessName, Id |
        ForEach-Object {
            New-ResultObject @{
                name = $_.ProcessName
                id = $_.Id
            }
        }
    )
}

function Get-StaleScheduledTasks {
    $items = New-Object 'System.Collections.Generic.List[object]'

    foreach ($task in @(Get-ScheduledTask -ErrorAction SilentlyContinue)) {
        $actionText = @(
            foreach ($action in @($task.Actions)) {
                '{0} {1} {2}' -f (ConvertTo-SafeString $action.Execute), (ConvertTo-SafeString $action.Arguments), (ConvertTo-SafeString $action.WorkingDirectory)
            }
        ) -join ' | '

        $combinedText = '{0} {1} {2}' -f (ConvertTo-SafeString $task.TaskName), (ConvertTo-SafeString $task.TaskPath), $actionText
        if (-not (Test-TextHasTargetToken -Text $combinedText)) {
            continue
        }

        if (($scheduledTaskAllowlist -contains $task.TaskName) -or (Test-CommandAllowlisted -Text $combinedText)) {
            continue
        }

        $items.Add((New-ResultObject @{
            task_name = $task.TaskName
            task_path = $task.TaskPath
            state = [string]$task.State
            actions = @(
                foreach ($action in @($task.Actions)) {
                    New-ResultObject @{
                        execute = ConvertTo-SafeString $action.Execute
                        arguments = ConvertTo-SafeString $action.Arguments
                        working_directory = ConvertTo-SafeString $action.WorkingDirectory
                    }
                }
            )
        }))
    }

    return @(
        $items |
        Sort-Object task_path, task_name
    )
}

function Get-StaleServices {
    $items = New-Object 'System.Collections.Generic.List[object]'

    foreach ($service in @(Get-CimInstance Win32_Service -ErrorAction SilentlyContinue)) {
        $combinedText = '{0} {1} {2}' -f (ConvertTo-SafeString $service.Name), (ConvertTo-SafeString $service.DisplayName), (ConvertTo-SafeString $service.PathName)
        if (-not (Test-TextHasTargetToken -Text $combinedText)) {
            continue
        }

        if (Test-CommandAllowlisted -Text $combinedText) {
            continue
        }

        $items.Add((New-ResultObject @{
            name = [string]$service.Name
            display_name = [string]$service.DisplayName
            state = [string]$service.State
            start_mode = [string]$service.StartMode
            path_name = ConvertTo-SafeString $service.PathName
        }))
    }

    return @(
        $items |
        Sort-Object name
    )
}

function Get-StaleStartupRegistryEntries {
    $items = New-Object 'System.Collections.Generic.List[object]'

    foreach ($path in $startupRegistryPaths) {
        if (-not (Test-Path $path)) {
            continue
        }

        $item = Get-ItemProperty -Path $path -ErrorAction SilentlyContinue
        if ($null -eq $item) {
            continue
        }

        foreach ($property in $item.PSObject.Properties) {
            if ($property.Name -like 'PS*') {
                continue
            }

            $valueText = ConvertTo-SafeString $property.Value
            $combinedText = '{0} {1}' -f $property.Name, $valueText
            if (-not (Test-TextHasTargetToken -Text $combinedText)) {
                continue
            }

            if (Test-CommandAllowlisted -Text $combinedText) {
                continue
            }

            $items.Add((New-ResultObject @{
                path = $path
                name = $property.Name
                value = $valueText
            }))
        }
    }

    return @(
        $items |
        Sort-Object path, name
    )
}

function Get-ShortcutMetadata {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    try {
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($LiteralPath)
        return [ordered]@{
            target_path = ConvertTo-SafeString $shortcut.TargetPath
            arguments = ConvertTo-SafeString $shortcut.Arguments
            working_directory = ConvertTo-SafeString $shortcut.WorkingDirectory
        }
    }
    catch {
        return [ordered]@{
            target_path = ''
            arguments = ''
            working_directory = ''
        }
    }
}

function Get-StaleStartupFiles {
    $items = New-Object 'System.Collections.Generic.List[object]'

    foreach ($folder in @($userStartupFolder, $machineStartupFolder)) {
        if (-not (Test-Path $folder)) {
            continue
        }

        foreach ($item in Get-ChildItem -Path $folder -Force -ErrorAction SilentlyContinue) {
            $metadata = if ($item.Extension -ieq '.lnk') { Get-ShortcutMetadata -LiteralPath $item.FullName } else { [ordered]@{ target_path = ''; arguments = ''; working_directory = '' } }
            $combinedText = '{0} {1} {2} {3}' -f $item.Name, $metadata.target_path, $metadata.arguments, $metadata.working_directory
            if (-not (Test-TextHasTargetToken -Text $combinedText)) {
                continue
            }

            if (Test-CommandAllowlisted -Text $combinedText) {
                continue
            }

            $items.Add((New-ResultObject @{
                path = $item.FullName
                name = $item.Name
                target_path = $metadata.target_path
                arguments = $metadata.arguments
                working_directory = $metadata.working_directory
            }))
        }
    }

    return @(
        $items |
        Sort-Object path
    )
}

function Get-StaleBinaries {
    $roots = New-Object 'System.Collections.Generic.List[string]'

    foreach ($path in $binarySearchRoots) {
        if (Test-Path $path) {
            $roots.Add($path)
        }
    }

    foreach ($item in Get-DeployDirectories) {
        if (Test-Path $item.path) {
            $roots.Add($item.path)
        }
    }

    foreach ($item in Get-InboundPayloads | Where-Object { $_.kind -eq 'directory' }) {
        if (Test-Path $item.path) {
            $roots.Add($item.path)
        }
    }

    foreach ($item in Get-AppDataArtifactRoots | Where-Object { $_.kind -eq 'directory' }) {
        if (Test-Path $item.path) {
            $roots.Add($item.path)
        }
    }

    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($root in @($roots | Sort-Object -Unique)) {
        foreach ($file in Get-ChildItem -Path $root -Recurse -File -Force -ErrorAction SilentlyContinue) {
            if (-not (Test-NameMatchesPattern -Name $file.Name -Patterns $binaryPatterns)) {
                continue
            }

            $items.Add((New-ResultObject @{
                path = $file.FullName
                name = $file.Name
                directory = $file.DirectoryName
            }))
        }
    }

    return @(
        $items |
        Group-Object -Property path |
        ForEach-Object { $_.Group[0] } |
        Sort-Object path
    )
}

function Get-AppArtifactState {
    $deployDirectories = @(Get-DeployDirectories)
    $inboundPayloads = @(Get-InboundPayloads)
    $rootDeployTargets = @(Get-RootDeployTargets)
    $appDataArtifactRoots = @(Get-AppDataArtifactRoots)
    $scheduledTasks = @(Get-StaleScheduledTasks)
    $services = @(Get-StaleServices)
    $startupRegistry = @(Get-StaleStartupRegistryEntries)
    $startupFiles = @(Get-StaleStartupFiles)
    $processes = @(Get-StaleProcesses)
    $staleBinaries = @(Get-StaleBinaries)

    $staleBinaryCount = @($staleBinaries).Count
    $residualCount =
        @($deployDirectories).Count +
        @($inboundPayloads).Count +
        @($rootDeployTargets).Count +
        @($appDataArtifactRoots).Count +
        @($scheduledTasks).Count +
        @($services).Count +
        @($startupRegistry).Count +
        @($startupFiles).Count +
        @($processes).Count

    return [ordered]@{
        clean = ($staleBinaryCount -eq 0 -and $residualCount -eq 0)
        stale_binary_count = $staleBinaryCount
        residual_item_count = $residualCount
        deploy_directories = $deployDirectories
        inbound_payloads = $inboundPayloads
        root_deploy_targets = $rootDeployTargets
        appdata_roots = $appDataArtifactRoots
        scheduled_tasks = $scheduledTasks
        services = $services
        startup_registry = $startupRegistry
        startup_files = $startupFiles
        running_processes = $processes
        stale_binaries = $staleBinaries
    }
}

function Remove-ArtifactPath {
    param(
        [Parameter(Mandatory = $true)][string]$LiteralPath,
        [Parameter(Mandatory = $true)]$Removed,
        [Parameter(Mandatory = $true)]$Errors
    )

    if (-not (Test-Path $LiteralPath)) {
        return
    }

    try {
        $item = Get-Item -LiteralPath $LiteralPath -Force -ErrorAction Stop
        Remove-Item -LiteralPath $item.FullName -Recurse -Force -ErrorAction Stop
        $Removed.Add([string]$item.FullName)
    }
    catch {
        $Errors.Add("Failed to remove path '$LiteralPath': $($_.Exception.Message)")
    }
}

function Stop-StaleProcesses {
    param(
        [Parameter(Mandatory = $true)]$Removed,
        [Parameter(Mandatory = $true)]$Errors
    )

    foreach ($proc in Get-StaleProcesses) {
        try {
            Stop-Process -Id $proc.id -Force -ErrorAction Stop
            $Removed.Add('{0}:{1}' -f $proc.name, $proc.id)
        }
        catch {
            $Errors.Add("Failed to stop process '$($proc.name)' ($($proc.id)): $($_.Exception.Message)")
        }
    }
}

function Remove-StaleScheduledTasks {
    param(
        [Parameter(Mandatory = $true)]$Removed,
        [Parameter(Mandatory = $true)]$Errors
    )

    foreach ($task in Get-StaleScheduledTasks) {
        try {
            Unregister-ScheduledTask -TaskName $task.task_name -TaskPath $task.task_path -Confirm:$false -ErrorAction Stop
            $Removed.Add('{0}{1}' -f $task.task_path, $task.task_name)
        }
        catch {
            $Errors.Add("Failed to remove scheduled task '$($task.task_path)$($task.task_name)': $($_.Exception.Message)")
        }
    }
}

function Remove-StaleServices {
    param(
        [Parameter(Mandatory = $true)]$Removed,
        [Parameter(Mandatory = $true)]$Errors
    )

    foreach ($service in Get-StaleServices) {
        try {
            if ($service.state -eq 'Running') {
                Stop-Service -Name $service.name -Force -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 1
            }

            & sc.exe delete $service.name | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "sc delete returned exit code $LASTEXITCODE"
            }

            $Removed.Add($service.name)
        }
        catch {
            $Errors.Add("Failed to remove service '$($service.name)': $($_.Exception.Message)")
        }
    }
}

function Remove-StaleStartupRegistryEntries {
    param(
        [Parameter(Mandatory = $true)]$Removed,
        [Parameter(Mandatory = $true)]$Errors
    )

    foreach ($entry in Get-StaleStartupRegistryEntries) {
        try {
            Remove-ItemProperty -Path $entry.path -Name $entry.name -ErrorAction Stop
            $Removed.Add('{0}::{1}' -f $entry.path, $entry.name)
        }
        catch {
            $Errors.Add("Failed to remove startup registry entry '$($entry.path)::$($entry.name)': $($_.Exception.Message)")
        }
    }
}

function Remove-StaleStartupFiles {
    param(
        [Parameter(Mandatory = $true)]$Removed,
        [Parameter(Mandatory = $true)]$Errors
    )

    foreach ($item in Get-StaleStartupFiles) {
        Remove-ArtifactPath -LiteralPath $item.path -Removed $Removed -Errors $Errors
    }
}

function Remove-StaleDeployArtifacts {
    param(
        [Parameter(Mandatory = $true)]$Removed,
        [Parameter(Mandatory = $true)]$Errors
    )

    foreach ($item in Get-DeployDirectories) {
        Remove-ArtifactPath -LiteralPath $item.path -Removed $Removed -Errors $Errors
    }

    foreach ($item in Get-RootDeployTargets | Where-Object { $_.kind -eq 'file' }) {
        Remove-ArtifactPath -LiteralPath $item.path -Removed $Removed -Errors $Errors
    }

    foreach ($item in Get-InboundPayloads) {
        Remove-ArtifactPath -LiteralPath $item.path -Removed $Removed -Errors $Errors
    }

    foreach ($item in Get-AppDataArtifactRoots) {
        Remove-ArtifactPath -LiteralPath $item.path -Removed $Removed -Errors $Errors
    }

    foreach ($item in Get-StaleBinaries) {
        Remove-ArtifactPath -LiteralPath $item.path -Removed $Removed -Errors $Errors
    }
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null

$removalActions = [ordered]@{
    stopped_processes = @()
    removed_scheduled_tasks = @()
    removed_services = @()
    removed_startup_registry = @()
    removed_startup_files = @()
    removed_paths = @()
}
$errors = New-Object 'System.Collections.Generic.List[string]'
$preState = $null
$postState = $null

try {
    $preState = Get-AppArtifactState

    if ($Mode -eq 'cleanup') {
        $stopped = New-Object 'System.Collections.Generic.List[string]'
        $removedTasks = New-Object 'System.Collections.Generic.List[string]'
        $removedServices = New-Object 'System.Collections.Generic.List[string]'
        $removedRegistry = New-Object 'System.Collections.Generic.List[string]'
        $removedStartupFiles = New-Object 'System.Collections.Generic.List[string]'
        $removedPaths = New-Object 'System.Collections.Generic.List[string]'

        Stop-StaleProcesses -Removed $stopped -Errors $errors
        Remove-StaleScheduledTasks -Removed $removedTasks -Errors $errors
        Remove-StaleServices -Removed $removedServices -Errors $errors
        Remove-StaleStartupRegistryEntries -Removed $removedRegistry -Errors $errors
        Remove-StaleStartupFiles -Removed $removedStartupFiles -Errors $errors
        Remove-StaleDeployArtifacts -Removed $removedPaths -Errors $errors

        $removalActions.stopped_processes = @($stopped)
        $removalActions.removed_scheduled_tasks = @($removedTasks)
        $removalActions.removed_services = @($removedServices)
        $removalActions.removed_startup_registry = @($removedRegistry)
        $removalActions.removed_startup_files = @($removedStartupFiles)
        $removalActions.removed_paths = @($removedPaths)

        $postState = Get-AppArtifactState
    }
}
catch {
    $errors.Add($_.Exception.Message)
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    mode = $Mode
    user_profile_root = $UserProfileRoot
    pre_cleanup = $preState
    removals = $removalActions
    post_cleanup = $postState
    policy_compliant =
        if ($Mode -eq 'cleanup') {
            [bool]($postState -and $postState.clean)
        }
        else {
            [bool]($preState -and $preState.clean)
        }
    status =
        if ($errors.Count -eq 0) {
            'ok'
        }
        elseif ($Mode -eq 'cleanup' -and $postState -and $postState.clean) {
            'ok-with-warnings'
        }
        else {
            'error'
        }
    errors = @($errors)
}

$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath -Encoding UTF8
Write-Output $OutputPath
