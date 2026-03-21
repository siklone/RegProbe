using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Developer;

namespace WindowsOptimizer.App.Services.TweakProviders;

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
        // Windows Long Paths
        // Source: https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation
        yield return CreateRegistryTweak(
            context,
            "developer.enable-windows-long-paths",
            "Enable Windows Long Paths",
            "Enables the Windows long-path prerequisite for compatible applications, including development tools that work with deep directory trees. Source: Microsoft Windows Developer Documentation",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\FileSystem",
            "LongPathsEnabled",
            RegistryValueKind.DWord,
            1);

        // .NET SDK Telemetry Disable
        // Source: https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry
        yield return CreateRegistryTweak(
            context,
            "developer.dotnet-telemetry-disable",
            "Disable .NET SDK Telemetry",
            "Stops .NET SDK from sending usage data to Microsoft. Source: Microsoft .NET SDK Documentation",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Environment",
            "DOTNET_CLI_TELEMETRY_OPTOUT",
            RegistryValueKind.String,
            "1",
            requiresElevation: false);

        // Visual Studio IntelliSense Cache Optimization
        yield return CreateRegistryTweak(
            context,
            "developer.vs-intellisense-cache",
            "Optimize VS IntelliSense Cache",
            "Increases Visual Studio IntelliSense cache size for better code completion performance in large projects.",
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

        // Node.js Performance Optimization
        yield return CreateRegistryTweak(
            context,
            "developer.nodejs-performance",
            "Optimize Node.js Performance",
            "Increases Node.js memory limit and enables performance optimizations for large JavaScript projects.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            "NODE_OPTIONS",
            RegistryValueKind.String,
            "--max-old-space-size=8192",
            requiresElevation: true);

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

        // Python Path Configuration
        yield return CreateRegistryTweak(
            context,
            "developer.python-path-fix",
            "Fix Python Path Length Issues",
            "Ensures Python can handle long paths on Windows, preventing import errors in deep directory structures.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\FileSystem",
            "LongPathsEnabled",
            RegistryValueKind.DWord,
            1);

        // Windows Developer Mode
        // Source: https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development
        yield return CreateRegistryTweak(
            context,
            "developer.windows-dev-mode",
            "Enable Windows Developer Mode",
            "Enables Windows Developer Mode for sideloading apps and accessing advanced development features. Source: Microsoft Windows Developer Documentation",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
            "AllowDevelopmentWithoutDevLicense",
            RegistryValueKind.DWord,
            1);

        // Docker Desktop Performance
        yield return new EnableDockerWsl2BackendTweak();

        // PowerShell Execution Policy
        // Source: https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy
        yield return CreateRegistryTweak(
            context,
            "developer.powershell-execution",
            "Allow Local PowerShell Scripts",
            "Sets PowerShell execution policy to RemoteSigned, allowing local scripts to run while requiring signatures for remote scripts. Source: Microsoft PowerShell Documentation",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\PowerShell",
            "ExecutionPolicy",
            RegistryValueKind.String,
            "RemoteSigned");

        // SSH Agent Auto-start
        yield return CreateRegistryTweak(
            context,
            "developer.ssh-agent-autostart",
            "Enable SSH Agent Auto-start",
            "Automatically starts SSH agent on login for seamless Git SSH key authentication.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "SSH Agent",
            RegistryValueKind.String,
            @"C:\Windows\System32\OpenSSH\ssh-agent.exe",
            requiresElevation: false);

        // WSL2 Memory Optimization
        // Source: https://learn.microsoft.com/en-us/windows/wsl/wsl-config
        yield return new SetWsl2MemoryLimitTweak();

        // Visual Studio Solution Load Performance
        yield return CreateRegistryTweak(
            context,
            "developer.vs-solution-load",
            "Speed Up Visual Studio Solution Load",
            "Disables background solution load analysis for faster Visual Studio startup on large solutions.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\VisualStudio\SolutionLoading",
            "BackgroundAnalysis",
            RegistryValueKind.DWord,
            0,
            requiresElevation: false);
    }
}
