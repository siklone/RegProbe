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
            "explorer.show-protected-operating-system-files",
            "Show Protected Operating System Files",
            "Shows protected operating system files in File Explorer. Best kept for advanced troubleshooting.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "ShowSuperHidden",
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
            "explorer.enable-explorer-compact-mode",
            "Enable Compact View",
            "Reduces the spacing between items in File Explorer for a more information-dense view.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "UseCompactMode",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-status-bar",
            "Show Explorer Status Bar",
            "Shows the File Explorer status bar at the bottom of Explorer windows.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "ShowStatusBar",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-info-tips",
            "Show Explorer Info Tips",
            "Shows pop-up descriptions for folders and desktop items in File Explorer.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "ShowInfoTip",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-type-overlay",
            "Display File Icons on Thumbnails",
            "Shows the file-type icon overlay on thumbnail previews in File Explorer.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "ShowTypeOverlay",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.always-show-icons-never-thumbnails",
            "Always Show Icons, Never Thumbnails",
            "Uses file icons instead of thumbnail previews in File Explorer.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "IconsOnly",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-compressed-and-encrypted-files-in-color",
            "Show Compressed and Encrypted Files in Color",
            "Uses special filename colors for compressed and encrypted NTFS files in File Explorer.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "ShowCompColor",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-drive-letters-first",
            "Show Drive Letters First",
            "Shows drive letters more prominently in File Explorer drive labels.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer",
            "ShowDriveLettersFirst",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.launch-folder-windows-in-a-separate-process",
            "Launch Folder Windows in a Separate Process",
            "Runs File Explorer folder windows in a separate process for isolation and troubleshooting.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "SeparateProcess",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.hide-empty-drives",
            "Hide Empty Drives",
            "Hides drives with no media present from File Explorer views such as This PC.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "HideDrivesWithNoMedia",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "explorer.show-recent-items",
            "Show Recent Items In Home",
            "Shows recently used files in the Home section of File Explorer.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer",
            "ShowRecent",
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
            "Hide Taskbar Chat Icon",
            "Hides the Microsoft Teams Chat icon from the taskbar by default.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Windows Chat",
            "ChatIcon",
            RegistryValueKind.DWord,
            2);

        yield return CreateRegistryTweak(
            context,
            "explorer.disable-low-disk-space-warning",
            "Disable Low Disk Space Warning",
            "Turns off the low disk space warning notification.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            "NoLowDiskSpaceChecks",
            RegistryValueKind.DWord,
            1);

        // System Visuals & Animations
        yield return CreateRegistryTweak(
            context,
            "visibility.disable-common-control-animations",
            "Disable Common Control Animations",
            "Turns off common control and window animations for the current user.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            "TurnOffSPIAnimations",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-window-animations",
            "Disable Window Animations",
            "Disables window animations like minimize and restore.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\DWM",
            "DisallowAnimations",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.default-account-picture",
            "Use Default Account Picture",
            "Forces the default account picture for all users on this device.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            "UseDefaultTile",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-wcn-wizards",
            "Disable Windows Connect Now Wizards",
            "Disables Windows Connect Now setup wizards for wireless and device setup.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\WCN\UI",
            "DisableWcnUi",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-first-signin-animation",
            "Disable First Sign-In Animation",
            "Skips the first sign-in animation and Microsoft account opt-in prompt.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
            "EnableFirstLogonAnimation",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "visibility.hide-language-bar",
            "Hide Language Bar",
            "Hides the language bar UI for the current user.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\CTF\LangBar",
            "ShowStatus",
            RegistryValueKind.DWord,
            3,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-widgets",
            "Disable Widgets (Policy)",
            "Disables the Widgets/News and Interests feature via policy.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Dsh",
            "AllowNewsAndInterests",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "visibility.hide-most-used-apps",
            "Hide Most Used Apps",
            "Forces the Start menu Most used list to stay hidden.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "ShowOrHideMostUsedApps",
            RegistryValueKind.DWord,
            2);

        yield return CreateRegistryTweak(
            context,
            "visibility.hide-people-bar",
            "Hide People Bar",
            "Removes the People Bar from the taskbar.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "HidePeopleBar",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        // Spotlight & Cloud Content
        yield return CreateRegistryTweak(
            context,
            "visibility.disable-spotlight-features",
            "Disable Windows Spotlight Features",
            "Turns off Windows Spotlight features for the current user.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CloudContent",
            "DisableWindowsSpotlightFeatures",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-spotlight-welcome",
            "Disable Windows Spotlight Welcome Experience",
            "Disables the Windows Spotlight welcome experience.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CloudContent",
            "DisableWindowsSpotlightWindowsWelcomeExperience",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-spotlight-action-center",
            "Disable Windows Spotlight on Action Center",
            "Stops Windows Spotlight notifications in Action Center.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CloudContent",
            "DisableWindowsSpotlightOnActionCenter",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-spotlight-settings",
            "Disable Windows Spotlight on Settings",
            "Stops Windows Spotlight suggestions in Settings.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CloudContent",
            "DisableWindowsSpotlightOnSettings",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-spotlight-desktop-collection",
            "Disable Spotlight Collection on Desktop",
            "Removes the Spotlight collection option for desktop backgrounds.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CloudContent",
            "DisableSpotlightCollectionOnDesktop",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-spotlight-third-party",
            "Disable Spotlight Third-Party Suggestions",
            "Stops Windows Spotlight from suggesting third-party content.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\CloudContent",
            "DisableThirdPartySuggestions",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        // Lock Screen
        yield return CreateRegistryTweak(
            context,
            "visibility.disable-lock-screen",
            "Disable Lock Screen",
            "Skips the lock screen and goes directly to the sign-in screen.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Personalization",
            "NoLockScreen",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-lock-screen-camera",
            "Disable Lock Screen Camera",
            "Prevents the lock screen camera from being invoked.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Personalization",
            "NoLockScreenCamera",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-lock-screen-slideshow",
            "Disable Lock Screen Slideshow",
            "Prevents lock screen slideshows from running.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Personalization",
            "NoLockScreenSlideshow",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-lock-screen-motion",
            "Disable Lock Screen Background Motion",
            "Stops the subtle motion effect on the lock screen background.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Personalization",
            "AnimateLockScreenBackground",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-lock-screen-changes",
            "Prevent Changing Lock Screen",
            "Prevents users from changing the lock screen image.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Personalization",
            "NoChangingLockScreen",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.disable-acrylic-logon",
            "Disable Acrylic Logon Background",
            "Disables the acrylic blur on the logon background image.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            "DisableAcrylicBackgroundOnLogon",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "visibility.restore-classic-context-menu",
            "Restore Classic Context Menu",
            "Restores the Windows 10 style context menu on Windows 11.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
            "",
            RegistryValueKind.String,
            "",
            requiresElevation: false);

        yield return CreateRegistryTweak(
            context,
            "visibility.force-classic-control-panel",
            "Force Classic Control Panel View",
            "Always open Control Panel in the icon view.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            "ForceClassicControlPanel",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);
    }
}
