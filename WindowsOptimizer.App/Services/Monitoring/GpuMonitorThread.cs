using WindowsOptimizer.App.Services.Monitoring;

namespace WindowsOptimizer.App.Services.Monitoring;

/// <summary>
/// Dedicated GPU monitoring thread.
/// Update interval: 500ms (responsive).
/// Metrics: Usage, voltage, temps, power, clocks, fan, VRAM.
/// </summary>
public class GpuMonitorThread : MonitorThread
{
    private readonly HardwareSensorService _hardwareService;
    
    public event EventHandler<GpuMetrics>? MetricsUpdated;

    protected override TimeSpan UpdateInterval => TimeSpan.FromMilliseconds(500);

    public GpuMonitorThread(HardwareSensorService hardwareService)
    {
        _hardwareService = hardwareService;
    }

    protected override async Task<object> CollectMetricsAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _hardwareService.GetSnapshotAsync();
        
        return new GpuMetrics
        {
            Name = snapshot.GpuName,
            Usage = snapshot.GpuUsage,
            CoreVoltage = snapshot.GpuCoreVoltage,
            CoreTemp = snapshot.GpuCoreTemp,
            HotspotTemp = snapshot.GpuHotspotTemp,
            MemoryTemp = snapshot.GpuMemoryTemp,
            Power = snapshot.GpuPower,
            CoreClock = snapshot.GpuCoreClock,
            MemoryClock = snapshot.GpuMemoryClock,
            MemoryUsedMB = snapshot.GpuMemoryUsedMB,
            FanSpeed = snapshot.GpuFanSpeed,
            Timestamp = snapshot.Timestamp
        };
    }

    protected override void OnMetricsCollected(object metrics)
    {
        if (metrics is GpuMetrics gpuMetrics)
        {
            MetricsUpdated?.Invoke(this, gpuMetrics);
        }
    }
}

public class GpuMetrics
{
    public string Name { get; set; } = string.Empty;
    public float Usage { get; set; }
    public float CoreVoltage { get; set; }
    public float CoreTemp { get; set; }
    public float HotspotTemp { get; set; }
    public float MemoryTemp { get; set; }
    public float Power { get; set; }
    public float CoreClock { get; set; }
    public float MemoryClock { get; set; }
    public float MemoryUsedMB { get; set; }
    public float FanSpeed { get; set; }
    public DateTime Timestamp { get; set; }
}
