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
| <a id="privacy.block-microsoft-accounts"></a> `privacy.block-microsoft-accounts` | Block Microsoft Accounts | Prevents adding or signing in with Microsoft accounts. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L805` |
| <a id="privacy.deny-app-access"></a> `privacy.deny-app-access` | Deny App Access (Except Microphone) | Forces Windows apps to be denied access to sensitive capabilities. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L422` |
| <a id="privacy.disable-activity-history"></a> `privacy.disable-activity-history` | Disable Activity History | Stops publishing and uploading activity history (Timeline) across devices. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L46` |
| <a id="privacy.disable-advertising-id"></a> `privacy.disable-advertising-id` | Disable Advertising ID | Prevents Windows from tracking you for advertising personalization. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L34` |
| <a id="privacy.disable-app-diagnostics"></a> `privacy.disable-app-diagnostics` | Disable App Diagnostics | Prevents apps from accessing diagnostic information. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L458` |
| <a id="privacy.disable-app-launch-tracking"></a> `privacy.disable-app-launch-tracking` | Disable App Launch Tracking | Stops Windows from tracking app launches for Start/Search personalization. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1068` |
| <a id="privacy.disable-app-suggestions"></a> `privacy.disable-app-suggestions` | Disable App Suggestions in Start | Prevents Windows from suggesting promoted apps in the Start menu. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L324` |
| <a id="privacy.disable-application-compatibility"></a> `privacy.disable-application-compatibility` | Disable Application Compatibility | Turns off Windows application compatibility components, telemetry, and related tasks. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L73` |
| <a id="privacy.disable-application-telemetry"></a> `privacy.disable-application-telemetry` | Disable Application Telemetry | Stops the Application Telemetry engine from collecting usage data for compatibility. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L61` |
| <a id="privacy.disable-background-apps"></a> `privacy.disable-background-apps` | Disable Background Apps | Prevents Windows apps from running in the background, saving battery and resources. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L494` |
| <a id="privacy.disable-biometrics"></a> `privacy.disable-biometrics` | Disable Biometrics | Turns off Windows biometric features on this device. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1081` |
| <a id="privacy.disable-biometrics-domain-logon"></a> `privacy.disable-biometrics-domain-logon` | Disable Biometrics for Domain Logon | Prevents domain users from signing in with biometrics. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1105` |
| <a id="privacy.disable-biometrics-logon"></a> `privacy.disable-biometrics-logon` | Disable Biometrics Logon | Prevents users from signing in with biometrics. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1093` |
| <a id="privacy.disable-camera"></a> `privacy.disable-camera` | Disable Camera Access (Policy) | Disables camera access for all applications via group policy. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L482` |
| <a id="privacy.disable-ceip"></a> `privacy.disable-ceip` | Disable CEIP | Opts out of Customer Experience Improvement Program data collection. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L139` |
| <a id="privacy.disable-cli-telemetry"></a> `privacy.disable-cli-telemetry` | Disable PowerShell & .NET CLI Telemetry | Opts out of PowerShell and .NET CLI telemetry for the current user. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L724` |
| <a id="privacy.disable-consumer-account-content"></a> `privacy.disable-consumer-account-content` | Disable Consumer Account State Content | Prevents Windows experiences from using cloud consumer account content. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L947` |
| <a id="privacy.disable-copilot"></a> `privacy.disable-copilot` | Disable Windows Copilot | Turns off the Windows Copilot AI experience for the current user. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L395` |
| <a id="privacy.disable-cross-device-experiences"></a> `privacy.disable-cross-device-experiences` | Cross-Device Sharing | Choose whether nearby Windows experiences stay off, work only with your devices, or are available to everyone nearby. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L619` |
| <a id="privacy.disable-device-name-telemetry"></a> `privacy.disable-device-name-telemetry` | Disable Device Name in Diagnostics | Prevents the device name from being included in diagnostic data. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L152` |
| <a id="privacy.disable-diagnostic-data"></a> `privacy.disable-diagnostic-data` | Set Diagnostic Data to Minimum Supported Level | Sets diagnostic data collection to the lowest documented level supported by the edition. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L22` |
| <a id="privacy.disable-diagnostic-data-delete"></a> `privacy.disable-diagnostic-data-delete` | Disable Diagnostic Data Deletion | Disables the ability to delete diagnostic data in Settings. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L176` |
| <a id="privacy.disable-diagnostic-data-viewer"></a> `privacy.disable-diagnostic-data-viewer` | Disable Diagnostic Data Viewer | Blocks access to the Diagnostic Data Viewer in Settings. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L164` |
| <a id="privacy.disable-edge-search-suggestions"></a> `privacy.disable-edge-search-suggestions` | Disable Edge Search Suggestions | Turns off search suggestions in Microsoft Edge address bar. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L982` |
| <a id="privacy.disable-f1-help"></a> `privacy.disable-f1-help` | Disable F1 Help | Disables F1 help by renaming HelpPane.exe. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L973` |
| <a id="privacy.disable-feedback-notifications"></a> `privacy.disable-feedback-notifications` | Disable Feedback Notifications | Stops Windows Feedback prompts from appearing. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L546` |
| <a id="privacy.disable-file-history"></a> `privacy.disable-file-history` | Disable File History | Turns off File History backups. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L585` |
| <a id="privacy.disable-find-my-device"></a> `privacy.disable-find-my-device` | Disable Find My Device | Stops Windows from registering this PC with Find My Device and keeps location-based recovery turned off. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L663` |
| <a id="privacy.disable-font-providers"></a> `privacy.disable-font-providers` | Disable Font Providers | Prevents Windows from downloading fonts from online providers. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L370` |
| <a id="privacy.disable-inking-typing-personalization"></a> `privacy.disable-inking-typing-personalization` | Disable Inking & Typing Personalization | Stops sending inking and typing data to Microsoft. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L382` |
| <a id="privacy.disable-kms-activation-telemetry"></a> `privacy.disable-kms-activation-telemetry` | Disable KMS Activation Telemetry | Stops KMS client activation data from being sent to Microsoft. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L248` |
| <a id="privacy.disable-language-list-access"></a> `privacy.disable-language-list-access` | Disable Website Access to Language List | Prevents websites from accessing the language list for content customization. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L739` |
| <a id="privacy.disable-local-security-questions"></a> `privacy.disable-local-security-questions` | Disable Local Security Questions | Prevents setting security questions for local accounts. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L817` |
| <a id="privacy.disable-location-consent"></a> `privacy.disable-location-consent` | Disable Location Consent (User) | Denies location capability access for the current user. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L995` |
| <a id="privacy.disable-location-consent-system"></a> `privacy.disable-location-consent-system` | Disable Location Consent (System) | Denies location capability access at the system level. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1008` |
| <a id="privacy.disable-location-scripting"></a> `privacy.disable-location-scripting` | Disable Location Scripting | Disables location scripting support for apps and scripts. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1020` |
| <a id="privacy.disable-location-services"></a> `privacy.disable-location-services` | Disable Location Services | Turns off location tracking services system-wide for all users. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L470` |
| <a id="privacy.disable-mdm-enrollment"></a> `privacy.disable-mdm-enrollment` | Disable MDM Enrollment | Prevents new MDM enrollments for this device. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L712` |
| <a id="privacy.disable-message-sync"></a> `privacy.disable-message-sync` | Disable Message Sync | Stops SMS/MMS cloud sync for this device. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L700` |
| <a id="privacy.disable-offline-files"></a> `privacy.disable-offline-files` | Disable Offline Files | Disables Offline Files (CSC) via policy, services, tasks, and Sync Center. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L598` |
| <a id="privacy.disable-onesettings-downloads"></a> `privacy.disable-onesettings-downloads` | Disable OneSettings Downloads | Stops Windows from downloading configuration settings from OneSettings. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L236` |
| <a id="privacy.disable-online-tips"></a> `privacy.disable-online-tips` | Disable Online Tips | Stops Settings from retrieving online tips and help content. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L959` |
| <a id="privacy.disable-pca-diagnostics.policy"></a> `privacy.disable-pca-diagnostics.policy` | Disable PCA Diagnostics Detection | Turns off Program Compatibility Assistant compatibility-issue detection through the documented policy. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L115` |
| <a id="privacy.disable-phone-linking"></a> `privacy.disable-phone-linking` | Disable Phone Linking | Prevents the device from participating in Phone-PC linking. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L675` |
| <a id="privacy.disable-recall"></a> `privacy.disable-recall` | Disable Windows Recall | Disables saving snapshots for the Recall AI feature on this user. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L408` |
| <a id="privacy.disable-resume"></a> `privacy.disable-resume` | Disable Resume Experiences | Turns off Resume (start on one device, continue on this PC). | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L687` |
| <a id="privacy.disable-rsop-logging"></a> `privacy.disable-rsop-logging` | Disable RSoP Logging | Turns off Resultant Set of Policy logging on this device. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L260` |
| <a id="privacy.disable-search-box-suggestions"></a> `privacy.disable-search-box-suggestions` | Disable Search Box Suggestions | Stops File Explorer from showing recent search suggestions. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L866` |
| <a id="privacy.disable-search-history"></a> `privacy.disable-search-history` | Disable Search History | Prevents search history from being stored for this user. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L853` |
| <a id="privacy.disable-sensors"></a> `privacy.disable-sensors` | Disable Sensors | Turns off hardware sensor access for apps and system features. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1044` |
| <a id="privacy.disable-sleep-study-diagnostics"></a> `privacy.disable-sleep-study-diagnostics` | Disable Sleep Study Diagnostics | Disables sleep study diagnostic event channels. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L271` |
| <a id="privacy.disable-steps-recorder"></a> `privacy.disable-steps-recorder` | Disable Steps Recorder | Disables Steps Recorder through policy to prevent recording user actions. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1056` |
| <a id="privacy.disable-suggestions"></a> `privacy.disable-suggestions` | Disable Suggestions & Tips | Turns off Windows tips, welcome experiences, and Settings recommendations. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L929` |
| <a id="privacy.disable-suggestions-cdm"></a> `privacy.disable-suggestions-cdm` | Disable Content Delivery Manager Suggestions | Disables various suggestions and auto-installed apps from the Content Delivery Manager. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L337` |
| <a id="privacy.disable-suggestions.policy"></a> `privacy.disable-suggestions.policy` | Disable Suggestion Surfaces (Policy) | Turns off the documented CloudContent suggestion surfaces in Start, Settings, and the Windows Welcome experience. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L354` |
| <a id="privacy.disable-sync-settings"></a> `privacy.disable-sync-settings` | Turn Off Settings Sync by Default | Turns off syncing Windows settings and related data across devices while still allowing user override. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L774` |
| <a id="privacy.disable-telemetry-change-notifications"></a> `privacy.disable-telemetry-change-notifications` | Disable Diagnostic Data Change Notifications | Stops opt-in change notifications for diagnostic data. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L188` |
| <a id="privacy.disable-telemetry-optin-ui"></a> `privacy.disable-telemetry-optin-ui` | Disable Diagnostic Data Opt-in UI | Disables the diagnostic data opt-in settings UI in Settings. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L200` |
| <a id="privacy.disable-wer"></a> `privacy.disable-wer` | Disable Windows Error Reporting | Disables automatic generation and upload of error reports to Microsoft. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L127` |
| <a id="privacy.disable-windows-location-provider"></a> `privacy.disable-windows-location-provider` | Disable Windows Location Provider | Disables the Windows Location Provider for all apps. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L1032` |
| <a id="privacy.disable-windows-tips"></a> `privacy.disable-windows-tips` | Turn Off Windows Tips | Turns off Windows tips through the documented CloudContent machine policy. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L312` |
| <a id="privacy.disable-wmplayer-telemetry"></a> `privacy.disable-wmplayer-telemetry` | Disable Windows Media Player Telemetry | Turns off usage tracking and online metadata for Windows Media Player. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L752` |
| <a id="privacy.hide-last-logged-in-user"></a> `privacy.hide-last-logged-in-user` | Hide Last Logged-In User | Removes the last signed-in username from the sign-in screen. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L829` |
| <a id="privacy.hide-recommended-personalized-sites"></a> `privacy.hide-recommended-personalized-sites` | Hide Start Personalized Site Recommendations (Policy) | Removes personalized website recommendations from Start for all users. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L904` |
| <a id="privacy.hide-recommended-personalized-sites-user"></a> `privacy.hide-recommended-personalized-sites-user` | Hide Start Personalized Site Recommendations (User) | Removes personalized website recommendations from Start for the current user. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L916` |
| <a id="privacy.hide-recommended-section"></a> `privacy.hide-recommended-section` | Hide Start Recommended Section (Policy) | Removes the Recommended section from the Start menu for all users. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L879` |
| <a id="privacy.hide-recommended-section-user"></a> `privacy.hide-recommended-section-user` | Hide Start Recommended Section (User) | Removes the Recommended section from the Start menu for the current user. | Safe | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L891` |
| <a id="privacy.hide-username-at-signin"></a> `privacy.hide-username-at-signin` | Hide Username at Sign-In | Hides the username after credentials are entered at sign-in. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L841` |
| <a id="privacy.limit-diagnostic-log-collection"></a> `privacy.limit-diagnostic-log-collection` | Limit Diagnostic Log Collection | Prevents additional diagnostic logs from being collected. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L212` |
| <a id="privacy.limit-dump-collection"></a> `privacy.limit-dump-collection` | Limit Dump Collection | Limits diagnostic dumps to reduce the data sent in diagnostics. | Risky | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L224` |
| <a id="privacy.troubleshooter-dont-run"></a> `privacy.troubleshooter-dont-run` | Troubleshooter: Don't Run Any | Prevents recommended troubleshooters from running automatically. | Advanced | `OpenTraceProject.App\Services\TweakProviders\PrivacyTweakProvider.cs#L299` |
<!-- TWEAK INDEX END -->
