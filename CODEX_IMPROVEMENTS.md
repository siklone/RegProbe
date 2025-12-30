# 💡 Windows Optimizer - İyileştirme Önerileri

**Son Güncelleme:** 30 Aralık 2025  
**Versiyon:** 1.0.0

---

> Not (2025-12-30): `LegacyTweakProvider` geçici bir köprü olarak eklendi. Parite tamamlandıktan sonra kategori provider'lara taşınıp kaldırılmalı.

## 📋 İçindekiler

1. [Kod Kalitesi İyileştirmeleri](#kod-kalitesi-iyileştirmeleri)
2. [Performans Optimizasyonları](#performans-optimizasyonları)
3. [Mimari İyileştirmeler](#mimari-iyileştirmeler)
4. [Kullanıcı Deneyimi](#kullanıcı-deneyimi)
5. [Güvenlik İyileştirmeleri](#güvenlik-iyileştirmeleri)
6. [Test Stratejisi](#test-stratejisi)
7. [DevOps & Altyapı](#devops--altyapı)
8. [Dokümantasyon](#dokümantasyon)

---

## 🔧 Kod Kalitesi İyileştirmeleri

### 1. TweaksViewModel Refactoring ✅ TAMAMLANDI

**Önceki Durum:**
- `TweaksViewModel.cs` → 4505 satır (çok büyük)
- Constructor → 3000+ satır (tek metod içinde tüm tweakler)

**Şimdiki Durum:**
- `TweaksViewModel.cs` → ~1300 satır
- 10 adet TweakProvider implementasyonu
- Dinamik provider injection

**Uygulanan İyileştirmeler:**

```csharp
// Yeni mimari (provider dosyaları)
// PrivacyTweakProvider.cs
public class PrivacyTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Privacy";
    
    public override IEnumerable<ITweak> CreateTweaks(...)
    {
        yield return CreateDisableTelemetryTweak();
        yield return CreateDisableActivityHistoryTweak();
    }
}
```

**Faydalar (Gerçekleşti):**
- ✅ Bakımı kolay
- ✅ Modüler yapı
- ✅ Yeni tweak ekleme kolaylığı
- ✅ Unit test edilebilirlik

---

### 2. Null Safety İyileştirmeleri

**Mevcut Durum:**
- Nullable reference types kısmen kullanılıyor
- Bazı potansiyel null dereference

**Önerilen:**
```csharp
// Directory.Build.props
<PropertyGroup>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

**Kontrol Edilecek Alanlar:**
- [ ] MonitorViewModel null service handling
- [ ] TweakItemViewModel property access
- [ ] Registry value reads

---

### 3. Async Best Practices

**Mevcut Sorunlar:**
- `async void` kullanımı (event handler dışında)
- Fire-and-forget pattern

**Önerilen Düzeltmeler:**
```csharp
// Kötü
public async void ToggleExpand()
{
    await DetectAllTweaksAsync();
}

// İyi
public async Task ToggleExpandAsync()
{
    await DetectAllTweaksAsync();
}

// Event handler için
public async void OnButtonClick(object sender, EventArgs e)
{
    try
    {
        await ToggleExpandAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex);
    }
}
```

---

### 4. Dependency Injection İyileştirmeleri

**Mevcut Durum:**
- Constructor içinde manuel servis oluşturma
- Bazı tight coupling

**Önerilen:**
```csharp
// App.xaml.cs
services.AddSingleton<IAppLogger, FileAppLogger>();
services.AddSingleton<ITweakLogStore, FileTweakLogStore>();
services.AddSingleton<IRollbackStateStore, RollbackStateStore>();
services.AddTransient<TweaksViewModel>();
services.AddHostedService<MetricsBackgroundService>();
```

---

## ⚡ Performans Optimizasyonları

### 5. Tweak Lazy Loading

**Mevcut Durum:**
- Tüm tweakler uygulama başlangıcında yükleniyor
- 200+ tweak → gecikme potansiyeli

**Önerilen:**
```csharp
public class LazyTweakProvider
{
    private readonly Lazy<IEnumerable<ITweak>> _tweaks;
    
    public IEnumerable<ITweak> GetTweaks()
    {
        return _tweaks.Value;
    }
}
```

---

### 6. Virtualization İyileştirmesi

**Mevcut Durum:**
- ItemsControl ile tweak listesi
- Büyük listeler için performans sorunu potansiyeli

**Önerilen:**
```xml
<!-- VirtualizingStackPanel kullanımı -->
<ItemsControl ItemsSource="{Binding Tweaks}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel 
                VirtualizingPanel.IsVirtualizing="True"
                VirtualizingPanel.VirtualizationMode="Recycling"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

---

### 7. Metrik Caching

**Mevcut Durum:**
- Her 1 saniyede tam metrik toplama
- Bazı statik veriler gereksiz yenileniyor

**Önerilen:**
```csharp
public class CachedMetricProvider
{
    private readonly TimeSpan _staticDataCacheDuration = TimeSpan.FromMinutes(5);
    private HardwareInfo? _cachedHardwareInfo;
    private DateTime _lastHardwareCacheTime;
    
    public HardwareInfo GetHardwareInfo()
    {
        if (_cachedHardwareInfo != null && 
            DateTime.Now - _lastHardwareCacheTime < _staticDataCacheDuration)
        {
            return _cachedHardwareInfo;
        }
        
        _cachedHardwareInfo = CollectHardwareInfo();
        _lastHardwareCacheTime = DateTime.Now;
        return _cachedHardwareInfo;
    }
}
```

---

### 8. String Interning

**Mevcut Durum:**
- `StringPool.cs` mevcut ama kullanımı sınırlı

**Önerilen Genişletme:**
```csharp
// Sık kullanılan stringler için pool
public static class TweakStringPool
{
    public static readonly string Applied = "Applied";
    public static readonly string NotApplied = "Not Applied";
    public static readonly string Unknown = "Unknown";
    public static readonly string[] Categories = { "Privacy", "Security", ... };
}
```

---

## 🏗️ Mimari İyileştirmeler

### 9. CQRS Pattern (Büyük Ölçekli)

**Mevcut:** Tek model hem read hem write

**Önerilen Ayrım:**
```csharp
// Commands
public record ApplyTweakCommand(string TweakId);
public record RollbackTweakCommand(string TweakId);

// Queries
public record GetTweakStatusQuery(string TweakId);
public record GetAllTweaksQuery(CategoryFilter? Filter);

// Handlers
public class ApplyTweakHandler : ICommandHandler<ApplyTweakCommand>
{
    public async Task HandleAsync(ApplyTweakCommand command)
    {
        // ...
    }
}
```

---

### 10. Event Sourcing (Gelişmiş)

**Mevcut:** State değişiklikleri kaybolabiliyor

**Önerilen:**
```csharp
// Tüm tweak olayları kayıt altında
public interface ITweakEvent
{
    string TweakId { get; }
    DateTime Timestamp { get; }
}

public record TweakApplied(string TweakId, DateTime Timestamp, object? PreviousValue) : ITweakEvent;
public record TweakRolledBack(string TweakId, DateTime Timestamp) : ITweakEvent;

// Event store
public class TweakEventStore
{
    public Task AppendAsync(ITweakEvent @event);
    public Task<IEnumerable<ITweakEvent>> GetEventsAsync(string tweakId);
}
```

---

### 11. Mediator Pattern

**Mevcut:** ViewModel'lar arası doğrudan bağımlılık

**Önerilen:**
```csharp
// MediatR veya custom mediator
public interface IMediator
{
    Task SendAsync<TRequest>(TRequest request);
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request);
}

// Kullanım
await _mediator.SendAsync(new RefreshHealthScoreRequest());
```

---

## 🎨 Kullanıcı Deneyimi

### 12. Progress İndikatörleri

**Mevcut:** Bazı işlemler sırasında feedback eksik

**Önerilen:**
```xml
<!-- Genel progress overlay -->
<Grid Visibility="{Binding IsOperationInProgress}">
    <Border Background="#80000000">
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
            <ProgressBar IsIndeterminate="True" Width="200"/>
            <TextBlock Text="{Binding OperationMessage}" />
            <Button Content="Cancel" Command="{Binding CancelOperationCommand}"/>
        </StackPanel>
    </Border>
</Grid>
```

---

### 13. Toast Notifications

**Mevcut:** MessageBox veya inline mesajlar

**Önerilen:**
```csharp
public interface INotificationService
{
    void ShowSuccess(string message, TimeSpan? duration = null);
    void ShowWarning(string message, TimeSpan? duration = null);
    void ShowError(string message, TimeSpan? duration = null);
    void ShowInfo(string message, TimeSpan? duration = null);
}

// Kullanım
_notificationService.ShowSuccess("3 tweak başarıyla uygulandı");
```

---

### 14. Undo/Redo Stack

**Mevcut:** Tek seviye rollback

**Önerilen:**
```csharp
public class UndoRedoStack
{
    private readonly Stack<ITweakAction> _undoStack = new();
    private readonly Stack<ITweakAction> _redoStack = new();
    
    public void Push(ITweakAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
    }
    
    public async Task UndoAsync()
    {
        if (_undoStack.TryPop(out var action))
        {
            await action.UndoAsync();
            _redoStack.Push(action);
        }
    }
}
```

---

### 15. Keyboard Shortcuts

**Mevcut:** Sadece fare navigasyonu

**Önerilen:**
```csharp
// MainWindow.xaml.cs
<Window.InputBindings>
    <KeyBinding Key="F5" Command="{Binding RefreshCommand}" />
    <KeyBinding Key="A" Modifiers="Control" Command="{Binding SelectAllCommand}" />
    <KeyBinding Key="D" Modifiers="Control" Command="{Binding DeselectAllCommand}" />
    <KeyBinding Key="F" Modifiers="Control" Command="{Binding FocusSearchCommand}" />
    <KeyBinding Key="Z" Modifiers="Control" Command="{Binding UndoCommand}" />
</Window.InputBindings>
```

---

### 16. Onboarding Wizard

**Mevcut:** İlk kullanım için rehberlik yok

**Önerilen:**
```csharp
public class OnboardingService
{
    private const string HasCompletedOnboardingKey = "HasCompletedOnboarding";
    
    public async Task<bool> ShouldShowOnboardingAsync()
    {
        return !await _settings.GetAsync<bool>(HasCompletedOnboardingKey);
    }
    
    public async Task ShowOnboardingAsync()
    {
        // Step 1: Hoşgeldin
        // Step 2: Risk seviyeleri açıklaması
        // Step 3: Restore point önerisi
        // Step 4: Profil seçimi
    }
}
```

---

## 🔒 Güvenlik İyileştirmeleri

### 17. Digital Signature Verification

**Mevcut:** Plugin imza doğrulama stub

**Önerilen:**
```csharp
public class PluginSignatureVerifier
{
    public bool VerifySignature(string pluginPath)
    {
        var cert = X509Certificate.CreateFromSignedFile(pluginPath);
        if (cert == null) return false;
        
        var cert2 = new X509Certificate2(cert);
        var chain = new X509Chain();
        
        return chain.Build(cert2) && 
               IsTrustedPublisher(cert2);
    }
    
    private bool IsTrustedPublisher(X509Certificate2 cert)
    {
        // Güvenilir publisher listesi kontrolü
        var trustedThumbprints = LoadTrustedThumbprints();
        return trustedThumbprints.Contains(cert.Thumbprint);
    }
}
```

---

### 18. Secure Settings Storage

**Mevcut:** Ayarlar plain text

**Önerilen:**
```csharp
public class SecureSettingsStore
{
    public async Task<T> GetSecureAsync<T>(string key)
    {
        var encrypted = await File.ReadAllBytesAsync(GetPath(key));
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(decrypted));
    }
    
    public async Task SetSecureAsync<T>(string key, T value)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(value);
        var encrypted = ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(GetPath(key), encrypted);
    }
}
```

---

### 19. Input Validation

**Mevcut:** Sınırlı input validation

**Önerilen:**
```csharp
public static class TweakValidation
{
    public static ValidationResult ValidateTweakId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ValidationResult.Error("Tweak ID cannot be empty");
            
        if (!Regex.IsMatch(id, @"^[a-z0-9\-\.]+$"))
            return ValidationResult.Error("Invalid tweak ID format");
            
        return ValidationResult.Success();
    }
    
    public static ValidationResult ValidateRegistryPath(string path)
    {
        // Tehlikeli path kontrolü
        var dangerousPaths = new[] { @"HKLM\SYSTEM", @"HKLM\SAM" };
        if (dangerousPaths.Any(d => path.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Error("Access to this registry path is not allowed");
            
        return ValidationResult.Success();
    }
}
```

---

## 🧪 Test Stratejisi

### 20. Test Kategorileri

**Önerilen Test Piramidi:**

```
                    ┌───────────────┐
                    │   E2E Tests   │  ← 5%
                    │   (Selenium)  │
                ┌───┴───────────────┴───┐
                │  Integration Tests    │  ← 20%
                │  (Mocked Registry)    │
            ┌───┴───────────────────────┴───┐
            │       Unit Tests              │  ← 75%
            │  (Each class in isolation)    │
            └───────────────────────────────┘
```

---

### 21. Test Pattern'ler

```csharp
// Arrange-Act-Assert pattern
[Fact]
public async Task ApplyAsync_WhenRegistryKeyExists_ModifiesValue()
{
    // Arrange
    var mockRegistry = new Mock<IRegistryAccessor>();
    mockRegistry.Setup(r => r.GetValue(...)).Returns(0);
    
    var tweak = new RegistryValueTweak(mockRegistry.Object, ...);
    
    // Act
    var result = await tweak.ApplyAsync(CancellationToken.None);
    
    // Assert
    Assert.Equal(TweakStatus.Applied, result.Status);
    mockRegistry.Verify(r => r.SetValue(...), Times.Once);
}

// Golden path + edge cases
[Theory]
[InlineData(0, 1, TweakStatus.Applied)]
[InlineData(1, 1, TweakStatus.Applied)] // Already set
[InlineData(null, 1, TweakStatus.Applied)] // Key didn't exist
public async Task ApplyAsync_VariousScenarios(int? initial, int desired, TweakStatus expected)
{
    // ...
}
```

---

### 22. Mock Infrastructure

```csharp
public class FakeRegistryAccessor : IRegistryAccessor
{
    private readonly Dictionary<(RegistryHive, string, string), object?> _values = new();
    
    public object? GetValue(RegistryHive hive, string key, string valueName)
    {
        return _values.TryGetValue((hive, key, valueName), out var value) ? value : null;
    }
    
    public void SetValue(RegistryHive hive, string key, string valueName, object value, RegistryValueKind kind)
    {
        _values[(hive, key, valueName)] = value;
    }
}
```

---

## 🚀 DevOps & Altyapı

### 23. GitHub Actions Workflow

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore -c Release
    
    - name: Test
      run: dotnet test --no-build -c Release --logger trx
    
    - name: Publish
      run: dotnet publish WindowsOptimizer.App -c Release -r win-x64 --self-contained -o publish
    
    - name: Verify ElevatedHost
      run: |
        if (!(Test-Path "publish/ElevatedHost/WindowsOptimizer.ElevatedHost.exe")) {
          Write-Error "ElevatedHost.exe not found!"
          exit 1
        }
    
    - name: Upload Artifact
      uses: actions/upload-artifact@v4
      with:
        name: WindowsOptimizer
        path: publish/
```

---

### 24. Release Automation

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Build Release
      run: |
        dotnet publish WindowsOptimizer.App -c Release -r win-x64 --self-contained -o release
    
    - name: Create Installer
      run: |
        # WiX build komutları
    
    - name: Create Release
      uses: softprops/action-gh-release@v1
      with:
        files: |
          release/**/*
          installer/*.msi
        generate_release_notes: true
```

---

## 📚 Dokümantasyon

### 25. API Dokümantasyonu

**Önerilen:** DocFX veya XML comments

```csharp
/// <summary>
/// Applies a registry-based tweak to the Windows system.
/// </summary>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// A <see cref="TweakResult"/> indicating success or failure.
/// </returns>
/// <exception cref="OperationCanceledException">
/// The operation was cancelled.
/// </exception>
/// <example>
/// <code>
/// var tweak = new RegistryValueTweak(...);
/// var result = await tweak.ApplyAsync(CancellationToken.None);
/// if (result.Status == TweakStatus.Applied)
///     Console.WriteLine("Tweak applied successfully!");
/// </code>
/// </example>
public async Task<TweakResult> ApplyAsync(CancellationToken cancellationToken)
```

---

### 26. Tweak Wiki

**Önerilen Yapı:**
```
/docs
  /tweaks
    /privacy
      disable-telemetry.md
      disable-activity-history.md
    /security
      enable-uac.md
    /power
      disable-power-throttling.md
  /guides
    getting-started.md
    creating-plugins.md
    troubleshooting.md
  /api
    ITweak.md
    ITweakProvider.md
```

---

## 📊 Öncelik Özeti

| İyileştirme | Etki | Çaba | Öncelik |
|-------------|------|------|---------|
| TweaksViewModel refactoring | Yüksek | Yüksek | 🔴 Kritik |
| Async best practices | Orta | Düşük | 🟠 Yüksek |
| Virtualization | Orta | Düşük | 🟠 Yüksek |
| Progress indicators | Yüksek | Düşük | 🟠 Yüksek |
| CI/CD pipeline | Yüksek | Orta | 🟠 Yüksek |
| Unit test coverage | Yüksek | Yüksek | 🟡 Orta |
| Null safety | Orta | Orta | 🟡 Orta |
| DI improvements | Orta | Orta | 🟡 Orta |
| Toast notifications | Orta | Düşük | 🟢 Düşük |
| Keyboard shortcuts | Düşük | Düşük | 🟢 Düşük |
| CQRS pattern | Düşük | Yüksek | 🔵 Gelecek |
| Event sourcing | Düşük | Yüksek | 🔵 Gelecek |

---

## 📝 Uygulama Önerileri

1. **Küçük, Sık Commitler:** Her iyileştirme ayrı PR olarak
2. **Geri Uyumluluk:** Mevcut API'yi bozmadan değişiklik
3. **Feature Flags:** Yeni özellikler toggle arkasında
4. **Aşamalı Geçiş:** Büyük refactoring'ler aşamalı
5. **Dokümantasyon Önce:** Değişiklik öncesi dokümante et
