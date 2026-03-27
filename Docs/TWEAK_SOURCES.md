# Tweak Sources

> Single-source rule: every shipped tweak should point to a clear primary source and, when possible, a second confirming layer such as VM runtime evidence or official Microsoft documentation.

## Primary source families

| Source | Link | What it is used for | Trust level |
| --- | --- | --- | --- |
| `nohuto/win-config` | [github.com/nohuto/win-config](https://github.com/nohuto/win-config) | Upstream configuration discovery, state expectations, and reverse-engineered option catalogs | High |
| Microsoft Learn | [learn.microsoft.com](https://learn.microsoft.com) | Official Windows, policy, API, and product documentation | Highest |
| Microsoft Support | [support.microsoft.com](https://support.microsoft.com) | Official support guidance, KBs, and operational notes | Highest |
| Windows Internals | Book reference | Deep subsystem semantics and architecture background | Highest |

## High-trust security sources

### Microsoft Security Baselines

- URL: [Microsoft Security Baselines](https://learn.microsoft.com/en-us/windows/security/operating-system-security/device-management/windows-security-configuration-framework/windows-security-baselines)
- Download: [Security Compliance Toolkit](https://www.microsoft.com/download/details.aspx?id=55319)
- Trust: official Microsoft baseline material
- Typical use: Group Policy security settings and managed-device hardening guidance

### Australian Signals Directorate hardening guidance

- Windows 11: [ASD Windows 11 hardening guide](https://www.cyber.gov.au/sites/default/files/2025-09/hardening_microsoft_windows_11_workstations_september_2025.pdf)
- Windows 10: [ASD Windows 10 hardening guide](https://www.cyber.gov.au/sites/default/files/2025-09/hardening_microsoft_windows_10_workstations_september_2025.pdf)
- Trust: government hardening guidance
- Typical use: enterprise-facing workstation hardening decisions

### DISA STIGs

- URL: [DISA STIG downloads](https://public.cyber.mil/stigs/downloads/)
- Trust: U.S. Department of Defense hardening guidance
- Typical use: restrictive security baselines and compliance cross-checking

### CIS Benchmarks

- URL: [CIS Benchmarks](https://www.cisecurity.org/cis-benchmarks)
- Trust: industry-standard hardening baseline
- Typical use: broad validation across security-focused configuration choices

### NIST Zero Trust hardening guidance

- URL: [NIST Zero Trust hardening volume](https://pages.nist.gov/zero-trust-architecture/VolumeC/Hardening.html)
- Trust: federal standard and architecture guidance
- Typical use: system and network hardening rationale

## Category-to-source mapping

### System

- Upstream repo starting point: [nohuto/win-config system.md](https://github.com/nohuto/win-config/blob/main/system/system.md)
- Secondary references: Windows Internals and Microsoft service/kernel documentation
- Typical settings: scheduler, DPC, timer, service splitting, and low-level system behavior

### Graphics and visibility

- Upstream repo starting point: [nohuto/win-config visibility.md](https://github.com/nohuto/win-config/blob/main/visibility/visibility.md)
- Secondary references: DWM, graphics scheduler, display stack, and Microsoft graphics docs
- Typical settings: TDR, MPO, and UI/display presentation behavior

### Network

- Upstream repo starting point: [nohuto/win-config network.md](https://github.com/nohuto/win-config/blob/main/network/network.md)
- Secondary references:
  - [Windows networking documentation](https://learn.microsoft.com/en-us/windows-server/networking/)
  - [SMB overview](https://learn.microsoft.com/en-us/windows-server/storage/file-server/file-server-smb-overview)
- Typical settings: SMB signing, LLMNR, mDNS, IPv6, and adapter-backed tuning

### Power

- Upstream repo starting point: [nohuto/win-config power.md](https://github.com/nohuto/win-config/blob/main/power/power.md)
- Secondary references: power policy documentation, platform power guides, and runtime traces
- Typical settings: USB suspend, Modern Standby-related behavior, and power throttling

### Privacy

- Upstream repo starting point: [nohuto/win-config privacy.md](https://github.com/nohuto/win-config/blob/main/privacy/privacy.md)
- Secondary references:
  - [Windows privacy documentation](https://learn.microsoft.com/en-us/windows/privacy/)
  - [Microsoft privacy dashboard](https://account.microsoft.com/privacy)
- Typical settings: telemetry, Cortana, activity history, and related privacy policies

### Performance

- Primary official reference: [MMCSS documentation](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service)
- Secondary references:
  - [Tips to improve PC performance](https://support.microsoft.com/en-us/windows/tips-to-improve-pc-performance-in-windows-b3b3ef5b-5953-fb6a-2528-4bbed82fba96)
  - [Windows 11 performance overview](https://techcommunity.microsoft.com/blog/microsoftmechanicsblog/windows-11-the-optimization-and-performance-improvements/2733299)
- Typical settings: `SystemResponsiveness`, `NetworkThrottlingIndex`, and related responsiveness values

### Security

- Primary references:
  - Microsoft Security Baselines
  - ASD hardening guidance
  - DISA STIGs
  - CIS Benchmarks
- Secondary references:
  - [Windows hardening guidance](https://support.microsoft.com/en-us/topic/latest-windows-hardening-guidance-and-key-dates-eb1bd411-f68c-4d74-a4e1-456721a6551b)
  - [Defender performance mode](https://learn.microsoft.com/en-us/defender-endpoint/microsoft-defender-endpoint-antivirus-performance-mode)

### Developer tooling

- [Windows long path support](https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation)
- [.NET CLI telemetry](https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry)
- [Developer Mode](https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development)
- [PowerShell execution policy](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy)
- [WSL configuration](https://learn.microsoft.com/en-us/windows/wsl/wsl-config)
- [Git config reference](https://git-scm.com/docs/git-config)
- [Git Credential Manager](https://github.com/GitCredentialManager/git-credential-manager)
- [Git SSH setup guide](https://git-scm.com/book/en/v2/Git-on-the-Server-Generating-Your-SSH-Public-Key)

## Validation method

1. Start with the upstream or official source that defines the setting.
2. Cross-check against Microsoft Learn or another primary vendor/government source when one exists.
3. For security settings, compare against Microsoft baselines and, when relevant, ASD, DISA, or CIS guidance.
4. Confirm runtime behavior in the `Win25H2Clean` VM with ETW, Procmon, WPR/WPA, Ghidra, or the v3.1 pipeline.
5. If the value cannot be sourced clearly or validated safely, do not ship it as a normal tweak.

## Trust hierarchy

```text
Tier 1:
|- Microsoft Learn
|- Microsoft Support
|- Microsoft Security Baselines
|- ASD hardening guidance
|- DISA STIGs
`- CIS Benchmarks

Tier 2:
|- Windows Internals
|- nohuto upstream repos
`- Windows source-derived references and decompilation evidence

Tier 3:
|- Microsoft Tech Community
`- Other ecosystem and community references
```

Every tweak should have one clear primary source and a documented validation path.
