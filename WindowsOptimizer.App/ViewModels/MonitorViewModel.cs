using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Metrics;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MonitorViewModel : ViewModelBase
{
    private readonly MetricProvider _metricProvider;
    private readonly ProcessMonitor _processMonitor = new();
    private readonly NetworkMonitor _networkMonitor = new();
    private readonly DiskMonitor _diskMonitor = new();
    private readonly DispatcherTimer _updateTimer;

    private double _cpuUsage;
    private double _ramUsedGb;
    private double _ramTotalGb;
    private double _cpuTemp;
    private double _gpuTemp;
    private double _gpuUsage;
    private SystemInfo? _systemInfo;
    private int _refreshIntervalSeconds = 1;
    private double _cpuAlertThreshold = 90.0;
    private double _ramAlertThreshold = 90.0;
    private bool _isCpuAlertActive;
    private bool _isRamAlertActive;

    public MonitorViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _metricProvider = new MetricProvider();

        // Initialize collections
        CpuHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        RamHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        NetworkUploadHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        NetworkDownloadHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        DiskReadHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        DiskWriteHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        TopProcessesByCpu = new ObservableCollection<ProcessInfo>();
        TopProcessesByRam = new ObservableCollection<ProcessInfo>();
        NetworkAdapters = new ObservableCollection<NetworkAdapterInfo>();
        Disks = new ObservableCollection<DiskInfo>();
        RefreshIntervalOptions = new ObservableCollection<int> { 1, 2, 5 };

        // Timer: 1 second refresh
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateTimer.Tick += OnUpdateTick;
        _updateTimer.Start();

        // Get initial total RAM and system info
        RamTotalGb = _metricProvider.GetTotalRamGb();
        SystemInfo = _metricProvider.GetSystemInfo();

        // Initialize process management commands
        KillProcessCommand = new RelayCommand(param =>
        {
            if (param is ProcessInfo process)
            {
                _processMonitor.KillProcess(process.Pid);
            }
        });

        SuspendProcessCommand = new RelayCommand(param =>
        {
            if (param is ProcessInfo process)
            {
                _processMonitor.SuspendProcess(process.Pid);
            }
        });

        ResumeProcessCommand = new RelayCommand(param =>
        {
            if (param is ProcessInfo process)
            {
                _processMonitor.ResumeProcess(process.Pid);
            }
        });

        ExportMetricsCommand = new RelayCommand(_ => ExportMetricsToCsv());
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
            csv.AppendLine($"CPU Temperature,{CpuTemp:F1}°C");
            csv.AppendLine($"GPU Usage,{GpuUsage:F2}%");
            csv.AppendLine($"GPU Temperature,{GpuTemp:F1}°C");
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

            File.WriteAllText(filepath, csv.ToString());
            System.Diagnostics.Debug.WriteLine($"Metrics exported to: {filepath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
        }
    }

    public string Title => "Monitor";

    public ObservableCollection<double> CpuHistory { get; }
    public ObservableCollection<double> RamHistory { get; }
    public ObservableCollection<double> NetworkUploadHistory { get; }
    public ObservableCollection<double> NetworkDownloadHistory { get; }
    public ObservableCollection<double> DiskReadHistory { get; }
    public ObservableCollection<double> DiskWriteHistory { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByCpu { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByRam { get; }
    public ObservableCollection<NetworkAdapterInfo> NetworkAdapters { get; }
    public ObservableCollection<DiskInfo> Disks { get; }
    public ObservableCollection<int> RefreshIntervalOptions { get; }

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
        private set => SetProperty(ref _cpuTemp, value);
    }

    public double GpuTemp
    {
        get => _gpuTemp;
        private set => SetProperty(ref _gpuTemp, value);
    }

    public double GpuUsage
    {
        get => _gpuUsage;
        private set => SetProperty(ref _gpuUsage, value);
    }

    public SystemInfo? SystemInfo
    {
        get => _systemInfo;
        private set => SetProperty(ref _systemInfo, value);
    }

    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            if (SetProperty(ref _refreshIntervalSeconds, value))
            {
                _updateTimer.Interval = TimeSpan.FromSeconds(value);
            }
        }
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
            // Update system metrics
            CpuUsage = _metricProvider.GetCpuUsage();
            RamUsedGb = _metricProvider.GetUsedRamGb();
            CpuTemp = _metricProvider.GetCpuTemperature();
            GpuTemp = _metricProvider.GetGpuTemperature();
            GpuUsage = _metricProvider.GetGpuUsage();

            // Update history (60 second sliding window)
            UpdateHistory(CpuHistory, CpuUsage);
            UpdateHistory(RamHistory, RamUsagePercent);

            // Update network and disk I/O history
            var networkAdapters = _networkMonitor.GetActiveAdapters();
            var totalUpload = networkAdapters.Sum(a => a.SendMbps);
            var totalDownload = networkAdapters.Sum(a => a.ReceiveMbps);
            UpdateHistory(NetworkUploadHistory, totalUpload);
            UpdateHistory(NetworkDownloadHistory, totalDownload);

            var disks = _diskMonitor.GetDiskActivity();
            var totalRead = disks.Sum(d => d.ReadMBps);
            var totalWrite = disks.Sum(d => d.WriteMBps);
            UpdateHistory(DiskReadHistory, totalRead);
            UpdateHistory(DiskWriteHistory, totalWrite);

            // Update top processes
            UpdateCollection(TopProcessesByCpu, _processMonitor.GetTopProcessesByCpu(10));
            UpdateCollection(TopProcessesByRam, _processMonitor.GetTopProcessesByRam(10));

            // Update network adapters
            UpdateCollection(NetworkAdapters, networkAdapters);

            // Update disks
            UpdateCollection(Disks, disks);

            // Check alert thresholds
            IsCpuAlertActive = CpuUsage >= CpuAlertThreshold;
            IsRamAlertActive = RamUsagePercent >= RamAlertThreshold;

            // Cleanup dead process entries
            _processMonitor.Cleanup();

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

    private void UpdateCollection<T>(ObservableCollection<T> collection, List<T> newItems)
    {
        collection.Clear();
        foreach (var item in newItems)
        {
            collection.Add(item);
        }
    }
}
