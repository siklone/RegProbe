using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Misc;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PrivacyTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Privacy & Notifications";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Data Collection & Telemetry
        yield return CreateRegistryTweak(
            context,
            "privacy.disable-diagnostic-data",
            "Disable Diagnostic Data (Policy)",
            "Sets diagnostic data collection to the minimum level required for Windows to operate.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "AllowTelemetry",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-advertising-id",
            "Disable Advertising ID",
            "Prevents Windows from tracking you for advertising personalization.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
            "Enabled",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryValueSetTweak(
            context,
            "privacy.disable-activity-history",
            "Disable Activity History",
            "Stops publishing and uploading activity history (Timeline) across devices.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            new[]
            {
                new RegistryValueSetEntry("EnableActivityFeed", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("PublishUserActivities", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("UploadUserActivities", RegistryValueKind.DWord, 0)
            });

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-application-telemetry",
            "Disable Application Telemetry",
            "Stops the Application Telemetry engine from collecting usage data for compatibility.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\AppCompat",
            "AITEnable",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-wer",
            "Disable Windows Error Reporting",
            "Disables automatic generation and upload of error reports to Microsoft.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Windows Error Reporting",
            "Disabled",
            RegistryValueKind.DWord,
            1);

        // Experience & Personalization
        yield return CreateRegistryTweak(
            context,
            "privacy.disable-windows-tips",
            "Disable Windows Tips & Tricks",
            "Stops Windows from showing tips, shortcuts, and suggestions on the lock screen and in Settings.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            "SubscribedContent-338389Enabled",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-app-suggestions",
            "Disable App Suggestions in Start",
            "Prevents Windows from suggesting promoted apps in the Start menu.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            "SystemPaneSuggestionsEnabled",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryValueSetTweak(
            context,
            "privacy.disable-suggestions-cdm",
            "Disable Content Delivery Manager Suggestions",
            "Disables various suggestions and auto-installed apps from the Content Delivery Manager.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            new[]
            {
                new RegistryValueSetEntry("SubscribedContent-310093Enabled", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SubscribedContent-338393Enabled", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SubscribedContent-353694Enabled", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SubscribedContent-353696Enabled", RegistryValueKind.DWord, 0)
            },
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-copilot",
            "Disable Windows Copilot",
            "Turns off the Windows Copilot AI experience for the current user.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\WindowsCopilot",
            "TurnOffWindowsCopilot",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-recall",
            "Disable Windows Recall",
            "Disables saving snapshots for the Recall AI feature on this user.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\WindowsAI",
            "DisableAIDataAnalysis",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        // Hardware & Capability Access
        yield return CreateRegistryTweak(
            context,
            "privacy.disable-location-services",
            "Disable Location Services",
            "Turns off location tracking services system-wide for all users.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\LocationAndSensors",
            "DisableLocation",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-camera",
            "Disable Camera Access (Policy)",
            "Disables camera access for all applications via group policy.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Camera",
            "AllowCamera",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-background-apps",
            "Disable Background Apps",
            "Prevents Windows apps from running in the background, saving battery and resources.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\AppPrivacy",
            "LetAppsRunInBackground",
            RegistryValueKind.DWord,
            2);

        // Notifications
        yield return CreateRegistryTweak(
            context,
            "notifications.disable-toast",
            "Disable Toast Notifications",
            "Blocks balloon and toast notifications for all applications for the current user.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
            "NoToastApplicationNotification",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "notifications.disable-lock-screen",
            "Disable Lock Screen Notifications",
            "Prevents app notifications from showing on the lock screen.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
            "NoToastApplicationNotificationOnLockScreen",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "notifications.disable-feedback-frequency",
            "Disable Feedback Requests",
            "Stops Windows from asking for feedback or ratings.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Siuf\Rules",
            "NumberOfSIUFInPeriod",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "notifications.disable-tile",
            "Disable Tile Notifications",
            "Prevents apps from updating tiles and tile badges.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
            "NoTileApplicationNotification",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "notifications.disable-mirroring",
            "Disable Notification Mirroring",
            "Stops notifications from being mirrored to other devices.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
            "DisallowNotificationMirroring",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        // Complex/Composite Tweaks
        var MobSyncPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "mobsync.exe");
        yield return CreateCompositeTweak(
            "privacy.disable-offline-files",
            "Disable Offline Files",
            "Disables Offline Files (CSC) via policy, services, tasks, and Sync Center.",
            TweakRiskLevel.Advanced,
            new ITweak[]
            {
                CreateRegistryTweak(context, "privacy.disable-offline-files.policy", "Disable Offline Files (Policy)", "", TweakRiskLevel.Advanced, RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\NetCache", "Enabled", RegistryValueKind.DWord, 0),
                CreateServiceStartModeBatchTweak(context, "privacy.disable-offline-files.services", "Disable Offline Files (Services)", "", TweakRiskLevel.Advanced, new[] { "CSC", "CscService" }, ServiceStartMode.Disabled),
                CreateScheduledTaskBatchTweak(context, "privacy.disable-offline-files.tasks", "Disable Offline Files (Tasks)", "", TweakRiskLevel.Advanced, new[] { @"\Microsoft\Windows\Offline Files\Background Synchronization", @"\Microsoft\Windows\Offline Files\Logon Synchronization" }),
                CreateFileRenameTweak(context, "privacy.disable-offline-files.binary", "Disable Offline Files (Sync Center)", "", TweakRiskLevel.Advanced, MobSyncPath, MobSyncPath + ".disabled")
            });

        // App-specific Telemetry
        yield return DisableOneDriveTweaks.CreateDisableOneDriveTweak(context.ElevatedRegistry);
        yield return DisableEdgeFeaturesTweaks.CreateDisableEdgeFeaturesTweak(context.ElevatedRegistry);
        yield return DisableVisualStudioTelemetryTweak.CreateDisableVisualStudioTelemetryTweak(context.ElevatedRegistry);
        yield return DisableOfficeTelemetryTweak.CreateDisableOfficeTelemetryTweak(context.LocalRegistry);
        yield return new DisableVSCodeTelemetryTweak();

        // Additional Privacy Tweaks
        yield return CreateRegistryTweak(
            context,
            "privacy.disable-cross-device-experiences",
            "Disable Cross-Device Experiences",
            "Disables continue experiences on this device (CDP).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            "EnableCdp",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-phone-linking",
            "Disable Phone Linking",
            "Prevents the device from participating in Phone-PC linking.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            "EnableMmx",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-resume",
            "Disable Resume Experiences",
            "Turns off Resume (start on one device, continue on this PC).",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\CrossDeviceResume\Configuration",
            "IsResumeAllowed",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryValueSetTweak(
            context,
            "privacy.disable-cli-telemetry",
            "Disable PowerShell & .NET CLI Telemetry",
            "Opts out of PowerShell and .NET CLI telemetry for the current user.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Environment",
            new[]
            {
                new RegistryValueSetEntry("POWERSHELL_TELEMETRY_OPTOUT", RegistryValueKind.String, "1"),
                new RegistryValueSetEntry("DOTNET_CLI_TELEMETRY_OPTOUT", RegistryValueKind.String, "1")
            },
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-language-list-access",
            "Disable Website Access to Language List",
            "Prevents websites from accessing the language list for content customization.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Control Panel\International\User Profile",
            "HttpAcceptLanguageOptOut",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryValueSetTweak(
            context,
            "privacy.disable-wmplayer-telemetry",
            "Disable Windows Media Player Telemetry",
            "Turns off usage tracking and online metadata for Windows Media Player.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\MediaPlayer\Preferences",
            new[]
            {
                new RegistryValueSetEntry("AcceptedPrivacyStatement", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("MetadataRetrieval", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SendUserGUID", RegistryValueKind.Binary, new byte[] { 0x00 }),
                new RegistryValueSetEntry("SilentAcquisition", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("UsageTracking", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableMRUMusic", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("DisableMRUPictures", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("DisableMRUVideo", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("DisableMRUPlaylists", RegistryValueKind.DWord, 1)
            },
            requiresElevation: false);

        yield return CreateRegistryValueSetTweak(
            context,
            "privacy.disable-sync-settings",
            "Disable Settings Sync",
            "Disables syncing Windows settings and related data across devices.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\SettingSync",
            new[]
            {
                new RegistryValueSetEntry("DisableSyncOnPaidNetwork", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("DisableAppSyncSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableAppSyncSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableApplicationSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableApplicationSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableCredentialsSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableCredentialsSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisablePersonalizationSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisablePersonalizationSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableDesktopThemeSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableDesktopThemeSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableStartLayoutSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableStartLayoutSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableWebBrowserSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableWebBrowserSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("DisableWindowsSettingSync", RegistryValueKind.DWord, 2),
                new RegistryValueSetEntry("DisableWindowsSettingSyncUserOverride", RegistryValueKind.DWord, 0)
            });

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-search-history",
            "Disable Search History",
            "Prevents search history from being stored for this user.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "DisableSearchHistory",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-search-box-suggestions",
            "Disable Search Box Suggestions",
            "Stops File Explorer from showing recent search suggestions.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "DisableSearchBoxSuggestions",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.hide-recommended-section",
            "Hide Start Recommended Section (Policy)",
            "Removes the Recommended section from the Start menu for all users.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "HideRecommendedSection",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.hide-recommended-section-user",
            "Hide Start Recommended Section (User)",
            "Removes the Recommended section from the Start menu for the current user.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "HideRecommendedSection",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.hide-recommended-personalized-sites",
            "Hide Start Personalized Site Recommendations (Policy)",
            "Removes personalized website recommendations from Start for all users.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "HideRecommendedPersonalizedSites",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.hide-recommended-personalized-sites-user",
            "Hide Start Personalized Site Recommendations (User)",
            "Removes personalized website recommendations from Start for the current user.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "HideRecommendedPersonalizedSites",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryValueSetTweak(
            context,
            "privacy.disable-suggestions",
            "Disable Suggestions & Tips",
            "Turns off Windows tips, welcome experiences, and Settings recommendations.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            new[]
            {
                new RegistryValueSetEntry("SubscribedContent-338389Enabled", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SubscribedContent-310093Enabled", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SubscribedContent-338393Enabled", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SubscribedContent-353694Enabled", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("SubscribedContent-353696Enabled", RegistryValueKind.DWord, 0)
            },
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-consumer-account-content",
            "Disable Consumer Account State Content",
            "Prevents Windows experiences from using cloud consumer account content.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\CloudContent",
            "DisableConsumerAccountStateContent",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-online-tips",
            "Disable Online Tips",
            "Stops Settings from retrieving online tips and help content.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            "AllowOnlineTips",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryValueBatchTweak(
            context,
            "privacy.disable-edge-search-suggestions",
            "Disable Edge Search Suggestions",
            "Turns off search suggestions in Microsoft Edge address bar.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Edge", "SearchSuggestEnabled", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Edge", "LocalProvidersEnabled", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\MicrosoftEdge\SearchScopes", "ShowSearchSuggestionsGlobal", RegistryValueKind.DWord, 0)
            });

        yield return CreateRegistryValueBatchTweak(
            context,
            "privacy.disable-location-consent",
            "Disable Location Consent (User)",
            "Denies location capability access for the current user.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value", RegistryValueKind.String, "Deny"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location\NonPackaged", "Value", RegistryValueKind.String, "Deny"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "ShowGlobalPrompts", RegistryValueKind.DWord, 1)
            },
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-location-consent-system",
            "Disable Location Consent (System)",
            "Denies location capability access at the system level.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
            "Value",
            RegistryValueKind.String,
            "Deny");

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-location-scripting",
            "Disable Location Scripting",
            "Disables location scripting support for apps and scripts.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\LocationAndSensors",
            "DisableLocationScripting",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-windows-location-provider",
            "Disable Windows Location Provider",
            "Disables the Windows Location Provider for all apps.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\LocationAndSensors",
            "DisableWindowsLocationProvider",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-sensors",
            "Disable Sensors",
            "Turns off hardware sensor access for apps and system features.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\LocationAndSensors",
            "DisableSensors",
            RegistryValueKind.DWord,
            1);

        var psrPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "psr.exe");
        yield return CreateCompositeTweak(
            "privacy.disable-steps-recorder",
            "Disable Steps Recorder",
            "Disables Steps Recorder to prevent recording user actions.",
            TweakRiskLevel.Advanced,
            new ITweak[]
            {
                CreateRegistryTweak(context, "privacy.disable-steps-recorder.policy", "Disable Steps Recorder (Policy)", "", TweakRiskLevel.Advanced, RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppCompat", "DisableUAR", RegistryValueKind.DWord, 1),
                CreateFileRenameTweak(context, "privacy.disable-steps-recorder.binary", "Disable Steps Recorder (Binary)", "", TweakRiskLevel.Advanced, psrPath, psrPath + ".disabled")
            });

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-app-launch-tracking",
            "Disable App Launch Tracking",
            "Stops Windows from tracking app launches for Start/Search personalization.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "Start_TrackProgs",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-reserved-storage",
            "Disable Reserved Storage",
            "Disables Windows reserved storage for updates.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\ReserveManager",
            "DisableDeletes",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-biometrics",
            "Disable Biometrics",
            "Turns off Windows biometric features on this device.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Biometrics",
            "Enabled",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-biometrics-logon",
            "Disable Biometrics Logon",
            "Prevents users from signing in with biometrics.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Biometrics\Credential Provider",
            "Enabled",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-biometrics-domain-logon",
            "Disable Biometrics for Domain Logon",
            "Prevents domain users from signing in with biometrics.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Biometrics\Credential Provider",
            "Domain Accounts",
            RegistryValueKind.DWord,
            0);
    }
}
