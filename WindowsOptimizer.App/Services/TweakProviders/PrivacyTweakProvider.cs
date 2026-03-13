using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Privacy;
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
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\AdvertisingInfo",
            "DisabledByGroupPolicy",
            RegistryValueKind.DWord,
            1);

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

        yield return CreateCompositeTweak(
            "privacy.disable-application-compatibility",
            "Disable Application Compatibility",
            "Turns off Windows application compatibility components, telemetry, and related tasks.",
            TweakRiskLevel.Risky,
            new ITweak[]
            {
                CreateRegistryValueSetTweak(
                    context,
                    "privacy.disable-application-compatibility.policy",
                    "Disable Application Compatibility (Policy)",
                    "Turns off application compatibility policies.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\AppCompat",
                    new[]
                    {
                        new RegistryValueSetEntry("DisableEngine", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableAPISamping", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableApplicationFootprint", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableInstallTracing", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableWin32AppBackup", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisablePCA", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("SbEnable", RegistryValueKind.DWord, 0)
                    }),
                CreateScheduledTaskBatchTweak(
                    context,
                    "privacy.disable-application-compatibility.tasks",
                    "Disable Application Compatibility (Tasks)",
                    "Disables Application Experience scheduled tasks.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        @"\Microsoft\Windows\Application Experience\MareBackup",
                        @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                        @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp",
                        @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
                        @"\Microsoft\Windows\Application Experience\SdbinstMergeDbTask",
                        @"\Microsoft\Windows\Application Experience\StartupAppTask"
                    })
            });

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

        yield return CreateCommandBackedRegistryValueBatchTweak(
            context,
            "privacy.disable-ceip",
            "Disable CEIP",
            "Opts out of Customer Experience Improvement Program data collection.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\AppV\CEIP", "CEIPEnable", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Messenger\Client", "CEIP", RegistryValueKind.DWord, 2)
            });

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-device-name-telemetry",
            "Disable Device Name in Diagnostics",
            "Prevents the device name from being included in diagnostic data.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "AllowDeviceNameInTelemetry",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-diagnostic-data-viewer",
            "Disable Diagnostic Data Viewer",
            "Blocks access to the Diagnostic Data Viewer in Settings.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "DisableDiagnosticDataViewer",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-diagnostic-data-delete",
            "Disable Diagnostic Data Deletion",
            "Disables the ability to delete diagnostic data in Settings.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "DisableDeviceDelete",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-telemetry-change-notifications",
            "Disable Diagnostic Data Change Notifications",
            "Stops opt-in change notifications for diagnostic data.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "DisableTelemetryOptInChangeNotification",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-telemetry-optin-ui",
            "Disable Diagnostic Data Opt-in UI",
            "Disables the diagnostic data opt-in settings UI in Settings.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "DisableTelemetryOptInSettingsUx",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.limit-diagnostic-log-collection",
            "Limit Diagnostic Log Collection",
            "Prevents additional diagnostic logs from being collected.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "LimitDiagnosticLogCollection",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.limit-dump-collection",
            "Limit Dump Collection",
            "Limits diagnostic dumps to reduce the data sent in diagnostics.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "LimitDumpCollection",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-onesettings-downloads",
            "Disable OneSettings Downloads",
            "Stops Windows from downloading configuration settings from OneSettings.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "DisableOneSettingsDownloads",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-kms-activation-telemetry",
            "Disable KMS Activation Telemetry",
            "Stops KMS client activation data from being sent to Microsoft.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
            "NoGenTicket",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryValueBatchTweak(
            context,
            "privacy.disable-rsop-logging",
            "Disable RSoP Logging",
            "Turns off Resultant Set of Policy logging on this device.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SYSTEM", "RsopLogging", RegistryValueKind.DWord, 0)
            });

        yield return CreateRegistryValueBatchTweak(
            context,
            "privacy.disable-sleep-study-diagnostics",
            "Disable Sleep Study Diagnostics",
            "Disables sleep study diagnostic event channels.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\Microsoft-Windows-SleepStudy/Diagnostic",
                    "Enabled",
                    RegistryValueKind.DWord,
                    0),
                new RegistryValueBatchEntry(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\Microsoft-Windows-Kernel-Processor-Power/Diagnostic",
                    "Enabled",
                    RegistryValueKind.DWord,
                    0),
                new RegistryValueBatchEntry(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\Microsoft-Windows-UserModePowerService/Diagnostic",
                    "Enabled",
                    RegistryValueKind.DWord,
                    0)
            });

        yield return CreateRegistryValueBatchTweak(
            context,
            "privacy.troubleshooter-dont-run",
            "Troubleshooter: Don't Run Any",
            "Prevents recommended troubleshooters from running automatically.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsMitigation", "UserPreference", RegistryValueKind.DWord, 1),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Troubleshooting\AllowRecommendations", "TroubleshootingAllowRecommendations", RegistryValueKind.DWord, 0)
            });

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
            "privacy.disable-font-providers",
            "Disable Font Providers",
            "Prevents Windows from downloading fonts from online providers.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            "EnableFontProviders",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryValueBatchTweak(
            context,
            "privacy.disable-inking-typing-personalization",
            "Disable Inking & Typing Personalization",
            "Stops sending inking and typing data to Microsoft.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Policies\TextInput", "AllowLinguisticDataCollection", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\WindowsInkWorkspace", "AllowSuggestedAppsInWindowsInkWorkspace", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\WindowsInkWorkspace", "AllowWindowsInkWorkspace", RegistryValueKind.DWord, 0)
            });

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
        yield return CreateRegistryValueBatchTweak(
            context,
            "privacy.deny-app-access",
            "Deny App Access (Except Microphone)",
            "Forces Windows apps to be denied access to sensitive capabilities.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\System", "AllowUserInfoAccess", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessAccountInfo", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCalendar", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCallHistory", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCamera", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessContacts", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessEmail", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGraphicsCaptureProgrammatic", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGraphicsCaptureWithoutBorder", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessHumanPresence", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessLocation", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMessaging", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMicrophone", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMotion", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessNotifications", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessPhone", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessRadios", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsSyncWithDevices", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessTasks", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessTrustedDevices", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsGetDiagnosticInfo", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGazeInput", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoice", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoiceAboveLock", RegistryValueKind.DWord, 2),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessBackgroundSpatialPerception", RegistryValueKind.DWord, 2)
            });

        yield return CreateCommandBackedRegistryTweak(
            context,
            "privacy.disable-app-diagnostics",
            "Disable App Diagnostics",
            "Prevents apps from accessing diagnostic information.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
            "LetAppsAccessDiagnosticInfo",
            RegistryValueKind.DWord,
            2);

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
            "privacy.disable-feedback-notifications",
            "Disable Feedback Notifications",
            "Stops Windows Feedback prompts from appearing.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "DoNotShowFeedbackNotifications",
            RegistryValueKind.DWord,
            1);

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
        yield return CreateRegistryTweak(
            context,
            "privacy.disable-file-history",
            "Disable File History",
            "Turns off File History backups.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\FileHistory",
            "Disabled",
            RegistryValueKind.DWord,
            1);

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
        yield return DisableEdgeFeaturesTweaks.CreateDisableEdgeFeaturesTweak(context.LocalRegistry);
        yield return DisableVisualStudioTelemetryTweak.CreateDisableVisualStudioTelemetryTweak(context.ElevatedRegistry);
        yield return DisableOfficeTelemetryTweak.CreateDisableOfficeTelemetryTweak(context.LocalRegistry);
        yield return new DisableVSCodeTelemetryTweak();

        // Additional Privacy Tweaks
        yield return CreateRegistryValuePresetBatchTweak(
            context,
            "privacy.disable-cross-device-experiences",
            "Cross-Device Sharing",
            "Choose whether nearby Windows experiences stay off, work only with your devices, or are available to everyone nearby.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValuePresetBatchOption(
                    "off",
                    "Off",
                    "Stops nearby sharing and continue-on-other-device experiences on this PC.",
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\System", "EnableCdp", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP", "CdpSessionUserAuthzPolicy", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP\SettingsPage", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 0)
                    }),
                new RegistryValuePresetBatchOption(
                    "my-devices",
                    "My devices only",
                    "Keeps cross-device experiences limited to devices signed in with your account.",
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\System", "EnableCdp", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP", "CdpSessionUserAuthzPolicy", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP\SettingsPage", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 1)
                    }),
                new RegistryValuePresetBatchOption(
                    "everyone-nearby",
                    "Everyone nearby",
                    "Allows Windows to share supported experiences with nearby devices, not just your own.",
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\System", "EnableCdp", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP", "CdpSessionUserAuthzPolicy", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CDP\SettingsPage", "RomeSdkChannelUserAuthzPolicy", RegistryValueKind.DWord, 2)
                    })
            },
            "off");

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-find-my-device",
            "Disable Find My Device",
            "Stops Windows from registering this PC with Find My Device and keeps location-based recovery turned off.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\FindMyDevice",
            "AllowFindMyDevice",
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

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-message-sync",
            "Disable Message Sync",
            "Stops SMS/MMS cloud sync for this device.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Messaging",
            "AllowMessageSync",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-mdm-enrollment",
            "Disable MDM Enrollment",
            "Prevents new MDM enrollments for this device.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\CurrentVersion\MDM",
            "DisableRegistration",
            RegistryValueKind.DWord,
            1);

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
            "privacy.block-microsoft-accounts",
            "Block Microsoft Accounts",
            "Prevents adding or signing in with Microsoft accounts.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "NoConnectedUser",
            RegistryValueKind.DWord,
            3);

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-local-security-questions",
            "Disable Local Security Questions",
            "Prevents setting security questions for local accounts.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            "NoLocalPasswordResetQuestions",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.hide-last-logged-in-user",
            "Hide Last Logged-In User",
            "Removes the last signed-in username from the sign-in screen.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "DontDisplayLastUserName",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "privacy.hide-username-at-signin",
            "Hide Username at Sign-In",
            "Hides the username after credentials are entered at sign-in.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "DontDisplayUserName",
            RegistryValueKind.DWord,
            1);

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

        yield return CreateCommandBackedRegistryTweak(
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

        yield return CreateCommandBackedRegistryTweak(
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

        yield return CreateCommandBackedRegistryTweak(
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

        var helpPanePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "HelpPane.exe");
        var helpPaneDisabledPath = helpPanePath + ".disabled";
        yield return CreateFileRenameTweak(
            context,
            "privacy.disable-f1-help",
            "Disable F1 Help",
            "Disables F1 help by renaming HelpPane.exe.",
            TweakRiskLevel.Advanced,
            helpPanePath,
            helpPaneDisabledPath);

        yield return CreateCommandBackedRegistryValueBatchTweak(
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
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location\NonPackaged", "Value", RegistryValueKind.String, "Deny")
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

        yield return CreateRegistryTweak(
            context,
            "privacy.disable-steps-recorder",
            "Disable Steps Recorder",
            "Disables Steps Recorder through policy to prevent recording user actions.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\AppCompat",
            "DisableUAR",
            RegistryValueKind.DWord,
            1);

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
