using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;

namespace RegProbe.Application.Services.TweakProviders;

/// <summary>
/// Provides developer-focused tweaks for Visual Studio, Git, .NET, and other development tools.
/// Sources:
/// - Microsoft Windows Developer Documentation: https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation
/// - Git Documentation: https://git-scm.com/docs/git-config
/// - Visual Studio Performance Guide: https://learn.microsoft.com/en-us/visualstudio/ide/optimize-visual-studio-performance
/// - .NET SDK Documentation: https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry
/// </summary>
public sealed class DeveloperTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Developer Tools";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Visual Studio IntelliSense Cache Optimization
        yield return CreateRegistryTweak(
            context,
            "developer.vs-intellisense-cache",
            "VS IntelliSense DisableAutoUpdating Setting",
            "Writes the Visual Studio DisableAutoUpdating IntelliSense value used by this tweak.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\VisualStudio\IntelliSense",
            "DisableAutoUpdating",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

        // Windows Terminal Developer Mode
        yield return CreateRegistryValueSetTweak(
            context,
            "developer.terminal-dev-mode",
            "Enable Windows Terminal Developer Features",
            "Enables advanced features in Windows Terminal like debug tap and developer mode settings.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows Terminal",
            new[]
            {
                new RegistryValueSetEntry("DeveloperMode", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("EnableDebugTap", RegistryValueKind.DWord, 1)
            },
            requiresElevation: false);

        // Visual Studio Code Git Autofetch Disable
        yield return CreateRegistryTweak(
            context,
            "developer.vscode-git-autofetch",
            "Disable VS Code Git Autofetch",
            "Disables automatic Git fetching in VS Code to reduce network usage and CPU spikes.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            "DisableGitAutofetch",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        // Visual Studio Solution Load Performance
        yield return CreateRegistryTweak(
            context,
            "developer.vs-solution-load",
            "VS Solution BackgroundAnalysis Setting",
            "Writes the Visual Studio BackgroundAnalysis solution-loading value used by this tweak.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\VisualStudio\SolutionLoading",
            "BackgroundAnalysis",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);

    }
}
