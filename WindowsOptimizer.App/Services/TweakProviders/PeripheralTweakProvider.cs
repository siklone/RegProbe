using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Misc;

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
    }
}
