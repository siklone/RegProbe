# KVM Policy Revalidation Batch

Date: 2026-05-05T13:13:57.1735609Z
Domain: `regprobe-win11-25h2-session`

This batch re-read selected machine-scope policy and registry targets on the live KVM guest without changing guest configuration.

## Machine

- Computer: `DESKTOP-AHPV0FV`
- CurrentBuildNumber: `26200`
- UBR: `8246`

## Observations

### `privacy.disable-appcompat-engine.policy`

- `HKLM\Software\Policies\Microsoft\Windows\AppCompat` :: `SbEnable`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppCompat` :: `DisableEngine`
  path_exists=`True` value_exists=`False` value=`None`

### `privacy.set-diagnostic-data-to-minimum-supported-level`

- `HKLM\Software\Policies\Microsoft\Windows\DataCollection` :: `AllowTelemetry`
  path_exists=`True` value_exists=`False` value=`None`

### `security.disable-defender-sample-submission`

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet` :: `SubmitSamplesConsent`
  path_exists=`False` value_exists=`False` value=`None`

### `security.disable-enhanced-defender-notifications`

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications` :: `DisableEnhancedNotifications`
  path_exists=`False` value_exists=`False` value=`None`

### `security.disable-ntfs-encryption`

- `HKLM\System\CurrentControlSet\Policies` :: `NtfsDisableEncryption`
  path_exists=`True` value_exists=`False` value=`None`

### `security.disable-password-reveal`

- `HKLM\Software\Policies\Microsoft\Windows\CredUI` :: `DisablePasswordReveal`
  path_exists=`True` value_exists=`False` value=`None`

### `security.disable-picture-password`

- `HKLM\Software\Policies\Microsoft\Windows\System` :: `BlockDomainPicturePassword`
  path_exists=`True` value_exists=`False` value=`None`

### `security.disable-system-restore`

- `HKLM\Software\Policies\Microsoft\Windows NT\SystemRestore` :: `DisableSR`
  path_exists=`False` value_exists=`False` value=`None`

### `security.disable-windows-firewall`

- `HKLM\SOFTWARE\Policies\Microsoft\WindowsFirewall\DomainProfile` :: `EnableFirewall`
  path_exists=`False` value_exists=`False` value=`None`
- `HKLM\SOFTWARE\Policies\Microsoft\WindowsFirewall\StandardProfile` :: `EnableFirewall`
  path_exists=`False` value_exists=`False` value=`None`

### `security.disable-windows-update.policy`

- `HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate` :: `DisableWindowsUpdateAccess`
  path_exists=`False` value_exists=`False` value=`None`
- `HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU` :: `NoAutoUpdate`
  path_exists=`False` value_exists=`False` value=`None`

### `security.enable-dynamic-lock`

- `HKLM\SOFTWARE\Policies\Microsoft\PassportForWork\DynamicLock` :: `DynamicLock`
  path_exists=`False` value_exists=`False` value=`None`

### `security.hide-defender-exclusions-from-local-admins`

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender` :: `HideExclusionsFromLocalAdmins`
  path_exists=`True` value_exists=`False` value=`None`

### `security.powershell-unrestricted`

- `HKLM\Software\Policies\Microsoft\Windows\PowerShell` :: `EnableScripts`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\PowerShell` :: `ExecutionPolicy`
  path_exists=`True` value_exists=`False` value=`None`

### `security.uac-never-notify`

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` :: `EnableLUA`
  path_exists=`True` value_exists=`True` value=`0`
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` :: `ConsentPromptBehaviorAdmin`
  path_exists=`True` value_exists=`True` value=`0`
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` :: `PromptOnSecureDesktop`
  path_exists=`True` value_exists=`True` value=`0`

### `system.disable-shortcut-arrow`

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons` :: `29`
  path_exists=`True` value_exists=`False` value=`None`

