# RegProbe - KapsamlÃ„Â± Teknik Mimari DokÃƒÂ¼mantasyonu

Bu belge, uygulamanÃ„Â±n tÃƒÂ¼m teknik bileÃ…Å¸enlerini, sensÃƒÂ¶r fallback mekanizmalarÃ„Â±nÃ„Â±, thread yÃƒÂ¶netimini, UI/UX yapÃ„Â±sÃ„Â±nÃ„Â± ve servis mimarisini detaylÃ„Â± olarak aÃƒÂ§Ã„Â±klar.

---

## Ã°Å¸â€œÂ Dosya YapÃ„Â±sÃ„Â± Ãƒâ€“zeti

```
RegProbe/
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ core/           # Temel arayÃƒÂ¼zler ve modeller
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ engine/         # Tweak iÃ…Å¸leme motoru
Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Tweaks/                      # 9 tweak tÃƒÂ¼rÃƒÂ¼
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ infrastructure/ # AltyapÃ„Â± servisleri
Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Metrics/                     # 11 metrik dosyasÃ„Â± (102KB+)
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ app/            # Masaustu UI uygulamasi
Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Views/                       # XAML gÃƒÂ¶rÃƒÂ¼nÃƒÂ¼mleri
Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ ViewModels/                  # MVVM ViewModelleri
Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Services/                    # Uygulama servisleri
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ elevated-host/   # YÃƒÂ¼kseltilmiÃ…Å¸ iÃ…Å¸lem sunucusu
```

---

## Ã°Å¸â€Â§ Servis Mimarisi

### 1. TweakProvider Sistemi (13 dosya)

Provider'lar `app/Services/TweakProviders/` dizininde bulunur:

| Provider | Sorumluluk | Tweak SayÃ„Â±sÃ„Â± |
|----------|------------|--------------|
| `AudioTweakProvider` | Ses ayarlarÃ„Â± (beep, ducking) | ~6 |
| `NetworkTweakProvider` | AÃ„Å¸ optimizasyonu (IPv6, SMB) | ~30+ |
| `PerformanceTweakProvider` | Animasyonlar, throttling | ~8 |
| `PeripheralTweakProvider` | Mouse, keyboard | ~10 |
| `PrivacyTweakProvider` | Telemetri, konum | ~70+ |
| `SecurityTweakProvider` | UAC, firewall, VBS | ~15 |
| `SystemTweakProvider` | Servisler, gÃƒÂ¶revler | ~25 |
| `SystemRegistryTweakProvider` | Kernel, NTFS, DWM | ~30 |
| `VisibilityTweakProvider` | UI ÃƒÂ¶Ã„Å¸eleri, spotlight | ~25 |
| `MiscTweakProvider` | DiÃ„Å¸er uygulamalar | ~5 |
| `LegacyTweakProvider` | Eski tweak kataloÃ„Å¸u | ~100+ |

### 2. Metrik Servisleri (11 dosya, 180KB+)

**Dosyalar:**
- `MetricProvider.cs` (102KB, 3053 satÃ„Â±r) - Ana sensÃƒÂ¶r saÃ„Å¸layÃ„Â±cÃ„Â±
- `ProcessMonitor.cs` (26KB, 908 satÃ„Â±r) - Ã„Â°Ã…Å¸lem izleme
- `NetworkMonitor.cs` (12KB, 340 satÃ„Â±r) - AÃ„Å¸ adaptÃƒÂ¶rleri
- `DiskMonitor.cs` (5.7KB) - Disk I/O
- `NetworkEtwSampler.cs` (3.9KB) - ETW aÃ„Å¸ ÃƒÂ¶rnekleme
- `NetworkLatencyMonitor.cs` (3.4KB) - Ping/latency
- `WifiSignalMonitor.cs` (9.5KB) - WiFi sinyal gÃƒÂ¼cÃƒÂ¼
- `GpuEngineMonitor.cs` (4KB) - GPU motor kullanÃ„Â±mÃ„Â±
- `BootTimeTracker.cs` (9.2KB) - Boot sÃƒÂ¼resi analizi
- `KernelImpactAnalyzer.cs` (1.5KB) - Ãƒâ€¡ekirdek etkisi
- `PerformanceSnapshots.cs` (1.7KB) - AnlÃ„Â±k snapshot modelleri

---

## Ã°Å¸â€Å’ SensÃƒÂ¶r ve Fallback MekanizmalarÃ„Â±

