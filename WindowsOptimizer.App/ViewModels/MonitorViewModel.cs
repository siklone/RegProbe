using System;
using System.Collections.ObjectModel;
using System.Linq;
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

    public MonitorViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _metricProvider = new MetricProvider();

        // Initialize collections
        CpuHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        RamHistory = new ObservableCollection<double>(Enumerable.Repeat(0.0, 60));
        TopProcessesByCpu = new ObservableCollection<ProcessInfo>();
        TopProcessesByRam = new ObservableCollection<ProcessInfo>();
        NetworkAdapters = new ObservableCollection<NetworkAdapterInfo>();
        Disks = new ObservableCollection<DiskInfo>();

        // Timer: 1 second refresh
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateTimer.Tick += OnUpdateTick;
        _updateTimer.Start();

        // Get initial total RAM
        RamTotalGb = _metricProvider.GetTotalRamGb();
    }

    public string Title => "Monitor";

    public ObservableCollection<double> CpuHistory { get; }
    public ObservableCollection<double> RamHistory { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByCpu { get; }
    public ObservableCollection<ProcessInfo> TopProcessesByRam { get; }
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
        private set => SetProperty(ref _cpuTemp, value);
    }

    public double GpuTemp
    {
        get => _gpuTemp;
        private set => SetProperty(ref _gpuTemp, value);
    }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        // Update system metrics
        CpuUsage = _metricProvider.GetCpuUsage();
        RamUsedGb = _metricProvider.GetUsedRamGb();
        CpuTemp = _metricProvider.GetCpuTemperature();
        GpuTemp = _metricProvider.GetGpuTemperature();

        // Update history (60 second sliding window)
        UpdateHistory(CpuHistory, CpuUsage);
        UpdateHistory(RamHistory, RamUsagePercent);

        // Update top processes
        UpdateCollection(TopProcessesByCpu, _processMonitor.GetTopProcessesByCpu(10));
        UpdateCollection(TopProcessesByRam, _processMonitor.GetTopProcessesByRam(10));

        // Update network adapters
        UpdateCollection(NetworkAdapters, _networkMonitor.GetActiveAdapters());

        // Update disks
        UpdateCollection(Disks, _diskMonitor.GetDiskActivity());

        // Cleanup dead process entries
        _processMonitor.Cleanup();

        // Trigger RamUsagePercent update
        OnPropertyChanged(nameof(RamUsagePercent));
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
