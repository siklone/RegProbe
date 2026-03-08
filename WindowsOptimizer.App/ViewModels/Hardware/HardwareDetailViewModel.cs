using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;
using WindowsOptimizer.App.Models;
using WindowsOptimizer.App.Diagnostics;
using WindowsOptimizer.App.HardwareDb.Models;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public abstract class HardwareDetailViewModelBase : ViewModelBase, IDisposable
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _iconKey = string.Empty;
    private ImageSource _iconSource = IconResolver.ResolveByKey(null, HardwareType.Display);
    private bool _isLoading;
    private string _loadingStatus = "Loading system data...";
    private readonly ObservableCollection<SpecItem> _specs = new();
    private readonly ObservableCollection<HardwareDetailTab> _deviceTabs = new();
    private HardwareDetailTab? _selectedDeviceTab;
    private bool _suppressTabSpecSync;
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
        // Keep the XAML-facing `Specs` property in sync whenever the underlying
        // SpecsCollection changes so bindings update correctly.
        SpecsCollection.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Specs));
        _deviceTabs.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasDeviceTabs));
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

    // Backwards-compatible property expected by the XAML. Use a stable
    // collection instance so WPF can observe incremental changes reliably.
    public ObservableCollection<SpecItem> Specs => _specs;
    public ObservableCollection<HardwareDetailTab> DeviceTabs => _deviceTabs;
    public bool HasDeviceTabs => _deviceTabs.Count > 0;

    public HardwareDetailTab? SelectedDeviceTab
    {
        get => _selectedDeviceTab;
        set
        {
            if (!SetProperty(ref _selectedDeviceTab, value) || value == null || _suppressTabSpecSync)
            {
                return;
            }

            SetSpecs(value.Specs);
        }
    }

    public ObservableCollection<int> SkeletonRows { get; } = new() { 1, 2, 3, 4, 5, 6 };

    public bool IsLoading
    {
        get => _isLoading;
        protected set => SetProperty(ref _isLoading, value);
    }

    public string LoadingStatus
    {
        get => _loadingStatus;
        protected set
        {
            if (SetProperty(ref _loadingStatus, value))
            {
                // Notify the legacy XAML binding that expects `LoadingMessage`.
                OnPropertyChanged(nameof(LoadingMessage));
            }
        }
    }

    // Backwards-compatible alias used by the XAML
    public string LoadingMessage => LoadingStatus;

    public ICommand CloseCommand { get; }

    protected void SetSpecs(IEnumerable<KeyValuePair<string, string>>? specs)
    {
        specs ??= Array.Empty<KeyValuePair<string, string>>();
        var incomingCount = specs.Count();
        AppDiagnostics.Log($"[DetailVM] SetSpecs invoked. Incoming specs count: {incomingCount}, VM: {GetType().Name}");

        SpecsCollection.Clear();
        foreach (var item in specs)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
            {
                continue;
            }

            SpecsCollection.Add(item);
        }
        AppDiagnostics.Log($"[DetailVM] SetSpecs completed. SpecsCollection count after add: {SpecsCollection.Count}, VM: {GetType().Name}");

        // Keep the stable Specs collection in sync so bindings observe the
        // incremental changes rather than losing the collection instance.
        _specs.Clear();
        foreach (var item in SpecsCollection)
        {
            _specs.Add(SpecItem.Row(item.Key, item.Value));
        }
        AppDiagnostics.Log($"[DetailVM] _specs synced. _specs.Count: {_specs.Count}, VM: {GetType().Name}");
    }

    protected void SetDeviceTabs(IEnumerable<HardwareDetailTab>? tabs)
    {
        _suppressTabSpecSync = true;
        try
        {
            _deviceTabs.Clear();
            foreach (var tab in tabs ?? Enumerable.Empty<HardwareDetailTab>())
            {
                if (string.IsNullOrWhiteSpace(tab.Header))
                {
                    continue;
                }

                _deviceTabs.Add(tab);
            }

            if (_deviceTabs.Count > 0)
            {
                SelectedDeviceTab = _deviceTabs[0];
                SetSpecs(SelectedDeviceTab.Specs);
            }
            else
            {
                SelectedDeviceTab = null;
            }
        }
        finally
        {
            _suppressTabSpecSync = false;
            OnPropertyChanged(nameof(DeviceTabs));
            OnPropertyChanged(nameof(HasDeviceTabs));
            OnPropertyChanged(nameof(SelectedDeviceTab));
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
        SetDeviceTabs(null);
        SetSpecs(new[]
        {
            new KeyValuePair<string, string>("Loading", "Collecting system info..."),
            new KeyValuePair<string, string>("Loading", "Collecting system info..."),
            new KeyValuePair<string, string>("Loading", "Collecting system info...")
        });
        AppDiagnostics.Log($"[DetailVM] BeginLoad placeholder SetSpecs called. SpecsCollection count: {SpecsCollection.Count}, Title: {Title}, VM: {GetType().Name}");

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
        AppDiagnostics.Log($"[DetailVM] OnSnapshotUpdated payload.Specs count: {payload.Specs?.Count() ?? 0}, VM: {GetType().Name}, Tier: {tier}");
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            Title = payload.Title;
            Subtitle = payload.Subtitle;
            IconKey = payload.IconResolution.IconKey;
            IconSource = HardwareIconService.Resolve(payload.IconResolution);

            var tabs = BuildDisplayTabs(payload.DeviceTabs);
            if (tabs.Count > 0)
            {
                SetDeviceTabs(tabs);
            }
            else
            {
                SetDeviceTabs(null);
                SetSpecs(BuildDisplaySpecs(payload.Specs, payload.IconResolution, payload.MatchedModel));
            }

            AppDiagnostics.Log($"[DetailVM] SetSpecs called. SpecsCollection count: {SpecsCollection.Count}, Title: {Title}, VM: {GetType().Name}");
            // Always show partial data immediately instead of hiding the specs while tiers complete
            IsLoading = false;
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

            AppDiagnostics.Log($"[DetailVM] LoadAsync payload.Specs count: {payload.Specs?.Count() ?? 0}, VM: {GetType().Name}, SnapshotProvided: {snapshot != null}");

            // Set UI-bound properties on the UI thread. LoadAsync runs the
            // collector on a background thread via Task.Run so we must marshal
            // updates to the Dispatcher before touching ObservableCollection.
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Title = payload.Title;
                Subtitle = payload.Subtitle;
                IconKey = payload.IconResolution.IconKey;
                IconSource = HardwareIconService.Resolve(payload.IconResolution);

                var tabs = BuildDisplayTabs(payload.DeviceTabs);
                if (tabs.Count > 0)
                {
                    SetDeviceTabs(tabs);
                }
                else
                {
                    SetDeviceTabs(null);
                    SetSpecs(BuildDisplaySpecs(payload.Specs, payload.IconResolution, payload.MatchedModel));
                }

                AppDiagnostics.Log($"[DetailVM] SetSpecs called. SpecsCollection count: {SpecsCollection.Count}, Title: {Title}, VM: {GetType().Name}");
                IsLoading = false;
                LoadingStatus = HardwarePreloadService.Instance.CurrentTier switch
                {
                    1 => "Collecting security metrics...",
                    2 => "Collecting firmware and board data...",
                    _ => "Ready"
                };
            });
        }
        finally
        {
            // No-op: IsLoading is set to false when payload arrives; keep finally for symmetry but do not override
        }
    }

    protected sealed record HardwareDetailPayload(
        string Title,
        string Subtitle,
        HardwareIconResolutionResult IconResolution,
        IEnumerable<KeyValuePair<string, string>> Specs,
        HardwareModelBase? MatchedModel = null,
        IEnumerable<HardwareDetailTab>? DeviceTabs = null);

    public sealed record HardwareDetailTab(
        string Header,
        IReadOnlyList<KeyValuePair<string, string>> Specs,
        HardwareIconResolutionResult? IconResolution = null,
        HardwareModelBase? MatchedModel = null,
        ImageSource? IconSource = null);

    protected static T ResolveCached<T>(string key, T fallback) where T : class
    {
        return MetricCacheService.Instance.Get<T>(key) ?? fallback;
    }
    protected static string ValueOrUnknown(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();

    private static void AppendIconResolutionSpecs(List<KeyValuePair<string, string>> specs, HardwareIconResolutionResult resolution)
    {
        _ = specs;
        _ = resolution;
    }

    private static List<KeyValuePair<string, string>> BuildDisplaySpecs(
        IEnumerable<KeyValuePair<string, string>>? specs,
        HardwareIconResolutionResult? resolution,
        HardwareModelBase? matchedModel)
    {
        var rows = (specs ?? Enumerable.Empty<KeyValuePair<string, string>>()).ToList();
        if (resolution != null)
        {
            AppendIconResolutionSpecs(rows, resolution);
        }

        if (matchedModel != null)
        {
            AppendRichSpecs(rows, matchedModel, resolution);
        }

        return rows;
    }

    private static List<HardwareDetailTab> BuildDisplayTabs(IEnumerable<HardwareDetailTab>? tabs)
    {
        var displayTabs = new List<HardwareDetailTab>();
        foreach (var tab in tabs ?? Enumerable.Empty<HardwareDetailTab>())
        {
            var resolution = tab.IconResolution;
            var iconSource = tab.IconSource ?? (resolution != null ? HardwareIconService.Resolve(resolution) : null);
            displayTabs.Add(tab with
            {
                Specs = BuildDisplaySpecs(tab.Specs, resolution, tab.MatchedModel),
                IconSource = iconSource
            });
        }

        return displayTabs;
    }

    private static void AppendRichSpecs(List<KeyValuePair<string, string>> specs, HardwareModelBase model, HardwareIconResolutionResult? iconResolution = null)
    {
        specs.Add(new("---", "---")); // Separator
        specs.Add(new("Database Match", iconResolution?.DatabaseMatchLabel ?? "Verified"));
        if (!string.IsNullOrWhiteSpace(model.Generation)) specs.Add(new("Generation", model.Generation));
        if (!string.IsNullOrWhiteSpace(model.Codename)) specs.Add(new("Codename", model.Codename));
        if (model.ReleaseYear > 0) specs.Add(new("Release Year", model.ReleaseYear.ToString()));
        if (!string.IsNullOrWhiteSpace(model.Architecture)) specs.Add(new("Architecture", model.Architecture));
        if (!string.IsNullOrWhiteSpace(model.ProcessNode)) specs.Add(new("Process Node", model.ProcessNode));

        if (model is CpuModel cpu)
        {
            if (cpu.CoreCount > 0) specs.Add(new("Physical Cores", cpu.CoreCount.ToString()));
            if (cpu.ThreadCount > 0) specs.Add(new("Total Threads", cpu.ThreadCount.ToString()));
            if (cpu.MaxBoostGHz > 0) specs.Add(new("Max Boost", $"{cpu.MaxBoostGHz:F1} GHz"));
        }
        else if (model is GpuModel gpu)
        {
            if (gpu.VramGB > 0) specs.Add(new("VRAM Size", $"{gpu.VramGB} GB"));
            if (gpu.BoostMHz > 0) specs.Add(new("Boost Clock", $"{gpu.BoostMHz} MHz"));
        }
    }

    protected static string FormatBytes(long bytes)
    {
        const long giga = 1024L * 1024L * 1024L;
        const long tera = giga * 1024L;
        if (bytes <= 0) return "Unknown";
        return bytes >= tera
            ? $"{bytes / (double)tera:F2} TB"
            : $"{bytes / (double)giga:F1} GB";
    }

    protected static string FormatHz(int mhz)
    {
        if (mhz <= 0) return "Unknown";
        if (mhz >= 1000) return $"{mhz / 1000.0:F2} GHz";
        return $"{mhz} MHz";
    }

    protected static string FormatMacAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        var compact = value.Replace(":", string.Empty).Replace("-", string.Empty).Trim();
        if (compact.Length != 12)
        {
            return value;
        }

        return string.Join(":", Enumerable.Range(0, 6).Select(i => compact.Substring(i * 2, 2)));
    }
}

