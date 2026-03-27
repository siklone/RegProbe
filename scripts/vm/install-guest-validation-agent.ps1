[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestScriptPath = 'C:\Tools\Scripts\guest-validation-agent.ps1',
    [string]$SharedRootGuest = '\\vmware-host\Shared Folders\vm-tooling-staging'
)

$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$localAgent = Join-Path $PSScriptRoot 'guest-validation-agent.ps1'

function Invoke-Vmrun {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [int]$TimeoutSeconds = 120
    )

    & $VmrunPath @Arguments | Out-String
}

if (-not (Test-Path $localAgent)) {
    throw "Guest agent script not found at $localAgent"
}

Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'copyFileFromHostToGuest', $VmPath, $localAgent, $GuestScriptPath)

$taskCommand = @"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$GuestScriptPath" -SharedRoot "$SharedRootGuest"
"@

$registerScript = @"
`$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-NoProfile -ExecutionPolicy Bypass -File "$GuestScriptPath" -SharedRoot "$SharedRootGuest"'
`$trigger = New-ScheduledTaskTrigger -AtStartup
Register-ScheduledTask -TaskName 'RegProbeValidationAgent' -Action `$action -Trigger `$trigger -User 'SYSTEM' -RunLevel Highest -Force
"@

$tempRegister = Join-Path $env:TEMP 'register-guest-validation-agent.ps1'
$registerScript | Set-Content -Path $tempRegister -Encoding ASCII
try {
    $guestRegister = 'C:\Users\Administrator\Desktop\register-guest-validation-agent.ps1'
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'copyFileFromHostToGuest', $VmPath, $tempRegister, $guestRegister)
    Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'runProgramInGuest', $VmPath, 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe', '-ExecutionPolicy', 'Bypass', '-File', $guestRegister)
} finally {
    Remove-Item -Path $tempRegister -ErrorAction SilentlyContinue
}

Write-Host 'Guest validation agent installed and scheduled task registered.'
