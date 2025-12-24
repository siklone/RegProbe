using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PerformanceTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Performance";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "performance.disable-animations",
                "Disable Visual Effects and Animations",
                "Disables window animations for improved performance.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Control Panel\Desktop\WindowMetrics",
                "MinAnimate",
                RegistryValueKind.String,
                "0",
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "performance.disable-transparency",
                "Disable Window Transparency Effects",
                "Turns off Aero glass transparency for better performance.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "performance.disable-menu-show-delay",
                "Disable Menu Show Delay",
                "Makes menus appear instantly without delay.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Control Panel\Desktop",
                "MenuShowDelay",
                RegistryValueKind.String,
                "0",
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "performance.disable-taskbar-animations",
                "Disable Taskbar Animations",
                "Removes animation effects when opening/closing programs.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "TaskbarAnimations",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "performance.disable-paging-executive",
                "Lock Kernel in RAM (Disable Paging)",
                "Prevents Windows kernel from being paged to disk (requires 4GB+ RAM).",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                "DisablePagingExecutive",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "performance.enable-large-system-cache",
                "Enable Large System Cache",
                "Optimizes memory for server-like workloads with lots of file I/O.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                "LargeSystemCache",
                RegistryValueKind.DWord,
                1)
        };
    }
}
