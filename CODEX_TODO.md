# 📋 Windows Optimizer - Yapılacaklar Listesi (TODO)

**Son Güncelleme:** 30 Aralık 2025  
**Versiyon:** 1.0.0

---

## 🎯 Öncelik Sistemi

| Öncelik | Sembol | Açıklama |
|---------|--------|----------|
| KRİTİK | 🔴 | Release öncesi mutlaka yapılmalı |
| YÜKSEK | 🟠 | UX için önemli |
| ORTA | 🟡 | Stabilite iyileştirmesi |
| DÜŞÜK | 🟢 | Gelecek versiyon için |
| GELİŞTİRME | 🔵 | Yeni özellik |

---

## 🔴 KRİTİK - Release Öncesi Gerekli

### 1. Windows 10/11 Native Test Doğrulaması
**Dosyalar:** N/A (manuel test)  
**Çaba:** Yüksek  
**Durum:** ⏳ Beklemede

**Test Kontrol Listesi:**
- [ ] Windows 10 22H2 temiz kurulum testi
- [ ] Windows 11 testi
- [ ] Non-admin kullanıcı olarak uygulama çalıştırma
- [ ] Tüm tweak kategorilerini genişletme
- [ ] CurrentUser tweak uygulama (UAC yok)
- [ ] LocalMachine tweak uygulama (UAC prompt)
- [ ] Rollback çalışıyor mu doğrulama
- [ ] Monitor sayfa metrik doğruluğu
- [ ] 1 saat Monitor açık bırakma (bellek testi)
- [ ] CSV log dışa aktarma
- [ ] Profil import/export
- [ ] Light/Dark theme (Dashboard, Tweaks, Monitor, MainWindow)

---

### 2. ElevatedHost Paketleme Doğrulaması
**Dosyalar:** 
- `WindowsOptimizer.App/WindowsOptimizer.App.csproj`
- Build/publish scripts

**Çaba:** Orta  
**Durum:** ✅ Uygulandı (doğrulama gerekli)

**Yapılanlar:**
- [x] Build sonrası kopyalama (CopyElevatedHostAfterBuild)
- [x] Publish sonrası kopyalama (CopyElevatedHostAfterPublish)

**Kalan:**
- [ ] CI check: publish çıktısında `ElevatedHost/WindowsOptimizer.ElevatedHost.exe` var mı?
- [ ] Windows 10/11 üzerinde publish layout doğrulaması

---

### 3. Kurtarma Diyaloğu Implementasyonu
**Dosyalar:**
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.Infrastructure/RollbackStateStore.cs`

**Çaba:** Orta  
**Durum:** ✅ Uygulandı (banner + aksiyonlar)

**Mevcut Durum:**
- ✅ `RollbackStateStore.GetPendingRollbacksAsync()` hazır
- ✅ Uygulama başlangıcında pending rollback kontrolü var
- ✅ MainWindow üst banner ile Recover/Dismiss aksiyonları var

**Kalan (opsiyonel):**
- [ ] Modal dialog + detay liste (hangi tweakler etkilenmiş)
- [ ] Kullanıcıya tek-tek geri alma seçeneği

---

## 🟠 YÜKSEK - UX İyileştirmeleri

### 4. Ağ İzleme Boş Liste Düzeltmesi
**Dosyalar:**
- `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs`

**Çaba:** Orta  
**Durum:** 🧪 Test gerekli

**Mevcut Durumlar:**
- ✅ Delta-based throughput fallback implementasyonu var
- ⚠️ Windows 10/11 native ortamda test gerekli

**Yapılacaklar:**
- [ ] Windows 10/11'de adapter listesi kontrolü
- [ ] Performance counter instance name eşleştirme
- [ ] Fallback logic test

---

### 5. Disk İzleme Boş Liste Düzeltmesi
**Dosyalar:**
- `WindowsOptimizer.Infrastructure/Metrics/DiskMonitor.cs`

**Çaba:** Orta  
**Durum:** 🧪 Test gerekli

**Mevcut Durumlar:**
- ✅ LogicalDisk counter implementasyonu var
- ⚠️ Windows 10/11 native ortamda test gerekli

**Yapılacaklar:**
- [ ] Windows 10/11'de disk listesi kontrolü
- [ ] I/O rate doğruluğu kontrolü
- [ ] WMI fallback (gerekirse)

---

### 6. CPU Sıcaklık Tooltip Açıklaması
**Dosyalar:**
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.App/Views/MonitorView.xaml`

**Çaba:** Düşük  
**Durum:** ✅ Uygulandı

**Yapılanlar:**
- [x] CPU Temp kartında tooltip açıklaması eklendi (N/A durumunu açıklar)

