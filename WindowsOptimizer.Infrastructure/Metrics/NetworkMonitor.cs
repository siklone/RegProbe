using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class NetworkMonitor
{
    private readonly Dictionary<string, PerformanceCounter> _sendCounters = new();
    private readonly Dictionary<string, PerformanceCounter> _receiveCounters = new();

    public List<NetworkAdapterInfo> GetActiveAdapters()
    {
        var adapters = new List<NetworkAdapterInfo>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            var stats = nic.GetIPv4Statistics();
            var adapterName = nic.Name;

            // Get or create performance counters
            if (!_sendCounters.ContainsKey(adapterName))
            {
                try
                {
                    _sendCounters[adapterName] = new PerformanceCounter(
                        "Network Interface", "Bytes Sent/sec", adapterName);
                    _receiveCounters[adapterName] = new PerformanceCounter(
                        "Network Interface", "Bytes Received/sec", adapterName);
                }
                catch { continue; }
            }

            adapters.Add(new NetworkAdapterInfo
            {
                Name = adapterName,
                Type = nic.NetworkInterfaceType.ToString(),
                SendBytesPerSec = _sendCounters[adapterName].NextValue(),
                ReceiveBytesPerSec = _receiveCounters[adapterName].NextValue(),
                TotalBytesSent = stats.BytesSent,
                TotalBytesReceived = stats.BytesReceived
            });
        }

        return adapters;
    }

    public void Dispose()
    {
        foreach (var counter in _sendCounters.Values)
            counter?.Dispose();
        foreach (var counter in _receiveCounters.Values)
            counter?.Dispose();
    }
}

public sealed class NetworkAdapterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public float SendBytesPerSec { get; set; }
    public float ReceiveBytesPerSec { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }

    public double SendMbps => (SendBytesPerSec * 8) / (1024 * 1024);
    public double ReceiveMbps => (ReceiveBytesPerSec * 8) / (1024 * 1024);
}
