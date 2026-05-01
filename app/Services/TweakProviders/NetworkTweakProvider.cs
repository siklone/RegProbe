using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Network;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class NetworkTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Network";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Topology & Discovery
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
        yield return new DisableNetbiosOverTcpIpTweak(context.ElevatedCommandRunner);

        // Usage and Connectivity
        // SMB Security & Features
        yield return CreateRegistryValueSetTweak(
            context,
            "network.smb-require-signing-client",
            "SMB: Require Client Signing",
            "Requires SMB client signing for outbound connections.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
            new[]
            {
                new RegistryValueSetEntry("RequireSecuritySignature", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("EnableSecuritySignature", RegistryValueKind.DWord, 1)
            });
        yield return CreateRegistryValueSetTweak(
            context,
            "network.smb-require-signing-server",
            "SMB: Require Signing (Server)",
            "Requires SMB server signing for inbound connections.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanServer\Parameters",
            new[]
            {
                new RegistryValueSetEntry("RequireSecuritySignature", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("EnableSecuritySignature", RegistryValueKind.DWord, 1)
            });

        yield return new DisableSmbLeasingTweak(context.ElevatedCommandRunner);

        yield return new EnableSmbMultichannelTweak(context.ElevatedCommandRunner);

        yield return CreateRegistryValueBatchTweak(
            context,
            "network.smb-set-cipher-suite-order",
            "SMB: Set Cipher Suite Order",
            "Sets the SMB encryption cipher suite order to AES-256 variants.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanWorkstation", "CipherSuiteOrder", RegistryValueKind.MultiString, new[] { "AES_256_GCM", "AES_256_CCM" }),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanServer", "CipherSuiteOrder", RegistryValueKind.MultiString, new[] { "AES_256_GCM", "AES_256_CCM" })
            });

    }
}
