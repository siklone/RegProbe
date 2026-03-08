# Win-Config Batch 01 - Registry Mapping

Source: nohuto/win-config (with Microsoft references). This file adds per-tweak anchors for the Win-Config batch and links back to the main Docs tree (do not edit the main docs to keep provenance clean).

Notes:
- Tweak IDs match the app IDs so links can jump directly to these sections.
- Some entries are documented but not implemented yet; those are listed in the "Not Implemented" section.

## Service Host Splitting

### <a id="system.disable-service-splitting"></a> Disable Service Splitting
- Path: HKLM\SYSTEM\CurrentControlSet\Control
- Value: SvcHostSplitThresholdInKB (REG_DWORD)
- Default: 380000
- Target: 0xFFFFFFFF (disable splitting)
- Source: https://learn.microsoft.com/en-us/windows/application-management/svchost-service-refactoring
- Main docs: Docs/system/system.md

## Kernel Scheduler (DPC)

### <a id="system.kernel-adjust-dpc-threshold"></a> Adjust DPC Threshold
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: AdjustDpcThreshold (REG_DWORD)
- Default/Target: 20
- Main docs: Docs/system/system.md

### <a id="system.kernel-ideal-dpc-rate"></a> Ideal DPC Rate
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: IdealDpcRate (REG_DWORD)
- Default/Target: 20
- Main docs: Docs/system/system.md

### <a id="system.kernel-minimum-dpc-rate"></a> Minimum DPC Rate
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: MinimumDpcRate (REG_DWORD)
- Default/Target: 3
- Main docs: Docs/system/system.md

### <a id="system.kernel-dpc-queue-depth"></a> DPC Queue Depth
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: DpcQueueDepth (REG_DWORD)
- Default/Target: 4
- Main docs: Docs/system/system.md

### <a id="system.kernel-dpc-watchdog-period"></a> DPC Watchdog Period
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: DpcWatchdogPeriod (REG_DWORD)
- Default/Target: 120000
- Main docs: Docs/system/system.md

### <a id="system.kernel-serialize-timer-expiration"></a> Serialize Timer Expiration
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: SerializeTimerExpiration (REG_DWORD)
- Default/Target: 1
- Main docs: Docs/system/system.md

### <a id="system.kernel-thread-dpc-enable"></a> Threaded DPC Enable
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: ThreadDpcEnable (REG_DWORD)
- Default/Target: 1
- Main docs: Docs/system/system.md

### <a id="system.kernel-disable-low-qos-timer-resolution"></a> Disable Low QoS Timer Resolution
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel
- Value: DisableLowQosTimerResolution (REG_DWORD)
- Default/Target: 1
- Main docs: Docs/system/system.md

## Priority Control

### <a id="system.priority-control"></a> Win32 Priority Separation
- Path: HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl
- Value: Win32PrioritySeparation (REG_DWORD)
- Default/Target: 38 (0x26)
- Source: Windows Internals 7th Edition
- Main docs: Docs/system/system.md

## MMCSS (Multimedia Scheduler)

### <a id="power.disable-network-power-saving"></a> Network Throttling + System Responsiveness
- Path: HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile
- Values: NetworkThrottlingIndex (REG_DWORD), SystemResponsiveness (REG_DWORD)
- Default: 10, 20
- Target: 0xFFFFFFFF, 10
- Source: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
- Main docs: Docs/system/system.md

## Graphics Drivers (DXGKRNL)

### <a id="system.enable-hags"></a> Hardware Accelerated GPU Scheduling (HAGS)
- Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
- Value: HwSchMode (REG_DWORD)
- Default: 1 (disabled)
- Target: 2 (enabled)
- Source: https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/
- Main docs: Docs/system/system.md

### <a id="system.graphics-tdr-delay"></a> TDR Delay
- Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
- Value: TdrDelay (REG_DWORD)
- Default/Target: 2
- Main docs: Docs/system/system.md

### <a id="system.graphics-tdr-ddi-delay"></a> TDR DDI Delay
- Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
- Value: TdrDdiDelay (REG_DWORD)
- Default/Target: 5
- Main docs: Docs/system/system.md

### <a id="system.graphics-tdr-level"></a> TDR Level
- Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
- Value: TdrLevel (REG_DWORD)
- Default/Target: 3
- Main docs: Docs/system/system.md

