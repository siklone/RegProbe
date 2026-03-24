# Windows Security Settings - Verified Documentation
## Microsoft-first references, English only

This document consolidates security-related registry settings and behavior notes
verified against Microsoft documentation. Non-Microsoft references are listed
separately as secondary context.

Related docs:
- [Security use-case guide](use-case-guide.md)
- [Security tweaks](security.md)
- [Tweak catalog](../tweaks/tweak-catalog.html)
- [Tweak details](../tweaks/tweak-details.html)

---

## Table of Contents

1. [UAC (User Account Control)](#1-uac-user-account-control)
2. [VBS/HVCI (Virtualization Based Security)](#2-vbshvci-virtualization-based-security)
3. [TDR (Timeout Detection and Recovery)](#3-tdr-timeout-detection-and-recovery)
4. [PowerShell Execution Policy](#4-powershell-execution-policy)
5. [TLS/SSL Protocols](#5-tlsssl-protocols)
6. [Windows Defender](#6-windows-defender)
7. [Windows Firewall](#7-windows-firewall)
8. [System Mitigations](#8-system-mitigations)
9. [Sources Summary](#9-sources-summary)

---

## 1. UAC (User Account Control)

Official Microsoft sources:
- https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/settings-and-configuration
- https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-gpsb/341747f5-6b5d-4d30-85fc-fa1cc04038d4

Registry path:
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
```

### ConsentPromptBehaviorAdmin values

| Value | Meaning |
| --- | --- |
| 0 | Elevate without prompting (not recommended) |
| 1 | Prompt for credentials on secure desktop |
| 2 | Prompt for consent on secure desktop |
| 3 | Prompt for credentials (non-secure desktop) |
| 4 | Prompt for consent (non-secure desktop) |
| 5 | Default: prompt for consent for non-Windows binaries on secure desktop |

### ConsentPromptBehaviorUser values

| Value | Meaning |
| --- | --- |
| 0 | Automatically deny elevation requests |
| 1 | Prompt for credentials on secure desktop |
| 3 | Prompt for credentials (default) |

### Other UAC values

| Value | Meaning | Default |
| --- | --- | --- |
| EnableLUA | Enables UAC policies (0 = off, 1 = on) | 1 |
| PromptOnSecureDesktop | Secure desktop prompt (0 = off, 1 = on) | 1 |
| FilterAdministratorToken | Built-in admin approval mode | 0 |
| ValidateAdminCodeSignatures | Require signed code for elevation | 0 |
| EnableVirtualization | File/registry virtualization | 1 |
| EnableInstallerDetection | Installer heuristic detection | 1 |

### Windows settings slider mapping (common presets)

```
Always notify:
  EnableLUA = 1
  ConsentPromptBehaviorAdmin = 2
  PromptOnSecureDesktop = 1

Default:
  EnableLUA = 1
  ConsentPromptBehaviorAdmin = 5
  PromptOnSecureDesktop = 1

Do not dim desktop:
  EnableLUA = 1
  ConsentPromptBehaviorAdmin = 5
  PromptOnSecureDesktop = 0

Never notify (not recommended):
  EnableLUA = 1
  ConsentPromptBehaviorAdmin = 0
  PromptOnSecureDesktop = 0
```

WARNING: Setting EnableLUA to 0 disables UAC protections and is unsafe for
normal use. Avoid disabling UAC unless you fully understand the risks.

---

## 2. VBS/HVCI (Virtualization Based Security)

Official Microsoft sources:
- https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity
- https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/oem-vbs

Registry path:
```
HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard
```

### VBS/HVCI registry values

| Value | Meaning |
| --- | --- |
| EnableVirtualizationBasedSecurity | 0 = off, 1 = on |
| RequirePlatformSecurityFeatures | 1 = Secure Boot, 3 = Secure Boot + DMA protection |
| HypervisorEnforcedCodeIntegrity | 0 = disabled, 1 = UEFI lock, 2 = without lock |
| LsaCfgFlags | Credential Guard (0 = off, 1 = UEFI lock, 2 = without lock) |
| ConfigureSystemGuardLaunch | 0 = not configured, 1 = enabled, 2 = disabled |

### Requirements (high level)

- 64-bit CPU with virtualization (Intel VT-x or AMD-V)
- SLAT support (Intel EPT or AMD RVI)
- IOMMU (Intel VT-d or AMD-Vi) for DMA protection
- TPM 2.0 and UEFI with Secure Boot

### Disable VBS (if required)

```
bcdedit /set hypervisorlaunchtype off
```

Or via registry policy:
```
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard]
"EnableVirtualizationBasedSecurity"=dword:00000000
```

WARNING: Disabling VBS/HVCI reduces kernel protection and increases exposure to
memory-based attacks. Use only when performance constraints are well understood.

---

## 3. TDR (Timeout Detection and Recovery)

Official Microsoft sources:
- https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys
- https://learn.microsoft.com/en-us/windows-hardware/drivers/display/timeout-detection-and-recovery

Registry path:
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
```

### TDR registry values

| Value | Meaning | Default |
| --- | --- | --- |
| TdrLevel | 0 = detection off, 3 = recover on timeout | 3 |
| TdrDelay | GPU preempt timeout (seconds) | 2 |
| TdrDdiDelay | Driver exit timeout (seconds) | 5 |
| TdrLimitTime | Time window for TDR count (seconds) | 60 |
| TdrLimitCount | TDR count before action | 5 |
| TdrDebugMode | Debug behavior | 2 |

### Typical use-case ranges (examples)

| Scenario | TdrDelay | TdrDdiDelay | Notes |
| --- | --- | --- | --- |
| Gaming | 8-10 | 10 | Helps with shader compilation stalls |
| AI/ML | 60-120 | 60 | Long model inference or training |
| 3D rendering | 60-120 | 60 | Long GPU jobs |
| Video editing | 60 | 60 | Heavy encoding workloads |

Example (AI/ML):
```
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers]
"TdrDelay"=dword:00000078
"TdrDdiDelay"=dword:0000003c
"TdrLimitCount"=dword:0000000a
"TdrLimitTime"=dword:00000078
```

WARNING: Aggressive TDR changes can destabilize the system. Microsoft recommends
keeping GPU tasks within the default timeout for typical consumer workloads.

---

## 4. PowerShell Execution Policy

Official Microsoft sources:
- https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies
- https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy

Registry path:
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
Value: ExecutionPolicy (REG_SZ)
```

### Execution policy types

| Policy | Meaning |
| --- | --- |
| Restricted | No scripts; commands only |
| AllSigned | All scripts must be signed |
| RemoteSigned | Internet scripts must be signed or unblocked |
| Unrestricted | Scripts run with warnings for remote content |
| Bypass | No restrictions or prompts |
| Undefined | No policy set in the scope |

### Scope precedence (highest to lowest)

- MachinePolicy
- UserPolicy
- Process
- CurrentUser
- LocalMachine

Check current policies:
```
Get-ExecutionPolicy -List
```

Set for CurrentUser:
```
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

Unblock downloaded scripts (Mark of the Web):
```
Unblock-File -Path "C:\Downloads\script.ps1"
```

NOTE: Execution policy is not a security boundary; it helps prevent accidental
script execution and can be bypassed by a determined user.

---

## 5. TLS/SSL Protocols

Official Microsoft sources:
- https://learn.microsoft.com/en-us/windows-server/security/tls/tls-registry-settings
- https://learn.microsoft.com/en-us/windows-server/identity/ad-fs/operations/manage-ssl-protocols-in-ad-fs

Registry path (protocols):
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols
```

Protocol structure:
```
...\Protocols\{Protocol}\Client
  Enabled (DWORD)
  DisabledByDefault (DWORD)

...\Protocols\{Protocol}\Server
  Enabled (DWORD)
  DisabledByDefault (DWORD)
```

Recommended status:

| Protocol | Status |
| --- | --- |
| SSL 2.0 | Disabled |
| SSL 3.0 | Disabled |
| TLS 1.0 | Disabled (legacy compatibility risk) |
| TLS 1.1 | Disabled (legacy compatibility risk) |
| TLS 1.2 | Enabled |
| TLS 1.3 | Enabled |

Enable TLS 1.2 example:
```
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client]
"Enabled"=dword:00000001
"DisabledByDefault"=dword:00000000

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server]
"Enabled"=dword:00000001
"DisabledByDefault"=dword:00000000
```

Disable TLS 1.0 example:
```
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Client]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Server]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001
```

.NET strong crypto settings:
```
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\v4.0.30319]
"SchUseStrongCrypto"=dword:00000001
"SystemDefaultTlsVersions"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\.NETFramework\v4.0.30319]
"SchUseStrongCrypto"=dword:00000001
"SystemDefaultTlsVersions"=dword:00000001
```

---

## 6. Windows Defender

Official Microsoft sources:
- https://learn.microsoft.com/en-us/windows/security/threat-protection/microsoft-defender-antivirus/microsoft-defender-antivirus-in-windows-10
- https://learn.microsoft.com/en-us/powershell/module/defender/add-mppreference

WARNING: Disabling Microsoft Defender Antivirus removes malware protection. Use
exclusions instead of disabling protection layers.

Example exclusions:
```
Add-MpPreference -ExclusionPath "D:\Games"
Add-MpPreference -ExclusionPath "D:\ComfyUI"
Add-MpPreference -ExclusionPath "D:\models"
Add-MpPreference -ExclusionExtension ".safetensors"
Add-MpPreference -ExclusionExtension ".ckpt"
Add-MpPreference -ExclusionProcess "python.exe"
```

---

## 7. Windows Firewall

Official Microsoft sources:
- https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/configure-with-command-line

Microsoft guidance highlights that disabling the firewall service can break
store apps, Windows Sandbox, activation, and other system components. If a
temporary disable is required, prefer toggling profiles rather than stopping
the service.

Disable all profiles via PowerShell:
```
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False
```

Disable all profiles via netsh:
```
netsh advfirewall set allprofiles state off
```

Registry (profiles):
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy
```

---

## 8. System Mitigations

Official Microsoft sources:
- https://learn.microsoft.com/en-us/windows/security/operating-system-security/device-management/override-mitigation-options-for-app-related-security-policies

Mitigations include:
- DEP (Data Execution Prevention)
- ASLR (Address Space Layout Randomization)
- SEHOP (Structured Exception Handler Overwrite Protection)
- CFG (Control Flow Guard)

DEP modes (bcdedit):
```
bcdedit /set nx OptIn
```

Set per-process mitigations:
```
Set-ProcessMitigation -Name "process.exe" -Disable DEP
```

Check system mitigations:
```
Get-ProcessMitigation -System
```

---

## 9. Sources Summary

### Official Microsoft references (primary)

- UAC settings and configuration:
  https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/settings-and-configuration
- UAC registry protocol:
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-gpsb/341747f5-6b5d-4d30-85fc-fa1cc04038d4
- VBS/HVCI:
  https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity
- VBS OEM guidance:
  https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/oem-vbs
- TDR registry keys:
  https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys
- TDR overview:
  https://learn.microsoft.com/en-us/windows-hardware/drivers/display/timeout-detection-and-recovery
- PowerShell execution policies:
  https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies
- Set-ExecutionPolicy:
  https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy
- TLS registry settings:
  https://learn.microsoft.com/en-us/windows-server/security/tls/tls-registry-settings
- SSL/TLS in AD FS:
  https://learn.microsoft.com/en-us/windows-server/identity/ad-fs/operations/manage-ssl-protocols-in-ad-fs
- Microsoft Defender Antivirus:
  https://learn.microsoft.com/en-us/windows/security/threat-protection/microsoft-defender-antivirus/microsoft-defender-antivirus-in-windows-10
- Add-MpPreference:
  https://learn.microsoft.com/en-us/powershell/module/defender/add-mppreference
- Windows Firewall CLI:
  https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/configure-with-command-line
- Process mitigation options:
  https://learn.microsoft.com/en-us/windows/security/operating-system-security/device-management/override-mitigation-options-for-app-related-security-policies

### Additional sources (secondary)

- Tom's Hardware
- PC Gamer
- ComputerBase
- Neowin
- Adobe (TDR guidance)
- Intel oneAPI
- Puget Systems
- D5 Render
- NSA cybersecurity guidance

---

Version:
- Created: 2026-01
- Source: nohuto/win-config (security/)
- Status: Reference documentation

<!-- TWEAK INDEX START -->
## Tweak Index (Generated)

This section is generated from `Docs/tweaks/tweak-catalog.csv`.
Do not edit manually.

| ID | Name | Changes | Risk | Source |
| --- | --- | --- | --- | --- |
| <a id="security.disable-downloads-blocking"></a> `security.disable-downloads-blocking` | Disable Downloads Blocking | Prevents Windows from marking downloads with zone information (MOTW). | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L253` |
| <a id="security.disable-enhanced-defender-notifications"></a> `security.disable-enhanced-defender-notifications` | Hide Non-Critical Windows Security Notifications | Shows only critical Windows Security notifications by setting the documented enhanced-notifications policy. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L94` |
| <a id="security.disable-ntfs-encryption"></a> `security.disable-ntfs-encryption` | Disable NTFS Encryption (EFS) | Prevents EFS encryption on NTFS volumes to avoid accidental data lockouts. | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L193` |
| <a id="security.disable-p2p-updates"></a> `security.disable-p2p-updates` | Disable P2P Updates | Disables Delivery Optimization peer-to-peer caching for updates. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L229` |
| <a id="security.disable-password-reveal"></a> `security.disable-password-reveal` | Disable Password Reveal Button | Hides the 'eye' icon button that reveals passwords in credential prompts. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L58` |
| <a id="security.disable-picture-password"></a> `security.disable-picture-password` | Disable Picture Password Sign-In | Prevents domain users from using picture passwords for sign-in. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L70` |
| <a id="security.disable-remote-assistance"></a> `security.disable-remote-assistance` | Disable Remote Assistance | Disables solicited Remote Assistance connections to reduce attack surface. | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L181` |
| <a id="security.disable-system-mitigations"></a> `security.disable-system-mitigations` | Disable System Mitigations | Imports the documented Exploit Protection XML baseline that disables the researched system-wide mitigation bundle. | Risky | `WindowsOptimizer.Engine\Tweaks\Commands\Security\DisableSystemMitigationsTweak.cs#L30` |
| <a id="security.disable-system-restore"></a> `security.disable-system-restore` | Disable System Restore | Disables System Restore through the official machine policy surface. | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L241` |
| <a id="security.disable-uac"></a> `security.disable-uac` | Disable UAC (Full) | Disables User Account Control entirely. Requires a reboot and severely lowers system security. | Risky | `WindowsOptimizer.Engine\Tweaks\Commands\Security\DisableUacFullTweak.cs#L18` |
| <a id="security.disable-vbs"></a> `security.disable-vbs` | Disable VBS (HVCI) | Turns off virtualization-based security and memory integrity policies for lower latency. | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L124` |
| <a id="security.disable-windows-firewall"></a> `security.disable-windows-firewall` | Disable Windows Firewall | Turns off Windows Defender Firewall for the documented Domain and Standard firewall policy profiles. Reference: Microsoft Defender Firewa... | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L110` |
| <a id="security.disable-windows-update"></a> `security.disable-windows-update` | Disable Windows Update | Pauses updates and sets Windows Update policies to block access effectively till 2030. | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L152` |
| <a id="security.disable-wpbt"></a> `security.disable-wpbt` | Disable WPBT Execution | Blocks Windows Platform Binary Table (WPBT) programs from running at startup (prevents BIOS-injected bloatware). | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L139` |
| <a id="security.disable-wu-driver-updates"></a> `security.disable-wu-driver-updates` | Disable WU Driver Updates | Stops Windows Update from offering driver updates and device metadata to prevent problematic driver overwrites. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L167` |
| <a id="security.enable-dynamic-lock"></a> `security.enable-dynamic-lock` | Enable Dynamic Lock | Enables the documented Dynamic Lock policy so Windows can evaluate user-absence signal rules and lock the device. | Safe | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L82` |
| <a id="security.enable-sudo"></a> `security.enable-sudo` | Enable Windows Sudo | Enables the sudo for Windows feature with in-place elevation behavior. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L217` |
| <a id="security.powershell-unrestricted"></a> `security.powershell-unrestricted` | Set PowerShell Policy to Unrestricted | Allows all PowerShell scripts to run without signing requirements. Very risky for general use. | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L205` |
| <a id="security.trusted-path-credential-prompting"></a> `security.trusted-path-credential-prompting` | Require Trusted Path for Credentials | Forces credential prompts to use the Secure Desktop to prevent interception. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L46` |
| <a id="security.uac-never-notify"></a> `security.uac-never-notify` | Set UAC to Never Notify | Lowers User Account Control prompts to the least restrictive setting. Risky for security but reduces interruptions. Reference: Microsoft... | Risky | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L29` |
<!-- TWEAK INDEX END -->
