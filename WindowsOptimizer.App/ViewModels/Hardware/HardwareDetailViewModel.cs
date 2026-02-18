using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public enum HardwareType
{
    Os,
    Cpu,
    Gpu,
    Motherboard,
    Memory,
    Storage,
    Displays,
    Network,
    Usb
}

public abstract class HardwareDetailViewModelBase : ViewModelBase, IDisposable
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _iconKey = string.Empty;
    private ImageSource _iconSource = HardwareIconResolver.ResolveIcon("display_generic", "display_generic");
    private bool _isLoading;
    private string _loadingStatus = "Loading system data...";
    private Func<HardwareDetailSnapshot, HardwareDetailPayload>? _collector;
    private bool _subscribedToUpdates;

    protected HardwareDetailViewModelBase(HardwareType hardwareType)
    {
        HardwareType = hardwareType;
        CloseCommand = new RelayCommand(parameter =>
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        });
    }

    public HardwareType HardwareType { get; }

    public string Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        protected set => SetProperty(ref _subtitle, value);
    }

    public ImageSource IconSource
    {
        get => _iconSource;
        protected set => SetProperty(ref _iconSource, value);
    }

    public string IconKey
    {
        get => _iconKey;
        protected set => SetProperty(ref _iconKey, value);
    }

    public ObservableCollection<KeyValuePair<string, string>> SpecsCollection { get; } = new();

    public ObservableCollection<int> SkeletonRows { get; } = new() { 1, 2, 3, 4, 5, 6 };

    public bool IsLoading
    {
        get => _isLoading;
        protected set => SetProperty(ref _isLoading, value);
    }

    public string LoadingStatus
    {
        get => _loadingStatus;
        protected set => SetProperty(ref _loadingStatus, value);
    }

    public ICommand CloseCommand { get; }

    protected void SetSpecs(IEnumerable<KeyValuePair<string, string>> specs)
    {
        SpecsCollection.Clear();
        foreach (var item in specs)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
            {
                continue;
            }

            SpecsCollection.Add(item);
        }
    }

    public virtual void Dispose()
    {
        if (_subscribedToUpdates)
        {
            HardwarePreloadService.Instance.SnapshotUpdated -= OnSnapshotUpdated;
            _subscribedToUpdates = false;
        }
    }

    protected void BeginLoad(HardwareDetailSnapshot? snapshot, Func<HardwareDetailSnapshot, HardwareDetailPayload> collector)
    {
        IsLoading = true;
        LoadingStatus = "Loading system data...";
        SetSpecs(new[]
        {
            new KeyValuePair<string, string>("Loading", "Collecting system info..."),
            new KeyValuePair<string, string>("Loading", "Collecting system info..."),
            new KeyValuePair<string, string>("Loading", "Collecting system info...")
        });

        _ = LoadAsync(snapshot, collector);
    }

    private void OnSnapshotUpdated(int tier)
    {
        if (_collector == null)
        {
            return;
        }

        var snapshot = HardwarePreloadService.Instance.GetSnapshot();
        var payload = _collector(snapshot);
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            Title = payload.Title;
            Subtitle = payload.Subtitle;
            IconKey = payload.IconKey;
            IconSource = HardwareIconResolver.ResolveIcon(payload.IconKey, payload.FallbackIconKey);
            SetSpecs(payload.Specs);
            IsLoading = tier < 3;
            LoadingStatus = tier switch
            {
                1 => "Collecting security metrics...",
                2 => "Collecting firmware and board data...",
                _ => "Ready"
            };
        });
    }

    private async Task LoadAsync(HardwareDetailSnapshot? snapshot, Func<HardwareDetailSnapshot, HardwareDetailPayload> collector)
    {
        try
        {
            _collector = collector;
            if (!_subscribedToUpdates)
            {
                HardwarePreloadService.Instance.SnapshotUpdated += OnSnapshotUpdated;
                _subscribedToUpdates = true;
            }

            var payload = await Task.Run(() =>
            {
                var cache = snapshot ?? HardwarePreloadService.Instance.GetSnapshot();
                return collector(cache);
            });

            Title = payload.Title;
            Subtitle = payload.Subtitle;
            IconKey = payload.IconKey;
            IconSource = HardwareIconResolver.ResolveIcon(payload.IconKey, payload.FallbackIconKey);
            SetSpecs(payload.Specs);
            IsLoading = HardwarePreloadService.Instance.CurrentTier < 3;
            LoadingStatus = HardwarePreloadService.Instance.CurrentTier switch
            {
                1 => "Collecting security metrics...",
                2 => "Collecting firmware and board data...",
                _ => "Ready"
            };
        }
        finally
        {
            if (HardwarePreloadService.Instance.CurrentTier >= 3)
            {
                IsLoading = false;
            }
        }
    }

    protected sealed record HardwareDetailPayload(
        string Title,
        string Subtitle,
        string IconKey,
        string FallbackIconKey,
        IEnumerable<KeyValuePair<string, string>> Specs);

    protected static T ResolveCached<T>(string key, T fallback) where T : class
    {
        return MetricCacheService.Instance.Get<T>(key) ?? fallback;
    }
}

