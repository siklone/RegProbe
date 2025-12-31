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
    private readonly MetricProvider? _metricProvider;
    private readonly ProcessMonitor? _processMonitor;
    private readonly NetworkMonitor? _networkMonitor;
    private readonly DiskMonitor? _diskMonitor;
    private readonly DispatcherTimer? _updateTimer;

    private double _cpuUsage;
    private double _ramUsedGb;
    private double _ramTotalGb;
    private double _cpuTemp = double.NaN;
    private double _gpuTemp = double.NaN;
    private double _gpuUsage;
    private SystemInfo? _systemInfo;
    private double _cpuAlertThreshold = 90.0;
    private double _ramAlertThreshold = 90.0;
    private bool _isCpuAlertActive;
    private bool _isRamAlertActive;

    public MonitorViewModel()
    {
        try
        {
            var paths = AppPaths.FromEnvironment();

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
            NetworkAdapters ??= new ObservableCollection<NetworkAdapterInfo>();
            Disks ??= new ObservableCollection<DiskInfo>();

            // Initialize commands if they weren't created
            KillProcessCommand ??= new RelayCommand(_ => { });
            SuspendProcessCommand ??= new RelayCommand(_ => { });
            ResumeProcessCommand ??= new RelayCommand(_ => { });
            ExportMetricsCommand ??= new RelayCommand(_ => { });
        }
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
            csv.AppendLine("Top Processes by Network (Approx.)");
            csv.AppendLine("Name,PID,Mbps,Threads,Handles");
            foreach (var proc in TopProcessesByNetwork)
            {
                csv.AppendLine($"{proc.Name},{proc.Pid},{proc.IoMbps:F2},{proc.Threads},{proc.Handles}");
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
    public ObservableCollection<ProcessInfo> TopProcessesByNetwork { get; }
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
                UpdateCollection(TopProcessesByNetwork, _processMonitor.GetTopProcessesByIo(10));

                // Cleanup dead process entries
                _processMonitor.Cleanup();
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
