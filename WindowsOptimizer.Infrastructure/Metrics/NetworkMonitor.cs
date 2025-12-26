using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class NetworkMonitor
{
    private sealed record AdapterSample(long TotalBytesSent, long TotalBytesReceived, DateTimeOffset Timestamp);

    private readonly Dictionary<string, PerformanceCounter> _sendCountersById = new();
    private readonly Dictionary<string, PerformanceCounter> _receiveCountersById = new();
    private readonly Dictionary<string, AdapterSample> _lastSamplesById = new();

    public List<NetworkAdapterInfo> GetActiveAdapters()
    {
        var adapters = new List<NetworkAdapterInfo>();

        // Get all available Network Interface instances (Windows only). If unavailable, fall back to
        // NetworkInterface statistics and compute rates using deltas.
        string[] instanceNames = Array.Empty<string>();
        var perfCountersAvailable = false;

        try
        {
            var category = new PerformanceCounterCategory("Network Interface");
            instanceNames = category.GetInstanceNames();
            perfCountersAvailable = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Network Interface instances: {ex.Message}");
        }

        var now = DateTimeOffset.UtcNow;
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            var (totalBytesSent, totalBytesReceived) = GetTotalBytes(nic);
            var adapterName = nic.Name;
            var adapterId = nic.Id;

            activeIds.Add(adapterId);

            float sendRateBytesPerSec = 0;
            float receiveRateBytesPerSec = 0;

            if (perfCountersAvailable)
            {
                // Performance counter instance names usually correspond to NetworkInterface.Description,
                // not Name. We attempt both.
                if (!_sendCountersById.ContainsKey(adapterId) || !_receiveCountersById.ContainsKey(adapterId))
                {
                    var matchingInstance = FindMatchingInstance(instanceNames, nic);
                    if (matchingInstance == null)
                    {
                        Debug.WriteLine($"No matching performance counter instance for adapter: {adapterName} ({nic.Description})");
                    }
                    else
                    {
                        try
                        {
                            _sendCountersById[adapterId] = new PerformanceCounter(
                                "Network Interface", "Bytes Sent/sec", matchingInstance);
                            _receiveCountersById[adapterId] = new PerformanceCounter(
                                "Network Interface", "Bytes Received/sec", matchingInstance);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to create performance counters for {adapterName}: {ex.Message}");
                            RemoveCounters(adapterId);
                        }
                    }
                }

                if (_sendCountersById.TryGetValue(adapterId, out var sendCounter) &&
                    _receiveCountersById.TryGetValue(adapterId, out var receiveCounter))
                {
                    try
                    {
                        sendRateBytesPerSec = sendCounter.NextValue();
                        receiveRateBytesPerSec = receiveCounter.NextValue();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to read performance counters for {adapterName}: {ex.Message}");
                        // Keep 0 values and fall back to delta-based rates below.
                    }
                }
            }

            // Fallback: compute rates from byte deltas over time. This also handles cases where
            // performance counters are missing/unavailable or couldn't be matched.
            if (sendRateBytesPerSec <= 0 && receiveRateBytesPerSec <= 0)
            {
                (sendRateBytesPerSec, receiveRateBytesPerSec) =
                    ComputeRatesFromDeltas(adapterId, totalBytesSent, totalBytesReceived, now);
            }

            adapters.Add(new NetworkAdapterInfo
            {
                Name = adapterName,
                Type = nic.NetworkInterfaceType.ToString(),
                SendBytesPerSec = sendRateBytesPerSec,
                ReceiveBytesPerSec = receiveRateBytesPerSec,
                TotalBytesSent = totalBytesSent,
                TotalBytesReceived = totalBytesReceived
            });
        }

        // Cleanup counters/samples for adapters that are no longer active.
        CleanupInactive(activeIds);
        return adapters;
    }

    public void Dispose()
    {
        foreach (var counter in _sendCountersById.Values)
            counter.Dispose();
        foreach (var counter in _receiveCountersById.Values)
            counter.Dispose();

        _sendCountersById.Clear();
        _receiveCountersById.Clear();
        _lastSamplesById.Clear();
    }

    private static (long TotalBytesSent, long TotalBytesReceived) GetTotalBytes(NetworkInterface nic)
    {
        long totalSent = 0;
        long totalReceived = 0;

        try
        {
            var stats = nic.GetIPv4Statistics();
            totalSent += stats.BytesSent;
            totalReceived += stats.BytesReceived;
        }
        catch
        {
            // Ignore
        }

        try
        {
            var stats = nic.GetIPv6Statistics();
            totalSent += stats.BytesSent;
            totalReceived += stats.BytesReceived;
        }
        catch
        {
            // Ignore
        }

        return (totalSent, totalReceived);
    }

    private static string? FindMatchingInstance(string[] instanceNames, NetworkInterface nic)
    {
        var candidates = new[] { nic.Description, nic.Name };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var exact = instanceNames.FirstOrDefault(name =>
                name.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }
        }

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var fuzzy = instanceNames.FirstOrDefault(name =>
                name.Contains(candidate, StringComparison.OrdinalIgnoreCase) ||
                candidate.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (fuzzy != null)
            {
                return fuzzy;
            }
        }

        return null;
    }

    private (float SendBytesPerSec, float ReceiveBytesPerSec) ComputeRatesFromDeltas(
        string adapterId,
        long totalBytesSent,
        long totalBytesReceived,
        DateTimeOffset now)
    {
        float sendRate = 0;
        float receiveRate = 0;

        if (_lastSamplesById.TryGetValue(adapterId, out var last))
        {
            var elapsedSeconds = (now - last.Timestamp).TotalSeconds;
            if (elapsedSeconds > 0.001)
            {
                var sentDelta = totalBytesSent - last.TotalBytesSent;
                var receivedDelta = totalBytesReceived - last.TotalBytesReceived;

                if (sentDelta > 0)
                {
                    sendRate = (float)(sentDelta / elapsedSeconds);
                }

                if (receivedDelta > 0)
                {
                    receiveRate = (float)(receivedDelta / elapsedSeconds);
                }
            }
        }

        _lastSamplesById[adapterId] = new AdapterSample(totalBytesSent, totalBytesReceived, now);
        return (sendRate, receiveRate);
    }

    private void CleanupInactive(HashSet<string> activeIds)
    {
        foreach (var adapterId in _sendCountersById.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            RemoveCounters(adapterId);
        }

        foreach (var adapterId in _lastSamplesById.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            _lastSamplesById.Remove(adapterId);
        }
    }

    private void RemoveCounters(string adapterId)
    {
        if (_sendCountersById.Remove(adapterId, out var sendCounter))
        {
            sendCounter.Dispose();
        }

        if (_receiveCountersById.Remove(adapterId, out var receiveCounter))
        {
            receiveCounter.Dispose();
        }
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
