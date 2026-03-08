using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Diagnostics;
using BaseViewModel = WindowsOptimizer.App.ViewModels.Base.HardwareDetailViewModelBase;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public sealed class CpuDetailViewModel : BaseViewModel
{
    public override HardwareType HardwareType => HardwareType.Cpu;

    public CpuDetailViewModel()
    {
        Title = "CPU";
        Subtitle = "Processor";
    }

    public override void LoadFromCache()
    {
        var cpu = Cache.Get<CpuHardwareData>("CPU");
        AppDiagnostics.Log($"[CpuDetailVM] Cache raw: Name={cpu?.Name ?? "<null>"}, Manufacturer={cpu?.Manufacturer ?? "<null>"}, Cores={cpu?.Cores.ToString() ?? "<null>"}");
        // Treat null or minimal CPU entries as cache miss to allow fallback.
        if (cpu == null || (string.IsNullOrWhiteSpace(cpu.Name) && string.IsNullOrWhiteSpace(cpu.Manufacturer) && cpu.Cores <= 0))
        {
            Title = "CPU";
            Subtitle = "No data available";
            AddRow("Error", "CPU data not loaded");
            AppDiagnostics.Log("[CpuDetailVM] Cache miss or trivial entry");
            SetLoadingComplete();
            return;
        }

            // Mark that we successfully loaded from cache so the base fallback won't run
        WasCacheHit = true;
        AppDiagnostics.Log("[CpuDetailVM] Cache hit — populating properties from cache");

            Title = cpu.Name ?? cpu.Manufacturer ?? "CPU";
            Subtitle = cpu.Manufacturer ?? "Unknown";

        ClearSpecs();

        AddHeader("Identification");
        AddRow("Name", cpu.Name);
        AddRow("Manufacturer", cpu.Manufacturer);
        AddRow("Architecture", cpu.Architecture);
        AddRowIf("Description", cpu.Description);

        AddHeader("Topology");
        AddRow("Cores", cpu.Cores > 0 ? cpu.Cores.ToString() : null);
        AddRow("Threads", cpu.Threads > 0 ? cpu.Threads.ToString() : null);
        if (cpu.Cores > 0 && cpu.Threads > 0)
        {
            var threadsPerCore = cpu.Threads / cpu.Cores;
            AddRowIf("Threads/Core", threadsPerCore > 1 ? threadsPerCore.ToString() : null);
        }

        AddHeader("Frequency");
        AddRow("Max Clock", FormatHz(cpu.MaxClockMhz));

        AddHeader("Cache");
        AddRowIf("L2 Cache", cpu.L2CacheKB > 0 ? $"{cpu.L2CacheKB} KB" : null);
        AddRowIf("L3 Cache", cpu.L3CacheKB > 0 ? $"{cpu.L3CacheKB} KB" : null);
        if (cpu.L2CacheKB > 0 && cpu.Cores > 0)
        {
            var l2PerCore = cpu.L2CacheKB / cpu.Cores;
            AddRowIf("L2/Core", $"{l2PerCore} KB");
        }

        AddHeader("Performance");
        AddRow("Status", "Data from cache");

        ResolveIcon(cpu.Manufacturer, cpu.Name);
        AppDiagnostics.Log($"[CpuDetailVM] After populate: Title={Title}, Subtitle={Subtitle}, SpecsCount={Specs.Count}");
        SetLoadingComplete();
    }
}