public sealed class OsDetailVM : HardwareDetailViewModelBase
{
    public OsDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Os)
    {
        Title = "Operating System";
        Subtitle = "Loading system data...";
        var runtimeOsName = GetRuntimeOsDefaultName();
        IconKey = HardwareIconResolver.ResolveOsIconKey(runtimeOsName);
        IconSource = HardwareIconResolver.ResolveOsIcon(runtimeOsName);

        BeginLoad(snapshot, cache =>
        {
            var os = ResolveCached("OS", cache.Os);
            var normalized = !string.IsNullOrWhiteSpace(os.NormalizedName)
                ? os.NormalizedName
                : BuildNormalizedOsName(os.BuildNumber, os.Edition, os.DisplayVersion, os.ReleaseId);
            var displayProductName = BuildDisplayProductName(os.ProductName, os.BuildNumber, os.Edition);
            
            var subtitle = !string.IsNullOrWhiteSpace(normalized) ? normalized : "Windows Operating System";
            var iconResolution = HardwareIconService.ResolveByIconKeyResult(HardwareType.Os, os.IconKey, normalized);

            var specs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(displayProductName)) specs.Add(new("Product Name", displayProductName));
            if (!string.IsNullOrWhiteSpace(os.ProductName) &&
                !string.Equals(displayProductName, os.ProductName, StringComparison.OrdinalIgnoreCase))
            {
                specs.Add(new("Registry Product Name", os.ProductName));
            }
            if (!string.IsNullOrWhiteSpace(os.Edition)) specs.Add(new("Edition", os.Edition));
            if (!string.IsNullOrWhiteSpace(os.DisplayVersion)) specs.Add(new("Display Version", os.DisplayVersion));
            if (!string.IsNullOrWhiteSpace(os.ReleaseId)) specs.Add(new("Release ID", os.ReleaseId));
            if (!string.IsNullOrWhiteSpace(os.Version)) specs.Add(new("Version", os.Version));
            if (os.BuildNumber > 0) specs.Add(new("Build", os.BuildNumber.ToString()));
            if (os.Ubr > 0) specs.Add(new("UBR", os.Ubr.ToString()));
            if (!string.IsNullOrWhiteSpace(os.Architecture)) specs.Add(new("Architecture", os.Architecture));
            if (!string.IsNullOrWhiteSpace(os.InstallType)) specs.Add(new("Install Type", os.InstallType));
            if (!string.IsNullOrWhiteSpace(os.InstallDate)) specs.Add(new("Install Date", os.InstallDate));
            if (!string.IsNullOrWhiteSpace(os.LastBootTime)) specs.Add(new("Last Boot", os.LastBootTime));
            if (!string.IsNullOrWhiteSpace(os.Uptime)) specs.Add(new("Uptime", os.Uptime));
            if (!string.IsNullOrWhiteSpace(os.Username)) specs.Add(new("User", os.Username));
            if (!string.IsNullOrWhiteSpace(os.RegisteredOwner)) specs.Add(new("Registered Owner", os.RegisteredOwner));
            if (!string.IsNullOrWhiteSpace(os.RegisteredOrganization)) specs.Add(new("Registered Organization", os.RegisteredOrganization));
            if (!string.IsNullOrWhiteSpace(os.WindowsExperienceIndex)) specs.Add(new("Windows Experience Index", os.WindowsExperienceIndex));
            if (!string.IsNullOrWhiteSpace(os.BiosMode)) specs.Add(new("BIOS Mode", os.BiosMode));
            if (!string.IsNullOrWhiteSpace(os.SecureBootState)) specs.Add(new("Secure Boot", os.SecureBootState));
            if (!string.IsNullOrWhiteSpace(os.TpmVersion)) specs.Add(new("TPM", os.TpmVersion));
            if (!string.IsNullOrWhiteSpace(os.BitLockerStatus)) specs.Add(new("BitLocker", os.BitLockerStatus));
            if (!string.IsNullOrWhiteSpace(os.DeviceGuardStatus)) specs.Add(new("Device Guard", os.DeviceGuardStatus));
            if (!string.IsNullOrWhiteSpace(os.CredentialGuardStatus)) specs.Add(new("Credential Guard", os.CredentialGuardStatus));
            if (!string.IsNullOrWhiteSpace(os.VirtualizationEnabled)) specs.Add(new("Virtualization", os.VirtualizationEnabled));
            if (!string.IsNullOrWhiteSpace(os.HyperVInstalled)) specs.Add(new("Hyper-V", os.HyperVInstalled));
            if (!string.IsNullOrWhiteSpace(os.DefenderStatus)) specs.Add(new("Defender Service", os.DefenderStatus));
            if (!string.IsNullOrWhiteSpace(os.FirewallStatus)) specs.Add(new("Firewall Service", os.FirewallStatus));

            return new HardwareDetailPayload("Operating System", subtitle, iconResolution, specs);
        });
    }

    private static string GetRuntimeOsDefaultName()
    {
        return Environment.OSVersion.Version.Build >= 22000 ? "Windows 11" : "Windows 10";
    }

    private static string BuildNormalizedOsName(int buildNumber, string? edition, string? displayVersion, string? releaseId)
    {
        var osBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var normalizedEdition = NormalizeEditionLabel(edition);
        var normalized = string.IsNullOrWhiteSpace(normalizedEdition) ? osBase : $"{osBase} {normalizedEdition}";
        var version = !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion : releaseId;
        return string.IsNullOrWhiteSpace(version) ? normalized : $"{normalized} ({version})";
    }

    private static string BuildDisplayProductName(string? productName, int buildNumber, string? edition)
    {
        var osBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var normalizedEdition = NormalizeEditionLabel(edition);
        var normalizedBaseWithEdition = string.IsNullOrWhiteSpace(normalizedEdition)
            ? osBase
            : $"{osBase} {normalizedEdition}";

        var candidate = productName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return normalizedBaseWithEdition;
        }

        if (buildNumber >= 22000 && candidate.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
        {
            candidate = Regex.Replace(candidate, "Windows\\s+10", "Windows 11", RegexOptions.IgnoreCase);
        }
        else if (buildNumber > 0 && buildNumber < 22000 && candidate.Contains("Windows 11", StringComparison.OrdinalIgnoreCase))
        {
            candidate = Regex.Replace(candidate, "Windows\\s+11", "Windows 10", RegexOptions.IgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(normalizedEdition) &&
            !candidate.Contains(normalizedEdition, StringComparison.OrdinalIgnoreCase))
        {
            candidate = normalizedBaseWithEdition;
        }

        return candidate;
    }

    private static string NormalizeEditionLabel(string? edition)
    {
        if (string.IsNullOrWhiteSpace(edition))
        {
            return string.Empty;
        }

        return edition.Trim() switch
        {
            "Professional" => "Pro",
            "Core" => "Home",
            "CoreSingleLanguage" => "Home Single Language",
            "EnterpriseS" => "Enterprise LTSC",
            _ => edition.Trim()
        };
    }
}

