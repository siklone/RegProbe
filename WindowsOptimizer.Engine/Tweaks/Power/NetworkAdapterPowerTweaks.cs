using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Power;

public static class NetworkAdapterPowerTweaks
{
    /// <summary>
    /// Disables power saving features for network adapters
    /// This is a simplified version - full implementation would enumerate all adapters
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableNetworkAdapterPowerSavingTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Global network adapter power settings
            // Note: Full implementation would enumerate HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}
            // and apply settings to each adapter instance

            // Common Intel adapter settings
            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                "DisableTaskOffload",
                RegistryValueKind.DWord,
                0), // 0 = enable offload (better performance)

            // Disable network throttling
            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "NetworkThrottlingIndex",
                RegistryValueKind.DWord,
                0xFFFFFFFF), // Disable throttling

            // System responsiveness (audio/multimedia priority)
            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "SystemResponsiveness",
                RegistryValueKind.DWord,
                0) // 0 = best responsiveness (default 20)
        };

        return new RegistryValueBatchTweak(
            id: "power-disable-network-power-saving",
            name: "Disable Network Adapter Power Saving",
            description: "Disables network throttling and optimizes multimedia/network responsiveness. Improves gaming and streaming performance.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }

    /// <summary>
    /// Optimizes gaming network settings
    /// </summary>
    public static RegistryValueBatchTweak CreateOptimizeGamingNetworkTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Game priority boost
            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                "Priority",
                RegistryValueKind.DWord,
                8), // High priority

            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                "Scheduling Category",
                RegistryValueKind.String,
                "High"),

            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                "SFIO Priority",
                RegistryValueKind.String,
                "High"),

            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                "GPU Priority",
                RegistryValueKind.DWord,
                8),

            new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                "Affinity",
                RegistryValueKind.DWord,
                0)
        };

        return new RegistryValueBatchTweak(
            id: "power-optimize-gaming-network",
            name: "Optimize Gaming Network Settings",
            description: "Boosts priority for gaming tasks, improving network latency and frame timing. Sets high priority for GPU, scheduling, and I/O.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
