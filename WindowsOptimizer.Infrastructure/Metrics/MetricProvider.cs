using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    private readonly double? _gpuMemoryTotalMb;
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

        _gpuMemoryTotalMb = TryGetGpuMemoryTotalMb();
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
        if (_computer == null) return double.NaN;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                hardware.Update();
                var temps = hardware.Sensors
                    .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue)
                    .ToList();

                if (temps.Count == 0)
                {
                    continue;
                }

                var preferred = temps.FirstOrDefault(s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                                ?? temps.FirstOrDefault(s => s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase)
                                                             || s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase))
                                ?? temps.FirstOrDefault(s => s.Name.Contains("Core Average", StringComparison.OrdinalIgnoreCase))
                                ?? temps.FirstOrDefault(s => s.Name.Contains("CCD", StringComparison.OrdinalIgnoreCase));

                if (preferred?.Value is { } preferredValue)
                {
                    return preferredValue;
                }

                var coreTemps = temps
                    .Where(s => s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                                && !s.Name.Contains("Max", StringComparison.OrdinalIgnoreCase)
                                && !s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.Value!.Value)
                    .ToList();

                if (coreTemps.Count > 0)
                {
                    return coreTemps.Average();
                }

                return temps.Max(s => s.Value!.Value);
            }
        }

        return double.NaN;
    }

    public double GetGpuTemperature()
    {
        if (_computer == null) return double.NaN;

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
                        return sensor.Value ?? double.NaN;
                }
            }
        }
        return double.NaN;
    }

    public double GetGpuUsage()
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
                    if (sensor.SensorType == SensorType.Load &&
                        (sensor.Name.Contains("Core") || sensor.Name.Contains("GPU Core")))
                        return sensor.Value ?? 0;
                }
            }
        }
        return 0;
    }

    public GpuMemorySnapshot GetGpuMemorySnapshot()
    {
        if (_computer == null)
        {
            return new GpuMemorySnapshot(0, _gpuMemoryTotalMb ?? 0, 0, false);
        }

        double? usedMb = null;
        double? totalMb = null;
        double? usagePercent = null;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (!sensor.Value.HasValue)
                    {
                        continue;
                    }

                    var name = sensor.Name ?? string.Empty;

                    if (sensor.SensorType == SensorType.Load &&
                        name.Contains("Memory", StringComparison.OrdinalIgnoreCase))
                    {
                        usagePercent ??= sensor.Value.Value;
                    }

                    if (sensor.SensorType == SensorType.Data || sensor.SensorType == SensorType.SmallData)
                    {
                        if (name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("GPU Memory", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("VRAM", StringComparison.OrdinalIgnoreCase))
                        {
                            usedMb ??= sensor.SensorType == SensorType.Data
                                ? sensor.Value.Value * 1024
                                : sensor.Value.Value;
                        }

                        if (name.Contains("Memory Total", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Total Memory", StringComparison.OrdinalIgnoreCase))
                        {
                            totalMb ??= sensor.SensorType == SensorType.Data
                                ? sensor.Value.Value * 1024
                                : sensor.Value.Value;
                        }
                    }
                }

                break;
            }
        }

        totalMb ??= _gpuMemoryTotalMb;

        if (!usagePercent.HasValue && usedMb.HasValue && totalMb.HasValue && totalMb > 0)
        {
            usagePercent = usedMb.Value / totalMb.Value * 100.0;
        }

        if (!usedMb.HasValue && usagePercent.HasValue && totalMb.HasValue && totalMb > 0)
        {
            usedMb = totalMb.Value * usagePercent.Value / 100.0;
        }

        var isAvailable = usagePercent.HasValue || usedMb.HasValue;

        return new GpuMemorySnapshot(
            usedMb ?? 0,
            totalMb ?? 0,
            usagePercent ?? 0,
            isAvailable);
    }

    public FanSpeedSnapshot GetFanSpeedSnapshot()
    {
        if (_computer == null)
        {
            return new FanSpeedSnapshot(double.NaN, double.NaN);
        }

        double? cpuRpm = null;
        double? cpuFallback = null;
        double? gpuRpm = null;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu ||
                hardware.HardwareType == HardwareType.Motherboard)
            {
                UpdateHardware(hardware);

                foreach (var sensor in EnumerateSensors(hardware))
                {
                    if (sensor.SensorType != SensorType.Fan || !sensor.Value.HasValue)
                    {
                        continue;
                    }

                    var name = sensor.Name ?? string.Empty;
                    if (IsCpuFanName(name))
                    {
                        cpuRpm = Math.Max(cpuRpm ?? 0, sensor.Value.Value);
                    }
                    else
                    {
                        cpuFallback = Math.Max(cpuFallback ?? 0, sensor.Value.Value);
                    }
                }
            }

            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                UpdateHardware(hardware);

                foreach (var sensor in EnumerateSensors(hardware))
                {
                    if (sensor.SensorType != SensorType.Fan || !sensor.Value.HasValue)
                    {
                        continue;
                    }

                    gpuRpm = Math.Max(gpuRpm ?? 0, sensor.Value.Value);
                }
            }
        }

        cpuRpm ??= cpuFallback;
        return new FanSpeedSnapshot(cpuRpm ?? double.NaN, gpuRpm ?? double.NaN);
    }

    public DiskHealthSnapshot GetDiskHealthSnapshot()
    {
        double? healthPercent = null;
        bool? predictFailure = null;

        if (_computer != null)
        {
            var values = new List<double>();
            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Storage)
                {
                    continue;
                }

                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    if (!sensor.Value.HasValue)
                    {
                        continue;
                    }

                    if (sensor.SensorType == SensorType.Level &&
                        (sensor.Name.Contains("Health", StringComparison.OrdinalIgnoreCase) ||
                         sensor.Name.Contains("Remaining Life", StringComparison.OrdinalIgnoreCase)))
                    {
                        values.Add(sensor.Value.Value);
                    }
                }
            }

            if (values.Count > 0)
            {
                healthPercent = values.Min();
            }
        }

        if (!healthPercent.HasValue)
        {
            healthPercent = TryGetStorageReliabilityHealth();
        }

        if (!healthPercent.HasValue)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT PredictFailure FROM MSStorageDriver_FailurePredictStatus");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["PredictFailure"] is bool predicted)
                    {
                        predictFailure = predicted;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read disk SMART status: {ex.Message}");
            }
        }

        if (!predictFailure.HasValue)
        {
            predictFailure = TryGetStorageHealthStatus();
        }

        return new DiskHealthSnapshot(healthPercent, predictFailure);
    }

    public SystemInfo GetSystemInfo()
    {
        var info = new SystemInfo();

        try
        {
            // Get OS info
            using (var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.OsName = obj["Caption"]?.ToString() ?? "Unknown";
                    info.OsVersion = obj["Version"]?.ToString() ?? "Unknown";
                }
            }

            // Get CPU info
            using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.CpuName = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
                    info.CpuCores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                    info.CpuThreads = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                    break; // Take first CPU
                }
            }

            // Get system uptime
            using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var bootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    info.Uptime = DateTime.Now - bootTime;
                }
            }

            // Get GPU name
            if (_computer != null)
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.GpuNvidia ||
                        hardware.HardwareType == HardwareType.GpuAmd ||
                        hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        info.GpuName = hardware.Name;
                        break;
                    }
                }
            }

            info.TotalRamGb = GetTotalRamGb();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get system info: {ex.Message}");
        }

        return info;
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

    private static void UpdateHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware)
        {
            sub.Update();
        }
    }

    private static IEnumerable<ISensor> EnumerateSensors(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            yield return sensor;
        }

        foreach (var sub in hardware.SubHardware)
        {
            foreach (var sensor in sub.Sensors)
            {
                yield return sensor;
            }
        }
    }

    private static bool IsCpuFanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Contains("CPU", StringComparison.OrdinalIgnoreCase)
               || name.Contains("Pump", StringComparison.OrdinalIgnoreCase)
               || name.Contains("AIO", StringComparison.OrdinalIgnoreCase)
               || name.Contains("Water", StringComparison.OrdinalIgnoreCase);
    }

    [SupportedOSPlatform("windows")]
    private static double? TryGetStorageReliabilityHealth()
    {
        try
        {
            using var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT PercentLifeUsed, RemainingLife, Wear FROM MSFT_StorageReliabilityCounter"));

            var values = new List<double>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var percentUsed = NormalizePercent(TryReadDouble(obj, "PercentLifeUsed"), invert: true);
                var remainingLife = NormalizePercent(TryReadDouble(obj, "RemainingLife"), invert: false);
                var wear = NormalizePercent(TryReadDouble(obj, "Wear"), invert: true);

                if (percentUsed.HasValue)
                {
                    values.Add(percentUsed.Value);
                }
                else if (remainingLife.HasValue)
                {
                    values.Add(remainingLife.Value);
                }
                else if (wear.HasValue)
                {
                    values.Add(wear.Value);
                }
            }

            return values.Count > 0 ? values.Min() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read storage reliability counters: {ex.Message}");
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool? TryGetStorageHealthStatus()
    {
        try
        {
            using var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT HealthStatus FROM MSFT_PhysicalDisk"));

            bool? anyHealthy = null;
            foreach (ManagementObject obj in searcher.Get())
            {
                var healthStatus = TryReadInt(obj, "HealthStatus");
                if (!healthStatus.HasValue)
                {
                    continue;
                }

                switch (healthStatus.Value)
                {
                    case 1:
                        anyHealthy = anyHealthy ?? true;
                        break;
                    case 2:
                    case 3:
                        return true;
                }
            }

            return anyHealthy.HasValue ? false : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read physical disk health status: {ex.Message}");
            return null;
        }
    }

    private static double? NormalizePercent(double? value, bool invert)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var raw = value.Value;
        if (double.IsNaN(raw) || double.IsInfinity(raw) || raw < 0)
        {
            return null;
        }

        if (raw > 1000)
        {
            return null;
        }

        var normalized = invert ? 100.0 - raw : raw;
        if (normalized < 0) normalized = 0;
        if (normalized > 100) normalized = 100;
        return normalized;
    }

    private static double? TryReadDouble(ManagementBaseObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                return null;
            }

            return value switch
            {
                double d => d,
                float f => f,
                decimal m => (double)m,
                ushort us => us,
                short s => s,
                uint ui => ui,
                int i => i,
                long l => l,
                _ => double.TryParse(value.ToString(), out var parsed) ? parsed : null
            };
        }
        catch
        {
            return null;
        }
    }

    private static int? TryReadInt(ManagementBaseObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                return null;
            }

            return value switch
            {
                int i => i,
                uint ui => (int)ui,
                ushort us => us,
                short s => s,
                byte b => b,
                long l => (int)l,
                _ => int.TryParse(value.ToString(), out var parsed) ? parsed : null
            };
        }
        catch
        {
            return null;
        }
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

    private static double? TryGetGpuMemoryTotalMb()
    {
        try
        {
            double? best = null;
            using var searcher = new ManagementObjectSearcher("SELECT AdapterRAM FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["AdapterRAM"] == null)
                {
                    continue;
                }

                var bytes = Convert.ToDouble(obj["AdapterRAM"]);
                if (bytes <= 0)
                {
                    continue;
                }

                var mb = bytes / (1024 * 1024);
                if (!best.HasValue || mb > best.Value)
                {
                    best = mb;
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read GPU memory total: {ex.Message}");
            return null;
        }
    }
}

public sealed class SystemInfo
{
    public string OsName { get; set; } = "Unknown";
    public string OsVersion { get; set; } = "Unknown";
    public string CpuName { get; set; } = "Unknown";
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public string GpuName { get; set; } = "Unknown";
    public double TotalRamGb { get; set; }
    public TimeSpan Uptime { get; set; }

    public string UptimeFormatted => $"{(int)Uptime.TotalDays}d {Uptime.Hours}h {Uptime.Minutes}m";
}

public readonly record struct GpuMemorySnapshot(double UsedMb, double TotalMb, double UsagePercent, bool IsAvailable);

public readonly record struct FanSpeedSnapshot(double CpuRpm, double GpuRpm);

public readonly record struct DiskHealthSnapshot(double? HealthPercent, bool? PredictFailure);
