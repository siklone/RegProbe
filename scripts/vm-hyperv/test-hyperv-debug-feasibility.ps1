[CmdletBinding()]
param(
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

function Get-OptionalFeatureState {
    param([Parameter(Mandatory = $true)][string]$Name)

    try {
        $feature = Get-WindowsOptionalFeature -Online -FeatureName $Name -ErrorAction Stop
        return [ordered]@{
            name = $Name
            state = [string]$feature.State
        }
    }
    catch {
        return [ordered]@{
            name = $Name
            state = 'unknown'
            error = $_.Exception.Message
        }
    }
}

function Get-ServiceSnapshot {
    param([Parameter(Mandatory = $true)][string]$Name)

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        return [ordered]@{
            name = $Name
            present = $false
        }
    }

    return [ordered]@{
        name = $Name
        present = $true
        status = [string]$service.Status
        start_type = try { [string]$service.StartType } catch { 'unknown' }
    }
}

function Get-CommandSnapshot {
    param([Parameter(Mandatory = $true)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        return [ordered]@{
            name = $Name
            present = $false
        }
    }

    return [ordered]@{
        name = $Name
        present = $true
        source = [string]$command.Source
        module = [string]$command.ModuleName
        command_type = [string]$command.CommandType
    }
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

$computerInfo = Get-ComputerInfo
$systemDrive = Get-PSDrive -Name C -ErrorAction SilentlyContinue
$features = @(
    Get-OptionalFeatureState -Name 'Microsoft-Hyper-V'
    Get-OptionalFeatureState -Name 'Microsoft-Hyper-V-Management-PowerShell'
    Get-OptionalFeatureState -Name 'VirtualMachinePlatform'
)
$services = @(
    Get-ServiceSnapshot -Name 'vmcompute'
    Get-ServiceSnapshot -Name 'vmms'
    Get-ServiceSnapshot -Name 'hns'
)
$module = Get-Module -ListAvailable Hyper-V | Select-Object -First 1
$commands = @(
    Get-CommandSnapshot -Name 'Get-VM'
    Get-CommandSnapshot -Name 'New-VM'
    Get-CommandSnapshot -Name 'Start-VM'
    Get-CommandSnapshot -Name 'Stop-VM'
    Get-CommandSnapshot -Name 'Checkpoint-VM'
)

$hyperVFeatureEnabled = @($features | Where-Object { $_.name -eq 'Microsoft-Hyper-V' -and $_.state -eq 'Enabled' }).Count -gt 0
$hyperVManagementEnabled = @($features | Where-Object { $_.name -eq 'Microsoft-Hyper-V-Management-PowerShell' -and $_.state -eq 'Enabled' }).Count -gt 0
$virtualMachinePlatformEnabled = @($features | Where-Object { $_.name -eq 'VirtualMachinePlatform' -and $_.state -eq 'Enabled' }).Count -gt 0
$modulePresent = $null -ne $module
$cmdletCount = @($commands | Where-Object { $_.present }).Count
$hypervisorPresent = [bool]$computerInfo.HyperVisorPresent

$hyperVBlockers = New-Object System.Collections.Generic.List[string]
if (-not $hyperVFeatureEnabled) {
    $hyperVBlockers.Add('feature-disabled:Microsoft-Hyper-V')
}
if (-not $hyperVManagementEnabled) {
    $hyperVBlockers.Add('feature-disabled:Microsoft-Hyper-V-Management-PowerShell')
}
if (-not $modulePresent) {
    $hyperVBlockers.Add('module-missing:Hyper-V')
}
if ($cmdletCount -lt 5) {
    $hyperVBlockers.Add('cmdlets-missing:Hyper-V')
}
if (-not $hypervisorPresent) {
    $hyperVBlockers.Add('hypervisor-not-present')
}

$hyperVSupportedNow = $hyperVBlockers.Count -eq 0
$hyperVReadiness = if ($hyperVSupportedNow) {
    'ready'
}
elseif ($hypervisorPresent -or $virtualMachinePlatformEnabled) {
    'blocked-prereqs'
}
else {
    'unsupported'
}

$hyperVCandidate = [ordered]@{
    type = 'Hyper-V'
    supported = $hyperVSupportedNow
    readiness = $hyperVReadiness
    priority = 1
    reason = if ($hyperVSupportedNow) {
        'Native Microsoft hypervisor and debugger stack is available.'
    }
    else {
        'Preferred long-term debugger-first environment, but host prerequisites are not complete yet.'
    }
    blockers = @($hyperVBlockers)
}

$vmwareFallback = [ordered]@{
    type = 'VMware-debug-only'
    supported = $true
    readiness = 'ready'
    priority = 2
    reason = 'Immediate fallback if Hyper-V remains unavailable, but keep it separate from runtime VMware lanes.'
    blockers = @(
        'named-pipe-contract-unreliable'
    )
}

$selection = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    debug_environment_candidates = @(
        $hyperVCandidate
        $vmwareFallback
    )
    selected = 'Hyper-V'
    selected_status = if ($hyperVSupportedNow) { 'ready' } else { 'blocked-prereqs' }
    selected_long_term = 'Hyper-V'
    selected_long_term_status = if ($hyperVSupportedNow) { 'ready' } else { 'blocked-prereqs' }
    selected_immediate = if ($hyperVSupportedNow) { 'Hyper-V' } else { 'VMware-debug-only' }
    selected_immediate_status = if ($hyperVSupportedNow) { 'ready' } else { 'ready-short-try' }
    selected_immediate_reason = if ($hyperVSupportedNow) {
        'Hyper-V prerequisites are already available on this host.'
    }
    else {
        'Hyper-V is the preferred long-term debugger-first environment, but while host prerequisites remain blocked the next short debugger-first attempt should use a fresh VMware debug-only VM instead of the frozen lane.'
    }
    fallback_if_blocked = 'VMware-debug-only'
    frozen_lane_return_allowed = $false
    host = [ordered]@{
        os_name = [string]$computerInfo.WindowsProductName
        os_version = [string]$computerInfo.WindowsVersion
        os_build = [string]$computerInfo.OsBuildNumber
        hypervisor_present = $hypervisorPresent
        system_drive_free_bytes = if ($systemDrive) { [int64]$systemDrive.Free } else { $null }
    }
    hyperv = [ordered]@{
        supported_now = $hyperVSupportedNow
        readiness = $hyperVReadiness
        features = $features
        services = $services
        module_present = $modulePresent
        module_path = if ($modulePresent) { [string]$module.Path } else { $null }
        cmdlets = $commands
        virtual_machine_platform_enabled = $virtualMachinePlatformEnabled
    }
    debug_environment = 'hyperv'
    debug_vm_name = 'RegProbe-Debug-HyperV'
    debug_transport = 'serial'
    debug_baseline = 'RegProbe-Debug-HyperV-Baseline-20260403'
    transport_reliability = 'low'
    reproducible_runs = 0
    vm_role = 'debug_arbiter_only'
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-JsonFile -Path $OutputPath -InputObject $selection
}

$selection | ConvertTo-Json -Depth 10
