using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Diagnostics;
using BaseViewModel = WindowsOptimizer.App.ViewModels.Base.HardwareDetailViewModelBase;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public sealed class GpuDetailViewModel : BaseViewModel
{
    public override HardwareType HardwareType => HardwareType.Gpu;

    public GpuDetailViewModel()
    {
        Title = "GPU";
        Subtitle = "Graphics";
    }

    public override void LoadFromCache()
    {
        var gpu = Cache.Get<GpuHardwareData>("GPU");
        AppDiagnostics.Log($"[GpuDetailVM] Cache raw: Name={gpu?.Name ?? "<null>"}, Vendor={gpu?.Vendor ?? "<null>"}, AdapterRamBytes={gpu?.AdapterRamBytes.ToString() ?? "<null>"}");
        // Treat null or effectively-empty GPU entries as a cache miss so the
        // fallback preloader can run and populate full details.
        if (gpu == null || (string.IsNullOrWhiteSpace(gpu.Name) && string.IsNullOrWhiteSpace(gpu.Vendor) && gpu.AdapterRamBytes <= 0))
        {
            Title = "GPU";
            Subtitle = "No data available";
            AddRow("Error", "GPU data not loaded");
            AppDiagnostics.Log("[GpuDetailVM] Cache miss or trivial entry");
            SetLoadingComplete();
            return;
        }

        // Mark that we successfully loaded from cache so the base fallback won't run
        WasCacheHit = true;
        AppDiagnostics.Log("[GpuDetailVM] Cache hit — populating properties from cache");

        Title = gpu.Name ?? gpu.Vendor ?? "GPU";
        Subtitle = gpu.Vendor ?? "Unknown";

        ClearSpecs();

        AddHeader("Identification");
        AddRow("Name", gpu.Name);
        AddRow("Vendor", gpu.Vendor);
        AddRowIf("Video Processor", gpu.VideoProcessor);
        AddRow("Vendor ID", gpu.VendorId);
        AddRow("Device ID", gpu.DeviceId);
        AddRowIf("PNP Device ID", gpu.PnpDeviceId);

        AddHeader("Memory");
        AddRow("VRAM", FormatBytes(gpu.AdapterRamBytes));
        AddRowIf("DAC Type", gpu.AdapterDacType);

        AddHeader("Driver");
        AddRow("Version", gpu.DriverVersion);
        AddRowIf("Date", gpu.DriverDate);

        AddHeader("Display");
        if (gpu.CurrentHorizontalResolution > 0 && gpu.CurrentVerticalResolution > 0)
        {
            AddRow("Resolution", $"{gpu.CurrentHorizontalResolution} x {gpu.CurrentVerticalResolution}");
        }
        else
        {
            AddRow("Resolution", (string?)null);
        }
        AddRowIf("Refresh Rate", gpu.RefreshRateHz > 0 ? $"{gpu.RefreshRateHz} Hz" : null);

        AddHeader("Status");
        AddRow("Data Source", "Startup cache");

        ResolveIcon(gpu.Vendor, gpu.Name);
        AppDiagnostics.Log($"[GpuDetailVM] After populate: Title={Title}, Subtitle={Subtitle}, SpecsCount={Specs.Count}");
        SetLoadingComplete();
    }
}