public sealed class CpuDetailVM : HardwareDetailViewModelBase
{
    public CpuDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Cpu)
    {
        Title = "CPU";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Cpu);
        IconSource = IconResolver.Resolve(HardwareType.Cpu, null, null);

        BeginLoad(snapshot, cache =>
        {
            var cpu = ResolveCached("CPU", cache.Cpu);
            var matchedCpu = HardwareKnowledgeDbService.Instance.MatchCpu(cpu.Name ?? string.Empty);
            var specs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(cpu.Name)) specs.Add(new("Name", cpu.Name));
            if (!string.IsNullOrWhiteSpace(cpu.Manufacturer)) specs.Add(new("Manufacturer", cpu.Manufacturer));
            if (!string.IsNullOrWhiteSpace(cpu.Architecture)) specs.Add(new("Architecture", cpu.Architecture));
            if (!string.IsNullOrWhiteSpace(cpu.Socket)) specs.Add(new("Socket", cpu.Socket));

            if (cpu.MaxClockMhz > 0) specs.Add(new("Max Clock", FormatHz(cpu.MaxClockMhz)));
            if (cpu.CurrentClockMhz > 0) specs.Add(new("Current Clock", $"{cpu.CurrentClockMhz} MHz"));
            if (cpu.BusSpeedMhz > 0) specs.Add(new("Bus Speed", $"{cpu.BusSpeedMhz} MHz"));
            if (cpu.Cores > 0) specs.Add(new("Cores", cpu.Cores.ToString()));
            if (cpu.Threads > 0) specs.Add(new("Threads", cpu.Threads.ToString()));
            if (cpu.HyperThreading.HasValue) specs.Add(new("Hyper-Threading", cpu.HyperThreading.Value ? "Supported" : "Not Supported"));
            if (cpu.Virtualization.HasValue) specs.Add(new("Virtualization Firmware", cpu.Virtualization.Value ? "Enabled" : "Disabled"));
            
            if (cpu.L1CacheKB > 0) specs.Add(new("L1 Cache", $"{cpu.L1CacheKB} KB"));
            if (cpu.L2CacheKB > 0) specs.Add(new("L2 Cache", $"{cpu.L2CacheKB} KB"));
            if (cpu.L3CacheKB > 0) specs.Add(new("L3 Cache", $"{cpu.L3CacheKB} KB"));
            
            if (cpu.VoltageV > 0) specs.Add(new("Voltage", $"{cpu.VoltageV:F1} V"));
            if (cpu.AddressWidth > 0) specs.Add(new("Address Width", $"{cpu.AddressWidth}-bit"));
            if (!string.IsNullOrWhiteSpace(cpu.CpuFamily)) specs.Add(new("Family", cpu.CpuFamily));
            if (!string.IsNullOrWhiteSpace(cpu.CpuModel)) specs.Add(new("Model", cpu.CpuModel));
            if (!string.IsNullOrWhiteSpace(cpu.Stepping)) specs.Add(new("Stepping", cpu.Stepping));
            if (!string.IsNullOrWhiteSpace(cpu.Microcode)) specs.Add(new("Microcode", cpu.Microcode));
            if (!string.IsNullOrWhiteSpace(cpu.Revision)) specs.Add(new("Revision", cpu.Revision));
            if (!string.IsNullOrWhiteSpace(cpu.Caption)) specs.Add(new("Description", cpu.Caption));
            if (!string.IsNullOrWhiteSpace(cpu.Description) &&
                !string.Equals(cpu.Description, cpu.Caption, StringComparison.OrdinalIgnoreCase))
            {
                specs.Add(new("WMI Description", cpu.Description));
            }

            var subtitle = matchedCpu?.ModelName ?? cpu.Name ?? "Processor";
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Cpu, cpu.Name, matchedCpu);
            return new HardwareDetailPayload("CPU", subtitle, iconResolution, specs, matchedCpu);
        });
    }
}

