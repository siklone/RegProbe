using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Privacy;
using RegProbe.Engine.Tweaks.Misc;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class PrivacyTweakProvider : BaseTweakProvider
{
    private const string AllowTelemetryEditionMessage =
        "This tweak only applies on Enterprise, Education, or Server-class editions where AllowTelemetry=0 is documented as supported.";
    private static readonly RegistryValueReference WindowsEditionIdReference = new(
        RegistryHive.LocalMachine,
        RegistryView.Default,
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
        "EditionID");

    public override string CategoryName => "Privacy & Notifications";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Data Collection & Telemetry
        var allowTelemetryTweak = CreateRegistryTweak(
            context,
            "privacy.disable-diagnostic-data",
            "Set Diagnostic Data to Minimum Supported Level",
            "Sets diagnostic data collection to the lowest documented level supported by the edition.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\DataCollection",
            "AllowTelemetry",
            RegistryValueKind.DWord,
            0);
        yield return new ConditionalTweak(
            allowTelemetryTweak,
            ct => EvaluateAllowTelemetryEditionAsync(context.LocalRegistry, ct));

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

        // Experience & Personalization
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

        // Notifications
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

    }

    private static async Task<TweakResult?> EvaluateAllowTelemetryEditionAsync(
        IRegistryAccessor registryAccessor,
        CancellationToken ct)
    {
        var editionId = await ReadRegistryStringAsync(registryAccessor, WindowsEditionIdReference, ct);
        if (string.IsNullOrWhiteSpace(editionId))
        {
            return new TweakResult(
                TweakStatus.Failed,
                "Unable to determine the Windows edition for the diagnostic data policy gate.",
                DateTimeOffset.UtcNow);
        }

        if (SupportsAllowTelemetryMinimumLevel(editionId))
        {
            return null;
        }

        return new TweakResult(
            TweakStatus.NotApplicable,
            $"{AllowTelemetryEditionMessage} Current edition: {editionId.Trim()}.",
            DateTimeOffset.UtcNow);
    }

    private static async Task<string?> ReadRegistryStringAsync(
        IRegistryAccessor registryAccessor,
        RegistryValueReference reference,
        CancellationToken ct)
    {
        var result = await registryAccessor.ReadValueAsync(reference, ct);
        if (!result.Exists || result.Value is null)
        {
            return null;
        }

        return result.Value.Kind is RegistryValueKind.String or RegistryValueKind.ExpandString
            ? result.Value.StringValue
            : result.Value.ToObject().ToString();
    }

    private static bool SupportsAllowTelemetryMinimumLevel(string editionId)
    {
        var normalized = editionId.Trim();
        return normalized.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Education", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Server", StringComparison.OrdinalIgnoreCase);
    }
}
