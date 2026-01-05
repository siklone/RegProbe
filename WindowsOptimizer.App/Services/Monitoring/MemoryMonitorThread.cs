using System.Diagnostics;
using WindowsOptimizer.App.Services.Monitoring;

namespace WindowsOptimizer.App.Services.Monitoring;

/// <summary>
/// Dedicated memory monitoring thread.
/// Update interval: 1000ms (changes slowly).
/// Metrics: Total usage, available, committed, cached.
/// </summary>
public class MemoryMonitorThread : MonitorThread
{
    private readonly PerformanceCounter _availableMemoryCounter;
    private readonly PerformanceCounter _committedMemoryCounter;
    
    public event EventHandler<MemoryMetrics>? MetricsUpdated;

    protected override TimeSpan UpdateInterval => TimeSpan.FromMilliseconds(1000);

    public MemoryMonitorThread()
    {
        _availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        _committedMemoryCounter = new PerformanceCounter("Memory", "Committed Bytes");
    }

    protected override async Task<object> CollectMetricsAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            var totalPhysical = (long)computerInfo.TotalPhysicalMemory;
            var availablePhysical = (long)computerInfo.AvailablePhysicalMemory;
            var usedPhysical = totalPhysical - availablePhysical;

            return new MemoryMetrics
            {
                TotalMB = totalPhysical / 1024 / 1024,
                UsedMB = usedPhysical / 1024 / 1024,
                AvailableMB = availablePhysical / 1024 / 1024,
                UsagePercent = (float)(usedPhysical * 100.0 / totalPhysical),
                CommittedMB = (long)(_committedMemoryCounter.NextValue() / 1024 / 1024),
                Timestamp = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    protected override void OnMetricsCollected(object metrics)
    {
        if (metrics is MemoryMetrics memoryMetrics)
        {
            MetricsUpdated?.Invoke(this, memoryMetrics);
        }
    }

    public new void Dispose()
    {
        _availableMemoryCounter?.Dispose();
        _committedMemoryCounter?.Dispose();
        base.Dispose();
    }
}

public class MemoryMetrics
{
    public long TotalMB { get; set; }
    public long UsedMB { get; set; }
    public long AvailableMB { get; set; }
    public float UsagePercent { get; set; }
    public long CommittedMB { get; set; }
    public DateTime Timestamp { get; set; }
}
