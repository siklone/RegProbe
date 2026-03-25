# Open Trace Project - Servis YÃ¶netimi DokÃ¼mantasyonu

Bu belge, uygulamanÄ±n yÃ¶nettiÄŸi tÃ¼m Windows servislerini, zamanlanmÄ±ÅŸ gÃ¶revleri ve sistem bileÅŸenlerini detaylÄ± olarak aÃ§Ä±klar.

---

## ðŸ”§ Devre DÄ±ÅŸÄ± BÄ±rakÄ±labilir Servisler

### Telemetri ve Veri Toplama

| Servis | GÃ¶rÃ¼nen Ad | Risk | AÃ§Ä±klama |
|--------|-----------|------|----------|
| `DiagTrack` | Connected User Experiences and Telemetry | Safe | Microsoft'a telemetri gÃ¶nderir |
| `dmwappushservice` | WAP Push Message Routing | Safe | Push bildirim yÃ¶nlendirme |
| `WerSvc` | Windows Error Reporting Service | Safe | Hata raporlarÄ± gÃ¶nderir |

### Performans ve Arama

| Servis | GÃ¶rÃ¼nen Ad | Risk | AÃ§Ä±klama |
|--------|-----------|------|----------|
| `SysMain` | Superfetch | Advanced | Uygulama Ã¶n yÃ¼kleme (SSD'lerde gereksiz) |
| `WSearch` | Windows Search | Advanced | Dosya indeksleme (SSD'de hÄ±z farkÄ± az) |

### YazdÄ±rma

| Servis | GÃ¶rÃ¼nen Ad | Risk | AÃ§Ä±klama |
|--------|-----------|------|----------|
| `Spooler` | Print Spooler | Risky | YazÄ±cÄ± yoksa devre dÄ±ÅŸÄ± bÄ±rakÄ±labilir |
| `PrintNotify` | Printer Notifications | Safe | YazÄ±cÄ± bildirimleri |
| `PrintWorkflowUserSvc_*` | Per-user Print Workflow | Safe | KullanÄ±cÄ± bazlÄ± yazdÄ±rma |
| `PrintDeviceConfigurationService` | Printer Device Configuration | Safe | YazÄ±cÄ± yapÄ±landÄ±rma |
| `PrintScanBrokerService` | Print/Scan Broker | Safe | YazdÄ±rma/tarama aracÄ± |

### Bluetooth

| Servis | GÃ¶rÃ¼nen Ad | Risk | AÃ§Ä±klama |
|--------|-----------|------|----------|
| `bthserv` | Bluetooth Support Service | Risky | BT kullanmÄ±yorsanÄ±z kapatÄ±n |
| `BluetoothUserService_*` | Per-user Bluetooth | Safe | KullanÄ±cÄ± bazlÄ± BT |
| `BTAGService` | Bluetooth Audio Gateway | Advanced | BT ses geÃ§idi |

---

## â° Devre DÄ±ÅŸÄ± BÄ±rakÄ±labilir ZamanlanmÄ±ÅŸ GÃ¶revler

### Telemetri GÃ¶revleri

| GÃ¶rev Yolu | AÃ§Ä±klama |
|------------|----------|
| `\Microsoft\Windows\Application Experience\MareBackup` | Uygulama uyumluluÄŸu yedekleme |
| `\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser` | Uyumluluk deÄŸerlendirmesi |
| `\Microsoft\Windows\Customer Experience Improvement Program\Consolidator` | CEIP veri birleÅŸtirme |
| `\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip` | USB CEIP |
| `\Microsoft\Windows\Feedback\Siuf\DmClient` | Geri bildirim istemcisi |
| `\Microsoft\Windows\Windows Error Reporting\QueueReporting` | Hata raporu kuyruÄŸu |

### BakÄ±m GÃ¶revleri

| GÃ¶rev Yolu | Risk | AÃ§Ä±klama |
|------------|------|----------|
| `\Microsoft\Windows\DiskCleanup\SilentCleanup` | Safe | Otomatik disk temizleme |
| `\Microsoft\Windows\Diagnosis\Scheduled` | Safe | ZamanlanmÄ±ÅŸ tanÄ±lama |
| `\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector` | Safe | Disk tanÄ±lama |
| `\Microsoft\Windows\Maintenance\WinSAT` | Safe | Windows Sistem DeÄŸerlendirmesi |
| `\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem` | Safe | GÃ¼Ã§ verimliliÄŸi analizi |

### Cihaz Bilgi GÃ¶revleri

| GÃ¶rev Yolu | AÃ§Ä±klama |
|------------|----------|
| `\Microsoft\Windows\Device Information\Device` | Cihaz bilgisi toplama |
| `\Microsoft\Windows\Device Information\Device User` | KullanÄ±cÄ± cihaz bilgisi |

---

## ðŸ“Š Tweak Kategorileri ve SayÄ±larÄ±

| Kategori | Dosya | Tweak SayÄ±sÄ± | AÃ§Ä±klama |
|----------|-------|--------------|----------|
| System | `SystemTweakProvider.cs` | 9 | Game Mode, Startup Delay, Services |
| System Registry | `SystemRegistryTweakProvider.cs` | 30+ | Kernel, NTFS, DWM |
| Privacy | `PrivacyTweakProvider.cs` | 70+ | Telemetri, Konum, Aktivite |
| Security | `SecurityTweakProvider.cs` | 15 | UAC, Firewall, VBS |
| Network | `NetworkTweakProvider.cs` | 30+ | SMB, IPv6, AdaptÃ¶rler |
| Performance | `PerformanceTweakProvider.cs` | 8 | Animasyonlar, Throttling |
| Peripheral | `PeripheralTweakProvider.cs` | 10 | Mouse, Keyboard |
| Audio | `AudioTweakProvider.cs` | 6 | Beep, Ducking |
| Visibility | `VisibilityTweakProvider.cs` | 25 | UI Ã¶ÄŸeleri, Spotlight |
| Misc | `MiscTweakProvider.cs` | 5 | 3rd party apps |
| Legacy | `LegacyTweakProvider.cs` | 100+ | Eski tweak kataloÄŸu |

**Toplam: 278+ tweak** (`Docs/tweaks/tweak-catalog.csv` kaynaÄŸÄ±)

---

## ðŸ”’ Yetki Gereksinimleri

### Admin Gerektiren Ä°ÅŸlemler

- `HKLM` registry yazma
- Servis baÅŸlangÄ±Ã§ modunu deÄŸiÅŸtirme
- ZamanlanmÄ±ÅŸ gÃ¶rev devre dÄ±ÅŸÄ± bÄ±rakma
- Sistem dosyasÄ± iÅŸlemleri
- BCD (Boot Configuration Data) deÄŸiÅŸiklikleri

### KullanÄ±cÄ± Seviyesi Ä°ÅŸlemler

- `HKCU` registry yazma
- KullanÄ±cÄ± profil ayarlarÄ±
- Tema ve gÃ¶rÃ¼nÃ¼m deÄŸiÅŸiklikleri

---

## ðŸ“ Ä°lgili Dosyalar

```
OpenTraceProject.App/Services/TweakProviders/
â”œâ”€â”€ AudioTweakProvider.cs
â”œâ”€â”€ BaseTweakProvider.cs     # Abstract base class
â”œâ”€â”€ LegacyTweakProvider.cs   # 100+ eski tweak
â”œâ”€â”€ MiscTweakProvider.cs
â”œâ”€â”€ NetworkTweakProvider.cs
â”œâ”€â”€ PerformanceTweakProvider.cs
â”œâ”€â”€ PeripheralTweakProvider.cs
â”œâ”€â”€ PrivacyTweakProvider.cs
â”œâ”€â”€ SecurityTweakProvider.cs
â”œâ”€â”€ SystemRegistryTweakProvider.cs
â”œâ”€â”€ SystemTweakProvider.cs
â””â”€â”€ VisibilityTweakProvider.cs
```

---

## ðŸ”— Referanslar

- [Windows Services Reference](https://learn.microsoft.com/en-us/windows/application-management/per-user-services-in-windows)
- [Task Scheduler Reference](https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-start-page)
- [Service Control Manager](https://learn.microsoft.com/en-us/windows/win32/services/service-control-manager)
