# 🔍 Windows Optimizer - Kapsamlı Proje Analizi

**Oluşturulma Tarihi:** 30 Aralık 2025  
**Analiz Versiyonu:** 1.0.0  
**Hedef:** Codex/AI Ajanları için detaylı proje referansı

---

## 📊 Genel Bakış

Windows Optimizer, WPF ve .NET 8 ile geliştirilmiş, güvenli ve geri alınabilir Windows optimizasyon aracıdır. Temiz katmanlı mimari, MVVM deseni ve modüler yapıya sahiptir.

### Proje İstatistikleri

| Metrik | Değer |
|--------|-------|
| Toplam Proje Sayısı | 6 |
| C# Dosya Sayısı | 64+ |
| XAML Dosya Sayısı | 12 |
| TweakProvider Sayısı | 12 |
| Tweak Tip Sayısı | 50+ |
| Kod Satırı (TweaksViewModel) | ~1,300 (4,500'den azaltıldı) |
| Kod Satırı (Toplam Tahmini) | 15,000+ |
| Dokümantasyon Dosyası | 14 |
| Genel Tamamlanma | ~90% |

---

## 🏗️ Proje Mimarisi

### Katman Diyagramı

```
┌─────────────────────────────────────────────────────────────┐
│                  WindowsOptimizer.App                       │
│     (WPF UI Layer - Views, ViewModels, Converters)          │
│     📁 59 dosya/klasör                                      │
└────────────────────────────────┬────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────┐
│                 WindowsOptimizer.Engine                     │
│     (Business Logic - Pipeline, Tweak Implementations)      │
│     📁 59 dosya/klasör | 50+ tweak tipi                     │
└────────────────────────────────┬────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────┐
│              WindowsOptimizer.Infrastructure                │
│   (External Services - Registry, Metrics, Elevation)        │
│     📁 66 dosya/klasör                                      │
└────────────────────────────────┬────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────┐
│                  WindowsOptimizer.Core                      │
│     (Domain Models - Interfaces, Contracts, DTOs)           │
│     📁 35 dosya/klasör                                      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│              WindowsOptimizer.ElevatedHost                  │
│     (UAC Elevation - Named Pipe Communication)              │
│     📁 5 dosya                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│          WindowsOptimizer.Plugins.HelloWorld                │
│     (Example Plugin - ITweakPlugin Implementation)          │
│     📁 4 dosya                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 📁 Proje Detayları

### 1. WindowsOptimizer.Core (Domain Layer)

**Amaç:** Domain modelleri ve kontratlar

#### Dizin Yapısı
```
WindowsOptimizer.Core/
├── Cloud/              # Bulut entegrasyon modelleri (2 dosya)
├── Files/              # Dosya işlem arayüzleri (1 dosya)
├── Intelligence/       # AI/Öneri sistemi (1 dosya)
├── Models/             # Genel modeller (1 dosya)
├── Plugins/            # Plugin arayüzleri (3 dosya)
├── Registry/           # Registry arayüzleri (4 dosya)
├── Remote/             # Uzak yönetim modelleri (3 dosya)
├── Scripting/          # Script engine arayüzleri (4 dosya)
├── Security/           # Güvenlik servisleri (2 dosya)
├── Services/           # Servis arayüzleri (9 dosya)
├── Tasks/              # Zamanlanmış görev arayüzleri (2 dosya)
├── Telemetry/          # Telemetri servisi (1 dosya)
└── TweakContracts.cs   # Ana tweak kontratları
```

#### Önemli Arayüzler
| Arayüz | Açıklama |
|--------|----------|
| `ITweak` | Temel tweak kontratı (Detect, Apply, Verify, Rollback) |
| `ITweakProvider` | Kategori bazlı tweak sağlayıcı |
| `ITweakPlugin` | Plugin sistemi arayüzü |
| `IRegistryAccessor` | Registry işlem soyutlaması |
| `IServiceManager` | Windows servis yönetimi |
| `IRollbackAwareTweak` | Geri alma durumu farkındalığı |

---

### 2. WindowsOptimizer.Engine (Business Logic)

**Amaç:** İş mantığı ve tweak uygulaması

#### Dizin Yapısı
```
WindowsOptimizer.Engine/
├── Intelligence/           # Öneri motoru (1 dosya)
├── Services/               # Servis implementasyonları (1 dosya)
├── Tweaks/                 # Tweak implementasyonları (50 dosya)
│   ├── Audio/              # Ses ayarları
│   ├── Misc/               # Çeşitli tweakler
│   ├── Network/            # Ağ optimizasyonları
│   ├── Peripheral/         # Çevre birimleri
│   ├── Power/              # Güç yönetimi
│   ├── Privacy/            # Gizlilik ayarları
│   ├── Security/           # Güvenlik ayarları
│   └── System/             # Sistem ayarları
├── TweakExecutionPipeline.cs  # Ana orkestrasyon (357 satır)
├── TweakExecutionOptions.cs
├── TweakExecutionReport.cs
└── TweakExecutionStep.cs
```

#### Pipeline Akışı
```
Detect → Apply → Verify → (Rollback on failure)
   │        │        │            │
   ▼        ▼        ▼            ▼
Mevcut   Değişiklik  Doğrulama   Geri
Durum    Uygulama    Kontrolü    Alma
```

---

### 3. WindowsOptimizer.Infrastructure (External Services)

**Amaç:** Harici servis implementasyonları

#### Dizin Yapısı
```
WindowsOptimizer.Infrastructure/
├── Commands/           # Komut yürütme (5 dosya)
├── Discord/            # Discord entegrasyonu (3 dosya)
├── Elevation/          # UAC yükseltme (31 dosya)
├── Files/              # Dosya işlemleri
├── Metrics/            # Donanım metrikleri (6 dosya)
│   ├── MetricProvider.cs   # CPU/RAM/Sıcaklık
│   ├── ProcessMonitor.cs   # İşlem takibi
│   ├── NetworkMonitor.cs   # Ağ aktivitesi
│   └── DiskMonitor.cs      # Disk aktivitesi
├── Packaging/          # Paketleme (1 dosya)
├── Registry/           # Registry erişimi (2 dosya)
├── Security/           # Güvenlik (1 dosya)
├── Services/           # Windows servisleri (4 dosya)
├── RollbackStateStore.cs   # Geri alma durumu (11KB)
├── AppPaths.cs
├── AppSettings.cs
├── FileTweakLogStore.cs
└── FavoritesStore.cs
```

---

### 4. WindowsOptimizer.App (Presentation Layer)

**Amaç:** WPF kullanıcı arayüzü

#### Dizin Yapısı
```
WindowsOptimizer.App/
├── Behaviors/              # UI davranışları
├── Converters/             # Değer dönüştürücüler
├── Diagnostics/            # Performans izleme
├── Resources/              # XAML kaynakları
│   ├── Animations.xaml
│   ├── Colors.xaml
│   ├── Colors.Light.xaml
│   ├── Converters.xaml
│   └── Styles.xaml
├── Services/
│   └── TweakProviders/     # 11 TweakProvider
├── Utilities/
│   ├── AppInfo.cs
│   ├── ElevatedHostLocator.cs
│   ├── ProcessElevation.cs
│   └── StringPool.cs
├── ViewModels/             # 17 ViewModel
│   ├── TweaksViewModel.cs  # 4505 satır (ana VM)
│   ├── MonitorViewModel.cs
│   ├── DashboardViewModel.cs
│   └── ...
└── Views/                  # XAML görünümleri
    ├── TweaksView.xaml
    ├── MonitorView.xaml
    ├── DashboardView.xaml
    └── ...
```

---

## 🔧 TweakProvider Analizi

### Provider Listesi

| Provider | Kategori | Açıklama |
|----------|----------|----------|
| `AudioTweakProvider` | Audio | Ses ayarları optimizasyonu |
| `MiscTweakProvider` | Misc | Çeşitli sistem tweakleri |
| `NetworkTweakProvider` | Network | Ağ performans optimizasyonu |
| `PerformanceTweakProvider` | Performance | Genel performans iyileştirmeleri |
| `PeripheralTweakProvider` | Peripheral | Fare, klavye, giriş aygıtları |
| `PowerTweakProvider` | Power | Güç planı optimizasyonu |
| `PrivacyTweakProvider` | Privacy | Telemetri ve gizlilik ayarları |
| `SecurityTweakProvider` | Security | Güvenlik ayarları |
| `SystemTweakProvider` | System | Windows sistem ayarları |
| `VisibilityTweakProvider` | Visibility | UI/UX görünürlük ayarları |
| `LegacyTweakProvider` | Legacy | Refactor sonrası eksik tweakleri geçici olarak geri yükler |
| `BaseTweakProvider` | (Abstract) | Ortak provider mantığı |

### Provider Mimarisi
```csharp
public interface ITweakProvider
{
    string CategoryName { get; }
    IEnumerable<ITweak> CreateTweaks(
        TweakExecutionPipeline pipeline,
        TweakContext context,
        bool isElevated);
}
```

---

## 💡 Tweak Tipleri

### Mevcut Tweak İmplementasyonları

| Tip | Dosya Sayısı | Açıklama |
|-----|--------------|----------|
| RegistryValueTweak | Çoklu | Tekil registry değeri |
| RegistryValueBatchTweak | Çoklu | Toplu registry değerleri |
| ServiceStartModeTweak | Çoklu | Servis başlatma modu |
| ServiceStartModeBatchTweak | Çoklu | Toplu servis ayarları |
| ScheduledTaskBatchTweak | 1 | Zamanlanmış görev yönetimi |
| CompositeTweak | 1 | Birleşik tweak |
| FileRenameTweak | 1 | Dosya yeniden adlandırma |
| CommandTweak | 1 | Komut çalıştırma |

---

## 🖥️ Özellik Matrisi

### Uygulama Özellikleri

| Özellik | Durum | Açıklama |
|---------|-------|----------|
| ✅ Gerçek Zamanlı İzleme | Çalışıyor | CPU, RAM, Disk, Ağ metrikleri |
| ✅ Tweak Yönetimi | Çalışıyor | Detect → Apply → Verify → Rollback |
| ✅ Kategori Navigasyonu | Çalışıyor | 10+ kategori |
| ✅ Arama & Filtreleme | Çalışıyor | Risk seviyesi, durum filtresi |
| ✅ Toplu İşlemler | Çalışıyor | Çoklu seçim, toplu uygula/geri al |
| ✅ Profil Yönetimi | Çalışıyor | Dışa/içe aktarma, hazır presetler |
| ✅ Plugin Sistemi | Çalışıyor | Dinamik plugin yükleme |
| ✅ Yükseltilmiş İşlemler | Çalışıyor | ElevatedHost ile UAC |
| ✅ Kalıcı Geri Alma | Çalışıyor | JSON tabanlı durum |
| ⚠️ Ağ/Disk İzleme | Kısmen | Bazı ortamlarda boş kalabiliyor |
| ❌ Kurtarma Diyaloğu | Yok | Uygulama başlangıcında |

---

## 📂 Dokümantasyon Haritası

```
Project Root/
├── README.md                   # Proje genel bakış
├── ARCHITECTURE.md             # Sistem mimarisi
├── DEVELOPMENT_STATUS.md       # Bilinen sorunlar, düzeltmeler
├── CODEX_PLAN.md               # Geliştirme yol haritası
├── ECOSYSTEM_FOUNDATIONS.md    # Gelecek özellikler
├── HANDOFF_REPORT.md           # Son değişiklikler
├── CONTRIBUTING.md             # Katkıda bulunma rehberi
├── AGENTS.md                   # AI ajanları için kurallar
├── CLAUDE.md                   # Claude operasyonel notlar
├── Docs/                       # Tweak belgeleri (73 dosya)
│   ├── affinities/
│   ├── cleanup/
│   ├── misc/
│   ├── network/
│   ├── peripheral/
│   ├── policies/
│   ├── power/
│   ├── privacy/
│   ├── security/
│   ├── system/
│   └── visibility/
└── scripts/                    # Yardımcı scriptler (3 dosya)
```

---

## 🔒 Güvenlik Modeli

### Yetkilendirme Katmanları

```
┌─────────────────────────────────────┐
│         Kullanıcı (Normal)          │
│   CurrentUser registry tweakleri    │
└─────────────────▼───────────────────┘
                  │
        UAC Prompt (Gerekirse)
                  │
┌─────────────────▼───────────────────┐
│       ElevatedHost (Admin)          │
│   LocalMachine registry tweakleri   │
│   Servis yönetimi                   │
│   Sistem dosyaları                  │
└─────────────────────────────────────┘
```

### İletişim Protokolü
- **Named Pipes:** Ana uygulama ↔ ElevatedHost
- **JSON:** İstek/yanıt formatı
- **Timeout:** 30 saniye bağlantı zaman aşımı
- **Platform Kontrolü:** Windows dışında anında hata

---

## 📈 Performans Metrikleri

### Zamanlama Parametreleri

| Parametre | Değer | Açıklama |
|-----------|-------|----------|
| Metrik Güncelleme | 1 saniye | DispatcherTimer aralığı |
| Tweak Detect Timeout | 5 saniye | Kategori genişletme |
| Tweak Apply Timeout | 30 saniye | İşlem zaman aşımı |
| Heartbeat | 30 saniye | Uzak yönetim (gelecek) |
| Telemetri Batch | 100 olay | Toplu gönderim (gelecek) |

### Bellek Yönetimi
- ObservableCollection boyutu: 60 nokta (geçmiş grafikler)
- Kayan pencere deseni (eski kaldır, yeni ekle)
- PerformanceCounter dispose pattern
- LibreHardwareMonitor temizleme

---

## 🎯 Kritik Dosya Referansları

### En Önemli Dosyalar

| Dosya | Boyut | Önem |
|-------|-------|------|
| `TweaksViewModel.cs` | 4505 satır | Ana tweak yönetimi |
| `TweakExecutionPipeline.cs` | 357 satır | İş akışı orkestrasyon |
| `MonitorViewModel.cs` | ~800 satır | Donanım izleme |
| `ElevatedHostClient.cs` | ~500 satır | UAC iletişimi |
| `RollbackStateStore.cs` | 11KB | Kalıcı geri alma |
| `BaseTweakProvider.cs` | ~150 satır | Provider temeli |

---

## 🔗 Bağımlılıklar

### NuGet Paketleri

| Paket | Kullanım |
|-------|----------|
| LibreHardwareMonitor | CPU/GPU sıcaklık okuma |
| System.Management | WMI sorguları |
| System.Text.Json | JSON serileştirme |
| (Opsiyonel) NLua | LUA script desteği |
| (Opsiyonel) pythonnet | Python script desteği |

### .NET Gereksinimleri
- **.NET 8.0 SDK**
- **Windows 10/11** (WPF gereksinimi)
- **x64 platformu**

---

## 📝 Sonuç

Windows Optimizer, olgun ve iyi yapılandırılmış bir .NET 8 WPF uygulamasıdır. Temiz mimari, kapsamlı tweak sistemi ve güvenlik odaklı tasarımı ile güçlü bir temel sunar. Mevcut odak noktaları:

1. Windows 10/11 native testleri
2. Ağ/Disk izleme iyileştirmeleri
3. Kurtarma diyaloğu implementasyonu
4. Bellek sızıntısı testleri

Detaylı uygulama planları için `CODEX_TODO.md` ve `CODEX_IMPROVEMENTS.md` dosyalarına bakınız.
