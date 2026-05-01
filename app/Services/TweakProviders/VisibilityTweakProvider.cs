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

    }
}
