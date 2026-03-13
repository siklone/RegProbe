using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Network;

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


        // Usage and Connectivity
        yield return CreateRegistryTweak(
            context,
            "network.disable-llmnr",
            "Disable LLMNR",
            "Turns off multicast name resolution (LLMNR).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows NT\DNSClient",
            "EnableMulticast",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "network.disable-mdns",
            "Disable mDNS",
            "Turns off multicast DNS name resolution.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows NT\DNSClient",
            "EnableMDNS",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "network.disable-netbios-resolution",
            "Disable NetBIOS Name Resolution",
            "Disables NetBIOS name resolution on the DNS client.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows NT\DNSClient",
            "EnableNetbios",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "network.disable-smart-name-resolution",
            "Disable Smart Multi-Homed Name Resolution",
            "Disables smart name resolution across multiple network interfaces.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows NT\DNSClient",
            "DisableSmartNameResolution",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryValueSetTweak(
            context,
            "network.disable-lltd",
            "Disable Network Discovery (LLTD)",
            "Disables LLTD mapper and responder for network discovery.",
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

        yield return CreateRegistryValueBatchTweak(
            context,
            "network.disable-active-probing",
            "Disable Active Probing",
            "Turns off NCSI active probing for internet connectivity tests.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator", "NoActiveProbe", RegistryValueKind.DWord, 1)
            });

        yield return CreateRegistryTweak(
            context,
            "network.prefer-ipv4",
            "Prefer IPv4 over IPv6",
            "Configures the IPv6 stack to prefer IPv4 without fully disabling IPv6.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters",
            "DisabledComponents",
            RegistryValueKind.DWord,
            32);

        // SMB Security & Features
        yield return CreateRegistryTweak(
            context,
            "network.smb-enable-large-mtu",
            "SMB: Enable Large MTU",
            "Enables large MTU support for SMB client connections.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
            "DisableLargeMtu",
            RegistryValueKind.DWord,
            0);

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

        yield return CreateRegistryTweak(
            context,
            "network.smb-encrypt-data",
            "SMB: Require Encryption",
            "Requires SMB server encryption for shared data.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanServer\Parameters",
            "EncryptData",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "network.smb-reject-unencrypted-access",
            "SMB: Reject Unencrypted Access",
            "Rejects SMB clients that do not support encryption.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanServer\Parameters",
            "RejectUnencryptedAccess",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "network.smb-disable-leasing",
            "SMB: Disable Leasing",
            "Disables SMB server leasing (read/write/handle caching).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanServer\Parameters",
            "DisableLeasing",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "network.smb-enable-multichannel",
            "SMB: Enable Multichannel",
            "Enables SMB multichannel for parallel network paths.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
            "DisableMultiChannel",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryValueBatchTweak(
            context,
            "network.smb-enable-quic",
            "SMB: Enable QUIC",
            "Enables SMB over QUIC for client and server.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "EnableSMBQUIC", RegistryValueKind.DWord, 1),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "EnableSMBQUIC", RegistryValueKind.DWord, 1)
            });

        yield return CreateRegistryValueBatchTweak(
            context,
            "network.smb-require-dialect-3_1_1",
            "SMB: Require Dialect 3.1.1",
            "Restricts SMB client/server dialects to SMB 3.1.1 or newer.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "MinSmb2Dialect", RegistryValueKind.DWord, 785),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "MaxSmb2Dialect", RegistryValueKind.DWord, 65536),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "MinSmb2Dialect", RegistryValueKind.DWord, 785),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "MaxSmb2Dialect", RegistryValueKind.DWord, 65536)
            });

        yield return CreateRegistryValueBatchTweak(
            context,
            "network.smb-set-cipher-suite-order",
            "SMB: Set Cipher Suite Order",
            "Sets the SMB encryption cipher suite order to AES-256 variants.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "CipherSuiteOrder", RegistryValueKind.MultiString, new[] { "AES_256_GCM", "AES_256_CCM" }),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "CipherSuiteOrder", RegistryValueKind.MultiString, new[] { "AES_256_GCM", "AES_256_CCM" })
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

        yield return CreateRegistryTweak(
            context,
            "network.disable-smb1",
            "Disable SMBv1",
            "Disables the legacy SMBv1 protocol on the server.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanServer\Parameters",
            "SMB1",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "network.disable-smb2",
            "Disable SMBv2/SMBv3",
            "Disables the SMBv2/SMBv3 protocol on the server.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanServer\Parameters",
            "SMB2",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "network.disable-plaintext-smb-passwords",
            "Disable Plaintext SMB Passwords",
            "Prevents sending unencrypted passwords to SMB servers.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
            "EnablePlainTextPassword",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "network.require-ntlmv2-session-security",
            "Require NTLMv2 Session Security",
            "Requires NTLMv2 session security and 128-bit encryption for SMB clients.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Control\Lsa\MSV1_0",
            "NTLMMinClientSec",
            RegistryValueKind.DWord,
            537395200);
    }
}
