# Windows Security Settings Guide
## Use-case based settings (English)

This guide maps common security scenarios to registry-backed settings.
It is a companion to the main docs and stays separate so the source trail stays clear.

Related docs:
- [Security verified documentation](security-verified.md)
- [Security tweaks](security.md)
- [Tweak catalog](../tweaks/tweak-catalog.html)
- [Tweak details](../tweaks/tweak-details.html)

---

## Table of Contents

1. [Maximum Security (Enterprise / Sensitive Data)](#1-maximum-security-enterprise-sensitive-data)
2. [Gaming / Performance](#2-gaming-performance)
3. [AI/ML Workstation (ComfyUI, Stable Diffusion)](#3-aiml-workstation-comfyui-stable-diffusion)
4. [Developer / Programmer](#4-developer-programmer)
5. [Home User / Daily Use](#5-home-user-daily-use)
6. [Virtual Machine Host](#6-virtual-machine-host)
7. [Offline / Air-Gapped System](#7-offline-air-gapped-system)
8. [Server / Workstation](#8-server-workstation)
9. [Privacy Focused](#9-privacy-focused)

---

## Security Settings Summary

| Setting | Security Benefit | Performance Impact | Risk |
| --- | --- | --- | --- |
| UAC | Privilege control | Low | Disabling increases malware risk |
| VBS/HVCI | Kernel hardening | 5-15% (workload dependent) | Disabling increases memory attack risk |
| Windows Defender | Antivirus | Variable (scan spikes) | Disabling removes malware protection |
| Firewall | Network protection | Very low | Disabling exposes services |
| TDR | GPU timeout recovery | None | Aggressive changes can destabilize |
| System Mitigations | Exploit protection | 1-5% | Disabling increases exploit risk |
| TLS/Crypto | Secure transport | Very low | Legacy protocols are unsafe |

---

## 1. Maximum Security (Enterprise / Sensitive Data)

Goal: maximum protection and compliance.

### UAC - Highest Level
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
```

| Value | Recommended | Notes |
| --- | --- | --- |
| EnableLUA | 1 | UAC enabled |
| ConsentPromptBehaviorAdmin | 2 | Always prompt (secure desktop) |
| PromptOnSecureDesktop | 1 | Secure desktop enabled |
| FilterAdministratorToken | 1 | Require prompt for built-in admin |
| ValidateAdminCodeSignatures | 1 | Require signed code |

### VBS/HVCI - Full Protection
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard
```

| Value | Recommended | Notes |
| --- | --- | --- |
| EnableVirtualizationBasedSecurity | 1 | VBS enabled |
| RequirePlatformSecurityFeatures | 3 | Secure Boot + DMA protection |
| HypervisorEnforcedCodeIntegrity | 1 | HVCI enabled |
| LsaCfgFlags | 1 | Credential Guard with UEFI lock |
| ConfigureSystemGuardLaunch | 1 | Secure Launch enabled |

### Windows Defender - Maximum
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows Defender
```

Enable:
- Real-time protection
- Cloud-delivered protection
- Automatic sample submission
- Tamper protection
- Controlled folder access
- Attack Surface Reduction (ASR) rules

### Firewall - All Profiles Enabled
```
Path: HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy
```

| Profile | EnableFirewall |
| --- | --- |
| DomainProfile | 1 |
| StandardProfile | 1 |
| PublicProfile | 1 |

### TLS/Crypto - Modern Only
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols
```

Enable:
- TLS 1.2
- TLS 1.3
- DTLS 1.2

Disable:
- SSL 2.0
- SSL 3.0
- TLS 1.0
- TLS 1.1
- DTLS 1.0

Cipher suites to disable:
- RC4 (all variants)
- DES
- 3DES
- NULL
- MD5
- SHA-1 (signature use)

Key sizes:
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\KeyExchangeAlgorithms
```
- Diffie-Hellman: ClientMinKeyBitLength = 3072
- RSA (PKCS): ClientMinKeyBitLength = 3072

### NTLM - v2 Only
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\Lsa
LmCompatibilityLevel = 5
```

### BitLocker
Enable on all volumes.

### System Mitigations
DEP, ASLR, SEHOP, CFG enabled for all processes.

---

## 2. Gaming / Performance

Goal: maximum FPS and low latency with acceptable security.

### UAC - No Prompt (Use with Caution)
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
EnableLUA = 1
ConsentPromptBehaviorAdmin = 0
PromptOnSecureDesktop = 0
```

Warning: this reduces protections. Use only with trusted software.

### VBS/HVCI - OFF
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard
EnableVirtualizationBasedSecurity = 0
```

Potential gain: 5-15% depending on game.

### Windows Defender - Exclusions
```powershell
Add-MpPreference -ExclusionPath "D:\Games"
Add-MpPreference -ExclusionPath "C:\Program Files\Steam"
Add-MpPreference -ExclusionPath "C:\Program Files\Epic Games"
```

### TDR - Increased for Shader Compilation
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
TdrDelay = 10
TdrDdiDelay = 10
```

### Firewall - Keep On
Open game ports instead of disabling the firewall.

---

## 3. AI/ML Workstation (ComfyUI, Stable Diffusion)

Goal: long GPU workloads, stability, CUDA compatibility.

### TDR - Critical for Long Jobs
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers
```

| Value | AI/ML | Notes |
| --- | --- | --- |
| TdrDelay | 60-120 | GPU timeout (seconds) |
| TdrDdiDelay | 60 | DDI timeout |
| TdrLimitCount | 10 | Max TDR count |
| TdrLimitTime | 120 | Window (seconds) |

Example:
```reg
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers]
"TdrDelay"=dword:00000078
"TdrDdiDelay"=dword:0000003c
"TdrLimitCount"=dword:0000000a
"TdrLimitTime"=dword:00000078
```

### VBS/HVCI - Optional
- OFF for maximum GPU performance
- ON if shared workstation security is required

### Windows Defender - Exclusions
```powershell
Add-MpPreference -ExclusionPath "D:\ComfyUI"
Add-MpPreference -ExclusionPath "D:\stable-diffusion"
Add-MpPreference -ExclusionPath "D:\models"
Add-MpPreference -ExclusionExtension ".safetensors"
Add-MpPreference -ExclusionExtension ".ckpt"
```

### UAC - Default or Low
```
ConsentPromptBehaviorAdmin = 5
```

or
```
ConsentPromptBehaviorAdmin = 0
```

### PowerShell Execution Policy
```
Path: HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
ExecutionPolicy = "Unrestricted"
```

---

## 4. Developer / Programmer

Goal: scripting, debugging, flexibility.

### UAC - No Secure Desktop
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
EnableLUA = 1
ConsentPromptBehaviorAdmin = 5
PromptOnSecureDesktop = 0
```

### PowerShell Execution Policy
```
Path: HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
ExecutionPolicy = "Unrestricted"
```

Scoped alternative:
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

| Policy | Notes |
| --- | --- |
| Restricted | No scripts (client default) |
| AllSigned | Signed scripts only |
| RemoteSigned | Downloaded scripts must be signed |
| Unrestricted | All scripts (warning shown) |
| Bypass | No restrictions |

### Zone.Identifier (Downloaded Files)
```
Path: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Attachments
SaveZoneInformation = 1
```

Manual:
```powershell
Unblock-File -Path "C:\Downloads\script.ps1"
dir C:\Downloads\*.ps1 | Unblock-File
```

### Windows Defender - Dev Exclusions
```powershell
Add-MpPreference -ExclusionPath "C:\Users\$env:USERNAME\source\repos"
Add-MpPreference -ExclusionPath "C:\dev"
Add-MpPreference -ExclusionPath "$env:LOCALAPPDATA\Packages"
```

### VBS/HVCI - Optional
- OFF for debugging if a tool conflicts with HVCI
- ON for production-like testing

### Sudo (Windows 11)
```
Path: HKLM\Software\Policies\Microsoft\Windows\Sudo
Enabled = 3
```

| Value | Mode |
| --- | --- |
| 0 | Disabled |
| 1 | Force new window |
| 2 | Disable input |
| 3 | Normal (inline) |

### TDR - Light Increase
```
TdrDelay = 10
TdrDdiDelay = 10
```

---

## 5. Home User / Daily Use

Goal: balance security and usability.

### UAC - Default
```
Path: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
EnableLUA = 1
ConsentPromptBehaviorAdmin = 5
PromptOnSecureDesktop = 1
```

### VBS/HVCI
Keep enabled on Windows 11.

### Windows Defender
Leave enabled with defaults.

### Firewall
Keep enabled for all profiles.

### Windows Update
Keep automatic updates enabled.

### TLS
Default Windows settings are sufficient.

### Optional: Dynamic Lock
```
Path: HKCU\Software\Microsoft\Windows NT\CurrentVersion\Winlogon
EnableGoodbye = 1
```

---

## 6. Virtual Machine Host

Goal: VM performance with strong host isolation.

### VBS - ON for Host
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard
EnableVirtualizationBasedSecurity = 1
```

### Hyper-V
```cmd
bcdedit /set hypervisorlaunchtype auto
```

### Opt-Out for Guest VMs
```powershell
Set-VMSecurity -VMName "VMName" -VirtualizationBasedSecurityOptOut $true
```

### Firewall
Keep enabled for isolation.

---

## 7. Offline / Air-Gapped System

Goal: isolated system with no external connectivity.

### Windows Update - Disabled
```
Path: HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings
PauseFeatureUpdatesEndTime = "2099-01-01T00:00:00Z"
PauseQualityUpdatesEndTime = "2099-01-01T00:00:00Z"
PauseUpdatesExpiryTime = "2099-01-01T00:00:00Z"
```

```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate
DisableWindowsUpdateAccess = 1
DoNotConnectToWindowsUpdateInternetLocations = 1
```

```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU
NoAutoUpdate = 1
```

### Driver Updates - Disabled
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate
ExcludeWUDriversInQualityUpdate = 1
```

```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DriverSearching
SearchOrderConfig = 0
DontSearchWindowsUpdate = 1
```

### Telemetry - Minimum
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection
AllowTelemetry = 0
```

### P2P Updates - Disabled
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization
DODownloadMode = 0
```

---

## 8. Server / Workstation

Goal: uptime, security, remote management.

### UAC - Default
```
ConsentPromptBehaviorAdmin = 5
PromptOnSecureDesktop = 1
```

### VBS/HVCI
Enable for security.

### Firewall - Strict Rules
```powershell
New-NetFirewallRule -DisplayName "RDP from Admin" -Direction Inbound -LocalPort 3389 -Protocol TCP -RemoteAddress 192.168.1.0/24 -Action Allow
```

### TLS
Only TLS 1.2+ enabled.

### Password Policy
```cmd
NET ACCOUNTS /MAXPWAGE:90 /MINPWLEN:12 /UNIQUEPW:5
```

### RDP - NLA Required
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp
UserAuthentication = 1
SecurityLayer = 2
```

---

## 9. Privacy Focused

Goal: minimize data sharing and telemetry.

### Telemetry - Minimum
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection
AllowTelemetry = 0
```

### P2P Updates - Disabled
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization
DODownloadMode = 0
```

### WPBT - Disabled
```
Path: HKLM\SYSTEM\CurrentControlSet\Control\Session Manager
DisableWpbtExecution = 1
```

### Device Metadata - Disabled
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\Device Metadata
PreventDeviceMetadataFromNetwork = 1
```

### Clipboard Sync - Disabled
```
Path: HKLM\SOFTWARE\Policies\Microsoft\Windows\System
AllowCrossDeviceClipboard = 0
```

### Camera Indicator (OEM)
```
Path: HKLM\SOFTWARE\Microsoft\OEM\Device\Capture
NoPhysicalCameraLED = 1
```

### Password Reveal - Disabled
```
Path: HKLM\Software\Policies\Microsoft\Windows\CredUI
DisablePasswordReveal = 1
```

---

## Quick Reference Matrix

| Scenario | UAC | VBS | Defender | Firewall | TDR | TLS |
| --- | --- | --- | --- | --- | --- | --- |
| Max Security | Max (2) | ON + Lock | Full | ON | Default | TLS 1.2+ only |
| Gaming | No prompt (0) | OFF | Exclusions | ON | 10s | Default |
| AI/ML | Low (0/5) | Optional | Exclusions | ON | 60-120s | Default |
| Developer | No secure desktop (5) | Optional | Exclusions | ON | 10s | Default |
| Home User | Default (5) | ON | Full | ON | Default | Default |
| VM Host | Default (5) | ON | Full | ON | Default | Default |
| Offline | Default (5) | ON | Offline | Strict | Default | Strict |
| Server | Default (5) | ON | Full | Strict | Default | TLS 1.2+ only |
| Privacy | Default (5) | ON | Full | ON | Default | Default |

---

## Example Registry Scripts

### Gaming / Performance
```reg
Windows Registry Editor Version 5.00

; UAC - No prompt (warning: reduces security)
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
"EnableLUA"=dword:00000001
"ConsentPromptBehaviorAdmin"=dword:00000000
"PromptOnSecureDesktop"=dword:00000000

; VBS off
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard]
"EnableVirtualizationBasedSecurity"=dword:00000000

; TDR increased
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers]
"TdrDelay"=dword:0000000a
"TdrDdiDelay"=dword:0000000a
```

### AI/ML Workstation
```reg
Windows Registry Editor Version 5.00

; TDR for long GPU jobs
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers]
"TdrDelay"=dword:00000078
"TdrDdiDelay"=dword:0000003c
"TdrLimitCount"=dword:0000000a
"TdrLimitTime"=dword:00000078

; UAC low
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
"EnableLUA"=dword:00000001
"ConsentPromptBehaviorAdmin"=dword:00000000
"PromptOnSecureDesktop"=dword:00000000

; PowerShell unrestricted
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell]
"ExecutionPolicy"="Unrestricted"
```

### Developer Setup
```reg
Windows Registry Editor Version 5.00

; UAC - no secure desktop
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
"EnableLUA"=dword:00000001
"ConsentPromptBehaviorAdmin"=dword:00000005
"PromptOnSecureDesktop"=dword:00000000

; PowerShell unrestricted
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell]
"ExecutionPolicy"="Unrestricted"

; Zone.Identifier handling
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Attachments]
"SaveZoneInformation"=dword:00000001

; TDR light increase
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers]
"TdrDelay"=dword:0000000a
"TdrDdiDelay"=dword:0000000a

; Sudo (Windows 11)
[HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Sudo]
"Enabled"=dword:00000003
```

### Maximum Security
```reg
Windows Registry Editor Version 5.00

; UAC - max
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
"EnableLUA"=dword:00000001
"ConsentPromptBehaviorAdmin"=dword:00000002
"PromptOnSecureDesktop"=dword:00000001
"FilterAdministratorToken"=dword:00000001
"ValidateAdminCodeSignatures"=dword:00000001

; VBS full
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard]
"EnableVirtualizationBasedSecurity"=dword:00000001
"RequirePlatformSecurityFeatures"=dword:00000003
"HypervisorEnforcedCodeIntegrity"=dword:00000001
"LsaCfgFlags"=dword:00000001
"ConfigureSystemGuardLaunch"=dword:00000001

; NTLM v2 only
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa]
"LmCompatibilityLevel"=dword:00000005

