using System;
using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;
using WindowsOptimizer.Engine.Tweaks.Commands.System;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class SystemTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "System";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Core System Behavior
        yield return CreateRegistryTweak(
            context,
            "system.enable-game-mode",
            "Enable Game Mode",
            "Ensures Windows Game Mode is active for optimized resource allocation during gaming.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\GameBar",
            "AutoGameModeEnabled",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "system.disable-startup-delay",
            "Disable Startup Program Delay",
            "Removes the artificial 10-second delay for startup programs to make boot feel faster.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
            "StartupDelayInMSec",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "system.verbose-status-messages",
            "Enable Verbose Status Messages",
            "Shows detailed status messages during startup, shutdown, logon, and logoff.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "VerboseStatus",
            RegistryValueKind.DWord,
            1);

        // Appearance & Explorer
        yield return CreateRegistryTweak(
            context,
            "system.disable-shortcut-arrow",
            "Remove Shortcut Arrow Overlay",
            "Removes the small arrow icon that appears on desktop shortcuts.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons",
            "29",
            RegistryValueKind.String,
            @"%windir%\System32\shell32.dll,-50");

        yield return CreateRegistryTweak(
            context,
            "system.disable-search-highlights",
            "Disable Search Highlights",
            "Turns off dynamic 'highlights' content in the Windows search box.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\SearchSettings",
            "IsDynamicSearchBoxEnabled",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        // Mass Disablements
        yield return CreateScheduledTaskBatchTweak(
            context,
            "system.disable-scheduled-tasks",
            "Disable Telemetry & Maintenance Tasks",
            "Disables dozens of scheduled tasks related to telemetry, data collection, and non-essential maintenance.",
            TweakRiskLevel.Risky,
            new[]
            {
                @"\Microsoft\Windows\Application Experience\MareBackup",
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\DiskCleanup\SilentCleanup",
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Windows Error Reporting\QueueReporting"
            });

        yield return CreateServiceStartModeBatchTweak(
            context,
            "system.disable-non-essential-services",
            "Disable Non-Essential Services",
            "Disables various non-critical services (Print Spooler, Bluetooth if unused, etc.) to free up resources.",
            TweakRiskLevel.Risky,
            new[]
            {
                "DiagTrack",       // Connected User Experiences and Telemetry
                "dmwappushservice", // WAP Push Message Routing Service
                "SysMain",          // Superfetch/SysMain (can be risky on SSDs)
                "WSearch",          // Windows Search
                "WerSvc",           // Windows Error Reporting Service
                "Spooler",          // Print Spooler
                "PrintNotify",      // Printer Notifications
                "PrintWorkflowUserSvc_*", // Per-user print workflow
                "PrintDeviceConfigurationService", // Printer device configuration
                "PrintScanBrokerService", // Print/Scan broker
                "bthserv",          // Bluetooth Support Service
                "BluetoothUserService_*", // Per-user Bluetooth service
                "BTAGService"       // Bluetooth Audio Gateway
            },
            ServiceStartMode.Disabled);

        yield return CreateRegistryTweak(
            context,
            "system.aero-shake",
            "Disable Aero Shake",
            "Prevents windows from being minimized or restored when the active window is shaken back and forth with the mouse.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "NoWindowMinimizingShortcuts",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "system.disable-jpeg-reduction",
            "Disable JPEG Reduction",
            "Sets the desktop wallpaper JPEG import quality to 100% to avoid compression artifacts.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Control Panel\Desktop",
            "JPEGImportQuality",
            RegistryValueKind.DWord,
            100,
            requiresElevation: false);

        yield return CreateRegistryValueSetTweak(
            context,
            "system.disable-clipboard-history",
            "Disable Clipboard History & Sync",
            "Turns off clipboard history and cross-device clipboard synchronization.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            new[]
            {
                new RegistryValueSetEntry("AllowClipboardHistory", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("AllowCrossDeviceClipboard", RegistryValueKind.DWord, 0)
            });

        yield return CreateRegistryTweak(
            context,
            "system.disable-clipboard-redirection",
            "Disable Clipboard Redirection (RDP)",
            "Prevents clipboard sharing between remote desktop sessions and the local machine.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
            "fDisableClip",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.disable-background-gp-updates",
            "Disable Background Group Policy Updates",
            "Prevents Group Policy from refreshing while users are active.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
            "DisableBkGndGroupPolicy",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.disable-auto-maintenance",
            "Disable Automatic Maintenance",
            "Stops scheduled automatic maintenance tasks from running.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
            "MaintenanceDisabled",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.disable-store-open-with",
            "Disable Store in Open With",
            "Removes the \"Look for an app in the Store\" option from Open With.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "NoUseStoreOpenWith",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.disable-restartable-apps",
            "Disable Restartable Apps",
            "Prevents apps from automatically restarting after sign-in.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon",
            "RestartApps",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);



        yield return CreateRegistryTweak(
            context,
            "system.enable-hags",
            "Enable Hardware-Accelerated GPU Scheduling",
            "Lets the GPU handle its own scheduling for improved responsiveness.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Control\GraphicsDrivers",
            "HwSchMode",
            RegistryValueKind.DWord,
            2);

        yield return CreateRegistryTweak(
            context,
            "system.disable-storage-sense",
            "Disable Storage Sense",
            "Turns off Storage Sense automatic cleanup.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\StorageSense",
            "AllowStorageSenseGlobal",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.disable-storage-sense-temp-cleanup",
            "Disable Storage Sense Temporary Files Cleanup",
            "Prevents Storage Sense from deleting temporary files.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\StorageSense",
            "AllowStorageSenseTemporaryFilesCleanup",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.disable-search-highlights-policy",
            "Disable Search Highlights (Policy)",
            "Disables search highlights via policy for all users.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Windows Search",
            "EnableDynamicContentInWSB",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.disable-service-splitting",
            "Disable Service Splitting",
            "Prevents services from being split into separate svchost processes.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Control",
            "SvcHostSplitThresholdInKB",
            RegistryValueKind.DWord,
            -1);

        // Command-based System Tweaks
        yield return new CheckDiskHealthTweak(context.ElevatedCommandRunner);
        yield return new ClearEventLogsTweak(context.ElevatedCommandRunner, "System");
    }
}
