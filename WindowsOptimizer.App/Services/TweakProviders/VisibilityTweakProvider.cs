using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class VisibilityTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Visibility & Explorer";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "explorer.show-file-extensions",
                "Show File Extensions",
                "Displays file extensions for all known file types in Explorer.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "HideFileExt",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "explorer.show-hidden-files",
                "Show Hidden Files and Folders",
                "Makes hidden files and folders visible in Explorer.",
                TweakRiskLevel.Advanced,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "Hidden",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "explorer.show-system-files",
                "Show Protected Operating System Files",
                "Displays protected system files in Explorer (use with caution).",
                TweakRiskLevel.Risky,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "ShowSuperHidden",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "explorer.show-full-path-title-bar",
                "Show Full Path in Title Bar",
                "Displays the complete folder path in Explorer's title bar.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState",
                "FullPath",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.show-all-tray-icons",
                "Show All System Tray Icons",
                "Prevents Windows from hiding system tray icons.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer",
                "EnableAutoTray",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-taskbar-thumbnails",
                "Disable Taskbar Thumbnails",
                "Disables preview thumbnails when hovering over taskbar items.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "DisablePreviewDesktop",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.show-seconds-in-clock",
                "Show Seconds in System Clock",
                "Displays seconds in the taskbar clock.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "ShowSecondsInSystemClock",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-shake-to-minimize",
                "Disable Aero Shake",
                "Prevents windows from minimizing when you shake the active window.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Policies\Microsoft\Windows\Explorer",
                "NoWindowMinimizingShortcuts",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.remove-3d-objects-folder",
                "Remove 3D Objects from This PC",
                "Hides the 3D Objects folder from File Explorer.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}",
                "(Default)",
                RegistryValueKind.String,
                ""),

            CreateRegistryTweak(
                context,
                "visibility.remove-music-folder",
                "Remove Music from This PC",
                "Hides the Music folder from This PC view.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}",
                "(Default)",
                RegistryValueKind.String,
                ""),

            CreateRegistryTweak(
                context,
                "visibility.remove-videos-folder",
                "Remove Videos from This PC",
                "Hides the Videos folder from This PC view.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}",
                "(Default)",
                RegistryValueKind.String,
                ""),

            CreateRegistryTweak(
                context,
                "visibility.show-this-pc-on-desktop",
                "Show 'This PC' on Desktop",
                "Adds This PC icon to the desktop.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel",
                "{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.show-user-folder-on-desktop",
                "Show User Folder on Desktop",
                "Adds your user folder icon to the desktop.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel",
                "{59031a47-3f72-44a7-89c5-5595fe6b30ee}",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.show-recycle-bin-on-desktop",
                "Show Recycle Bin on Desktop",
                "Ensures Recycle Bin icon is visible on desktop.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel",
                "{645FF040-5081-101B-9F08-00AA002F954E}",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-search-highlights",
                "Disable Taskbar Search Highlights",
                "Removes dynamic content from the taskbar search box.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\SearchSettings",
                "IsDynamicSearchBoxEnabled",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-news-and-interests",
                "Disable News and Interests (Widgets)",
                "Removes the news widget from the taskbar.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Feeds",
                "ShellFeedsTaskbarViewMode",
                RegistryValueKind.DWord,
                2,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-meet-now",
                "Disable Meet Now Icon",
                "Removes the Meet Now video chat icon from system tray.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "HideSCAMeetNow",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-people-bar",
                "Disable People Bar on Taskbar",
                "Removes the People icon from the taskbar.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People",
                "PeopleBand",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-task-view-button",
                "Hide Task View Button",
                "Removes the Task View button from the taskbar.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "ShowTaskViewButton",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-cortana-button",
                "Hide Cortana Button",
                "Removes the Cortana/Search button from the taskbar.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "ShowCortanaButton",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.show-file-checkboxes",
                "Enable File Checkboxes in Explorer",
                "Adds checkboxes to files and folders for easier multi-selection.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "AutoCheckSelect",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "visibility.disable-recent-files-quick-access",
                "Disable Recent Files in Quick Access",
                "Prevents Quick Access from showing recently used files.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer",
                "ShowRecent",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false)
        };
    }
}
