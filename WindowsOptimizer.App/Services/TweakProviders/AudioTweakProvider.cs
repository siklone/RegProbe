using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Peripheral;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class AudioTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Audio";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Audio Experience
        yield return AudioTweaks.CreateDisableAudioDuckingTweak(context.LocalRegistry);
        yield return AudioTweaks.CreateDisableAudioEnhancementsTweak(context.ElevatedRegistry);

        yield return CreateRegistryTweak(
            context,
            "audio.disable-beep",
            "Disable System Beep",
            "Disables the hardware system beep driver (Beep.sys).",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Services\Beep",
            "Start",
            RegistryValueKind.DWord,
            4);

        yield return CreateRegistryTweak(
            context,
            "audio.show-hidden-devices",
            "Show Hidden Audio Devices",
            "Shows hidden audio devices in the sound control panel.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Multimedia\Audio\DeviceCpl",
            "ShowHiddenDevices",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "audio.show-disconnected-devices",
            "Show Disconnected Audio Devices",
            "Shows disconnected audio devices in the sound control panel.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Multimedia\Audio\DeviceCpl",
            "ShowDisconnectedDevices",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "audio.disable-spatial-audio",
            "Disable Spatial Audio",
            "Disables spatial audio for low-latency devices.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio",
            "DisableSpatialOnLowLatency",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryValueBatchTweak(
            context,
            "audio.disable-system-sounds",
            "Disable System Sounds",
            "Clears the default sound events for common system sounds.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemAsterisk\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Reminder\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Close\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\CriticalBatteryAlarm\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemHand\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\.Default\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MailBeep\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\DeviceConnect\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\DeviceDisconnect\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\DeviceFail\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemExclamation\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.IM\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\LowBatteryAlarm\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Maximize\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MenuCommand\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MenuPopup\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MessageNudge\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Minimize\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\FaxBeep\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Mail\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.SMS\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Proximity\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\ProximityConnection\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Open\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\PrintComplete\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\AppGPFault\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemQuestion\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\RestoreDown\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\RestoreUp\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\CCSelect\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\ShowBand\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemNotification\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\ChangeTheme\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\WindowsUAC\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\BlockedPopup\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\ActivatingDocument\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\EmptyRecycleBin\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\FeedDiscovered\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\MoveMenuItem\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\SecurityBand\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\Navigating\.Current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\DisNumbersSound\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\PanelSound\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\MisrecoSound\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\HubOffSound\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\HubOnSound\.current", "", RegistryValueKind.String, string.Empty),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\HubSleepSound\.current", "", RegistryValueKind.String, string.Empty)
            },
            requiresElevation: false);
    }
}
