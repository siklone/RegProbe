using OpenTraceProject.App.HardwareDb;
using OpenTraceProject.App.Services;
using OpenTraceProject.App.Diagnostics;
using BaseViewModel = OpenTraceProject.App.ViewModels.Base.HardwareDetailViewModelBase;

namespace OpenTraceProject.App.ViewModels.Hardware;

public sealed class MotherboardDetailViewModel : BaseViewModel
{
    public override HardwareType HardwareType => HardwareType.Motherboard;

    public MotherboardDetailViewModel()
    {
        Title = "Motherboard";
        Subtitle = "Mainboard";
    }

    public override void LoadFromCache()
    {
        var mb = Cache.Get<MotherboardHardwareData>("Motherboard");
        AppDiagnostics.Log($"[MotherboardDetailVM] Cache raw: Manufacturer={mb?.Manufacturer ?? "<null>"}, Model={mb?.Model ?? "<null>"}, Product={mb?.Product ?? "<null>"}, Serial={mb?.Serial ?? "<null>"}, Version={mb?.Version ?? "<null>"}");
        // Consider null or minimal motherboard entries as a cache miss so the
        // fallback can run and gather full details.
        if (mb == null || (string.IsNullOrWhiteSpace(mb.Manufacturer) && string.IsNullOrWhiteSpace(mb.Model) && string.IsNullOrWhiteSpace(mb.Product)))
        {
            Title = "Motherboard";
            Subtitle = "No data available";
            AddRow("Error", "Motherboard data not loaded");
            AppDiagnostics.Log("[MotherboardDetailVM] Cache miss or trivial entry");
            SetLoadingComplete();
            return;
        }

        // Mark that we successfully loaded from cache so the base fallback won't run
        WasCacheHit = true;
        AppDiagnostics.Log("[MotherboardDetailVM] Cache hit â€” populating properties from cache");

        var manufacturer = mb.Manufacturer ?? "Unknown";
        var model = mb.Model ?? mb.Product ?? "Unknown";
        Title = $"{manufacturer} {model}".Trim();
        Subtitle = manufacturer;

        ClearSpecs();

        AddHeader("Board");
        AddRow("Manufacturer", mb.Manufacturer);
        AddRow("Model", mb.Model);
        AddRowIf("Product", mb.Product);
        AddRowIf("Version", mb.Version);
        AddRowIf("Serial Number", mb.Serial);
        AddRowIf("Chassis", mb.ChassisType);
        AddRowIf("Asset Tag", mb.AssetTag);

        AddHeader("BIOS");
        AddRow("Vendor", mb.BiosVendor);
        AddRow("Version", mb.BiosVersion);
        AddRowIf("Date", mb.BiosDate);

        AddHeader("Chipset");
        AddRowIf("Chipset", mb.Chipset);

        AddHeader("Slots");
        AddRowIf("System Slots", mb.SystemSlotCount > 0 ? mb.SystemSlotCount.ToString() : null);

        AddHeader("Memory Support");
        AddRow("Info", "See Memory tab for details");

        ResolveIcon(mb.Manufacturer, mb.Product ?? mb.Model);
        AppDiagnostics.Log($"[MotherboardDetailVM] After populate: Title={Title}, Subtitle={Subtitle}, SpecsCount={Specs.Count}");
        SetLoadingComplete();
    }
}
