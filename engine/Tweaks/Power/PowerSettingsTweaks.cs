using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Power;

public static class PowerSettingsTweaks
{
    /// <summary>
    /// Disables Fast Startup (Hiberboot)
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableFastStartupTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", RegistryValueKind.DWord, 0, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.disable-fast-startup",
            name: "Disable Fast Startup (Hiberboot)",
            description: "Disables Fast Startup so shutdown uses a full kernel teardown instead of reusing the hibernated kernel session on next boot.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }

    /// <summary>
    /// Disables Power Throttling
    /// </summary>
    public static RegistryValueBatchTweak CreateDisablePowerThrottlingTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", RegistryValueKind.DWord, 1, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.disable-power-throttling",
            name: "Disable Power Throttling",
            description: "Disables Windows Power Throttling so background processes are not deprioritized by that policy surface.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }

    /// <summary>
    /// Applies a grouped power-control bundle
    /// </summary>
    public static RegistryValueBatchTweak CreateOptimizePowerSettingsTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Disable coalescing (reduces timer coalescing delays)
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "CoalescingTimerInterval", RegistryValueKind.DWord, 0, RegistryView.Default),

            // Disable deep IO coalescing
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "DeepIoCoalescingEnabled", RegistryValueKind.DWord, 0, RegistryView.Default),

            // Enable event processor
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "EventProcessorEnabled", RegistryValueKind.DWord, 1, RegistryView.Default),

            // Disable parking (let all cores run)
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceParked", RegistryValueKind.DWord, 0, RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceSoftParked", RegistryValueKind.DWord, 0, RegistryView.Default),

            // Energy estimation
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "EnergyEstimationEnabled", RegistryValueKind.DWord, 0, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.optimize-performance",
            name: "Optimize Power Settings for Performance",
            description: "Applies the current grouped power-control bundle for timer coalescing, deep I/O coalescing, parked-core latency, and energy estimation.",
            risk: TweakRiskLevel.Advanced,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
