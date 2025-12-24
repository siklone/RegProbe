using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PrivacyTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Privacy";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "privacy.disable-telemetry",
                "Disable Telemetry",
                "Blocks Windows telemetry data collection.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "AllowTelemetry",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "privacy.disable-advertising-id",
                "Disable Advertising ID",
                "Prevents Windows from tracking you for advertising.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                "Enabled",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "privacy.disable-activity-feed",
                "Disable Activity Feed",
                "Stops Windows from collecting activity history.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System",
                "EnableActivityFeed",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "privacy.disable-location-tracking",
                "Disable Location Tracking",
                "Prevents apps and services from accessing your location.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                "DisableLocation",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "privacy.disable-cortana",
                "Disable Cortana",
                "Disables Cortana voice assistant and search suggestions.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "AllowCortana",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "privacy.disable-cloud-clipboard",
                "Disable Cloud Clipboard",
                "Prevents clipboard data from syncing across devices.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Clipboard",
                "EnableClipboardHistory",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "privacy.disable-windows-tips",
                "Disable Windows Tips",
                "Stops Windows from showing tips and suggestions.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SubscribedContent-338389Enabled",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "privacy.disable-app-suggestions",
                "Disable App Suggestions",
                "Prevents Windows from suggesting apps in Start menu.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SystemPaneSuggestionsEnabled",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryValueSetTweak(
                context,
                "privacy.disable-timeline",
                "Disable Windows Timeline",
                "Disables activity history collection for Timeline feature.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System",
                new[]
                {
                    new RegistryValueSetEntry("EnableActivityFeed", RegistryValueKind.DWord, 0),
                    new RegistryValueSetEntry("PublishUserActivities", RegistryValueKind.DWord, 0),
                    new RegistryValueSetEntry("UploadUserActivities", RegistryValueKind.DWord, 0)
                }),

            CreateRegistryTweak(
                context,
                "privacy.disable-feedback-frequency",
                "Disable Feedback Requests",
                "Stops Windows from asking for feedback.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Siuf\Rules",
                "NumberOfSIUFInPeriod",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false)
        };
    }
}
