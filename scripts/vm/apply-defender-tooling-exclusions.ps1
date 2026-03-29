[CmdletBinding()]
param(
    [ValidateSet('apply', 'read')]
    [string]$Mode = 'apply',

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string[]]$ExclusionPaths = @(
        'C:\Tools',
        'C:\RegProbe-Diag'
    ),

    [string[]]$ExclusionProcesses = @(
        'powershell.exe',
        'Procmon64.exe',
        'wpr.exe',
        'wpa.exe',
        'xperf.exe',
        'java.exe',
        'javaw.exe',
        'diskspd.exe',
        'winsat.exe',
        'RegProbe.App.exe',
        'RegProbe.ElevatedHost.exe'
    ),

    [string]$ExecutionPolicy = 'RemoteSigned'
)

$ErrorActionPreference = 'Stop'

function Get-ExecutionPolicyMap {
    $map = [ordered]@{}
    foreach ($entry in (Get-ExecutionPolicy -List)) {
        $map[[string]$entry.Scope] = [string]$entry.ExecutionPolicy
    }

    return $map
}

function Get-PreferenceSnapshot {
    $pref = Get-MpPreference
    return [ordered]@{
        exclusion_paths = @($pref.ExclusionPath | Sort-Object -Unique)
        exclusion_processes = @($pref.ExclusionProcess | Sort-Object -Unique)
        execution_policy = Get-ExecutionPolicyMap
    }
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Add-UniquePathExclusion {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Applied
    )

    $current = @((Get-MpPreference).ExclusionPath)
    if ($current -notcontains $Path) {
        Add-MpPreference -ExclusionPath $Path
        $Applied.Add($Path)
    }
}

function Add-UniqueProcessExclusion {
    param(
        [Parameter(Mandatory = $true)][string]$Process,
        [Parameter(Mandatory = $true)]$Applied
    )

    $current = @((Get-MpPreference).ExclusionProcess)
    if ($current -notcontains $Process) {
        Add-MpPreference -ExclusionProcess $Process
        $Applied.Add($Process)
    }
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null

$appliedPaths = New-Object 'System.Collections.Generic.List[string]'
$appliedProcesses = New-Object 'System.Collections.Generic.List[string]'
$errors = New-Object 'System.Collections.Generic.List[string]'
$before = $null
$after = $null

try {
    $before = Get-PreferenceSnapshot

    foreach ($path in $ExclusionPaths) {
        Ensure-Directory -Path $path
    }

    if ($Mode -eq 'apply') {
        foreach ($path in $ExclusionPaths) {
            try {
                Add-UniquePathExclusion -Path $path -Applied $appliedPaths
            }
            catch {
                $errors.Add("Failed to add path exclusion '$path': $($_.Exception.Message)")
            }
        }

        foreach ($process in $ExclusionProcesses) {
            try {
                Add-UniqueProcessExclusion -Process $process -Applied $appliedProcesses
            }
            catch {
                $errors.Add("Failed to add process exclusion '$process': $($_.Exception.Message)")
            }
        }

        if ($before.execution_policy.LocalMachine -ne $ExecutionPolicy) {
            try {
                Set-ExecutionPolicy -ExecutionPolicy $ExecutionPolicy -Scope LocalMachine -Force
            }
            catch {
                $errors.Add("Failed to set LocalMachine execution policy to ${ExecutionPolicy}: $($_.Exception.Message)")
            }
        }
    }

    $after = Get-PreferenceSnapshot
}
catch {
    $errors.Add($_.Exception.Message)
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    mode = $Mode
    exclusion_paths_requested = @($ExclusionPaths)
    exclusion_processes_requested = @($ExclusionProcesses)
    execution_policy_requested = $ExecutionPolicy
    applied_paths = @($appliedPaths)
    applied_processes = @($appliedProcesses)
    before = $before
    after = $after
    status = if ($errors.Count -eq 0) { 'ok' } else { 'error' }
    errors = @($errors)
}

$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
