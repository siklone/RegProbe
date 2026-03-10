using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Power;

namespace WindowsOptimizer.App.Services.TweakProviders;

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
            "power.disable-modern-standby",
            "Disable Modern Standby",
            "Disables Modern Standby (S0 Low Power Idle) and switches to traditional S3 sleep mode. Improves desktop power behavior.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "MSDisabled", RegistryValueKind.DWord, 1, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\ModernSleep", "EnabledActions", RegistryValueKind.DWord, 0, RegistryView.Default)
            },
            requiresElevation: true);

        yield return CreateRegistryValueBatchTweak(
            context,
            "power.disable-fast-startup",
            "Disable Fast Startup (Hiberboot)",
            "Disables Fast Startup feature which uses hibernation for faster boot times. Fixes some driver and dual-boot issues.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HiberbootEnabled", RegistryValueKind.DWord, 0, RegistryView.Default)
            },
            requiresElevation: true);

        yield return CreateRegistryValueBatchTweak(
            context,
            "power.disable-power-throttling",
            "Disable Power Throttling",
            "Disables Windows Power Throttling which limits background process performance. Improves overall system responsiveness.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", RegistryValueKind.DWord, 1, RegistryView.Default)
            },
            requiresElevation: true);

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

        // CPU Performance Management
        yield return CreateRegistryValueBatchTweak(
            context,
            "power.disable-cpu-idle-states",
            "Disable CPU Idle States (C-States)",
            "Disables CPU idle states and C-States for minimum latency. Increases power consumption but improves responsiveness. Recommended for gaming/audio production.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "DisableIdleStatesAtBoot", RegistryValueKind.DWord, 1, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "IdleStateTimeout", RegistryValueKind.DWord, 0, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "ExitLatencyCheckEnabled", RegistryValueKind.DWord, 1, RegistryView.Default)
            },
            requiresElevation: true);

        yield return CreateRegistryValueBatchTweak(
            context,
            "power.optimize-cpu-boost",
            "Optimize CPU Performance Boost",
            "Optimizes CPU boost behavior for better performance. Enables boost at guaranteed frequency, extends high-performance duration, and minimizes latency tolerance.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "PerfBoostAtGuaranteed", RegistryValueKind.DWord, 1, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HighPerfDurationBoot", RegistryValueKind.DWord, 120000, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "LatencyToleranceDefault", RegistryValueKind.DWord, 0, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "PerfCalculateActualUtilization", RegistryValueKind.DWord, 1, RegistryView.Default)
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

        yield return CreateRegistryValueBatchTweak(
            context,
            "power.optimize-gaming-network",
            "Optimize Gaming Network Settings",
            "Boosts priority for gaming tasks, improving network latency and frame timing. Sets high priority for GPU, scheduling, and I/O.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", RegistryValueKind.DWord, 8, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", RegistryValueKind.String, "High", RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", RegistryValueKind.String, "High", RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", RegistryValueKind.DWord, 8, RegistryView.Default),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Affinity", RegistryValueKind.DWord, 0, RegistryView.Default)
            },
            requiresElevation: true);

        // UI Power Options
        yield return CreateRegistryTweak(
            context,
            "power.hide-lock-option",
            "Hide Lock Power Option",
            "Hides the Lock option from the power menu.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "ShowLockOption",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "power.hide-sleep-option",
            "Hide Sleep Power Option",
            "Hides the Sleep option from the power menu.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "ShowSleepOption",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "power.hide-hibernate-option",
            "Hide Hibernate Power Option",
            "Hides the Hibernate option from the power menu.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "ShowHibernateOption",
            RegistryValueKind.DWord,
            0);
    }
}
