# Open Trace Project - KapsamlÄ± Teknik Mimari DokÃ¼mantasyonu

Bu belge, uygulamanÄ±n tÃ¼m teknik bileÅŸenlerini, sensÃ¶r fallback mekanizmalarÄ±nÄ±, thread yÃ¶netimini, UI/UX yapÄ±sÄ±nÄ± ve servis mimarisini detaylÄ± olarak aÃ§Ä±klar.

---

## ðŸ“ Dosya YapÄ±sÄ± Ã–zeti

```
OpenTraceProject/
â”œâ”€â”€ OpenTraceProject.Core/           # Temel arayÃ¼zler ve modeller
â”œâ”€â”€ OpenTraceProject.Engine/         # Tweak iÅŸleme motoru
â”‚   â””â”€â”€ Tweaks/                      # 9 tweak tÃ¼rÃ¼
â”œâ”€â”€ OpenTraceProject.Infrastructure/ # AltyapÄ± servisleri
â”‚   â””â”€â”€ Metrics/                     # 11 metrik dosyasÄ± (102KB+)
â”œâ”€â”€ OpenTraceProject.App/            # Masaustu UI uygulamasi
â”‚   â”œâ”€â”€ Views/                       # XAML gÃ¶rÃ¼nÃ¼mleri
â”‚   â”œâ”€â”€ ViewModels/                  # MVVM ViewModelleri
â”‚   â””â”€â”€ Services/                    # Uygulama servisleri
â””â”€â”€ OpenTraceProject.ElevatedHost/   # YÃ¼kseltilmiÅŸ iÅŸlem sunucusu
```

---

## ðŸ”§ Servis Mimarisi

### 1. TweakProvider Sistemi (13 dosya)

Provider'lar `OpenTraceProject.App/Services/TweakProviders/` dizininde bulunur:

| Provider | Sorumluluk | Tweak SayÄ±sÄ± |
|----------|------------|--------------|
| `AudioTweakProvider` | Ses ayarlarÄ± (beep, ducking) | ~6 |
| `NetworkTweakProvider` | AÄŸ optimizasyonu (IPv6, SMB) | ~30+ |
| `PerformanceTweakProvider` | Animasyonlar, throttling | ~8 |
| `PeripheralTweakProvider` | Mouse, keyboard | ~10 |
| `PrivacyTweakProvider` | Telemetri, konum | ~70+ |
| `SecurityTweakProvider` | UAC, firewall, VBS | ~15 |
| `SystemTweakProvider` | Servisler, gÃ¶revler | ~25 |
| `SystemRegistryTweakProvider` | Kernel, NTFS, DWM | ~30 |
| `VisibilityTweakProvider` | UI Ã¶ÄŸeleri, spotlight | ~25 |
| `MiscTweakProvider` | DiÄŸer uygulamalar | ~5 |
| `LegacyTweakProvider` | Eski tweak kataloÄŸu | ~100+ |

### 2. Metrik Servisleri (11 dosya, 180KB+)

**Dosyalar:**
- `MetricProvider.cs` (102KB, 3053 satÄ±r) - Ana sensÃ¶r saÄŸlayÄ±cÄ±
- `ProcessMonitor.cs` (26KB, 908 satÄ±r) - Ä°ÅŸlem izleme
- `NetworkMonitor.cs` (12KB, 340 satÄ±r) - AÄŸ adaptÃ¶rleri
- `DiskMonitor.cs` (5.7KB) - Disk I/O
- `NetworkEtwSampler.cs` (3.9KB) - ETW aÄŸ Ã¶rnekleme
- `NetworkLatencyMonitor.cs` (3.4KB) - Ping/latency
- `WifiSignalMonitor.cs` (9.5KB) - WiFi sinyal gÃ¼cÃ¼
- `GpuEngineMonitor.cs` (4KB) - GPU motor kullanÄ±mÄ±
- `BootTimeTracker.cs` (9.2KB) - Boot sÃ¼resi analizi
- `KernelImpactAnalyzer.cs` (1.5KB) - Ã‡ekirdek etkisi
- `PerformanceSnapshots.cs` (1.7KB) - AnlÄ±k snapshot modelleri

---

## ðŸ”Œ SensÃ¶r ve Fallback MekanizmalarÄ±

### CPU SÄ±caklÄ±k Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor SensÃ¶rleri] -->|BaÅŸarÄ±sÄ±z| B[WMI MSAcpi_ThermalZoneTemperature]
    B -->|BaÅŸarÄ±sÄ±z| C["N/A" dÃ¶ndÃ¼r]
