using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.App.ViewModels;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public class SystemTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "System";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "system.enable-game-mode",
                "Enable Game Mode",
                "Ensures Game Mode is enabled for the current user.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\GameBar",
                "AutoGameModeEnabled",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "system.disable-dst-notifications",
                "Disable DST Change Notifications",
                "Turns off daylight saving time change notifications.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Control Panel\TimeDate",
                "DstNotification",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "system.disable-search-highlights",
                "Disable Search Highlights (User)",
                "Turns off search highlights in the search box for the current user.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\SearchSettings",
                "IsDynamicSearchBoxEnabled",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "system.disable-low-disk-space-checks",
                "Disable Low Disk Space Checks",
                "Disables the Low Disk Space warning notifications for the current user.",
                TweakRiskLevel.Advanced,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "NoLowDiskSpaceChecks",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false)
        };
    }
}
