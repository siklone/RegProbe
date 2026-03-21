using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Diagnostics;
using BaseViewModel = WindowsOptimizer.App.ViewModels.Base.HardwareDetailViewModelBase;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public sealed class StorageDetailViewModel : BaseViewModel
{
    public override HardwareType HardwareType => HardwareType.Storage;

    public StorageDetailViewModel()
    {
        Title = "Storage";
        Subtitle = "Drives";
    }

    public override void LoadFromCache()
    {
        var storage = Cache.Get<StorageHardwareData>("Storage");
        AppDiagnostics.Log($"[StorageDetailVM] Cache raw: DeviceCount={storage?.DeviceCount.ToString() ?? "<null>"}, TotalSizeBytes={storage?.TotalSizeBytes.ToString() ?? "<null>"}, PrimaryModel={storage?.PrimaryModel ?? "<null>"}");
        // Treat null or effectively-empty cache entries as a miss so the
        // fallback preload can run and populate real data.
        if (storage == null || (storage.DeviceCount <= 0 && storage.TotalSizeBytes <= 0 && string.IsNullOrWhiteSpace(storage.PrimaryModel)))
        {
            Title = "Storage";
            Subtitle = "No data available";
            AddRow("Error", "Storage data not loaded");
            AppDiagnostics.Log("[StorageDetailVM] Cache miss or trivial entry");
            SetLoadingComplete();
            return;
        }

        // Mark that we successfully loaded from cache so the base fallback won't run
        WasCacheHit = true;
        AppDiagnostics.Log("[StorageDetailVM] Cache hit — populating properties from cache");

        Title = "Storage";
        Subtitle = storage.DeviceCount > 0 ? $"{storage.DeviceCount} drive(s)" : "Drives";

        ClearSpecs();

        AddHeader("Overview");
        AddRow("Total Drives", storage.DeviceCount > 0 ? storage.DeviceCount.ToString() : null);
        AddRow("Total Capacity", FormatBytes(storage.TotalSizeBytes));

        AddHeader("Primary Drive");
        AddRowIf("Model", storage.PrimaryModel);
        AddRowIf("Interface", storage.PrimaryInterface);

        AddHeader("Status");
        AddRow("Tip", "Use Hardware Details for per-drive details");

        ResolveIcon(null, storage.PrimaryModel);
        AppDiagnostics.Log($"[StorageDetailVM] After populate: Title={Title}, Subtitle={Subtitle}, SpecsCount={Specs.Count}");
        SetLoadingComplete();
    }
}