public sealed class GpuDetailVM : HardwareDetailViewModelBase
{
    public GpuDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Gpu)
    {
        Title = "GPU";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Gpu);
        IconSource = IconResolver.Resolve(HardwareType.Gpu, null, null);

        BeginLoad(snapshot, cache =>
        {
            var gpu = ResolveCached("GPU", cache.Gpu);
            var gpuLookupSeed = HardwareIconService.BuildGpuLookupSeed(
                gpu.Name,
                gpu.Vendor,
                gpu.VendorId,
                gpu.DeviceId,
                gpu.PnpDeviceId,
                gpu.VideoProcessor);
            var matchedGpu = HardwareKnowledgeDbService.Instance.MatchGpu(gpuLookupSeed);
            var specs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(gpu.Name)) specs.Add(new("Name", gpu.Name));
            if (!string.IsNullOrWhiteSpace(gpu.Vendor)) specs.Add(new("Vendor", gpu.Vendor));
            if (!string.IsNullOrWhiteSpace(gpu.VendorId) && !string.IsNullOrWhiteSpace(gpu.DeviceId))
            {
                specs.Add(new("PCI ID", $"VEN_{gpu.VendorId}&DEV_{gpu.DeviceId}"));
            }
            if (!string.IsNullOrWhiteSpace(gpu.VendorId)) specs.Add(new("PCI Vendor ID", gpu.VendorId));
            if (!string.IsNullOrWhiteSpace(gpu.DeviceId)) specs.Add(new("PCI Device ID", gpu.DeviceId));
            if (!string.IsNullOrWhiteSpace(gpu.DriverVersion)) specs.Add(new("Driver Version", gpu.DriverVersion));
            if (!string.IsNullOrWhiteSpace(gpu.DriverDate)) specs.Add(new("Driver Date", gpu.DriverDate));
            if (gpu.AdapterRamBytes > 0) specs.Add(new("VRAM", FormatBytes(gpu.AdapterRamBytes)));
            if (!string.IsNullOrWhiteSpace(gpu.InferredVramType)) specs.Add(new("VRAM Type", gpu.InferredVramType));
            if (!string.IsNullOrWhiteSpace(gpu.VideoProcessor)) specs.Add(new("Processor", gpu.VideoProcessor));
            if (!string.IsNullOrWhiteSpace(gpu.AdapterDacType)) specs.Add(new("Adapter DAC", gpu.AdapterDacType));
            if (!string.IsNullOrWhiteSpace(gpu.PnpDeviceId))
            {
                specs.Add(new("PNP Device ID", gpu.PnpDeviceId));
            }
            if (gpu.CurrentHorizontalResolution > 0 && gpu.CurrentVerticalResolution > 0)
            {
                specs.Add(new("Active Desktop Resolution", $"{gpu.CurrentHorizontalResolution} x {gpu.CurrentVerticalResolution}"));
            }
            if (gpu.RefreshRateHz > 0) specs.Add(new("Active Refresh Rate", $"{gpu.RefreshRateHz} Hz"));

            var subtitle = matchedGpu?.ModelName ?? gpu.Name ?? "Graphics Card";
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Gpu, gpuLookupSeed, matchedGpu);
            return new HardwareDetailPayload("GPU", subtitle, iconResolution, specs, matchedGpu);
        });
    }
}

