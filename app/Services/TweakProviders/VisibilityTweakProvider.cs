using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class VisibilityTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "UI & Explorer";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // File Explorer Enhancements
        // Taskbar & Start
        // System Visuals & Animations
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
