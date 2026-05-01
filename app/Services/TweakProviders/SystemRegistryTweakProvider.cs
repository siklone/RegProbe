using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class SystemRegistryTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "System Registry";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Kernel scheduler (DPC) defaults
        yield return CreateRegistryTweak(
            context,
            "system.kernel-adjust-dpc-threshold",
            "Kernel: Adjust DPC Threshold",
            "Sets the DPC threshold adjustment value to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "AdjustDpcThreshold",
            RegistryValueKind.DWord,
            20);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-ideal-dpc-rate",
            "Kernel: Ideal DPC Rate",
            "Sets the target DPC rate per second to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "IdealDpcRate",
            RegistryValueKind.DWord,
            20);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-minimum-dpc-rate",
            "Kernel: Minimum DPC Rate",
            "Sets the minimum DPC rate threshold to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "MinimumDpcRate",
            RegistryValueKind.DWord,
            3);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-dpc-queue-depth",
            "Kernel: DPC Queue Depth",
            "Sets the maximum DPC queue depth to the documented default.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DpcQueueDepth",
            RegistryValueKind.DWord,
            4);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-dpc-watchdog-period",
            "Kernel: DPC Watchdog Period",
            "Sets the DPC watchdog timeout to the documented default (milliseconds).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DpcWatchdogPeriod",
            RegistryValueKind.DWord,
            120000);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-serialize-timer-expiration",
            "Kernel: Serialize Timer Expiration",
            "Enables timer serialization using the documented default value.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "SerializeTimerExpiration",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-cache-aware-scheduling",
            "Kernel: Cache-Aware Scheduling",
            "Restores Windows' documented cache-aware scheduling default so the scheduler stays aligned with normal CPU cache topology behavior.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "CacheAwareScheduling",
            RegistryValueKind.DWord,
            47);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-default-dynamic-hetero-cpu-policy",
            "Kernel: Default Dynamic Hetero CPU Policy",
            "Returns Windows' hybrid CPU scheduling policy to the documented default used for heterogeneous core systems.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DefaultDynamicHeteroCpuPolicy",
            RegistryValueKind.DWord,
            3);

        yield return CreateRegistryTweak(
            context,
            "system.kernel-disable-low-qos-timer-resolution",
            "Kernel: Disable Low QoS Timer Resolution",
            "Disables low QoS timer resolution using the documented default value.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            "DisableLowQosTimerResolution",
            RegistryValueKind.DWord,
            1);

        // Graphics driver defaults (TDR + overlays)
        yield return CreateRegistryTweak(
            context,
            "system.graphics-disable-overlays",
            "Graphics: Disable Overlay Planes",
            "Disables overlay planes to reduce composition issues in some configurations.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "DisableOverlays",
            RegistryValueKind.DWord,
            1);

        // Desktop Window Manager
        yield return CreateRegistryTweak(
            context,
            "system.dwm-disable-mpo",
            "DWM: Disable Multiplane Overlay (MPO)",
            "Disables MPO to avoid flicker or driver issues on some systems.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\Dwm",
            "OverlayTestMode",
            RegistryValueKind.DWord,
            5);

        yield return CreateRegistryTweak(
            context,
            "system.graphics-page-fault-debug-mode",
            "Graphics: Page Fault Debug Mode",
            "Restores the graphics page-fault debug mode value Windows expects by default. Useful when undoing manual scheduler experiments.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            "PageFaultDebugMode",
            RegistryValueKind.DWord,
            1);

    }
}
