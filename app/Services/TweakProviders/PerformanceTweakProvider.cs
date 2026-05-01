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
        // Subsystem Performance
        yield return new DisableSuperfetchTweak(context.ElevatedCommandRunner);

    }
}
