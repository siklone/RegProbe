using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PeripheralTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Peripherals";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "peripheral.disable-autoplay",
                "Disable AutoPlay",
                "Prevents automatic actions when inserting removable media.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers",
                "DisableAutoplay",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "peripheral.disable-pointer-acceleration",
                "Disable Pointer Acceleration",
                "Disables enhance pointer precision for consistent mouse movement.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Control Panel\Mouse",
                "MouseSpeed",
                RegistryValueKind.String,
                "0",
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "peripheral.disable-keyboard-shortcuts",
                "Disable Windows Key Shortcuts",
                "Prevents accidental triggering of Windows key combinations.",
                TweakRiskLevel.Advanced,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "NoWinKeys",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "audio.disable-exclusive-mode",
                "Disable Audio Exclusive Mode",
                "Prevents applications from taking exclusive control of audio devices.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render",
                "DisableExclusiveMode",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false)
        };
    }
}
