# Windows Privacy and Telemetry Configuration - Verified Documentation
## Microsoft-first references, English only

This document consolidates Windows 10/11 privacy and telemetry configuration
settings. All settings are verified against Microsoft Learn documentation. Any
non-Microsoft references are listed as secondary sources.

Related docs:
- [Privacy tweaks](privacy.md)
- [Tweak catalog](../tweaks/tweak-catalog.html)
- [Tweak details](../tweaks/tweak-details.html)

---

## Table of Contents

1. [Telemetry and Diagnostic Data](#1-telemetry-and-diagnostic-data)
2. [Windows Error Reporting (WER)](#2-windows-error-reporting-wer)
3. [TDR - GPU Timeout Settings](#3-tdr---gpu-timeout-settings)
4. [Location and Sensor Services](#4-location-and-sensor-services)
5. [App Privacy Permissions](#5-app-privacy-permissions)
6. [Activity and Sync](#6-activity-and-sync)
7. [Cross-Device Experiences](#7-cross-device-experiences)
8. [Cortana and Speech](#8-cortana-and-speech)
9. [Feedback and Suggestions](#9-feedback-and-suggestions)
10. [Automatic Maintenance](#10-automatic-maintenance)
11. [Maps and Font Providers](#11-maps-and-font-providers)
12. [Xbox and Gaming](#12-xbox-and-gaming)
13. [Biometrics](#13-biometrics)
14. [Remote Desktop and Assistance](#14-remote-desktop-and-assistance)
15. [App Compatibility](#15-app-compatibility)
16. [File History and Offline Files](#16-file-history-and-offline-files)
17. [Troubleshooting](#17-troubleshooting)
18. [Crash Dump and Sleep Study](#18-crash-dump-and-sleep-study)
19. [Additional Privacy Settings](#19-additional-privacy-settings)
20. [UI Privacy](#20-ui-privacy)

---

## 1. Telemetry and Diagnostic Data

Windows diagnostic data controls the level of system information sent to
Microsoft.

### AllowTelemetry (primary telemetry control)

Registry path:
```
HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection
```

Value name: `AllowTelemetry` (REG_DWORD)

| Value | Windows 10 | Windows 11 | Notes |
| --- | --- | --- | --- |
| 0 | Security | Diagnostic data off | Enterprise/Education/Server only |
| 1 | Basic | Required diagnostic data | Minimum required data |
| 2 | Enhanced | - | Removed in Windows 11 |
| 3 | Full | Optional diagnostic data | Includes optional logs/dumps |

Group Policy:
`Computer Configuration > Administrative Templates > Windows Components > Data Collection and Preview Builds > Allow Diagnostic Data`

ADMX: DataCollection.admx

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system#allowtelemetry
- https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization

### Related telemetry settings

```
HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection
```

- `DisableOneSettingsDownloads = 1` (disable OneSettings configuration downloads)
- `EnableOneSettingsAuditing = 1` (enable OneSettings auditing)
- `ConfigureTelemetryOptInSettingsUx = 1` (hide diagnostic UI controls)
- `LimitDiagnosticLogCollection = 1` (limit diagnostic logs)
- `LimitDumpCollection = 1` (limit dump collection)

---

## 2. Windows Error Reporting (WER)

WER reports application and system failures to Microsoft.

Registry paths:
```
HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting
HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting
```

### Disable WER

Value name: `Disabled` (REG_DWORD)

| Value | Meaning |
| --- | --- |
| 0 | WER enabled (default) |
| 1 | WER disabled |

Group Policy:
`Computer Configuration > Administrative Templates > Windows Components > Windows Error Reporting > Disable Windows Error Reporting`

### WER sub-settings

```
HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting
```

- `DontSendAdditionalData = 1`
- `DontShowUI = 1`
- `LoggingDisabled = 1`

### LocalDumps

Registry path:
```
HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps
```

| Value | Type | Notes |
| --- | --- | --- |
| DumpFolder | REG_EXPAND_SZ | Dump output location |
| DumpCount | REG_DWORD | Max dumps (default 10) |
| DumpType | REG_DWORD | 0 = custom, 1 = mini, 2 = full |

PowerShell:
```
Get-WindowsErrorReporting
Disable-WindowsErrorReporting
Enable-WindowsErrorReporting
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/win32/wer/wer-settings
- https://learn.microsoft.com/en-us/powershell/module/windowserrorreporting/disable-windowserrorreporting

---

## 3. TDR - GPU Timeout Settings

Timeout Detection and Recovery (TDR) resets the GPU when it stops responding.

Registry path:
```
HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
```

| Value | Type | Default | Recommended (AI/ML) | Notes |
| --- | --- | --- | --- | --- |
| TdrDelay | REG_DWORD | 2 | 60-120 | GPU timeout (seconds) |
| TdrDdiDelay | REG_DWORD | 5 | 60 | DDI timeout (seconds) |
| TdrLimitCount | REG_DWORD | 5 | 5-10 | Count in TdrLimitTime |
| TdrLimitTime | REG_DWORD | 60 | 60-120 | Window (seconds) |

TdrLevel values:

| Value | Meaning |
| --- | --- |
| 0 | TdrLevelOff (not recommended) |
| 1 | TdrLevelBugcheck (BSOD) |
| 2 | TdrLevelRecoverVGA |
| 3 | TdrLevelRecover (default) |

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys
- https://learn.microsoft.com/en-us/windows-hardware/drivers/display/timeout-detection-and-recovery

WARNING: Disabling TDR can destabilize the system. Prefer raising
`TdrDelay` instead of disabling TDR entirely.

---

## 4. Location and Sensor Services

### Location policy

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\LocationAndSensors
```

| Value | Type | Notes |
| --- | --- | --- |
| DisableLocation | REG_DWORD | 1 = disable location |
| DisableLocationScripting | REG_DWORD | 1 = disable location scripting |
| DisableWindowsLocationProvider | REG_DWORD | 1 = disable Windows location provider |

Group Policy:
`Computer Configuration > Administrative Templates > Windows Components > Location and Sensors > Turn off location`

### Sensor services (optional)

- `SensorDataService`
- `SensrSvc`
- `SensorService`

PowerShell:
```
Set-Service -Name "SensorDataService" -StartupType Disabled
Set-Service -Name "SensrSvc" -StartupType Disabled
Set-Service -Name "SensorService" -StartupType Disabled
```

### App location access

Registry path:
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location
```

Values:
- `Allow`
- `Deny`

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy

---

## 5. App Privacy Permissions

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\AppPrivacy
```

Common `LetAppsAccess*` values:

| Value | Meaning |
| --- | --- |
| 0 | User in control |
| 1 | Force allow |
| 2 | Force deny |

Examples:
```
LetAppsAccessCamera = 2
LetAppsAccessMicrophone = 2
LetAppsAccessLocation = 2
LetAppsAccessContacts = 2
LetAppsAccessCalendar = 2
LetAppsAccessEmail = 2
LetAppsAccessCallHistory = 2
LetAppsAccessMessaging = 2
LetAppsAccessNotifications = 2
LetAppsAccessAccountInfo = 2
LetAppsAccessMotion = 2
LetAppsAccessRadios = 2
LetAppsAccessTasks = 2
LetAppsAccessDiagnosticInfo = 2
LetAppsActivateWithVoice = 2
LetAppsAccessBackgroundSpatialPerception = 2
LetAppsAccessGazeInput = 2
LetAppsGetDiagnosticInfo = 2
```

Background apps:
```
LetAppsRunInBackground = 2
```

Group Policy example:
`Computer Configuration > Administrative Templates > Windows Components > App Privacy > Let Windows apps access the camera`

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy

---

## 6. Activity and Sync

### Activity history

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\System
```

| Value | Notes |
| --- | --- |
| EnableActivityFeed | 0 = disable activity feed |
| PublishUserActivities | 0 = disable publish |
| UploadUserActivities | 0 = disable upload |

Group Policy:
`Computer Configuration > Administrative Templates > System > OS Policies > Enable Activity Feed`

### Search history

```
HKLM\Software\Policies\Microsoft\Windows\System
DisableSearchHistory = 1
```

### Sync settings

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\SettingSync
```

| Value | Notes |
| --- | --- |
| DisableSettingSync | 2 = disable all sync |
| DisableSettingSyncUserOverride | 1 = block user override |

Per-setting sync controls:
```
DisableAppSyncSettingSync = 2
DisableApplicationSettingSync = 2
DisableCredentialsSettingSync = 2
DisablePersonalizationSettingSync = 2
DisableDesktopThemeSettingSync = 2
DisableStartLayoutSettingSync = 2
DisableWebBrowserSettingSync = 2
DisableWindowsSettingSync = 2
DisableSyncOnPaidNetwork = 1
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-settingsync

---

## 7. Cross-Device Experiences

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\System
```

| Value | Notes |
| --- | --- |
| EnableCdp | 0 = disable CDP |
| RomeSdkChannelUserAuthzPolicy | 0 = off, 1 = my devices, 2 = everyone nearby |
| EnableMmx | 0 = disable Phone Link |
| IsResumeAllowed | 0 = disable resume |

Group Policy:
`Computer Configuration > Administrative Templates > System > Group Policy > Continue experiences on this device`

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system

---

## 8. Cortana and Speech

### Cortana policy

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\Windows Search
```

| Value | Notes |
| --- | --- |
| AllowCortana | 0 = disable Cortana |
| AllowCortanaAboveLock | 0 = disable on lock screen |
| AllowSearchToUseLocation | 0 = disable location use |
| ConnectedSearchUseWeb | 0 = disable web search |
| DisableWebSearch | 1 = disable web search |

### Speech recognition

```
HKCU\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy
HasAccepted = 0
```

Speech model updates:
```
HKLM\Software\Policies\Microsoft\Speech
AllowSpeechModelUpdate = 0
```

Mixed Reality speech input:
```
HKLM\Software\Policies\Microsoft\Windows\System
DisableSpeechInput = 1
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-speech

---

## 9. Feedback and Suggestions

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\DataCollection
```

| Value | Notes |
| --- | --- |
| DoNotShowFeedbackNotifications | 1 = disable feedback notifications |
| NumberOfSIUFInPeriod | 0 = disable feedback requests |
| PeriodInNanoSeconds | feedback frequency |

Cloud content:
```
HKLM\Software\Policies\Microsoft\Windows\CloudContent
```

| Value | Notes |
| --- | --- |
| DisableThirdPartySuggestions | 1 = disable third-party suggestions |
| DisableWindowsConsumerFeatures | 1 = disable consumer features |
| DisableSoftLanding | 1 = disable soft landing |
| DisableConsumerAccountStateContent | 1 = disable account content |

SubscribedContent values:
```
HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager
SubscribedContent-338389Enabled = 0
SubscribedContent-310093Enabled = 0
SubscribedContent-338393Enabled = 0
SubscribedContent-353694Enabled = 0
SubscribedContent-353696Enabled = 0
SystemPaneSuggestionsEnabled = 0
SilentInstalledAppsEnabled = 0
SoftLandingEnabled = 0
RotatingLockScreenEnabled = 0
RotatingLockScreenOverlayEnabled = 0
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience

---

## 10. Automatic Maintenance

Registry path:
```
HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance
```

| Value | Type | Notes |
| --- | --- | --- |
| MaintenanceDisabled | REG_DWORD | 1 = disable maintenance |
| WakeUp | REG_DWORD | 0 = disable wake for maintenance |

Task examples:
- `\Microsoft\Windows\Diagnosis\Scheduled`
- `\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector`
- `\Microsoft\Windows\Maintenance\WinSAT`

PowerShell:
```
Disable-ScheduledTask -TaskName "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-start-page

---

## 11. Maps and Font Providers

### Offline maps

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\Maps
```

| Value | Notes |
| --- | --- |
| AllowOfflineMapsDownloadOverMeteredConnection | 0 = block metered downloads |
| EnableOfflineMapsAutoUpdate | 0 = no auto update |
| AutoDownloadAndUpdateMapData | 0 = no auto download |
| AllowUntriggeredNetworkTrafficOnSettingsPage | 0 = no background traffic |

### Font providers

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\System
```

| Value | Notes |
| --- | --- |
| EnableFontProviders | 0 = disable font providers |

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-maps

---

## 12. Xbox and Gaming

### Game DVR

Registry path:
```
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR
```

| Value | Notes |
| --- | --- |
| AppCaptureEnabled | 0 = disable capture |
| HistoricalCaptureEnabled | 0 = disable history capture |

### Game Bar

Registry path:
```
HKCU\SOFTWARE\Microsoft\GameBar
```

| Value | Notes |
| --- | --- |
| AllowAutoGameMode | 0 = disable auto game mode |
| AutoGameModeEnabled | 0 = disable game mode |
| UseNexusForGameBarEnabled | 0 = disable Nexus |
| ShowStartupPanel | 0 = hide startup panel |

### Xbox services

- `XblAuthManager`
- `XblGameSave`
- `XboxNetApiSvc`
- `XboxGipSvc`

PowerShell:
```
$xboxServices = @("XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc")
foreach ($service in $xboxServices) {
    Set-Service -Name $service -StartupType Disabled -ErrorAction SilentlyContinue
}
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-gaming

---

## 13. Biometrics

Registry path:
```
HKLM\SOFTWARE\Policies\Microsoft\Biometrics
```

| Value | Type | Notes |
| --- | --- | --- |
| Enabled | REG_DWORD | 0 = disable biometrics |

Credential Provider:
```
HKLM\SOFTWARE\Policies\Microsoft\Biometrics\Credential Provider
Enabled = 0
```

Enhanced anti-spoofing:
```
HKLM\SOFTWARE\Policies\Microsoft\Biometrics\FacialFeatures
EnhancedAntiSpoofing = 1
```

Group Policy:
`Computer Configuration > Administrative Templates > Windows Components > Biometrics > Allow the use of biometrics`

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/hello-identity-verification

---

## 14. Remote Desktop and Assistance

### Remote Assistance

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows NT\Terminal Services
```

| Value | Notes |
| --- | --- |
| fAllowToGetHelp | 0 = disable remote assistance |
| fAllowUnsolicited | 0 = disable unsolicited assistance |

Group Policy:
`Computer Configuration > Administrative Templates > System > Remote Assistance > Configure Offer Remote Assistance`

### Remote Desktop security

```
HKLM\Software\Policies\Microsoft\Windows NT\Terminal Services
fDenyTSConnections = 1
fDisableCdm = 1
fDisableClip = 1
fEncryptRPCTraffic = 1
MinEncryptionLevel = 3
SecurityLayer = 2
UserAuthentication = 1
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/rds-security-best-practices

---

## 15. App Compatibility

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\AppCompat
```

| Value | Notes |
| --- | --- |
| DisableEngine | 1 = disable compatibility engine |
| DisablePCA | 1 = disable Program Compatibility Assistant |
| DisablePcaUI | 1 = disable PCA UI |
| AITEnable | 0 = disable app impact telemetry |
| DisableInventory | 1 = disable inventory collector |

Windows 24H2+ (if present):
```
DisableAPISamping = 1
DisableApplicationFootprint = 1
DisableInstallTracing = 1
DisableWin32AppBackup = 1
```

SwitchBack engine:
```
SbEnable = 0
```

Scheduled tasks:
```
Disable-ScheduledTask -TaskName "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"
Disable-ScheduledTask -TaskName "\Microsoft\Windows\Application Experience\ProgramDataUpdater"
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/deployment/planning/compatibility-faq

---

## 16. File History and Offline Files

### File History

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\FileHistory
```

| Value | Notes |
| --- | --- |
| Disabled | 1 = disable File History |

Group Policy:
`Computer Configuration > Administrative Templates > Windows Components > File History > Turn off File History`

### Offline Files (CSC)

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\NetCache
```

| Value | Notes |
| --- | --- |
| Enabled | 0 = disable offline files |
| BackgroundSyncEnabled | 0 = disable background sync |
| NoReminders | 1 = disable reminders |
| SyncAtLogoff | 0 = no sync at logoff |
| SyncAtLogon | 0 = no sync at logon |

Services:
```
Set-Service -Name "CSC" -StartupType Disabled
Set-Service -Name "CscService" -StartupType Disabled
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows-server/storage/dfs-namespaces/offline-files

---

## 17. Troubleshooting

Registry path:
```
HKLM\Software\Policies\Microsoft\Windows\Troubleshooting\AllowRecommendations
```

| Value | Notes |
| --- | --- |
| 0 | disabled |
| 1 | critical only |
| 2 | all |
| 3 | silent |
| 4 | automatic |
| 5 | all recommendations |

Diagnostic services:
```
Set-Service -Name "DPS" -StartupType Disabled
Set-Service -Name "TroubleshootingSvc" -StartupType Disabled
Set-Service -Name "diagsvc" -StartupType Disabled
```

User preference:
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack\Settings
UserPreference = 1
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-troubleshooting

---

## 18. Crash Dump and Sleep Study

### Crash dump settings

Registry path:
```
HKLM\SYSTEM\CurrentControlSet\Control\CrashControl
```

| Value | Type | Notes |
| --- | --- | --- |
| CrashDumpEnabled | REG_DWORD | dump type |
| FilterPages | REG_DWORD | 1 = filter for active memory dump |
| AlwaysKeepMemoryDump | REG_DWORD | 0 = delete if disk full |
| AutoReboot | REG_DWORD | 1 = auto reboot |
| LogEvent | REG_DWORD | 1 = log event |

CrashDumpEnabled values:

| Value | Meaning |
| --- | --- |
| 0 | None |
| 1 | Complete |
| 2 | Kernel |
| 3 | Small (minidump) |
| 7 | Automatic (default) |

### Sleep Study

Disable ETL channels:
```
wevtutil sl Microsoft-Windows-SleepStudy/Diagnostic /e:false
wevtutil sl Microsoft-Windows-Kernel-Processor-Power/Diagnostic /e:false
```

Disable tasks:
```
Disable-ScheduledTask -TaskName "\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options

---

## 19. Additional Privacy Settings

RSoP logging:
```
HKLM\Software\Policies\Microsoft\Windows\System
RSoPLogging = 0
```

Desktop heap logging:
```
HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\SubSystems
DesktopHeapLogging = 0
```

Message sync:
```
HKLM\Software\Policies\Microsoft\Windows\Messaging
AllowMessageSync = 0
```

Device census tasks:
```
Disable-ScheduledTask -TaskName "\Microsoft\Windows\Device Information\Device"
Disable-ScheduledTask -TaskName "\Microsoft\Windows\Device Information\Device User"
```

MDM enrollment:
```
HKLM\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\MDM
DisableRegistration = 1
AutoEnrollMDM = 0
```

KMS telemetry:
```
HKLM\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform
NoGenTicket = 1
```

Reserved storage:
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager
DisableDeletes = 1
```

PowerShell and .NET telemetry:
```
[System.Environment]::SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", "1", "Machine")
[System.Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1", "Machine")
```

CEIP:
```
HKLM\SOFTWARE\Microsoft\SQMClient\Windows
CEIPEnable = 0

HKLM\SOFTWARE\Policies\Microsoft\SQMClient\Windows
CEIPEnable = 0

HKLM\SOFTWARE\Wow6432Node\Microsoft\VSCommon\15.0\SQM
OptIn = 0
```

Defender telemetry (policy):
```
HKLM\SOFTWARE\Policies\Microsoft\Windows Defender
DisableCoreService1DSTelemetry = 1
DisableCoreServiceECSIntegration = 1
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/privacy/manage-telemetry

---

## 20. UI Privacy

Last signed-in user:
```
HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System
DontDisplayLastUserName = 1
DontDisplayUserName = 1
```

Disable F1 help (rename HelpPane.exe):
```
takeown /f "C:\Windows\HelpPane.exe"
icacls "C:\Windows\HelpPane.exe" /grant administrators:F
Rename-Item "C:\Windows\HelpPane.exe" "C:\Windows\HelpPane.exe.bak"
```

Windows Copilot:
```
HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot
TurnOffWindowsCopilot = 1

HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot
TurnOffWindowsCopilot = 1
```

Windows Recall:
```
HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI
DisableAIDataAnalysis = 1
```

Background apps:
```
HKLM\Software\Policies\Microsoft\Windows\AppPrivacy
LetAppsRunInBackground = 2
```

Camera policy:
```
HKLM\Software\Policies\Microsoft\Camera
AllowCamera = 0

HKLM\Software\Policies\Microsoft\Windows\AppPrivacy
LetAppsAccessCamera = 2
```

Microsoft accounts:
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
NoConnectedUser = 3
```

Microsoft Learn:
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai

---

## Registry Export Example

Combine privacy settings into a single .reg file:

```
Windows Registry Editor Version 5.00

; === TELEMETRY ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection]
"AllowTelemetry"=dword:00000000
"DisableOneSettingsDownloads"=dword:00000001
"DoNotShowFeedbackNotifications"=dword:00000001
"LimitDiagnosticLogCollection"=dword:00000001
"LimitDumpCollection"=dword:00000001

; === WER ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting]
"Disabled"=dword:00000001
"DontSendAdditionalData"=dword:00000001
"LoggingDisabled"=dword:00000001

; === ACTIVITY ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]
"EnableActivityFeed"=dword:00000000
"PublishUserActivities"=dword:00000000
"UploadUserActivities"=dword:00000000
"EnableCdp"=dword:00000000

; === CLOUD CONTENT ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent]
"DisableThirdPartySuggestions"=dword:00000001
"DisableWindowsConsumerFeatures"=dword:00000001

; === CORTANA ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search]
"AllowCortana"=dword:00000000
"DisableWebSearch"=dword:00000001
"ConnectedSearchUseWeb"=dword:00000000

; === LOCATION ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors]
"DisableLocation"=dword:00000001
"DisableLocationScripting"=dword:00000001

; === SYNC ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync]
"DisableSettingSync"=dword:00000002
"DisableSettingSyncUserOverride"=dword:00000001

; === APP PRIVACY ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy]
"LetAppsAccessCamera"=dword:00000002
"LetAppsAccessMicrophone"=dword:00000002
"LetAppsAccessLocation"=dword:00000002
"LetAppsRunInBackground"=dword:00000002

; === COPILOT/RECALL ===
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot]
"TurnOffWindowsCopilot"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI]
"DisableAIDataAnalysis"=dword:00000001
```

---

## Sources

### Official Microsoft Learn (primary)

- Telemetry (Policy CSP System):
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system#allowtelemetry
- Diagnostic data configuration:
  https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization
- WER settings:
  https://learn.microsoft.com/en-us/windows/win32/wer/wer-settings
- Disable Windows Error Reporting (PowerShell):
  https://learn.microsoft.com/en-us/powershell/module/windowserrorreporting/disable-windowserrorreporting
- TDR registry keys:
  https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys
- VBS OEM guidance:
  https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/oem-vbs
- Location and sensors:
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
- App privacy controls:
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
- Settings sync:
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-settingsync
- Cortana and search policies:
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search
- Speech policies:
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-speech
- Troubleshooting policies:
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-troubleshooting
- Windows AI policies:
  https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai
- Windows Firewall CLI:
  https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/configure-with-command-line

### Additional sources (secondary)

- Tom's Hardware
- PC Gamer
- ComputerBase
- Neowin

---

Version:
- Document version: 1.0
- Updated: 2026-01
- Supported OS: Windows 10 21H2+, Windows 11
- Status: Reference documentation

Warning: Back up your system before applying registry changes. Settings can
behave differently across Windows editions and enterprise policies.

<!-- TWEAK INDEX START -->
## Tweak Index (Generated)

This section is generated from `Docs/tweaks/tweak-catalog.csv`.
Do not edit manually.

| ID | Name | Changes | Risk | Source |
| --- | --- | --- | --- | --- |
| <a id="privacy.block-microsoft-accounts"></a> `privacy.block-microsoft-accounts` | Microsoft Accounts on This Device | Windows can let people add Microsoft accounts or sign in with them on the device. This security option controls whether Microsoft account... | Medium | `research/records/privacy.block-microsoft-accounts.json` |
| <a id="privacy.deny-app-access"></a> `privacy.deny-app-access` | Deny App Access (Except Microphone) | Forces Windows apps to be denied access to sensitive capabilities. | Risky | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L162` |
| <a id="privacy.deny-app-access.policy"></a> `privacy.deny-app-access.policy` | App capability access policies | These policies stop Windows apps from accessing sensitive capabilities like camera, contacts, location, and background activity. | Medium | `research/records/privacy.deny-app-access.policy.review.json` |
| <a id="privacy.disable-activity-history"></a> `privacy.disable-activity-history` | Windows Activity History | Windows can keep track of what you did on the device and can upload some of that activity so it can appear across devices. These policy v... | Medium | `research/records/privacy.disable-activity-history.json` |
| <a id="privacy.disable-advertising-id"></a> `privacy.disable-advertising-id` | Disable Advertising ID | Windows uses an advertising ID so apps can personalize experiences across apps. Turning it off stops apps from using that ID for cross-ap... | Medium | `research/records/privacy.disable-advertising-id.json` |
| <a id="privacy.disable-app-diagnostics"></a> `privacy.disable-app-diagnostics` | App Diagnostic Information Access | Some Windows apps can request diagnostic information about other apps. This policy decides whether apps are user-controlled, always allow... | Medium | `research/records/privacy.disable-app-diagnostics.json` |
| <a id="privacy.disable-app-launch-tracking"></a> `privacy.disable-app-launch-tracking` | App Launch Tracking | Windows can track which apps you launch so Start and Search can personalize some results. People turn it off when they want less app-usag... | Medium | `research/records/privacy.disable-app-launch-tracking.review.json` |
| <a id="privacy.disable-app-suggestions"></a> `privacy.disable-app-suggestions` | App Suggestions in Start | Windows can suggest promoted apps and related content in places like the Start menu. People often turn this off when they want a cleaner... | Medium | `research/records/privacy.disable-app-suggestions.review.json` |
| <a id="privacy.disable-appcompat-engine.policy"></a> `privacy.disable-appcompat-engine.policy` | Disable Application Compatibility Engine and SwitchBack | Windows includes an Application Compatibility Engine that checks a database every time an app starts and may apply compatibility fixes. S... | Medium | `research/records/privacy.disable-appcompat-engine.policy.review.json` |
| <a id="privacy.disable-appdeviceinventory.policy"></a> `privacy.disable-appdeviceinventory.policy` | Disable App Device Inventory Policies | Windows 11 24H2 added policies that track which APIs applications use, record install events, capture application footprints, and back up... | Medium | `research/records/privacy.disable-appdeviceinventory.policy.review.json` |
| <a id="privacy.disable-application-compatibility"></a> `privacy.disable-application-compatibility` | Disable Application Compatibility | Turns off Windows application compatibility components, telemetry, and related tasks. | Risky | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L47` |
| <a id="privacy.disable-application-telemetry"></a> `privacy.disable-application-telemetry` | Application Compatibility Telemetry | Windows can collect anonymous telemetry about how applications use certain system components so compatibility systems can learn from it.... | Medium | `research/records/privacy.disable-application-telemetry.json` |
| <a id="privacy.disable-background-apps"></a> `privacy.disable-background-apps` | Background App Execution | Windows apps can keep running in the background to sync, refresh notifications, or finish work. This policy decides whether users control... | Medium | `research/records/privacy.disable-background-apps.json` |
| <a id="privacy.disable-biometrics"></a> `privacy.disable-biometrics` | Windows Biometrics | Windows can use fingerprint, face, or other biometric features for sign-in and related capabilities. This device policy controls whether... | Medium | `research/records/privacy.disable-biometrics.json` |
| <a id="privacy.disable-biometrics-domain-logon"></a> `privacy.disable-biometrics-domain-logon` | Biometric Sign-in for Domain Accounts | Windows can let domain users sign in with biometrics when the device and domain setup support it. This policy controls whether domain acc... | Medium | `research/records/privacy.disable-biometrics-domain-logon.json` |
| <a id="privacy.disable-biometrics-logon"></a> `privacy.disable-biometrics-logon` | Biometric Sign-in | Windows can let users sign in with biometrics like fingerprint or face through the credential provider. This policy controls whether that... | Medium | `research/records/privacy.disable-biometrics-logon.json` |
| <a id="privacy.disable-camera"></a> `privacy.disable-camera` | Camera Device Use | Windows can allow or block the use of camera devices on the machine. This policy decides whether cameras are usable at all. | Medium | `research/records/privacy.disable-camera.json` |
| <a id="privacy.disable-ceip"></a> `privacy.disable-ceip` | Disable CEIP | Opts out of Customer Experience Improvement Program data collection. | Advanced | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L89` |
| <a id="privacy.disable-cli-telemetry"></a> `privacy.disable-cli-telemetry` | PowerShell and .NET CLI Telemetry Opt-Out | PowerShell and the .NET CLI can send telemetry about tool usage. This tweak sets the current user's opt-out environment variables so new... | Medium | `research/records/privacy.disable-cli-telemetry.json` |
| <a id="privacy.disable-consumer-account-content"></a> `privacy.disable-consumer-account-content` | Cloud Consumer Account State Content | Some Windows experiences can use cloud-backed consumer account state content. This policy decides whether those experiences may use that... | Medium | `research/records/privacy.disable-consumer-account-content.json` |
| <a id="privacy.disable-copilot"></a> `privacy.disable-copilot` | Windows Copilot | Windows Copilot is the built-in AI assistant experience in Windows. This policy decides whether that experience is available to the signe... | Medium | `research/records/privacy.disable-copilot.json` |
| <a id="privacy.disable-cross-device-experiences"></a> `privacy.disable-cross-device-experiences` | Cross-Device Sharing | Choose whether nearby Windows experiences stay off, work only with your devices, or are available to everyone nearby. | Advanced | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L222` |
| <a id="privacy.disable-cross-device-experiences.policy"></a> `privacy.disable-cross-device-experiences.policy` | Cross-Device Experiences Machine Policy | Windows can let devices belonging to the same user discover each other and continue some experiences across devices. This policy controls... | Medium | `research/records/privacy.disable-cross-device-experiences.policy.review.json` |
| <a id="privacy.disable-device-name-telemetry"></a> `privacy.disable-device-name-telemetry` | Device Name in Diagnostic Data | Windows diagnostic data can include the computer name. This policy decides whether that name is allowed to be sent with diagnostic data. | Medium | `research/records/privacy.disable-device-name-telemetry.json` |
| <a id="privacy.disable-diagnostic-data-delete"></a> `privacy.disable-diagnostic-data-delete` | Diagnostic Data Deletion | Windows can offer a Settings action that deletes diagnostic data associated with the device. This policy decides whether that delete opti... | Medium | `research/records/privacy.disable-diagnostic-data-delete.json` |
| <a id="privacy.disable-diagnostic-data-viewer"></a> `privacy.disable-diagnostic-data-viewer` | Diagnostic Data Viewer | Windows includes a viewer that can show diagnostic data categories on the device. This policy decides whether that viewer is available. | Medium | `research/records/privacy.disable-diagnostic-data-viewer.json` |
| <a id="privacy.disable-edge-search-suggestions"></a> `privacy.disable-edge-search-suggestions` | Edge Address Bar Suggestions | Microsoft Edge can show web suggestions, local history and favorites suggestions, and legacy address-bar search suggestions as you type.... | Medium | `research/records/privacy.disable-edge-search-suggestions.json` |
| <a id="privacy.disable-f1-help"></a> `privacy.disable-f1-help` | Disable F1 Help | Disables F1 help by renaming HelpPane.exe. | Advanced | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L308` |
| <a id="privacy.disable-feedback-notifications"></a> `privacy.disable-feedback-notifications` | Microsoft Feedback Notifications | Windows can occasionally show prompts asking for feedback. This policy decides whether those feedback questions appear on the device. | Medium | `research/records/privacy.disable-feedback-notifications.json` |
| <a id="privacy.disable-file-history"></a> `privacy.disable-file-history` | File History | File History is Windows' built-in feature for making regular automatic backups of personal files. This policy decides whether File Histor... | Medium | `research/records/privacy.disable-file-history.json` |
| <a id="privacy.disable-find-my-device"></a> `privacy.disable-find-my-device` | Disable Find My Device | Stops Windows from registering this PC with Find My Device and keeps location-based recovery turned off. | Safe | `engine/Tweaks/Commands/Privacy/DisableFindMyDeviceTweak.cs#L18` |
| <a id="privacy.disable-font-providers"></a> `privacy.disable-font-providers` | Online Font Providers | Windows can contact an online font provider to download font catalog data and, when needed, font data for rendering text. This policy dec... | Medium | `research/records/privacy.disable-font-providers.json` |
| <a id="privacy.disable-inking-typing-personalization"></a> `privacy.disable-inking-typing-personalization` | Disable Inking & Typing Personalization | Stops sending inking and typing data to Microsoft. | Advanced | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L148` |
| <a id="privacy.disable-kms-activation-telemetry"></a> `privacy.disable-kms-activation-telemetry` | KMS Client Online AVS Validation | Windows can send KMS client activation-state data to Microsoft during activation. This policy decides whether that data is sent automatic... | Medium | `research/records/privacy.disable-kms-activation-telemetry.json` |
| <a id="privacy.disable-language-list-access"></a> `privacy.disable-language-list-access` | Website Access to Language List | Windows can let websites use your language list to show locally relevant content. This setting controls whether that language-list access... | Medium | `research/records/privacy.disable-language-list-access.review.json` |
| <a id="privacy.disable-local-security-questions"></a> `privacy.disable-local-security-questions` | Security Questions for Local Accounts | Windows can let local-account users create security questions so they can reset their password later. This policy decides whether those s... | Medium | `research/records/privacy.disable-local-security-questions.json` |
| <a id="privacy.disable-location-consent"></a> `privacy.disable-location-consent` | Disable Location Consent (Current User) | This changes the current user's location permission state to deny access for packaged and desktop apps. | Medium | `research/records/privacy.disable-location-consent.review.json` |
| <a id="privacy.disable-location-consent-system"></a> `privacy.disable-location-consent-system` | Location Consent Store (System) | This changes the system-level location permission state to deny access. | Medium | `research/records/privacy.disable-location-consent-system.review.json` |
| <a id="privacy.disable-location-scripting"></a> `privacy.disable-location-scripting` | Location Scripting | Some scripts and apps can access Windows location features through scripting interfaces. This policy decides whether that scripting acces... | Medium | `research/records/privacy.disable-location-scripting.json` |
| <a id="privacy.disable-location-services"></a> `privacy.disable-location-services` | Location Feature | Windows can provide location information to apps and system features. This policy decides whether the location feature is turned off for... | Medium | `research/records/privacy.disable-location-services.json` |
| <a id="privacy.disable-mdm-enrollment"></a> `privacy.disable-mdm-enrollment` | MDM Enrollment | Windows can allow the computer to enroll into a Mobile Device Management service for remote management. This policy decides whether new M... | Medium | `research/records/privacy.disable-mdm-enrollment.json` |
| <a id="privacy.disable-message-sync"></a> `privacy.disable-message-sync` | Message Service Cloud Sync | Windows can back up and restore cellular text messages through Microsoft's cloud services. This policy decides whether that cloud sync is... | Medium | `research/records/privacy.disable-message-sync.json` |
| <a id="privacy.disable-offline-files"></a> `privacy.disable-offline-files` | Disable Offline Files | Disables Offline Files (CSC) via policy, services, tasks, and Sync Center. | Advanced | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L201` |
| <a id="privacy.disable-offline-files.policy"></a> `privacy.disable-offline-files.policy` | Offline Files Feature Policy | Offline Files keeps local copies of network files so they can still be used when the computer is offline. This policy decides whether the... | Medium | `research/records/privacy.disable-offline-files.policy.json` |
| <a id="privacy.disable-onesettings-downloads"></a> `privacy.disable-onesettings-downloads` | OneSettings Downloads | Windows can periodically connect to the OneSettings service to download configuration settings. This policy decides whether those downloa... | Medium | `research/records/privacy.disable-onesettings-downloads.json` |
| <a id="privacy.disable-online-tips"></a> `privacy.disable-online-tips` | Online Tips in Settings | The Settings app can contact Microsoft to retrieve tips and help content. This policy decides whether those online tips are allowed. | Medium | `research/records/privacy.disable-online-tips.json` |
| <a id="privacy.disable-pca-diagnostics.policy"></a> `privacy.disable-pca-diagnostics.policy` | Disable PCA Compatibility Diagnostics Detection | Windows PCA watches for compatibility issues when you run applications and drivers, and can show warnings or suggest fixes. This policy c... | Medium | `research/records/privacy.disable-pca-diagnostics.policy.review.json` |
| <a id="privacy.disable-phone-linking"></a> `privacy.disable-phone-linking` | Phone-PC Linking | Windows can link the PC with a phone so reading, emailing, and similar tasks can continue across devices. This policy decides whether the... | Medium | `research/records/privacy.disable-phone-linking.json` |
| <a id="privacy.disable-program-compatibility-assistant"></a> `privacy.disable-program-compatibility-assistant` | Disable Program Compatibility Assistant | Program Compatibility Assistant monitors applications and can suggest compatibility solutions when Windows detects install failures, runt... | Medium | `research/records/privacy.disable-program-compatibility-assistant.review.json` |
| <a id="privacy.disable-recall"></a> `privacy.disable-recall` | Recall Snapshot Saving | Recall can save snapshots of what you have seen on the screen so you can search and revisit it later. This policy decides whether those s... | Medium | `research/records/privacy.disable-recall.json` |
| <a id="privacy.disable-resume"></a> `privacy.disable-resume` | Disable Resume for Current User | Resume lets you start on one device and continue on this PC. This tweak turns off that current-user Resume setting. | Medium | `research/records/privacy.disable-resume.json` |
| <a id="privacy.disable-rsop-logging"></a> `privacy.disable-rsop-logging` | Resultant Set of Policy Logging | RSoP logging records which Group Policy settings were applied to the PC. Some people turn it off to reduce policy logging, while administ... | Medium | `research/records/privacy.disable-rsop-logging.json` |
| <a id="privacy.disable-search-box-suggestions"></a> `privacy.disable-search-box-suggestions` | File Explorer Search Box Suggestions | File Explorer can show suggestion pop-ups as you type into its search box, based on past search entries. This policy decides whether thos... | Medium | `research/records/privacy.disable-search-box-suggestions.json` |
| <a id="privacy.disable-search-history"></a> `privacy.disable-search-history` | Search History Storage and Display | Windows can remember previous search queries and use them to suggest searches later. This policy decides whether that search history is s... | Medium | `research/records/privacy.disable-search-history.json` |
| <a id="privacy.disable-sensors"></a> `privacy.disable-sensors` | Windows Sensors | Windows can use hardware sensors such as orientation, ambient light, motion, or other environmental sensors. This policy decides whether... | Medium | `research/records/privacy.disable-sensors.json` |
| <a id="privacy.disable-sleep-study-diagnostics"></a> `privacy.disable-sleep-study-diagnostics` | Disable Sleep Study Diagnostics | Disables sleep study diagnostic event channels. | Advanced | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L102` |
| <a id="privacy.disable-steps-recorder"></a> `privacy.disable-steps-recorder` | Steps Recorder | Steps Recorder can capture user actions and screenshots to help explain a problem. This policy decides whether the feature is available. | Medium | `research/records/privacy.disable-steps-recorder.json` |
| <a id="privacy.disable-suggestions"></a> `privacy.disable-suggestions` | Disable Suggestions & Tips | Turns off Windows tips, welcome experiences, and Settings recommendations. | Safe | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L288` |
| <a id="privacy.disable-suggestions-cdm"></a> `privacy.disable-suggestions-cdm` | Disable Content Delivery Manager Suggestions | Disables various suggestions and auto-installed apps from the Content Delivery Manager. | Safe | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L131` |
| <a id="privacy.disable-suggestions.policy"></a> `privacy.disable-suggestions.policy` | Windows Suggestion Surfaces Policy Group | Windows uses Spotlight-like suggestion surfaces in Start, Settings, and the Windows Welcome experience. These policies control those offi... | Medium | `research/records/privacy.disable-suggestions.policy.review.json` |
| <a id="privacy.disable-switchback.policy"></a> `privacy.disable-switchback.policy` | Disable SwitchBack Compatibility Policy | SwitchBack is a Windows compatibility feature that lets newer applications receive compatibility fixes originally designed for older OS v... | Medium | `research/records/privacy.disable-switchback.policy.review.json` |
| <a id="privacy.disable-telemetry-change-notifications"></a> `privacy.disable-telemetry-change-notifications` | Diagnostic Data Change Notifications | Windows can notify the user when the diagnostic-data opt-in setting changes. This policy decides whether those notifications are shown. | Medium | `research/records/privacy.disable-telemetry-change-notifications.json` |
| <a id="privacy.disable-telemetry-optin-ui"></a> `privacy.disable-telemetry-optin-ui` | Diagnostic Data Opt-In Settings UI | Windows can show controls in Settings that let a user change diagnostic data choices. This policy decides whether that Settings UI stays... | Medium | `research/records/privacy.disable-telemetry-optin-ui.json` |
| <a id="privacy.disable-wer"></a> `privacy.disable-wer` | Windows Error Reporting | Windows Error Reporting can send information about crashes and failures so Microsoft or an internal server can analyze them. This policy... | Medium | `research/records/privacy.disable-wer.json` |
| <a id="privacy.disable-windows-location-provider"></a> `privacy.disable-windows-location-provider` | Windows Location Provider | Windows has a built-in location provider that apps and system features can use. This policy decides whether that provider is available. | Medium | `research/records/privacy.disable-windows-location-provider.json` |
| <a id="privacy.disable-windows-tips"></a> `privacy.disable-windows-tips` | Turn Off Windows Tips | Windows can show popups and onboarding tips that explain features and suggest ways to use the system. People often turn them off when the... | Medium | `research/records/privacy.disable-windows-tips.review.json` |
| <a id="privacy.disable-wmplayer-telemetry"></a> `privacy.disable-wmplayer-telemetry` | Disable Windows Media Player Telemetry | Turns off usage tracking and online metadata for Windows Media Player. | Advanced | `app/Services/TweakProviders/PrivacyTweakProvider.cs#L266` |
| <a id="privacy.hide-last-logged-in-user"></a> `privacy.hide-last-logged-in-user` | Display of the Last Signed-In Username | Windows can show the name of the last person who signed in on the sign-in screen. This setting decides whether that username is shown or... | Medium | `research/records/privacy.hide-last-logged-in-user.json` |
| <a id="privacy.hide-recommended-personalized-sites"></a> `privacy.hide-recommended-personalized-sites` | Start Personalized Site Recommendations | Windows 11 Start can surface personalized website suggestions. This device policy hides those recommendations for everyone on the machine. | Medium | `research/records/privacy.hide-recommended-personalized-sites.json` |
| <a id="privacy.hide-recommended-personalized-sites-user"></a> `privacy.hide-recommended-personalized-sites-user` | Start Personalized Site Recommendations (Current User) | Windows 11 Start can surface personalized website suggestions. This user policy hides those recommendations just for the current account. | Medium | `research/records/privacy.hide-recommended-personalized-sites-user.json` |
| <a id="privacy.hide-recommended-section"></a> `privacy.hide-recommended-section` | Start Menu Recommended Section | Windows 11 Start can show a Recommended section with suggested apps and files. This policy lets the device hide that section for everyone. | Medium | `research/records/privacy.hide-recommended-section.json` |
| <a id="privacy.hide-recommended-section-user"></a> `privacy.hide-recommended-section-user` | Start Menu Recommended Section (Current User) | Windows 11 Start can show a Recommended section with suggested apps and files. This user policy hides that section just for the current a... | Medium | `research/records/privacy.hide-recommended-section-user.json` |
| <a id="privacy.hide-username-at-signin"></a> `privacy.hide-username-at-signin` | Display of Username During Sign-In | After someone enters their credentials, Windows can keep showing the username or hide it. This setting decides whether the username stays... | Medium | `research/records/privacy.hide-username-at-signin.json` |
| <a id="privacy.limit-diagnostic-log-collection"></a> `privacy.limit-diagnostic-log-collection` | Advanced Diagnostic Log Collection | Windows can collect extra diagnostic logs when advanced diagnostics are involved. This policy decides whether that additional log collect... | Medium | `research/records/privacy.limit-diagnostic-log-collection.json` |
| <a id="privacy.limit-dump-collection"></a> `privacy.limit-dump-collection` | Diagnostic Dump Collection | Windows can collect diagnostic dump data for troubleshooting. This policy decides whether that dump collection is limited. | Medium | `research/records/privacy.limit-dump-collection.json` |
| <a id="privacy.set-diagnostic-data-to-minimum-supported-level"></a> `privacy.set-diagnostic-data-to-minimum-supported-level` | Set Diagnostic Data to Minimum Supported Level | Windows can send different amounts of diagnostic data. This policy sets how much data Windows is allowed to collect and send. | Medium | `research/records/privacy.set-diagnostic-data-to-minimum-supported-level.review.json` |
| <a id="privacy.troubleshooter-dont-run"></a> `privacy.troubleshooter-dont-run` | Recommended Troubleshooting for Known Problems | Windows can automatically apply or suggest troubleshooting for known problems. This policy controls how much of that automation is allowed. | Medium | `research/records/privacy.troubleshooter-dont-run.review.json` |
| <a id="privacy.turn-off-sync-by-default-allow-user-override"></a> `privacy.turn-off-sync-by-default-allow-user-override` | Turn Off Settings Sync by Default | Windows can sync settings like passwords, personalization, app settings, browser settings, and Start layout across devices. These policie... | Medium | `research/records/privacy.turn-off-sync-by-default-allow-user-override.review.json` |
<!-- TWEAK INDEX END -->
