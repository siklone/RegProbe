using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Performance;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PerformanceTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Performance";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Visual Effects for Performance
        yield return CreateRegistryTweak(
            context,
            "performance.disable-animations",
            "Disable Window Animations",
            "Disables window animations to make the UI feel snappier and improve responsiveness on lower-end hardware.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Control Panel\Desktop\WindowMetrics",
            "MinAnimate",
            RegistryValueKind.String,
            "0",
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "performance.disable-menu-show-delay",
            "Remove Menu Show Delay",
            "Removes the artificial delay when showing menus for a more responsive feel.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Control Panel\Desktop",
            "MenuShowDelay",
            RegistryValueKind.String,
            "0",
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "performance.disable-taskbar-animations",
            "Disable Taskbar Animations",
            "Disables taskbar animations for a slight performance boost.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "TaskbarAnimations",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        // Subsystem Performance
        yield return new DisableSuperfetchTweak(context.ElevatedCommandRunner);
        yield return new DisableWindowsSearchTweak(context.ElevatedCommandRunner);

        yield return CreateRegistryTweak(
            context,
            "performance.disable-background-apps",
            "Disable Power Throttling",
            "Disables Windows Power Throttling to ensure apps get full CPU performance even when in background.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Power",
            "PowerThrottlingOff",
            RegistryValueKind.DWord,
            1);
    }
}
