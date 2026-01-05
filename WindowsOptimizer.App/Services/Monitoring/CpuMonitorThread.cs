using WindowsOptimizer.App.Services.Monitoring;

namespace WindowsOptimizer.App.Services.Monitoring;

/// <summary>
/// Dedicated CPU monitoring thread.
/// Update interval: 500ms (responsive).
/// Metrics: Usage, voltage, temps, power, clocks.
/// </summary>
public class CpuMonitorThread : MonitorThread
{
    private readonly HardwareSensorService _hardwareService;
    private readonly WmiCache _wmiCache;
    
    public event EventHandler<CpuMetrics>? MetricsUpdated;

    protected override TimeSpan UpdateInterval => TimeSpan.FromMilliseconds(500);

    public CpuMonitorThread(HardwareSensorService hardwareService, WmiCache wmiCache)
    {
        _hardwareService = hardwareService;
        _wmiCache = wmiCache;
    }

    protected override async Task<object> CollectMetricsAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _hardwareService.GetSnapshotAsync();
        
        return new CpuMetrics
        {
            UsageTotal = snapshot.CpuUsageTotal,
            Voltage = snapshot.CpuVoltage,
            PackageTemp = snapshot.CpuPackageTemp,
            CoreTemps = snapshot.CpuCoreTemps.ToArray(),
            Power = snapshot.CpuPower,
            CoreClocks = snapshot.CpuCoreClocks.ToArray(),
            Timestamp = snapshot.Timestamp
        };
    }

    protected override void OnMetricsCollected(object metrics)
    {
        if (metrics is CpuMetrics cpuMetrics)
        {
            MetricsUpdated?.Invoke(this, cpuMetrics);
        }
    }
}

public class CpuMetrics
{
    public float UsageTotal { get; set; }
    public float Voltage { get; set; }
    public float PackageTemp { get; set; }
    public float[] CoreTemps { get; set; } = Array.Empty<float>();
    public float Power { get; set; }
    public float[] CoreClocks { get; set; } = Array.Empty<float>();
    public DateTime Timestamp { get; set; }
}
