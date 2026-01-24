# Tweak Sources

> **Tek Kaynak Prensibi**: Her tweak'in kaynağı burada açıkça belirtilmiştir.

## 📚 Ana Kaynaklar

| Kaynak | Link | Açıklama |
|--------|------|----------|
| **nohuto/win-config** | [github.com/nohuto/win-config](https://github.com/nohuto/win-config) | Registry reverse engineering (IDA Pro, WPR) |
| **Microsoft Learn** | [learn.microsoft.com](https://learn.microsoft.com) | Resmi Windows dokümantasyonu |

---

## 🔧 Tweak Kategorileri ve Kaynakları

### System (Kernel, DPC, Scheduler)
- **Kaynak**: nohuto/win-config → [system.md](https://github.com/nohuto/win-config/blob/main/system/system.md)
- Tweaks: Win32PrioritySeparation, DPC settings, Timer Resolution

### Graphics (TDR, DWM, HAGS)
- **Kaynak**: nohuto/win-config → [visibility.md](https://github.com/nohuto/win-config/blob/main/visibility/visibility.md)
- Tweaks: TdrDelay, HwSchMode, DWM optimizations

### Network (SMB, DNS, IPv6)
- **Kaynak**: nohuto/win-config → [network.md](https://github.com/nohuto/win-config/blob/main/network/network.md)
- Tweaks: SMB signing, LLMNR, mDNS, IPv6 disable

### Power Management
- **Kaynak**: nohuto/win-config → [power.md](https://github.com/nohuto/win-config/blob/main/power/power.md)
- Tweaks: USB Suspend, Modern Standby, Power Throttling

### Privacy
- **Kaynak**: nohuto/win-config → [privacy.md](https://github.com/nohuto/win-config/blob/main/privacy/privacy.md)
- Tweaks: Telemetry, Cortana, Activity History

### Performance (MMCSS)
- **Kaynak**: Microsoft Learn → [MMCSS](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service)
- Tweaks: SystemResponsiveness, NetworkThrottlingIndex

---

## ✅ Doğrulama Yöntemi

1. Registry değeri nohuto docs'ta aranır
2. Microsoft Learn'de cross-check yapılır (varsa)
3. Değer bulunamazsa → tweak eklenmez

---

*Her tweak'in tek ve net kaynağı vardır. Karışık linkler yok.*