---

### 6.1 Splash + Tema Flicker / Scan Donması
**Dosyalar:**
- `WindowsOptimizer.App/App.xaml.cs`
- `WindowsOptimizer.App/StartupWindow.xaml`
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`

**Çaba:** Orta  
**Durum:** 🧪 Test gerekli

**Yapılanlar:**
- [x] Tema splash açılmadan önce uygulanıyor
- [x] Splash render edilip sonra scan başlıyor
- [x] Detect işlemleri UI thread’i daha az bloklar
- [x] Splash üzerinde tarama ilerlemesi gösteriliyor (X/Y)

**Yapılacaklar:**
- [ ] Windows 10/11’de flicker kalmadığını doğrula
- [ ] Scan sırasında UI freeze olmadığını doğrula

---

### 6.2 Tweak Kartı Kısa Özet (Current → Target + Area)
**Dosyalar:**
- `WindowsOptimizer.App/Views/TweaksView.xaml`
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

**Çaba:** Orta  
**Durum:** 🧪 Uygulandı (test gerekli)

**Yapılacaklar:**
- [x] Kart kapalıyken `Current → Target` satırı göster
- [x] Etki alanı (Registry/Service/Task vb.) kısa etiketle
- [ ] Durum ikonları netleştir (Applied/Mixed/Error)

---

### 6.3 Tweak Dokümantasyon Derinliği
**Dosyalar:**
- `Docs/tweaks/*.md`
- `Docs/tweaks/tweak-catalog.csv`
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`

**Çaba:** Orta  
**Durum:** 🧪 Kısmen uygulandı (test gerekli)

**Mevcut Durumlar:**
- ✅ UI tarafında `Source file` linkleri CSV’den okunuyor
- ✅ Catalog artık `Description` + `Risk` içeriyor (CSV/MD/HTML)

**Yapılacaklar:**
- [x] Per‑tweak kısa açıklama (Changes / Risk / Source) katalogda mevcut
- [ ] Docs içinde tweak başlıklarına anchor ekle (kategori docs)

---

### 6.4 Monitor UI Modernizasyonu
**Dosyalar:**
- `WindowsOptimizer.App/Views/MonitorView.xaml`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

**Çaba:** Orta  
**Durum:** ⏳ Beklemede

**Yapılacaklar:**
- [ ] Daha kompakt kart düzeni + okunabilir grafikler
- [ ] Top 10 network process + disk IO per process
- [ ] Save butonu ve toolbar’ı modernize et

---

## 🟡 ORTA - Stabilite İyileştirmeleri

### 7. Bellek Sızıntısı Testi
**Dosyalar:**
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.Infrastructure/Metrics/*.cs`

**Çaba:** Orta  
**Durum:** ⏳ Beklemede

**Yapılacaklar:**
- [ ] VS Diagnostic Tools ile profiling
- [ ] 1 saat stress testi
- [ ] PerformanceCounter dispose kontrolü
- [ ] LibreHardwareMonitor temizlik kontrolü
- [ ] ObservableCollection bounded kontrolü

---

### 8. Log Rotation Implementasyonu
**Dosyalar:**
- `WindowsOptimizer.Infrastructure/FileAppLogger.cs`
- Loglama yapan diğer dosyalar

**Çaba:** Düşük  
**Durum:** ✅ Uygulandı (test gerekli)

**Yapılanlar:**
- [x] Maksimum log dosya boyutu (10MB)
- [x] Eski logları arşivle/sil (son 5 dosya tutulur)

**Kalan:**
- [ ] Konfigürasyon seçeneği (opsiyonel)

---

### 9. Yapılandırılmış Loglama (Structured Logging)
**Dosyalar:**
- `WindowsOptimizer.Infrastructure/IAppLogger.cs`
- `WindowsOptimizer.Infrastructure/FileAppLogger.cs`

**Çaba:** Orta  
**Durum:** ⏳ Beklemede

**Yapılacaklar:**
- [ ] JSON format desteği
- [ ] Log level (Info, Debug, Warning, Error)
- [ ] Timestamp + EventId
- [ ] Konfigürasyondan log level seçimi

---

### 10. ElevatedHost Retry Logic
**Dosyalar:**
- `WindowsOptimizer.Infrastructure/Elevation/ElevatedHostClient.cs`

**Çaba:** Orta  
**Durum:** ✅ Kısmen tamamlandı (status göstergesi eksik)

**Yapılanlar:**
- [x] Exponential backoff retry
- [x] Maksimum retry sayısı (varsayılan 3)
- [x] Kullanıcı dostu hata mesajları

**Kalan:**
- [ ] Bağlantı durumu göstergesi (UI)

---

### 11. Legacy TweakProvider Migration (Parite Temizliği)
**Dosyalar:**
- `WindowsOptimizer.App/Services/TweakProviders/LegacyTweakProvider.cs`
- `WindowsOptimizer.App/Services/TweakProviders/*.cs`
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`

**Çaba:** Orta  
**Durum:** ⏳ Beklemede

**Yapılacaklar:**
- [ ] Legacy tweak listesi ile provider paritesini karşılaştır
- [ ] Eksik tweakleri kategori provider'lara taşı
- [ ] Duplicate ID temizliği
- [ ] Parite sağlandıktan sonra `LegacyTweakProvider` kaldır

---

## 🟢 DÜŞÜK - Gelecek Versiyon

### 11. Platform Tespiti Başlangıç Uyarısı
**Dosyalar:**
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.App/App.xaml.cs`

**Çaba:** Düşük  
**Durum:** ⏳ Beklemede

**Yapılacaklar:**
- [ ] WSL2/Linux başlangıç tespiti
- [ ] "Sınırlı fonksiyonellik" uyarı dialogi
- [ ] Native Windows önerisi

---

### 12. Kritik Path Unit Testleri
**Dosyalar:**
- `WindowsOptimizer.Tests/`

**Çaba:** Orta  
**Durum:** ⏳ Beklemede

**Öncelikli Test Alanları:**
- [ ] TweakExecutionPipeline timeout logic
- [ ] Platform detection
- [ ] Registry operations (mocked)
- [ ] RollbackStateStore serialization
- [ ] Provider instantiation

---

### 13. Telemetri Opt-In/Out UI
**Dosyalar:**
- `WindowsOptimizer.App/Views/SettingsView.xaml`
- `WindowsOptimizer.App/ViewModels/SettingsViewModel.cs`

**Çaba:** Orta  
**Durum:** ⏳ Stub mevcut, UI yok

**Yapılacaklar:**
- [ ] Settings sayfasında toggle switch
- [ ] Açıklama metni
- [ ] AppSettings'e kayıt

---

### 14. Crash Reporting (Opsiyonel)
**Dosyalar:**
- Yeni dosya gerekli

**Çaba:** Orta  
**Durum:** ⏳ Beklemede

**Yapılacaklar:**
- [ ] Global exception handler
- [ ] Anonymous crash data
- [ ] Opt-in only

---

## 🔵 GELİŞTİRME - Yeni Özellikler

### 15. WiX MSI Installer
**Dosyalar:**
- Yeni proje: WindowsOptimizer.Installer

**Çaba:** Yüksek  
**Durum:** ⏳ Başlanmadı

**Yapılacaklar:**
- [ ] WiX Toolset kurulumu
- [ ] Product.wxs tanımlama
- [ ] ElevatedHost entegrasyonu
- [ ] Start menu/desktop shortcut
- [ ] Uninstaller

---

### 16. Auto-Update Mekanizması
**Dosyalar:**
- Yeni servis/modül gerekli

**Çaba:** Yüksek  
**Durum:** ⏳ Başlanmadı

**Yapılacaklar:**
- [ ] Version check endpoint
- [ ] Download manager
- [ ] Silent install (restart gerekirse)
- [ ] Update notification

---

### 17. CI/CD Pipeline (GitHub Actions)
**Dosyalar:**
- `.github/workflows/build.yml`

**Çaba:** Orta  
**Durum:** ⏳ Temel mevcut

**Yapılacaklar:**
- [ ] PR build kontrolü
- [ ] Release build + publish
- [ ] ElevatedHost paketleme kontrolü
- [ ] Artifact upload
- [ ] Release notes generation

---

### 18. Çoklu Dil Desteği (i18n)
**Dosyalar:**
- `WindowsOptimizer.App/Resources/`

**Çaba:** Yüksek  
**Durum:** ⏳ Başlanmadı

**Öncelikli Diller:**
- [ ] Türkçe (TR)
- [ ] İngilizce (EN) - mevcut
- [ ] Almanca (DE)
- [ ] Rusça (RU)

**Yapılacaklar:**
- [ ] Resource dictionary yapısı
- [ ] Dil seçim UI
- [ ] Tüm string'leri externalize et

---

### 19. Backend API Servisleri
**Dosyalar:**
- Yeni proje: WindowsOptimizer.API

**Çaba:** Çok Yüksek  
**Durum:** ⏳ Başlanmadı

**Yapılacaklar:**
- [ ] ASP.NET Core Web API
- [ ] PostgreSQL/MySQL database
- [ ] JWT authentication
- [ ] Preset repository endpoints
- [ ] Telemetry aggregation

---

### 20. Plugin Marketplace UI
**Dosyalar:**
- `WindowsOptimizer.App/Views/PluginsView.xaml`
- `WindowsOptimizer.App/ViewModels/PluginsViewModel.cs`

**Çaba:** Yüksek  
**Durum:** ⏳ Stub mevcut

**Yapılacaklar:**
- [ ] Plugin listesi görünümü
- [ ] Yükle/Kaldır butonları
- [ ] Rating gösterimi
- [ ] Backend entegrasyonu

---

### 21. Script Editor
**Dosyalar:**
- Yeni View/ViewModel gerekli

**Çaba:** Yüksek  
**Durum:** ⏳ Stub mevcut (ScriptApi.cs)

**Yapılacaklar:**
- [ ] Syntax highlighting (AvalonEdit?)
- [ ] LUA/Python desteği
- [ ] Script çalıştırma
- [ ] Script kütüphanesi

---

### 22. Audit Log Viewer
**Dosyalar:**
- Yeni View/ViewModel gerekli

**Çaba:** Orta  
**Durum:** ⏳ Stub mevcut (CryptographicLogger)

**Yapılacaklar:**
- [ ] Log listesi görünümü
- [ ] Filtreleme (tarih, tür)
- [ ] Hash chain verification UI
- [ ] Export seçenekleri

---

### 23. Dark/Light Theme Toggle
**Dosyalar:**
- `WindowsOptimizer.App/Resources/Colors.xaml`
- `WindowsOptimizer.App/Resources/Colors.Light.xaml`
- `WindowsOptimizer.App/Services/ThemeManager.cs`

**Çaba:** Orta  
**Durum:** ⏳ Kısmen mevcut

**Yapılacaklar:**
- [ ] Settings'de theme toggle
- [ ] Canlı tema değişimi
- [ ] Sistem temasını takip (opsiyonel)

---

### 24. AppDomain Plugin Isolation
**Dosyalar:**
- `WindowsOptimizer.Infrastructure/Plugins/PluginLoader.cs`

**Çaba:** Yüksek  
**Durum:** ⏳ Başlanmadı

**Yapılacaklar:**
- [ ] AssemblyLoadContext kullanımı
- [ ] Güvenlik sandbox
- [ ] Plugin izolasyonu

---

### 25. Remote Management Dashboard
**Dosyalar:**
- Yeni web projesi gerekli

**Çaba:** Çok Yüksek  
**Durum:** ⏳ Stub mevcut (Remote/ klasörü)

**Yapılacaklar:**
- [ ] WebSocket server (SignalR)
- [ ] Agent registration
- [ ] Fleet policy deployment
- [ ] Web dashboard (React/Blazor)

---

## 📊 Özet Tablo

| Kategori | Toplam | Tamamlandı | Beklemede |
|----------|--------|------------|-----------|
| Kritik | 3 | 0 | 3 |
| Yüksek | 3 | 0 | 3 |
| Orta | 4 | 0 | 4 |
| Düşük | 4 | 0 | 4 |
| Geliştirme | 11 | 0 | 11 |
| **TOPLAM** | **25** | **0** | **25** |

> **NOT:** Phase 8 TweaksViewModel Refactoring %100 tamamlandı. 4500 satır → 1300 satır.

---

## 🎯 Önerilen Sıralama

### Faz 1: Stabilizasyon (1-2 Hafta)
1. Windows 10/11 native test doğrulaması
2. ElevatedHost paketleme doğrulaması
3. Kurtarma diyaloğu implementasyonu

### Faz 2: UX İyileştirmeleri (1 Hafta)
4. Ağ izleme düzeltmesi
5. Disk izleme düzeltmesi
6. CPU sıcaklık tooltip

### Faz 3: Stabilite (1 Hafta)
7. Bellek sızıntısı testi
8. Log rotation
9. ElevatedHost retry logic

### Faz 4: Kalite (2 Hafta)
10. Unit testler
11. CI/CD pipeline
12. Structured logging

### Faz 5: Yeni Özellikler (4+ Hafta)
13. WiX MSI installer
14. Auto-update
15. i18n desteği
16. Backend API

---

## 📝 Notlar

> **ÖNEMLİ:** Tüm geliştirmeler `AGENTS.md` ve `CLAUDE.md` dosyalarındaki güvenlik kurallarına uymalıdır:
> - SAFE tweakler GERİ ALINABILIR olmalı
> - Varsayılan Preview/DryRun
> - "Defender/Firewall/SmartScreen devre dışı" SAFE altında OLAMAZ
> - Admin işlemleri ElevatedHost üzerinden
> - Tüm aksiyonlar loglanmalı
