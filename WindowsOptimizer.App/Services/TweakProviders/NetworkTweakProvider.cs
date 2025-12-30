using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Misc;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class NetworkTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Network";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Topology & Discovery
        yield return CreateRegistryTweak(
            context,
            "network.enable-lltdio",
            "Enable LLTD Mapper I/O",
            "Enables the Link-Layer Topology Discovery Mapper I/O driver for network mapping.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\LLTD",
            "EnableLLTDIO",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "network.enable-lltd-responder",
            "Enable LLTD Responder",
            "Enables the Link-Layer Topology Discovery Responder driver for discovery by other PCs.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\LLTD",
            "EnableRspndr",
            RegistryValueKind.DWord,
            1);

        // Optimization
        yield return CreateRegistryValueSetTweak(
            context,
            "network.optimize-smb",
            "Optimize SMB Performance",
            "Enables SMB multichannel and optimizes cache lifetimes for network file sharing.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters",
            new[]
            {
                new RegistryValueSetEntry("DisableBandwidthThrottling", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("FileInfoCacheLifetime", RegistryValueKind.DWord, 30),
                new RegistryValueSetEntry("DirectoryCacheLifetime", RegistryValueKind.DWord, 30)
            });

        // Command-based Network Tweaks
        yield return new FlushDnsCacheTweak(context.ElevatedCommandRunner);
        yield return new ResetNetworkStackTweak(context.ElevatedCommandRunner);

        // Security
        yield return CreateRegistryTweak(
            context,
            "network.disable-ipv6",
            "Disable IPv6",
            "Disables IPv6 protocol system-wide. May cause issues with some modern network apps.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters",
            "DisabledComponents",
            RegistryValueKind.DWord,
            0xFF);

        yield return CreateRegistryTweak(
            context,
            "network.disable-netbios",
            "Disable NetBIOS over TCP/IP",
            "Disables the legacy NetBIOS protocol to modernize the network stack.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters",
            "NoNameReleaseOnDemand",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "network.disable-wifi-sense",
            "Disable Wi-Fi Sense",
            "Prevents Windows from automatically connecting to suggested open hotspots.",
            TweakRiskLevel.Safe,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config",
            "AutoConnectAllowedOEM",
            RegistryValueKind.DWord,
            0);
    }
}
