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
                "WerSvc"            // Windows Error Reporting Service
            },
            ServiceStartMode.Disabled);

        // Command-based System Tweaks
        yield return new CheckDiskHealthTweak(context.ElevatedCommandRunner);
        yield return new ClearEventLogsTweak(context.ElevatedCommandRunner, "System");
    }
}
