using System;
using System.Collections.Generic;
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
    }
}
