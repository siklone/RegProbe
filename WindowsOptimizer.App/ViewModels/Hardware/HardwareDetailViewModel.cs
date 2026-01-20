using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WindowsOptimizer.Infrastructure.Hardware;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.ViewModels.Hardware;

public enum HardwareType
{
    Cpu,
    Gpu,
    Ram,
    Disk
}

public sealed class HardwareDetailViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly MetricDataBus? _bus;
    private readonly HardwareType _type;
    private readonly HardwareSpecsService _specsService = new();
    private bool _isDisposed;
    private double? _ramTotalGb;
    private double? _ramUsedGb;
    private double? _ramAvailableGb;
    private double? _diskReadBytes;
    private double? _diskWriteBytes;

    private string _windowTitle = "Hardware Details";
    private string _icon = "";
    private Brush _iconBackground = Brushes.Gray;
    private string _title = "";
    private string _subtitle = "";
    private string _primaryValue = "--";
    private string _primaryUnit = "";
    private Brush _primaryValueColor = Brushes.White;
    private Brush _statusColor = Brushes.LimeGreen;
    private string _statusText = "OK";

    private string _stat1Label = "";
    private string _stat1Value = "";
    private string _stat2Label = "";
    private string _stat2Value = "";
    private string _stat3Label = "";
    private string _stat3Value = "";
    private string _stat4Label = "";
    private string _stat4Value = "";

    private string _subItemsHeader = "";
    private string _footerText = "";

    public HardwareDetailViewModel(HardwareType type, MetricDataBus? bus = null)
    {
        _type = type;
        _bus = bus;

        CloseCommand = new RelayCommand(param =>
        {
            if (param is Window window)
            {
                window.Close();
            }
        });

        InitializeView(type);

        if (_bus != null)
        {
            _bus.MetricsUpdated += OnMetricsUpdated;
        }

        _ = LoadHardwareDetailsAsync(type);
    }

    #region Properties

    public string WindowTitle
    {
        get => _windowTitle;
        private set => SetProperty(ref _windowTitle, value);
    }

    public string Icon
    {
        get => _icon;
        private set => SetProperty(ref _icon, value);
    }

    public Brush IconBackground
    {
        get => _iconBackground;
        private set => SetProperty(ref _iconBackground, value);
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        private set => SetProperty(ref _subtitle, value);
    }

    public string PrimaryValue
    {
        get => _primaryValue;
        private set => SetProperty(ref _primaryValue, value);
    }

    public string PrimaryUnit
    {
        get => _primaryUnit;
        private set => SetProperty(ref _primaryUnit, value);
    }

    public Brush PrimaryValueColor
    {
        get => _primaryValueColor;
        private set => SetProperty(ref _primaryValueColor, value);
    }

    public Brush StatusColor
    {
        get => _statusColor;
        private set => SetProperty(ref _statusColor, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string Stat1Label
    {
        get => _stat1Label;
        private set => SetProperty(ref _stat1Label, value);
    }

    public string Stat1Value
    {
        get => _stat1Value;
        private set => SetProperty(ref _stat1Value, value);
    }

    public string Stat2Label
    {
        get => _stat2Label;
        private set => SetProperty(ref _stat2Label, value);
    }

    public string Stat2Value
    {
        get => _stat2Value;
        private set => SetProperty(ref _stat2Value, value);
    }

    public string Stat3Label
    {
        get => _stat3Label;
        private set => SetProperty(ref _stat3Label, value);
    }

    public string Stat3Value
    {
        get => _stat3Value;
        private set => SetProperty(ref _stat3Value, value);
    }

    public string Stat4Label
    {
        get => _stat4Label;
        private set => SetProperty(ref _stat4Label, value);
    }

    public string Stat4Value
    {
        get => _stat4Value;
        private set => SetProperty(ref _stat4Value, value);
    }

    public ObservableCollection<KeyValuePair<string, string>> Specifications { get; } = new();
    public ObservableCollection<KeyValuePair<string, string>> Details { get; } = new();
    public ObservableCollection<SubItemViewModel> SubItems { get; } = new();

    public bool HasDetails => Details.Count > 0;
    public bool HasSubItems => SubItems.Count > 0;

    public string SubItemsHeader
    {
        get => _subItemsHeader;
        private set => SetProperty(ref _subItemsHeader, value);
    }

    public string FooterText
    {
        get => _footerText;
        private set => SetProperty(ref _footerText, value);
    }

    public ICommand CloseCommand { get; }

    #endregion

    private void InitializeView(HardwareType type)
    {
        switch (type)
        {
            case HardwareType.Cpu:
                WindowTitle = "CPU Details";
                Icon = "\uD83D\uDCBB";
                IconBackground = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                Title = "CPU";
                PrimaryUnit = "%";
                StatusText = "Active";
                break;
            case HardwareType.Gpu:
                WindowTitle = "GPU Details";
                Icon = "\uD83C\uDFAE";
                IconBackground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                Title = "GPU";
                PrimaryUnit = "%";
                StatusText = "Active";
                break;
            case HardwareType.Ram:
                WindowTitle = "RAM Details";
                Icon = "\uD83D\uDCBE";
                IconBackground = new SolidColorBrush(Color.FromRgb(168, 85, 247));
                Title = "Memory";
                PrimaryUnit = "%";
                StatusText = "Active";
                break;
            case HardwareType.Disk:
                WindowTitle = "Storage Details";
                Icon = "\uD83D\uDCBE";
                IconBackground = new SolidColorBrush(Color.FromRgb(249, 115, 22));
                Title = "Storage";
                PrimaryUnit = "% used";
                StatusText = "Healthy";
                break;
        }
    }

    private async Task LoadHardwareDetailsAsync(HardwareType type)
    {
        switch (type)
        {
            case HardwareType.Cpu:
                await LoadCpuDetailsAsync();
                break;
            case HardwareType.Gpu:
                await LoadGpuDetailsAsync();
                break;
            case HardwareType.Ram:
                await LoadRamDetailsAsync();
                break;
            case HardwareType.Disk:
                await LoadDiskDetailsAsync();
                break;
        }

        await DispatchAsync(() =>
        {
            FooterText = $"Data collected at {DateTime.Now:HH:mm:ss}";
        });
    }

    private async Task LoadCpuDetailsAsync()
    {
        CpuIdentity cpu;
        try
        {
            cpu = await Task.Run(HardwareIdentifier.GetCpuId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await DispatchAsync(() =>
            {
                Subtitle = "Failed to load CPU information";
                StatusText = "Unavailable";
            });
            System.Diagnostics.Debug.WriteLine($"[HardwareDetailViewModel] CPU load failed: {ex.Message}");
            return;
        }

        await DispatchAsync(() =>
        {
            Subtitle = cpu.WmiName ?? "Unknown CPU";
            Stat1Label = "Cores";
            Stat1Value = cpu.Cores.ToString();
            Stat2Label = "Threads";
            Stat2Value = cpu.Threads.ToString();
            Stat3Label = "Clock";
            Stat3Value = cpu.MaxClockSpeed > 0 ? $"{cpu.MaxClockSpeed} MHz" : "N/A";
            Stat4Label = "Power";
            Stat4Value = "N/A";

            Specifications.Clear();
            Details.Clear();
            SubItems.Clear();

            Specifications.Add(new("Processor Name", cpu.WmiName ?? "N/A"));
            Specifications.Add(new("Manufacturer", cpu.Manufacturer ?? "N/A"));
            Specifications.Add(new("Physical Cores", cpu.Cores.ToString()));
            Specifications.Add(new("Logical Processors", cpu.Threads.ToString()));
            Specifications.Add(new("Max Clock Speed", cpu.MaxClockSpeed > 0 ? $"{cpu.MaxClockSpeed} MHz" : "N/A"));
            Specifications.Add(new("Architecture", cpu.Architecture ?? "N/A"));
            Specifications.Add(new("Family", cpu.Family > 0 ? cpu.Family.ToString() : "N/A"));

            Details.Add(new("Processor ID", cpu.ProcessorId ?? "N/A"));
            Details.Add(new("Lookup Key", cpu.LookupKey));

            SubItemsHeader = string.Empty;
            OnPropertyChanged(nameof(HasDetails));
            OnPropertyChanged(nameof(HasSubItems));
        });
    }

    private async Task LoadGpuDetailsAsync()
    {
        GpuIdentity gpu;
        try
        {
            gpu = await Task.Run(HardwareIdentifier.GetGpuId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await DispatchAsync(() =>
            {
                Subtitle = "Failed to load GPU information";
                StatusText = "Unavailable";
            });
            System.Diagnostics.Debug.WriteLine($"[HardwareDetailViewModel] GPU load failed: {ex.Message}");
            return;
        }

        await DispatchAsync(() =>
        {
            Subtitle = gpu.WmiName ?? gpu.DriverDesc ?? "Unknown GPU";
            Stat1Label = "VRAM";
            Stat1Value = gpu.AdapterRamGB > 0 ? $"{gpu.AdapterRamGB:F0} GB" : "N/A";
            Stat2Label = "Memory Used";
            Stat2Value = "N/A";
            Stat3Label = "Clock";
            Stat3Value = "N/A";
            Stat4Label = "Power";
            Stat4Value = "N/A";

            Specifications.Clear();
            Details.Clear();
            SubItems.Clear();

            Specifications.Add(new("Graphics Card", gpu.WmiName ?? "N/A"));
            Specifications.Add(new("Vendor", gpu.VendorName ?? "N/A"));
            Specifications.Add(new("Vendor ID", gpu.VendorId ?? "N/A"));
            Specifications.Add(new("Device ID", gpu.DeviceId ?? "N/A"));
            Specifications.Add(new("PCI ID", gpu.PciId ?? "N/A"));
            Specifications.Add(new("Adapter RAM", gpu.AdapterRamGB > 0 ? $"{gpu.AdapterRamGB:F1} GB" : "N/A"));
            Specifications.Add(new("Driver Version", gpu.DriverVersion ?? "N/A"));
            Specifications.Add(new("Video Processor", gpu.VideoProcessor ?? "N/A"));

            Details.Add(new("Driver Description", gpu.DriverDesc ?? "N/A"));
            Details.Add(new("PnP Device ID", gpu.PnpDeviceId ?? "N/A"));
            Details.Add(new("Lookup Key", gpu.LookupKey));

            SubItemsHeader = string.Empty;
            OnPropertyChanged(nameof(HasDetails));
            OnPropertyChanged(nameof(HasSubItems));
        });
    }

    private async Task LoadRamDetailsAsync()
    {
        RamIdentity ram;
        try
        {
            ram = await Task.Run(HardwareIdentifier.GetRamId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await DispatchAsync(() =>
            {
                Subtitle = "Failed to load RAM information";
                StatusText = "Unavailable";
            });
            System.Diagnostics.Debug.WriteLine($"[HardwareDetailViewModel] RAM load failed: {ex.Message}");
            return;
        }

        var moduleSpecs = new List<RamSpecs>();
        RamSpecs? primarySpec = null;
        foreach (var module in ram.Modules)
        {
            var spec = await _specsService.GetRamSpecsAsync(module, CancellationToken.None).ConfigureAwait(false);
            moduleSpecs.Add(spec);
            if (primarySpec == null || (!primarySpec.IsFromDatabase && spec.IsFromDatabase))
            {
                primarySpec = spec;
            }
        }

        await DispatchAsync(() =>
        {
            _ramTotalGb = ram.TotalCapacityGB;
            Subtitle = primarySpec?.Model ?? ram.LookupKey;
            Stat1Label = "Total";
            Stat1Value = $"{ram.TotalCapacityGB:F0} GB";
            Stat2Label = "Used";
            Stat2Value = "N/A";
            Stat3Label = "Free";
            Stat3Value = "N/A";
            Stat4Label = "Speed";
            var speedValue = primarySpec?.SpeedMhz ?? (ram.Modules.Count > 0 ? ram.Modules[0].SpeedMHz : 0);
            Stat4Value = speedValue > 0 ? $"{speedValue} MHz" : "N/A";

            Specifications.Clear();
            Details.Clear();
            SubItems.Clear();

            Specifications.Add(new("Total Capacity", $"{ram.TotalCapacityGB:F1} GB"));
            Specifications.Add(new("Installed Modules", ram.Modules.Count.ToString()));

            if (ram.Modules.Count > 0)
            {
                var first = ram.Modules[0];
                Specifications.Add(new("Memory Type", primarySpec?.Type ?? first.MemoryType ?? "N/A"));
                Specifications.Add(new("Speed", speedValue > 0 ? $"{speedValue} MHz" : "N/A"));
                Specifications.Add(new("Form Factor", first.FormFactor ?? "DIMM"));
            }

            if (primarySpec?.CasLatency is { } casLatency)
            {
                Specifications.Add(new("CAS Latency", $"CL{casLatency}"));
            }

            if (!string.IsNullOrWhiteSpace(primarySpec?.Timings))
            {
                Specifications.Add(new("Timings", primarySpec.Timings));
            }

            if (primarySpec?.Voltage is { } voltage)
            {
                Specifications.Add(new("Voltage", $"{voltage:0.00} V"));
            }

            if (primarySpec?.Ecc is { } ecc)
            {
                Specifications.Add(new("ECC", ecc ? "Yes" : "No"));
            }

            if (!string.IsNullOrWhiteSpace(primarySpec?.XmpProfiles))
            {
                Specifications.Add(new("XMP Profiles", primarySpec.XmpProfiles));
            }

            SubItemsHeader = "Memory Modules";
            var slot = 1;
            var moduleIndex = 0;
            foreach (var module in ram.Modules)
            {
                RamSpecs? moduleSpec = moduleIndex < moduleSpecs.Count ? moduleSpecs[moduleIndex] : null;
                moduleIndex++;

                var subItem = new SubItemViewModel
                {
                    Icon = "\uD83D\uDCBF",
                    Name = $"Slot {slot++}: {module.CapacityGB:F0} GB {module.MemoryType ?? "DDR4"}"
                };
                subItem.Properties.Add(new("Manufacturer", module.Manufacturer ?? "Unknown"));
                subItem.Properties.Add(new("Part Number", module.PartNumber ?? "N/A"));
                if (!string.IsNullOrWhiteSpace(moduleSpec?.Model))
                {
                    subItem.Properties.Add(new("Model", moduleSpec.Model));
                }
                subItem.Properties.Add(new("Speed", $"{module.SpeedMHz} MHz"));
                if (moduleSpec?.CasLatency is { } moduleCas)
                {
                    subItem.Properties.Add(new("CAS", $"CL{moduleCas}"));
                }
                if (!string.IsNullOrWhiteSpace(moduleSpec?.Timings))
                {
                    subItem.Properties.Add(new("Timings", moduleSpec.Timings));
                }
                if (moduleSpec?.Voltage is { } moduleVoltage)
                {
                    subItem.Properties.Add(new("Voltage", $"{moduleVoltage:0.00} V"));
                }
                if (moduleSpec?.Ecc is { } moduleEcc)
                {
                    subItem.Properties.Add(new("ECC", moduleEcc ? "Yes" : "No"));
                }
                subItem.Properties.Add(new("Capacity", $"{module.CapacityGB:F0} GB"));
                subItem.Properties.Add(new("Bank Label", module.BankLabel ?? "N/A"));
                subItem.Properties.Add(new("Device Locator", module.DeviceLocator ?? "N/A"));
                SubItems.Add(subItem);
            }

            OnPropertyChanged(nameof(HasDetails));
            OnPropertyChanged(nameof(HasSubItems));
        });
    }

    private async Task LoadDiskDetailsAsync()
    {
        List<StorageDriveInfo> disks;
        long totalSize;
        long totalUsed;

        try
        {
            var result = await Task.Run(() =>
            {
                var driveList = new List<StorageDriveInfo>();
                long size = 0;
                long used = 0;

                using var diskSearcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (var disk in diskSearcher.Get())
                {
                    var info = new StorageDriveInfo
                    {
                        Model = disk["Model"]?.ToString() ?? "Unknown",
                        InterfaceType = disk["InterfaceType"]?.ToString() ?? "Unknown",
                        MediaType = disk["MediaType"]?.ToString() ?? "Unknown",
                        SizeBytes = Convert.ToInt64(disk["Size"]),
                        Partitions = Convert.ToInt32(disk["Partitions"])
                    };
                    info.IsSsd = info.Model.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                                 info.Model.Contains("NVMe", StringComparison.OrdinalIgnoreCase) ||
                                 info.InterfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
                    size += info.SizeBytes;
                    driveList.Add(info);
                }

                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.DriveType == System.IO.DriveType.Fixed)
                    {
                        used += drive.TotalSize - drive.AvailableFreeSpace;
                    }
                }

                return (driveList, size, used);
            }).ConfigureAwait(false);

            disks = result.driveList;
            totalSize = result.size;
            totalUsed = result.used;
        }
        catch (Exception ex)
        {
            await DispatchAsync(() =>
            {
                Subtitle = "Failed to load storage information";
                StatusText = "Unavailable";
            });
            System.Diagnostics.Debug.WriteLine($"[HardwareDetailViewModel] Disk load failed: {ex.Message}");
            return;
        }

        var specsByModel = new Dictionary<string, StorageSpecs>(StringComparer.OrdinalIgnoreCase);
        foreach (var disk in disks)
        {
            var model = disk.Model ?? string.Empty;
            if (string.IsNullOrWhiteSpace(model))
            {
                continue;
            }

            if (!specsByModel.TryGetValue(model, out var specs))
            {
                specs = await _specsService.GetStorageSpecsAsync(model, CancellationToken.None).ConfigureAwait(false);
                specsByModel[model] = specs;
            }

            disk.Specs = specs;
        }

        await DispatchAsync(() =>
        {
            Subtitle = disks.Count == 1 ? disks[0].Model : $"{disks.Count} Drives";
            var usagePercent = totalSize > 0 ? (double)totalUsed / totalSize * 100 : 0;
            PrimaryValue = $"{usagePercent:F0}";
            PrimaryUnit = "% used";
            PrimaryValueColor = GetUsageBrush(usagePercent);
            StatusText = "Healthy";

            Stat1Label = "Total";
            Stat1Value = FormatSize(totalSize);
            Stat2Label = "Used";
            Stat2Value = FormatSize(totalUsed);
            Stat3Label = "Read";
            Stat3Value = "0 B/s";
            Stat4Label = "Write";
            Stat4Value = "0 B/s";

            Specifications.Clear();
            Details.Clear();
            SubItems.Clear();

            Specifications.Add(new("Total Storage", FormatSize(totalSize)));
            Specifications.Add(new("Used Space", FormatSize(totalUsed)));
            Specifications.Add(new("Free Space", FormatSize(totalSize - totalUsed)));
            Specifications.Add(new("Physical Drives", disks.Count.ToString()));
            var specMatches = disks.Count(d => d.Specs?.IsFromDatabase == true);
            if (specMatches > 0)
            {
                Specifications.Add(new("Specs Matches", $"{specMatches}/{disks.Count}"));
            }

            SubItemsHeader = "Drives";
            foreach (var disk in disks)
            {
                var subItem = new SubItemViewModel
                {
                    Icon = disk.IsSsd ? "\u26A1" : "\uD83D\uDCBF",
                    Name = disk.Model
                };
                var specType = disk.Specs?.Type ?? (disk.IsSsd ? "SSD" : "HDD");
                subItem.Properties.Add(new("Type", specType));
                var interfaceLabel = disk.Specs?.Interface ?? disk.InterfaceType;
                if (!string.IsNullOrWhiteSpace(interfaceLabel))
                {
                    subItem.Properties.Add(new("Interface", interfaceLabel));
                }
                if (!string.IsNullOrWhiteSpace(disk.Specs?.FormFactor))
                {
                    subItem.Properties.Add(new("Form Factor", disk.Specs.FormFactor));
                }
                subItem.Properties.Add(new("Size", FormatSize(disk.SizeBytes)));
                subItem.Properties.Add(new("Partitions", disk.Partitions.ToString()));
                if (disk.Specs?.SeqReadMbps is { } seqRead && seqRead > 0)
                {
                    subItem.Properties.Add(new("Seq Read", $"{seqRead} MB/s"));
                }
                if (disk.Specs?.SeqWriteMbps is { } seqWrite && seqWrite > 0)
                {
                    subItem.Properties.Add(new("Seq Write", $"{seqWrite} MB/s"));
                }
                if (disk.Specs?.TbwTb is { } tbw && tbw > 0)
                {
                    subItem.Properties.Add(new("TBW", $"{tbw} TB"));
                }
                SubItems.Add(subItem);
            }

            foreach (var drive in System.IO.DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == System.IO.DriveType.Fixed)
                {
                    var subItem = new SubItemViewModel
                    {
                        Icon = "\uD83D\uDCC1",
                        Name = $"{drive.Name} {drive.VolumeLabel}"
                    };
                    subItem.Properties.Add(new("File System", drive.DriveFormat));
                    subItem.Properties.Add(new("Total", FormatSize(drive.TotalSize)));
                    subItem.Properties.Add(new("Free", FormatSize(drive.AvailableFreeSpace)));
                    subItem.Properties.Add(new("Used", $"{(drive.TotalSize - drive.AvailableFreeSpace) * 100.0 / drive.TotalSize:F0}%"));
                    SubItems.Add(subItem);
                }
            }

            OnPropertyChanged(nameof(HasDetails));
            OnPropertyChanged(nameof(HasSubItems));
        });
    }

    private void OnMetricsUpdated(object? sender, MetricBatchEventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.InvokeAsync(() => HandleMetricsUpdated(e));
            return;
        }

        HandleMetricsUpdated(e);
    }

    private void HandleMetricsUpdated(MetricBatchEventArgs e)
    {
        switch (_type)
        {
            case HardwareType.Cpu:
                if (e.TryGetValue<double>("cpu.usage", out var cpuUsage))
                {
                    PrimaryValue = $"{cpuUsage:F0}";
                    PrimaryValueColor = GetUsageBrush(cpuUsage);
                }

                if (e.TryGetValue<double>("cpu.temp", out var cpuTemp))
                {
                    StatusText = $"{cpuTemp:F0}\u00B0C";
                    StatusColor = GetTempBrush(cpuTemp);
                }

                if (e.TryGetValue<double>("cpu.clock", out var cpuClock))
                {
                    Stat3Value = $"{cpuClock:F0} MHz";
                }

                if (e.TryGetValue<double>("cpu.power", out var cpuPower))
                {
                    Stat4Value = $"{cpuPower:F0} W";
                }
                break;
            case HardwareType.Gpu:
                if (e.TryGetValue<double>("gpu.usage", out var gpuUsage))
                {
                    PrimaryValue = $"{gpuUsage:F0}";
                    PrimaryValueColor = GetUsageBrush(gpuUsage);
                }

                if (e.TryGetValue<double>("gpu.temp", out var gpuTemp))
                {
                    StatusText = $"{gpuTemp:F0}\u00B0C";
                    StatusColor = GetTempBrush(gpuTemp);
                }

                if (e.TryGetValue<double>("gpu.memory.used", out var memUsedMb))
                {
                    var memUsedGb = memUsedMb / 1024.0;
                    Stat2Value = $"{memUsedGb:F1} GB";
                }

                if (e.TryGetValue<double>("gpu.clock", out var gpuClock))
                {
                    Stat3Value = $"{gpuClock:F0} MHz";
                }

                if (e.TryGetValue<double>("gpu.power", out var gpuPower))
                {
                    Stat4Value = $"{gpuPower:F0} W";
                }
                break;
            case HardwareType.Ram:
                if (e.TryGetValue<double>("ram.total", out var totalGb))
                {
                    _ramTotalGb = totalGb;
                    Stat1Value = $"{totalGb:F0} GB";
                }

                if (e.TryGetValue<double>("ram.used", out var usedGb))
                {
                    _ramUsedGb = usedGb;
                    UpdateRamStatusText();
                    UpdateRamStats();
                }

                if (e.TryGetValue<double>("ram.usage", out var ramUsage))
                {
                    PrimaryValue = $"{ramUsage:F0}";
                    PrimaryValueColor = GetUsageBrush(ramUsage);
                }

                if (e.TryGetValue<double>("ram.available", out var availableGb))
                {
                    _ramAvailableGb = availableGb;
                    UpdateRamStats();
                }
                break;
            case HardwareType.Disk:
                if (e.TryGetValue<double>("disk.read.speed", out var readBytes))
                {
                    _diskReadBytes = readBytes;
                    UpdateDiskStatusText();
                }

                if (e.TryGetValue<double>("disk.write.speed", out var writeBytes))
                {
                    _diskWriteBytes = writeBytes;
                    UpdateDiskStatusText();
                }

                if (e.TryGetValue<double>("disk.health", out var health))
                {
                    StatusColor = GetHealthBrush(health);
                }
                break;
        }
    }

    private void UpdateRamStatusText()
    {
        if (_ramTotalGb.HasValue && _ramUsedGb.HasValue)
        {
            StatusText = $"{_ramUsedGb.Value:F1} / {_ramTotalGb.Value:F1} GB";
        }
    }

    private void UpdateRamStats()
    {
        if (_ramUsedGb.HasValue)
        {
            Stat2Value = $"{_ramUsedGb.Value:F1} GB";
        }

        if (_ramAvailableGb.HasValue)
        {
            Stat3Value = $"{_ramAvailableGb.Value:F1} GB";
        }
    }

    private void UpdateDiskStatusText()
    {
        if (_diskReadBytes.HasValue || _diskWriteBytes.HasValue)
        {
            var readText = _diskReadBytes.HasValue ? FormatSpeed(_diskReadBytes.Value) : "0 B/s";
            var writeText = _diskWriteBytes.HasValue ? FormatSpeed(_diskWriteBytes.Value) : "0 B/s";
            StatusText = $"R: {readText} / W: {writeText}";
            Stat3Value = readText;
            Stat4Value = writeText;
        }
    }

    private static Brush GetUsageBrush(double value) => value switch
    {
        >= 90 => Brushes.Red,
        >= 70 => Brushes.OrangeRed,
        >= 50 => Brushes.Orange,
        _ => Brushes.LimeGreen
    };

    private static Brush GetTempBrush(double temp) => temp switch
    {
        >= 90 => Brushes.Red,
        >= 80 => Brushes.OrangeRed,
        >= 70 => Brushes.Orange,
        >= 60 => Brushes.Yellow,
        _ => Brushes.LimeGreen
    };

    private static Brush GetHealthBrush(double health) => health switch
    {
        >= 90 => Brushes.LimeGreen,
        >= 70 => Brushes.YellowGreen,
        >= 50 => Brushes.Orange,
        _ => Brushes.Red
    };

    private static string FormatSize(long bytes)
    {
        const long kilo = 1024;
        const long mega = kilo * 1024;
        const long giga = mega * 1024;
        const long tera = giga * 1024;

        if (bytes >= tera)
            return $"{bytes / (double)tera:F1} TB";
        if (bytes >= giga)
            return $"{bytes / (double)giga:F0} GB";
        if (bytes >= mega)
            return $"{bytes / (double)mega:F0} MB";
        return $"{bytes / (double)kilo:F0} KB";
    }

    private static string FormatSpeed(double bytesPerSec)
    {
        const double kilo = 1024.0;
        const double mega = kilo * 1024.0;
        const double giga = mega * 1024.0;

        if (bytesPerSec >= giga)
            return $"{bytesPerSec / giga:F1} GB/s";
        if (bytesPerSec >= mega)
            return $"{bytesPerSec / mega:F0} MB/s";
        if (bytesPerSec >= kilo)
            return $"{bytesPerSec / kilo:F0} KB/s";
        return $"{bytesPerSec:F0} B/s";
    }

    private Task DispatchAsync(Action action)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_bus != null)
        {
            _bus.MetricsUpdated -= OnMetricsUpdated;
        }
    }

    #endregion
}

public class SubItemViewModel
{
    public string Icon { get; set; } = "";
    public string Name { get; set; } = "";
    public ObservableCollection<KeyValuePair<string, string>> Properties { get; } = new();
}