```

**Kod:** `MetricProvider.GetCpuTemperature()` (satÄ±r 210-263)

### CPU HÄ±z Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor Clock SensÃ¶rleri] -->|BaÅŸarÄ±sÄ±z| B[WMI Win32_Processor.CurrentClockSpeed]
    B -->|BaÅŸarÄ±sÄ±z| C[WMI Win32_PerfFormattedData_Counters]
    C -->|BaÅŸarÄ±sÄ±z| D[Base Clock dÃ¶ndÃ¼r]
```

**Kod:** `MetricProvider.TryGetCpuCurrentSpeedMhz()` (satÄ±r 1678-1826)

### GPU Bellek Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor GPU Memory SensÃ¶rleri] -->|BaÅŸarÄ±sÄ±z| B[PerformanceCounter GPU Dedicated Memory]
    B -->|BaÅŸarÄ±sÄ±z| C[Registry DirectX Version + WMI]
    C -->|BaÅŸarÄ±sÄ±z| D["N/A" dÃ¶ndÃ¼r]
```

**Kod:** `MetricProvider.TryGetGpuMemoryTotalMb()` (satÄ±r 2524-2620)

### AÄŸ Ä°ÅŸlem TrafiÄŸi Fallback Zinciri

```mermaid
graph TD
    A[ETW Network Sampler] -->|BaÅŸarÄ±sÄ±z veya Admin deÄŸil| B[TCP EStats API]
    B -->|BaÅŸarÄ±sÄ±z| C[Process I/O Counters Approximate]
```

**Kod:** `ProcessMonitor.GetTopProcessesByNetwork()` (satÄ±r 160-218)

```csharp
public List<ProcessInfo> GetTopProcessesByNetwork(int count = 10)
{
    // 1. ETW (en doÄŸru, admin gerektirir)
    if (TryGetEtwBytesByPid(out var etwBytes))
    {
        mode = NetworkProcessMode.TcpUdpEtw;
    }
    // 2. TCP EStats (orta doÄŸruluk)
    else if (TryGetTcpBytesByPid(out var tcpBytes))
    {
        mode = NetworkProcessMode.TcpOnly;
    }
    // 3. YaklaÅŸÄ±k I/O (dÃ¼ÅŸÃ¼k doÄŸruluk)
    else
    {
        return GetTopProcessesByIo(count); // Fallback
    }
}
```

### Disk SaÄŸlÄ±ÄŸÄ± Fallback Zinciri

```mermaid
graph TD
    A[LibreHardwareMonitor Storage SensÃ¶rleri] -->|BaÅŸarÄ±sÄ±z| B[WMI MSStorageDriver_FailurePredictStatus]
    B -->|BaÅŸarÄ±sÄ±z| C[WMI MSFT_PhysicalDisk.HealthStatus]
    C -->|BaÅŸarÄ±sÄ±z| D[WMI Win32_DiskDrive.Status]
    D -->|BaÅŸarÄ±sÄ±z| E["Unknown" dÃ¶ndÃ¼r]
```

**Kod:** `MetricProvider.GetDiskHealthSnapshot()` (satÄ±r 654-716)

---

## ðŸ§µ Thread ve Performans YÃ¶netimi

### DispatcherTimer KullanÄ±mÄ±

`MonitorViewModel.cs` ana timer'Ä± kontrol eder:

```csharp
// Timer aralÄ±ÄŸÄ±: 1 saniye
private readonly DispatcherTimer _timer = new()
{
    Interval = TimeSpan.FromSeconds(1)
};

// UI thread'inde Ã§alÄ±ÅŸÄ±r
private void Timer_Tick(object? sender, EventArgs e)
{
    UpdateMetrics();
}
```

### Background Thread KullanÄ±mÄ±

**I/O yoÄŸun iÅŸlemler Task.Run ile arka planda:**

```csharp
// SensÃ¶r gÃ¼ncellemeleri
await Task.Run(() => _metricProvider.UpdateHardware());

