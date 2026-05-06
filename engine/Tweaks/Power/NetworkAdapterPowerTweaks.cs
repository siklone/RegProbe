using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Power;

public static class NetworkAdapterPowerTweaks
{
    /// <summary>
    /// Writes the narrowed TCP/IP offload and MMCSS values that have record-backed semantics.
    /// </summary>
    public static RegistryValueBatchTweak CreateDisableNetworkAdapterPowerSavingTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DisableTaskOffload", RegistryValueKind.DWord, 0, RegistryView.Default),
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", RegistryValueKind.DWord, 10, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.disable-network-power-saving.policy",
            name: "Network Power and Multimedia Responsiveness",
            description: "Writes the documented DisableTaskOffload and MMCSS SystemResponsiveness values while excluding the archived opaque NetworkThrottlingIndex write.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }

    /// <summary>
    /// Writes the current MMCSS Games task profile bundle
    /// </summary>
    public static RegistryValueBatchTweak CreateOptimizeGamingNetworkTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Game priority boost
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", RegistryValueKind.DWord, 8, RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", RegistryValueKind.String, "High", RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", RegistryValueKind.String, "High", RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", RegistryValueKind.DWord, 8, RegistryView.Default),

            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Affinity", RegistryValueKind.DWord, 0, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "power.optimize-gaming-network",
            name: "Set MMCSS Games Task Profile",
            description: "Writes the MMCSS Games task-profile values for priority, scheduling category, SFIO priority, GPU priority, and affinity.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
