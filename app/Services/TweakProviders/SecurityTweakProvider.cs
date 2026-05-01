using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Security;

namespace RegProbe.Application.Services.TweakProviders;

/// <summary>
/// Security tweaks provider with references to trusted sources.
/// Sources:
/// - Microsoft Security Baselines: https://aka.ms/baselines
/// - ASD Windows Hardening: https://www.cyber.gov.au/hardening-guides
/// - Microsoft Learn Security: https://learn.microsoft.com/en-us/windows/security/
/// - CIS Benchmarks: https://www.cisecurity.org/cis-benchmarks
/// </summary>
public sealed class SecurityTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Security";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // UAC and Auth
        // Source: Microsoft Security Baselines - User Account Control
        // https://learn.microsoft.com/en-us/windows/security/identity-protection/user-account-control/how-user-account-control-works
        yield return new DisableUacFullTweak(context.ElevatedCommandRunner);

        // System Defense
        // Windows Firewall Configuration
        // Source: Microsoft Defender Firewall Documentation
        // https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/
        yield return new DisableSystemMitigationsTweak(context.ElevatedCommandRunner);

        // Windows Update Security
        // Remote Access & Network Security
        // Developer & Modern Features
    }
}
