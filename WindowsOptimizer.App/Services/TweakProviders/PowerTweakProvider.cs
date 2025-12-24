using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class PowerTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Power";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "power.disable-fast-startup",
                "Disable Fast Startup",
                "Disables hybrid shutdown for a cleaner boot process.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                "HiberbootEnabled",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "power.disable-hibernation",
                "Disable Hibernation",
                "Disables hibernation mode and deletes hiberfil.sys to save disk space.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Power",
                "HibernateEnabled",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "power.disable-usb-selective-suspend",
                "Disable USB Selective Suspend",
                "Prevents USB devices from entering power-saving mode.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\USB",
                "DisableSelectiveSuspend",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "power.disable-display-timeout-lock",
                "Disable Display Timeout Auto-Lock",
                "Prevents automatic locking when display turns off.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Policies\Microsoft\Windows\Personalization",
                "NoLockScreen",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false)
        };
    }
}
