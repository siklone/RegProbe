using System.Diagnostics;
using WindowsOptimizer.App.Services.Monitoring;

namespace WindowsOptimizer.App.Services.Monitoring;

/// <summary>
/// Dedicated network monitoring thread.
/// Update interval: 500ms (real-time throughput).
/// Metrics: Bytes sent/received per second.
/// </summary>
public class NetworkMonitorThread : MonitorThread
{
    private readonly PerformanceCounter _bytesSentCounter;
    private readonly PerformanceCounter _bytesReceivedCounter;
    
    public event EventHandler<NetworkMetrics>? MetricsUpdated;

    protected override TimeSpan UpdateInterval => TimeSpan.FromMilliseconds(500);

    public NetworkMonitorThread()
    {
        _bytesSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", GetPrimaryNetworkInterface());
        _bytesReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", GetPrimaryNetworkInterface());
    }

    private string GetPrimaryNetworkInterface()
    {
        // Use first non-loopback interface
        var category = new PerformanceCounterCategory("Network Interface");
        var instanceNames = category.GetInstanceNames();
        
        foreach (var instance in instanceNames)
        {
            if (!instance.Contains("Loopback", StringComparison.OrdinalIgnoreCase))
            {
                return instance;
            }
        }
        
        return instanceNames.Length > 0 ? instanceNames[0] : string.Empty;
    }

    protected override async Task<object> CollectMetricsAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var bytesSent = _bytesSentCounter.NextValue();
            var bytesReceived = _bytesReceivedCounter.NextValue();

            return new NetworkMetrics
            {
                UploadMBPerSec = bytesSent / 1024 / 1024,
                DownloadMBPerSec = bytesReceived / 1024 / 1024,
                TotalMBPerSec = (bytesSent + bytesReceived) / 1024 / 1024,
                Timestamp = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    protected override void OnMetricsCollected(object metrics)
    {
        if (metrics is NetworkMetrics networkMetrics)
        {
            MetricsUpdated?.Invoke(this, networkMetrics);
        }
    }

    public new void Dispose()
    {
        _bytesSentCounter?.Dispose();
        _bytesReceivedCounter?.Dispose();
        base.Dispose();
    }
}

public class NetworkMetrics
{
    public float UploadMBPerSec { get; set; }
    public float DownloadMBPerSec { get; set; }
    public float TotalMBPerSec { get; set; }
    public DateTime Timestamp { get; set; }
}
