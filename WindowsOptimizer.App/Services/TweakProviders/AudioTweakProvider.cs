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
    }
}
