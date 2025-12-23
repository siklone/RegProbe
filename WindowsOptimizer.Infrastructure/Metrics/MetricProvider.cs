using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;
using LibreHardwareMonitor.Hardware;

namespace WindowsOptimizer.Infrastructure.Metrics;

[SupportedOSPlatform("windows")]
public sealed class MetricProvider : IDisposable
{
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly PerformanceCounter? _diskReadCounter;
    private readonly PerformanceCounter? _diskWriteCounter;
    private readonly Computer? _computer;
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

        // Initialize LibreHardwareMonitor for temperature sensors
        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true
            };
            _computer.Open();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize LibreHardwareMonitor: {ex.Message}");
            _computer = null;
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

    public double GetCpuTemperature()
    {
        if (_computer == null) return 0;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature &&
                        (sensor.Name.Contains("Package") || sensor.Name.Contains("Core Average")))
                        return sensor.Value ?? 0;
                }
            }
        }
        return 0;
    }

    public double GetGpuTemperature()
    {
        if (_computer == null) return 0;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature &&
                        (sensor.Name.Contains("Core") || sensor.Name.Contains("GPU")))
                        return sensor.Value ?? 0;
                }
            }
        }
        return 0;
    }

    public double GetTotalRamGb()
    {
        var totalRam = 0.0;
        try
        {
            var query = new ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory");
            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get())
            {
                totalRam += Convert.ToDouble(obj["Capacity"]);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get total RAM: {ex.Message}");
        }
        return totalRam / (1024 * 1024 * 1024); // Convert to GB
    }

    public double GetUsedRamGb()
    {
        var availableMb = GetAvailableRamMb();
        var totalGb = GetTotalRamGb();
        return totalGb - (availableMb / 1024.0);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _diskReadCounter?.Dispose();
            _diskWriteCounter?.Dispose();
            _computer?.Close();
            _disposed = true;
        }
    }
}
