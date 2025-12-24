using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class SecurityTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Security";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(
                context,
                "security.enable-uac",
                "Enable User Account Control (UAC)",
                "Ensures UAC prompts are enabled for administrator actions.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                "EnableLUA",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.disable-autorun",
                "Disable AutoRun for All Drives",
                "Prevents automatic execution of programs from removable media.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "NoDriveTypeAutoRun",
                RegistryValueKind.DWord,
                0xFF),

            CreateRegistryTweak(
                context,
                "security.enable-sehop",
                "Enable Structured Exception Handler Overwrite Protection (SEHOP)",
                "Protects against stack overflow exploits.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                "DisableExceptionChainValidation",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "security.enable-strong-crypto",
                "Enable Strong Cryptography for .NET",
                "Forces .NET applications to use strong TLS versions.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\.NETFramework\v4.0.30319",
                "SchUseStrongCrypto",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.disable-llmnr",
                "Disable Link-Local Multicast Name Resolution (LLMNR)",
                "Prevents potential man-in-the-middle attacks via LLMNR poisoning.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient",
                "EnableMulticast",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "security.enable-firewall-logging",
                "Enable Windows Firewall Logging",
                "Logs dropped packets and successful connections.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile\Logging",
                "LogDroppedPackets",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.disable-remote-assistance",
                "Disable Windows Remote Assistance",
                "Prevents remote users from connecting to your computer for assistance.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Remote Assistance",
                "fAllowToGetHelp",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "security.disable-remote-desktop",
                "Disable Remote Desktop",
                "Prevents remote desktop connections to this computer.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Terminal Server",
                "fDenyTSConnections",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.enable-credential-guard",
                "Enable Credential Guard (Virtualization-based Security)",
                "Protects credentials using virtualization-based security.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Lsa",
                "LsaCfgFlags",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.disable-powershell-v2",
                "Disable PowerShell v2",
                "Removes legacy PowerShell 2.0 to prevent downgrade attacks.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine",
                "PowerShellVersion",
                RegistryValueKind.String,
                ""),

            CreateRegistryTweak(
                context,
                "security.enable-smb-signing",
                "Require SMB Signing",
                "Enforces SMB packet signing to prevent tampering.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters",
                "RequireSecuritySignature",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.disable-ntlm",
                "Disable NTLM Authentication",
                "Forces Kerberos authentication only, disabling legacy NTLM.",
                TweakRiskLevel.Risky,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Lsa",
                "LmCompatibilityLevel",
                RegistryValueKind.DWord,
                5),

            CreateRegistryTweak(
                context,
                "security.enable-applocker",
                "Enable AppLocker Service",
                "Starts the Application Identity service for AppLocker policies.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\AppIDSvc",
                "Start",
                RegistryValueKind.DWord,
                2), // Automatic

            CreateRegistryTweak(
                context,
                "security.disable-autoplay-all-drives",
                "Disable AutoPlay for All Drives and Media Types",
                "Completely disables AutoPlay functionality.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers",
                "DisableAutoplay",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "security.enable-windows-defender-realtime",
                "Enable Windows Defender Real-Time Protection",
                "Ensures real-time antivirus scanning is enabled.",
                TweakRiskLevel.Safe,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
                "DisableRealtimeMonitoring",
                RegistryValueKind.DWord,
                0),

            CreateRegistryTweak(
                context,
                "security.enable-controlled-folder-access",
                "Enable Controlled Folder Access (Ransomware Protection)",
                "Protects important folders from unauthorized changes.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access",
                "EnableControlledFolderAccess",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.disable-script-host",
                "Disable Windows Script Host",
                "Prevents VBScript and JScript execution.",
                TweakRiskLevel.Advanced,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows Script Host\Settings",
                "Enabled",
                RegistryValueKind.DWord,
                0,
                requiresElevation: false),

            CreateRegistryTweak(
                context,
                "security.enable-app-sandbox",
                "Enable Application Sandboxing",
                "Forces applications to run in isolated sandboxes.",
                TweakRiskLevel.Advanced,
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                "EnableVirtualization",
                RegistryValueKind.DWord,
                1),

            CreateRegistryTweak(
                context,
                "security.enable-dynamic-lock",
                "Enable Dynamic Lock",
                "Automatically locks PC when paired Bluetooth device is out of range.",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon",
                "EnableGoodbye",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false)
        };
    }
}