public sealed class MotherboardDetailVM : HardwareDetailViewModelBase
{
    public MotherboardDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Motherboard)
    {
        Title = "Motherboard";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Motherboard);
        IconSource = IconResolver.Resolve(HardwareType.Motherboard, null, null);

        BeginLoad(snapshot, cache =>
        {
            var mb = ResolveCached("Motherboard", cache.Motherboard);
            var specs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(mb.Manufacturer)) specs.Add(new("Manufacturer", mb.Manufacturer));
            if (!string.IsNullOrWhiteSpace(mb.Model)) specs.Add(new("Model", mb.Model));
            if (!string.IsNullOrWhiteSpace(mb.Product)) specs.Add(new("Product", mb.Product));
            if (!string.IsNullOrWhiteSpace(mb.Version)) specs.Add(new("Version", mb.Version));
            if (!string.IsNullOrWhiteSpace(mb.BiosVersion)) specs.Add(new("BIOS Version", mb.BiosVersion));
            if (!string.IsNullOrWhiteSpace(mb.BiosVendor)) specs.Add(new("BIOS Vendor", mb.BiosVendor));
            if (!string.IsNullOrWhiteSpace(mb.BiosDate)) specs.Add(new("BIOS Date", mb.BiosDate));
            var chipset = !string.IsNullOrWhiteSpace(mb.Chipset)
                ? mb.Chipset
                : TryExtractChipset(mb.Product ?? mb.Model);
            if (!string.IsNullOrWhiteSpace(chipset)) specs.Add(new("Chipset", chipset));
            if (!string.IsNullOrWhiteSpace(mb.Serial)) specs.Add(new("Serial", mb.Serial));
            if (!string.IsNullOrWhiteSpace(mb.AssetTag)) specs.Add(new("Asset Tag", mb.AssetTag));
            if (mb.SystemSlotCount > 0) specs.Add(new("Expansion Slots", mb.SystemSlotCount.ToString()));
            if (!string.IsNullOrWhiteSpace(mb.ChassisType)) specs.Add(new("Chassis Type", mb.ChassisType));
            if (!string.IsNullOrWhiteSpace(cache.Cpu.Socket)) specs.Add(new("CPU Socket", cache.Cpu.Socket));
            if (!string.IsNullOrWhiteSpace(cache.Os.BiosMode)) specs.Add(new("Firmware Mode", cache.Os.BiosMode));
            if (!string.IsNullOrWhiteSpace(cache.Os.SecureBootState)) specs.Add(new("Secure Boot", cache.Os.SecureBootState));

            var boardLookup = HardwareIconService.BuildMotherboardLookupSeed(
                mb.Manufacturer,
                mb.Product,
                mb.Model,
                mb.Version,
                mb.BiosVendor,
                chipset);
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Motherboard, boardLookup);
            var matchedMb = iconResolution.MatchedModel as MotherboardModel;
            var subtitle = matchedMb?.ModelName ?? mb.Product ?? mb.Model ?? "Motherboard";
            return new HardwareDetailPayload("Motherboard", subtitle, iconResolution, specs, matchedMb);
        });
    }

    private static string? TryExtractChipset(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var match = Regex.Match(input, @"\b([ABHXZ]\d{2,3}|W\d{2,3}|TRX\d{2,3}|X\d{3}E?)\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }
}

public sealed class MemoryDetailVM : HardwareDetailViewModelBase
{
    public MemoryDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Memory)
    {
        Title = "Memory";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Memory);
        IconSource = IconResolver.Resolve(HardwareType.Memory, null, null);

        BeginLoad(snapshot, cache =>
        {
            var mem = ResolveCached("Memory", cache.Memory);
            var memoryLookup = $"{mem.PrimaryManufacturer} {mem.PrimaryModel} {mem.MemoryType}".Trim();
            var matchedMem = HardwareKnowledgeDbService.Instance.MatchMemory(memoryLookup);
            var specs = new List<KeyValuePair<string, string>>();
            if (mem.TotalBytes > 0) specs.Add(new("Total Capacity", FormatBytes(mem.TotalBytes)));
            if (mem.ModuleCount > 0) specs.Add(new("Modules", mem.ModuleCount.ToString()));
            if (mem.TotalBytes > 0 && mem.ModuleCount > 0) specs.Add(new("Average Module Size", FormatBytes(mem.TotalBytes / mem.ModuleCount)));
            if (mem.SpeedMhz > 0) specs.Add(new("Frequency", $"{mem.SpeedMhz} MHz"));
            if (mem.ConfiguredSpeedMhz > 0 && mem.ConfiguredSpeedMhz != mem.SpeedMhz) specs.Add(new("Configured Speed", $"{mem.ConfiguredSpeedMhz} MHz"));
            if (!string.IsNullOrWhiteSpace(mem.MemoryType)) specs.Add(new("Memory Type", mem.MemoryType));
            if (!string.IsNullOrWhiteSpace(mem.FormFactor)) specs.Add(new("Form Factor", mem.FormFactor));
            if (mem.MinVoltageMv > 0) specs.Add(new("Min Voltage", $"{mem.MinVoltageMv / 1000.0:F2} V"));
            if (!string.IsNullOrWhiteSpace(mem.PrimaryManufacturer)) specs.Add(new("Manufacturer", mem.PrimaryManufacturer));
            if (!string.IsNullOrWhiteSpace(mem.PrimaryModel)) specs.Add(new("Primary Module", mem.PrimaryModel));

            if (mem.Modules.Count > 0)
            {
                specs.Add(new("---", "---"));
                for (var i = 0; i < mem.Modules.Count; i++)
                {
                    var module = mem.Modules[i];
                    var slotName = !string.IsNullOrWhiteSpace(module.Slot) ? module.Slot : $"Module {i + 1}";
                    var prefix = $"[{slotName}]";
                    if (!string.IsNullOrWhiteSpace(module.BankLabel)) specs.Add(new($"{prefix} Bank", module.BankLabel));
                    if (!string.IsNullOrWhiteSpace(module.Manufacturer)) specs.Add(new($"{prefix} Manufacturer", module.Manufacturer));
                    if (!string.IsNullOrWhiteSpace(module.PartNumber)) specs.Add(new($"{prefix} Part Number", module.PartNumber));
                    if (module.CapacityBytes > 0) specs.Add(new($"{prefix} Capacity", FormatBytes(module.CapacityBytes)));
                    if (module.SpeedMhz > 0) specs.Add(new($"{prefix} Speed", $"{module.SpeedMhz} MHz"));
                    if (module.ConfiguredSpeedMhz > 0 && module.ConfiguredSpeedMhz != module.SpeedMhz) specs.Add(new($"{prefix} Configured Speed", $"{module.ConfiguredSpeedMhz} MHz"));
                    if (!string.IsNullOrWhiteSpace(module.MemoryType)) specs.Add(new($"{prefix} Type", module.MemoryType));
                    if (!string.IsNullOrWhiteSpace(module.FormFactor)) specs.Add(new($"{prefix} Form Factor", module.FormFactor));
                    if (module.MinVoltageMv > 0) specs.Add(new($"{prefix} Min Voltage", $"{module.MinVoltageMv / 1000.0:F2} V"));
                    if (!string.IsNullOrWhiteSpace(module.SerialNumber)) specs.Add(new($"{prefix} Serial", module.SerialNumber));
                }
            }

            var subtitle = matchedMem?.ModelName ?? (!string.IsNullOrWhiteSpace(mem.MemoryType) ? $"{FormatBytes(mem.TotalBytes)} {mem.MemoryType}" : "System RAM");
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Memory, memoryLookup, matchedMem);
            return new HardwareDetailPayload("Memory", subtitle, iconResolution, specs, matchedMem);
        });
    }
}