// Ä°ÅŸlem listesi
var processes = await Task.Run(() => _processMonitor.GetTopProcessesByCpu());
```

### CollectionView Thread GÃ¼venliÄŸi

**UI thread'ine deferral ile gÃ¼ncelleme:**

```csharp
await _dispatcherService.RunOnUIThreadAsync(() =>
{
    CollectionViewSource.GetDefaultView(collection).Refresh();
});
```

### WMI Scope BaÄŸlantÄ± YÃ¶netimi

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

## ðŸŽ¨ UI/UX Mimarisi

### MonitorView.xaml YapÄ±sÄ± (2734 satÄ±r)

**Ana BÃ¶lÃ¼mler:**

| BÃ¶lÃ¼m | SatÄ±r AralÄ±ÄŸÄ± | Ä°Ã§erik |
|-------|---------------|--------|
| Resources | 1-620 | Styles, Converters, Gradients |
| MonitorSystemInfoTemplate | 561-620 | Sistem bilgisi kartÄ± |
| MonitorPerformanceTemplate | 622-785 | Performans grafikleri |
| MonitorStatCardsTemplate | 787-1200 | CPU/RAM/Disk/Network kartlarÄ± |
| Process Lists | 1200-2200 | Ä°ÅŸlem tablolarÄ± |
| Network/Disk Sections | 2200-2734 | AdaptÃ¶r ve disk listeleri |

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

| Gradient | Renk | KullanÄ±m |
|----------|------|----------|
| `CpuAreaGradient` | Cyan | CPU grafik alanÄ± |
| `RamAreaGradient` | Green | RAM grafik alanÄ± |
| `DownloadAreaGradient` | Green | Ä°ndirme Ã§izgisi |
| `UploadAreaGradient` | Cyan | YÃ¼kleme Ã§izgisi |
| `DiskReadAreaGradient` | Green | Disk okuma |
| `DiskWriteAreaGradient` | Red | Disk yazma |

### Tema DesteÄŸi

**DynamicResource kullanÄ±mÄ±:**
```xml
Background="{DynamicResource BackgroundDarkestBrush}"
Foreground="{DynamicResource ForegroundBrightestBrush}"
```

**Tema dosyalarÄ±:**
- `Resources/Colors.xaml` - Dark theme
- `Resources/Colors.Light.xaml` - Light theme

---

## ðŸ“Š Veri AkÄ±ÅŸÄ±

### Monitor Veri AkÄ±ÅŸÄ±

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

### Tweak Uygulama AkÄ±ÅŸÄ±

```mermaid
sequenceDiagram
    participant User as KullanÄ±cÄ±
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

## ðŸ”’ GÃ¼venlik ve Yetkilendirme

### ElevatedHost Mimarisi

**Named Pipe Ä°letiÅŸimi:**
```
Pipe Name: OpenTraceProjectElevatedPipe
Security: Admin only access
```

**Ä°stek TÃ¼rleri:**
- Registry yazma (HKLM, HKCR)
- Servis yÃ¶netimi
- ZamanlanmÄ±ÅŸ gÃ¶rev yÃ¶netimi
- Sistem komutlarÄ± (bcdedit, powercfg)

### Process YÃ¶netimi P/Invoke

```csharp
// Thread durdurma/devam ettirme
[DllImport("kernel32.dll")]
private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, ...);

[DllImport("kernel32.dll")]
private static extern uint SuspendThread(IntPtr hThread);

// I/O sayaÃ§larÄ±
[DllImport("kernel32.dll")]
private static extern bool GetProcessIoCounters(IntPtr hProcess, out IoCounters ioCounters);

// TCP baÄŸlantÄ± istatistikleri
[DllImport("iphlpapi.dll")]
private static extern uint GetExtendedTcpTable(...);

[DllImport("iphlpapi.dll")]
private static extern uint GetPerTcpConnectionEStats(...);
```

---

## ðŸ“ˆ Performans OptimizasyonlarÄ±

### Bellek YÃ¶netimi

| Ã–zellik | Uygulama |
|---------|----------|
| Dictionary temizleme | `ProcessMonitor.Cleanup()` Ã¶lÃ¼ PID'leri kaldÄ±rÄ±r |
| Lazy loading | WMI scope'larÄ± lazy init |
| Dispose pattern | TÃ¼m monitor'lar IDisposable |
| Object pooling | PerformanceCounter reuse |

### CPU Optimizasyonu

| Teknik | AÃ§Ä±klama |
|--------|----------|
| Delta hesaplama | CPU/IO iÃ§in Ã¶nceki deÄŸerle karÅŸÄ±laÅŸtÄ±rma |
| Batch processing | TÃ¼m process'leri tek sorguda al |
| Caching | WMI sonuÃ§larÄ±nÄ± cache'le |
| Throttling | 1 saniye minimum gÃ¼ncelleme aralÄ±ÄŸÄ± |

---

## ðŸ› Bilinen Sorunlar ve Ã‡Ã¶zÃ¼mler

| Sorun | Sebep | Ã‡Ã¶zÃ¼m |
|-------|-------|-------|
| GPU temp N/A | LibreHardwareMonitor driver eksik | Fallback WMI kullan |
| Network list boÅŸ | Adapter adÄ± eÅŸleÅŸmiyor | Instance name normalization |
| CPU speed 0 | WMI sorgu hatasÄ± | Multiple fallback zinciri |
| High memory | Timer leak | Dispose pattern uygula |

---

## ðŸ“š Referanslar

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
- [WMI Classes Reference](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov)
- [PerformanceCounter Class](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter)
- [GetProcessIoCounters](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getprocessiocounters)
- [GetExtendedTcpTable](https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-getextendedtcptable)
- [Desktop animation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/animation-overview)