; Minimum DH/RSA key size
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\KeyExchangeAlgorithms\Diffie-Hellman]
"ClientMinKeyBitLength"=dword:00000c00

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\KeyExchangeAlgorithms\PKCS]
"ClientMinKeyBitLength"=dword:00000c00
```

### Privacy Focused
```reg
Windows Registry Editor Version 5.00

; Telemetry minimum
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection]
"AllowTelemetry"=dword:00000000

; P2P updates off
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization]
"DODownloadMode"=dword:00000000

; WPBT off
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager]
"DisableWpbtExecution"=dword:00000001

; Device metadata off
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Device Metadata]
"PreventDeviceMetadataFromNetwork"=dword:00000001

; Clipboard sync off
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]
"AllowCrossDeviceClipboard"=dword:00000000

; Camera OSD
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\OEM\Device\Capture]
"NoPhysicalCameraLED"=dword:00000001

; Password reveal off
[HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\CredUI]
"DisablePasswordReveal"=dword:00000001
```

### Offline System
```reg
Windows Registry Editor Version 5.00

; Windows Update disabled
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings]
"PauseFeatureUpdatesEndTime"="2099-01-01T00:00:00Z"
"PauseQualityUpdatesEndTime"="2099-01-01T00:00:00Z"
"PauseUpdatesExpiryTime"="2099-01-01T00:00:00Z"

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate]
"DisableWindowsUpdateAccess"=dword:00000001
"DoNotConnectToWindowsUpdateInternetLocations"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU]
"NoAutoUpdate"=dword:00000001

