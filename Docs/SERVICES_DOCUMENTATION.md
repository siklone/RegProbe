# RegProbe Service and Task Reference

This document explains the Windows services, scheduled tasks, and supporting system components that RegProbe touches in the shipped product surface.

## Services Commonly Managed By The App

### Telemetry and data collection

| Service | Display name | Risk | Notes |
| --- | --- | --- | --- |
| `DiagTrack` | Connected User Experiences and Telemetry | Safe | Sends telemetry data to Microsoft. |
| `dmwappushservice` | WAP Push Message Routing Service | Safe | Routes push-related messages and notifications. |
| `WerSvc` | Windows Error Reporting Service | Safe | Uploads crash and diagnostic reports. |

### Performance and search

| Service | Display name | Risk | Notes |
| --- | --- | --- | --- |
| `SysMain` | SysMain | Advanced | Prefetches application data; benefits vary on SSD-heavy systems. |
| `WSearch` | Windows Search | Advanced | Maintains the file/content index for search. |

### Printing

| Service | Display name | Risk | Notes |
| --- | --- | --- | --- |
| `Spooler` | Print Spooler | Risky | Only disable when the machine does not need local or network printing. |
| `PrintNotify` | Printer Extensions and Notifications | Safe | Handles print-related notifications. |
| `PrintWorkflowUserSvc_*` | Print Workflow User Service | Safe | Per-user print pipeline helper. |
| `PrintDeviceConfigurationService` | Printer Device Configuration Service | Safe | Applies printer-specific configuration. |
| `PrintScanBrokerService` | Print/Scan Broker Service | Safe | Brokering layer for print and scan flows. |

### Bluetooth

| Service | Display name | Risk | Notes |
| --- | --- | --- | --- |
| `bthserv` | Bluetooth Support Service | Risky | Reasonable to disable only when Bluetooth is not used at all. |
| `BluetoothUserService_*` | Bluetooth User Support Service | Safe | Per-user Bluetooth service instance. |
| `BTAGService` | Bluetooth Audio Gateway Service | Advanced | Supports Bluetooth audio gateway scenarios. |

## Scheduled Tasks Commonly Managed By The App

### Telemetry tasks

| Task path | Notes |
| --- | --- |
| `\Microsoft\Windows\Application Experience\MareBackup` | Application compatibility backup task. |
| `\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser` | Compatibility assessment task. |
| `\Microsoft\Windows\Customer Experience Improvement Program\Consolidator` | CEIP data consolidation task. |
| `\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip` | USB CEIP data task. |
| `\Microsoft\Windows\Feedback\Siuf\DmClient` | Feedback prompt task. |
| `\Microsoft\Windows\Windows Error Reporting\QueueReporting` | Error report queue processing. |

### Maintenance tasks

| Task path | Risk | Notes |
| --- | --- | --- |
| `\Microsoft\Windows\DiskCleanup\SilentCleanup` | Safe | Automated disk cleanup run. |
| `\Microsoft\Windows\Diagnosis\Scheduled` | Safe | Scheduled diagnostics collection. |
| `\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector` | Safe | Disk diagnostic collector. |
| `\Microsoft\Windows\Maintenance\WinSAT` | Safe | Windows System Assessment Tool maintenance task. |
| `\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem` | Safe | Power-efficiency analysis task. |

### Device information tasks

| Task path | Notes |
| --- | --- |
| `\Microsoft\Windows\Device Information\Device` | Collects system device information. |
| `\Microsoft\Windows\Device Information\Device User` | Collects per-user device information. |

## Tweak Provider Coverage

| Category | Source file | Scope | Notes |
| --- | --- | --- | --- |
| System | `SystemTweakProvider.cs` | Curated UI surface | Game Mode, startup delay, and service-backed actions. |
| System Registry | `SystemRegistryTweakProvider.cs` | Advanced registry catalog | Kernel, NTFS, DWM, and low-level system values. |
| Privacy | `PrivacyTweakProvider.cs` | Curated UI surface | Telemetry, location, activity history, and policy-backed privacy settings. |
| Security | `SecurityTweakProvider.cs` | Curated UI surface | UAC, firewall, Defender-related, and policy-backed security settings. |
| Network | `NetworkTweakProvider.cs` | Curated UI surface | SMB, IPv6, adapter, and network stack values. |
| Performance | `PerformanceTweakProvider.cs` | Curated UI surface | Animations, throttling, and responsiveness settings. |
| Peripheral | `PeripheralTweakProvider.cs` | Curated UI surface | Mouse, keyboard, and input behavior settings. |
| Audio | `AudioTweakProvider.cs` | Curated UI surface | Beep, ducking, and audio device presentation settings. |
| Visibility | `VisibilityTweakProvider.cs` | Curated UI surface | Explorer/UI visibility and presentation options. |
| Misc | `MiscTweakProvider.cs` | Curated UI surface | Small non-core convenience actions. |
| Legacy | `LegacyTweakProvider.cs` | Research/back-compat catalog | Historical or compatibility-focused tweak mappings. |

## Permission Model

### Operations that require elevation

- Writing to `HKLM`
- Changing service startup types
- Disabling scheduled tasks
- Editing protected system locations
- Applying BCD changes

### Operations that can stay user-scoped

- Writing to `HKCU`
- Updating per-user profile settings
- Applying theme and appearance changes

## Related Files

```text
app/Services/TweakProviders/
|- AudioTweakProvider.cs
|- BaseTweakProvider.cs
|- LegacyTweakProvider.cs
|- MiscTweakProvider.cs
|- NetworkTweakProvider.cs
|- PerformanceTweakProvider.cs
|- PeripheralTweakProvider.cs
|- PrivacyTweakProvider.cs
|- SecurityTweakProvider.cs
|- SystemRegistryTweakProvider.cs
|- SystemTweakProvider.cs
`- VisibilityTweakProvider.cs
```

## References

- [Windows services and per-user services](https://learn.microsoft.com/en-us/windows/application-management/per-user-services-in-windows)
- [Task Scheduler start page](https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-start-page)
- [Service Control Manager overview](https://learn.microsoft.com/en-us/windows/win32/services/service-control-manager)
