using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Power;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class PowerTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Power Management";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Core Power Behavior
        yield return new DisableHibernationTweak(context.ElevatedCommandRunner);
        yield return new DisableUsbSelectiveSuspendTweak(context.ElevatedCommandRunner);
        yield return new DisableCpuCoreParkingTweak(context.ElevatedCommandRunner);

        // Advanced Power Settings
        yield return CreateRegistryValueBatchTweak(
            context,
            "power.optimize-performance",
            "Optimize Power Settings for Performance",
            "Applies multiple power optimizations: disables timer coalescing, deep IO coalescing, core parking latency, and energy estimation for maximum performance.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "CoalescingTimerInterval", RegistryValueKind.DWord, 0, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "DeepIoCoalescingEnabled", RegistryValueKind.DWord, 0, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "EventProcessorEnabled", RegistryValueKind.DWord, 1, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceParked", RegistryValueKind.DWord, 0, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceSoftParked", RegistryValueKind.DWord, 0, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "EnergyEstimationEnabled", RegistryValueKind.DWord, 0, RegistryView.Default)
            },
            requiresElevation: true);

        // Network Power Management
        yield return CreateRegistryValueBatchTweak(
            context,
            "power.disable-network-power-saving",
            "Disable Network Adapter Power Saving",
            "Disables network throttling and optimizes multimedia/network responsiveness. Improves gaming and streaming performance.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DisableTaskOffload", RegistryValueKind.DWord, 0, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", RegistryValueKind.DWord, 0xFFFFFFFF, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", RegistryValueKind.DWord, 10, RegistryView.Default)
            },
            requiresElevation: true);

        // UI Power Options
    }
}
