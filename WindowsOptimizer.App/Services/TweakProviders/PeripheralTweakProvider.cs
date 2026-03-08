using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Peripheral;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PeripheralTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Peripherals & Input";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Mouse Optimization
        yield return MouseTweaks.CreateDisableMouseThrottleTweak(context.LocalRegistry);
        yield return MouseTweaks.CreateDisableMouseAccelerationTweak(context.LocalRegistry);

        // Keyboard Optimization
        yield return KeyboardTweaks.CreateOptimizeKeyboardRepeatTweak(context.LocalRegistry);
        yield return KeyboardTweaks.CreateDisableLanguageSwitchHotkeyTweak(context.LocalRegistry);

        // General Input
        yield return CreateRegistryTweak(
            context,
            "peripheral.disable-sticky-keys-prompt",
            "Disable Sticky Keys Prompt",
            "Prevents the annoying prompt when pressing Shift multiple times.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Control Panel\Accessibility\StickyKeys",
            "Flags",
            RegistryValueKind.String,
            "506",
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "peripheral.disable-autoplay",
            "Disable AutoPlay",
            "Disables AutoPlay for removable media on this user account.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers",
            "DisableAutoplay",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryValueBatchTweak(
            context,
            "peripheral.autoplay-take-no-action",
            "AutoPlay: Take No Action",
            "Sets AutoPlay handlers to take no action for common media events.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\StorageOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\StorageOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\CameraAlternate\ShowPicturesOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\CameraAlternate\ShowPicturesOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedDVDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedDVDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleDVDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleDVDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayBluRayOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayBluRayOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleBDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleBDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayCDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayCDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedCDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedCDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleCDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleCDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlaySuperVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlaySuperVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\AutorunINFLegacyArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\AutorunINFLegacyArrival", "", RegistryValueKind.String, "MSTakeNoAction")
            },
            requiresElevation: false);
    }
}
