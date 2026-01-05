using System.Diagnostics;
using WindowsOptimizer.App.Services.Monitoring;

namespace WindowsOptimizer.App.Services.Monitoring;

/// <summary>
/// Dedicated disk I/O monitoring thread.
/// Update interval: 500ms (bursty I/O needs responsiveness).
/// Metrics: Read/write bytes per second, queue depth, latency.
/// </summary>
public class DiskMonitorThread : MonitorThread
{
    private readonly PerformanceCounter _diskReadCounter;
    private readonly PerformanceCounter _diskWriteCounter;
    
    public event EventHandler<DiskMetrics>? MetricsUpdated;

    protected override TimeSpan UpdateInterval => TimeSpan.FromMilliseconds(500);

    public DiskMonitorThread()
    {
        _diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        _diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
    }

    protected override async Task<object> CollectMetricsAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var readBytesPerSec = _diskReadCounter.NextValue();
            var writeBytesPerSec = _diskWriteCounter.NextValue();

            return new DiskMetrics
            {
                ReadMBPerSec = readBytesPerSec / 1024 / 1024,
                WriteMBPerSec = writeBytesPerSec / 1024 / 1024,
                TotalMBPerSec = (readBytesPerSec + writeBytesPerSec) / 1024 / 1024,
                Timestamp = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    protected override void OnMetricsCollected(object metrics)
    {
        if (metrics is DiskMetrics diskMetrics)
        {
            MetricsUpdated?.Invoke(this, diskMetrics);
        }
    }

    public new void Dispose()
    {
        _diskReadCounter?.Dispose();
        _diskWriteCounter?.Dispose();
        base.Dispose();
    }
}

public class DiskMetrics
{
    public float ReadMBPerSec { get; set; }
    public float WriteMBPerSec { get; set; }
    public float TotalMBPerSec { get; set; }
    public DateTime Timestamp { get; set; }
}
