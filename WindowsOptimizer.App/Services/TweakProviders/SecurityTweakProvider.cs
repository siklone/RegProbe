using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Security;

namespace WindowsOptimizer.App.Services.TweakProviders;

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

        yield return CreateRegistryTweak(
            context,
            "security.trusted-path-credential-prompting",
            "Require Trusted Path for Credentials",
            "Forces credential prompts to use the Secure Desktop to prevent interception.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\CredUI",
            "EnableSecureCredentialPrompting",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "security.disable-password-reveal",
            "Disable Password Reveal Button",
            "Hides the 'eye' icon button that reveals passwords in credential prompts.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\CredUI",
            "DisablePasswordReveal",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "security.disable-picture-password",
            "Disable Picture Password Sign-In",
            "Prevents domain users from using picture passwords for sign-in.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\System",
            "BlockDomainPicturePassword",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "security.enable-dynamic-lock",
            "Enable Dynamic Lock",
            "Automatically locks the device when the paired Bluetooth device is away.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon",
            "EnableGoodbye",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);

        // System Defense
        // Windows Firewall Configuration
        // Source: Microsoft Defender Firewall Documentation
        // https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/
        yield return CreateRegistryValueBatchTweak(
            context,
            "security.disable-windows-firewall",
            "Disable Windows Firewall",
            "Turns off Windows Defender Firewall for Domain, Private, and Public profiles. Reference: Microsoft Defender Firewall Docs",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile", "EnableFirewall", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile", "EnableFirewall", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile", "EnableFirewall", RegistryValueKind.DWord, 0)
            });

        yield return CreateRegistryValueBatchTweak(
            context,
            "security.disable-system-mitigations",
            "Disable System Mitigations",
            "Turns off system-wide exploit mitigation settings (ASLR, DEP, etc.) for performance.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Session Manager\kernel", "MitigationOptions", RegistryValueKind.Binary, new byte[] { 0x00, 0x22, 0x22, 0x20, 0x22, 0x20, 0x22, 0x22, 0x22, 0x20, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Session Manager\kernel", "MitigationAuditOptions", RegistryValueKind.Binary, new byte[] { 0x02, 0x22, 0x22, 0x02, 0x02, 0x02, 0x20, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })
            });

        yield return CreateRegistryValueSetTweak(
            context,
            "security.disable-vbs",
            "Disable VBS (HVCI)",
            "Turns off virtualization-based security and memory integrity policies for lower latency.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard",
            new[]
            {
                new RegistryValueSetEntry("EnableVirtualizationBasedSecurity", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("HypervisorEnforcedCodeIntegrity", RegistryValueKind.DWord, 0),
                new RegistryValueSetEntry("LsaCfgFlags", RegistryValueKind.DWord, 0)
            });

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

        yield return CreateRegistryValueBatchTweak(
            context,
            "security.disable-wu-driver-updates",
            "Disable WU Driver Updates",
            "Stops Windows Update from offering driver updates and device metadata to prevent problematic driver overwrites.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", RegistryValueKind.DWord, 1),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DriverSearching", "SearchOrderConfig", RegistryValueKind.DWord, 0),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DriverSearching", "DontSearchWindowsUpdate", RegistryValueKind.DWord, 1)
            });

        // Remote Access & Network Security
        yield return CreateRegistryTweak(
            context,
            "security.disable-remote-assistance",
            "Disable Remote Assistance",
            "Disables solicited Remote Assistance connections to reduce attack surface.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows NT\Terminal Services",
            "fAllowToGetHelp",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryValueBatchTweak(
            context,
            "security.disable-ntfs-encryption",
            "Disable NTFS Encryption (EFS)",
            "Prevents EFS encryption on NTFS volumes to avoid accidental data lockouts.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Policies", "NtfsDisableEncryption", RegistryValueKind.DWord, 1),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\FileSystem", "NtfsDisableEncryption", RegistryValueKind.DWord, 1)
            });

        // Developer & Modern Features
        yield return CreateRegistryValueBatchTweak(
            context,
            "security.powershell-unrestricted",
            "Set PowerShell Policy to Unrestricted",
            "Allows all PowerShell scripts to run without signing requirements. Very risky for general use.",
            TweakRiskLevel.Risky,
            new[]
            {
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\PowerShell", "EnableScripts", RegistryValueKind.DWord, 1),
                new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\PowerShell", "ExecutionPolicy", RegistryValueKind.String, "Unrestricted")
            });

        yield return CreateRegistryTweak(
            context,
            "security.enable-sudo",
            "Enable Windows Sudo",
            "Enables the sudo for Windows feature with in-place elevation behavior.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\Sudo",
            "Enabled",
            RegistryValueKind.DWord,
            3);

        yield return CreateRegistryTweak(
            context,
            "security.disable-p2p-updates",
            "Disable P2P Updates",
            "Disables Delivery Optimization peer-to-peer caching for updates.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
            "DODownloadMode",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "security.disable-system-restore",
            "Disable System Restore",
            "Disables System Restore through the official machine policy surface.",
            TweakRiskLevel.Risky,
            RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore",
            "DisableSR",
            RegistryValueKind.DWord,
            1);

        yield return CreateRegistryTweak(
            context,
            "security.disable-downloads-blocking",
            "Disable Downloads Blocking",
            "Prevents Windows from marking downloads with zone information (MOTW).",
            TweakRiskLevel.Risky,
            RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments",
            "SaveZoneInformation",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);
    }
}
