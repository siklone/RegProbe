using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace WindowsOptimizer.Infrastructure.Metrics;

[SupportedOSPlatform("windows")]
public sealed class MetricProvider : IDisposable
{
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly PerformanceCounter? _diskReadCounter;
    private readonly PerformanceCounter? _diskWriteCounter;
    private bool _disposed;

    public MetricProvider()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            _diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            _diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize performance counters: {ex.Message}");
        }
    }

    public float GetCpuUsage() => _cpuCounter?.NextValue() ?? 0;
    
    public float GetAvailableRamMb() => _ramCounter?.NextValue() ?? 0;
    
    public float GetDiskIOBytesPerSec() 
    {
        var read = _diskReadCounter?.NextValue() ?? 0;
        var write = _diskWriteCounter?.NextValue() ?? 0;
        return read + write;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _diskReadCounter?.Dispose();
            _diskWriteCounter?.Dispose();
            _disposed = true;
        }
    }
}
