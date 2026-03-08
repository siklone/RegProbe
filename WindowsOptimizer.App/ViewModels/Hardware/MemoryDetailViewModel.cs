using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Diagnostics;
using BaseViewModel = WindowsOptimizer.App.ViewModels.Base.HardwareDetailViewModelBase;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public sealed class MemoryDetailViewModel : BaseViewModel
{
    public override HardwareType HardwareType => HardwareType.Memory;

    public MemoryDetailViewModel()
    {
        Title = "Memory";
        Subtitle = "RAM";
    }

    public override void LoadFromCache()
    {
        var memory = Cache.Get<MemoryHardwareData>("Memory");
        AppDiagnostics.Log($"[MemoryDetailVM] Cache raw: ModuleCount={memory?.ModuleCount.ToString() ?? "<null>"}, TotalBytes={memory?.TotalBytes.ToString() ?? "<null>"}, PrimaryModel={memory?.PrimaryModel ?? "<null>"}, PrimaryManufacturer={memory?.PrimaryManufacturer ?? "<null>"}");
        // Treat null or trivial memory entries as a miss so fallback can populate live values.
        if (memory == null || (memory.ModuleCount <= 0 && memory.TotalBytes <= 0 && string.IsNullOrWhiteSpace(memory.PrimaryModel)))
        {
            Title = "Memory";
            Subtitle = "No data available";
            AddRow("Error", "Memory data not loaded");
            AppDiagnostics.Log("[MemoryDetailVM] Cache miss or trivial entry");
            SetLoadingComplete();
            return;
        }

        // Mark that we successfully loaded from cache so the base fallback won't run
        WasCacheHit = true;
        AppDiagnostics.Log("[MemoryDetailVM] Cache hit — populating properties from cache");

        Title = "Memory";
        Subtitle = memory.ModuleCount > 0 ? $"{memory.ModuleCount} module(s)" : "RAM";

        ClearSpecs();

        AddHeader("Capacity");
        AddRow("Total", FormatBytes(memory.TotalBytes));
        AddRow("Modules", memory.ModuleCount > 0 ? memory.ModuleCount.ToString() : null);

        AddHeader("Speed & Type");
        AddRow("Type", memory.MemoryType);
        AddRow("Speed", memory.SpeedMhz > 0 ? $"{memory.SpeedMhz} MHz" : null);

        AddHeader("Primary Module");
        AddRowIf("Manufacturer", memory.PrimaryManufacturer);
        AddRowIf("Model", memory.PrimaryModel);

        AddHeader("Slots");
        AddRow("Status", "Per-slot details require WMI query");
        AddRow("Tip", "Use Monitor tab for live memory metrics");

        ResolveIcon(memory.PrimaryManufacturer, memory.PrimaryModel);
        AppDiagnostics.Log($"[MemoryDetailVM] After populate: Title={Title}, Subtitle={Subtitle}, SpecsCount={Specs.Count}");
        SetLoadingComplete();
    }
}
