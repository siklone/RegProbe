# Windows Visibility and Personalization Guide
## Use-case based settings (English)

This guide maps common visibility and personalization preferences to registry-backed settings.
It is a companion to the main docs and is intentionally separated to preserve source provenance.

Related docs:
- [Visibility tweaks](visibility.md)
- [Tweak catalog](../tweaks/tweak-catalog.html)
- [Tweak details](../tweaks/tweak-details.html)

---

## Table of Contents

1. [Minimal / Clean Desktop](#1-minimal-clean-desktop)
2. [Productivity / Work Focused](#2-productivity-work-focused)
3. [Content Creator / Video Editor](#3-content-creator-video-editor)
4. [Gaming Setup](#4-gaming-setup)
5. [Developer / Programmer](#5-developer-programmer)
6. [Privacy Focused](#6-privacy-focused)
7. [Low-End / Performance Focused](#7-low-end-performance-focused)
8. [Streaming / OBS](#8-streaming-obs)
9. [Presentation / Presentation Mode](#9-presentation-presentation-mode)

---

## 1. Minimal / Clean Desktop

Goal: a clean, low-distraction desktop.

### Taskbar
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
```

| Value | Recommended | Notes |
| --- | --- | --- |
| TaskbarAl | 0 | Taskbar left-aligned (classic) |
| ShowTaskViewButton | 0 | Hide Task View button |
| TaskbarBadges | 0 | Disable app badges |
| TaskbarFlashing | 0 | Disable flashing alerts |
| TaskbarSd | 0 | Hide "Show Desktop" corner |

```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Search
SearchboxTaskbarMode = 0 (Hide search box)
```

### Desktop Icon Spacing
```
Path: HKCU\Control Panel\Desktop\WindowMetrics
IconSpacing = -1125
IconVerticalSpacing = -1500
```

Spacing formula: `-15 * pixel_value`
- 75px = -1125 (default)
- 100px = -1500
- 120px = -1800

### Remove Shortcut Arrows
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons
29 = C:\Windows\Blank.ico
```

Remove the " - Shortcut" suffix:
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer
link = REG_BINARY: 00 00 00 00
```

### Classic Context Menu (Windows 11)
```
Path: HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32
(Default) = ""
```

Why classic:
- Faster
- No extra "Show more options" step

### Start Menu
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
Start_TrackDocs = 0
Start_TrackProgs = 0
LaunchTo = 1
```

```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer
HubMode = 1 (Hide Home)
```

### Theme (Clean)
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
AppsUseLightTheme = 0
SystemUsesLightTheme = 0
EnableTransparency = 0
```

---

## 2. Productivity / Work Focused

Goal: fast access, minimal distractions.

### Explorer
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
```

| Value | Recommended | Notes |
| --- | --- | --- |
| HideFileExt | 0 | Show file extensions |
| Hidden | 1 | Show hidden files |
| ShowSuperHidden | 1 | Show system files |
| LaunchTo | 1 | Open to This PC |
| NavPaneShowAllFolders | 1 | Show all folders |

### Remove Quick Access
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer
HubMode = 1
```

```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer
HubMode = 1
```

### Remove Home and Gallery
```
Path: HKCU\Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}
System.IsPinnedToNameSpaceTree = 0
```

### Detailed File Transfer UI
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager
EnthusiastMode = 1
```

### Alt-Tab Tabs Filter
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
MultiTaskingAltTabFilter = 4
```

| Value | Result |
| --- | --- |
| 1 | 20 recent tabs |
| 2 | 5 recent tabs |
| 3 | 3 recent tabs |
| 4 | Windows only (recommended) |

### Notifications (Work hours)
```
Path: HKCU\Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications
NoToastApplicationNotification = 1
NoTileApplicationNotification = 1
```

### Faster Shutdown
```
Path: HKCU\Control Panel\Desktop
WaitToKillAppTimeout = "2000"
HungAppTimeout = "1000"
AutoEndTasks = "1"
```

```
Path: HKLM\SYSTEM\CurrentControlSet\Control
WaitToKillServiceTimeout = "2000"
```

---

## 3. Content Creator / Video Editor

Goal: smooth file navigation and consistent previews.

### Disable Audio/Video Preview Handlers
```
Path: HKCR\.mp4\ShellEx\{E357FCCD-A995-4576-B01F-234630154E96}
(Default) = ""
```

Common extensions:
```
3gp, aac, avi, flac, m4a, m4v, mkv, mod, mov, mp3, mp4,
mpeg, mpg, ogg, ts, vob, wav, webm, wma, wmv
```

### Wallpaper Quality
```
Path: HKCU\Control Panel\Desktop
JPEGImportQuality = 100
```

### Disable Folder Type Discovery
```
Path: HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell
Bags = (delete)
BagMRU = (delete)
```

Then:
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
FolderType = NotSpecified
```

### Large Icons or Details View
```
Path: HKCU\Software\Microsoft\Windows\Shell\Bags\1\Desktop
IconSize = 96
```

| Value | Size |
| --- | --- |
| 32 | Small |
| 48 | Medium |
| 96 | Large |
| 256 | Extra large |

### Open Explorer to This PC
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
LaunchTo = 1
```

---

## 4. Gaming Setup

Goal: performance and minimal distractions.

### Taskbar Auto-Hide
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3
Settings = (binary, bit 3 = auto-hide)
```

### Disable Notifications
```
Path: HKCU\Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications
NoToastApplicationNotification = 1
NoToastApplicationNotificationOnLockScreen = 1
DisallowNotificationMirroring = 1
```

### Disable Aero Shake
```
Path: HKCU\Software\Policies\Microsoft\Windows\Explorer
NoWindowMinimizingShortcuts = 1
```

### Disable Animations
```
Path: HKCU\Control Panel\Desktop
MinAnimate = "0"
```

```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
TaskbarAnimations = 0
```

```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DWM
DisallowAnimations = 1
```

### Dark Theme
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
AppsUseLightTheme = 0
SystemUsesLightTheme = 0
```

### Disable Lock Screen
```
Path: HKLM\Software\Policies\Microsoft\Windows\Personalization
NoLockScreen = 1
```

---

## 5. Developer / Programmer

Goal: terminal-first, visibility, fast access.

### PowerShell Console Colors
```
Path: HKCU\Console\%SystemRoot%_System32_WindowsPowerShell_v1.0_powershell.exe
ScreenColors = 0x07
```

Color bits:
- Bits 0-3: foreground
- Bits 4-7: background

### Show File Extensions and Hidden Items
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
HideFileExt = 0
Hidden = 1
ShowSuperHidden = 1
```

### Enable Long Paths
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\FileSystem
LongPathsEnabled = 1
```

### Add New File Types to Context Menu
```
Path: HKCR\.ps1\ShellNew
NullFile = ""
```

```
Path: HKCR\.bat\ShellNew
NullFile = ""
```

```
Path: HKCR\.sh\ShellNew
NullFile = ""
```

### Show Seconds in Taskbar Clock
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
ShowSecondsInSystemClock = 1
```

### Classic Context Menu
```
Path: HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32
(Default) = ""
```

### Verbose Boot Messages
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
VerboseStatus = 1
```

### BSOD Details
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\CrashControl
DisplayParameters = 1
AutoReboot = 0
```

---

## 6. Privacy Focused

Goal: minimize tracking and data sharing.

### Disable Spotlight
```
Path: HKCU\Software\Policies\Microsoft\Windows\CloudContent
DisableWindowsSpotlightFeatures = 1
DisableWindowsSpotlightOnActionCenter = 1
DisableWindowsSpotlightOnSettings = 1
DisableSpotlightCollectionOnDesktop = 1
DisableThirdPartySuggestions = 1
DisableWindowsSpotlightWindowsWelcomeExperience = 1
```

### Disable Lock Screen Content
```
Path: HKLM\Software\Policies\Microsoft\Windows\Personalization
NoLockScreen = 1
```

Or:
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager
RotatingLockScreenOverlayEnabled = 0
SubscribedContent-338387Enabled = 0
```

### Disable Clipboard History and Sync
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\System
AllowClipboardHistory = 0
AllowCrossDeviceClipboard = 0
```

### Disable Recent Files
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
Start_TrackDocs = 0
Start_TrackProgs = 0
```

### Disable User Tracking
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
NoInstrumentation = 1
NoRecentDocsHistory = 1
```

### Hide Email on Login Screen
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\SystemProtectedUserData\{SID}\AnyoneRead\Logon
ShowEmail = 0
```

### Hide Settings Pages
```
Path: HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
SettingsPageVisibility = "hide:sync;findmydevice;maps;mobile-devices;family-group"
```

---

## 7. Low-End / Performance Focused

Goal: reduce resource usage and UI overhead.

### Disable Animations
```
Path: HKCU\Control Panel\Desktop
UserPreferencesMask = (binary)
MinAnimate = "0"
```

```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
TaskbarAnimations = 0
ListviewAlphaSelect = 0
ListviewShadow = 0
```

```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects
VisualFXSetting = 2
```

### Disable Transparency
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
EnableTransparency = 0
```

### Disable Shadows
```
Path: HKLM\SOFTWARE\Microsoft\Windows\Dwm
DisableProjectedShadows = 1
```

### Disable Thumbnail Cache (HDD)
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
DisableThumbnailCache = 1
```

### Disable Wallpaper Slideshow
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Lock Screen
SlideshowEnabled = 0
```

### Reduce Search Indexing
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search
PreventIndexingLowDiskSpaceMB = 1
```

### Disable Startup Delay
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize
StartupDelayInMSec = 0
```

---

## 8. Streaming / OBS

Goal: clean capture, no popups.

### Disable Notifications
```
Path: HKCU\Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications
NoToastApplicationNotification = 1
```

### Clean Taskbar
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
TaskbarBadges = 0
TaskbarFlashing = 0
ShowTaskViewButton = 0
```

```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Search
SearchboxTaskbarMode = 0
```

### Disable Widgets
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Dsh
AllowNewsAndInterests = 0
```

---

## 9. Presentation / Presentation Mode

Goal: no distractions during presentations.

### Disable Notifications
```
Path: HKCU\Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications
NoToastApplicationNotification = 1
NoToastApplicationNotificationOnLockScreen = 1
```

### Disable Lock Screen
```
Path: HKLM\Software\Policies\Microsoft\Windows\Personalization
NoLockScreen = 1
```

### Disable Screen Saver
```
Path: HKCU\Control Panel\Desktop
ScreenSaveActive = "0"
```

### Increase Text Size
```
Path: HKCU\Software\Microsoft\Accessibility
TextScaleFactor = 125
```

### Legal Notice (Optional)
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
legalnoticecaption = "Company Name"
legalnoticetext = "Confidential - Authorized Use Only"
```

---

## Quick Reference Matrix

| Scenario | Animations | Transparency | Notifications | Spotlight | Thumbnails | Classic Menu |
| --- | --- | --- | --- | --- | --- | --- |
| Minimal | OFF | OFF | OFF | OFF | ON | YES |
| Productivity | OFF | ON | Limited | OFF | ON | YES |
| Content Creator | ON | ON | ON | OFF | OFF | Optional |
| Gaming | OFF | OFF | OFF | OFF | ON | Optional |
| Developer | OFF | ON | ON | OFF | ON | YES |
| Privacy | OFF | OFF | OFF | OFF | ON | YES |
| Low-End | OFF | OFF | OFF | OFF | OFF | YES |
| Streaming | ON | ON | OFF | OFF | ON | No |
| Presentation | OFF | ON | OFF | OFF | ON | No |

---

## Example Registry Scripts

### Minimal Desktop
```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
"TaskbarAl"=dword:00000000
"ShowTaskViewButton"=dword:00000000
"TaskbarBadges"=dword:00000000
"TaskbarFlashing"=dword:00000000
"Start_TrackDocs"=dword:00000000
"LaunchTo"=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search]
"SearchboxTaskbarMode"=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize]
"AppsUseLightTheme"=dword:00000000
"SystemUsesLightTheme"=dword:00000000
"EnableTransparency"=dword:00000000

[HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32]
@=""
```

### Developer Setup
```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
"HideFileExt"=dword:00000000
"Hidden"=dword:00000001
"ShowSuperHidden"=dword:00000001
"ShowSecondsInSystemClock"=dword:00000001
"LaunchTo"=dword:00000001

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
"LongPathsEnabled"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
"VerboseStatus"=dword:00000001

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CrashControl]
"DisplayParameters"=dword:00000001
"AutoReboot"=dword:00000000

[HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32]
@=""
```

### Privacy Focused
```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent]
"DisableWindowsSpotlightFeatures"=dword:00000001
"DisableThirdPartySuggestions"=dword:00000001
"DisableWindowsSpotlightOnActionCenter"=dword:00000001
"DisableWindowsSpotlightOnSettings"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]
"AllowClipboardHistory"=dword:00000000
"AllowCrossDeviceClipboard"=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
"Start_TrackDocs"=dword:00000000
"Start_TrackProgs"=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer]
"NoInstrumentation"=dword:00000001
"NoRecentDocsHistory"=dword:00000001
```

### Low-End Performance
```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Control Panel\Desktop]
"MinAnimate"="0"

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
"TaskbarAnimations"=dword:00000000
"ListviewAlphaSelect"=dword:00000000
"ListviewShadow"=dword:00000000
"DisableThumbnailCache"=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]
"VisualFXSetting"=dword:00000002

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize]
"EnableTransparency"=dword:00000000

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm]
"DisableProjectedShadows"=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize]
"StartupDelayInMSec"=dword:00000000
```

### Streaming / OBS
```reg
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications]
"NoToastApplicationNotification"=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
"TaskbarBadges"=dword:00000000
"TaskbarFlashing"=dword:00000000
"ShowTaskViewButton"=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search]
"SearchboxTaskbarMode"=dword:00000000

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Dsh]
"AllowNewsAndInterests"=dword:00000000
```

---

## Accent Color Examples

### Nord Theme
```reg
[HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM]
"ColorizationColor"=dword:c42e3440
"ColorizationAfterglow"=dword:c42e3440
"AccentColor"=dword:ff403e35

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent]
"AccentColorMenu"=dword:ff403e35
"StartColorMenu"=dword:ff3b3a2a
"AccentPalette"=hex:59,65,7c,ff,4a,54,68,ff,3f,48,59,ff,35,3c,4a,ff,2a,30,3b,ff,1f,24,2c,ff,11,13,17,ff,88,17,98,00
```

### Default Blue (Restore)
```reg
[HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM]
"ColorizationColor"=dword:c40078d4
"ColorizationAfterglow"=dword:c40078d4
"AccentColor"=dword:ffd40078

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent]
"AccentColorMenu"=dword:ffd40078
"StartColorMenu"=dword:ffc20040
"AccentPalette"=hex:99,eb,ff,00,4c,c2,ff,00,00,91,f8,00,00,78,d4,00,00,67,c0,00,00,3e,92,00,00,1a,68,00,f7,63,0c,00
```

---

## Fonts

### Apply Custom Font
```reg
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts]
"Segoe UI (TrueType)"=""
"Segoe UI Bold (TrueType)"=""
"Segoe UI Italic (TrueType)"=""
"Segoe UI Bold Italic (TrueType)"=""
"Segoe UI Light (TrueType)"=""
"Segoe UI Semibold (TrueType)"=""
"Segoe UI Semilight (TrueType)"=""

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes]
"Segoe UI"="Inter"
```

### Restore Default Font
```reg
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts]
"Segoe UI (TrueType)"="segoeui.ttf"
"Segoe UI Bold (TrueType)"="segoeuib.ttf"
"Segoe UI Italic (TrueType)"="segoeuii.ttf"
"Segoe UI Bold Italic (TrueType)"="segoeuiz.ttf"
"Segoe UI Light (TrueType)"="segoeuil.ttf"
"Segoe UI Semibold (TrueType)"="seguisb.ttf"
"Segoe UI Semilight (TrueType)"="segoeuisl.ttf"

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes]
"Segoe UI"=-
```

Recommended fonts:
- Inter (modern, clean)
- JetBrains Mono (developer)
- Cascadia Code (terminal)
- Fira Code (programming)

---

## Important Notes

1. Backup first: export registry keys before changes.
2. Sign out or reboot may be required for some settings.
3. HKLM vs HKCU:
   - HKLM = all users (admin required)
   - HKCU = current user only
4. Policy vs Settings:
   - Policy paths lock the setting
   - Non-policy paths allow user changes
