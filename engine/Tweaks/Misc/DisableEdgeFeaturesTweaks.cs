using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Misc;

public static class DisableEdgeFeaturesTweaks
{
    public static RegistryValueBatchTweak CreateDisableEdgeFeaturesTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>();
        var basePath = @"Software\Policies\Microsoft\Edge";

        // Core Edge features to disable
        var policies = new Dictionary<string, object>
        {
            // Auto-import and first run
            { "AutoImportAtFirstRun", 4 }, // Disabled
            { "HideFirstRunExperience", 1 },
            { "DefaultBrowserSettingEnabled", 0 },

            // Telemetry and personalization
            { "PersonalizationReportingEnabled", 0 },
            { "ShowRecommendationsEnabled", 0 },
            { "UserFeedbackAllowed", 0 },

            // UI elements
            { "PinBrowserEssentialsToolbarButton", 0 },
            { "HubsSidebarEnabled", 0 },
            { "StandaloneHubsSidebarEnabled", 0 },
            { "EdgeCollectionsEnabled", 0 },
            { "SplitScreenEnabled", 0 },

            // Sync and sign-in
            { "SyncDisabled", 1 },
            { "ImplicitSignInEnabled", 0 },

            // Features
            { "EdgeFollowEnabled", 0 },
            { "HideRestoreDialogEnabled", 1 },
            { "EdgeShoppingAssistantEnabled", 0 },
            { "ShowMicrosoftRewards", 0 },
            { "QuickSearchShowMiniMenu", 0 },
            { "SearchbarAllowed", 0 },
            { "StartupBoostEnabled", 0 },

            // New Tab Page
            { "NewTabPageHideDefaultTopSites", 1 },
            { "NewTabPageQuickLinksEnabled", 0 },
            { "NewTabPageAllowedBackgroundTypes", 1 },
            { "NewTabPageContentEnabled", 0 },

            // Tab services and predictions
            { "TabServicesEnabled", 0 },
            { "TextPredictionEnabled", 0 },

            // Privacy
            { "TrackingPrevention", 3 }, // Strict
            { "DefaultSensorsSetting", 2 }  // Block sensors
        };

        foreach (var policy in policies)
        {
            entries.Add(new RegistryValueBatchEntry(RegistryHive.LocalMachine, basePath, policy.Key, RegistryValueKind.DWord, policy.Value, RegistryView.Default));
        }

        // EdgeUI policies (Windows 8/8.1 specific)
        var edgeUIPath = @"Software\Policies\Microsoft\Windows\EdgeUI";
        var edgeUIPolicies = new Dictionary<string, int>
        {
            { "DisableHelpSticker", 1 },
            { "DisableMFUTracking", 1 },
            { "DisableRecentApps", 1 },
            { "DisableCharms", 1 },
            { "TurnOffBackstack", 1 },
            { "AllowEdgeSwipe", 0 }
        };

        foreach (var policy in edgeUIPolicies)
        {
            // Add for both HKLM and HKCU
            entries.Add(new RegistryValueBatchEntry(RegistryHive.LocalMachine, edgeUIPath, policy.Key, RegistryValueKind.DWord, policy.Value, RegistryView.Default));

            entries.Add(new RegistryValueBatchEntry(RegistryHive.CurrentUser, edgeUIPath, policy.Key, RegistryValueKind.DWord, policy.Value, RegistryView.Default));
        }

        return new RegistryValueBatchTweak(
            id: "misc.disable-edge-features",
            name: "Disable Microsoft Edge Features",
            description: "Disables telemetry, personalization, sync, sidebar, shopping, rewards, and various other Edge features. Also disables Windows 8/8.1 EdgeUI features.",
            risk: TweakRiskLevel.Advanced,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
