using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class NetworkTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Network";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "network.disable-ipv6",
                "Disable IPv6",
                "Disables IPv6 protocol system-wide.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters",
                "DisabledComponents",
                RegistryValueKind.DWord,
                0xFF),

            CreateRegistryTweak(
                context,
                "network.disable-netbios",
                "Disable NetBIOS over TCP/IP",
                "Disables legacy NetBIOS protocol for improved security.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters",
                "NoNameReleaseOnDemand",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "network.disable-llmnr",
                "Disable LLMNR",
                "Disables Link-Local Multicast Name Resolution to prevent spoofing attacks.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient",
                "EnableMulticast",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "network.disable-wifi-sense",
                "Disable Wi-Fi Sense",
                "Prevents automatic connection to suggested open hotspots.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config",
                "AutoConnectAllowedOEM",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "network.disable-smart-multi-homed-name-resolution",
                "Disable Smart Multi-Homed Name Resolution",
                "Prevents DNS queries from being sent to all network adapters.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient",
                "DisableSmartNameResolution",
                RegistryValueKind.DWord,
                1),

            CreateRegistryValueSetTweak(
                context,
                "network.optimize-smb",
                "Optimize SMB Performance",
                "Enables SMB multichannel and large MTU for better network file sharing.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                new[]
                {
                    new RegistryValueSetEntry("DisableBandwidthThrottling", RegistryValueKind.DWord, 1),
                    new RegistryValueSetEntry("DisableLargeMtu", RegistryValueKind.DWord, 0),
                    new RegistryValueSetEntry("FileInfoCacheLifetime", RegistryValueKind.DWord, 30),
                    new RegistryValueSetEntry("DirectoryCacheLifetime", RegistryValueKind.DWord, 30)
                }),

            CreateRegistryTweak(
                context,
                "network.require-smb-encryption",
                "Require SMB Encryption",
                "Forces encryption for SMB connections to improve security.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters",
                "EncryptData",
                RegistryValueKind.DWord,
                1)
        };
    }
}
