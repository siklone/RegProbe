using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Power;
using WindowsOptimizer.Engine.Tweaks.Power;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PowerTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Power Management";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Core Power Behavior
        yield return new DisableHibernationTweak(context.ElevatedCommandRunner);
        yield return new DisableUsbSelectiveSuspendTweak(context.ElevatedCommandRunner);

        // Advanced Power Settings (via Registry Helpers)
        yield return PowerSettingsTweaks.CreateDisableModernStandbyTweak(context.ElevatedRegistry);
        yield return PowerSettingsTweaks.CreateDisableFastStartupTweak(context.ElevatedRegistry);
        yield return PowerSettingsTweaks.CreateDisablePowerThrottlingTweak(context.ElevatedRegistry);
        yield return PowerSettingsTweaks.CreateOptimizePowerSettingsTweak(context.ElevatedRegistry);

        // CPU Performance Management
        yield return CPUPowerTweaks.CreateDisableCPUParkingTweak(context.ElevatedRegistry);
        yield return CPUPowerTweaks.CreateDisableIdleStatesTweak(context.ElevatedRegistry);
        yield return CPUPowerTweaks.CreateOptimizeCPUBoostTweak(context.ElevatedRegistry);

        // Network Power Management
        yield return NetworkAdapterPowerTweaks.CreateDisableNetworkAdapterPowerSavingTweak(context.ElevatedRegistry);
        yield return NetworkAdapterPowerTweaks.CreateOptimizeGamingNetworkTweak(context.ElevatedRegistry);
    }
}
