using System.Collections.Generic;
using Microsoft.Win32;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Registry;
using OpenTraceProject.Core.Services;
using OpenTraceProject.Engine;
using OpenTraceProject.Engine.Tweaks;

namespace OpenTraceProject.App.Services.TweakProviders;

public sealed class SystemRegistryTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "System Registry";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Priority control (scheduler foreground boost)
        yield return CreateRegistryTweak(
            context,
            "system.priority-control",
            "Set Foreground Scheduling Priority",
            "Sets Win32PrioritySeparation to the app's observed 0x26 foreground scheduling profile for research comparisons.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\PriorityControl",
            "Win32PrioritySeparation",
            RegistryValueKind.DWord,
            38);

        // Kernel scheduler (DPC) defaults
        yield return CreateRegistryTweak(
            context,
            "system.kernel-adjust-dpc-threshold",
            "Kernel: Adjust DPC Threshold",
            "Sets the DPC threshold adjustment value to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "AdjustDpcThreshold",
            RegistryValueKind.DWord,
            20);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-ideal-dpc-rate",
            "Kernel: Ideal DPC Rate",
            "Sets the target DPC rate per second to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "IdealDpcRate",
            RegistryValueKind.DWord,
            20);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-minimum-dpc-rate",
            "Kernel: Minimum DPC Rate",
            "Sets the minimum DPC rate threshold to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "MinimumDpcRate",
            RegistryValueKind.DWord,
            3);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-dpc-queue-depth",
            "Kernel: DPC Queue Depth",
            "Sets the maximum DPC queue depth to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DpcQueueDepth",
            RegistryValueKind.DWord,
            4);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-dpc-watchdog-period",
            "Kernel: DPC Watchdog Period",
            "Sets the DPC watchdog timeout to the documented default (milliseconds).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DpcWatchdogPeriod",
            RegistryValueKind.DWord,
            120000);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-serialize-timer-expiration",
            "Kernel: Serialize Timer Expiration",
            "Enables timer serialization using the documented default value.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "SerializeTimerExpiration",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-cache-aware-scheduling",
            "Kernel: Cache-Aware Scheduling",
            "Restores Windows' documented cache-aware scheduling default so the scheduler stays aligned with normal CPU cache topology behavior.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "CacheAwareScheduling",
            RegistryValueKind.DWord,
            47);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-default-dynamic-hetero-cpu-policy",
            "Kernel: Default Dynamic Hetero CPU Policy",
            "Returns Windows' hybrid CPU scheduling policy to the documented default used for heterogeneous core systems.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DefaultDynamicHeteroCpuPolicy",
            RegistryValueKind.DWord,
            3);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-thread-dpc-enable",
            "Kernel: Threaded DPC Enable",
            "Enables threaded DPCs using the documented default value.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "ThreadDpcEnable",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-disable-low-qos-timer-resolution",
            "Kernel: Disable Low QoS Timer Resolution",
            "Disables low QoS timer resolution using the documented default value.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DisableLowQosTimerResolution",
            RegistryValueKind.DWord,
            1);

        // Graphics driver defaults (TDR + overlays)
        yield return CreateRegistryTweak(
            context,
            "system.graphics-tdr-delay",
            "Graphics: TDR Delay (Default)",
            "Sets the GPU timeout delay (TdrDelay) to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "TdrDelay",
            RegistryValueKind.DWord,
            2);

        yield return CreateRegistryTweak(
            context,
            "system.graphics-tdr-ddi-delay",
            "Graphics: TDR DDI Delay (Default)",
            "Sets the DDI timeout delay (TdrDdiDelay) to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "TdrDdiDelay",
            RegistryValueKind.DWord,
            5);

        yield return CreateRegistryTweak(
            context,
            "system.graphics-tdr-level",
            "Graphics: TDR Level (Default)",
            "Sets the TDR recovery level to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "TdrLevel",
            RegistryValueKind.DWord,
            3);

        yield return CreateRegistryTweak(
            context,
            "system.graphics-tdr-limit-count",
            "Graphics: TDR Limit Count (Default)",
            "Sets the TDR limit count to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "TdrLimitCount",
            RegistryValueKind.DWord,
            5);

        yield return CreateRegistryTweak(
            context,
            "system.graphics-tdr-limit-time",
            "Graphics: TDR Limit Time (Default)",
            "Sets the TDR limit time to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "TdrLimitTime",
            RegistryValueKind.DWord,
            60);

        yield return CreateRegistryTweak(
            context,
            "system.graphics-disable-overlays",
            "Graphics: Disable Overlay Planes",
            "Disables overlay planes to reduce composition issues in some configurations.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "DisableOverlays",
            RegistryValueKind.DWord,
            1);

        // Desktop Window Manager
        yield return CreateRegistryTweak(
            context,
            "system.dwm-disable-mpo",
            "DWM: Disable Multiplane Overlay (MPO)",
            "Disables MPO to avoid flicker or driver issues on some systems.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\Dwm",
            "OverlayTestMode",
            RegistryValueKind.DWord,
            5);

        yield return CreateRegistryTweak(
            context,
            "system.dwm-disable-overlay-min-fps",
            "DWM: Disable Overlay Minimum FPS",
            "Removes the minimum FPS requirement for overlay assignment.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\Dwm",
            "OverlayMinFPS",
            RegistryValueKind.DWord,
            0);

        // File system (NTFS)
        yield return CreateRegistryTweak(
            context,
            "system.ntfs-disable-8dot3",
            "Disable 8.3 Name Creation",
            "Stops NTFS from creating 8.3 short file names on all volumes.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\FileSystem",
            "NtfsDisable8dot3NameCreation",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.ntfs-disable-last-access",
            "Disable Last Access Updates",
            "Disables last access timestamp updates to reduce disk I/O.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\FileSystem",
            "NtfsDisableLastAccessUpdate",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.ntfs-enable-long-paths",
            "Enable Win32 Long Paths",
            "Allows Win32 long paths for applications that opt in.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\FileSystem",
            "LongPathsEnabled",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.ntfs-reset-memory-usage",
            "Reset NTFS Memory Usage",
            "Resets NTFS memory usage back to the Windows default.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\FileSystem",
            "NtfsMemoryUsage",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.ntfs-reset-mft-zone",
            "Reset NTFS MFT Zone Reservation",
            "Resets the MFT zone reservation back to the Windows default.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\FileSystem",
            "NtfsMftZoneReservation",
            RegistryValueKind.DWord,
            1);

        // Service shutdown timeout
        yield return CreateRegistryTweak(
            context,
            "system.wait-to-kill-service-timeout",
            "Reduce Service Shutdown Timeout",
            "Shortens the service shutdown timeout so Windows waits less time before terminating an unresponsive service during shutdown.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control",
            "WaitToKillServiceTimeout",
            RegistryValueKind.String,
            "2500");

        // Game DVR + Fullscreen Optimizations
        yield return WithMicrosoftDoc(
            CreateRegistryTweak(
                context,
                "system.disable-game-recording-broadcasting",
                "Disable Game Recording & Broadcasting",
                "Disables Windows game recording and broadcasting for all users through the official policy setting.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
                "AllowGameDVR",
                RegistryValueKind.DWord,
                0),
            "https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement");

        yield return CreateRegistryValueSetTweak(
            context,
            "system.disable-fullscreen-optimizations",
            "Disable Fullscreen Optimizations",
            "Disables fullscreen optimizations for more consistent exclusive fullscreen behavior.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"System\GameConfigStore",
            new[]
            {
                new RegistryValueSetEntry("GameDVR_FSEBehavior", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("GameDVR_FSEBehaviorMode", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("GameDVR_HonorUserFSEBehaviorMode", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("GameDVR_DXGIHonorFSEWindowsCompatible", RegistryValueKind.DWord, 1),
            },
            requiresElevation: false);

        // Windows Search policies
        yield return CreateCommandBackedRegistryTweak(
            context,
            "system.disable-search-web-results",
            "Disable Web Search Results",
            "Removes web results from Windows Search for all users.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            "ConnectedSearchUseWeb",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.disable-search-remote-queries",
            "Disable Remote Search Queries",
            "Prevents remote clients from querying this device's search index.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            "PreventRemoteQueries",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.enable-indexing-encrypted-items",
            "Enable Indexing of Encrypted Items",
            "Allows encrypted files to be indexed by Windows Search.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
            "AllowIndexingEncryptedStoresOrItems",
            RegistryValueKind.DWord,
            1);

        // Blue Screen settings
        yield return CreateRegistryTweak(
            context,
            "system.bsod-display-parameters",
            "Show BSOD Parameters",
            "Displays additional crash parameters on the blue screen.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\CrashControl",
            "DisplayParameters",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.bsod-disable-auto-reboot",
            "Disable Auto Reboot on Crash",
            "Prevents automatic reboot after a crash to allow reading the BSOD.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\CrashControl",
            "AutoReboot",
            RegistryValueKind.DWord,
            0);

        // Memory management
        yield return CreateRegistryTweak(
            context,
            "system.memory-clear-pagefile-at-shutdown",
            "Clear Page File at Shutdown",
            "Clears the page file on shutdown for privacy (slower shutdown).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            "ClearPageFileAtShutdown",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.memory-disable-paging-executive",
            "Disable Paging Executive",
            "Keeps kernel and drivers in RAM (requires sufficient memory).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            "DisablePagingExecutive",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryValueSetTweak(
            context,
            "system.reliability-timestamp-enabled",
            "Enable Reliability Event Timestamping",
            "Turns on the reliability timestamp gate and sets the companion policy interval to 24 hours.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability",
            new[]
            {
                new RegistryValueSetEntry("TimeStampEnabled", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("TimeStampInterval", RegistryValueKind.DWord, 86400)
            });

        yield return CreateRegistryTweak(
            context,
            "system.memory-large-system-cache-client",
            "Memory: Use Client System Cache",
            "Keeps memory behavior on the normal desktop/client default so Windows favors applications instead of file-server style caching.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            "LargeSystemCache",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.memory-paged-pool-dynamic",
            "Memory: Reset Paged Pool Size",
            "Returns kernel paged pool allocation to the Windows-managed dynamic default. Use this when a manual pool tweak should be undone.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            "PagedPoolSize",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.memory-nonpaged-pool-dynamic",
            "Memory: Reset Non-Paged Pool Size",
            "Returns kernel non-paged pool allocation to the Windows-managed dynamic default. This is mainly a rollback-to-default style setting.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            "NonPagedPoolSize",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.memory-registry-quota-default",
            "Memory: Reset Registry Quota",
            "Restores registry quota to the documented default so the registry goes back to normal Windows sizing behavior.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control",
            "RegistrySizeLimit",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.graphics-page-fault-debug-mode",
            "Graphics: Page Fault Debug Mode",
            "Restores the graphics page-fault debug mode value Windows expects by default. Useful when undoing manual scheduler experiments.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "PageFaultDebugMode",
            RegistryValueKind.DWord,
            1);

        // App archiving
        yield return CreateRegistryTweak(
            context,
            "system.disable-app-archiving",
            "Disable Automatic App Archiving",
            "Stops Windows from archiving unused apps automatically.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\Appx",
            "AllowAutomaticAppArchiving",
            RegistryValueKind.DWord,
            0);
    }
}
