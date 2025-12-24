using System;
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

        // Get all available Network Interface instances
        PerformanceCounterCategory category;
        string[] instanceNames;

        try
        {
            category = new PerformanceCounterCategory("Network Interface");
            instanceNames = category.GetInstanceNames();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Network Interface instances: {ex.Message}");
            return adapters; // Return empty list if category is not available
        }

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            var stats = nic.GetIPv4Statistics();
            var adapterName = nic.Name;

            // Try to find matching instance name
            // Performance counter names may differ slightly from NetworkInterface.Name
            var matchingInstance = instanceNames.FirstOrDefault(name =>
                name.Equals(adapterName, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(adapterName) ||
                adapterName.Contains(name)
            );

            // Get or create performance counters
            if (!_sendCounters.ContainsKey(adapterName))
            {
                if (matchingInstance == null)
                {
                    Debug.WriteLine($"No matching performance counter instance for adapter: {adapterName}");

                    // Add adapter without rate info
                    adapters.Add(new NetworkAdapterInfo
                    {
                        Name = adapterName,
                        Type = nic.NetworkInterfaceType.ToString(),
                        SendBytesPerSec = 0,
                        ReceiveBytesPerSec = 0,
                        TotalBytesSent = stats.BytesSent,
                        TotalBytesReceived = stats.BytesReceived
                    });
                    continue;
                }

                try
                {
                    _sendCounters[adapterName] = new PerformanceCounter(
                        "Network Interface", "Bytes Sent/sec", matchingInstance);
                    _receiveCounters[adapterName] = new PerformanceCounter(
                        "Network Interface", "Bytes Received/sec", matchingInstance);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create performance counters for {adapterName}: {ex.Message}");

                    // Add adapter without rate info
                    adapters.Add(new NetworkAdapterInfo
                    {
                        Name = adapterName,
                        Type = nic.NetworkInterfaceType.ToString(),
                        SendBytesPerSec = 0,
                        ReceiveBytesPerSec = 0,
                        TotalBytesSent = stats.BytesSent,
                        TotalBytesReceived = stats.BytesReceived
                    });
                    continue;
                }
            }

            // Only add adapter if counters were successfully created
            if (!_sendCounters.ContainsKey(adapterName) || !_receiveCounters.ContainsKey(adapterName))
                continue;

            float sendRate = 0;
            float receiveRate = 0;

            try
            {
                sendRate = _sendCounters[adapterName].NextValue();
                receiveRate = _receiveCounters[adapterName].NextValue();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read performance counters for {adapterName}: {ex.Message}");
                // Continue with 0 values instead of skipping
            }

            adapters.Add(new NetworkAdapterInfo
            {
                Name = adapterName,
                Type = nic.NetworkInterfaceType.ToString(),
                SendBytesPerSec = sendRate,
                ReceiveBytesPerSec = receiveRate,
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
