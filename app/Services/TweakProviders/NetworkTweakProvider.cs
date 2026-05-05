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
            "Configure SMB Workstation Parameters",
            "Writes the SMB workstation bandwidth-throttling and cache-lifetime values used by this tweak.",
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
        // Usage and Connectivity
        // SMB Security & Features
    }
}
