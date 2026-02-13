using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsOptimizer.Infrastructure.Metrics;

namespace WindowsOptimizer.App.ViewModels;

public class TrayViewModel : ViewModelBase, IDisposable
{
    private readonly MetricProvider _metricProvider;
    private readonly DispatcherTimer _updateTimer;
    private string _toolTipText = "Windows Optimizer\nInitializing...";
    private double _cpuUsage;
    private double _ramUsagePercent;
    private string _cpuTemp = "N/A";
    private string _gpuTemp = "N/A";
    
    // Commands
    public ICommand ShowWindowCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    public TrayViewModel()
    {
        _metricProvider = new MetricProvider();
        
        ShowWindowCommand = new RelayCommand(_ => ShowMainWindow());
        ExitApplicationCommand = new RelayCommand(_ => Application.Current.Shutdown());

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _updateTimer.Tick += OnUpdateTick;
        _updateTimer.Start();
    }

    public string ToolTipText
    {
        get => _toolTipText;
        set => SetProperty(ref _toolTipText, value);
    }
    
    public double CpuUsage
    {
        get => _cpuUsage;
        set => SetProperty(ref _cpuUsage, value);
    }

    public double RamUsagePercent
    {
        get => _ramUsagePercent;
        set => SetProperty(ref _ramUsagePercent, value);
    }

    public string CpuTemp
    {
        get => _cpuTemp;
        set => SetProperty(ref _cpuTemp, value);
    }

    public string GpuTemp
    {
        get => _gpuTemp;
        set => SetProperty(ref _gpuTemp, value);
    }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        try
        {
            // Update CPU
            CpuUsage = _metricProvider.GetCpuUsage();
            
            // Update RAM
            var ramTotal = _metricProvider.GetTotalRamGb();
            var ramUsed = _metricProvider.GetUsedRamGb();
            if (ramTotal > 0)
            {
                RamUsagePercent = (ramUsed / ramTotal) * 100.0;
            }

            // Update Temps
            var cpuTemp = _metricProvider.GetCpuTemperature();
            CpuTemp = double.IsNaN(cpuTemp) ? "N/A" : $"{cpuTemp:F0}°C";

            var gpuTemp = _metricProvider.GetGpuTemperature();
            GpuTemp = double.IsNaN(gpuTemp) ? "N/A" : $"{gpuTemp:F0}°C";

            // Update Tooltip
            var sb = new StringBuilder();
            sb.AppendLine("Windows Optimizer");
            sb.AppendLine($"CPU: {CpuUsage:F0}% ({CpuTemp})");
            sb.AppendLine($"RAM: {RamUsagePercent:F0}%");
            sb.AppendLine($"GPU: {GpuTemp}");
            ToolTipText = sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tray update failed: {ex.Message}");
        }
    }

    private void ShowMainWindow()
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
            
            mainWindow.Show();
            mainWindow.Activate();
        }
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        _metricProvider.Dispose();
    }
}
