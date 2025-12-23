using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Misc;

public static class SevenZipSettingsTweak
{
    /// <summary>
    /// Optimizes 7-Zip context menu settings for better UX
    /// </summary>
    public static RegistryValueBatchTweak CreateOptimize7ZipSettingsTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Cascaded context menu (1 = enabled, 0 = disabled)
            new RegistryValueBatchEntry(
                RegistryHive.CurrentUser,
                RegistryView.Default,
                @"Software\7-Zip\Options",
                "CascadedMenu",
                RegistryValueKind.DWord,
                1),

            // Eliminate duplication of root folders (1 = enabled, 0 = disabled)
            new RegistryValueBatchEntry(
                RegistryHive.CurrentUser,
                RegistryView.Default,
                @"Software\7-Zip\Options",
                "ElimDupExtract",
                RegistryValueKind.DWord,
                1),

            // Icons in context menu (1 = enabled, 0 = disabled)
            new RegistryValueBatchEntry(
                RegistryHive.CurrentUser,
                RegistryView.Default,
                @"Software\7-Zip\Options",
                "MenuIcons",
                RegistryValueKind.DWord,
                1),

            // Propagate Zone.Id stream (delete = No, 1 = Yes, 2 = For Office files)
            new RegistryValueBatchEntry(
                RegistryHive.CurrentUser,
                RegistryView.Default,
                @"Software\7-Zip\Options",
                "WriteZoneIdExtract",
                RegistryValueKind.DWord,
                1)
        };

        return new RegistryValueBatchTweak(
            id: "misc-optimize-7zip-settings",
            name: "Optimize 7-Zip Context Menu Settings",
            description: "Configures 7-Zip with optimal settings: cascaded menu, eliminate duplicate extraction folders, show icons in context menu, and propagate Zone.Id stream for Office files.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }
}