### CPU SÃ„Â±caklÃ„Â±k Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor SensÃƒÂ¶rleri] -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| B[WMI MSAcpi_ThermalZoneTemperature]
    B -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| C["N/A" dÃƒÂ¶ndÃƒÂ¼r]
```

**Kod:** `MetricProvider.GetCpuTemperature()` (satÃ„Â±r 210-263)

### CPU HÃ„Â±z Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor Clock SensÃƒÂ¶rleri] -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| B[WMI Win32_Processor.CurrentClockSpeed]
    B -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| C[WMI Win32_PerfFormattedData_Counters]
    C -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| D[Base Clock dÃƒÂ¶ndÃƒÂ¼r]
```

**Kod:** `MetricProvider.TryGetCpuCurrentSpeedMhz()` (satÃ„Â±r 1678-1826)

### GPU Bellek Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor GPU Memory SensÃƒÂ¶rleri] -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| B[PerformanceCounter GPU Dedicated Memory]
    B -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| C[Registry DirectX Version + WMI]
    C -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| D["N/A" dÃƒÂ¶ndÃƒÂ¼r]
```

**Kod:** `MetricProvider.TryGetGpuMemoryTotalMb()` (satÃ„Â±r 2524-2620)

### AÃ„Å¸ Ã„Â°Ã…Å¸lem TrafiÃ„Å¸i Fallback Zinciri

```mermaid
graph TD
    A[ETW Network Sampler] -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z veya Admin deÃ„Å¸il| B[TCP EStats API]
    B -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| C[Process I/O Counters Approximate]
```

**Kod:** `ProcessMonitor.GetTopProcessesByNetwork()` (satÃ„Â±r 160-218)

```csharp
public List<ProcessInfo> GetTopProcessesByNetwork(int count = 10)
{
    // 1. ETW (en doÃ„Å¸ru, admin gerektirir)
    if (TryGetEtwBytesByPid(out var etwBytes))
    {
        mode = NetworkProcessMode.TcpUdpEtw;
    }
    // 2. TCP EStats (orta doÃ„Å¸ruluk)
    else if (TryGetTcpBytesByPid(out var tcpBytes))
    {
        mode = NetworkProcessMode.TcpOnly;
    }
    // 3. YaklaÃ…Å¸Ã„Â±k I/O (dÃƒÂ¼Ã…Å¸ÃƒÂ¼k doÃ„Å¸ruluk)
    else
    {
        return GetTopProcessesByIo(count); // Fallback
    }
}
```

### Disk SaÃ„Å¸lÃ„Â±Ã„Å¸Ã„Â± Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor Storage SensÃƒÂ¶rleri] -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| B[WMI MSStorageDriver_FailurePredictStatus]
    B -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| C[WMI MSFT_PhysicalDisk.HealthStatus]
    C -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| D[WMI Win32_DiskDrive.Status]
    D -->|BaÃ…Å¸arÃ„Â±sÃ„Â±z| E["Unknown" dÃƒÂ¶ndÃƒÂ¼r]
```

**Kod:** `MetricProvider.GetDiskHealthSnapshot()` (satÃ„Â±r 654-716)

---

## Ã°Å¸Â§Âµ Thread ve Performans YÃƒÂ¶netimi

### DispatcherTimer KullanÃ„Â±mÃ„Â±

`MonitorViewModel.cs` ana timer'Ã„Â± kontrol eder:

```csharp
// Timer aralÃ„Â±Ã„Å¸Ã„Â±: 1 saniye
private readonly DispatcherTimer _timer = new()
{
    Interval = TimeSpan.FromSeconds(1)
};

// UI thread'inde ÃƒÂ§alÃ„Â±Ã…Å¸Ã„Â±r
private void Timer_Tick(object? sender, EventArgs e)
{
    UpdateMetrics();
}
```

### Background Thread KullanÃ„Â±mÃ„Â±

**I/O yoÃ„Å¸un iÃ…Å¸lemler Task.Run ile arka planda:**

```csharp
// SensÃƒÂ¶r gÃƒÂ¼ncellemeleri
await Task.Run(() => _metricProvider.UpdateHardware());

// Ã„Â°Ã…Å¸lem listesi
var processes = await Task.Run(() => _processMonitor.GetTopProcessesByCpu());
```

### CollectionView Thread GÃƒÂ¼venliÃ„Å¸i

