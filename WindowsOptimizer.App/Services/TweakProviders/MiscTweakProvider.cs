using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class MiscTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Miscellaneous";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            // Security
            CreateRegistryTweak(
                context,
                "security.enable-uac",
                "Enable User Account Control",
                "Ensures UAC is enabled for better security.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                "EnableLUA",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.disable-autorun",
                "Disable AutoRun",
                "Prevents automatic execution of programs from removable drives.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "NoDriveTypeAutoRun",
                RegistryValueKind.DWord,
                0xFF),

            // Notifications
            CreateRegistryTweak(
                context,
                "notifications.disable-action-center",
                "Disable Action Center",
                "Disables the Windows Action Center notification panel.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Policies\Microsoft\Windows\Explorer",
                "DisableNotificationCenter",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "notifications.disable-lockscreen-notifications",
                "Disable Lock Screen Notifications",
                "Prevents notifications from appearing on the lock screen.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings",
                "NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            // Performance
            CreateRegistryTweak(
                context,
                "performance.disable-animations",
                "Disable Window Animations",
                "Disables visual effects and animations for better performance.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Control Panel\Desktop\WindowMetrics",
                "MinAnimate",
                RegistryValueKind.String,
                "0",
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "performance.disable-transparency",
                "Disable Transparency Effects",
                "Disables window transparency for improved performance.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            // Explorer/Visibility
            CreateRegistryTweak(
                context,
                "explorer.show-file-extensions",
                "Show File Extensions",
                "Shows file extensions for known file types in Explorer.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "HideFileExt",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "explorer.show-hidden-files",
                "Show Hidden Files",
                "Shows hidden files and folders in Explorer.",
                TweakRiskLevel.Advanced,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "Hidden",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false)
        };
    }
}
