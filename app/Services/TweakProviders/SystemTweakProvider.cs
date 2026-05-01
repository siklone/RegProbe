using System;
using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Cleanup;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class SystemTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "System";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Core System Behavior
        // Appearance & Explorer
        // Mass Disablements
        yield return CreateRegistryTweak(
            context,
            "system.disable-service-splitting",
            "Disable Service Splitting",
            "Prevents services from being split into separate svchost processes.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Control",
            "SvcHostSplitThresholdInKB",
            RegistryValueKind.DWord,
            -1);

        // Command-based System Tweaks
        yield return new ClearEventLogsTweak(context.ElevatedCommandRunner, "System");
    }
}
