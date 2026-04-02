$ErrorActionPreference = 'Stop'

$machine = $env:COMPUTERNAME
$winlogon = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
$userList = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\SpecialAccounts\UserList'

foreach ($name in @('Admin', 'User')) {
    $user = Get-LocalUser -Name $name -ErrorAction SilentlyContinue
    if ($user) {
        Disable-LocalUser -Name $name
    }
}

New-Item -Path $userList -Force | Out-Null
New-ItemProperty -Path $userList -Name 'codexvm' -PropertyType DWord -Value 0 -Force | Out-Null
New-ItemProperty -Path $userList -Name 'Admin' -PropertyType DWord -Value 0 -Force | Out-Null
New-ItemProperty -Path $userList -Name 'User' -PropertyType DWord -Value 0 -Force | Out-Null
New-ItemProperty -Path $userList -Name 'Administrator' -PropertyType DWord -Value 1 -Force | Out-Null

New-ItemProperty -Path $winlogon -Name 'AutoAdminLogon' -PropertyType String -Value '1' -Force | Out-Null
New-ItemProperty -Path $winlogon -Name 'ForceAutoLogon' -PropertyType String -Value '1' -Force | Out-Null
New-ItemProperty -Path $winlogon -Name 'DefaultUserName' -PropertyType String -Value 'Administrator' -Force | Out-Null
New-ItemProperty -Path $winlogon -Name 'DefaultPassword' -PropertyType String -Value $env:REGPROBE_VM_GUEST_PASSWORD -Force | Out-Null
New-ItemProperty -Path $winlogon -Name 'DefaultDomainName' -PropertyType String -Value $machine -Force | Out-Null
Remove-ItemProperty -Path $winlogon -Name 'AutoLogonCount' -ErrorAction SilentlyContinue

New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'dontdisplaylastusername' -PropertyType DWord -Value 0 -Force | Out-Null

[ordered]@{
    machine = $machine
    auto_admin_logon = (Get-ItemProperty $winlogon).AutoAdminLogon
    force_auto_logon = (Get-ItemProperty $winlogon).ForceAutoLogon
    default_user = (Get-ItemProperty $winlogon).DefaultUserName
    default_domain = (Get-ItemProperty $winlogon).DefaultDomainName
    default_password_present = -not [string]::IsNullOrEmpty((Get-ItemProperty $winlogon).DefaultPassword)
    auto_logon_count = (Get-ItemProperty $winlogon -ErrorAction SilentlyContinue).AutoLogonCount
    users = Get-LocalUser | Select-Object Name, Enabled, LastLogon
} | ConvertTo-Json -Depth 5 | Out-File -Encoding utf8 'C:\Temp\guest-logon-fix-state.json'

