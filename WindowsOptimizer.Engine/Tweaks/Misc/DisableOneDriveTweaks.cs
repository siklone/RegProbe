using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Misc;

public static class DisableOneDriveTweaks
{
    public static RegistryValueBatchTweak CreateDisableOneDriveTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>
        {
            // Disable OneDrive as default save location
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\OneDrive", "DisableLibrariesDefaultSaveToOneDrive", RegistryValueKind.DWord, 1, RegistryView.Default),

            // Disable OneDrive file sync (Windows 8.1)
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\OneDrive", "DisableFileSync", RegistryValueKind.DWord, 1, RegistryView.Default),

            // Disable OneDrive file sync (Next-Gen Sync Client)
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", RegistryValueKind.DWord, 1, RegistryView.Default),

            // Block syncing on metered connections
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\OneDrive", "DisableMeteredNetworkFileSync", RegistryValueKind.DWord, 0, RegistryView.Default),

            // Prevent network traffic before user sign-in
            new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\OneDrive", "PreventNetworkTrafficPreUserSignIn", RegistryValueKind.DWord, 1, RegistryView.Default),

            // Hide OneDrive from File Explorer (CLSID 1)
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", "System.IsPinnedToNameSpaceTree", RegistryValueKind.DWord, 0, RegistryView.Default),

            // Hide OneDrive from File Explorer (CLSID 2)
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Classes\Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", "System.IsPinnedToNameSpaceTree", RegistryValueKind.DWord, 0, RegistryView.Default)
        };

        return new RegistryValueBatchTweak(
            id: "misc-disable-onedrive",
            name: "Disable OneDrive",
            description: "Completely disables OneDrive sync, hides it from File Explorer, and prevents network traffic before user sign-in.",
            risk: TweakRiskLevel.Advanced,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
