using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using RegProbe.App.HardwareDb;
using RegProbe.App.Services.Hardware;
using RegProbe.App.Services.OsDetection;
using RegProbe.App.Diagnostics;
using BaseViewModel = RegProbe.App.ViewModels.Base.HardwareDetailViewModelBase;

namespace RegProbe.App.Services;

public static class HardwareDetailWindowFactory
{
    public static void OpenDetailWindow(HardwareType type, IMetricCacheService cache, Window owner)
    {
        try
        {
            var viewModel = CreateViewModel(type, cache);
            if (viewModel == null)
            {
                Debug.WriteLine($"[HardwareDetailWindowFactory] No ViewModel for type: {type}");
                return;
            }

            // Diagnostic: check both legacy and current cache keys before loading view model
            var keysToCheck = new[]
            {
                "hardware.os", "hardware.cpu", "hardware.gpu", "hardware.motherboard", "hardware.memory", "hardware.storage",
                CacheKeys.Os, CacheKeys.Cpu, CacheKeys.Gpu, CacheKeys.Motherboard, CacheKeys.Memory, CacheKeys.Storage
            };

            foreach (var key in keysToCheck)
            {
                var hit = cache.TryGet<object>(key, out var obj);
                Debug.WriteLine($"[CACHE DEBUG] {key} = {(hit ? obj?.GetType().Name : "MISS")}");
            }

            // Use the public Load() entry point which attempts a cache read and runs
            // the non-blocking fallback preload when necessary. This avoids blocking
            // the UI thread from the factory while keeping behavior consistent.
            viewModel.Load();

            var window = new Views.HardwareDetailWindow
            {
                DataContext = viewModel,
                Owner = owner,
                WindowStartupLocation = owner != null
                    ? WindowStartupLocation.CenterOwner
                    : WindowStartupLocation.CenterScreen
            };

            // Log runtime DataContext details for debugging binding issues
            try
            {
                var dc = window.DataContext;
                AppDiagnostics.Log($"[WindowOpen] DataContext type: {dc?.GetType().Name}, SpecsCollection count: {(dc as dynamic)?.SpecsCollection?.Count ?? -1}, Specs count: {(dc as dynamic)?.Specs?.Count ?? -1}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HardwareDetailWindowFactory] DataContext log failed: {ex.Message}");
            }

            window.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HardwareDetailWindowFactory] Failed to open window: {ex.Message}");
        }
    }

    private static BaseViewModel? CreateViewModel(HardwareType type, IMetricCacheService cache) => type switch
    {
        HardwareType.Os => new OsDetailViewModel(cache),
        HardwareType.Cpu => new CpuDetailViewModel(cache),
        HardwareType.Gpu => new GpuDetailViewModel(cache),
        HardwareType.Motherboard => new MotherboardDetailViewModel(cache),
        HardwareType.Memory => new MemoryDetailViewModel(cache),
        HardwareType.Storage => new StorageDetailViewModel(cache),
        HardwareType.Display => new DisplayDetailViewModel(cache),
        HardwareType.Network => new NetworkDetailViewModel(cache),
        HardwareType.Usb => new UsbDetailViewModel(cache),
        HardwareType.Audio => new AudioDetailViewModel(cache),
        _ => null
    };
}

