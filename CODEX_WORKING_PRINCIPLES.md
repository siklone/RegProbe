# ⚙️ Windows Optimizer - Çalışma Prensipleri

**Son Güncelleme:** 30 Aralık 2025  
**Versiyon:** 1.0.0

---

> Not (2025-12-30): `LegacyTweakProvider` geçici bir geri uyumluluk katmanı. Yeni tweakler doğrudan kategori provider'lara eklenmeli.

## 📋 İçindekiler

1. [Güvenlik Kuralları](#güvenlik-kuralları)
2. [Tweak Yaşam Döngüsü](#tweak-yaşam-döngüsü)
3. [Yükseltme Modeli](#yükseltme-modeli)
4. [Risk Seviyeleri](#risk-seviyeleri)
5. [Kod Standartları](#kod-standartları)
6. [MVVM Pattern](#mvvm-pattern)
7. [WPF Animasyon Kuralları](#wpf-animasyon-kuralları)
8. [Logging Stratejisi](#logging-stratejisi)
9. [Hata Yönetimi](#hata-yönetimi)
10. [Test Gereksinimleri](#test-gereksinimleri)

---

## 🛡️ Güvenlik Kuralları

### Temel Güvenlik İlkeleri

Bu kurallar **DEĞİŞMEZ** ve tüm geliştirmeler bunlara uymalıdır:

> [!CAUTION]
> Bu kuralları ihlal eden değişiklikler **REDDEDİLMELİDİR**.

#### Kural 1: SAFE Tweakler GERİ ALINABİLİR Olmalı
```
✅ DOĞRU: Detect → Apply → Verify → Rollback zinciri
❌ YANLIŞ: Sadece Apply, rollback yok
```

#### Kural 2: Varsayılan Preview/DryRun
```
✅ DOĞRU: Kullanıcı "Apply" tıklamadan sistem değişikliği yok
❌ YANLIŞ: Otomatik uygulama
```

#### Kural 3: Tehlikeli Tweakler SAFE Olamaz
```
❌ ASLA SAFE ALTINDA:
- Windows Defender devre dışı bırakma
- Windows Firewall devre dışı bırakma
- SmartScreen devre dışı bırakma
- UAC devre dışı bırakma
- Güvenlik güncellemelerini engelleme

✅ Bu tweakler YALNIZCA "Risky" kategorisinde olabilir
```

#### Kural 4: Admin İşlemleri ElevatedHost Üzerinden
```
✅ DOĞRU: LocalMachine registry → ElevatedHost üzerinden
❌ YANLIŞ: Ana uygulamayı her zaman admin olarak çalıştırma
```

#### Kural 5: Tüm Aksiyonlar Loglanmalı
```
✅ DOĞRU: Her Apply/Rollback kaydedilir, dışa aktarılabilir
❌ YANLIŞ: Sessiz değişiklikler
```

---

## 🔄 Tweak Yaşam Döngüsü

### 4 Aşamalı Pipeline

```
┌─────────────────────────────────────────────────────────────────────┐
│                        TWEAK PIPELINE                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐     │
│  │  DETECT  │───▶│  APPLY   │───▶│  VERIFY  │───▶│ ROLLBACK │     │
│  └──────────┘    └──────────┘    └──────────┘    └──────────┘     │
│       │               │               │               │            │
│       ▼               ▼               ▼               ▼            │
│  Mevcut durumu   Değişikliği    Değişikliği    Başarısızlıkta     │
│  tespit et       uygula        doğrula        geri al             │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Her Aşamanın Görevi

#### DetectAsync
```csharp
/// <summary>
/// Sistem üzerindeki mevcut durumu tespit eder.
/// </summary>
/// <returns>
/// - Status: Detected
/// - Ek bilgi: Current value, isAlreadyApplied
/// </returns>
public async Task<TweakResult> DetectAsync(CancellationToken ct)
{
    // 1. Registry/servis/dosya durumunu oku
    // 2. Mevcut değeri kaydet (rollback için)
    // 3. Zaten uygulanmış mı kontrol et
    return new TweakResult
    {
        Status = TweakStatus.Detected,
        CurrentValue = currentValue,
        Message = isApplied ? "Already applied" : "Not applied"
    };
}
```

#### ApplyAsync
```csharp
/// <summary>
/// Değişikliği sisteme uygular.
/// </summary>
/// <remarks>
/// ÖNEMLİ: Apply çağrılmadan önce MUTLAKA Detect çağrılmalı!
/// </remarks>
public async Task<TweakResult> ApplyAsync(CancellationToken ct)
{
    // 1. Detect edilmiş mi kontrol et
    if (!_hasDetected)
        throw new InvalidOperationException("Must call DetectAsync first");
    
    // 2. Orijinal değeri sakla (rollback için)
    _originalValue = _detectedValue;
    
    // 3. Yeni değeri uygula
    await _registry.SetValue(...);
    
    return new TweakResult { Status = TweakStatus.Applied };
}
```

#### VerifyAsync
```csharp
/// <summary>
/// Uygulanan değişikliğin başarılı olduğunu doğrular.
/// </summary>
public async Task<TweakResult> VerifyAsync(CancellationToken ct)
{
    // 1. Değeri tekrar oku
    var actualValue = await _registry.GetValue(...);
    
    // 2. Beklenen ile karşılaştır
    if (actualValue == _desiredValue)
        return new TweakResult { Status = TweakStatus.Verified };
    
    return new TweakResult 
    { 
        Status = TweakStatus.Failed,
        Message = $"Expected {_desiredValue} but found {actualValue}"
    };
}
```

#### RollbackAsync
```csharp
/// <summary>
/// Değişikliği geri alır, orijinal duruma döndürür.
/// </summary>
public async Task<TweakResult> RollbackAsync(CancellationToken ct)
{
    // 1. Orijinal değer mevcut mu kontrol et
    if (_originalValue == null)
        return new TweakResult 
        { 
            Status = TweakStatus.Failed,
            Message = "No original value to restore"
        };
    
    // 2. Orijinal değeri geri yükle
    await _registry.SetValue(..., _originalValue);
    
    return new TweakResult { Status = TweakStatus.RolledBack };
}
```

---

## 🔐 Yükseltme Modeli

### Dual-Process Mimarisi

```
┌──────────────────────────────────────────────────────────────────┐
│                        KULLANICI OTURUMU                         │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              WindowsOptimizer.App (Normal User)            │ │
│  │                                                            │ │
│  │  • UI rendering                                            │ │
│  │  • ViewModel logic                                         │ │
│  │  • CurrentUser registry (doğrudan erişim)                  │ │
│  │  • Kullanıcı ayarları                                      │ │
│  └───────────────────────┬────────────────────────────────────┘ │
│                          │                                       │
│                   Named Pipe                                     │
│                   (JSON protokolü)                               │
│                          │                                       │
│  ┌───────────────────────▼────────────────────────────────────┐ │
│  │         WindowsOptimizer.ElevatedHost (Administrator)      │ │
│  │                                                            │ │
│  │  • UAC ile yükseltilmiş                                    │ │
│  │  • LocalMachine registry erişimi                           │ │
│  │  • Servis yönetimi                                         │ │
│  │  • Sistem dosyaları                                        │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### İletişim Protokolü

```csharp
// İstek formatı
{
    "RequestId": "guid",
    "Operation": "SetRegistryValue",
    "Parameters": {
        "Hive": "LocalMachine",
        "Key": "SOFTWARE\\...",
        "ValueName": "...",
        "Value": 1,
        "ValueKind": "DWord"
    }
}

// Yanıt formatı
{
    "RequestId": "guid",
    "Success": true,
    "Result": { ... },
    "Error": null
}
```

### Bağlantı Yaşam Döngüsü

```
1. Kullanıcı LocalMachine tweak'i uygular
2. App, ElevatedHost path'ini bulur (ElevatedHostLocator)
3. ElevatedHost.exe başlatılır (UAC prompt gösterilir)
4. Named pipe bağlantısı kurulur (30 saniye timeout)
5. İşlemler JSON üzerinden iletilir
6. App kapandığında ElevatedHost otomatik sonlanır
```

---

## ⚠️ Risk Seviyeleri

### 3 Seviyeli Risk Sistemi

| Seviye | Renk | Açıklama | Örnekler |
|--------|------|----------|----------|
| **Safe** | 🟢 | Yan etkisi yok, tüm kullanıcılar için önerilir | UI ayarları, tema değişiklikleri |
| **Advanced** | 🟡 | Fonksiyonelliği etkileyebilir, anlayarak uygula | Telemetri ayarları, servis optimizasyonları |
| **Risky** | 🔴 | Yalnızca ileri düzey kullanıcılar için, sorun çıkarabilir | Güvenlik değişiklikleri, sistem kritik ayarlar |

### Risk Seviyesi Belirleme Kriterleri

```csharp
// Risk seviyesi belirleme kılavuzu
public TweakRiskLevel DetermineRiskLevel(ITweak tweak)
{
    // SAFE kriterleri:
    // - Yalnızca görsel/UI değişikliği
    // - CurrentUser registry
    // - Kolayca geri alınabilir
    // - Hiçbir işlevselliği etkilemez
    
    // ADVANCED kriterleri:
    // - Bazı özellikleri devre dışı bırakır
    // - Performans etkisi olabilir
    // - Ağ veya disk kullanımını etkiler
    // - Bazı uygulamaları etkileyebilir
    
    // RISKY kriterleri:
    // - Güvenlik ayarlarını değiştirir
    // - Sistem kararlılığını etkileyebilir
    // - Windows Update'i etkiler
    // - Kurtarma zor olabilir
}
```

---

## 📐 Kod Standartları

### Genel Kurallar

```csharp
// 1. Nullable reference types zorunlu
#nullable enable

// 2. Dosya başı namespace
namespace WindowsOptimizer.Engine.Tweaks;

// 3. Primary constructor (C# 12)
public class RegistryValueTweak(IRegistryAccessor registry, string id, ...)

// 4. Expression-bodied members
public string Id => _id;

// 5. Async suffix
public async Task<TweakResult> DetectAsync(CancellationToken ct)

// 6. CancellationToken son parametre
public async Task DoSomethingAsync(string param, CancellationToken ct = default)
```

### Adlandırma Kuralları

```csharp
// Sınıflar: PascalCase
public class RegistryValueTweak

// Arayüzler: I prefix
public interface ITweak

// Metodlar: PascalCase
public void ExecuteTweak()

// Properties: PascalCase
public string Name { get; }

// Private fieldlar: _camelCase
private readonly IRegistryAccessor _registry;

// Parametreler: camelCase
public void SetValue(string keyPath, object value)

// Sabitler: PascalCase veya UPPER_CASE
public const int MaxRetryCount = 3;
private const string DEFAULT_VALUE = "default";
```

### Dosya Organizasyonu

```
WindowsOptimizer.Engine/
├── Tweaks/
│   ├── Base/
│   │   ├── ITweak.cs
│   │   └── BaseTweak.cs
│   ├── Registry/
│   │   ├── RegistryValueTweak.cs
│   │   └── RegistryValueBatchTweak.cs
│   ├── Service/
│   │   └── ServiceStartModeTweak.cs
│   └── Composite/
│       └── CompositeTweak.cs
└── Pipeline/
    └── TweakExecutionPipeline.cs
```

---

## 🎯 MVVM Pattern

### Temel Bileşenler

```
┌─────────────────────────────────────────────────────────────────┐
│                           VIEW (XAML)                           │
│                                                                 │
│  • UI tanımları                                                │
│  • Data binding                                                │
│  • Commands                                                    │
│  • Minimal code-behind                                         │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                        DataContext
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                       VIEWMODEL (C#)                            │
│                                                                 │
│  • INotifyPropertyChanged                                      │
│  • ObservableCollection<T>                                     │
│  • ICommand implementations                                    │
│  • UI logic (no business logic)                                │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                          Dependency
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                         MODEL (C#)                              │
│                                                                 │
│  • Domain entities                                             │
│  • Services                                                    │
│  • Business logic                                              │
└─────────────────────────────────────────────────────────────────┘
```

### Örnek ViewModel

```csharp
public class TweakItemViewModel : ViewModelBase
{
    private bool _isSelected;
    private TweakStatus _status;
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public TweakStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
                OnPropertyChanged(nameof(StatusDisplayText));
        }
    }
    
    public string StatusDisplayText => Status switch
    {
        TweakStatus.Applied => "✓ Applied",
        TweakStatus.NotApplied => "○ Not Applied",
        TweakStatus.Unknown => "? Unknown",
        _ => Status.ToString()
    };
    
    public ICommand ApplyCommand { get; }
    public ICommand RollbackCommand { get; }
    
    public TweakItemViewModel(ITweak tweak)
    {
        ApplyCommand = new RelayCommand(
            async () => await ApplyAsync(),
            () => CanApply);
        
        RollbackCommand = new RelayCommand(
            async () => await RollbackAsync(),
            () => CanRollback);
    }
}
```

---

## 🎨 WPF Animasyon Kuralları

> [!WARNING]
> Bu kuralları ihlal etmek **uygulama çökmesine** neden olabilir!

### Yapılmaması Gerekenler

```xml
<!-- ❌ YANLIŞ: Template içinde Freezable animasyonu -->
<ControlTemplate>
    <Border>
        <Border.Effect>
            <DropShadowEffect x:Name="shadow" BlurRadius="5"/>
        </Border.Effect>
    </Border>
    <ControlTemplate.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <!-- BU ÇÖKECEK! -->
            <Trigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard>
                        <DoubleAnimation 
                            Storyboard.TargetName="shadow"
                            Storyboard.TargetProperty="BlurRadius"
                            To="15"/>
                    </Storyboard>
                </BeginStoryboard>
            </Trigger.EnterActions>
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>
```

### Güvenli Alternatifler

```xml
<!-- ✅ DOĞRU: Named transform animasyonu -->
<Border RenderTransformOrigin="0.5,0.5">
    <Border.RenderTransform>
        <TransformGroup>
            <ScaleTransform x:Name="scaleTransform" ScaleX="1" ScaleY="1"/>
            <TranslateTransform x:Name="translateTransform" X="0" Y="0"/>
        </TransformGroup>
    </Border.RenderTransform>
</Border>

<!-- Trigger -->
<Trigger Property="IsMouseOver" Value="True">
    <Trigger.EnterActions>
        <BeginStoryboard>
            <Storyboard>
                <DoubleAnimation 
                    Storyboard.TargetName="scaleTransform"
                    Storyboard.TargetProperty="ScaleX"
                    To="1.02" Duration="0:0:0.2"/>
            </Storyboard>
        </BeginStoryboard>
    </Trigger.EnterActions>
</Trigger>
```

```xml
<!-- ✅ DOĞRU: Overlay opacity animasyonu -->
<Grid>
    <Border x:Name="hoverOverlay" Background="#10FFFFFF" Opacity="0"/>
    <!-- Ana içerik -->
</Grid>

<Trigger Property="IsMouseOver" Value="True">
    <Trigger.EnterActions>
        <BeginStoryboard>
            <Storyboard>
                <DoubleAnimation 
                    Storyboard.TargetName="hoverOverlay"
                    Storyboard.TargetProperty="Opacity"
                    To="1"/>
            </Storyboard>
        </BeginStoryboard>
    </Trigger.EnterActions>
</Trigger>
```

---

## 📝 Logging Stratejisi

### Log Seviyeleri

```csharp
public enum LogLevel
{
    Debug,      // Geliştirme detayları
    Info,       // Normal operasyonlar
    Warning,    // Beklenmeyen ama hatalı olmayan durumlar
    Error       // Hatalar ve istisnalar
}
```

### Log Formatı

```
2025-12-30T10:15:30.123 [INFO] [TweaksViewModel] Category expanded: Privacy
2025-12-30T10:15:30.456 [DEBUG] [TweakExecutionPipeline] Starting Detect for privacy.disable-telemetry
2025-12-30T10:15:31.789 [ERROR] [ElevatedHostClient] Connection failed: Timeout
```

### Log Konumları

| Log Tipi | Konum | Format |
|----------|-------|--------|
| Debug log | `%TEMP%\WindowsOptimizer_Debug.log` | Text |
| Tweak log | `%AppData%\WindowsOptimizerSuite\tweak-log.csv` | CSV |
| Rollback state | `%AppData%\WindowsOptimizerSuite\rollback-state.json` | JSON |

### Logging Best Practices

```csharp
// ✅ DOĞRU: Anlamlı mesaj, context bilgisi
_logger.Info($"[{nameof(TweaksViewModel)}] Applying tweak: {tweak.Id}");

// ✅ DOĞRU: Exception ile birlikte stack trace
_logger.Error($"[{nameof(TweaksViewModel)}] Failed to apply tweak: {ex.Message}", ex);

// ❌ YANLIŞ: Çok genel mesaj
_logger.Info("Error occurred");

// ❌ YANLIŞ: Hassas bilgi
_logger.Debug($"Registry value: {sensitiveValue}");
```

---

## 🚨 Hata Yönetimi

### Genel Hata Stratejisi

```csharp
// 1. Her async operasyon try-catch içinde
public async Task ApplyAsync()
{
    try
    {
        await _pipeline.ExecuteAsync(...);
    }
    catch (OperationCanceledException)
    {
        // Kullanıcı iptal etti - normal
        Status = TweakStatus.Cancelled;
    }
    catch (TimeoutException ex)
    {
        // Zaman aşımı - kullanıcıya bilgi ver
        _logger.Warning($"Operation timed out: {ex.Message}");
        Status = TweakStatus.Failed;
        ErrorMessage = "İşlem zaman aşımına uğradı";
    }
    catch (Exception ex)
    {
        // Beklenmeyen hata - logla ve kullanıcıya göster
        _logger.Error($"Unexpected error: {ex.Message}", ex);
        Status = TweakStatus.Failed;
        ErrorMessage = "Beklenmeyen bir hata oluştu";
    }
}

// 2. Fire-and-forget kullanma (event handler hariç)
// ❌ YANLIŞ
_ = SomeAsyncMethod();

// ✅ DOĞRU
await SomeAsyncMethod();
```

### Graceful Degradation

```csharp
public class MonitorViewModel
{
    private async Task InitializeMonitorsAsync()
    {
        // Her monitor bağımsız olarak başarısız olabilir
        try { _cpuMonitor = new CpuMonitor(); }
        catch (Exception ex) { _logger.Warning("CPU monitor failed"); }
        
        try { _ramMonitor = new RamMonitor(); }
        catch (Exception ex) { _logger.Warning("RAM monitor failed"); }
        
        try { _networkMonitor = new NetworkMonitor(); }
        catch (Exception ex) { _logger.Warning("Network monitor failed"); }
        
        // Bazıları başarısız olsa bile uygulama çalışmaya devam eder
    }
}
```

---

## 🧪 Test Gereksinimleri

### Minimum Test Kapsamı

| Katman | Minimum Coverage | Öncelikli Alanlar |
|--------|------------------|-------------------|
| Core | 80% | ITweak implementations |
| Engine | 70% | TweakExecutionPipeline |
| Infrastructure | 50% | Registry operations (mocked) |
| App | 30% | ViewModel logic |

### Test Kategorileri

```csharp
// Unit test örneği
[Fact]
public async Task DetectAsync_WhenValueExists_ReturnsDetected()
{
    // Arrange
    var mockRegistry = new Mock<IRegistryAccessor>();
    mockRegistry.Setup(...).Returns(1);
    
    var tweak = new RegistryValueTweak(mockRegistry.Object, ...);
    
    // Act
    var result = await tweak.DetectAsync(CancellationToken.None);
    
    // Assert
    Assert.Equal(TweakStatus.Detected, result.Status);
}

// Integration test örneği
[Fact]
public async Task Pipeline_ApplyThenRollback_RestoresOriginalState()
{
    // Arrange
    var fakeRegistry = new InMemoryRegistryAccessor();
    fakeRegistry.SetValue(..., 0);
    
    var pipeline = new TweakExecutionPipeline(...);
    var tweak = new RegistryValueTweak(fakeRegistry, ...);
    
    // Act
    await pipeline.ExecuteAsync(tweak, TweakExecutionOptions.ApplyAndVerify);
    await pipeline.ExecuteAsync(tweak, TweakExecutionOptions.Rollback);
    
    // Assert
    Assert.Equal(0, fakeRegistry.GetValue(...));
}
```

---

## 📋 Kontrol Listesi

### Yeni Tweak Ekleme Kontrol Listesi

- [ ] `ITweak` arayüzü tam implemente edildi
- [ ] Detect orijinal değeri saklıyor
- [ ] Apply öncesi Detect zorunlu
- [ ] Rollback orijinal değeri geri yüklüyor
- [ ] Risk seviyesi doğru belirlendi
- [ ] Kategori doğru seçildi
- [ ] Açıklama anlaşılır
- [ ] Dokümantasyon eklendi

### PR Kontrol Listesi

- [ ] Güvenlik kuralları ihlal edilmedi
- [ ] Birim testler yazıldı/güncellendi
- [ ] Build başarılı
- [ ] Linter uyarıları giderildi
- [ ] Dokümantasyon güncellendi
- [ ] HANDOFF_REPORT.md güncellendi

---

## 📚 Referanslar

- [AGENTS.md](AGENTS.md) - AI ajanları için temel kurallar
- [CLAUDE.md](CLAUDE.md) - Operasyonel notlar
- [ARCHITECTURE.md](ARCHITECTURE.md) - Detaylı mimari
- [DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) - Bilinen sorunlar
- [CONTRIBUTING.md](CONTRIBUTING.md) - Katkı rehberi
