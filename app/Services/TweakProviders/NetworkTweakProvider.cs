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

        yield return WithMicrosoftDoc(
            CreateRegistryValueSetTweak(
                context,
                "network.smb-increase-client-metadata-cache",
                "SMB: Increase Client Metadata Cache",
                "Raises SMB client metadata caches and request depth for heavier remote file workloads, especially higher-latency shares.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                new[]
                {
                    new RegistryValueSetEntry("DirectoryCacheEntriesMax", RegistryValueKind.DWord, 4096),
                    new RegistryValueSetEntry("FileInfoCacheEntriesMax", RegistryValueKind.DWord, 32768),
                    new RegistryValueSetEntry("FileNotFoundCacheEntriesMax", RegistryValueKind.DWord, 32768),
                    new RegistryValueSetEntry("MaxCmds", RegistryValueKind.DWord, 32768)
                }),
            "https://learn.microsoft.com/en-us/windows-server/administration/performance-tuning/role/file-server/");

        // Command-based Network Tweaks
        yield return new FlushDnsCacheTweak(context.ElevatedCommandRunner);
        yield return new ResetNetworkStackTweak(context.ElevatedCommandRunner);

        // Security
        yield return new DisableNetbiosOverTcpIpTweak(context.ElevatedCommandRunner);

        // Usage and Connectivity
        yield return CreateRegistryValueSetTweak(
            context,
            "network.disable-lltd",
            "Set LLTD Policies to Default Behavior",
            "Disables the explicit LLTD mapper and responder policies so Windows uses the documented default behavior.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\LLTD",
            new[]
            {
                new RegistryValueSetEntry("EnableLLTDIO", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("AllowLLTDIOOnDomain", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("AllowLLTDIOOnPublicNet", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("ProhibitLLTDIOOnPrivateNet", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("EnableRspndr", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("AllowRspndrOnDomain", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("AllowRspndrOnPublicNet", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("ProhibitRspndrOnPrivateNet", RegistryValueKind.DWord, 0)
            });

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
            "network.smb-enable-quic",
            "SMB: Enable QUIC",
            "Enables SMB over QUIC for client and server.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanWorkstation", "EnableSMBQUIC", RegistryValueKind.DWord, 1),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanServer", "EnableSMBQUIC", RegistryValueKind.DWord, 1)
            });

        yield return CreateRegistryValueBatchTweak(
            context,
            "network.smb-require-dialect-3_1_1",
            "SMB: Require Dialect 3.1.1",
            "Restricts SMB client/server dialects to exactly SMB 3.1.1.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanWorkstation", "MinSmb2Dialect", RegistryValueKind.DWord, 785),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanWorkstation", "MaxSmb2Dialect", RegistryValueKind.DWord, 785),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanServer", "MinSmb2Dialect", RegistryValueKind.DWord, 785),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\LanmanServer", "MaxSmb2Dialect", RegistryValueKind.DWord, 785)
            });

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

        yield return CreateRegistryValueBatchTweak(
            context,
            "network.disable-default-shares",
            "Disable Default Shares",
            "Disables automatic administrative shares on the SMB server.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "AutoShareServer", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "AutoShareWks", RegistryValueKind.DWord, 0)
            });

    }
}