public sealed class StorageDetailVM : HardwareDetailViewModelBase
{
    public StorageDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Storage)
    {
        Title = "Storage";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Storage);
        IconSource = IconResolver.Resolve(HardwareType.Storage, null, null);

        BeginLoad(snapshot, cache =>
        {
            var storage = ResolveCached("Storage", cache.Storage);
            var storageLookup = storage.PrimaryModel ?? string.Empty;
            var matchedStorage = HardwareKnowledgeDbService.Instance.MatchStorage(storageLookup);
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Storage, storageLookup, matchedStorage);
            var overviewSpecs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(storage.PrimaryModel)) overviewSpecs.Add(new("Primary Drive", storage.PrimaryModel));
            if (storage.TotalSizeBytes > 0) overviewSpecs.Add(new("Total Capacity", FormatBytes(storage.TotalSizeBytes)));
            if (storage.DeviceCount > 0) overviewSpecs.Add(new("Drive Count", storage.DeviceCount.ToString()));
            if (!string.IsNullOrWhiteSpace(storage.PrimaryMediaType)) overviewSpecs.Add(new("Primary Media Type", storage.PrimaryMediaType));
            if (!string.IsNullOrWhiteSpace(storage.PrimaryInterface)) overviewSpecs.Add(new("Primary Interface", storage.PrimaryInterface));
            if (!string.IsNullOrWhiteSpace(storage.Disks.FirstOrDefault()?.LogicalDrives)) overviewSpecs.Add(new("Primary Volumes", storage.Disks.First().LogicalDrives!));
            if (!string.IsNullOrWhiteSpace(storage.FirmwareRevision)) overviewSpecs.Add(new("Primary Firmware", storage.FirmwareRevision));
            if (storage.PartitionCount > 0) overviewSpecs.Add(new("Primary Partitions", storage.PartitionCount.ToString()));
            if (!string.IsNullOrWhiteSpace(storage.PrimarySerialNumber)) overviewSpecs.Add(new("Primary Serial Number", storage.PrimarySerialNumber));

            var tabs = new List<HardwareDetailTab>();
            if (overviewSpecs.Count > 0)
            {
                tabs.Add(new HardwareDetailTab(
                    "Overview",
                    overviewSpecs,
                    iconResolution,
                    matchedStorage));
            }

            for (var i = 0; i < storage.Disks.Count; i++)
            {
                var disk = storage.Disks[i];
                var diskLookup = disk.Model ?? string.Empty;
                var matchedDisk = HardwareKnowledgeDbService.Instance.MatchStorage(diskLookup);
                var diskResolution = HardwareIconService.ResolveResult(HardwareType.Storage, diskLookup, matchedDisk);
                var diskSpecs = new List<KeyValuePair<string, string>>();
                if (!string.IsNullOrWhiteSpace(disk.Model)) diskSpecs.Add(new("Model", disk.Model));
                if (disk.SizeBytes > 0) diskSpecs.Add(new("Capacity", FormatBytes(disk.SizeBytes)));
                if (!string.IsNullOrWhiteSpace(disk.MediaType)) diskSpecs.Add(new("Media Type", disk.MediaType));
                if (!string.IsNullOrWhiteSpace(disk.InterfaceType)) diskSpecs.Add(new("Interface", disk.InterfaceType));
                if (!string.IsNullOrWhiteSpace(disk.LogicalDrives)) diskSpecs.Add(new("Volumes", disk.LogicalDrives));
                if (!string.IsNullOrWhiteSpace(disk.FirmwareRevision)) diskSpecs.Add(new("Firmware", disk.FirmwareRevision));
                if (disk.PartitionCount > 0) diskSpecs.Add(new("Partitions", disk.PartitionCount.ToString()));
                if (disk.Index >= 0) diskSpecs.Add(new("Disk Index", disk.Index.ToString()));
                if (!string.IsNullOrWhiteSpace(disk.SerialNumber)) diskSpecs.Add(new("Serial", disk.SerialNumber));

                var shortLabel = !string.IsNullOrWhiteSpace(disk.Model)
                    ? TrimDiskTabHeader(disk.Model)
                    : $"Disk {i + 1}";
                tabs.Add(new HardwareDetailTab(
                    shortLabel,
                    diskSpecs,
                    diskResolution,
                    matchedDisk));
            }

            var subtitle = matchedStorage?.ModelName ?? storage.PrimaryModel ?? "System Storage";
            return new HardwareDetailPayload(
                "Storage",
                subtitle,
                iconResolution,
                overviewSpecs,
                matchedStorage,
                tabs);
        });
    }

    private static string TrimDiskTabHeader(string model)
    {
        var compact = Regex.Replace(model, @"\s+", " ").Trim();
        return compact.Length <= 18 ? compact : $"{compact[..18]}...";
    }
}

