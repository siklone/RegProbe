using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Power;

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
            description: "Prevents Windows from parking CPU cores, keeping all cores active. Reduces latency and improves responsiveness for gaming and real-time applications.",
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
            description: "Disables CPU idle states and C-States for minimum latency. Increases power consumption but improves responsiveness. Recommended for gaming/audio production.",
            risk: TweakRiskLevel.Advanced,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }

    /// <summary>
    /// Optimizes CPU performance boost behavior
    /// </summary>
    public static RegistryValueBatchTweak CreateOptimizeCPUBoostTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Performance boost settings
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "PerfBoostAtGuaranteed", RegistryValueKind.DWord, 1, RegistryView.Default), // Enable boost at guaranteed frequency

            // High performance duration after boot
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HighPerfDurationBoot", RegistryValueKind.DWord, 120000, RegistryView.Default), // 2 minutes high perf after boot

            // Latency tolerance
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceDefault", RegistryValueKind.DWord, 0, RegistryView.Default), // Minimum latency tolerance

            // Performance calculation
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "PerfCalculateActualUtilization", RegistryValueKind.DWord, 1, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.optimize-cpu-boost",
            name: "Optimize CPU Performance Boost",
            description: "Optimizes CPU boost behavior for better performance. Enables boost at guaranteed frequency, extends high-performance duration, and minimizes latency tolerance.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