**UI thread'ine deferral ile gÃƒÂ¼ncelleme:**

```csharp
await _dispatcherService.RunOnUIThreadAsync(() =>
{
    CollectionViewSource.GetDefaultView(collection).Refresh();
});
```

### WMI Scope BaÃ„Å¸lantÃ„Â± YÃƒÂ¶netimi

**Lazy initialization pattern:**

```csharp
private ManagementScope? _storageScope;

private bool TryConnectStorageScope()
{
    if (_storageScope?.IsConnected == true) return true;

    _storageScope = new ManagementScope(@"\\.\root\WMI");
    _storageScope.Connect();
    return _storageScope.IsConnected;
}
```

---

## Ã°Å¸Å½Â¨ UI/UX Mimarisi

### MonitorView.xaml YapÃ„Â±sÃ„Â± (2734 satÃ„Â±r)

**Ana BÃƒÂ¶lÃƒÂ¼mler:**

| BÃƒÂ¶lÃƒÂ¼m | SatÃ„Â±r AralÃ„Â±Ã„Å¸Ã„Â± | Ã„Â°ÃƒÂ§erik |
|-------|---------------|--------|
| Resources | 1-620 | Styles, Converters, Gradients |
| MonitorSystemInfoTemplate | 561-620 | Sistem bilgisi kartÃ„Â± |
| MonitorPerformanceTemplate | 622-785 | Performans grafikleri |
| MonitorStatCardsTemplate | 787-1200 | CPU/RAM/Disk/Network kartlarÃ„Â± |
| Process Lists | 1200-2200 | Ã„Â°Ã…Å¸lem tablolarÃ„Â± |
| Network/Disk Sections | 2200-2734 | AdaptÃƒÂ¶r ve disk listeleri |

### Animasyon Sistemi

**Card Reveal Animation:**
```xml
<DoubleAnimation From="0" To="1" Duration="0:0:0.35"/>
<DoubleAnimation From="12" To="0" Duration="0:0:0.35"/> <!-- TranslateY -->
```

**Hover Scale Animation:**
```xml
<DoubleAnimation To="1.015" Duration="0:0:0.12"/> <!-- ScaleX/Y -->
```

**Chart Glow Effects:**
```xml
<DropShadowEffect Color="Nord8" BlurRadius="10" Opacity="0.7"/>
```

### Gradient Sistemleri

| Gradient | Renk | KullanÃ„Â±m |
|----------|------|----------|
| `CpuAreaGradient` | Cyan | CPU grafik alanÃ„Â± |
| `RamAreaGradient` | Green | RAM grafik alanÃ„Â± |
| `DownloadAreaGradient` | Green | Ã„Â°ndirme ÃƒÂ§izgisi |
| `UploadAreaGradient` | Cyan | YÃƒÂ¼kleme ÃƒÂ§izgisi |
| `DiskReadAreaGradient` | Green | Disk okuma |
| `DiskWriteAreaGradient` | Red | Disk yazma |

### Tema DesteÃ„Å¸i

**DynamicResource kullanÃ„Â±mÃ„Â±:**
```xml
Background="{DynamicResource BackgroundDarkestBrush}"
Foreground="{DynamicResource ForegroundBrightestBrush}"
```

**Tema dosyalarÃ„Â±:**
- `Resources/Colors.xaml` - Dark theme
- `Resources/Colors.Light.xaml` - Light theme

---

## Ã°Å¸â€œÅ  Veri AkÃ„Â±Ã…Å¸Ã„Â±

### Monitor Veri AkÃ„Â±Ã…Å¸Ã„Â±

```mermaid
sequenceDiagram
    participant Timer as DispatcherTimer
    participant VM as MonitorViewModel
    participant MP as MetricProvider
    participant PM as ProcessMonitor
    participant UI as MonitorView

    Timer->>VM: Tick (1 saniye)
    VM->>MP: GetCpuUsage()
    VM->>MP: GetAvailableRamMb()
    VM->>MP: GetCpuTemperature()
    VM->>PM: GetTopProcessesByCpu()
    MP-->>VM: Metrics
    PM-->>VM: ProcessList
    VM->>UI: PropertyChanged
    UI->>UI: Binding Update
```

### Tweak Uygulama AkÃ„Â±Ã…Å¸Ã„Â±