### <a id="system.graphics-tdr-limit-count"></a> TDR Limit Count
- Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
- Value: TdrLimitCount (REG_DWORD)
- Default/Target: 5
- Main docs: Docs/system/system.md

### <a id="system.graphics-tdr-limit-time"></a> TDR Limit Time
- Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
- Value: TdrLimitTime (REG_DWORD)
- Default/Target: 60
- Main docs: Docs/system/system.md

### <a id="system.graphics-disable-overlays"></a> Disable Overlays
- Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
- Value: DisableOverlays (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

## Desktop Window Manager (DWM)

### <a id="system.dwm-disable-mpo"></a> Disable Multiplane Overlay (MPO)
- Path: HKLM\SOFTWARE\Microsoft\Windows\Dwm
- Value: OverlayTestMode (REG_DWORD)
- Default: 0
- Target: 5
- Main docs: Docs/system/system.md

### <a id="system.dwm-disable-overlay-min-fps"></a> Disable Overlay Minimum FPS
- Path: HKLM\SOFTWARE\Microsoft\Windows\Dwm
- Value: OverlayMinFPS (REG_DWORD)
- Default: 15
- Target: 0
- Main docs: Docs/system/system.md

## File System (NTFS)

### <a id="system.ntfs-disable-8dot3"></a> Disable 8.3 Name Creation
- Path: HKLM\SYSTEM\CurrentControlSet\Control\FileSystem
- Value: NtfsDisable8dot3NameCreation (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

### <a id="system.ntfs-disable-last-access"></a> Disable Last Access Update
- Path: HKLM\SYSTEM\CurrentControlSet\Control\FileSystem
- Value: NtfsDisableLastAccessUpdate (REG_DWORD)
- Default/Target: 1
- Main docs: Docs/system/system.md

### <a id="system.ntfs-enable-long-paths"></a> Enable Win32 Long Paths
- Path: HKLM\SYSTEM\CurrentControlSet\Control\FileSystem
- Value: LongPathsEnabled (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

### <a id="system.ntfs-reset-memory-usage"></a> Reset NTFS Memory Usage
- Path: HKLM\SYSTEM\CurrentControlSet\Control\FileSystem
- Value: NtfsMemoryUsage (REG_DWORD)
- Default/Target: 0
- Main docs: Docs/system/system.md

### <a id="system.ntfs-reset-mft-zone"></a> Reset NTFS MFT Zone Reservation
- Path: HKLM\SYSTEM\CurrentControlSet\Control\FileSystem
- Value: NtfsMftZoneReservation (REG_DWORD)
- Default/Target: 1
- Main docs: Docs/system/system.md

## Shutdown Timeouts

### <a id="system.reduce-shutdown-timeouts"></a> Reduce Shutdown Timeouts
- Path: HKLM\SYSTEM\CurrentControlSet\Control (WaitToKillServiceTimeout)
- Path: HKCU\Control Panel\Desktop (WaitToKillAppTimeout, HungAppTimeout, AutoEndTasks)
- Values: REG_SZ
- Defaults: 5000/5000/5000/0
- Targets: 2500/2500/1500/1
- Main docs: Docs/system/system.md

## Game Mode and Fullscreen Optimizations

### <a id="system.enable-game-mode"></a> Enable Game Mode
- Path: HKCU\Software\Microsoft\GameBar
- Value: AutoGameModeEnabled (REG_DWORD)
- Default/Target: 1
- Main docs: Docs/system/system.md

### <a id="system.disable-game-dvr"></a> Disable Game DVR
- Path: HKCU\System\GameConfigStore
- Value: GameDVR_Enabled (REG_DWORD)
- Default: 1
- Target: 0
- Main docs: Docs/system/system.md

### <a id="system.disable-fullscreen-optimizations"></a> Disable Fullscreen Optimizations (FSO)
- Path: HKCU\System\GameConfigStore
- Values: GameDVR_FSEBehavior, GameDVR_FSEBehaviorMode, GameDVR_HonorUserFSEBehaviorMode, GameDVR_DXGIHonorFSEWindowsCompatible (REG_DWORD)
- Defaults: 0/0/0/0
- Targets: 2/2/1/1
- Main docs: Docs/system/system.md

## Telemetry

### <a id="privacy.disable-diagnostic-data"></a> AllowTelemetry (Policy)
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection
- Value: AllowTelemetry (REG_DWORD)
- Default: 3
- Target: 0 (plus additional telemetry flags in this tweak)
- Main docs: Docs/privacy/privacy.md

## Storage Sense

### <a id="system.disable-storage-sense"></a> Disable Storage Sense
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense
- Value: AllowStorageSenseGlobal (REG_DWORD)
- Default: 1
- Target: 0
- Main docs: Docs/system/system.md

### <a id="system.disable-storage-sense-temp-cleanup"></a> Disable Storage Sense Temporary Files Cleanup
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense
- Value: AllowStorageSenseTemporaryFilesCleanup (REG_DWORD)
- Default: 1
- Target: 0
- Main docs: Docs/system/system.md

## Notifications

### <a id="notifications.disable-toast"></a> Disable Toast Notifications
- Path: HKCU\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications
- Value: NoToastApplicationNotification (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/notifications/notifications.md

### <a id="notifications.disable-lockscreen-toast"></a> Disable Lock Screen Toasts
- Path: HKCU\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications
- Value: NoToastApplicationNotificationOnLockScreen (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/notifications/notifications.md

### <a id="notifications.disable-tile"></a> Disable Tile Notifications
- Path: HKCU\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications
- Value: NoTileApplicationNotification (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/notifications/notifications.md

## Windows Search

### <a id="system.disable-search-web-results"></a> Disable Web Search Results
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search
- Value: DoNotUseWebResults (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

### <a id="system.disable-search-remote-queries"></a> Disable Remote Search Queries
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search
- Value: PreventRemoteQueries (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

### <a id="system.enable-indexing-encrypted-items"></a> Enable Indexing of Encrypted Items
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search
- Value: AllowIndexingEncryptedStoresOrItems (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

## Visual Effects

### <a id="system.aero-shake"></a> Disable Aero Shake
- Path: HKCU\Software\Policies\Microsoft\Windows\Explorer
- Value: NoWindowMinimizingShortcuts (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

### <a id="system.disable-jpeg-reduction"></a> Disable JPEG Reduction
- Path: HKCU\Control Panel\Desktop
- Value: JPEGImportQuality (REG_DWORD)
- Default: 85
- Target: 100
- Main docs: Docs/system/system.md

## Explorer

### <a id="explorer.disable-low-disk-space-warning"></a> Disable Low Disk Space Warning
- Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
- Value: NoLowDiskSpaceChecks (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

## Verbose Status Messages

### <a id="system.verbose-status-messages"></a> Verbose Status Messages
- Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
- Value: VerboseStatus (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

## Blue Screen Settings

### <a id="system.bsod-display-parameters"></a> Show BSOD Parameters
- Path: HKLM\SYSTEM\CurrentControlSet\Control\CrashControl
- Value: DisplayParameters (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

### <a id="system.bsod-disable-auto-reboot"></a> Disable Auto Reboot on Crash
- Path: HKLM\SYSTEM\CurrentControlSet\Control\CrashControl
- Value: AutoReboot (REG_DWORD)
- Default: 1
- Target: 0
- Main docs: Docs/system/system.md

## Clipboard

### <a id="system.disable-clipboard-history"></a> Disable Clipboard History and Sync
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\System
- Values: AllowClipboardHistory, AllowCrossDeviceClipboard (REG_DWORD)
- Defaults: 1/1
- Targets: 0/0
- Main docs: Docs/system/system.md

## Memory Management

### <a id="system.memory-clear-pagefile-at-shutdown"></a> Clear Page File At Shutdown
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management
- Value: ClearPageFileAtShutdown (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

### <a id="system.memory-disable-paging-executive"></a> Disable Paging Executive
- Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management
- Value: DisablePagingExecutive (REG_DWORD)
- Default: 0
- Target: 1
- Main docs: Docs/system/system.md

## App Archiving

### <a id="system.disable-app-archiving"></a> Disable Automatic App Archiving
- Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\Appx
- Value: AllowAutomaticAppArchiving (REG_DWORD)
- Default: 1
- Target: 0
- Main docs: Docs/system/system.md

## Not Implemented (Documented Only)

These are documented in the batch source but not exposed as tweaks yet.

- MMCSS NoLazyMode / LazyModeTimeout / SchedulerTimerResolution / SchedulerPeriod (docs warn against NoLazyMode = 1).
- Segment Heap global enable (high risk).
