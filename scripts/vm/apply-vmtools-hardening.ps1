[CmdletBinding()]
param(
    [ValidateSet('apply', 'read')]
    [string]$Mode = 'apply',
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

function Get-VmToolsServiceInfo {
    $service = Get-CimInstance Win32_Service |
        Where-Object { $_.Name -eq 'VMTools' -or $_.DisplayName -like 'VMware Tools*' } |
        Select-Object -First 1

    if ($null -eq $service) {
        throw 'VMware Tools service was not found in the guest.'
    }

    return $service
}

function Get-ServiceFailureText {
    param([Parameter(Mandatory = $true)][string]$ServiceName)

    return ((& sc.exe qfailure $ServiceName 2>&1) | Out-String).Trim()
}

function Get-VmtoolsdPriority {
    try {
        $proc = Get-Process -Name 'vmtoolsd' -ErrorAction Stop | Select-Object -First 1
        return $proc.PriorityClass.ToString()
    }
    catch {
        return $null
    }
}

function Get-StateSnapshot {
    $service = Get-VmToolsServiceInfo
    $serviceObject = Get-Service -Name $service.Name -ErrorAction Stop

    return [ordered]@{
        service_name = $service.Name
        display_name = $service.DisplayName
        state = $service.State
        status = $serviceObject.Status.ToString()
        start_mode = $service.StartMode
        failure_config = Get-ServiceFailureText -ServiceName $service.Name
        failure_flag_enabled = ((& sc.exe qfailureflag $service.Name 2>&1) | Out-String).Trim()
        vmtoolsd_priority = Get-VmtoolsdPriority
        directories = @(
            [ordered]@{ path = 'C:\RegProbe-Diag'; exists = [bool](Test-Path -LiteralPath 'C:\RegProbe-Diag') }
            [ordered]@{ path = 'C:\Tools\Scripts'; exists = [bool](Test-Path -LiteralPath 'C:\Tools\Scripts') }
        )
    }
}

$before = $null
$after = $null
$errors = New-Object System.Collections.Generic.List[string]

try {
    $before = Get-StateSnapshot

    if ($Mode -eq 'apply') {
        foreach ($dir in @('C:\RegProbe-Diag', 'C:\Tools\Scripts')) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }

        & sc.exe failure $before.service_name reset= 30 actions= restart/5000/restart/10000/restart/30000 | Out-Null
        & sc.exe failureflag $before.service_name 1 | Out-Null
        Set-Service -Name $before.service_name -StartupType Automatic
        Start-Service -Name $before.service_name -ErrorAction SilentlyContinue | Out-Null
        Start-Sleep -Seconds 2

        try {
            $proc = Get-Process -Name 'vmtoolsd' -ErrorAction Stop | Select-Object -First 1
            if ($proc.PriorityClass -lt [System.Diagnostics.ProcessPriorityClass]::AboveNormal) {
                $proc.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::AboveNormal
            }
        }
        catch {
            $errors.Add("Unable to raise vmtoolsd priority: $($_.Exception.Message)")
        }
    }

    $after = Get-StateSnapshot
}
catch {
    $errors.Add($_.Exception.Message)
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    mode = $Mode
    status = if ($errors.Count -eq 0) { 'ok' } else { 'error' }
    before = $before
    after = $after
    errors = @($errors)
}

$json = $payload | ConvertTo-Json -Depth 8
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null
    Set-Content -Path $OutputPath -Value $json -Encoding UTF8
}

$json