; Driver updates disabled
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate]
"ExcludeWUDriversInQualityUpdate"=dword:00000001

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DriverSearching]
"SearchOrderConfig"=dword:00000000
"DontSearchWindowsUpdate"=dword:00000001
```

---

## Disable TLS/SSL Protocols (Maximum Security)

```reg
Windows Registry Editor Version 5.00

; SSL 2.0 off
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 2.0\Client]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 2.0\Server]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

; SSL 3.0 off
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Client]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Server]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

; TLS 1.0 off
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Client]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Server]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

; TLS 1.1 off
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1\Client]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1\Server]
"Enabled"=dword:00000000
"DisabledByDefault"=dword:00000001

; TLS 1.2 on
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client]
"Enabled"=dword:00000001
"DisabledByDefault"=dword:00000000

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server]
"Enabled"=dword:00000001
"DisabledByDefault"=dword:00000000

; TLS 1.3 on
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Client]
"Enabled"=dword:00000001
"DisabledByDefault"=dword:00000000

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Server]
"Enabled"=dword:00000001
"DisabledByDefault"=dword:00000000
```

---

## Important Notes

### Security Warnings

1. Disabling UAC increases malware risk.
2. Disabling VBS/HVCI reduces kernel-level protections.
3. Disabling Defender removes malware protection.
4. Disabling Firewall exposes services.
5. Legacy TLS opens the door to MITM attacks.

### Rollback

All changes are reversible:
- Restore registry defaults
- `bcdedit /set hypervisorlaunchtype auto` (VBS)
- Re-enable services in Services.msc

### Test After Changes

1. Verify system stability
2. Confirm app compatibility
3. Benchmark performance (if relevant)

### Layered Security

Do not rely on a single setting:
- Security = UAC + Defender + Firewall + VBS + updates
- If one layer is reduced, others should remain enabled
