using System.Collections.Generic;
using RegProbe.Core;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks.Peripheral;

namespace RegProbe.Application.Services.TweakProviders;

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

    }
}
