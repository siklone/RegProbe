[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputJson,

    [Parameter(Mandatory = $true)]
    [string]$OutputReg,

    [string]$ExclusionPath = 'C:\Temp\CodexExclusionProbe'
)

$ErrorActionPreference = 'Stop'

function Get-PolicyValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    try {
        $item = Get-ItemProperty -Path $Path -Name $Name -ErrorAction Stop
        return $item.$Name
    }
    catch {
        return $null
    }
}

$managedExclusionRegistryPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Paths'
$defenderOwnedExclusionRegistryPath = 'HKLM:\SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths'
$errorPath = [System.IO.Path]::ChangeExtension($OutputJson, '.error.txt')

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputJson) | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputReg) | Out-Null
New-Item -ItemType Directory -Force -Path $ExclusionPath | Out-Null

$regOutput = ''
$regExitCode = $null

try {
    try {
        Remove-ItemProperty -Path $managedExclusionRegistryPath -Name $ExclusionPath -ErrorAction SilentlyContinue
    }
    catch {
    }

    New-Item -ItemType Directory -Force -Path $managedExclusionRegistryPath | Out-Null
    New-ItemProperty -Path $managedExclusionRegistryPath -Name $ExclusionPath -PropertyType DWord -Value 0 -Force | Out-Null
    Start-Sleep -Seconds 2

    try {
        Start-Process 'windowsdefender:' | Out-Null
    }
    catch {
    }
    Start-Sleep -Seconds 10

    $preferences = Get-MpPreference
    $exclusions = @($preferences.ExclusionPath)

    $regOutput = & reg.exe query 'HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths' 2>&1 | Out-String
    $regExitCode = $LASTEXITCODE
    $managedRegOutput = & reg.exe query 'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Paths' 2>&1 | Out-String
    $managedRegExitCode = $LASTEXITCODE

    $payload = [ordered]@{
        exclusion_path = $ExclusionPath
        exclusion_visible_in_get_mppreference = ($exclusions -contains $ExclusionPath)
        get_mppreference_exclusion_paths = $exclusions
        root_policy_value = Get-PolicyValue -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender' -Name 'HideExclusionsFromLocalAdmins'
        policy_manager_value = Get-PolicyValue -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager' -Name 'HideExclusionsFromLocalAdmins'
        registry_query_exitcode = $regExitCode
        registry_query_contains_exclusion = ($regOutput -match [regex]::Escape($ExclusionPath))
        managed_registry_query_exitcode = $managedRegExitCode
        managed_registry_query_contains_exclusion = ($managedRegOutput -match [regex]::Escape($ExclusionPath))
    }

    $payload | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputJson -Encoding UTF8
    $regOutput | Set-Content -Path $OutputReg -Encoding UTF8
    Add-Content -Path $OutputReg -Value ("EXITCODE={0}" -f $regExitCode)
    Add-Content -Path $OutputReg -Value ''
    Add-Content -Path $OutputReg -Value '--- MANAGED POLICY EXCLUSIONS ---'
    Add-Content -Path $OutputReg -Value $managedRegOutput
    Add-Content -Path $OutputReg -Value ("MANAGED_EXITCODE={0}" -f $managedRegExitCode)
}
catch {
    $lines = @(
        $_.Exception.GetType().FullName,
        $_.Exception.Message
    )
    if ($_.InvocationInfo) {
        $lines += $_.InvocationInfo.PositionMessage
    }
    $lines | Set-Content -Path $errorPath -Encoding UTF8
    throw
}
finally {
    try {
        Remove-ItemProperty -Path $managedExclusionRegistryPath -Name $ExclusionPath -ErrorAction SilentlyContinue
    }
    catch {
    }
}
