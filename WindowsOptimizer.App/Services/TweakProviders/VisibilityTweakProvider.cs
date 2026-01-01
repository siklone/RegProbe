using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class VisibilityTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "UI & Explorer";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // File Explorer Enhancements
        yield return CreateRegistryTweak(
            context,
            "explorer.show-hidden-files",
            "Show Hidden Files and Folders",
            "Configures File Explorer to show files and folders marked with the hidden attribute.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "Hidden",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-file-extensions",
            "Show File Extensions",
            "Shows the file extension (e.g., .txt, .exe) for all known file types in File Explorer.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "HideFileExt",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-full-path",
            "Show Full Path in Title Bar",
            "Displays the complete directory path in the title bar of File Explorer windows.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState",
            "FullPath",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.disable-compact-mode",
            "Enable Compact View",
            "Reduces the spacing between items in File Explorer for a more information-dense view.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "UseCompactMode",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        // Taskbar & Start
        yield return CreateRegistryTweak(
            context,
            "explorer.taskbar-alignment-left",
            "Align Taskbar to Left",
            "Moves the Taskbar icons and Start button to the traditional left alignment (Windows 11).",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "TaskbarAl",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.disable-taskbar-chat",
            "Disable Taskbar Chat Icon",
            "Removes the Microsoft Teams Chat icon from the taskbar.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "TaskbarMn",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.disable-taskbar-widgets",
            "Disable Taskbar Widgets Icon",
            "Removes the Widgets/News and Interests icon from the taskbar.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "TaskbarDa",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.disable-low-disk-space-warning",
            "Disable Low Disk Space Warning",
            "Turns off the low disk space warning notification.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            "NoLowDiskSpaceChecks",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);
    }
}
