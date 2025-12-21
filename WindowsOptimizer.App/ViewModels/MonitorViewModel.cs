using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsOptimizer.Infrastructure.Monitoring;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MonitorViewModel : ViewModelBase
{
    private readonly ISensorProvider _sensorProvider;
    private readonly DispatcherTimer _refreshTimer;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _stopCommand;
    private readonly RelayCommand _refreshCommand;
    private readonly List<TileDefinition> _tileDefinitions;
    private bool _isMonitoring;
    private bool _isRefreshing;
    private string _statusMessage = "Waiting for sensor data.";
    private string _lastUpdatedText = "Last update: -";
    private string _providerText = "Provider: -";

    public MonitorViewModel()
    {
        try
        {
            _sensorProvider = new LibreHardwareMonitorProvider();
            ProviderText = "Provider: LibreHardwareMonitor";
        }
        catch (Exception ex)
        {
            _sensorProvider = new NullSensorProvider(ex.Message);
            StatusMessage = "Hardware sensors unavailable. Monitoring is in fallback mode.";
            ProviderText = "Provider: Unavailable";
        }

        Highlights = new ObservableCollection<MonitorTileViewModel>
        {
            new("cpu-temp", "CPU Temp", "#F97316"),
            new("gpu-temp", "GPU Temp", "#EF4444"),
            new("cpu-load", "CPU Load", "#38BDF8"),
            new("gpu-load", "GPU Load", "#60A5FA"),
            new("mem-load", "Memory Load", "#22C55E"),
            new("cpu-power", "CPU Power", "#F59E0B"),
            new("cpu-voltage", "CPU Voltage", "#A855F7"),
            new("fan-speed", "Fan Speed", "#94A3B8")
        };

        _tileDefinitions = new List<TileDefinition>
        {
            new(Highlights[0], SensorType.Temperature, new[] { "CPU", "Package" }, "C", 85),
            new(Highlights[1], SensorType.Temperature, new[] { "GPU", "Core" }, "C", 85),
            new(Highlights[2], SensorType.Load, new[] { "CPU" }, "%", 90),
            new(Highlights[3], SensorType.Load, new[] { "GPU" }, "%", 90),
            new(Highlights[4], SensorType.Load, new[] { "Memory" }, "%", 90),
            new(Highlights[5], SensorType.Power, new[] { "CPU" }, "W", 120),
            new(Highlights[6], SensorType.Voltage, new[] { "CPU", "V" }, "V", 1.35),
            new(Highlights[7], SensorType.Fan, new[] { "Fan" }, "RPM", null)
        };

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += (_, _) => _ = RefreshAsync();

        _startCommand = new RelayCommand(_ => StartMonitoring(), _ => !IsMonitoring);
        _stopCommand = new RelayCommand(_ => StopMonitoring(), _ => IsMonitoring);
        _refreshCommand = new RelayCommand(_ => _ = RefreshAsync(), _ => !IsRefreshing);

        StartMonitoring();
    }

    public string Title => "Monitor";

    public ObservableCollection<MonitorTileViewModel> Highlights { get; }

    public ICommand StartCommand => _startCommand;

    public ICommand StopCommand => _stopCommand;

    public ICommand RefreshCommand => _refreshCommand;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        private set => SetProperty(ref _lastUpdatedText, value);
    }

    public string ProviderText
    {
        get => _providerText;
        private set => SetProperty(ref _providerText, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                _startCommand.RaiseCanExecuteChanged();
                _stopCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private void StartMonitoring()
    {
        if (IsMonitoring)
        {
            return;
        }

        IsMonitoring = true;
        StatusMessage = "Live monitoring started.";
        _refreshTimer.Start();
        _ = RefreshAsync();
    }

    private void StopMonitoring()
    {
        if (!IsMonitoring)
        {
            return;
        }

        _refreshTimer.Stop();
        IsMonitoring = false;
        StatusMessage = "Monitoring paused.";
    }

    private async Task RefreshAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        _refreshCommand.RaiseCanExecuteChanged();

        try
        {
            var snapshot = await _sensorProvider.CaptureAsync(CancellationToken.None);
            UpdateTiles(snapshot.Readings);
            ProviderText = $"Provider: {snapshot.Source}";
            LastUpdatedText = $"Last update: {snapshot.CapturedAt.ToLocalTime():HH:mm:ss}";
            StatusMessage = $"Live: {snapshot.Readings.Count} sensors.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Monitor error: {ex.Message}";
        }
        finally
        {
            _isRefreshing = false;
            _refreshCommand.RaiseCanExecuteChanged();
        }
    }

    private void UpdateTiles(IReadOnlyList<SensorReading> readings)
    {
        foreach (var definition in _tileDefinitions)
        {
            var reading = FindReading(readings, definition.Type, definition.Keywords)
                          ?? FindFallback(readings, definition.Type);

            if (reading is null)
            {
                definition.Tile.Update(null, definition.Unit, "Sensor unavailable", "No data", false);
                continue;
            }

            var unit = string.IsNullOrWhiteSpace(definition.Unit) ? reading.Unit : definition.Unit;
            var isWarning = definition.WarningThreshold.HasValue && reading.Value >= definition.WarningThreshold.Value;
            var status = isWarning ? "High" : "OK";
            definition.Tile.Update(reading.Value, unit, reading.Name, status, isWarning);
        }
    }

    private static SensorReading? FindReading(IEnumerable<SensorReading> readings, SensorType type, string[] keywords)
    {
        return readings.FirstOrDefault(reading =>
            reading.Type == type &&
            keywords.All(keyword => reading.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private static SensorReading? FindFallback(IEnumerable<SensorReading> readings, SensorType type)
    {
        return readings
            .Where(reading => reading.Type == type)
            .OrderByDescending(reading => reading.Value)
            .FirstOrDefault();
    }

    private sealed record TileDefinition(
        MonitorTileViewModel Tile,
        SensorType Type,
        string[] Keywords,
        string Unit,
        double? WarningThreshold);

    private sealed class NullSensorProvider : ISensorProvider
    {
        private readonly string _reason;

        public NullSensorProvider(string reason)
        {
            _reason = string.IsNullOrWhiteSpace(reason) ? "Unavailable" : reason;
        }

        public Task<MonitoringSnapshot> CaptureAsync(CancellationToken ct)
        {
            var snapshot = new MonitoringSnapshot(
                Array.Empty<SensorReading>(),
                DateTimeOffset.UtcNow,
                $"Unavailable ({_reason})");
            return Task.FromResult(snapshot);
        }
    }
}