public sealed class DisplaysDetailVM : HardwareDetailViewModelBase
{
    public DisplaysDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Display)
    {
        Title = "Displays";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Display);
        IconSource = IconResolver.Resolve(HardwareType.Display, null, null);

        BeginLoad(snapshot, cache =>
        {
            var display = ResolveCached("Displays", cache.Displays);
            var overview = new List<KeyValuePair<string, string>>();
            if (display.DisplayCount > 0) overview.Add(new("Monitor Count", display.DisplayCount.ToString()));
            if (!string.IsNullOrWhiteSpace(display.PrimaryMonitorName)) overview.Add(new("Primary Monitor", display.PrimaryMonitorName));
            if (display.PrimaryWidth > 0 && display.PrimaryHeight > 0) overview.Add(new("Primary Resolution", $"{display.PrimaryWidth} x {display.PrimaryHeight}"));
            if (display.PrimaryRefreshRateHz > 0) overview.Add(new("Primary Refresh Rate", $"{display.PrimaryRefreshRateHz} Hz"));
            if (display.PrimaryBitsPerPixel > 0) overview.Add(new("Primary Color Depth", $"{display.PrimaryBitsPerPixel}-bit"));
            if (display.VirtualWidth > 0 && display.VirtualHeight > 0) overview.Add(new("Virtual Desktop", $"{display.VirtualWidth} x {display.VirtualHeight}"));
            if (!string.IsNullOrWhiteSpace(display.GpuOutput)) overview.Add(new("GPU Output", display.GpuOutput));

            var orderedDisplays = display.Devices
                .OrderByDescending(device => device.IsPrimary)
                .ThenBy(device => device.Name ?? device.DeviceName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var primaryDisplay = orderedDisplays.FirstOrDefault();
            var displayLookupSeed = !string.IsNullOrWhiteSpace(primaryDisplay?.IconLookupSeed)
                ? primaryDisplay!.IconLookupSeed
                : HardwareIconService.BuildDisplayLookupSeed(
                    display.PrimaryMonitorName,
                    display.MonitorManufacturer,
                    display.MonitorModel);
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Display, displayLookupSeed);

            var tabs = new List<HardwareDetailTab>();
            if (display.DisplayCount > 1 && overview.Count > 0)
            {
                tabs.Add(new HardwareDetailTab(
                    "Overview",
                    overview,
                    iconResolution));
            }

            var displayIndex = 1;
            foreach (var device in orderedDisplays)
            {
                var deviceLookupSeed = !string.IsNullOrWhiteSpace(device.IconLookupSeed)
                    ? device.IconLookupSeed
                    : HardwareIconService.BuildDisplayLookupSeed(
                        device.Name,
                        device.Manufacturer,
                        device.Model,
                        device.ProductCode);
                var deviceResolution = HardwareIconService.ResolveResult(HardwareType.Display, deviceLookupSeed);
                var deviceSpecs = new List<KeyValuePair<string, string>>();
                if (!string.IsNullOrWhiteSpace(device.Name)) deviceSpecs.Add(new("Monitor", device.Name));
                if (!string.IsNullOrWhiteSpace(device.Manufacturer)) deviceSpecs.Add(new("Manufacturer", device.Manufacturer));
                if (!string.IsNullOrWhiteSpace(device.Model)) deviceSpecs.Add(new("Model", device.Model));
                if (!string.IsNullOrWhiteSpace(device.ProductCode)) deviceSpecs.Add(new("Product Code", device.ProductCode));
                if (!string.IsNullOrWhiteSpace(device.Resolution)) deviceSpecs.Add(new("Resolution", device.Resolution));
                if (device.RefreshRateHz > 0) deviceSpecs.Add(new("Refresh Rate", $"{device.RefreshRateHz} Hz"));
                if (device.BitsPerPixel > 0) deviceSpecs.Add(new("Color Depth", $"{device.BitsPerPixel}-bit"));
                if (device.PhysicalWidthCm > 0 && device.PhysicalHeightCm > 0)
                {
                    deviceSpecs.Add(new("Physical Size", $"{device.PhysicalWidthCm} x {device.PhysicalHeightCm} cm"));
                }
                if (!string.IsNullOrWhiteSpace(device.ConnectionType) &&
                    !string.Equals(device.ConnectionType, "Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    deviceSpecs.Add(new("Connection", device.ConnectionType));
                }

                if (!string.IsNullOrWhiteSpace(device.DeviceName))
                {
                    deviceSpecs.Add(new("Desktop Target", device.DeviceName.Replace(@"\\.\", "")));
                }

                deviceSpecs.Add(new("Desktop Role", device.IsPrimary ? "Primary" : "Secondary"));
                if (!string.IsNullOrWhiteSpace(display.GpuOutput)) deviceSpecs.Add(new("GPU Output", display.GpuOutput));

                var headerSeed = !string.IsNullOrWhiteSpace(device.Name)
                    ? device.Name
                    : !string.IsNullOrWhiteSpace(device.Model)
                        ? device.Model
                        : $"Display {displayIndex}";
                var tabHeader = device.IsPrimary
                    ? $"Primary - {TrimDisplayTabHeader(headerSeed)}"
                    : TrimDisplayTabHeader(headerSeed);
                tabs.Add(new HardwareDetailTab(
                    tabHeader,
                    deviceSpecs,
                    deviceResolution,
                    deviceResolution.MatchedModel));
                displayIndex++;
            }

            var subtitle = display.DisplayCount > 1
                ? $"{display.DisplayCount} monitors connected"
                : display.PrimaryMonitorName ?? primaryDisplay?.Name ?? "Primary Display";
            return new HardwareDetailPayload(
                "Displays",
                subtitle,
                iconResolution,
                overview,
                iconResolution.MatchedModel,
                tabs);
        });
    }

    private static string TrimDisplayTabHeader(string monitorName)
    {
        var compact = Regex.Replace(monitorName, @"\s+", " ").Trim();
        return compact.Length <= 20 ? compact : $"{compact[..20]}...";
    }
}

public sealed class NetworkDetailVM : HardwareDetailViewModelBase
{
    public NetworkDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Network)
    {
        Title = "Network";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Network);
        IconSource = IconResolver.Resolve(HardwareType.Network, null, null);

        BeginLoad(snapshot, cache =>
        {
            var net = ResolveCached("Network", cache.Network);
            var overview = new List<KeyValuePair<string, string>>();
            if (net.AdapterCount > 0) overview.Add(new("Total Adapters", net.AdapterCount.ToString()));
            if (net.AdapterUpCount > 0) overview.Add(new("Adapters Up", net.AdapterUpCount.ToString()));
            if (net.WirelessAdapterCount > 0) overview.Add(new("Wireless Adapters", net.WirelessAdapterCount.ToString()));
            if (!string.IsNullOrWhiteSpace(net.PrimaryAdapterName)) overview.Add(new("Primary Adapter", net.PrimaryAdapterName));
            if (!string.IsNullOrWhiteSpace(net.PrimaryAdapterDescription)) overview.Add(new("Primary Description", net.PrimaryAdapterDescription));
            if (!string.IsNullOrWhiteSpace(net.PrimaryLinkSpeed)) overview.Add(new("Primary Link Speed", net.PrimaryLinkSpeed));
            if (!string.IsNullOrWhiteSpace(net.NetConnectionStatus)) overview.Add(new("Primary Status", net.NetConnectionStatus));
            var iconLookup = $"{net.PrimaryAdapterDescription} {net.PrimaryAdapterName}".Trim();
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Network, iconLookup);

            var tabs = new List<HardwareDetailTab>();
            if (overview.Count > 0)
            {
                tabs.Add(new HardwareDetailTab(
                    "Overview",
                    overview,
                    iconResolution,
                    iconResolution.MatchedModel));
            }

            if (net.Adapters.Count > 0)
            {
                foreach (var adapter in net.Adapters)
                {
                    var adapterLookup = $"{adapter.Description} {adapter.Name}".Trim();
                    var matchedAdapter = HardwareKnowledgeDbService.Instance.MatchNetworkAdapter(adapterLookup);
                    var adapterResolution = HardwareIconService.ResolveResult(HardwareType.Network, adapterLookup, matchedAdapter);
                    var adapterSpecs = new List<KeyValuePair<string, string>>();
                    if (!string.IsNullOrWhiteSpace(adapter.Name)) adapterSpecs.Add(new("Name", adapter.Name));
                    if (!string.IsNullOrWhiteSpace(adapter.Description)) adapterSpecs.Add(new("Description", adapter.Description));
                    if (!string.IsNullOrWhiteSpace(adapter.AdapterType)) adapterSpecs.Add(new("Type", adapter.AdapterType));
                    if (!string.IsNullOrWhiteSpace(adapter.Status)) adapterSpecs.Add(new("Status", adapter.Status));
                    if (!string.IsNullOrWhiteSpace(adapter.LinkSpeed)) adapterSpecs.Add(new("Link Speed", adapter.LinkSpeed));
                    if (!string.IsNullOrWhiteSpace(adapter.MacAddress)) adapterSpecs.Add(new("MAC Address", FormatMacAddress(adapter.MacAddress)));
                    if (!string.IsNullOrWhiteSpace(adapter.Ipv4)) adapterSpecs.Add(new("IPv4", adapter.Ipv4));
                    if (!string.IsNullOrWhiteSpace(adapter.Ipv6)) adapterSpecs.Add(new("IPv6", adapter.Ipv6));
                    if (!string.IsNullOrWhiteSpace(adapter.Gateway)) adapterSpecs.Add(new("Gateway", adapter.Gateway));
                    if (!string.IsNullOrWhiteSpace(adapter.Dns)) adapterSpecs.Add(new("DNS", adapter.Dns));
                    if (!string.IsNullOrWhiteSpace(adapter.DhcpEnabled)) adapterSpecs.Add(new("DHCP Enabled", adapter.DhcpEnabled));
                    if (!string.IsNullOrWhiteSpace(adapter.DhcpServer)) adapterSpecs.Add(new("DHCP Server", adapter.DhcpServer));
                    if (!string.IsNullOrWhiteSpace(adapter.LeaseObtained)) adapterSpecs.Add(new("Lease Obtained", adapter.LeaseObtained));
                    if (!string.IsNullOrWhiteSpace(adapter.LeaseExpires)) adapterSpecs.Add(new("Lease Expires", adapter.LeaseExpires));

                    var tabHeader = !string.IsNullOrWhiteSpace(adapter.Name)
                        ? TrimNetworkTabHeader(adapter.Name)
                        : "Adapter";
                    tabs.Add(new HardwareDetailTab(
                        tabHeader,
                        adapterSpecs,
                        adapterResolution,
                        matchedAdapter));
                }
            }
            else
            {
                var primarySpecs = new List<KeyValuePair<string, string>>();
                if (!string.IsNullOrWhiteSpace(net.PrimaryAdapterName)) primarySpecs.Add(new("Name", net.PrimaryAdapterName));
                if (!string.IsNullOrWhiteSpace(net.PrimaryAdapterDescription)) primarySpecs.Add(new("Description", net.PrimaryAdapterDescription));
                if (!string.IsNullOrWhiteSpace(net.PrimaryAdapterType)) primarySpecs.Add(new("Type", net.PrimaryAdapterType));
                if (!string.IsNullOrWhiteSpace(net.PrimaryIpv4)) primarySpecs.Add(new("IPv4", net.PrimaryIpv4));
                if (!string.IsNullOrWhiteSpace(net.PrimaryIpv6)) primarySpecs.Add(new("IPv6", net.PrimaryIpv6));
                if (!string.IsNullOrWhiteSpace(net.PrimaryLinkSpeed)) primarySpecs.Add(new("Link Speed", net.PrimaryLinkSpeed));
                if (!string.IsNullOrWhiteSpace(net.PrimaryGateway)) primarySpecs.Add(new("Gateway", net.PrimaryGateway));
                if (!string.IsNullOrWhiteSpace(net.PrimaryDns)) primarySpecs.Add(new("DNS", net.PrimaryDns));
                if (!string.IsNullOrWhiteSpace(net.PrimaryMacAddress)) primarySpecs.Add(new("MAC Address", FormatMacAddress(net.PrimaryMacAddress)));
                if (!string.IsNullOrWhiteSpace(net.NetConnectionStatus)) primarySpecs.Add(new("Status", net.NetConnectionStatus));
                if (primarySpecs.Count > 0)
                {
                    tabs.Add(new HardwareDetailTab(
                        "Primary",
                        primarySpecs,
                        iconResolution,
                        iconResolution.MatchedModel));
                }
            }

            var subtitle = net.PrimaryAdapterName ?? "Network Adapter";
            return new HardwareDetailPayload(
                "Network",
                subtitle,
                iconResolution,
                overview,
                iconResolution.MatchedModel,
                tabs);
        });
    }

    private static string TrimNetworkTabHeader(string adapterName)
    {
        var compact = Regex.Replace(adapterName, @"\s+", " ").Trim();
        return compact.Length <= 20 ? compact : $"{compact[..20]}...";
    }
}

