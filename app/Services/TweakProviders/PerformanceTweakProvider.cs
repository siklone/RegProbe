using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Performance;

namespace RegProbe.Application.Services.TweakProviders;

/// <summary>
/// Performance optimization tweaks provider.
/// Sources:
/// - Microsoft PC Performance Tips: https://support.microsoft.com/en-us/windows/tips-to-improve-pc-performance-in-windows-b3b3ef5b-5953-fb6a-2528-4bbed82fba96
/// - Windows 11 Performance Improvements: https://techcommunity.microsoft.com/blog/microsoftmechanicsblog/windows-11-the-optimization-and-performance-improvements/2733299
/// - MMCSS Documentation: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
/// </summary>
public sealed class PerformanceTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Performance";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
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
        yield return CreateServiceStartModeBatchTweak(
            context,
            "power.disable-windows-search",
            "Disable Windows Search",
            "Disables the Windows Search indexing service. This can improve system performance but will slow down file searches. Useful for systems with SSDs where search performance is already fast.",
            TweakRiskLevel.Advanced,
            new[] { "WSearch" },
            ServiceStartMode.Disabled,
            stopRunning: true,
            requiresElevation: true);

    }
}
