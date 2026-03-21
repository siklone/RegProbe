# Tweak Sources

> **Tek Kaynak Prensibi**: Her tweak'in kaynaÄŸÄ± burada aÃ§Ä±kÃ§a belirtilmiÅŸtir.
>

## ðŸ“š Ana Kaynaklar

| Kaynak | Link | AÃ§Ä±klama | Trust Level |
|--------|------|----------|-------------|
| **nohuto/win-config** | [github.com/nohuto/win-config](https://github.com/nohuto/win-config) | Registry reverse engineering (IDA Pro, WPR) | â­â­â­â­ |
| **Microsoft Learn** | [learn.microsoft.com](https://learn.microsoft.com) | Resmi Windows dokÃ¼mantasyonu | â­â­â­â­â­ |
| **Microsoft Support** | [support.microsoft.com](https://support.microsoft.com) | Resmi destek ve performans kÄ±lavuzlarÄ± | â­â­â­â­â­ |
| **Windows Internals** | Windows Internals 7th Edition | Kernel-level teknik detaylar | â­â­â­â­â­ |

---

## ðŸ” GÃ¼venlik KaynaklarÄ± (YÃ¼ksek Trust)

### Microsoft Security Baselines
- **URL**: [Microsoft Security Baselines](https://learn.microsoft.com/en-us/windows/security/operating-system-security/device-management/windows-security-configuration-framework/windows-security-baselines)
- **Ä°ndirme**: [Security Compliance Toolkit (SCT)](https://www.microsoft.com/download/details.aspx?id=55319)
- **Trust**: â­â­â­â­â­ (Resmi Microsoft)
- **Ä°Ã§erik**: Group Policy gÃ¼venlik ayarlarÄ±, device management politikalarÄ±
- **Kategoriler**: SecurityTweakProvider, SystemTweakProvider

### Australian Signals Directorate (ASD) - Windows Hardening
- **Windows 11**: [ASD Windows 11 Hardening Guide (PDF)](https://www.cyber.gov.au/sites/default/files/2025-09/hardening_microsoft_windows_11_workstations_september_2025.pdf)
- **Windows 10**: [ASD Windows 10 Hardening Guide (PDF)](https://www.cyber.gov.au/sites/default/files/2025-09/hardening_microsoft_windows_10_workstations_september_2025.pdf)
- **Trust**: â­â­â­â­â­ (Avustralya HÃ¼kÃ¼meti)
- **Ä°Ã§erik**: Enterprise-grade workstation hardening
- **Kategoriler**: SecurityTweakProvider (Enterprise ayarlarÄ±)

### DISA STIGs (Defense Information Systems Agency)
- **URL**: [public.cyber.mil/stigs](https://public.cyber.mil/stigs/downloads/)
- **Trust**: â­â­â­â­â­ (ABD Savunma BakanlÄ±ÄŸÄ±)
- **Ä°Ã§erik**: Security Technical Implementation Guides
- **AraÃ§lar**: SCAP Compliance Checker (SCC)
- **Kategoriler**: SecurityTweakProvider (En kÄ±sÄ±tlayÄ±cÄ± ayarlar)

### CIS Benchmarks (Center for Internet Security)
- **URL**: [CIS Benchmarks](https://www.cisecurity.org/cis-benchmarks)
- **Trust**: â­â­â­â­â­ (EndÃ¼stri standardÄ±)
- **Ä°Ã§erik**: Hardened images, configuration baselines
- **Kategoriler**: TÃ¼m kategoriler (EndÃ¼stri standardÄ± doÄŸrulama)

### NIST Zero Trust Architecture
- **URL**: [NIST Zero Trust Hardening](https://pages.nist.gov/zero-trust-architecture/VolumeC/Hardening.html)
- **Trust**: â­â­â­â­â­ (Federal standartlar)
- **Ä°Ã§erik**: Zero Trust implementasyonu, Windows hardening
- **Kategoriler**: SecurityTweakProvider, NetworkTweakProvider

---

## ðŸ”§ Tweak Kategorileri ve KaynaklarÄ±

### System (Kernel, DPC, Scheduler)
- **Kaynak**: nohuto/win-config â†’ [system.md](https://github.com/nohuto/win-config/blob/main/system/system.md)
- **Ek Kaynak**: Windows Internals 7th Edition (Part 2)
- **Microsoft**: [Svchost Service Refactoring](https://learn.microsoft.com/en-us/windows/application-management/svchost-service-refactoring)
- Tweaks: Win32PrioritySeparation, DPC settings, Timer Resolution, SvcHostSplitThreshold

### Graphics (TDR, DWM, HAGS)
- **Kaynak**: nohuto/win-config â†’ [visibility.md](https://github.com/nohuto/win-config/blob/main/visibility/visibility.md)
- Tweaks: TdrDelay, HwSchMode, DWM optimizations

### Network (SMB, DNS, IPv6)
- **Kaynak**: nohuto/win-config â†’ [network.md](https://github.com/nohuto/win-config/blob/main/network/network.md)
- **Microsoft**: [Windows Networking](https://learn.microsoft.com/en-us/windows-server/networking/)
- **Microsoft**: [SMB Documentation](https://learn.microsoft.com/en-us/windows-server/storage/file-server/file-server-smb-overview)
- Tweaks: SMB signing, LLMNR, mDNS, IPv6 disable

### Power Management
- **Kaynak**: nohuto/win-config â†’ [power.md](https://github.com/nohuto/win-config/blob/main/power/power.md)
- Tweaks: USB Suspend, Modern Standby, Power Throttling

### Privacy
- **Kaynak**: nohuto/win-config â†’ [privacy.md](https://github.com/nohuto/win-config/blob/main/privacy/privacy.md)
- **Microsoft**: [Windows Privacy](https://learn.microsoft.com/en-us/windows/privacy/)
- **Microsoft**: [Privacy Dashboard](https://account.microsoft.com/privacy)
- Tweaks: Telemetry, Cortana, Activity History

### Performance (MMCSS)
- **Kaynak**: Microsoft Learn â†’ [MMCSS](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service)
- **Microsoft**: [PC Performance Tips](https://support.microsoft.com/en-us/windows/tips-to-improve-pc-performance-in-windows-b3b3ef5b-5953-fb6a-2528-4bbed82fba96)
- **Microsoft**: [Windows 11 Performance](https://techcommunity.microsoft.com/blog/microsoftmechanicsblog/windows-11-the-optimization-and-performance-improvements/2733299)
- Tweaks: SystemResponsiveness, NetworkThrottlingIndex

### Security
- **Kaynak**: Microsoft Security Baselines
- **Kaynak**: ASD Windows Hardening Guides
- **Kaynak**: DISA STIGs
- **Kaynak**: CIS Benchmarks
- **Microsoft**: [Windows Hardening Guidance](https://support.microsoft.com/en-us/topic/latest-windows-hardening-guidance-and-key-dates-eb1bd411-f68c-4d74-a4e1-456721a6551b)
- **Microsoft**: [Defender Performance Mode](https://learn.microsoft.com/en-us/defender-endpoint/microsoft-defender-endpoint-antivirus-performance-mode)

### Developer Tools (Yeni!)
- **Microsoft**: [Windows Long Paths](https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation)
- **Microsoft**: [.NET Telemetry](https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry)
- **Microsoft**: [Developer Mode](https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development)
- **Microsoft**: [PowerShell Execution Policy](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy)
- **Microsoft**: [WSL Configuration](https://learn.microsoft.com/en-us/windows/wsl/wsl-config)
- **Git**: [Git Config Documentation](https://git-scm.com/docs/git-config)
- **GitHub**: [Git Credential Manager](https://github.com/GitCredentialManager/git-credential-manager)
- **GitHub**: [Git SSH Documentation](https://git-scm.com/book/en/v2/Git-on-the-Server-Generating-Your-SSH-Public-Key)

---

## âœ… DoÄŸrulama YÃ¶ntemi

1. Registry deÄŸeri nohuto docs'ta aranÄ±r
2. Microsoft Learn'de cross-check yapÄ±lÄ±r (varsa)
3. GÃ¼venlik ayarlarÄ± iÃ§in Microsoft Security Baselines kontrol edilir
4. Enterprise ayarlarÄ± iÃ§in ASD/DISA guidelines kontrol edilir
5. EndÃ¼stri standardÄ± iÃ§in CIS Benchmarks karÅŸÄ±laÅŸtÄ±rÄ±lÄ±r
6. DeÄŸer bulunamazsa â†’ tweak eklenmez

---

## ðŸ“Š Trust Seviye HiyerarÅŸisi

```
Tier 1 (En YÃ¼ksek):
â”œâ”€â”€ Microsoft Learn (Resmi)
â”œâ”€â”€ Microsoft Support (Resmi)
â”œâ”€â”€ Microsoft Security Baselines (Resmi)
â”œâ”€â”€ ASD Guidelines (HÃ¼kÃ¼met)
â”œâ”€â”€ DISA STIGs (ABD DoD)
â””â”€â”€ CIS Benchmarks (EndÃ¼stri standardÄ±)

Tier 2 (AraÅŸtÄ±rma):
â”œâ”€â”€ Windows Internals Book (Microsoft Press)
â”œâ”€â”€ nohuto/win-config (Reverse Engineering)
â””â”€â”€ Windows Source Code References

Tier 3 (Topluluk):
â”œâ”€â”€ Microsoft Tech Community
â””â”€â”€ GitHub Microsoft Repos
```

---

*Her tweak'in tek ve net kaynaÄŸÄ± vardÄ±r. KarÄ±ÅŸÄ±k linkler yok.*
*Son gÃ¼ncelleme: 12 Åžubat 2026*
