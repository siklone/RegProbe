$paths = @(
    'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon',
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
)

$result = [ordered]@{}
foreach ($path in $paths) {
    if (Test-Path $path) {
        $item = Get-ItemProperty $path
        $result[$path] = [ordered]@{
            AutoAdminLogon = $item.AutoAdminLogon
            ForceAutoLogon = $item.ForceAutoLogon
            DefaultUserName = $item.DefaultUserName
            DefaultDomainName = $item.DefaultDomainName
            DefaultPasswordPresent = -not [string]::IsNullOrEmpty($item.DefaultPassword)
            AutoLogonCount = $item.AutoLogonCount
            LastUsedUsername = $item.LastUsedUsername
            DontDisplayLastUserName = $item.DontDisplayLastUserName
            HideFastUserSwitching = $item.HideFastUserSwitching
        }
    }
}

$result | ConvertTo-Json -Depth 5 | Out-File -Encoding utf8 'C:\Temp\logon-settings.json'