```mermaid
sequenceDiagram
    participant User as KullanÃ„Â±cÃ„Â±
    participant VM as TweaksViewModel
    participant Pipe as TweakExecutionPipeline
    participant Tweak as ITweak
    participant Store as RollbackStateStore
    participant Host as ElevatedHost

    User->>VM: Apply Click
    VM->>Store: SaveOriginalStateAsync()
    VM->>Pipe: ExecuteAsync(tweak)
    Pipe->>Tweak: DetectAsync()
    Pipe->>Tweak: ApplyAsync()
    alt RequiresElevation
        Pipe->>Host: SendElevatedRequest()
    end
    Pipe->>Tweak: VerifyAsync()
    Pipe->>Store: MarkAppliedAsync()
    Pipe-->>VM: TweakReport
    VM->>User: Status Update
```

---

## Ã°Å¸â€â€™ GÃƒÂ¼venlik ve Yetkilendirme

### ElevatedHost Mimarisi

**Named Pipe Ã„Â°letiÃ…Å¸imi:**
```
Pipe Name: RegProbeElevatedPipe
Security: Admin only access
```

**Ã„Â°stek TÃƒÂ¼rleri:**
- Registry yazma (HKLM, HKCR)
- Servis yÃƒÂ¶netimi
- ZamanlanmÃ„Â±Ã…Å¸ gÃƒÂ¶rev yÃƒÂ¶netimi
- Sistem komutlarÃ„Â± (bcdedit, powercfg)

### Process YÃƒÂ¶netimi P/Invoke

```csharp
// Thread durdurma/devam ettirme
[DllImport("kernel32.dll")]
private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, ...);

[DllImport("kernel32.dll")]
private static extern uint SuspendThread(IntPtr hThread);

// I/O sayaÃƒÂ§larÃ„Â±
[DllImport("kernel32.dll")]
private static extern bool GetProcessIoCounters(IntPtr hProcess, out IoCounters ioCounters);

// TCP baÃ„Å¸lantÃ„Â± istatistikleri
[DllImport("iphlpapi.dll")]
private static extern uint GetExtendedTcpTable(...);

[DllImport("iphlpapi.dll")]
private static extern uint GetPerTcpConnectionEStats(...);
```

---

## Ã°Å¸â€œË† Performans OptimizasyonlarÃ„Â±

### Bellek YÃƒÂ¶netimi

| Ãƒâ€“zellik | Uygulama |
|---------|----------|
| Dictionary temizleme | `ProcessMonitor.Cleanup()` ÃƒÂ¶lÃƒÂ¼ PID'leri kaldÃ„Â±rÃ„Â±r |
| Lazy loading | WMI scope'larÃ„Â± lazy init |
| Dispose pattern | TÃƒÂ¼m monitor'lar IDisposable |
| Object pooling | PerformanceCounter reuse |

### CPU Optimizasyonu

| Teknik | AÃƒÂ§Ã„Â±klama |
|--------|----------|
| Delta hesaplama | CPU/IO iÃƒÂ§in ÃƒÂ¶nceki deÃ„Å¸erle karÃ…Å¸Ã„Â±laÃ…Å¸tÃ„Â±rma |
| Batch processing | TÃƒÂ¼m process'leri tek sorguda al |
| Caching | WMI sonuÃƒÂ§larÃ„Â±nÃ„Â± cache'le |
| Throttling | 1 saniye minimum gÃƒÂ¼ncelleme aralÃ„Â±Ã„Å¸Ã„Â± |

---

## Ã°Å¸Ââ€º Bilinen Sorunlar ve Ãƒâ€¡ÃƒÂ¶zÃƒÂ¼mler

| Sorun | Sebep | Ãƒâ€¡ÃƒÂ¶zÃƒÂ¼m |
|-------|-------|-------|
| GPU temp N/A | LibreHardwareMonitor driver eksik | Fallback WMI kullan |
| Network list boÃ…Å¸ | Adapter adÃ„Â± eÃ…Å¸leÃ…Å¸miyor | Instance name normalization |
| CPU speed 0 | WMI sorgu hatasÃ„Â± | Multiple fallback zinciri |
| High memory | Timer leak | Dispose pattern uygula |

---

## Ã°Å¸â€œÅ¡ Referanslar

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
- [WMI Classes Reference](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov)
- [PerformanceCounter Class](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter)
- [GetProcessIoCounters](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getprocessiocounters)
- [GetExtendedTcpTable](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable)
- [Desktop animation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/animation-overview)