public sealed class UsbDetailVM : HardwareDetailViewModelBase
{
    public UsbDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Usb)
    {
        Title = "USB Controllers";
        Subtitle = "Loading system data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Usb);
        IconSource = IconResolver.Resolve(HardwareType.Usb, null, null);

        BeginLoad(snapshot, cache =>
        {
            var usb = ResolveCached("USB", cache.Usb);
            var matchedUsb = HardwareKnowledgeDbService.Instance.MatchUsb(usb.PrimaryControllerName ?? string.Empty);
            var specs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(usb.PrimaryControllerName)) specs.Add(new("Primary Controller", usb.PrimaryControllerName));
            if (usb.RemovableDriveCount > 0) specs.Add(new("Removable Drives", usb.RemovableDriveCount.ToString()));
            if (usb.UsbControllerCount > 0) specs.Add(new("Controllers", usb.UsbControllerCount.ToString()));
            if (usb.UsbDeviceCount > 0) specs.Add(new("Total Devices", usb.UsbDeviceCount.ToString()));
            if (!string.IsNullOrWhiteSpace(usb.PrimaryUsbDeviceName)) specs.Add(new("Primary Device", usb.PrimaryUsbDeviceName));
            if (!string.IsNullOrWhiteSpace(usb.PrimaryVendorId)) specs.Add(new("Vendor ID", usb.PrimaryVendorId));
            if (!string.IsNullOrWhiteSpace(usb.PrimaryProductId)) specs.Add(new("Product ID", usb.PrimaryProductId));
            if (!string.IsNullOrWhiteSpace(usb.PrimaryStatus)) specs.Add(new("Status", usb.PrimaryStatus));
            if (!string.IsNullOrWhiteSpace(usb.PrimaryDeviceId)) specs.Add(new("Device ID", usb.PrimaryDeviceId));

            var subtitle = matchedUsb?.ModelName ?? usb.PrimaryControllerName ?? "USB Controller";
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Usb, usb.PrimaryControllerName, matchedUsb);
            return new HardwareDetailPayload("USB", subtitle, iconResolution, specs, matchedUsb);
        });
    }
}

public sealed class AudioDetailVM : HardwareDetailViewModelBase
{
    public AudioDetailVM(HardwareDetailSnapshot? snapshot = null)
        : base(HardwareType.Audio)
    {
        Title = "Audio Devices";
        Subtitle = "Loading audio data...";
        IconKey = IconResolver.GetDefaultKey(HardwareType.Audio);
        IconSource = IconResolver.Resolve(HardwareType.Audio, null, null);

        BeginLoad(snapshot, cache =>
        {
            var audio = ResolveCached("Audio", cache.Audio);
            var specs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(audio.PrimaryDeviceName)) specs.Add(new("Device", audio.PrimaryDeviceName));
            if (!string.IsNullOrWhiteSpace(audio.PrimaryManufacturer)) specs.Add(new("Manufacturer", audio.PrimaryManufacturer));
            if (!string.IsNullOrWhiteSpace(audio.PrimaryStatus)) specs.Add(new("Status", audio.PrimaryStatus));

            var subtitle = audio.PrimaryDeviceName ?? "Audio Device";
            var iconResolution = HardwareIconService.ResolveResult(HardwareType.Audio, audio.PrimaryDeviceName);
            return new HardwareDetailPayload("Audio", subtitle, iconResolution, specs);
        });
    }
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
            HardwareType.Display => new DisplaysDetailVM(snapshot),
            HardwareType.Network => new NetworkDetailVM(snapshot),
            HardwareType.Usb => new UsbDetailVM(snapshot),
            HardwareType.Audio => new AudioDetailVM(snapshot),
            _ => null
        };
    }
}
