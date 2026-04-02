[CmdletBinding()]
param(
    [string]$SessionId = '',
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = $env:REGPROBE_VM_GUEST_PASSWORD,
    [string]$HostOutputRoot = 'H:\Temp\vm-tooling-staging',
    [string]$GuestRootBase = 'C:\RegProbe-Diag',
    [string]$RecordId = 'power.disable-cpu-idle-states',
    [string]$SnapshotName = '',
    [string]$IncidentLogPath = ''
)

& (Join-Path $PSScriptRoot 'run-cpu-idle-states-orchestration-step.ps1') `
    -Step A `
    -SessionId $SessionId `
    -VmPath $VmPath `
    -VmrunPath $VmrunPath `
    -GuestUser $GuestUser `
    -GuestPassword $GuestPassword `
    -HostOutputRoot $HostOutputRoot `
    -GuestRootBase $GuestRootBase `
    -RecordId $RecordId `
    -SnapshotName $SnapshotName `
    -IncidentLogPath $IncidentLogPath

