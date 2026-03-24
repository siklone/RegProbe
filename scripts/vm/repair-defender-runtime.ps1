[CmdletBinding()]
param(
    [string]$OutputJson = 'C:\Tools\Perf\Procmon\defender-runtime-repair.json',
    [switch]$Repair
)

$ErrorActionPreference = 'Stop'

function Get-MpStatusSummary {
    try {
        $status = Get-MpComputerStatus -ErrorAction Stop
        return [ordered]@{
            am_running_mode = $status.AMRunningMode
            antivirus_enabled = $status.AntivirusEnabled
            realtime_enabled = $status.RealTimeProtectionEnabled
            behavior_monitor_enabled = $status.BehaviorMonitorEnabled
            ioav_enabled = $status.IoavProtectionEnabled
            antispyware_enabled = $status.AntispywareEnabled
        }
    }
    catch {
        return [ordered]@{
            error = $_.Exception.Message
        }
    }
}

function Get-ServiceSummary {
    param([string]$Name)

    try {
        $service = Get-CimInstance -ClassName Win32_Service -Filter ("Name='{0}'" -f $Name) -ErrorAction Stop
        if (-not $service) {
            return [ordered]@{ name = $Name; exists = $false }
        }

        return [ordered]@{
            name = $Name
            exists = $true
            state = $service.State
            status = $service.Status
            start_mode = $service.StartMode
            path_name = $service.PathName
        }
    }
    catch {
        return [ordered]@{
            name = $Name
            exists = $false
            error = $_.Exception.Message
        }
    }
}

$actions = New-Object System.Collections.Generic.List[string]
$errors = New-Object System.Collections.Generic.List[string]

$summary = [ordered]@{
    timestamp = (Get-Date).ToString('o')
    repair_requested = [bool]$Repair
    before = [ordered]@{
        mp_status = Get-MpStatusSummary
        services = @(
            Get-ServiceSummary -Name 'WinDefend'
            Get-ServiceSummary -Name 'WdNisSvc'
            Get-ServiceSummary -Name 'SecurityHealthService'
        )
    }
}

if ($Repair) {
    try {
        & sc.exe config WinDefend start= auto | Out-Null
        $actions.Add('Configured WinDefend start=auto')
    }
    catch {
        $errors.Add('Failed to configure WinDefend start mode: ' + $_.Exception.Message)
    }

    try {
        Start-Service -Name 'WinDefend' -ErrorAction Stop
        $actions.Add('Started WinDefend')
    }
    catch {
        $errors.Add('Failed to start WinDefend: ' + $_.Exception.Message)
    }

    try {
        Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction Stop
        $actions.Add('Set DisableRealtimeMonitoring = false')
    }
    catch {
        $errors.Add('Failed to set DisableRealtimeMonitoring = false: ' + $_.Exception.Message)
    }

    try {
        Start-Service -Name 'SecurityHealthService' -ErrorAction SilentlyContinue
        $actions.Add('Attempted to start SecurityHealthService')
    }
    catch {
    }

    Start-Sleep -Seconds 8
}

$summary.actions = @($actions)
$summary.errors = @($errors)
$summary.after = [ordered]@{
    mp_status = Get-MpStatusSummary
    services = @(
        Get-ServiceSummary -Name 'WinDefend'
        Get-ServiceSummary -Name 'WdNisSvc'
        Get-ServiceSummary -Name 'SecurityHealthService'
    )
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputJson) | Out-Null
$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputJson -Encoding UTF8