public sealed class OsDetailVM : HardwareDetailViewModelBase
{
    public OsDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Os)
    {
        Title = "Operating System";
        Subtitle = "Loading system data...";
        IconKey = "os/windows10";
        IconSource = HardwareIconResolver.ResolveOsIcon("Windows 10");

        BeginLoad(snapshot, cache =>
        {
            var os = ResolveCached("OS", cache.Os);
            var osName = ValueOrUnknown(os.NormalizedName);
            var subtitle = ValueOrUnknown(os.ProductName);
            var normalized = !string.IsNullOrWhiteSpace(os.NormalizedName)
                ? os.NormalizedName
                : BuildNormalizedOsName(os.BuildNumber, os.Edition, os.DisplayVersion, os.ReleaseId);
            var iconKey = normalized.Contains("Windows 11", StringComparison.OrdinalIgnoreCase)
                ? "os/windows11"
                : "os/windows10";

            var specs = new[]
            {
                new KeyValuePair<string, string>("Edition", ValueOrUnknown(os.Edition)),
                new KeyValuePair<string, string>("Product Name", ValueOrUnknown(os.ProductName)),
                new KeyValuePair<string, string>("Normalized OS", ValueOrUnknown(normalized)),
                new KeyValuePair<string, string>("Display Version", ValueOrUnknown(os.DisplayVersion)),
                new KeyValuePair<string, string>("Release ID", ValueOrUnknown(os.ReleaseId)),
                new KeyValuePair<string, string>("UBR", os.Ubr > 0 ? os.Ubr.ToString() : "Unknown"),
                new KeyValuePair<string, string>("Install Type", ValueOrUnknown(os.InstallType)),
                new KeyValuePair<string, string>("Registered Owner", ValueOrUnknown(os.RegisteredOwner)),
                new KeyValuePair<string, string>("Registered Organization", ValueOrUnknown(os.RegisteredOrganization)),
                new KeyValuePair<string, string>("Windows Experience Index", ValueOrUnknown(os.WindowsExperienceIndex)),
                new KeyValuePair<string, string>("Last Boot Time", ValueOrUnknown(os.LastBootTime)),
                new KeyValuePair<string, string>("Uptime", ValueOrUnknown(os.Uptime)),
                new KeyValuePair<string, string>("BIOS Mode", ValueOrUnknown(os.BiosMode)),
                new KeyValuePair<string, string>("Secure Boot State", ValueOrUnknown(os.SecureBootState)),
                new KeyValuePair<string, string>("TPM Version", ValueOrUnknown(os.TpmVersion)),
                new KeyValuePair<string, string>("BitLocker Status", ValueOrUnknown(os.BitLockerStatus)),
                new KeyValuePair<string, string>("Device Guard", ValueOrUnknown(os.DeviceGuardStatus)),
                new KeyValuePair<string, string>("Credential Guard", ValueOrUnknown(os.CredentialGuardStatus)),
                new KeyValuePair<string, string>("Virtualization Enabled", ValueOrUnknown(os.VirtualizationEnabled)),
                new KeyValuePair<string, string>("Hyper-V Installed", ValueOrUnknown(os.HyperVInstalled)),
                new KeyValuePair<string, string>("Windows Defender Status", ValueOrUnknown(os.DefenderStatus)),
                new KeyValuePair<string, string>("Firewall Status", ValueOrUnknown(os.FirewallStatus)),
                new KeyValuePair<string, string>("Build", os.BuildNumber > 0 ? os.BuildNumber.ToString() : "Unknown"),
                new KeyValuePair<string, string>("Architecture", ValueOrUnknown(os.Architecture)),
                new KeyValuePair<string, string>("Username", ValueOrUnknown(os.Username)),
                new KeyValuePair<string, string>("Install Date", ValueOrUnknown(os.InstallDate)),
                new KeyValuePair<string, string>("Version", ValueOrUnknown(os.Version))
            };

            LogSpecSource("Edition", os.EditionSource);
            LogSpecSource("Product Name", os.ProductNameSource);
            LogSpecSource("Display Version", os.DisplayVersionSource);
            LogSpecSource("Release ID", os.ReleaseIdSource);
            LogSpecSource("Install Type", os.InstallTypeSource);
            LogSpecSource("Registered Owner", os.RegisteredOwnerSource);
            LogSpecSource("Registered Organization", os.RegisteredOrganizationSource);
            LogSpecSource("Windows Experience Index", os.WindowsExperienceIndexSource);
            LogSpecSource("Last Boot Time", os.LastBootTimeSource);
            LogSpecSource("Uptime", os.UptimeSource);
            LogSpecSource("BIOS Mode", os.BiosModeSource);
            LogSpecSource("Secure Boot State", os.SecureBootStateSource);
            LogSpecSource("TPM Version", os.TpmVersionSource);
            LogSpecSource("BitLocker Status", os.BitLockerStatusSource);
            LogSpecSource("Device Guard", os.DeviceGuardStatusSource);
            LogSpecSource("Credential Guard", os.CredentialGuardStatusSource);
            LogSpecSource("Virtualization Enabled", os.VirtualizationEnabledSource);
            LogSpecSource("Hyper-V Installed", os.HyperVInstalledSource);
            LogSpecSource("Windows Defender Status", os.DefenderStatusSource);
            LogSpecSource("Firewall Status", os.FirewallStatusSource);
            LogSpecSource("Build", os.BuildNumberSource);
            LogSpecSource("Architecture", os.ArchitectureSource);
            LogSpecSource("Username", os.UsernameSource);
            LogSpecSource("Install Date", os.InstallDateSource);
            LogSpecSource("Version", os.VersionSource);

            return new HardwareDetailPayload("Operating System", subtitle, iconKey, "os/windows10", specs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();

    private static void LogSpecSource(string name, string? source)
    {
        Debug.WriteLine($"[OS Detail] {name} source={ValueOrUnknown(source)}");
    }

    private static string BuildNormalizedOsName(int buildNumber, string? edition, string? displayVersion, string? releaseId)
    {
        var osBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var normalized = string.IsNullOrWhiteSpace(edition) ? osBase : $"{osBase} {edition}";
        var version = !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion : releaseId;
        return string.IsNullOrWhiteSpace(version) ? normalized : $"{normalized} ({version})";
    }
}

public sealed class CpuDetailVM : HardwareDetailViewModelBase
{
    public CpuDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Cpu)
    {
        Title = "CPU";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("cpu");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "cpu_generic");

        BeginLoad(snapshot, cache =>
        {
            var cpu = ResolveCached("CPU", cache.Cpu);
            var matchedCpu = HardwareKnowledgeDbService.Instance.MatchCpu(cpu.Name ?? string.Empty);
            var specs = new List<KeyValuePair<string, string>>
            {
                new("Name", ValueOrUnknown(cpu.Name)),
                new("Manufacturer", ValueOrUnknown(cpu.Manufacturer)),
                new("Architecture", ValueOrUnknown(cpu.Architecture)),
                new("Max Clock", cpu.MaxClockMhz > 0 ? $"{cpu.MaxClockMhz} MHz" : "Unknown")
            };

            if (cpu.Cores > 0) specs.Add(new("Cores", cpu.Cores.ToString()));
            if (cpu.Threads > 0) specs.Add(new("Threads", cpu.Threads.ToString()));
            if (!string.IsNullOrWhiteSpace(cpu.Description)) specs.Add(new("Description", cpu.Description));
            if (cpu.L2CacheKB > 0) specs.Add(new("L2 Cache", $"{cpu.L2CacheKB} KB"));
            if (cpu.L3CacheKB > 0) specs.Add(new("L3 Cache", $"{cpu.L3CacheKB} KB"));
            if (matchedCpu != null)
            {
                specs.Add(new("DB Match", $"{matchedCpu.Brand} {matchedCpu.ModelName}".Trim()));
                specs.Add(new("Performance Tier", matchedCpu.GetTier()));
            }

            string iconKey;
            if (matchedCpu != null)
            {
                iconKey = matchedCpu.IconKey ?? HardwareIconResolver.GetFallbackKey("cpu");
                // Normalize any legacy per-series Ryzen keys to the new universal key
                if (iconKey.IndexOf("ryzen", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    iconKey = "amd_ryzen_cpu";
                }
            }
            else
            {
                iconKey = HardwareIconResolver.ResolveIconKey("cpu", cpu.Name, HardwareIconResolver.GetFallbackKey("cpu"));
            }

            return new HardwareDetailPayload(
                ValueOrUnknown(cpu.Name),
                ValueOrUnknown(cpu.Manufacturer),
                iconKey,
                HardwareIconResolver.GetFallbackKey("cpu"),
                specs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
}

public sealed class GpuDetailVM : HardwareDetailViewModelBase
{
    public GpuDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Gpu)
    {
        Title = "GPU";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("gpu");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "gpu_generic");

        BeginLoad(snapshot, cache =>
        {
            var gpu = ResolveCached("GPU", cache.Gpu);
            var matchedGpu = HardwareKnowledgeDbService.Instance.MatchGpu(gpu.Name ?? string.Empty);
            var specs = new List<KeyValuePair<string, string>>
            {
                new("Name", ValueOrUnknown(gpu.Name)),
                new("Vendor", ValueOrUnknown(gpu.Vendor)),
                new("Vendor ID", ValueOrUnknown(gpu.VendorId)),
                new("Device ID", ValueOrUnknown(gpu.DeviceId)),
                new("Driver Version", ValueOrUnknown(gpu.DriverVersion)),
                new("Memory", gpu.AdapterRamBytes > 0 ? FormatBytes(gpu.AdapterRamBytes) : "Unknown")
            };

            if (!string.IsNullOrWhiteSpace(gpu.DriverDate)) specs.Add(new("Driver Date", ValueOrUnknown(gpu.DriverDate)));
            if (gpu.CurrentHorizontalResolution > 0 && gpu.CurrentVerticalResolution > 0) specs.Add(new("Current Resolution", $"{gpu.CurrentHorizontalResolution}x{gpu.CurrentVerticalResolution}"));
            if (gpu.RefreshRateHz > 0) specs.Add(new("Refresh Rate", $"{gpu.RefreshRateHz} Hz"));
            if (!string.IsNullOrWhiteSpace(gpu.VideoProcessor)) specs.Add(new("GPU Processor", ValueOrUnknown(gpu.VideoProcessor)));
            if (!string.IsNullOrWhiteSpace(gpu.AdapterDacType)) specs.Add(new("Adapter DAC", ValueOrUnknown(gpu.AdapterDacType)));

            if (matchedGpu != null)
            {
                specs.Add(new("DB Match", $"{matchedGpu.Brand} {matchedGpu.ModelName}".Trim()));
                specs.Add(new("Performance Tier", matchedGpu.GetTier()));
            }

            return new HardwareDetailPayload(
                ValueOrUnknown(gpu.Name),
                ValueOrUnknown(gpu.Vendor),
                matchedGpu != null
                    ? matchedGpu.IconKey
                    : HardwareIconResolver.GetFallbackKey("gpu"),
                HardwareIconResolver.GetFallbackKey("gpu"),
                specs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();

    private static string FormatBytes(long bytes)
    {
        const long giga = 1024L * 1024L * 1024L;
        return bytes <= 0 ? "Unknown" : $"{bytes / (double)giga:F1} GB";
    }
}

public sealed class MotherboardDetailVM : HardwareDetailViewModelBase
{
    public MotherboardDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Motherboard)
    {
        Title = "Motherboard";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("chipset");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "chipset_generic");

        BeginLoad(snapshot, cache =>
        {
            var board = ResolveCached("Motherboard", cache.Motherboard);
            var boardQuery = $"{board.Manufacturer} {board.Model} {board.Product}";
            var matchedBoard = HardwareKnowledgeDbService.Instance.MatchMotherboard(boardQuery);
            var matchedChipset = HardwareKnowledgeDbService.Instance.MatchChipset(board.Chipset ?? string.Empty);
            var specs = new[]
            {
                new KeyValuePair<string, string>("Manufacturer", ValueOrUnknown(board.Manufacturer)),
                new KeyValuePair<string, string>("Model", ValueOrUnknown(board.Model)),
                new KeyValuePair<string, string>("Product", ValueOrUnknown(board.Product)),
                new KeyValuePair<string, string>("Serial", ValueOrUnknown(board.Serial)),
                new KeyValuePair<string, string>("Version", ValueOrUnknown(board.Version)),
                new KeyValuePair<string, string>("BIOS Vendor", ValueOrUnknown(board.BiosVendor)),
                new KeyValuePair<string, string>("BIOS Version", ValueOrUnknown(board.BiosVersion)),
                new KeyValuePair<string, string>("BIOS Date", ValueOrUnknown(board.BiosDate)),
                new KeyValuePair<string, string>("Chipset", ValueOrUnknown(board.Chipset)),
                new KeyValuePair<string, string>("System Slots", board.SystemSlotCount > 0 ? board.SystemSlotCount.ToString() : "Unknown")
            };

            var enrichedSpecs = new List<KeyValuePair<string, string>>(specs);
            if (matchedBoard != null)
            {
                enrichedSpecs.Add(new("DB Match", $"{matchedBoard.Brand} {matchedBoard.ModelName}".Trim()));
            }

            if (matchedChipset != null)
            {
                enrichedSpecs.Add(new("Chipset Family", $"{matchedChipset.Brand} {matchedChipset.ModelName}".Trim()));
            }

            var iconKey = matchedBoard?.IconKey ?? HardwareIconResolver.GetFallbackKey("chipset");

            return new HardwareDetailPayload(
                BuildTitle(board),
                ValueOrUnknown(board.Product),
                iconKey,
                HardwareIconResolver.GetFallbackKey("chipset"),
                enrichedSpecs);
        });
    }

    private static string BuildTitle(MotherboardHardwareData board)
    {
        var manufacturer = ValueOrUnknown(board.Manufacturer);
        var model = ValueOrUnknown(board.Model);
        return $"{manufacturer} {model}".Trim();
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
}

public sealed class MemoryDetailVM : HardwareDetailViewModelBase
{
    public MemoryDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Memory)
    {
        Title = "Memory";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("memory");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "memory_generic");

        BeginLoad(snapshot, cache =>
        {
            var memory = ResolveCached("Memory", cache.Memory);
            var memoryQuery = $"{memory.PrimaryManufacturer} {memory.PrimaryModel} {memory.MemoryType} {memory.SpeedMhz}";
            var matchedMemory = HardwareKnowledgeDbService.Instance.MatchMemory(memoryQuery);
            var matchedChip = HardwareKnowledgeDbService.Instance.MatchMemoryChip(memory.PrimaryModel ?? string.Empty);
            var specs = new[]
            {
                new KeyValuePair<string, string>("Total", memory.TotalBytes > 0 ? FormatBytes(memory.TotalBytes) : "Unknown"),
                new KeyValuePair<string, string>("Modules", memory.ModuleCount > 0 ? memory.ModuleCount.ToString() : "Unknown"),
                new KeyValuePair<string, string>("Type", ValueOrUnknown(memory.MemoryType)),
                new KeyValuePair<string, string>("Speed", memory.SpeedMhz > 0 ? $"{memory.SpeedMhz} MHz" : "Unknown"),
                new KeyValuePair<string, string>("Primary Manufacturer", ValueOrUnknown(memory.PrimaryManufacturer)),
                new KeyValuePair<string, string>("Primary Model", ValueOrUnknown(memory.PrimaryModel))
            };

            var enrichedSpecs = new List<KeyValuePair<string, string>>(specs);
            if (matchedMemory != null)
            {
                enrichedSpecs.Add(new("DB Match", $"{matchedMemory.Brand} {matchedMemory.ModelName}".Trim()));
            }

            if (matchedChip != null)
            {
                enrichedSpecs.Add(new("Memory IC", $"{matchedChip.Brand} {matchedChip.ModelName}".Trim()));
            }

            return new HardwareDetailPayload(
                "Memory",
                memory.ModuleCount > 0 ? $"{memory.ModuleCount} module(s)" : "No module data",
                matchedMemory != null
                    ? matchedMemory.IconKey
                    : HardwareIconResolver.GetFallbackKey("memory"),
                HardwareIconResolver.GetFallbackKey("memory"),
                enrichedSpecs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();

    private static string FormatBytes(long bytes)
    {
        const long giga = 1024L * 1024L * 1024L;
        return bytes <= 0 ? "Unknown" : $"{bytes / (double)giga:F1} GB";
    }
}

public sealed class StorageDetailVM : HardwareDetailViewModelBase
{
    public StorageDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Storage)
    {
        Title = "Storage";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("storage");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "storage_generic");

        BeginLoad(snapshot, cache =>
        {
            var storage = ResolveCached("Storage", cache.Storage);
            var matchedStorage = HardwareKnowledgeDbService.Instance.MatchStorage(storage.PrimaryModel ?? string.Empty);
            var specs = new[]
            {
                new KeyValuePair<string, string>("Drives", storage.DeviceCount > 0 ? storage.DeviceCount.ToString() : "Unknown"),
                new KeyValuePair<string, string>("Total Capacity", storage.TotalSizeBytes > 0 ? FormatBytes(storage.TotalSizeBytes) : "Unknown"),
                new KeyValuePair<string, string>("Primary Model", ValueOrUnknown(storage.PrimaryModel)),
                new KeyValuePair<string, string>("Primary Interface", ValueOrUnknown(storage.PrimaryInterface))
            };

            var enrichedSpecs = new List<KeyValuePair<string, string>>(specs);
            if (matchedStorage != null)
            {
                enrichedSpecs.Add(new("DB Match", $"{matchedStorage.Brand} {matchedStorage.ModelName}".Trim()));
            }

            return new HardwareDetailPayload(
                "Storage",
                storage.DeviceCount > 0 ? $"{storage.DeviceCount} drive(s)" : "No drive data",
                matchedStorage != null
                    ? matchedStorage.IconKey
                    : HardwareIconResolver.GetFallbackKey("storage"),
                HardwareIconResolver.GetFallbackKey("storage"),
                enrichedSpecs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();

    private static string FormatBytes(long bytes)
    {
        const long giga = 1024L * 1024L * 1024L;
        const long tera = giga * 1024L;
        if (bytes <= 0)
        {
            return "Unknown";
        }

        return bytes >= tera
            ? $"{bytes / (double)tera:F2} TB"
            : $"{bytes / (double)giga:F1} GB";
    }
}

public sealed class DisplaysDetailVM : HardwareDetailViewModelBase
{
    public DisplaysDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Displays)
    {
        Title = "Displays";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("display");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "display_generic");

        BeginLoad(snapshot, cache =>
        {
            var displays = ResolveCached("Displays", cache.Displays);
            var gpu = ResolveCached("GPU", cache.Gpu);
            var specs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Primary Resolution", displays.PrimaryWidth > 0 && displays.PrimaryHeight > 0 ? $"{displays.PrimaryWidth}x{displays.PrimaryHeight}" : "Unknown"),
                new KeyValuePair<string, string>("Display Count", displays.DisplayCount > 0 ? displays.DisplayCount.ToString() : "Unknown"),
                new KeyValuePair<string, string>("Graphics Adapter", ValueOrUnknown(gpu.Name)),
                new KeyValuePair<string, string>("Adapter Vendor", ValueOrUnknown(gpu.Vendor))
            };

            if (displays.PrimaryRefreshRateHz > 0) specs.Add(new("Primary Refresh Rate", $"{displays.PrimaryRefreshRateHz} Hz"));
            if (displays.PrimaryBitsPerPixel > 0) specs.Add(new("Color Depth", $"{displays.PrimaryBitsPerPixel}-bit"));
            if (displays.VirtualWidth > 0 && displays.VirtualHeight > 0) specs.Add(new("Virtual Desktop", $"{displays.VirtualWidth}x{displays.VirtualHeight}"));
            if (!string.IsNullOrWhiteSpace(displays.PrimaryMonitorName)) specs.Add(new("Primary Monitor", ValueOrUnknown(displays.PrimaryMonitorName)));
            if (!string.IsNullOrWhiteSpace(displays.MonitorManufacturer)) specs.Add(new("Monitor Manufacturer", ValueOrUnknown(displays.MonitorManufacturer)));
            if (!string.IsNullOrWhiteSpace(displays.MonitorModel)) specs.Add(new("Monitor Model", ValueOrUnknown(displays.MonitorModel)));
            if (!string.IsNullOrWhiteSpace(displays.GpuOutput)) specs.Add(new("GPU Output", ValueOrUnknown(displays.GpuOutput)));

            return new HardwareDetailPayload(
                "Displays",
                ValueOrUnknown(gpu.Name),
                HardwareIconResolver.ResolveIconKey(
                    "display",
                    $"{displays.PrimaryMonitorName} {displays.MonitorManufacturer} {displays.MonitorModel}",
                    HardwareIconResolver.GetFallbackKey("display")),
                HardwareIconResolver.GetFallbackKey("display"),
                specs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
}

public sealed class NetworkDetailVM : HardwareDetailViewModelBase
{
    public NetworkDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Network)
    {
        Title = "Network";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("network");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "network_generic");

        BeginLoad(snapshot, cache =>
        {
            var network = ResolveCached("Network", cache.Network);
            var matchQuery = !string.IsNullOrWhiteSpace(network.PrimaryAdapterDescription)
                ? network.PrimaryAdapterDescription
                : network.PrimaryAdapterName;
            var matchedNetwork = HardwareKnowledgeDbService.Instance.MatchNetworkAdapter(matchQuery ?? string.Empty);

            var specs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Adapters Detected", network.AdapterCount.ToString()),
                new KeyValuePair<string, string>("Adapters Up", network.AdapterUpCount.ToString()),
                new KeyValuePair<string, string>("Wireless Adapters", network.WirelessAdapterCount.ToString()),
                new KeyValuePair<string, string>("Primary Adapter", ValueOrUnknown(network.PrimaryAdapterName)),
                new KeyValuePair<string, string>("Description", ValueOrUnknown(network.PrimaryAdapterDescription)),
                new KeyValuePair<string, string>("Adapter Type", ValueOrUnknown(network.PrimaryAdapterType)),
                new KeyValuePair<string, string>("Link Speed", ValueOrUnknown(network.PrimaryLinkSpeed)),
                new KeyValuePair<string, string>("IPv4", ValueOrUnknown(network.PrimaryIpv4)),
                new KeyValuePair<string, string>("Gateway", ValueOrUnknown(network.PrimaryGateway)),
                new KeyValuePair<string, string>("DNS", ValueOrUnknown(network.PrimaryDns)),
                new KeyValuePair<string, string>("MAC Address", ValueOrUnknown(network.PrimaryMacAddress)),
                new KeyValuePair<string, string>("Source", "Startup cache")
            };

            if (matchedNetwork != null)
            {
                specs.Add(new("DB Match", $"{matchedNetwork.Brand} {matchedNetwork.ModelName}".Trim()));
            }

            return new HardwareDetailPayload(
                "Network",
                ValueOrUnknown(network.PrimaryAdapterName),
                matchedNetwork != null
                    ? matchedNetwork.IconKey
                    : HardwareIconResolver.ResolveIconKey(
                        "network",
                        $"{network.PrimaryAdapterDescription} {network.PrimaryAdapterName}",
                        HardwareIconResolver.GetFallbackKey("network")),
                HardwareIconResolver.GetFallbackKey("network"),
                specs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
}

public sealed class UsbDetailVM : HardwareDetailViewModelBase
{
    public UsbDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Usb)
    {
        Title = "USB";
        Subtitle = "Loading system data...";
        IconKey = HardwareIconResolver.GetFallbackKey("usb");
        IconSource = HardwareIconResolver.ResolveIcon(IconKey, "usb_generic");

        BeginLoad(snapshot, cache =>
        {
            var usb = ResolveCached("USB", cache.Usb);
            var matchQuery = !string.IsNullOrWhiteSpace(usb.PrimaryControllerName)
                ? usb.PrimaryControllerName
                : usb.PrimaryUsbDeviceName;
            var matchedUsb = HardwareKnowledgeDbService.Instance.MatchUsbController(matchQuery ?? string.Empty);

            var specs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Removable Drives", usb.RemovableDriveCount.ToString()),
                new KeyValuePair<string, string>("USB Controllers", usb.UsbControllerCount.ToString()),
                new KeyValuePair<string, string>("USB Devices", usb.UsbDeviceCount.ToString()),
                new KeyValuePair<string, string>("Primary Controller", ValueOrUnknown(usb.PrimaryControllerName)),
                new KeyValuePair<string, string>("Primary Device", ValueOrUnknown(usb.PrimaryUsbDeviceName)),
                new KeyValuePair<string, string>("Vendor ID", ValueOrUnknown(usb.PrimaryVendorId)),
                new KeyValuePair<string, string>("Product ID", ValueOrUnknown(usb.PrimaryProductId)),
                new KeyValuePair<string, string>("Source", "Startup cache"),
                new KeyValuePair<string, string>("Tip", "Use Monitor tab for detailed USB inventory")
            };

            if (matchedUsb != null)
            {
                specs.Add(new("DB Match", $"{matchedUsb.Brand} {matchedUsb.ModelName}".Trim()));
            }

            return new HardwareDetailPayload(
                "USB",
                ValueOrUnknown(usb.PrimaryControllerName),
                matchedUsb != null
                    ? matchedUsb.IconKey
                    : HardwareIconResolver.ResolveIconKey(
                        "usb",
                        $"{usb.PrimaryControllerName} {usb.PrimaryUsbDeviceName}",
                        HardwareIconResolver.GetFallbackKey("usb")),
                HardwareIconResolver.GetFallbackKey("usb"),
                specs);
        });
    }

    private static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
}

public static class HardwareDetailViewModelFactory
{
    public static HardwareDetailViewModelBase? Create(HardwareType type, HardwareDetailSnapshot? snapshot = null)
    {
        return type switch
        {
            HardwareType.Os => new OsDetailVM(snapshot),
            HardwareType.Cpu => new CpuDetailVM(snapshot),
            HardwareType.Gpu => new GpuDetailVM(snapshot),
            HardwareType.Motherboard => new MotherboardDetailVM(snapshot),
            HardwareType.Memory => new MemoryDetailVM(snapshot),
            HardwareType.Storage => new StorageDetailVM(snapshot),
            HardwareType.Displays => new DisplaysDetailVM(snapshot),
            HardwareType.Network => new NetworkDetailVM(snapshot),
            HardwareType.Usb => new UsbDetailVM(snapshot),
            _ => null
        };
    }
}
