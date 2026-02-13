# Tweak Sources

> **Tek Kaynak Prensibi**: Her tweak'in kaynağı burada açıkça belirtilmiştir.
>

## 📚 Ana Kaynaklar

| Kaynak | Link | Açıklama | Trust Level |
|--------|------|----------|-------------|
| **nohuto/win-config** | [github.com/nohuto/win-config](https://github.com/nohuto/win-config) | Registry reverse engineering (IDA Pro, WPR) | ⭐⭐⭐⭐ |
| **Microsoft Learn** | [learn.microsoft.com](https://learn.microsoft.com) | Resmi Windows dokümantasyonu | ⭐⭐⭐⭐⭐ |
| **Microsoft Support** | [support.microsoft.com](https://support.microsoft.com) | Resmi destek ve performans kılavuzları | ⭐⭐⭐⭐⭐ |
| **Windows Internals** | Windows Internals 7th Edition | Kernel-level teknik detaylar | ⭐⭐⭐⭐⭐ |

---

## 🔐 Güvenlik Kaynakları (Yüksek Trust)

### Microsoft Security Baselines
- **URL**: [Microsoft Security Baselines](https://learn.microsoft.com/en-us/windows/security/operating-system-security/device-management/windows-security-configuration-framework/windows-security-baselines)
- **İndirme**: [Security Compliance Toolkit (SCT)](https://www.microsoft.com/download/details.aspx?id=55319)
- **Trust**: ⭐⭐⭐⭐⭐ (Resmi Microsoft)
- **İçerik**: Group Policy güvenlik ayarları, device management politikaları
- **Kategoriler**: SecurityTweakProvider, SystemTweakProvider

### Australian Signals Directorate (ASD) - Windows Hardening
- **Windows 11**: [ASD Windows 11 Hardening Guide (PDF)](https://www.cyber.gov.au/sites/default/files/2025-09/hardening_microsoft_windows_11_workstations_september_2025.pdf)
- **Windows 10**: [ASD Windows 10 Hardening Guide (PDF)](https://www.cyber.gov.au/sites/default/files/2025-09/hardening_microsoft_windows_10_workstations_september_2025.pdf)
- **Trust**: ⭐⭐⭐⭐⭐ (Avustralya Hükümeti)
- **İçerik**: Enterprise-grade workstation hardening
- **Kategoriler**: SecurityTweakProvider (Enterprise ayarları)

### DISA STIGs (Defense Information Systems Agency)
- **URL**: [public.cyber.mil/stigs](https://public.cyber.mil/stigs/downloads/)
- **Trust**: ⭐⭐⭐⭐⭐ (ABD Savunma Bakanlığı)
- **İçerik**: Security Technical Implementation Guides
- **Araçlar**: SCAP Compliance Checker (SCC)
- **Kategoriler**: SecurityTweakProvider (En kısıtlayıcı ayarlar)

### CIS Benchmarks (Center for Internet Security)
- **URL**: [CIS Benchmarks](https://www.cisecurity.org/cis-benchmarks)
- **Trust**: ⭐⭐⭐⭐⭐ (Endüstri standardı)
- **İçerik**: Hardened images, configuration baselines
- **Kategoriler**: Tüm kategoriler (Endüstri standardı doğrulama)

### NIST Zero Trust Architecture
- **URL**: [NIST Zero Trust Hardening](https://pages.nist.gov/zero-trust-architecture/VolumeC/Hardening.html)
- **Trust**: ⭐⭐⭐⭐⭐ (Federal standartlar)
- **İçerik**: Zero Trust implementasyonu, Windows hardening
- **Kategoriler**: SecurityTweakProvider, NetworkTweakProvider

---

## 🔧 Tweak Kategorileri ve Kaynakları

### System (Kernel, DPC, Scheduler)
- **Kaynak**: nohuto/win-config → [system.md](https://github.com/nohuto/win-config/blob/main/system/system.md)
- **Ek Kaynak**: Windows Internals 7th Edition (Part 2)
- **Microsoft**: [Svchost Service Refactoring](https://learn.microsoft.com/en-us/windows/application-management/svchost-service-refactoring)
- Tweaks: Win32PrioritySeparation, DPC settings, Timer Resolution, SvcHostSplitThreshold

### Graphics (TDR, DWM, HAGS)
- **Kaynak**: nohuto/win-config → [visibility.md](https://github.com/nohuto/win-config/blob/main/visibility/visibility.md)
- Tweaks: TdrDelay, HwSchMode, DWM optimizations

### Network (SMB, DNS, IPv6)
- **Kaynak**: nohuto/win-config → [network.md](https://github.com/nohuto/win-config/blob/main/network/network.md)
- **Microsoft**: [Windows Networking](https://learn.microsoft.com/en-us/windows-server/networking/)
- **Microsoft**: [SMB Documentation](https://learn.microsoft.com/en-us/windows-server/storage/file-server/file-server-smb-overview)
- Tweaks: SMB signing, LLMNR, mDNS, IPv6 disable

### Power Management
- **Kaynak**: nohuto/win-config → [power.md](https://github.com/nohuto/win-config/blob/main/power/power.md)
- Tweaks: USB Suspend, Modern Standby, Power Throttling

### Privacy
- **Kaynak**: nohuto/win-config → [privacy.md](https://github.com/nohuto/win-config/blob/main/privacy/privacy.md)
- **Microsoft**: [Windows Privacy](https://learn.microsoft.com/en-us/windows/privacy/)
- **Microsoft**: [Privacy Dashboard](https://account.microsoft.com/privacy)
- Tweaks: Telemetry, Cortana, Activity History

### Performance (MMCSS)
- **Kaynak**: Microsoft Learn → [MMCSS](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service)
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

## ✅ Doğrulama Yöntemi

1. Registry değeri nohuto docs'ta aranır
2. Microsoft Learn'de cross-check yapılır (varsa)
3. Güvenlik ayarları için Microsoft Security Baselines kontrol edilir
4. Enterprise ayarları için ASD/DISA guidelines kontrol edilir
5. Endüstri standardı için CIS Benchmarks karşılaştırılır
6. Değer bulunamazsa → tweak eklenmez

---

## 📊 Trust Seviye Hiyerarşisi

```
Tier 1 (En Yüksek):
├── Microsoft Learn (Resmi)
├── Microsoft Support (Resmi)
├── Microsoft Security Baselines (Resmi)
├── ASD Guidelines (Hükümet)
├── DISA STIGs (ABD DoD)
└── CIS Benchmarks (Endüstri standardı)

Tier 2 (Araştırma):
├── Windows Internals Book (Microsoft Press)
├── nohuto/win-config (Reverse Engineering)
└── Windows Source Code References

Tier 3 (Topluluk):
├── Microsoft Tech Community
└── GitHub Microsoft Repos
```

---

*Her tweak'in tek ve net kaynağı vardır. Karışık linkler yok.*
*Son güncelleme: 12 Şubat 2026*
