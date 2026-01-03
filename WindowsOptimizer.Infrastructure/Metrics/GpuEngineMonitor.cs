using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class GpuEngineMonitor : IDisposable
{
    private readonly Dictionary<string, PerformanceCounter> _counters = new(StringComparer.OrdinalIgnoreCase);
    private bool _countersInitialized;
    private bool _isAvailable = true;

    public GpuEngineUsageSnapshot GetUsageSnapshot()
    {
        EnsureCounters();
        if (!_isAvailable || _counters.Count == 0)
        {
            return new GpuEngineUsageSnapshot(0, 0, 0, 0, 0, false);
        }

        double engine3D = 0;
        double engineCopy = 0;
        double engineEncode = 0;
        double engineDecode = 0;

        foreach (var (instanceName, counter) in _counters)
        {
            float value;
            try
            {
                value = counter.NextValue();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GPU Engine counter read failed ({instanceName}): {ex.Message}");
                continue;
            }

            var engineType = ParseEngineType(instanceName);
            switch (engineType)
            {
                case "3d":
                    engine3D += value;
                    break;
                case "copy":
                    engineCopy += value;
                    break;
                case "videoencode":
                    engineEncode += value;
                    break;
                case "videodecode":
                    engineDecode += value;
                    break;
            }
        }

        var total = engine3D + engineCopy + engineEncode + engineDecode;
        if (total > 100)
        {
            total = 100;
        }

        return new GpuEngineUsageSnapshot(total, engine3D, engineCopy, engineEncode, engineDecode, true);
    }

    private void EnsureCounters()
    {
        if (_countersInitialized || !_isAvailable)
        {
            return;
        }

        _countersInitialized = true;
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            foreach (var instanceName in category.GetInstanceNames())
            {
                if (string.IsNullOrWhiteSpace(instanceName) ||
                    !instanceName.Contains("engtype_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    _counters[instanceName] = new PerformanceCounter(
                        "GPU Engine",
                        "Utilization Percentage",
                        instanceName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create GPU Engine counter for {instanceName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GPU Engine performance counters not available: {ex.Message}");
            _isAvailable = false;
        }
    }

    private static string ParseEngineType(string instanceName)
    {
        var markerIndex = instanceName.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return string.Empty;
        }

        var start = markerIndex + "engtype_".Length;
        var end = instanceName.IndexOf(' ', start);
        if (end < 0)
        {
            end = instanceName.Length;
        }

        var engineType = instanceName[start..end].Trim().ToLowerInvariant();
        return engineType switch
        {
            "3d" => "3d",
            "copy" => "copy",
            "videoencode" => "videoencode",
            "videodecode" => "videodecode",
            _ => engineType
        };
    }

    public void Dispose()
    {
        foreach (var counter in _counters.Values)
        {
            counter.Dispose();
        }
        _counters.Clear();
    }
}
