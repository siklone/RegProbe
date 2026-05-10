# KVM Machine/Policy Revalidation Batch

Date: 2026-05-06T00:20:41.9332318Z
Domain: `regprobe-win11-25h2-session`

This batch re-read selected machine-scope runtime and policy targets on the live KVM guest without changing guest configuration.

## Machine

- Computer: `DESKTOP-AHPV0FV`
- CurrentBuildNumber: `26200`
- UBR: `8246`

## Observations

### `power.optimize-gaming-network`

- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games` :: `Priority`
  path_exists=`True` value_exists=`True` value=`2`
- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games` :: `Scheduling Category`
  path_exists=`True` value_exists=`True` value=`Medium`
- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games` :: `SFIO Priority`
  path_exists=`True` value_exists=`True` value=`Normal`
- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games` :: `GPU Priority`
  path_exists=`True` value_exists=`True` value=`8`
- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games` :: `Affinity`
  path_exists=`True` value_exists=`True` value=`0`

### `privacy.deny-app-access.policy`

- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessAccountInfo`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessCalendar`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessCallHistory`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessCamera`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessContacts`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessEmail`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessGraphicsCaptureProgrammatic`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessGraphicsCaptureWithoutBorder`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessHumanPresence`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessLocation`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessMessaging`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessMicrophone`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessMotion`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessNotifications`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessPhone`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessRadios`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsSyncWithDevices`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessTasks`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessTrustedDevices`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsRunInBackground`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsGetDiagnosticInfo`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessGazeInput`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsActivateWithVoice`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsActivateWithVoiceAboveLock`
  path_exists=`True` value_exists=`False` value=`None`
- `HKLM\Software\Policies\Microsoft\Windows\AppPrivacy` :: `LetAppsAccessBackgroundSpatialPerception`
  path_exists=`True` value_exists=`False` value=`None`

### `security.disable-defender-sample-submission`

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet` :: `SubmitSamplesConsent`
  path_exists=`False` value_exists=`False` value=`None`

### `security.disable-enhanced-defender-notifications`

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications` :: `DisableEnhancedNotifications`
  path_exists=`False` value_exists=`False` value=`None`

### `security.disable-system-mitigations`

- `HKLM\Software\Policies\Microsoft\Windows Defender ExploitGuard\Exploit Protection` :: `ExploitProtectionSettings`
  path_exists=`False` value_exists=`False` value=`None`

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
