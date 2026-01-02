using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Metrics;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MonitorViewModel : ViewModelBase, IDisposable
{
    private static readonly MonitorSectionDefinition[] DefaultSections =
    [
        new("system-info", "System Information", "OS, CPU, GPU, RAM, uptime."),
        new("stat-cards", "Usage Summary Cards", "CPU, RAM, GPU usage and temperatures."),
        new("cpu-ram-history", "CPU & RAM History", "60-second usage history charts."),
        new("network-disk-io", "Network & Disk I/O", "60-second throughput charts."),
        new("top-cpu-ram", "Top CPU/RAM Processes", "Top processes by CPU and RAM usage."),
        new("top-network", "Top Network Processes", "Top processes by network throughput."),
        new("top-disk", "Top Disk Processes", "Top processes by disk I/O throughput."),
        new("network-adapters", "Network Adapters", "Per-adapter throughput and totals."),
        new("disk-activity", "Disk Activity", "Per-disk throughput and usage.")
    ];

    private readonly MetricProvider? _metricProvider;
    private readonly ProcessMonitor? _processMonitor;
    private readonly NetworkMonitor? _networkMonitor;
    private readonly DiskMonitor? _diskMonitor;
    private readonly NetworkLatencyMonitor? _latencyMonitor;
    private readonly WifiSignalMonitor? _wifiSignalMonitor;
    private readonly DispatcherTimer? _updateTimer;
    private readonly SettingsStore _settingsStore;

    private double _cpuUsage;
    private double _ramUsedGb;
    private double _ramTotalGb;
    private double _cpuTemp = double.NaN;
    private double _gpuTemp = double.NaN;
    private double _gpuUsage;
    private double _gpuMemoryUsedMb;
    private double _gpuMemoryTotalMb;
    private double _gpuMemoryUsagePercent;
    private bool _hasGpuMemory;
    private double _cpuFanRpm = double.NaN;
    private double _gpuFanRpm = double.NaN;
    private double? _diskHealthPercent;
    private bool? _diskPredictFailure;
    private double? _networkLatencyMs;
    private string _networkLatencyTarget = string.Empty;
    private double? _cloudflareLatencyMs;
    private double? _googleLatencyMs;
    private int? _wifiSignalQuality;
    private string _wifiSsid = string.Empty;
    private SystemInfo? _systemInfo;
    private double _cpuAlertThreshold = 90.0;
    private double _ramAlertThreshold = 90.0;
    private bool _isCpuAlertActive;
    private bool _isRamAlertActive;
    private ProcessMonitor.NetworkProcessMode _networkProcessMode = ProcessMonitor.NetworkProcessMode.ApproximateIo;
    private bool _isDisposed;
    private bool _isLayoutEditorVisible;
    private bool _isLayoutLoading;
    private DateTime _nextAuxSampleUtc = DateTime.MinValue;
    private bool _isAuxSampleInProgress;
    private static readonly TimeSpan AuxSampleInterval = TimeSpan.FromSeconds(5);

    private readonly RelayCommand _toggleLayoutEditorCommand;
    private readonly RelayCommand _moveSectionUpCommand;
    private readonly RelayCommand _moveSectionDownCommand;
    private readonly RelayCommand _resetLayoutCommand;

    public MonitorViewModel()
    {
        try
        {
            var paths = AppPaths.FromEnvironment();
            _settingsStore = new SettingsStore(paths);
            _toggleLayoutEditorCommand = new RelayCommand(_ => IsLayoutEditorVisible = !IsLayoutEditorVisible);
            _moveSectionUpCommand = new RelayCommand(param => MoveSection(param, -1), param => CanMoveSection(param, -1));
            _moveSectionDownCommand = new RelayCommand(param => MoveSection(param, 1), param => CanMoveSection(param, 1));
            _resetLayoutCommand = new RelayCommand(_ => ResetLayout());
            InitializeLayout();
            MonitorSections.CollectionChanged += OnMonitorSectionsChanged;

            // Initialize monitors safely
            try
            {
                _metricProvider = new MetricProvider();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create MetricProvider: {ex.Message}");
            }

            try
            {
                _processMonitor = new ProcessMonitor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create ProcessMonitor: {ex.Message}");
            }

            try
            {
                _networkMonitor = new NetworkMonitor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create NetworkMonitor: {ex.Message}");
            }

            try
            {
                _diskMonitor = new DiskMonitor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create DiskMonitor: {ex.Message}");
            }

            try
            {
                _latencyMonitor = new NetworkLatencyMonitor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create NetworkLatencyMonitor: {ex.Message}");
            }

            try
            {
                _wifiSignalMonitor = new WifiSignalMonitor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create WifiSignalMonitor: {ex.Message}");
            }

            // Initialize collections
            CpuHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            RamHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            NetworkUploadHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            NetworkDownloadHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            DiskReadHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            DiskWriteHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            TopProcessesByCpu = new ObservableCollection<ProcessInfo>();
            TopProcessesByRam = new ObservableCollection<ProcessInfo>();
            TopProcessesByNetwork = new ObservableCollection<ProcessInfo>();
            TopProcessesByDisk = new ObservableCollection<ProcessInfo>();
            NetworkAdapters = new ObservableCollection<NetworkAdapterInfo>();
            Disks = new ObservableCollection<DiskInfo>();

            // Initialize process management commands
            KillProcessCommand = new RelayCommand(param =>
            {
                if (param is ProcessInfo process && _processMonitor != null)
                {
                    _processMonitor.KillProcess(process.Pid);
                }
            });

            SuspendProcessCommand = new RelayCommand(param =>
            {
                if (param is ProcessInfo process && _processMonitor != null)
                {
                    _processMonitor.SuspendProcess(process.Pid);
                }
            });

            ResumeProcessCommand = new RelayCommand(param =>
            {
                if (param is ProcessInfo process && _processMonitor != null)
                {
                    _processMonitor.ResumeProcess(process.Pid);
                }
            });

            ExportMetricsCommand = new RelayCommand(_ => ExportMetricsToCsv());

            // Get initial total RAM and system info (with error handling)
            try
            {
                RamTotalGb = _metricProvider?.GetTotalRamGb() ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get total RAM: {ex.Message}");
                RamTotalGb = 0;
            }

            try
            {
                SystemInfo = _metricProvider?.GetSystemInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get system info: {ex.Message}");
                SystemInfo = null;
            }

            // Timer: 1 second refresh
            try
            {
                _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _updateTimer.Tick += OnUpdateTick;
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start timer: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel initialization failed: {ex.Message}");

            // Ensure collections are initialized even if other initialization fails
            CpuHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            RamHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            NetworkUploadHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            NetworkDownloadHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            DiskReadHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            DiskWriteHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            TopProcessesByCpu ??= new ObservableCollection<ProcessInfo>();
            TopProcessesByRam ??= new ObservableCollection<ProcessInfo>();
            TopProcessesByNetwork ??= new ObservableCollection<ProcessInfo>();
            TopProcessesByDisk ??= new ObservableCollection<ProcessInfo>();
            NetworkAdapters ??= new ObservableCollection<NetworkAdapterInfo>();
            Disks ??= new ObservableCollection<DiskInfo>();

            // Initialize commands if they weren't created
            KillProcessCommand ??= new RelayCommand(_ => { });
            SuspendProcessCommand ??= new RelayCommand(_ => { });
            ResumeProcessCommand ??= new RelayCommand(_ => { });
            ExportMetricsCommand ??= new RelayCommand(_ => { });
            _toggleLayoutEditorCommand = new RelayCommand(_ => { });
            _moveSectionUpCommand = new RelayCommand(_ => { });
            _moveSectionDownCommand = new RelayCommand(_ => { });
            _resetLayoutCommand = new RelayCommand(_ => { });

            if (MonitorSections.Count == 0)
            {
                foreach (var section in BuildDefaultSections())
                {
                    MonitorSections.Add(section);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_updateTimer != null)
        {
            _updateTimer.Stop();
            _updateTimer.Tick -= OnUpdateTick;
        }

        _metricProvider?.Dispose();
        _processMonitor?.Dispose();
        _networkMonitor?.Dispose();
        _diskMonitor?.Dispose();
        _latencyMonitor?.Dispose();
        _wifiSignalMonitor?.Dispose();
    }

    private void ExportMetricsToCsv()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"SystemMetrics_{timestamp}.csv";
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filepath = Path.Combine(desktop, filename);

            var csv = new StringBuilder();
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"CPU Usage,{CpuUsage:F2}%");
            csv.AppendLine($"RAM Usage,{RamUsedGb:F2} GB / {RamTotalGb:F2} GB ({RamUsagePercent:F2}%)");
            csv.AppendLine($"CPU Temperature,{(HasCpuTemp ? $"{CpuTemp:F1}°C" : "N/A")}");
            csv.AppendLine($"GPU Usage,{GpuUsage:F2}%");
            csv.AppendLine($"GPU Temperature,{(HasGpuTemp ? $"{GpuTemp:F1}°C" : "N/A")}");
            csv.AppendLine($"GPU VRAM,{(HasGpuMemory ? $"{GpuMemoryUsedGb:F1} GB / {GpuMemoryTotalGb:F1} GB ({GpuMemoryUsagePercent:F0}%)" : "N/A")}");
            csv.AppendLine($"CPU Fan,{CpuFanRpmText}");
            csv.AppendLine($"GPU Fan,{GpuFanRpmText}");
            csv.AppendLine($"Disk Health,{DiskHealthText}");
            var gatewayLabel = string.IsNullOrWhiteSpace(NetworkLatencyTarget)
                ? "Gateway"
                : $"Gateway ({NetworkLatencyTarget})";
            csv.AppendLine($"Latency {gatewayLabel},{GatewayLatencyText}");
            csv.AppendLine($"Latency Cloudflare (1.1.1.1),{CloudflareLatencyText}");
            csv.AppendLine($"Latency Google (8.8.8.8),{GoogleLatencyText}");
            csv.AppendLine($"Wi-Fi Signal,{WifiSignalText} {WifiSignalDetail}".Trim());
            csv.AppendLine();
            csv.AppendLine("System Information");
            csv.AppendLine($"OS,{SystemInfo?.OsName}");
            csv.AppendLine($"OS Version,{SystemInfo?.OsVersion}");
            csv.AppendLine($"CPU,{SystemInfo?.CpuName}");
            csv.AppendLine($"CPU Cores,{SystemInfo?.CpuCores}");
            csv.AppendLine($"CPU Threads,{SystemInfo?.CpuThreads}");
            csv.AppendLine($"GPU,{SystemInfo?.GpuName}");
            csv.AppendLine($"Total RAM,{SystemInfo?.TotalRamGb:F2} GB");
            csv.AppendLine($"Uptime,{SystemInfo?.UptimeFormatted}");
            csv.AppendLine();
            csv.AppendLine("Top Processes by CPU");
            csv.AppendLine("Name,PID,CPU %,RAM MB,Threads,Handles");
            foreach (var proc in TopProcessesByCpu)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.CpuPercent:F2},{proc.RamMb:F0},{proc.Threads},{proc.Handles}");
            }
            csv.AppendLine();
            csv.AppendLine("Top Processes by RAM");
            csv.AppendLine("Name,PID,RAM MB,Threads,Handles");
            foreach (var proc in TopProcessesByRam)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.RamMb:F0},{proc.Threads},{proc.Handles}");
            }

            csv.AppendLine();
            csv.AppendLine(NetworkProcessTitle);
            csv.AppendLine("Name,PID,Mbps,Threads,Handles");
            foreach (var proc in TopProcessesByNetwork)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.IoMbps:F2},{proc.Threads},{proc.Handles}");
            }

            csv.AppendLine();
            csv.AppendLine("Top Processes by Disk I/O");
            csv.AppendLine("Name,PID,MBps,Threads,Handles");
            foreach (var proc in TopProcessesByDisk)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.DiskMBps:F2},{proc.Threads},{proc.Handles}");
            }

            File.WriteAllText(filepath, csv.ToString());
            System.Diagnostics.Debug.WriteLine($"Metrics exported to: {filepath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
        }
    }

    private void InitializeLayout()
    {
        _isLayoutLoading = true;
        MonitorSections.Clear();

        foreach (var section in BuildDefaultSections())
        {
            section.PropertyChanged += OnSectionLayoutChanged;
            MonitorSections.Add(section);
        }

        _isLayoutLoading = false;
        UpdateLayoutCommands();
        _ = LoadSavedLayoutAsync();
    }

    private static List<MonitorSectionLayout> BuildDefaultSections()
    {
        return DefaultSections
            .Select(def => new MonitorSectionLayout(def.Key, def.Title, def.Description))
            .ToList();
    }

    private async Task LoadSavedLayoutAsync()
    {
        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None).ConfigureAwait(false);
            if (settings.MonitorSections.Count == 0)
            {
                return;
            }

            var defaults = BuildDefaultSections();
            var layout = BuildLayoutFromSettings(defaults, settings.MonitorSections);
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
            {
                ApplyLayout(layout);
                return;
            }

            await dispatcher.InvokeAsync(() => ApplyLayout(layout));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Monitor layout load failed: {ex.Message}");
        }
    }

    private static List<MonitorSectionLayout> BuildLayoutFromSettings(
        IReadOnlyList<MonitorSectionLayout> defaults,
        IReadOnlyList<MonitorSectionState> saved)
    {
        var defaultByKey = defaults.ToDictionary(section => section.Key, StringComparer.OrdinalIgnoreCase);
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<MonitorSectionLayout>();

        foreach (var state in saved.OrderBy(state => state.Order))
        {
            if (string.IsNullOrWhiteSpace(state.Key))
            {
                continue;
            }

            if (!defaultByKey.TryGetValue(state.Key, out var definition))
            {
                continue;
            }

            var section = new MonitorSectionLayout(definition.Key, definition.Title, definition.Description)
            {
                IsVisible = state.IsVisible
            };
            ordered.Add(section);
            usedKeys.Add(definition.Key);
        }

        foreach (var definition in defaults)
        {
            if (!usedKeys.Contains(definition.Key))
            {
                ordered.Add(new MonitorSectionLayout(definition.Key, definition.Title, definition.Description));
            }
        }

        return ordered;
    }

    private void ApplyLayout(IEnumerable<MonitorSectionLayout> layout)
    {
        _isLayoutLoading = true;
        foreach (var section in MonitorSections)
        {
            section.PropertyChanged -= OnSectionLayoutChanged;
        }

        MonitorSections.Clear();
        foreach (var section in layout)
        {
            section.PropertyChanged += OnSectionLayoutChanged;
            MonitorSections.Add(section);
        }

        _isLayoutLoading = false;
        UpdateLayoutCommands();
    }

    private void OnSectionLayoutChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLayoutLoading)
        {
            return;
        }

        if (e.PropertyName == nameof(MonitorSectionLayout.IsVisible))
        {
            _ = SaveLayoutAsync();
        }
    }

    private bool CanMoveSection(object? parameter, int direction)
    {
        if (parameter is not MonitorSectionLayout section)
        {
            return false;
        }

        var index = MonitorSections.IndexOf(section);
        if (index < 0)
        {
            return false;
        }

        var nextIndex = index + direction;
        return nextIndex >= 0 && nextIndex < MonitorSections.Count;
    }

    private void MoveSection(object? parameter, int direction)
    {
        if (parameter is not MonitorSectionLayout section)
        {
            return;
        }

        var index = MonitorSections.IndexOf(section);
        var nextIndex = index + direction;
        if (nextIndex < 0 || nextIndex >= MonitorSections.Count)
        {
            return;
        }

        MonitorSections.Move(index, nextIndex);
        UpdateLayoutCommands();
        _ = SaveLayoutAsync();
    }

    private void UpdateLayoutCommands()
    {
        _moveSectionUpCommand.RaiseCanExecuteChanged();
        _moveSectionDownCommand.RaiseCanExecuteChanged();
    }

    private void OnMonitorSectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isLayoutLoading)
        {
            return;
        }

        UpdateLayoutCommands();
        _ = SaveLayoutAsync();
    }

    private void ResetLayout()
    {
        ApplyLayout(BuildDefaultSections());
        _ = SaveLayoutAsync();
    }

    private async Task SaveLayoutAsync()
    {
        if (_isLayoutLoading)
        {
            return;
        }

        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            settings.MonitorSections = MonitorSections
                .Select((section, index) => new MonitorSectionState
                {
                    Key = section.Key,
                    Order = index,
                    IsVisible = section.IsVisible
                })
                .ToList();
            await _settingsStore.SaveAsync(settings, CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Monitor layout save failed: {ex.Message}");
        }
    }

    public string Title => "Monitor";

    public ObservableCollection<MonitorSectionLayout> MonitorSections { get; } = new();

    public bool IsLayoutEditorVisible
    {
        get => _isLayoutEditorVisible;
        set => SetProperty(ref _isLayoutEditorVisible, value);
    }

    public ICommand ToggleLayoutEditorCommand => _toggleLayoutEditorCommand;
    public ICommand MoveSectionUpCommand => _moveSectionUpCommand;
    public ICommand MoveSectionDownCommand => _moveSectionDownCommand;
    public ICommand ResetLayoutCommand => _resetLayoutCommand;
    public string NetworkProcessTitle => _networkProcessMode switch
    {
        ProcessMonitor.NetworkProcessMode.TcpUdpEtw => "Top 10 Processes by Network",
        ProcessMonitor.NetworkProcessMode.TcpOnly => "Top 10 Processes by Network (TCP only)",
        _ => "Top 10 Processes by Network (Approx.)"
    };

    public string NetworkProcessSubtitle => _networkProcessMode switch
    {
        ProcessMonitor.NetworkProcessMode.TcpUdpEtw => "Based on TCP + UDP bytes via ETW.",
        ProcessMonitor.NetworkProcessMode.TcpOnly => "Based on TCP EStats (UDP not included).",
        _ => "Based on total process I/O bytes. May include disk activity."
    };

    public ObservableCollection<double> CpuHistory { get; }
    public ObservableCollection<double> RamHistory { get; }
    public ObservableCollection<double> NetworkUploadHistory { get; }
    public ObservableCollection<double> NetworkDownloadHistory { get; }
    public ObservableCollection<double> DiskReadHistory { get; }
    public ObservableCollection<double> DiskWriteHistory { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByCpu { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByRam { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByNetwork { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByDisk { get; }
    public ObservableCollection<NetworkAdapterInfo> NetworkAdapters { get; }
    public ObservableCollection<DiskInfo> Disks { get; }

    public double CpuUsage
    {
        get => _cpuUsage;
        private set => SetProperty(ref _cpuUsage, value);
    }

    public double RamUsedGb
    {
        get => _ramUsedGb;
        private set => SetProperty(ref _ramUsedGb, value);
    }

    public double RamTotalGb
    {
        get => _ramTotalGb;
        private set => SetProperty(ref _ramTotalGb, value);
    }

    public double RamUsagePercent => RamTotalGb > 0 ? (RamUsedGb / RamTotalGb) * 100 : 0;

    public double CpuTemp
    {
        get => _cpuTemp;
        private set
        {
            if (SetProperty(ref _cpuTemp, value))
            {
                OnPropertyChanged(nameof(HasCpuTemp));
                OnPropertyChanged(nameof(CpuTempText));
            }
        }
    }

    public bool HasCpuTemp => double.IsFinite(CpuTemp) && CpuTemp > 0;

    public bool IsCpuTempAvailable => HasCpuTemp;

    public string CpuTempText => HasCpuTemp ? $"{CpuTemp:F0}°C" : "N/A";

    // Chart Min/Max values for better readability
    public double CpuHistoryMax => CpuHistory.Count > 0 ? CpuHistory.Max() : 0;
    public double CpuHistoryMin => CpuHistory.Count > 0 ? CpuHistory.Min() : 0;
    public double RamHistoryMax => RamHistory.Count > 0 ? RamHistory.Max() : 0;
    public double RamHistoryMin => RamHistory.Count > 0 ? RamHistory.Min() : 0;
    public double CpuHistoryScaleMax => 100.0;
    public double CpuHistoryScaleThreeQuarter => 75.0;
    public double CpuHistoryScaleMid => 50.0;
    public double CpuHistoryScaleQuarter => 25.0;
    public double RamHistoryScaleMax => 100.0;
    public double RamHistoryScaleThreeQuarter => 75.0;
    public double RamHistoryScaleMid => 50.0;
    public double RamHistoryScaleQuarter => 25.0;
    public double NetworkDownloadMax => GetHistoryMax(NetworkDownloadHistory);
    public double NetworkDownloadMin => GetHistoryMin(NetworkDownloadHistory);
    public double NetworkDownloadNow => GetHistoryNow(NetworkDownloadHistory);
    public double NetworkUploadMax => GetHistoryMax(NetworkUploadHistory);
    public double NetworkUploadMin => GetHistoryMin(NetworkUploadHistory);
    public double NetworkUploadNow => GetHistoryNow(NetworkUploadHistory);
    public double NetworkIoScaleMax => Math.Max(NetworkDownloadMax, NetworkUploadMax);
    public double NetworkIoScaleMid => NetworkIoScaleMax / 2.0;
    public double DiskReadMax => GetHistoryMax(DiskReadHistory);
    public double DiskReadMin => GetHistoryMin(DiskReadHistory);
    public double DiskReadNow => GetHistoryNow(DiskReadHistory);
    public double DiskWriteMax => GetHistoryMax(DiskWriteHistory);
    public double DiskWriteMin => GetHistoryMin(DiskWriteHistory);
    public double DiskWriteNow => GetHistoryNow(DiskWriteHistory);
    public double DiskIoScaleMax => Math.Max(DiskReadMax, DiskWriteMax);
    public double DiskIoScaleMid => DiskIoScaleMax / 2.0;

    public double GpuTemp
    {
        get => _gpuTemp;
        private set
        {
            if (SetProperty(ref _gpuTemp, value))
            {
                OnPropertyChanged(nameof(HasGpuTemp));
                OnPropertyChanged(nameof(GpuTempText));
            }
        }
    }

    public bool HasGpuTemp => double.IsFinite(GpuTemp) && GpuTemp > 0;

    public string GpuTempText => HasGpuTemp ? $"{GpuTemp:F0}°C" : "N/A";

    public double GpuUsage
    {
        get => _gpuUsage;
        private set => SetProperty(ref _gpuUsage, value);
    }

    public double GpuMemoryUsedMb
    {
        get => _gpuMemoryUsedMb;
        private set => SetProperty(ref _gpuMemoryUsedMb, value);
    }

    public double GpuMemoryTotalMb
    {
        get => _gpuMemoryTotalMb;
        private set => SetProperty(ref _gpuMemoryTotalMb, value);
    }

    public double GpuMemoryUsagePercent
    {
        get => _gpuMemoryUsagePercent;
        private set => SetProperty(ref _gpuMemoryUsagePercent, value);
    }

    public bool HasGpuMemory
    {
        get => _hasGpuMemory;
        private set => SetProperty(ref _hasGpuMemory, value);
    }

    public double GpuMemoryUsedGb => GpuMemoryUsedMb / 1024.0;

    public double GpuMemoryTotalGb => GpuMemoryTotalMb / 1024.0;

    public string GpuMemoryUsageText => HasGpuMemory
        ? $"{GpuMemoryUsedGb:F1} / {GpuMemoryTotalGb:F1} GB"
        : "N/A";

    public string GpuMemoryPercentText => HasGpuMemory
        ? $"{GpuMemoryUsagePercent:F0}%"
        : "N/A";

    public double CpuFanRpm
    {
        get => _cpuFanRpm;
        private set => SetProperty(ref _cpuFanRpm, value);
    }

    public double GpuFanRpm
    {
        get => _gpuFanRpm;
        private set => SetProperty(ref _gpuFanRpm, value);
    }

    public bool HasCpuFan => double.IsFinite(CpuFanRpm) && CpuFanRpm > 0;

    public bool HasGpuFan => double.IsFinite(GpuFanRpm) && GpuFanRpm > 0;

    public string CpuFanRpmText => HasCpuFan ? $"{CpuFanRpm:F0} RPM" : "N/A";

    public string GpuFanRpmText => HasGpuFan ? $"{GpuFanRpm:F0} RPM" : "N/A";

    public double? DiskHealthPercent
    {
        get => _diskHealthPercent;
        private set => SetProperty(ref _diskHealthPercent, value);
    }

    public bool? DiskPredictFailure
    {
        get => _diskPredictFailure;
        private set => SetProperty(ref _diskPredictFailure, value);
    }

    public string DiskHealthText => DiskHealthPercent.HasValue
        ? $"{DiskHealthPercent.Value:F0}%"
        : DiskPredictFailure == true
            ? "Warning"
            : DiskPredictFailure == false
                ? "OK"
                : "N/A";

    public string DiskHealthDetail => DiskHealthPercent.HasValue
        ? "SMART remaining life"
        : DiskPredictFailure == true
            ? "SMART predicts failure"
            : DiskPredictFailure == false
                ? "Drive health status OK"
                : "SMART data unavailable";

    public double? NetworkLatencyMs
    {
        get => _networkLatencyMs;
        private set => SetProperty(ref _networkLatencyMs, value);
    }

    public string NetworkLatencyTarget
    {
        get => _networkLatencyTarget;
        private set => SetProperty(ref _networkLatencyTarget, value);
    }

    public double? CloudflareLatencyMs
    {
        get => _cloudflareLatencyMs;
        private set => SetProperty(ref _cloudflareLatencyMs, value);
    }

    public double? GoogleLatencyMs
    {
        get => _googleLatencyMs;
        private set => SetProperty(ref _googleLatencyMs, value);
    }

    public string NetworkLatencyText => NetworkLatencyMs.HasValue
        ? $"{NetworkLatencyMs.Value:F0} ms"
        : "N/A";

    public string NetworkLatencyDetail => string.IsNullOrWhiteSpace(NetworkLatencyTarget)
        ? "Gateway ping"
        : $"Gateway {NetworkLatencyTarget}";

    public string GatewayLatencyText => NetworkLatencyText;

    public string GatewayLatencyLabel => string.IsNullOrWhiteSpace(NetworkLatencyTarget)
        ? "Gateway"
        : $"Gateway ({NetworkLatencyTarget})";

    public string CloudflareLatencyText => CloudflareLatencyMs.HasValue
        ? $"{CloudflareLatencyMs.Value:F0} ms"
        : "N/A";

    public string GoogleLatencyText => GoogleLatencyMs.HasValue
        ? $"{GoogleLatencyMs.Value:F0} ms"
        : "N/A";

    public int? WifiSignalQuality
    {
        get => _wifiSignalQuality;
        private set => SetProperty(ref _wifiSignalQuality, value);
    }

    public string WifiSsid
    {
        get => _wifiSsid;
        private set => SetProperty(ref _wifiSsid, value);
    }

    public string WifiSignalText => WifiSignalQuality.HasValue
        ? $"{WifiSignalQuality.Value}%"
        : "N/A";

    public string WifiSignalDetail => string.IsNullOrWhiteSpace(WifiSsid)
        ? "Not connected"
        : WifiSsid;

    public SystemInfo? SystemInfo
    {
        get => _systemInfo;
        private set => SetProperty(ref _systemInfo, value);
    }

    public double CpuAlertThreshold
    {
        get => _cpuAlertThreshold;
        set => SetProperty(ref _cpuAlertThreshold, value);
    }

    public double RamAlertThreshold
    {
        get => _ramAlertThreshold;
        set => SetProperty(ref _ramAlertThreshold, value);
    }

    public bool IsCpuAlertActive
    {
        get => _isCpuAlertActive;
        private set => SetProperty(ref _isCpuAlertActive, value);
    }

    public bool IsRamAlertActive
    {
        get => _isRamAlertActive;
        private set => SetProperty(ref _isRamAlertActive, value);
    }

    public ICommand KillProcessCommand { get; }
    public ICommand SuspendProcessCommand { get; }
    public ICommand ResumeProcessCommand { get; }
    public ICommand ExportMetricsCommand { get; }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        try
        {
            // Update system metrics (with null checks)
            if (_metricProvider != null)
            {
                CpuUsage = _metricProvider.GetCpuUsage();
                RamUsedGb = _metricProvider.GetUsedRamGb();
                var cpuTemp = _metricProvider.GetCpuTemperature();
                CpuTemp = double.IsFinite(cpuTemp) && cpuTemp > 0 ? cpuTemp : double.NaN;

                var gpuTemp = _metricProvider.GetGpuTemperature();
                GpuTemp = double.IsFinite(gpuTemp) && gpuTemp > 0 ? gpuTemp : double.NaN;
                GpuUsage = _metricProvider.GetGpuUsage();
            }

            // Update history (60 second sliding window)
            UpdateHistory(CpuHistory, CpuUsage);
            UpdateHistory(RamHistory, RamUsagePercent);

            // Notify Min/Max updates for chart labels
            OnPropertyChanged(nameof(CpuHistoryMax));
            OnPropertyChanged(nameof(CpuHistoryMin));
            OnPropertyChanged(nameof(RamHistoryMax));
            OnPropertyChanged(nameof(RamHistoryMin));

            // Update network and disk I/O history
            if (_networkMonitor != null)
            {
                var networkAdapters = _networkMonitor.GetActiveAdapters();
                var totalUpload = networkAdapters.Sum(a => a.SendMbps);
                var totalDownload = networkAdapters.Sum(a => a.ReceiveMbps);
                UpdateHistory(NetworkUploadHistory, totalUpload);
                UpdateHistory(NetworkDownloadHistory, totalDownload);
                OnPropertyChanged(nameof(NetworkDownloadMax));
                OnPropertyChanged(nameof(NetworkDownloadMin));
                OnPropertyChanged(nameof(NetworkDownloadNow));
                OnPropertyChanged(nameof(NetworkUploadMax));
                OnPropertyChanged(nameof(NetworkUploadMin));
                OnPropertyChanged(nameof(NetworkUploadNow));
                OnPropertyChanged(nameof(NetworkIoScaleMax));
                OnPropertyChanged(nameof(NetworkIoScaleMid));

                // Update network adapters list
                UpdateCollection(NetworkAdapters, networkAdapters);
            }

            if (_diskMonitor != null)
            {
                var disks = _diskMonitor.GetDiskActivity();
                var totalRead = disks.Sum(d => d.ReadMBps);
                var totalWrite = disks.Sum(d => d.WriteMBps);
                UpdateHistory(DiskReadHistory, totalRead);
                UpdateHistory(DiskWriteHistory, totalWrite);
                OnPropertyChanged(nameof(DiskReadMax));
                OnPropertyChanged(nameof(DiskReadMin));
                OnPropertyChanged(nameof(DiskReadNow));
                OnPropertyChanged(nameof(DiskWriteMax));
                OnPropertyChanged(nameof(DiskWriteMin));
                OnPropertyChanged(nameof(DiskWriteNow));
                OnPropertyChanged(nameof(DiskIoScaleMax));
                OnPropertyChanged(nameof(DiskIoScaleMid));

                // Update disks list
                UpdateCollection(Disks, disks);
            }

            // Update top processes
            if (_processMonitor != null)
            {
                UpdateCollection(TopProcessesByCpu, _processMonitor.GetTopProcessesByCpu(10));
                UpdateCollection(TopProcessesByRam, _processMonitor.GetTopProcessesByRam(10));
                UpdateCollection(TopProcessesByNetwork, _processMonitor.GetTopProcessesByNetwork(10));
                UpdateCollection(TopProcessesByDisk, _processMonitor.GetTopProcessesByDisk(10));
                UpdateNetworkProcessMode();

                // Cleanup dead process entries
                _processMonitor.Cleanup();
            }

            if (!_isAuxSampleInProgress && DateTime.UtcNow >= _nextAuxSampleUtc)
            {
                _nextAuxSampleUtc = DateTime.UtcNow.Add(AuxSampleInterval);
                _ = SampleAuxMetricsAsync();
            }

            // Check alert thresholds
            IsCpuAlertActive = CpuUsage >= CpuAlertThreshold;
            IsRamAlertActive = RamUsagePercent >= RamAlertThreshold;

            // Trigger RamUsagePercent update
            OnPropertyChanged(nameof(RamUsagePercent));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel update error: {ex.Message}");
            // Continue running - don't crash the app
        }
    }

    private void UpdateHistory(ObservableCollection<double> history, double newValue)
    {
        history.RemoveAt(0);
        history.Add(newValue);
    }

    private static double GetHistoryMax(ObservableCollection<double> history)
    {
        return history.Count > 0 ? history.Max() : 0;
    }

    private static double GetHistoryMin(ObservableCollection<double> history)
    {
        return history.Count > 0 ? history.Min() : 0;
    }

    private static double GetHistoryNow(ObservableCollection<double> history)
    {
        return history.Count > 0 ? history[history.Count - 1] : 0;
    }

    private void UpdateCollection<T>(ObservableCollection<T> collection, List<T> newItems)
    {
        if (collection.Count == newItems.Count)
        {
            for (var i = 0; i < newItems.Count; i++)
            {
                collection[i] = newItems[i];
            }
            return;
        }

        collection.Clear();
        foreach (var item in newItems)
        {
            collection.Add(item);
        }
    }

    private void UpdateNetworkProcessMode()
    {
        var mode = _processMonitor?.NetworkMode ?? ProcessMonitor.NetworkProcessMode.ApproximateIo;
        if (_networkProcessMode == mode)
        {
            return;
        }

        _networkProcessMode = mode;
        OnPropertyChanged(nameof(NetworkProcessTitle));
        OnPropertyChanged(nameof(NetworkProcessSubtitle));
    }

    private async Task SampleAuxMetricsAsync()
    {
        _isAuxSampleInProgress = true;
        try
        {
            if (_metricProvider != null)
            {
                var gpuMemory = _metricProvider.GetGpuMemorySnapshot();
                HasGpuMemory = gpuMemory.IsAvailable;
                GpuMemoryUsedMb = gpuMemory.UsedMb;
                GpuMemoryTotalMb = gpuMemory.TotalMb;
                GpuMemoryUsagePercent = gpuMemory.UsagePercent;
                OnPropertyChanged(nameof(GpuMemoryUsedGb));
                OnPropertyChanged(nameof(GpuMemoryTotalGb));
                OnPropertyChanged(nameof(GpuMemoryUsageText));
                OnPropertyChanged(nameof(GpuMemoryPercentText));

                var fans = _metricProvider.GetFanSpeedSnapshot();
                CpuFanRpm = fans.CpuRpm;
                GpuFanRpm = fans.GpuRpm;
                OnPropertyChanged(nameof(CpuFanRpmText));
                OnPropertyChanged(nameof(GpuFanRpmText));
                OnPropertyChanged(nameof(HasCpuFan));
                OnPropertyChanged(nameof(HasGpuFan));

                var diskHealth = _metricProvider.GetDiskHealthSnapshot();
                DiskHealthPercent = diskHealth.HealthPercent;
                DiskPredictFailure = diskHealth.PredictFailure;
                OnPropertyChanged(nameof(DiskHealthText));
                OnPropertyChanged(nameof(DiskHealthDetail));
            }

            if (_latencyMonitor != null)
            {
                var latency = await _latencyMonitor.SampleAsync(CancellationToken.None);
                if (latency.HasValue)
                {
                    NetworkLatencyMs = latency.Value.GatewayMs;
                    NetworkLatencyTarget = latency.Value.GatewayTarget;
                    CloudflareLatencyMs = latency.Value.CloudflareMs;
                    GoogleLatencyMs = latency.Value.GoogleMs;
                }
                else
                {
                    NetworkLatencyMs = null;
                    NetworkLatencyTarget = string.Empty;
                    CloudflareLatencyMs = null;
                    GoogleLatencyMs = null;
                }

                OnPropertyChanged(nameof(NetworkLatencyText));
                OnPropertyChanged(nameof(NetworkLatencyDetail));
                OnPropertyChanged(nameof(GatewayLatencyText));
                OnPropertyChanged(nameof(GatewayLatencyLabel));
                OnPropertyChanged(nameof(CloudflareLatencyText));
                OnPropertyChanged(nameof(GoogleLatencyText));
            }

            if (_wifiSignalMonitor != null)
            {
                var signal = _wifiSignalMonitor.TryGetSignal();
                if (signal.HasValue)
                {
                    WifiSignalQuality = signal.Value.SignalQuality;
                    WifiSsid = signal.Value.Ssid;
                }
                else
                {
                    WifiSignalQuality = null;
                    WifiSsid = string.Empty;
                }

                OnPropertyChanged(nameof(WifiSignalText));
                OnPropertyChanged(nameof(WifiSignalDetail));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel aux sample error: {ex.Message}");
        }
        finally
        {
            _isAuxSampleInProgress = false;
        }
    }

    private sealed record MonitorSectionDefinition(string Key, string Title, string Description);

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WindowsOptimizer_Debug.log");
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