public sealed class OsDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Os;

    public OsDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "Operating System";
        Subtitle = "Windows";
    }

    public override void LoadFromCache()
    {
        var os = _cache.Get<OsInfo>(CacheKeys.Os);
        if (os == null)
        {
            AddRow("Status", "Cache miss");
            SetLoadingComplete();
            return;
        }

        Title = "Operating System";
        Subtitle = os.NormalizedName ?? os.Caption ?? "Windows";

        ClearSpecs();
        AddHeader("System");
        AddRow("Caption", os.Caption);
        AddRow("Version", os.Version);
        AddRow("Build", os.BuildNumberRaw > 0 ? os.BuildNumberRaw.ToString() : os.BuildNumber);
        AddRowIf("UBR", os.Ubr > 0 ? os.Ubr.ToString() : null);
        AddRow("Edition", os.Edition);
        AddRow("Architecture", os.Architecture);
        AddRow("Display Version", os.DisplayVersion);

        AddHeader("Security");
        AddRow("BIOS Mode", os.BiosMode);
        AddRow("Secure Boot", os.SecureBootEnabled?.ToString() ?? "Unknown");
        AddRow("Activation", os.ActivationStatus);

        AddHeader("Runtime");
        AddRow("Last Boot", os.LastBootTime?.ToString("yyyy-MM-dd HH:mm"));
        AddRow("Uptime", os.UptimeFormatted);
        AddRow("Install Date", os.InstallDate?.ToString("yyyy-MM-dd"));

        AddHeader("Registration");
        AddRow("Registered User", os.RegisteredUser);
        AddRow("Organization", os.Organization);

        AddHeader("Diagnostics");
        AddRow("Source", os.Source);

        ResolveIcon("Microsoft", os.BuildNumberRaw > 0 ? os.BuildNumberRaw.ToString() : null);
        // Mark that we successfully loaded from cache so the base fallback won't run
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class CpuDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Cpu;

    public CpuDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "CPU";
        Subtitle = "Processor";
    }

    public override void LoadFromCache()
    {
        var cpu = _cache.Get<CpuHardwareData>(CacheKeys.Cpu);
        if (cpu == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = cpu.Name ?? "CPU";
        Subtitle = cpu.Manufacturer ?? "Unknown";

        ClearSpecs();
        AddHeader("Identification");
        AddRow("Name", cpu.Name);
        AddRow("Manufacturer", cpu.Manufacturer);
        AddRowIf("Architecture", cpu.Architecture);

        AddHeader("Topology");
        AddRowIf("Cores", cpu.Cores > 0 ? cpu.Cores.ToString() : null);
        AddRowIf("Threads", cpu.Threads > 0 ? cpu.Threads.ToString() : null);

        AddHeader("Frequency");
        AddRow("Max Clock", FormatHz(cpu.MaxClockMhz));

        AddHeader("Cache");
        if (cpu.L1CacheKB > 0) AddRow("L1", $"{cpu.L1CacheKB} KB");
        if (cpu.L2CacheKB > 0) AddRow("L2", $"{cpu.L2CacheKB} KB");
        if (cpu.L3CacheKB > 0) AddRow("L3", $"{cpu.L3CacheKB} KB");

        ResolveIcon(cpu.Manufacturer, cpu.Name);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class GpuDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Gpu;

    public GpuDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "GPU";
        Subtitle = "Graphics";
    }

    public override void LoadFromCache()
    {
        var gpu = _cache.Get<GpuHardwareData>(CacheKeys.Gpu);
        if (gpu == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = gpu.Name ?? "GPU";
        Subtitle = gpu.Vendor ?? "Unknown";

        ClearSpecs();
        AddHeader("Identification");
        AddRow("Name", gpu.Name);
        AddRow("Vendor", gpu.Vendor);
        AddRowIf("Processor", gpu.VideoProcessor);

        AddHeader("Memory");
        AddRow("VRAM", FormatBytes(gpu.AdapterRamBytes));

        AddHeader("Driver");
        AddRowIf("Version", gpu.DriverVersion);
        AddRowIf("Date", gpu.DriverDate);
        AddRowIf("Video Mode", gpu.VideoModeDescription);

        AddHeader("Display");
        if (gpu.CurrentHorizontalResolution > 0 && gpu.CurrentVerticalResolution > 0)
            AddRow("Resolution", $"{gpu.CurrentHorizontalResolution} x {gpu.CurrentVerticalResolution}");
        AddRowIf("Refresh Rate", gpu.RefreshRateHz > 0 ? $"{gpu.RefreshRateHz} Hz" : null);

        ResolveIcon(gpu.Vendor, gpu.Name);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class MotherboardDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Motherboard;

    public MotherboardDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "Motherboard";
        Subtitle = "Mainboard";
    }

    public override void LoadFromCache()
    {
        var mb = _cache.Get<MotherboardInfo>(CacheKeys.Motherboard);
        if (mb == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = mb.DisplayName ?? "Motherboard";
        Subtitle = mb.Manufacturer ?? "Unknown";

        ClearSpecs();
        AddHeader("Board");
        AddRow("Manufacturer", mb.Manufacturer);
        AddRow("Product", mb.Product);
        AddRowIf("Version", mb.Version);
        AddRowIf("Serial", mb.SerialNumber);
        AddRowIf("Chassis", mb.ChassisType);
        AddRowIf("Asset Tag", mb.AssetTag);

        AddHeader("BIOS");
        AddRow("Vendor", mb.BiosVendor);
        AddRow("Version", mb.BiosVersion);
        AddRowIf("Release Date", mb.BiosReleaseDate?.ToString("yyyy-MM-dd"));
        AddRow("Mode", mb.BiosMode);
        AddRow("Secure Boot", mb.SecureBootEnabled?.ToString() ?? "Unknown");

        AddHeader("Slots");
        AddRowIf("PCIe x16", mb.PcieX16Slots > 0 ? mb.PcieX16Slots.ToString() : null);
        AddRowIf("PCIe x4", mb.PcieX4Slots > 0 ? mb.PcieX4Slots.ToString() : null);
        AddRowIf("M.2", mb.M2Slots > 0 ? mb.M2Slots.ToString() : null);
        AddRowIf("DIMM", mb.DimmSlots > 0 ? mb.DimmSlots.ToString() : null);
        AddRowIf("SATA", mb.SataPorts > 0 ? mb.SataPorts.ToString() : null);

        AddHeader("Memory Support");
        AddRowIf("Max RAM", mb.MaxRamGb > 0 ? $"{mb.MaxRamGb} GB" : null);
        AddRowIf("Type", mb.MemoryType);
        AddRowIf("Max Speed", mb.MaxMemorySpeedMhz > 0 ? $"{mb.MaxMemorySpeedMhz} MHz" : null);
        AddRowIf("Slots Total", mb.DimmSlotsTotal > 0 ? mb.DimmSlotsTotal.ToString() : null);
        AddRowIf("Slots Used", mb.DimmSlotsUsed > 0 ? mb.DimmSlotsUsed.ToString() : null);

        ResolveIcon(mb.Manufacturer, mb.Product);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class MemoryDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Memory;

    public MemoryDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "Memory";
        Subtitle = "RAM";
    }

    public override void LoadFromCache()
    {
        var mem = _cache.Get<MemoryHardwareData>(CacheKeys.Memory);
        if (mem == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = "Memory";
        Subtitle = mem.ModuleCount > 0 ? $"{mem.ModuleCount} module(s)" : "RAM";

        ClearSpecs();
        AddHeader("Capacity");
        AddRow("Total", FormatBytes(mem.TotalBytes));
        AddRowIf("Modules", mem.ModuleCount > 0 ? mem.ModuleCount.ToString() : null);

        AddHeader("Speed & Type");
        AddRow("Type", mem.MemoryType);
        AddRowIf("Speed", mem.SpeedMhz > 0 ? $"{mem.SpeedMhz} MHz" : null);

        AddHeader("Primary Module");
        AddRowIf("Manufacturer", mem.PrimaryManufacturer);
        AddRowIf("Model", mem.PrimaryModel);

        ResolveIcon(mem.PrimaryManufacturer, mem.PrimaryModel);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class StorageDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Storage;

    public StorageDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "Storage";
        Subtitle = "Drives";
    }

    public override void LoadFromCache()
    {
        var storage = _cache.Get<StorageHardwareData>(CacheKeys.Storage);
        if (storage == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = "Storage";
        Subtitle = storage.DeviceCount > 0 ? $"{storage.DeviceCount} drive(s)" : "Drives";

        ClearSpecs();
        AddHeader("Overview");
        AddRowIf("Total Drives", storage.DeviceCount > 0 ? storage.DeviceCount.ToString() : null);
        AddRow("Total Capacity", FormatBytes(storage.TotalSizeBytes));

        AddHeader("Primary Drive");
        AddRowIf("Model", storage.PrimaryModel);
        AddRowIf("Interface", storage.PrimaryInterface);

        ResolveIcon(null, storage.PrimaryModel);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class DisplayDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Display;

    public DisplayDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "Displays";
        Subtitle = "Monitors";
    }

    public override void LoadFromCache()
    {
        var display = _cache.Get<DisplayHardwareData>(CacheKeys.Display);
        if (display == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = "Displays";
        Subtitle = display.DisplayCount > 0 ? $"{display.DisplayCount} display(s)" : "Monitors";

        ClearSpecs();
        AddHeader("Primary Display");
        if (display.PrimaryWidth > 0 && display.PrimaryHeight > 0)
            AddRow("Resolution", $"{display.PrimaryWidth} x {display.PrimaryHeight}");
        AddRowIf("Refresh Rate", display.PrimaryRefreshRateHz > 0 ? $"{display.PrimaryRefreshRateHz} Hz" : null);
        AddRowIf("Monitor", display.PrimaryMonitorName);
        AddRowIf("Manufacturer", display.MonitorManufacturer);

        AddHeader("Desktop");
        AddRowIf("Display Count", display.DisplayCount > 0 ? display.DisplayCount.ToString() : null);

        ResolveIcon(display.MonitorManufacturer, display.PrimaryMonitorName);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class NetworkDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Network;

    public NetworkDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "Network";
        Subtitle = "Adapters";
    }

    public override void LoadFromCache()
    {
        var net = _cache.Get<NetworkHardwareData>(CacheKeys.Network);
        if (net == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = "Network";
        Subtitle = net.AdapterCount > 0 ? $"{net.AdapterCount} adapter(s)" : "Adapters";

        ClearSpecs();
        AddHeader("Primary Adapter");
        AddRow("Name", net.PrimaryAdapterName);
        AddRowIf("Description", net.PrimaryAdapterDescription);
        AddRowIf("Type", net.PrimaryAdapterType);
        AddRowIf("MAC", net.PrimaryMacAddress);
        AddRowIf("IPv4", net.PrimaryIpv4);
        AddRowIf("IPv6", net.PrimaryIpv6);
        AddRowIf("Speed", net.PrimaryLinkSpeed);

        AddHeader("Overview");
        AddRowIf("Total Adapters", net.AdapterCount > 0 ? net.AdapterCount.ToString() : null);
        AddRowIf("Connected", net.AdapterUpCount > 0 ? net.AdapterUpCount.ToString() : null);
        AddRowIf("Wireless", net.WirelessAdapterCount > 0 ? net.WirelessAdapterCount.ToString() : null);

        ResolveIcon(null, net.PrimaryAdapterDescription);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class UsbDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Usb;

    public UsbDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "USB";
        Subtitle = "Devices";
    }

    public override void LoadFromCache()
    {
        var usb = _cache.Get<UsbHardwareData>(CacheKeys.Usb);
        if (usb == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = "USB";
        Subtitle = usb.UsbDeviceCount > 0 ? $"{usb.UsbDeviceCount} device(s)" : "USB";

        ClearSpecs();
        AddHeader("Overview");
        AddRowIf("Controllers", usb.UsbControllerCount > 0 ? usb.UsbControllerCount.ToString() : null);
        AddRowIf("Devices", usb.UsbDeviceCount > 0 ? usb.UsbDeviceCount.ToString() : null);
        AddRowIf("Removable Drives", usb.RemovableDriveCount > 0 ? usb.RemovableDriveCount.ToString() : null);

        AddHeader("Primary Controller");
        AddRowIf("Name", usb.PrimaryControllerName);
        AddRowIf("Device", usb.PrimaryUsbDeviceName);
        AddRowIf("Vendor ID", usb.PrimaryVendorId);
        AddRowIf("Product ID", usb.PrimaryProductId);

        ResolveIcon(null, usb.PrimaryControllerName);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}

public sealed class AudioDetailViewModel : BaseViewModel
{
    private readonly IMetricCacheService _cache;
    public override HardwareType HardwareType => HardwareType.Audio;

    public AudioDetailViewModel(IMetricCacheService cache)
    {
        _cache = cache;
        Title = "Audio";
        Subtitle = "Sound Devices";
    }

    public override void LoadFromCache()
    {
        var audio = _cache.Get<AudioHardwareData>("Audio");
        if (audio == null) { AddRow("Status", "Cache miss"); SetLoadingComplete(); return; }

        Title = "Audio";
        Subtitle = audio.PrimaryDeviceName ?? "Sound Devices";

        ClearSpecs();
        AddHeader("Overview");
        AddRow("Devices", audio.DeviceCount.ToString());
        AddRowIf("Primary Device", audio.PrimaryDeviceName);
        AddRowIf("Manufacturer", audio.PrimaryManufacturer);
        AddRowIf("Status", audio.PrimaryStatus);

        if (audio.AllDevices.Count > 1)
        {
            AddHeader("All Devices");
            foreach (var device in audio.AllDevices)
            {
                AddRow("Device", device);
            }
        }

        ResolveIcon(audio.PrimaryManufacturer, audio.PrimaryDeviceName);
        WasCacheHit = true;
        SetLoadingComplete();
    }
}
