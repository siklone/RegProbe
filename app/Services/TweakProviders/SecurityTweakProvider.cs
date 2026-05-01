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
        yield return CreateRegistryValueSetTweak(
            context,
            "security.uac-never-notify",
            "Set UAC to Never Notify",
            "Lowers User Account Control prompts to the least restrictive setting. Risky for security but reduces interruptions. Reference: Microsoft Security Baselines",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            new[]
            {
                new RegistryValueSetEntry("EnableLUA", RegistryValueKind.DWord, 1),
                new RegistryValueSetEntry("ConsentPromptBehaviorAdmin", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("PromptOnSecureDesktop", RegistryValueKind.DWord, 0)
            });

        yield return new DisableUacFullTweak(context.ElevatedCommandRunner);

        yield return CreateRegistryValueBatchTweak(
            context,
            "security.threat-file-hash-logging",
            "Defender Threat File Hash Logging",
            "Enables the documented Defender root policy used for threat file hash logging on current Windows Defender builds.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "ThreatFileHashLogging", RegistryValueKind.DWord, 1)
            });

        // System Defense
        // Windows Firewall Configuration
        // Source: Microsoft Defender Firewall Documentation
        // https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/
        yield return new DisableSystemMitigationsTweak(context.ElevatedCommandRunner);

        yield return CreateRegistryTweak(
            context,
            "security.disable-wpbt",
            "Disable WPBT Execution",
            "Blocks Windows Platform Binary Table (WPBT) programs from running at startup (prevents BIOS-injected bloatware).",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Control\Session Manager",
            "DisableWpbtExecution",
            RegistryValueKind.DWord,
            1);

        // Windows Update Security
        yield return CreateRegistryValueBatchTweak(
            context,
            "security.disable-windows-update",
            "Disable Windows Update",
            "Pauses updates and sets Windows Update policies to block access effectively till 2030.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseFeatureUpdatesEndTime", RegistryValueKind.String, "2030-01-01T00:00:00Z"),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseQualityUpdatesEndTime", RegistryValueKind.String, "2030-01-01T00:00:00Z"),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime", RegistryValueKind.String, "2030-01-01T00:00:00Z"),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableWindowsUpdateAccess", RegistryValueKind.DWord, 1),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", RegistryValueKind.DWord, 1)
            });

        // Remote Access & Network Security
        // Developer & Modern Features
    }
}
