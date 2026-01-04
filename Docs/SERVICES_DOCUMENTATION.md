# Windows Optimizer - Servis Yönetimi Dokümantasyonu

Bu belge, uygulamanın yönettiği tüm Windows servislerini, zamanlanmış görevleri ve sistem bileşenlerini detaylı olarak açıklar.

---

## 🔧 Devre Dışı Bırakılabilir Servisler

### Telemetri ve Veri Toplama

| Servis | Görünen Ad | Risk | Açıklama |
|--------|-----------|------|----------|
| `DiagTrack` | Connected User Experiences and Telemetry | Safe | Microsoft'a telemetri gönderir |
| `dmwappushservice` | WAP Push Message Routing | Safe | Push bildirim yönlendirme |
| `WerSvc` | Windows Error Reporting Service | Safe | Hata raporları gönderir |

### Performans ve Arama

| Servis | Görünen Ad | Risk | Açıklama |
|--------|-----------|------|----------|
| `SysMain` | Superfetch | Advanced | Uygulama ön yükleme (SSD'lerde gereksiz) |
| `WSearch` | Windows Search | Advanced | Dosya indeksleme (SSD'de hız farkı az) |

### Yazdırma

| Servis | Görünen Ad | Risk | Açıklama |
|--------|-----------|------|----------|
| `Spooler` | Print Spooler | Risky | Yazıcı yoksa devre dışı bırakılabilir |
| `PrintNotify` | Printer Notifications | Safe | Yazıcı bildirimleri |
| `PrintWorkflowUserSvc_*` | Per-user Print Workflow | Safe | Kullanıcı bazlı yazdırma |
| `PrintDeviceConfigurationService` | Printer Device Configuration | Safe | Yazıcı yapılandırma |
| `PrintScanBrokerService` | Print/Scan Broker | Safe | Yazdırma/tarama aracı |

### Bluetooth

| Servis | Görünen Ad | Risk | Açıklama |
|--------|-----------|------|----------|
| `bthserv` | Bluetooth Support Service | Risky | BT kullanmıyorsanız kapatın |
| `BluetoothUserService_*` | Per-user Bluetooth | Safe | Kullanıcı bazlı BT |
| `BTAGService` | Bluetooth Audio Gateway | Advanced | BT ses geçidi |

---

## ⏰ Devre Dışı Bırakılabilir Zamanlanmış Görevler

### Telemetri Görevleri

| Görev Yolu | Açıklama |
|------------|----------|
| `\Microsoft\Windows\Application Experience\MareBackup` | Uygulama uyumluluğu yedekleme |
| `\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser` | Uyumluluk değerlendirmesi |
| `\Microsoft\Windows\Customer Experience Improvement Program\Consolidator` | CEIP veri birleştirme |
| `\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip` | USB CEIP |
| `\Microsoft\Windows\Feedback\Siuf\DmClient` | Geri bildirim istemcisi |
| `\Microsoft\Windows\Windows Error Reporting\QueueReporting` | Hata raporu kuyruğu |

### Bakım Görevleri

| Görev Yolu | Risk | Açıklama |
|------------|------|----------|
| `\Microsoft\Windows\DiskCleanup\SilentCleanup` | Safe | Otomatik disk temizleme |
| `\Microsoft\Windows\Diagnosis\Scheduled` | Safe | Zamanlanmış tanılama |
| `\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector` | Safe | Disk tanılama |
| `\Microsoft\Windows\Maintenance\WinSAT` | Safe | Windows Sistem Değerlendirmesi |
| `\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem` | Safe | Güç verimliliği analizi |

### Cihaz Bilgi Görevleri

| Görev Yolu | Açıklama |
|------------|----------|
| `\Microsoft\Windows\Device Information\Device` | Cihaz bilgisi toplama |
| `\Microsoft\Windows\Device Information\Device User` | Kullanıcı cihaz bilgisi |

---

## 📊 Tweak Kategorileri ve Sayıları

| Kategori | Dosya | Tweak Sayısı | Açıklama |
|----------|-------|--------------|----------|
| System | `SystemTweakProvider.cs` | 9 | Game Mode, Startup Delay, Services |
| System Registry | `SystemRegistryTweakProvider.cs` | 30+ | Kernel, NTFS, DWM |
| Privacy | `PrivacyTweakProvider.cs` | 70+ | Telemetri, Konum, Aktivite |
| Security | `SecurityTweakProvider.cs` | 15 | UAC, Firewall, VBS |
| Network | `NetworkTweakProvider.cs` | 30+ | SMB, IPv6, Adaptörler |
| Performance | `PerformanceTweakProvider.cs` | 8 | Animasyonlar, Throttling |
| Peripheral | `PeripheralTweakProvider.cs` | 10 | Mouse, Keyboard |
| Audio | `AudioTweakProvider.cs` | 6 | Beep, Ducking |
| Visibility | `VisibilityTweakProvider.cs` | 25 | UI öğeleri, Spotlight |
| Misc | `MiscTweakProvider.cs` | 5 | 3rd party apps |
| Legacy | `LegacyTweakProvider.cs` | 100+ | Eski tweak kataloğu |

**Toplam: 278+ tweak** (`Docs/tweaks/tweak-catalog.csv` kaynağı)

---

## 🔒 Yetki Gereksinimleri

### Admin Gerektiren İşlemler

- `HKLM` registry yazma
- Servis başlangıç modunu değiştirme
- Zamanlanmış görev devre dışı bırakma
- Sistem dosyası işlemleri
- BCD (Boot Configuration Data) değişiklikleri

### Kullanıcı Seviyesi İşlemler

- `HKCU` registry yazma
- Kullanıcı profil ayarları
- Tema ve görünüm değişiklikleri

---

## 📁 İlgili Dosyalar

```
WindowsOptimizer.App/Services/TweakProviders/
├── AudioTweakProvider.cs
├── BaseTweakProvider.cs     # Abstract base class
├── LegacyTweakProvider.cs   # 100+ eski tweak
├── MiscTweakProvider.cs
├── NetworkTweakProvider.cs
├── PerformanceTweakProvider.cs
├── PeripheralTweakProvider.cs
├── PrivacyTweakProvider.cs
├── SecurityTweakProvider.cs
├── SystemRegistryTweakProvider.cs
├── SystemTweakProvider.cs
└── VisibilityTweakProvider.cs
```

---

## 🔗 Referanslar

- [Windows Services Reference](https://learn.microsoft.com/en-us/windows/application-management/per-user-services-in-windows)
- [Task Scheduler Reference](https://learn.microsoft.com/en-us/windows/win32/taskschd/task-scheduler-start-page)
- [Service Control Manager](https://learn.microsoft.com/en-us/windows/win32/services/service-control-manager)
