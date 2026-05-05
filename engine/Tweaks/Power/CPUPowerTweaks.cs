using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Power;

public static class CPUPowerTweaks
{
    /// <summary>
    /// Disables CPU parking for all cores
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableCPUParkingTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Disable parking algorithm
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceParked", RegistryValueKind.DWord, 0, RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceSoftParked", RegistryValueKind.DWord, 0, RegistryView.Default),

            // Initial unpark count (max cores active)
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "Class1InitialUnparkCount", RegistryValueKind.DWord, 100, RegistryView.Default), // High value keeps cores unparked

            // Multipark granularity
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "MultiparkGranularity", RegistryValueKind.DWord, 100, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.disable-cpu-parking",
            name: "Disable CPU Core Parking",
            description: "Writes the core-parking-related Control\\Power registry values used by this tweak, including LatencyToleranceParked, LatencyToleranceSoftParked, Class1InitialUnparkCount, and MultiparkGranularity.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }

    /// <summary>
    /// Disables C-States and idle states for minimum latency
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableIdleStatesTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Disable idle states at boot
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "DisableIdleStatesAtBoot", RegistryValueKind.DWord, 1, RegistryView.Default),

            // Reduce idle state timeout
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "IdleStateTimeout", RegistryValueKind.DWord, 0, RegistryView.Default), // Minimal timeout

            // Exit latency check (disable deep sleep)
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "ExitLatencyCheckEnabled", RegistryValueKind.DWord, 1, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.disable-cpu-idle-states",
            name: "Disable CPU Idle States (C-States)",
            description: "Writes the idle-state-related Control\\Power registry values used by this tweak. This changes power behavior and can increase power draw.",
            risk: TweakRiskLevel.Advanced,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }

    /// <summary>
    /// Sets the legacy CPU boost-related registry bundle used by the shipped tweak.
    /// </summary>
    public static RegistryValueBatchTweak CreateOptimizeCPUBoostTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // CPU boost-related settings
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "PerfBoostAtGuaranteed", RegistryValueKind.DWord, 1, RegistryView.Default), // Boost at guaranteed frequency

            // High-boost duration after boot
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HighPerfDurationBoot", RegistryValueKind.DWord, 120000, RegistryView.Default), // 2 minutes after boot

            // Latency tolerance
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceDefault", RegistryValueKind.DWord, 0, RegistryView.Default), // Lowest tolerance

            // Utilization calculation
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "PerfCalculateActualUtilization", RegistryValueKind.DWord, 1, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.optimize-cpu-boost",
            name: "Apply CPU Boost Registry Bundle",
            description: "Writes the CPU boost-related Control\\Power registry values used by this tweak.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
