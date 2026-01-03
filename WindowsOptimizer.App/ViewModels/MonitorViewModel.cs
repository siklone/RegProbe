using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.ServiceProcess;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Metrics;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MonitorViewModel : ViewModelBase, IDisposable
{
    private static readonly MonitorSectionDefinition[] DefaultSections =
    [
        new("stat-cards", "Usage Summary Cards", "CPU, RAM, GPU usage and temperatures."),
        new("cpu-ram-history", "CPU & RAM History", "60-second usage history charts."),
        new("network-disk-io", "Network & Disk I/O", "60-second throughput charts."),
        new("performance-panel", "Performance Panel", "Task Manager-style performance breakdown."),
        new("network-adapters", "Network Adapters", "Per-adapter throughput and totals."),
        new("disk-activity", "Disk Activity", "Per-disk throughput and usage.")
    ];

    private readonly MetricProvider? _metricProvider;
    private readonly ProcessMonitor? _processMonitor;
    private readonly NetworkMonitor? _networkMonitor;
    private readonly DiskMonitor? _diskMonitor;
    private readonly NetworkLatencyMonitor? _latencyMonitor;
    private readonly WifiSignalMonitor? _wifiSignalMonitor;
    private readonly GpuEngineMonitor? _gpuEngineMonitor;
    private readonly DispatcherTimer? _updateTimer;
    private readonly SettingsStore _settingsStore = new(AppPaths.FromEnvironment());
    private readonly IAppLogger _appLogger;

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
    private string _statusMessage = string.Empty;
    private SystemInfo? _systemInfo;
    private double _cpuAlertThreshold = 90.0;
    private double _ramAlertThreshold = 90.0;
    private bool _isCpuAlertActive;
    private bool _isRamAlertActive;
    private ProcessMonitor.NetworkProcessMode _networkProcessMode = ProcessMonitor.NetworkProcessMode.ApproximateIo;
    private PerformanceItemViewModel? _selectedPerformanceItem;
    private ObservableCollection<double> _performancePrimaryHistory = new(Enumerable.Repeat(0.0, 60));
    private ObservableCollection<double> _performanceSecondaryHistory = new(Enumerable.Repeat(0.0, 60));
    private string _performanceChartTitle = "Performance";
    private string _performancePrimaryLabel = "Usage";
    private string _performanceSecondaryLabel = string.Empty;
    private double _performanceHistoryScaleMax = 100;
    private bool _isDisposed;
    private bool _isLayoutEditorVisible;
    private bool _isLayoutLoading;
    private DateTime _nextCoreSampleUtc = DateTime.MinValue;
    private bool _isCoreSampleInProgress;
    private static readonly TimeSpan CoreSampleInterval = TimeSpan.FromSeconds(1);
    private DateTime _nextIoSampleUtc = DateTime.MinValue;
    private bool _isIoSampleInProgress;
    private static readonly TimeSpan IoSampleInterval = TimeSpan.FromSeconds(1);
    private DateTime _nextProcessSampleUtc = DateTime.MinValue;
    private bool _isProcessSampleInProgress;
    private static readonly TimeSpan ProcessSampleInterval = TimeSpan.FromSeconds(2);
    private DateTime _nextAuxSampleUtc = DateTime.MinValue;
    private bool _isAuxSampleInProgress;
    private static readonly TimeSpan AuxSampleInterval = TimeSpan.FromSeconds(5);
    private CpuPerformanceSnapshot _cpuPerformanceSnapshot = new(0, null, null, null, null, null, null, null, null, null, null, null);
    private MemoryPerformanceSnapshot _memoryPerformanceSnapshot = new(0, 0, 0, null, null, null, null, null, null, null, null, null, null);
    private IReadOnlyList<DiskPerformanceSnapshot> _diskPerformanceSnapshots = Array.Empty<DiskPerformanceSnapshot>();
    private GpuEngineUsageSnapshot _gpuEngineUsageSnapshot = new(0, 0, 0, 0, 0, false);
    private GpuPerformanceSnapshot _gpuPerformanceSnapshot = new(null, null, null, null, null, null, null, null, null, null, false);
    private readonly Dictionary<string, ObservableCollection<double>> _diskReadHistoryByDrive = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ObservableCollection<double>> _diskWriteHistoryByDrive = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ObservableCollection<double>> _netSendHistoryByAdapter = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ObservableCollection<double>> _netReceiveHistoryByAdapter = new(StringComparer.OrdinalIgnoreCase);

    private readonly RelayCommand _toggleLayoutEditorCommand;
    private readonly RelayCommand _moveSectionUpCommand;
    private readonly RelayCommand _moveSectionDownCommand;
    private readonly RelayCommand _resetLayoutCommand;
    private readonly RelayCommand _exportSensorDiagnosticsCommand;
    private readonly RelayCommand _refreshStartupAppsCommand;
    private readonly RelayCommand _refreshServicesCommand;
    private readonly RelayCommand _openServiceDocsCommand;

    private MonitorTabItem? _selectedTab;
    private ProcessCategoryItem? _selectedProcessCategory;
    private bool _isStartupAppsLoading;
    private bool _isServicesLoading;
    private DateTime _startupAppsUpdatedAt;
    private DateTime _servicesUpdatedAt;
    private StartupAppEntry? _selectedStartupApp;
    private ServiceEntry? _selectedService;

    public MonitorViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _appLogger = new FileAppLogger(paths);

        try
        {
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

            try
            {
                _gpuEngineMonitor = new GpuEngineMonitor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create GpuEngineMonitor: {ex.Message}");
            }

            // Initialize collections
            CpuHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            RamHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            GpuHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
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
            PerformanceItems = new ObservableCollection<PerformanceItemViewModel>();
            PerformanceDetailItems = new ObservableCollection<PerformanceDetailItem>();
            DiskHealthItems = new ObservableCollection<DiskHealthItemViewModel>();
            DiskHealthItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasDiskHealthItems));
            StartupApps = new ObservableCollection<StartupAppEntry>();
            StartupApps.CollectionChanged += (_, _) => OnPropertyChanged(nameof(StartupAppsSummary));
            Services = new ObservableCollection<ServiceEntry>();
            Services.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ServicesSummary));
            ServiceDetailItems = new ObservableCollection<InfoDetailItem>();

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
            _exportSensorDiagnosticsCommand = new RelayCommand(_ => ExportSensorDiagnostics());
            _refreshStartupAppsCommand = new RelayCommand(_ => _ = LoadStartupAppsAsync(true));
            _refreshServicesCommand = new RelayCommand(_ => _ = LoadServicesAsync(true));
            _openServiceDocsCommand = new RelayCommand(param => OpenServiceDocs(param as ServiceEntry));

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
                _updateTimer = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _updateTimer.Tick += OnUpdateTick;
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start timer: {ex.Message}");
            }

            MonitorTabs.Clear();
            MonitorTabs.Add(new MonitorTabItem(MonitorTab.Performance, "Performance", "Hardware charts and summaries"));
            MonitorTabs.Add(new MonitorTabItem(MonitorTab.Processes, "Processes", "Top CPU, RAM, disk, and network"));
            MonitorTabs.Add(new MonitorTabItem(MonitorTab.StartupApps, "Startup Apps", "Apps that launch with Windows"));
            MonitorTabs.Add(new MonitorTabItem(MonitorTab.Services, "Services", "Registry-backed services and drivers"));
            SelectedTab = MonitorTabs.FirstOrDefault();

            ProcessCategories.Clear();
            ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Cpu, "CPU", "Top CPU usage"));
            ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Memory, "RAM", "Top memory usage"));
            ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Network, "Network", "Top network activity"));
            ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Disk, "Disk I/O", "Top disk activity"));
            SelectedProcessCategory = ProcessCategories.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel initialization failed: {ex.Message}");

            // Ensure collections are initialized even if other initialization fails
            CpuHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            RamHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
            GpuHistory ??= new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
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
            PerformanceItems ??= new ObservableCollection<PerformanceItemViewModel>();
            PerformanceDetailItems ??= new ObservableCollection<PerformanceDetailItem>();
            DiskHealthItems ??= new ObservableCollection<DiskHealthItemViewModel>();
            DiskHealthItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasDiskHealthItems));
            StartupApps ??= new ObservableCollection<StartupAppEntry>();
            StartupApps.CollectionChanged += (_, _) => OnPropertyChanged(nameof(StartupAppsSummary));
            Services ??= new ObservableCollection<ServiceEntry>();
            Services.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ServicesSummary));
            ServiceDetailItems ??= new ObservableCollection<InfoDetailItem>();

            // Initialize commands if they weren't created
            KillProcessCommand ??= new RelayCommand(_ => { });
            SuspendProcessCommand ??= new RelayCommand(_ => { });
            ResumeProcessCommand ??= new RelayCommand(_ => { });
            ExportMetricsCommand ??= new RelayCommand(_ => { });
            _toggleLayoutEditorCommand = new RelayCommand(_ => { });
            _moveSectionUpCommand = new RelayCommand(_ => { });
            _moveSectionDownCommand = new RelayCommand(_ => { });
            _resetLayoutCommand = new RelayCommand(_ => { });
            _exportSensorDiagnosticsCommand = new RelayCommand(_ => { });
            _refreshStartupAppsCommand = new RelayCommand(_ => { });
            _refreshServicesCommand = new RelayCommand(_ => { });
            _openServiceDocsCommand = new RelayCommand(_ => { });

            if (MonitorSections.Count == 0)
            {
                foreach (var section in BuildDefaultSections())
                {
                    MonitorSections.Add(section);
                }
            }

            if (MonitorTabs.Count == 0)
            {
                MonitorTabs.Add(new MonitorTabItem(MonitorTab.Performance, "Performance", "Hardware charts and summaries"));
                MonitorTabs.Add(new MonitorTabItem(MonitorTab.Processes, "Processes", "Top CPU, RAM, disk, and network"));
                MonitorTabs.Add(new MonitorTabItem(MonitorTab.StartupApps, "Startup Apps", "Apps that launch with Windows"));
                MonitorTabs.Add(new MonitorTabItem(MonitorTab.Services, "Services", "Registry-backed services and drivers"));
            }

            SelectedTab ??= MonitorTabs.FirstOrDefault();

            if (ProcessCategories.Count == 0)
            {
                ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Cpu, "CPU", "Top CPU usage"));
                ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Memory, "RAM", "Top memory usage"));
                ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Network, "Network", "Top network activity"));
                ProcessCategories.Add(new ProcessCategoryItem(ProcessCategory.Disk, "Disk I/O", "Top disk activity"));
            }

            SelectedProcessCategory ??= ProcessCategories.FirstOrDefault();
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
        _gpuEngineMonitor?.Dispose();
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
            if (DiskHealthItems.Count > 0)
            {
                foreach (var item in DiskHealthItems)
                {
                    csv.AppendLine($"Disk Health - {item.DisplayName},{item.StatusText}");
                }
            }
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

            var topCpuSnapshot = TopProcessesByCpu.ToList();
            var topRamSnapshot = TopProcessesByRam.ToList();
            var topNetworkSnapshot = TopProcessesByNetwork.ToList();
            var topDiskSnapshot = TopProcessesByDisk.ToList();

            if (_processMonitor != null)
            {
                try
                {
                    topCpuSnapshot = _processMonitor.GetTopProcessesByCpu(10);
                    topRamSnapshot = _processMonitor.GetTopProcessesByRam(10);
                    topNetworkSnapshot = _processMonitor.GetTopProcessesByNetwork(10);
                    topDiskSnapshot = _processMonitor.GetTopProcessesByDisk(10);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Process snapshot export failed: {ex.Message}");
                }
            }

            csv.AppendLine("Top Processes by CPU");
            csv.AppendLine("Name,PID,CPU %,RAM MB,Threads,Handles");
            foreach (var proc in topCpuSnapshot)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.CpuPercent:F2},{proc.RamMb:F0},{proc.Threads},{proc.Handles}");
            }
            csv.AppendLine();
            csv.AppendLine("Top Processes by RAM");
            csv.AppendLine("Name,PID,RAM MB,Threads,Handles");
            foreach (var proc in topRamSnapshot)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.RamMb:F0},{proc.Threads},{proc.Handles}");
            }

            csv.AppendLine();
            csv.AppendLine(NetworkProcessTitle);
            csv.AppendLine("Name,PID,Mbps,Threads,Handles");
            foreach (var proc in topNetworkSnapshot)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.IoMbps:F2},{proc.Threads},{proc.Handles}");
            }

            csv.AppendLine();
            csv.AppendLine("Top Processes by Disk I/O");
            csv.AppendLine("Name,PID,MBps,Threads,Handles");
            foreach (var proc in topDiskSnapshot)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.DiskMBps:F2},{proc.Threads},{proc.Handles}");
            }

            File.WriteAllText(filepath, csv.ToString());
            StatusMessage = $"Metrics saved to Desktop: {filename}";
            _appLogger.Log(LogLevel.Info, $"Activity: Monitor - Metrics CSV saved ({filepath})");
            System.Diagnostics.Debug.WriteLine($"Metrics exported to: {filepath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Metrics export failed: {ex.Message}";
            _appLogger.Log(LogLevel.Error, "Activity: Monitor - Metrics export failed", ex);
            System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
        }
    }

    private void ExportSensorDiagnostics()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"SensorDiagnostics_{timestamp}.txt";
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filepath = Path.Combine(desktop, filename);
            var report = _metricProvider?.BuildSensorDiagnosticsReport()
                         ?? "Sensor diagnostics not available (MetricProvider not initialized).";
            File.WriteAllText(filepath, report);
            StatusMessage = $"Sensor report saved to Desktop: {filename}";
            _appLogger.Log(LogLevel.Info, $"Activity: Monitor - Sensor report saved ({filepath})");
            System.Diagnostics.Debug.WriteLine($"Sensor diagnostics exported to: {filepath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sensor export failed: {ex.Message}";
            _appLogger.Log(LogLevel.Error, "Activity: Monitor - Sensor export failed", ex);
            System.Diagnostics.Debug.WriteLine($"Sensor diagnostics export failed: {ex.Message}");
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

    private async Task LoadStartupAppsAsync(bool force)
    {
        if (IsStartupAppsLoading)
        {
            return;
        }

        if (!force && StartupApps.Count > 0)
        {
            return;
        }

        IsStartupAppsLoading = true;
        try
        {
            var items = await Task.Run(CollectStartupApps).ConfigureAwait(false);
            await DispatchAsync(() =>
            {
                UpdateCollection(StartupApps, items);
                _startupAppsUpdatedAt = DateTime.UtcNow;
                OnPropertyChanged(nameof(StartupAppsUpdatedText));
                if (SelectedStartupApp == null && StartupApps.Count > 0)
                {
                    SelectedStartupApp = StartupApps[0];
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup apps load failed: {ex.Message}");
        }
        finally
        {
            IsStartupAppsLoading = false;
        }
    }

    private async Task LoadServicesAsync(bool force)
    {
        if (IsServicesLoading)
        {
            return;
        }

        if (!force && Services.Count > 0)
        {
            return;
        }

        IsServicesLoading = true;
        try
        {
            var items = await Task.Run(CollectServices).ConfigureAwait(false);
            await DispatchAsync(() =>
            {
                UpdateCollection(Services, items);
                _servicesUpdatedAt = DateTime.UtcNow;
                OnPropertyChanged(nameof(ServicesUpdatedText));
                if (SelectedService == null && Services.Count > 0)
                {
                    SelectedService = Services[0];
                }
                else if (SelectedService != null)
                {
                    UpdateServiceDetails();
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Services load failed: {ex.Message}");
        }
        finally
        {
            IsServicesLoading = false;
        }
    }

    private static List<StartupAppEntry> CollectStartupApps()
    {
        var items = new List<StartupAppEntry>();
        var hkcuRunApproval = ReadStartupApproval(RegistryHive.CurrentUser, RegistryView.Default, @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run");
        var hklmRunApproval = ReadStartupApproval(RegistryHive.LocalMachine, RegistryView.Registry64, @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run");
        var hkcuFolderApproval = ReadStartupApproval(RegistryHive.CurrentUser, RegistryView.Default, @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder");
        var hklmFolderApproval = ReadStartupApproval(RegistryHive.LocalMachine, RegistryView.Registry64, @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder");

        AddStartupRunEntries(items, RegistryHive.CurrentUser, RegistryView.Default, "Current User", "Registry Run",
            @"Software\Microsoft\Windows\CurrentVersion\Run", hkcuRunApproval);
        AddStartupRunEntries(items, RegistryHive.LocalMachine, RegistryView.Registry64, "All Users", "Registry Run (64-bit)",
            @"Software\Microsoft\Windows\CurrentVersion\Run", hklmRunApproval);
        AddStartupRunEntries(items, RegistryHive.LocalMachine, RegistryView.Registry32, "All Users", "Registry Run (32-bit)",
            @"Software\Microsoft\Windows\CurrentVersion\Run", hklmRunApproval);

        AddStartupFolderEntries(items, Environment.SpecialFolder.Startup, "Current User", "Startup Folder", hkcuFolderApproval);
        AddStartupFolderEntries(items, Environment.SpecialFolder.CommonStartup, "All Users", "Startup Folder", hklmFolderApproval);

        return items
            .OrderBy(item => item.Scope)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddStartupRunEntries(
        List<StartupAppEntry> items,
        RegistryHive hive,
        RegistryView view,
        string scope,
        string source,
        string subKey,
        IReadOnlyDictionary<string, bool?> approvals)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var runKey = baseKey.OpenSubKey(subKey);
            if (runKey == null)
            {
                return;
            }

            foreach (var valueName in runKey.GetValueNames())
            {
                var rawValue = runKey.GetValue(valueName)?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    continue;
                }

                var enabled = approvals.TryGetValue(valueName, out var approval)
                    ? approval.GetValueOrDefault(true)
                    : true;

                var executablePath = ExtractExecutablePath(rawValue);
                items.Add(new StartupAppEntry(
                    valueName,
                    rawValue,
                    $"{hive}\\{subKey}",
                    scope,
                    source,
                    enabled,
                    executablePath));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup run entries read failed: {ex.Message}");
        }
    }

    private static void AddStartupFolderEntries(
        List<StartupAppEntry> items,
        Environment.SpecialFolder folder,
        string scope,
        string source,
        IReadOnlyDictionary<string, bool?> approvals)
    {
        try
        {
            var folderPath = Environment.GetFolderPath(folder);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(folderPath))
            {
                var name = Path.GetFileName(path);
                var enabled = approvals.TryGetValue(name, out var approval)
                    ? approval.GetValueOrDefault(true)
                    : true;
                items.Add(new StartupAppEntry(
                    name,
                    path,
                    folderPath,
                    scope,
                    source,
                    enabled,
                    path));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup folder read failed: {ex.Message}");
        }
    }

    private static Dictionary<string, bool?> ReadStartupApproval(RegistryHive hive, RegistryView view, string subKey)
    {
        var approvals = new Dictionary<string, bool?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey);
            if (key == null)
            {
                return approvals;
            }

            foreach (var valueName in key.GetValueNames())
            {
                if (key.GetValue(valueName) is byte[] data)
                {
                    approvals[valueName] = ParseStartupApproval(data);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup approval read failed: {ex.Message}");
        }

        return approvals;
    }

    private static bool? ParseStartupApproval(byte[] data)
    {
        if (data.Length == 0)
        {
            return null;
        }

        return data[0] switch
        {
            0x02 => false,
            0x03 => true,
            _ => null
        };
    }

    private static string ExtractExecutablePath(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return string.Empty;
        }

        var expanded = Environment.ExpandEnvironmentVariables(command).Trim();
        if (expanded.StartsWith('"'))
        {
            var end = expanded.IndexOf('"', 1);
            return end > 1 ? expanded[1..end] : expanded.Trim('"');
        }

        var space = expanded.IndexOf(' ');
        return space > 0 ? expanded[..space] : expanded;
    }

    private static List<ServiceEntry> CollectServices()
    {
        var results = new List<ServiceEntry>();
        var statusLookup = BuildServiceStatusLookup();
        var startTypeLookup = BuildServiceStartTypeLookup();

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var servicesKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (servicesKey == null)
            {
                return results;
            }

            foreach (var serviceName in servicesKey.GetSubKeyNames())
            {
                using var serviceKey = servicesKey.OpenSubKey(serviceName);
                if (serviceKey == null)
                {
                    continue;
                }

                var displayName = serviceKey.GetValue("DisplayName") as string ?? serviceName;
                var description = serviceKey.GetValue("Description") as string ?? string.Empty;
                var imagePath = serviceKey.GetValue("ImagePath") as string ?? string.Empty;
                var objectName = serviceKey.GetValue("ObjectName") as string ?? string.Empty;
                var group = serviceKey.GetValue("Group") as string ?? string.Empty;
                var startValue = TryGetInt(serviceKey.GetValue("Start"));
                var delayedAuto = TryGetInt(serviceKey.GetValue("DelayedAutoStart"));
                var typeValue = TryGetInt(serviceKey.GetValue("Type"));
                var serviceDll = string.Empty;

                using (var parametersKey = serviceKey.OpenSubKey("Parameters"))
                {
                    serviceDll = parametersKey?.GetValue("ServiceDll") as string ?? string.Empty;
                }

                var binaryPath = !string.IsNullOrWhiteSpace(imagePath) ? imagePath : serviceDll;
                var startType = DescribeStartType(startValue, delayedAuto, startTypeLookup, serviceName);
                var serviceType = DescribeServiceType(typeValue);
                var isDriver = IsDriverType(typeValue);
                var statusText = statusLookup.TryGetValue(serviceName, out var status) ? status : "Unknown";
                var docsLink = ResolveServiceDocsLink(serviceName);

                results.Add(new ServiceEntry(
                    serviceName,
                    displayName,
                    description,
                    serviceType,
                    startType,
                    statusText,
                    objectName,
                    group,
                    binaryPath,
                    servicesKey.Name + "\\" + serviceName,
                    isDriver,
                    docsLink));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Services registry read failed: {ex.Message}");
        }

        return results
            .OrderBy(entry => entry.IsDriver)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, string> BuildServiceStatusLookup()
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var service in ServiceController.GetServices())
            {
                lookup[service.ServiceName] = service.Status.ToString();
            }
        }
        catch
        {
            // Ignore status lookup failures.
        }

        try
        {
            foreach (var device in ServiceController.GetDevices())
            {
                lookup[device.ServiceName] = device.Status.ToString();
            }
        }
        catch
        {
            // Ignore device lookup failures.
        }

        return lookup;
    }

    private static int? TryGetInt(object? value)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string> BuildServiceStartTypeLookup()
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var service in ServiceController.GetServices())
            {
                try
                {
                    lookup[service.ServiceName] = DescribeServiceStartMode(service.StartType);
                }
                catch
                {
                    // Ignore individual service errors.
                }
            }
        }
        catch
        {
            // Ignore lookup failures.
        }

        return lookup;
    }

    private static string DescribeServiceStartMode(ServiceStartMode startMode)
    {
        return startMode switch
        {
            ServiceStartMode.Boot => "Boot",
            ServiceStartMode.System => "System",
            ServiceStartMode.Automatic => "Automatic",
            ServiceStartMode.Manual => "Manual",
            ServiceStartMode.Disabled => "Disabled",
            _ => "Unknown"
        };
    }

    private static string DescribeStartType(int? startValue, int? delayedAuto, IReadOnlyDictionary<string, string> startLookup, string serviceName)
    {
        if (startValue.HasValue)
        {
            var startType = startValue switch
            {
                0 => "Boot",
                1 => "System",
                2 => "Automatic",
                3 => "Manual",
                4 => "Disabled",
                _ => "Unknown"
            };

            if (startValue == 2 && delayedAuto.GetValueOrDefault() == 1)
            {
                return "Automatic (Delayed)";
            }

            return startType;
        }

        if (startLookup.TryGetValue(serviceName, out var fallback))
        {
            return fallback;
        }

        return "Unknown";
    }

    private static string DescribeServiceType(int? typeValue)
    {
        if (!typeValue.HasValue)
        {
            return "Unknown";
        }

        var type = typeValue.Value;
        var labels = new List<string>();
        if ((type & 0x1) != 0) labels.Add("Kernel Driver");
        if ((type & 0x2) != 0) labels.Add("File System Driver");
        if ((type & 0x10) != 0) labels.Add("Win32 Own Process");
        if ((type & 0x20) != 0) labels.Add("Win32 Shared Process");
        if ((type & 0x100) != 0) labels.Add("Interactive");

        return labels.Count > 0 ? string.Join(", ", labels) : $"0x{type:X}";
    }

    private static bool IsDriverType(int? typeValue)
    {
        if (!typeValue.HasValue)
        {
            return false;
        }

        var type = typeValue.Value;
        return (type & 0x1) != 0 || (type & 0x2) != 0;
    }

    private static string ResolveServiceDocsLink(string serviceName)
    {
        var docsRoot = DocsLocator.TryFindDocsRoot();
        if (!string.IsNullOrWhiteSpace(docsRoot))
        {
            var docsDir = Path.Combine(docsRoot, "services");
            var localMd = Path.Combine(docsDir, $"{serviceName}.md");
            var localHtml = Path.Combine(docsDir, $"{serviceName}.html");

            if (File.Exists(localMd)) return localMd;
            if (File.Exists(localHtml)) return localHtml;
        }

        var query = Uri.EscapeDataString($"{serviceName} windows service");
        return $"https://learn.microsoft.com/en-us/search/?terms={query}";
    }

    private void UpdateServiceDetails()
    {
        ServiceDetailItems.Clear();
        if (SelectedService == null)
        {
            return;
        }

        ServiceDetailItems.Add(new InfoDetailItem("Service name", SelectedService.Name));
        ServiceDetailItems.Add(new InfoDetailItem("Display name", SelectedService.DisplayName));
        ServiceDetailItems.Add(new InfoDetailItem("Status", SelectedService.Status));
        ServiceDetailItems.Add(new InfoDetailItem("Start type", SelectedService.StartType));
        ServiceDetailItems.Add(new InfoDetailItem("Service type", SelectedService.ServiceType));
        ServiceDetailItems.Add(new InfoDetailItem("Account", SelectedService.Account));
        ServiceDetailItems.Add(new InfoDetailItem("Group", SelectedService.Group));
        ServiceDetailItems.Add(new InfoDetailItem("Image path", SelectedService.BinaryPath));
        ServiceDetailItems.Add(new InfoDetailItem("Registry path", SelectedService.RegistryPath));
        if (!string.IsNullOrWhiteSpace(SelectedService.Description))
        {
            ServiceDetailItems.Add(new InfoDetailItem("Description", SelectedService.Description));
        }
    }

    private void OpenServiceDocs(ServiceEntry? entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.DocsLink))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = entry.DocsLink,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore open failures.
        }
    }

    public string Title => "Monitor";

    public ObservableCollection<MonitorSectionLayout> MonitorSections { get; } = new();
    public ObservableCollection<MonitorTabItem> MonitorTabs { get; } = new();
    public ObservableCollection<ProcessCategoryItem> ProcessCategories { get; } = new();

    public MonitorTabItem? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(IsPerformanceTab));
                OnPropertyChanged(nameof(IsProcessesTab));
                OnPropertyChanged(nameof(IsStartupAppsTab));
                OnPropertyChanged(nameof(IsServicesTab));

                if (!IsPerformanceTab)
                {
                    IsLayoutEditorVisible = false;
                }

                if (IsPerformanceTab)
                {
                    EnsurePerformanceItems();
                    _nextCoreSampleUtc = DateTime.UtcNow;
                    _nextIoSampleUtc = DateTime.UtcNow;
                    _nextAuxSampleUtc = DateTime.UtcNow;
                    _ = SampleCoreMetricsAsync();
                    _ = SampleIoMetricsAsync();
                    _ = SampleAuxMetricsAsync();
                }
                else if (IsStartupAppsTab)
                {
                    _ = LoadStartupAppsAsync(false);
                }
                else if (IsServicesTab)
                {
                    _ = LoadServicesAsync(false);
                }
                else if (IsProcessesTab && !_isProcessSampleInProgress)
                {
                    _nextProcessSampleUtc = DateTime.UtcNow;
                    _ = SampleProcessMetricsAsync();
                }
            }
        }
    }

    public bool IsPerformanceTab => SelectedTab?.Tab == MonitorTab.Performance;
    public bool IsProcessesTab => SelectedTab?.Tab == MonitorTab.Processes;
    public bool IsStartupAppsTab => SelectedTab?.Tab == MonitorTab.StartupApps;
    public bool IsServicesTab => SelectedTab?.Tab == MonitorTab.Services;

    public ProcessCategoryItem? SelectedProcessCategory
    {
        get => _selectedProcessCategory;
        set => SetProperty(ref _selectedProcessCategory, value);
    }

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
    public ObservableCollection<double> GpuHistory { get; }
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
    public ObservableCollection<PerformanceItemViewModel> PerformanceItems { get; }
    public ObservableCollection<PerformanceDetailItem> PerformanceDetailItems { get; }
    public ObservableCollection<StartupAppEntry> StartupApps { get; }
    public ObservableCollection<ServiceEntry> Services { get; }
    public ObservableCollection<InfoDetailItem> ServiceDetailItems { get; }

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

    public bool HasCpuFan => double.IsFinite(CpuFanRpm);

    public bool HasGpuFan => double.IsFinite(GpuFanRpm);

    public string CpuFanRpmText => HasCpuFan ? $"{CpuFanRpm:F0} RPM" : "N/A";

    public string GpuFanRpmText => HasGpuFan ? $"{GpuFanRpm:F0} RPM" : "N/A";

    public ObservableCollection<DiskHealthItemViewModel> DiskHealthItems { get; }

    public bool HasDiskHealthItems => DiskHealthItems.Any(item =>
        !string.Equals(item.StatusText, "N/A", StringComparison.OrdinalIgnoreCase));

    public ObservableCollection<double> PerformancePrimaryHistory
    {
        get => _performancePrimaryHistory;
        private set => SetProperty(ref _performancePrimaryHistory, value);
    }

    public ObservableCollection<double> PerformanceSecondaryHistory
    {
        get => _performanceSecondaryHistory;
        private set => SetProperty(ref _performanceSecondaryHistory, value);
    }

    public PerformanceItemViewModel? SelectedPerformanceItem
    {
        get => _selectedPerformanceItem;
        set
        {
            if (SetProperty(ref _selectedPerformanceItem, value))
            {
                UpdatePerformanceSelection();
            }
        }
    }

    public string PerformanceChartTitle
    {
        get => _performanceChartTitle;
        private set => SetProperty(ref _performanceChartTitle, value);
    }

    public string PerformancePrimaryLabel
    {
        get => _performancePrimaryLabel;
        private set => SetProperty(ref _performancePrimaryLabel, value);
    }

    public string PerformanceSecondaryLabel
    {
        get => _performanceSecondaryLabel;
        private set => SetProperty(ref _performanceSecondaryLabel, value);
    }

    public bool HasPerformanceSecondary => PerformanceSecondaryHistory.Count > 0;

    public double PerformanceHistoryScaleMax
    {
        get => _performanceHistoryScaleMax;
        private set => SetProperty(ref _performanceHistoryScaleMax, value);
    }

    public double PerformanceHistoryScaleMid => PerformanceHistoryScaleMax / 2.0;

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

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
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
    public ICommand ExportSensorDiagnosticsCommand => _exportSensorDiagnosticsCommand;
    public ICommand RefreshStartupAppsCommand => _refreshStartupAppsCommand;
    public ICommand RefreshServicesCommand => _refreshServicesCommand;
    public ICommand OpenServiceDocsCommand => _openServiceDocsCommand;

    public StartupAppEntry? SelectedStartupApp
    {
        get => _selectedStartupApp;
        set => SetProperty(ref _selectedStartupApp, value);
    }

    public ServiceEntry? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value))
            {
                UpdateServiceDetails();
            }
        }
    }

    public bool IsStartupAppsLoading
    {
        get => _isStartupAppsLoading;
        private set
        {
            if (SetProperty(ref _isStartupAppsLoading, value))
            {
                OnPropertyChanged(nameof(StartupAppsSummary));
            }
        }
    }

    public bool IsServicesLoading
    {
        get => _isServicesLoading;
        private set
        {
            if (SetProperty(ref _isServicesLoading, value))
            {
                OnPropertyChanged(nameof(ServicesSummary));
            }
        }
    }

    public string StartupAppsSummary => IsStartupAppsLoading
        ? "Loading..."
        : $"{StartupApps.Count} items";

    public string ServicesSummary => IsServicesLoading
        ? "Loading..."
        : $"{Services.Count} entries";

    public string StartupAppsUpdatedText => _startupAppsUpdatedAt == DateTime.MinValue
        ? "Not loaded yet"
        : $"Updated {FormatTimeAgo(DateTime.UtcNow - _startupAppsUpdatedAt)}";

    public string ServicesUpdatedText => _servicesUpdatedAt == DateTime.MinValue
        ? "Not loaded yet"
        : $"Updated {FormatTimeAgo(DateTime.UtcNow - _servicesUpdatedAt)}";

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        try
        {
            var now = DateTime.UtcNow;

            if (IsPerformanceTab && !_isCoreSampleInProgress && now >= _nextCoreSampleUtc)
            {
                _nextCoreSampleUtc = now.Add(CoreSampleInterval);
                _ = SampleCoreMetricsAsync();
            }

            if (IsPerformanceTab && !_isIoSampleInProgress && now >= _nextIoSampleUtc)
            {
                _nextIoSampleUtc = now.Add(IoSampleInterval);
                _ = SampleIoMetricsAsync();
            }

            if (IsProcessesTab && !_isProcessSampleInProgress && now >= _nextProcessSampleUtc)
            {
                _nextProcessSampleUtc = now.Add(ProcessSampleInterval);
                _ = SampleProcessMetricsAsync();
            }

            if (IsPerformanceTab && !_isAuxSampleInProgress && now >= _nextAuxSampleUtc)
            {
                _nextAuxSampleUtc = now.Add(AuxSampleInterval);
                _ = SampleAuxMetricsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel update error: {ex.Message}");
            // Continue running - don't crash the app
        }
    }

    private Task DispatchAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
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

    private static ObservableCollection<double> CreateHistoryBuffer()
    {
        return new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
    }

    private static ObservableCollection<double> GetOrCreateHistory(
        Dictionary<string, ObservableCollection<double>> cache,
        string key)
    {
        if (!cache.TryGetValue(key, out var history))
        {
            history = CreateHistoryBuffer();
            cache[key] = history;
        }

        return history;
    }

    private static void CleanupHistory(Dictionary<string, ObservableCollection<double>> cache, HashSet<string> activeKeys)
    {
        foreach (var key in cache.Keys.Where(k => !activeKeys.Contains(k)).ToList())
        {
            cache.Remove(key);
        }
    }

    private void RefreshPerformanceItems()
    {
        var selectedKey = SelectedPerformanceItem?.Key;
        var items = new List<PerformanceItemViewModel>();
        var existing = PerformanceItems.ToDictionary(item => item.Key, item => item, StringComparer.OrdinalIgnoreCase);

        var cpuSpeedText = FormatSpeedGHz(_cpuPerformanceSnapshot.CurrentSpeedMhz ?? _cpuPerformanceSnapshot.BaseSpeedMhz);
        items.Add(new PerformanceItemViewModel(
            "cpu",
            PerformanceItemKind.Cpu,
            "CPU",
            $"{CpuUsage:F0}% {cpuSpeedText}",
            null));

        items.Add(new PerformanceItemViewModel(
            "memory",
            PerformanceItemKind.Memory,
            "Memory",
            $"{RamUsedGb:F1}/{RamTotalGb:F1} GB ({RamUsagePercent:F0}%)",
            null));

        var diskSnapshots = _diskPerformanceSnapshots.Count > 0
            ? _diskPerformanceSnapshots
            : Disks.Select(disk => new DiskPerformanceSnapshot(
                disk.DriveLetter,
                null,
                null,
                null,
                null,
                disk.TotalSizeGb,
                disk.FreeSpaceGb,
                null,
                disk.ReadMBps,
                disk.WriteMBps,
                null,
                null,
                false,
                false)).ToList();

        foreach (var disk in diskSnapshots)
        {
            var indexLabel = disk.DiskIndex.HasValue ? $"Disk {disk.DiskIndex.Value}" : "Disk";
            var subtitle = disk.ActiveTimePercent.HasValue
                ? $"{disk.ActiveTimePercent.Value:F0}% active"
                : $"{disk.ReadMbps.GetValueOrDefault():F1}/{disk.WriteMbps.GetValueOrDefault():F1} MB/s";

            items.Add(new PerformanceItemViewModel(
                $"disk:{disk.DriveLetter}",
                PerformanceItemKind.Disk,
                $"{indexLabel} ({disk.DriveLetter})",
                subtitle,
                disk.DriveLetter));
        }

        foreach (var adapter in NetworkAdapters)
        {
            var subtitle = $"{adapter.SendMbps:F1}↑ {adapter.ReceiveMbps:F1}↓ Mbps";
            items.Add(new PerformanceItemViewModel(
                $"net:{adapter.AdapterId}",
                PerformanceItemKind.Network,
                adapter.Name,
                subtitle,
                adapter.AdapterId));
        }

        items.Add(new PerformanceItemViewModel(
            "gpu",
            PerformanceItemKind.Gpu,
            "GPU",
            $"{GpuUsage:F0}% {GpuTempText}",
            null));

        PerformanceItems.Clear();
        foreach (var item in items)
        {
            if (existing.TryGetValue(item.Key, out var cached))
            {
                cached.Title = item.Title;
                cached.Subtitle = item.Subtitle;
                PerformanceItems.Add(cached);
            }
            else
            {
                PerformanceItems.Add(item);
            }
        }

        var nextSelection = PerformanceItems.FirstOrDefault(i => i.Key == selectedKey)
                             ?? PerformanceItems.FirstOrDefault();
        if (nextSelection != SelectedPerformanceItem)
        {
            SelectedPerformanceItem = nextSelection;
        }
    }

    private void UpdatePerformanceSelection()
    {
        var selection = SelectedPerformanceItem ?? PerformanceItems.FirstOrDefault();
        if (selection == null)
        {
            return;
        }

        PerformanceDetailItems.Clear();

        switch (selection.Kind)
        {
            case PerformanceItemKind.Cpu:
                PerformanceChartTitle = "CPU Utilization";
                PerformancePrimaryHistory = CpuHistory;
                PerformanceSecondaryHistory = new ObservableCollection<double>();
                PerformancePrimaryLabel = "Usage %";
                PerformanceSecondaryLabel = string.Empty;
                PerformanceHistoryScaleMax = 100;
                PerformanceDetailItems.Add(new PerformanceDetailItem("Speed", FormatSpeedGHz(_cpuPerformanceSnapshot.CurrentSpeedMhz)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Base speed", FormatSpeedGHz(_cpuPerformanceSnapshot.BaseSpeedMhz)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Processes", FormatCount(_cpuPerformanceSnapshot.ProcessCount)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Threads", FormatCount(_cpuPerformanceSnapshot.ThreadCount)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Handles", FormatCount(_cpuPerformanceSnapshot.HandleCount)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Sockets", FormatCount(_cpuPerformanceSnapshot.Sockets)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Cores", FormatCount(_cpuPerformanceSnapshot.Cores)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Logical processors", FormatCount(_cpuPerformanceSnapshot.LogicalProcessors)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Virtualization", FormatBool(_cpuPerformanceSnapshot.VirtualizationEnabled)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("L2 cache", FormatCacheKb(_cpuPerformanceSnapshot.L2CacheKb)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("L3 cache", FormatCacheKb(_cpuPerformanceSnapshot.L3CacheKb)));
                break;

            case PerformanceItemKind.Memory:
                PerformanceChartTitle = "Memory Usage";
                PerformancePrimaryHistory = RamHistory;
                PerformanceSecondaryHistory = new ObservableCollection<double>();
                PerformancePrimaryLabel = "Usage %";
                PerformanceSecondaryLabel = string.Empty;
                PerformanceHistoryScaleMax = 100;
                PerformanceDetailItems.Add(new PerformanceDetailItem("In use", $"{_memoryPerformanceSnapshot.UsedGb:F1} GB"));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Available", $"{_memoryPerformanceSnapshot.AvailableGb:F1} GB"));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Committed", FormatPairGb(_memoryPerformanceSnapshot.CommittedGb, _memoryPerformanceSnapshot.CommitLimitGb)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Cached", FormatGb(_memoryPerformanceSnapshot.CachedGb)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Paged pool", FormatGb(_memoryPerformanceSnapshot.PagedPoolGb)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Non-paged pool", FormatGb(_memoryPerformanceSnapshot.NonPagedPoolGb)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Speed", FormatSpeedMhz(_memoryPerformanceSnapshot.SpeedMhz)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Slots used", FormatSlotUsage(_memoryPerformanceSnapshot.SlotsUsed, _memoryPerformanceSnapshot.SlotsTotal)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Form factor", _memoryPerformanceSnapshot.FormFactor ?? "N/A"));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Hardware reserved", FormatMb(_memoryPerformanceSnapshot.HardwareReservedMb)));
                break;

            case PerformanceItemKind.Disk:
            {
                var drive = selection.Identifier;
                var perf = _diskPerformanceSnapshots.FirstOrDefault(d => d.DriveLetter.Equals(drive, StringComparison.OrdinalIgnoreCase));
                var readHistory = !string.IsNullOrWhiteSpace(drive) ? GetOrCreateHistory(_diskReadHistoryByDrive, drive) : DiskReadHistory;
                var writeHistory = !string.IsNullOrWhiteSpace(drive) ? GetOrCreateHistory(_diskWriteHistoryByDrive, drive) : DiskWriteHistory;

                PerformanceChartTitle = $"Disk {drive}";
                PerformancePrimaryHistory = readHistory;
                PerformanceSecondaryHistory = writeHistory;
                PerformancePrimaryLabel = "Read MB/s";
                PerformanceSecondaryLabel = "Write MB/s";
                PerformanceHistoryScaleMax = GetCombinedHistoryMax(readHistory, writeHistory);

                if (perf != null)
                {
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Active time", FormatPercent(perf.ActiveTimePercent)));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Average response", FormatMs(perf.AvgResponseMs)));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Queue length", FormatNumber(perf.QueueLength)));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Read", FormatMbps(perf.ReadMbps)));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Write", FormatMbps(perf.WriteMbps)));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Capacity", $"{perf.TotalSizeGb:F1} GB"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("System disk", perf.IsSystemDisk ? "Yes" : "No"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Page file", perf.HasPageFile ? "Yes" : "No"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Type", perf.MediaType ?? "Unknown"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Model", FormatText(perf.Model)));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Bus", FormatText(perf.InterfaceType)));
                }
                else
                {
                    var fallback = Disks.FirstOrDefault(d => d.DriveLetter.Equals(drive, StringComparison.OrdinalIgnoreCase));
                    if (fallback != null)
                    {
                        PerformanceDetailItems.Add(new PerformanceDetailItem("Read", $"{fallback.ReadMBps:F1} MB/s"));
                        PerformanceDetailItems.Add(new PerformanceDetailItem("Write", $"{fallback.WriteMBps:F1} MB/s"));
                        PerformanceDetailItems.Add(new PerformanceDetailItem("Capacity", $"{fallback.TotalSizeGb:F1} GB"));
                        PerformanceDetailItems.Add(new PerformanceDetailItem("Free space", $"{fallback.FreeSpaceGb:F1} GB"));
                    }
                }
                break;
            }

            case PerformanceItemKind.Network:
            {
                var adapter = NetworkAdapters.FirstOrDefault(a => a.AdapterId == selection.Identifier)
                              ?? NetworkAdapters.FirstOrDefault(a => a.Name == selection.Title);

                var adapterId = adapter?.AdapterId ?? selection.Identifier ?? selection.Title;
                var sendHistory = GetOrCreateHistory(_netSendHistoryByAdapter, adapterId);
                var receiveHistory = GetOrCreateHistory(_netReceiveHistoryByAdapter, adapterId);

                PerformanceChartTitle = adapter?.Name ?? "Network";
                PerformancePrimaryHistory = receiveHistory;
                PerformanceSecondaryHistory = sendHistory;
                PerformancePrimaryLabel = "Receive Mbps";
                PerformanceSecondaryLabel = "Send Mbps";
                PerformanceHistoryScaleMax = GetCombinedHistoryMax(receiveHistory, sendHistory);

                if (adapter != null)
                {
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Send", $"{adapter.SendMbps:F1} Mbps"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Receive", $"{adapter.ReceiveMbps:F1} Mbps"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Link speed", adapter.LinkSpeedMbps > 0 ? $"{adapter.LinkSpeedMbps:F0} Mbps" : "N/A"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("IPv4 address", string.IsNullOrWhiteSpace(adapter.Ipv4Address) ? "N/A" : adapter.Ipv4Address));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("IPv6 address", string.IsNullOrWhiteSpace(adapter.Ipv6Address) ? "N/A" : adapter.Ipv6Address));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Adapter type", adapter.Type));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Adapter", FormatText(string.IsNullOrWhiteSpace(adapter.Description) ? adapter.Name : adapter.Description)));
                }
                break;
            }

            case PerformanceItemKind.Gpu:
                PerformanceChartTitle = "GPU Utilization";
                PerformancePrimaryHistory = GpuHistory;
                PerformanceSecondaryHistory = new ObservableCollection<double>();
                PerformancePrimaryLabel = "Usage %";
                PerformanceSecondaryLabel = string.Empty;
                PerformanceHistoryScaleMax = 100;
                PerformanceDetailItems.Add(new PerformanceDetailItem("Usage", $"{GpuUsage:F0}%"));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Temperature", GpuTempText));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Memory", GpuMemoryUsageText));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Dedicated memory", FormatMemoryMb(_gpuPerformanceSnapshot.DedicatedMemoryMb)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Shared memory", FormatMemoryMb(_gpuPerformanceSnapshot.SharedMemoryMb)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Driver version", FormatText(_gpuPerformanceSnapshot.DriverVersion)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Driver date", FormatDate(_gpuPerformanceSnapshot.DriverDate)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("DirectX version", FormatText(_gpuPerformanceSnapshot.DirectXVersion)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Location", FormatText(_gpuPerformanceSnapshot.LocationInfo)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Adapter", FormatText(_gpuPerformanceSnapshot.AdapterCompatibility)));
                PerformanceDetailItems.Add(new PerformanceDetailItem("Video processor", FormatText(_gpuPerformanceSnapshot.VideoProcessor)));
                if (_gpuEngineUsageSnapshot.IsAvailable)
                {
                    PerformanceDetailItems.Add(new PerformanceDetailItem("3D", $"{_gpuEngineUsageSnapshot.Engine3DPercent:F0}%"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Copy", $"{_gpuEngineUsageSnapshot.CopyPercent:F0}%"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Video Encode", $"{_gpuEngineUsageSnapshot.VideoEncodePercent:F0}%"));
                    PerformanceDetailItems.Add(new PerformanceDetailItem("Video Decode", $"{_gpuEngineUsageSnapshot.VideoDecodePercent:F0}%"));
                }
                break;
        }

        OnPropertyChanged(nameof(HasPerformanceSecondary));
        OnPropertyChanged(nameof(PerformanceHistoryScaleMid));
    }

    private static double GetCombinedHistoryMax(ObservableCollection<double> primary, ObservableCollection<double> secondary)
    {
        var max = Math.Max(GetHistoryMax(primary), GetHistoryMax(secondary));
        return max > 0 ? max : 100;
    }

    private static string FormatSpeedGHz(double? mhz)
    {
        if (!mhz.HasValue || mhz.Value <= 0)
        {
            return "N/A";
        }

        return mhz.Value >= 1000 ? $"{mhz.Value / 1000:F2} GHz" : $"{mhz.Value:F0} MHz";
    }

    private static string FormatSpeedMhz(double? mhz)
    {
        if (!mhz.HasValue || mhz.Value <= 0)
        {
            return "N/A";
        }

        return $"{mhz.Value:F0} MHz";
    }

    private static string FormatCount(int? value)
    {
        return value.HasValue && value.Value > 0 ? value.Value.ToString() : "N/A";
    }

    private static string FormatBool(bool? value)
    {
        if (!value.HasValue)
        {
            return "N/A";
        }

        return value.Value ? "Enabled" : "Disabled";
    }

    private static string FormatCacheKb(int? value)
    {
        return value.HasValue && value.Value > 0 ? $"{value.Value} KB" : "N/A";
    }

    private static string FormatGb(double? value)
    {
        return value.HasValue && value.Value > 0 ? $"{value.Value:F1} GB" : "N/A";
    }

    private static string FormatMb(double? value)
    {
        return value.HasValue && value.Value > 0 ? $"{value.Value:F0} MB" : "N/A";
    }

    private static string FormatMemoryMb(double? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return "N/A";
        }

        return value.Value >= 1024 ? $"{value.Value / 1024:F1} GB" : $"{value.Value:F0} MB";
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "N/A";
    }

    private static string FormatText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "N/A" : value.Trim();
    }

    private static string FormatTimeAgo(TimeSpan span)
    {
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return $"{(int)(span.TotalDays / 7)}w ago";
    }

    private static string FormatPairGb(double? used, double? limit)
    {
        if (used.HasValue && limit.HasValue)
        {
            return $"{used.Value:F1}/{limit.Value:F1} GB";
        }

        return "N/A";
    }

    private static string FormatPercent(double? value)
    {
        return value.HasValue ? $"{value.Value:F0}%" : "N/A";
    }

    private static string FormatMs(double? value)
    {
        return value.HasValue ? $"{value.Value:F1} ms" : "N/A";
    }

    private static string FormatNumber(double? value)
    {
        return value.HasValue ? $"{value.Value:F1}" : "N/A";
    }

    private static string FormatMbps(double? value)
    {
        return value.HasValue ? $"{value.Value:F1} MB/s" : "N/A";
    }

    private static string FormatSlotUsage(int? used, int? total)
    {
        if (used.HasValue && total.HasValue && total.Value > 0)
        {
            return $"{used.Value} of {total.Value}";
        }

        if (used.HasValue)
        {
            return $"{used.Value}";
        }

        return "N/A";
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

    private void UpdateNetworkProcessMode(ProcessMonitor.NetworkProcessMode mode)
    {
        if (_networkProcessMode == mode)
        {
            return;
        }

        _networkProcessMode = mode;
        OnPropertyChanged(nameof(NetworkProcessTitle));
        OnPropertyChanged(nameof(NetworkProcessSubtitle));
    }

    private static double NormalizeTemperature(double value)
    {
        return double.IsFinite(value) && value > 0 ? value : double.NaN;
    }

    private void EnsurePerformanceItems()
    {
        if (PerformanceItems.Count == 0)
        {
            RefreshPerformanceItems();
        }
    }

    private void UpdatePerformancePrimaryItems()
    {
        var cpuItem = PerformanceItems.FirstOrDefault(item => item.Key == "cpu");
        if (cpuItem != null)
        {
            var cpuSpeedText = FormatSpeedGHz(_cpuPerformanceSnapshot.CurrentSpeedMhz ?? _cpuPerformanceSnapshot.BaseSpeedMhz);
            cpuItem.Subtitle = $"{CpuUsage:F0}% {cpuSpeedText}";
        }

        var memoryItem = PerformanceItems.FirstOrDefault(item => item.Key == "memory");
        if (memoryItem != null)
        {
            memoryItem.Subtitle = $"{RamUsedGb:F1}/{RamTotalGb:F1} GB ({RamUsagePercent:F0}%)";
        }

        var gpuItem = PerformanceItems.FirstOrDefault(item => item.Key == "gpu");
        if (gpuItem != null)
        {
            gpuItem.Subtitle = $"{GpuUsage:F0}% {GpuTempText}";
        }
    }

    private async Task SampleCoreMetricsAsync()
    {
        _isCoreSampleInProgress = true;
        try
        {
            if (_metricProvider == null)
            {
                return;
            }

            var snapshot = await Task.Run(() =>
            {
                var cpuUsage = _metricProvider.GetCpuUsage();
                var ramUsedGb = _metricProvider.GetUsedRamGb();
                var cpuTemp = _metricProvider.GetCpuTemperature();
                var gpuTemp = _metricProvider.GetGpuTemperature();
                var gpuUsage = _metricProvider.GetGpuUsage();
                return new CoreMetricsSnapshot(cpuUsage, ramUsedGb, cpuTemp, gpuTemp, gpuUsage);
            }).ConfigureAwait(false);

            await DispatchAsync(() =>
            {
                CpuUsage = snapshot.CpuUsage;
                RamUsedGb = snapshot.RamUsedGb;
                CpuTemp = NormalizeTemperature(snapshot.CpuTemp);
                GpuTemp = NormalizeTemperature(snapshot.GpuTemp);
                GpuUsage = snapshot.GpuUsage;

                UpdateHistory(CpuHistory, CpuUsage);
                UpdateHistory(RamHistory, RamUsagePercent);
                UpdateHistory(GpuHistory, GpuUsage);

                OnPropertyChanged(nameof(CpuHistoryMax));
                OnPropertyChanged(nameof(CpuHistoryMin));
                OnPropertyChanged(nameof(RamHistoryMax));
                OnPropertyChanged(nameof(RamHistoryMin));
                OnPropertyChanged(nameof(RamUsagePercent));

                IsCpuAlertActive = CpuUsage >= CpuAlertThreshold;
                IsRamAlertActive = RamUsagePercent >= RamAlertThreshold;

                EnsurePerformanceItems();
                UpdatePerformancePrimaryItems();

                if (SelectedPerformanceItem?.Kind is PerformanceItemKind.Cpu
                    or PerformanceItemKind.Memory
                    or PerformanceItemKind.Gpu)
                {
                    UpdatePerformanceSelection();
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel core sample error: {ex.Message}");
        }
        finally
        {
            _isCoreSampleInProgress = false;
        }
    }

    private async Task SampleIoMetricsAsync()
    {
        _isIoSampleInProgress = true;
        try
        {
            if (_networkMonitor == null && _diskMonitor == null)
            {
                return;
            }

            var snapshot = await Task.Run(() =>
            {
                var adapters = _networkMonitor?.GetActiveAdapters() ?? new List<NetworkAdapterInfo>();
                var disks = _diskMonitor?.GetDiskActivity() ?? new List<DiskInfo>();
                return new IoMetricsSnapshot(adapters, disks);
            }).ConfigureAwait(false);

            await DispatchAsync(() =>
            {
                var refreshPerformance = false;

                if (_networkMonitor != null)
                {
                    var networkAdapters = snapshot.NetworkAdapters;
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

                    UpdateCollection(NetworkAdapters, networkAdapters);

                    var activeAdapters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var adapter in networkAdapters)
                    {
                        var key = string.IsNullOrWhiteSpace(adapter.AdapterId) ? adapter.Name : adapter.AdapterId;
                        activeAdapters.Add(key);
                        var sendHistory = GetOrCreateHistory(_netSendHistoryByAdapter, key);
                        var receiveHistory = GetOrCreateHistory(_netReceiveHistoryByAdapter, key);
                        UpdateHistory(sendHistory, adapter.SendMbps);
                        UpdateHistory(receiveHistory, adapter.ReceiveMbps);
                    }

                    CleanupHistory(_netSendHistoryByAdapter, activeAdapters);
                    CleanupHistory(_netReceiveHistoryByAdapter, activeAdapters);
                    refreshPerformance = true;
                }

                if (_diskMonitor != null)
                {
                    var disks = snapshot.Disks;
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

                    UpdateCollection(Disks, disks);

                    var activeDisks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var disk in disks)
                    {
                        var key = disk.DriveLetter;
                        activeDisks.Add(key);
                        var readHistory = GetOrCreateHistory(_diskReadHistoryByDrive, key);
                        var writeHistory = GetOrCreateHistory(_diskWriteHistoryByDrive, key);
                        UpdateHistory(readHistory, disk.ReadMBps);
                        UpdateHistory(writeHistory, disk.WriteMBps);
                    }

                    CleanupHistory(_diskReadHistoryByDrive, activeDisks);
                    CleanupHistory(_diskWriteHistoryByDrive, activeDisks);
                    refreshPerformance = true;
                }

                if (refreshPerformance)
                {
                    RefreshPerformanceItems();
                    if (SelectedPerformanceItem?.Kind is PerformanceItemKind.Network or PerformanceItemKind.Disk)
                    {
                        UpdatePerformanceSelection();
                    }
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel I/O sample error: {ex.Message}");
        }
        finally
        {
            _isIoSampleInProgress = false;
        }
    }

    private async Task SampleProcessMetricsAsync()
    {
        _isProcessSampleInProgress = true;
        try
        {
            if (_processMonitor == null)
            {
                return;
            }

            var snapshot = await Task.Run(() =>
            {
                var topCpu = _processMonitor.GetTopProcessesByCpu(10);
                var topRam = _processMonitor.GetTopProcessesByRam(10);
                var topNetwork = _processMonitor.GetTopProcessesByNetwork(10);
                var topDisk = _processMonitor.GetTopProcessesByDisk(10);
                var mode = _processMonitor.NetworkMode;
                _processMonitor.Cleanup();
                return new ProcessMetricsSnapshot(topCpu, topRam, topNetwork, topDisk, mode);
            }).ConfigureAwait(false);

            await DispatchAsync(() =>
            {
                UpdateCollection(TopProcessesByCpu, snapshot.TopCpu);
                UpdateCollection(TopProcessesByRam, snapshot.TopRam);
                UpdateCollection(TopProcessesByNetwork, snapshot.TopNetwork);
                UpdateCollection(TopProcessesByDisk, snapshot.TopDisk);
                UpdateNetworkProcessMode(snapshot.NetworkMode);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorViewModel process sample error: {ex.Message}");
        }
        finally
        {
            _isProcessSampleInProgress = false;
        }
    }

    private async Task SampleAuxMetricsAsync()
    {
        _isAuxSampleInProgress = true;
        try
        {
            AuxMetricsSnapshot? metricsSnapshot = null;
            if (_metricProvider != null)
            {
                metricsSnapshot = await Task.Run(() =>
                {
                    var cpuSnapshot = _metricProvider.GetCpuPerformanceSnapshot();
                    var memorySnapshot = _metricProvider.GetMemoryPerformanceSnapshot();
                    var diskSnapshots = _metricProvider.GetDiskPerformanceSnapshots();
                    var gpuMemory = _metricProvider.GetGpuMemorySnapshot();
                    var gpuPerformance = _metricProvider.GetGpuPerformanceSnapshot();
                    var fans = _metricProvider.GetFanSpeedSnapshot();
                    var diskItems = _metricProvider.GetDiskHealthItems().ToList();
                    var hasDiskHealthData = diskItems.Any(item => item.HealthPercent.HasValue || item.PredictFailure.HasValue);
                    var diskHealthFallback = hasDiskHealthData
                        ? new DiskHealthSnapshot(null, null)
                        : _metricProvider.GetDiskHealthSnapshot();
                    return new AuxMetricsSnapshot(
                        cpuSnapshot,
                        memorySnapshot,
                        diskSnapshots,
                        gpuMemory,
                        gpuPerformance,
                        fans,
                        diskItems,
                        diskHealthFallback);
                }).ConfigureAwait(false);
            }

            var gpuEngineSnapshot = _gpuEngineMonitor?.GetUsageSnapshot();

            NetworkLatencySample? latency = null;
            if (_latencyMonitor != null)
            {
                latency = await _latencyMonitor.SampleAsync(CancellationToken.None).ConfigureAwait(false);
            }

            var wifiSignal = _wifiSignalMonitor?.TryGetSignal();

            await DispatchAsync(() =>
            {
                if (metricsSnapshot != null)
                {
                    _cpuPerformanceSnapshot = metricsSnapshot.CpuSnapshot;
                    _memoryPerformanceSnapshot = metricsSnapshot.MemorySnapshot;
                    _diskPerformanceSnapshots = metricsSnapshot.DiskSnapshots;

                    HasGpuMemory = metricsSnapshot.GpuMemory.IsAvailable;
                    GpuMemoryUsedMb = metricsSnapshot.GpuMemory.UsedMb;
                    GpuMemoryTotalMb = metricsSnapshot.GpuMemory.TotalMb;
                    GpuMemoryUsagePercent = metricsSnapshot.GpuMemory.UsagePercent;
                    OnPropertyChanged(nameof(GpuMemoryUsedGb));
                    OnPropertyChanged(nameof(GpuMemoryTotalGb));
                    OnPropertyChanged(nameof(GpuMemoryUsageText));
                    OnPropertyChanged(nameof(GpuMemoryPercentText));

                    _gpuPerformanceSnapshot = metricsSnapshot.GpuPerformance;

                    CpuFanRpm = metricsSnapshot.Fans.CpuRpm;
                    GpuFanRpm = metricsSnapshot.Fans.GpuRpm;
                    OnPropertyChanged(nameof(CpuFanRpmText));
                    OnPropertyChanged(nameof(GpuFanRpmText));
                    OnPropertyChanged(nameof(HasCpuFan));
                    OnPropertyChanged(nameof(HasGpuFan));

                    UpdateCollection(DiskHealthItems, metricsSnapshot.DiskItems.Select(CreateDiskHealthItem).ToList());

                    var healthValues = metricsSnapshot.DiskItems
                        .Select(item => item.HealthPercent)
                        .Where(value => value.HasValue)
                        .Select(value => value!.Value)
                        .ToList();
                    DiskHealthPercent = healthValues.Count > 0 ? healthValues.Min() : metricsSnapshot.DiskHealthFallback.HealthPercent;

                    DiskPredictFailure = metricsSnapshot.DiskItems.Any(item => item.PredictFailure == true)
                        ? true
                        : metricsSnapshot.DiskItems.Any(item => item.PredictFailure == false)
                            ? false
                            : metricsSnapshot.DiskHealthFallback.PredictFailure;

                    OnPropertyChanged(nameof(DiskHealthText));
                    OnPropertyChanged(nameof(DiskHealthDetail));
                    OnPropertyChanged(nameof(HasDiskHealthItems));

                    RefreshPerformanceItems();
                    UpdatePerformancePrimaryItems();
                }

                if (gpuEngineSnapshot != null)
                {
                    _gpuEngineUsageSnapshot = gpuEngineSnapshot;
                }

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

                if (wifiSignal.HasValue)
                {
                    WifiSignalQuality = wifiSignal.Value.SignalQuality;
                    WifiSsid = wifiSignal.Value.Ssid;
                }
                else
                {
                    WifiSignalQuality = null;
                    WifiSsid = string.Empty;
                }

                OnPropertyChanged(nameof(WifiSignalText));
                OnPropertyChanged(nameof(WifiSignalDetail));

                UpdatePerformanceSelection();
            }).ConfigureAwait(false);
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

    private sealed record CoreMetricsSnapshot(
        double CpuUsage,
        double RamUsedGb,
        double CpuTemp,
        double GpuTemp,
        double GpuUsage);

    private sealed record IoMetricsSnapshot(
        List<NetworkAdapterInfo> NetworkAdapters,
        List<DiskInfo> Disks);

    private sealed record ProcessMetricsSnapshot(
        List<ProcessInfo> TopCpu,
        List<ProcessInfo> TopRam,
        List<ProcessInfo> TopNetwork,
        List<ProcessInfo> TopDisk,
        ProcessMonitor.NetworkProcessMode NetworkMode);

    private sealed record AuxMetricsSnapshot(
        CpuPerformanceSnapshot CpuSnapshot,
        MemoryPerformanceSnapshot MemorySnapshot,
        IReadOnlyList<DiskPerformanceSnapshot> DiskSnapshots,
        GpuMemorySnapshot GpuMemory,
        GpuPerformanceSnapshot GpuPerformance,
        FanSpeedSnapshot Fans,
        List<DiskHealthInfo> DiskItems,
        DiskHealthSnapshot DiskHealthFallback);

    private sealed record MonitorSectionDefinition(string Key, string Title, string Description);

    public enum MonitorTab
    {
        Performance,
        Processes,
        StartupApps,
        Services
    }

    public sealed record MonitorTabItem(MonitorTab Tab, string Title, string Subtitle);

    public enum ProcessCategory
    {
        Cpu,
        Memory,
        Network,
        Disk
    }

    public sealed record ProcessCategoryItem(ProcessCategory Category, string Title, string Subtitle);

    public sealed record DiskHealthItemViewModel(
        string DisplayName,
        string StatusText,
        string DetailText,
        bool IsWarning);

    public sealed class PerformanceItemViewModel : ViewModelBase
    {
        private string _title;
        private string _subtitle;

        public PerformanceItemViewModel(string key, PerformanceItemKind kind, string title, string subtitle, string? identifier)
        {
            Key = key;
            Kind = kind;
            _title = title;
            _subtitle = subtitle;
            Identifier = identifier ?? string.Empty;
        }

        public string Key { get; }

        public PerformanceItemKind Kind { get; }

        public string Identifier { get; }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }
    }

    public enum PerformanceItemKind
    {
        Cpu,
        Memory,
        Disk,
        Network,
        Gpu
    }

    public sealed record PerformanceDetailItem(string Label, string Value);

    public sealed record InfoDetailItem(string Label, string Value);

    public sealed class StartupAppEntry
    {
        public StartupAppEntry(
            string name,
            string command,
            string location,
            string scope,
            string source,
            bool isEnabled,
            string executablePath)
        {
            Name = name;
            Command = command;
            Location = location;
            Scope = scope;
            Source = source;
            IsEnabled = isEnabled;
            ExecutablePath = executablePath;
        }

        public string Name { get; }
        public string Command { get; }
        public string Location { get; }
        public string Scope { get; }
        public string Source { get; }
        public bool IsEnabled { get; }
        public string ExecutablePath { get; }
        public string StatusText => IsEnabled ? "Enabled" : "Disabled";
    }

    public sealed class ServiceEntry
    {
        public ServiceEntry(
            string name,
            string displayName,
            string description,
            string serviceType,
            string startType,
            string status,
            string account,
            string group,
            string binaryPath,
            string registryPath,
            bool isDriver,
            string docsLink)
        {
            Name = name;
            DisplayName = displayName;
            Description = description;
            ServiceType = serviceType;
            StartType = startType;
            Status = status;
            Account = account;
            Group = group;
            BinaryPath = binaryPath;
            RegistryPath = registryPath;
            IsDriver = isDriver;
            DocsLink = docsLink;
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string ServiceType { get; }
        public string StartType { get; }
        public string Status { get; }
        public string Account { get; }
        public string Group { get; }
        public string BinaryPath { get; }
        public string RegistryPath { get; }
        public bool IsDriver { get; }
        public string DocsLink { get; }

        public string KindText => IsDriver ? "Driver" : "Service";
    }

    private static DiskHealthItemViewModel CreateDiskHealthItem(DiskHealthInfo info)
    {
        var statusText = info.HealthPercent.HasValue
            ? $"{info.HealthPercent.Value:F0}%"
            : info.PredictFailure == true
                ? "Warning"
                : info.PredictFailure == false
                    ? "OK"
                    : "N/A";

        var detailText = info.HealthPercent.HasValue
            ? info.Source ?? "SMART life"
            : info.PredictFailure == true
                ? "SMART predicts failure"
                : info.PredictFailure == false
                    ? info.Source ?? "SMART status OK"
                    : "SMART data unavailable";

        return new DiskHealthItemViewModel(
            info.Name,
            statusText,
            detailText,
            info.PredictFailure == true);
    }

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
