using LibreHardwareMonitor.Hardware;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Hardware sensor service using LibreHardwareMonitor for native access to:
/// - CPU: Voltage, per-core temps, power, clock speeds
/// - GPU: Voltage, temps, power, fan speed, clocks
/// - Motherboard: Voltages, temps
/// - Storage: Temps, SMART data
/// </summary>
public class HardwareSensorService : IDisposable
{
    private readonly Computer _computer;
    private bool _isDisposed;

    public HardwareSensorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = false, // Use WMI for network
            IsStorageEnabled = true
        };
        
        _computer.Open();
    }

    /// <summary>
    /// Get comprehensive hardware snapshot.
    /// Runs on background thread to avoid UI blocking.
    /// </summary>
    public async Task<HardwareSnapshot> GetSnapshotAsync()
    {
        return await Task.Run(() =>
        {
            var snapshot = new HardwareSnapshot
            {
                Timestamp = DateTime.UtcNow
            };

            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();

                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        ProcessCpu(hardware, snapshot);
                        break;
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        ProcessGpu(hardware, snapshot);
                        break;
                    case HardwareType.Memory:
                        ProcessMemory(hardware, snapshot);
                        break;
                    case HardwareType.Storage:
                        ProcessStorage(hardware, snapshot);
                        break;
                    case HardwareType.Motherboard:
                        ProcessMotherboard(hardware, snapshot);
                        break;
                }
            }

            return snapshot;
        });
    }

    private void ProcessCpu(IHardware hardware, HardwareSnapshot snapshot)
    {
        foreach (var sensor in hardware.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Voltage:
                    if (sensor.Name.Contains("CPU Core", StringComparison.OrdinalIgnoreCase))
                        snapshot.CpuVoltage = sensor.Value ?? 0;
                    break;

                case SensorType.Temperature:
                    if (sensor.Name.Contains("Core #", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.CpuCoreTemps.Add(sensor.Value ?? 0);
                    }
                    else if (sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    {
                        snapshot.CpuPackageTemp = sensor.Value ?? 0;
                    }
                    break;

                case SensorType.Power:
                    if (sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                        snapshot.CpuPower = sensor.Value ?? 0;
                    break;

                case SensorType.Clock:
                    if (sensor.Name.Contains("Core #", StringComparison.OrdinalIgnoreCase))
                        snapshot.CpuCoreClocks.Add(sensor.Value ?? 0);
                    break;

                case SensorType.Load:
                    if (sensor.Name == "CPU Total")
                        snapshot.CpuUsageTotal = sensor.Value ?? 0;
                    break;
            }
        }
    }

    private void ProcessGpu(IHardware hardware, HardwareSnapshot snapshot)
    {
        snapshot.GpuName = hardware.Name;

        foreach (var sensor in hardware.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Voltage:
                    if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuCoreVoltage = sensor.Value ?? 0;
                    break;

                case SensorType.Temperature:
                    if (sensor.Name.Contains("Hotspot", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuHotspotTemp = sensor.Value ?? 0;
                    else if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuCoreTemp = sensor.Value ?? 0;
                    else if (sensor.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuMemoryTemp = sensor.Value ?? 0;
                    break;

                case SensorType.Power:
                    snapshot.GpuPower = sensor.Value ?? 0;
                    break;

                case SensorType.Clock:
                    if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuCoreClock = sensor.Value ?? 0;
                    else if (sensor.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuMemoryClock = sensor.Value ?? 0;
                    break;

                case SensorType.Load:
                    if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuUsage = sensor.Value ?? 0;
                    break;

                case SensorType.SmallData:
                    if (sensor.Name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase))
                        snapshot.GpuMemoryUsedMB = sensor.Value ?? 0;
                    break;

                case SensorType.Fan:
                    snapshot.GpuFanSpeed = sensor.Value ?? 0;
                    break;
            }
        }
    }

    private void ProcessMemory(IHardware hardware, HardwareSnapshot snapshot)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType == SensorType.Load && sensor.Name == "Memory")
            {
                snapshot.MemoryUsagePercent = sensor.Value ?? 0;
            }
        }
    }

    private void ProcessStorage(IHardware hardware, HardwareSnapshot snapshot)
    {
        var diskInfo = new DiskInfo
        {
            Name = hardware.Name
        };

        foreach (var sensor in hardware.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Temperature:
                    diskInfo.Temperature = sensor.Value ?? 0;
                    break;
                case SensorType.Load:
                    if (sensor.Name.Contains("Used Space", StringComparison.OrdinalIgnoreCase))
                        diskInfo.UsedSpacePercent = sensor.Value ?? 0;
                    break;
                case SensorType.Throughput:
                    if (sensor.Name.Contains("Read", StringComparison.OrdinalIgnoreCase))
                        diskInfo.ReadMBps = (sensor.Value ?? 0) / 1024 / 1024; // bytes to MB
                    else if (sensor.Name.Contains("Write", StringComparison.OrdinalIgnoreCase))
                        diskInfo.WriteMBps = (sensor.Value ?? 0) / 1024 / 1024;
                    break;
                case SensorType.Data:
                    if (sensor.Name.Contains("Total Read", StringComparison.OrdinalIgnoreCase) ||
                        sensor.Name.Contains("Host Read", StringComparison.OrdinalIgnoreCase))
                        diskInfo.TotalReadsGB = sensor.Value ?? 0;
                    else if (sensor.Name.Contains("Total Write", StringComparison.OrdinalIgnoreCase) ||
                             sensor.Name.Contains("Host Write", StringComparison.OrdinalIgnoreCase))
                        diskInfo.TotalWritesGB = sensor.Value ?? 0;
                    break;
                case SensorType.Level:
                    if (sensor.Name.Contains("Life", StringComparison.OrdinalIgnoreCase) ||
                        sensor.Name.Contains("Health", StringComparison.OrdinalIgnoreCase) ||
                        sensor.Name.Contains("Wear", StringComparison.OrdinalIgnoreCase))
                    {
                        diskInfo.HealthPercent = (int)(sensor.Value ?? 0);
                        diskInfo.HasSmartData = true;
                    }
                    break;
                case SensorType.TimeSpan:
                    if (sensor.Name.Contains("Power-On", StringComparison.OrdinalIgnoreCase))
                        diskInfo.PowerOnHours = (int)(sensor.Value ?? 0);
                    break;
            }
        }

        snapshot.Disks.Add(diskInfo);
    }

    private void ProcessMotherboard(IHardware hardware, HardwareSnapshot snapshot)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType == SensorType.Voltage)
            {
                snapshot.MotherboardVoltages.Add(new VoltageInfo
                {
                    Name = sensor.Name,
                    Value = sensor.Value ?? 0
                });
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _computer.Close();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Comprehensive hardware snapshot structure.
/// Uses value types where possible to reduce heap allocations.
/// </summary>
public class HardwareSnapshot
{
    public DateTime Timestamp { get; set; }

    // CPU
    public float CpuVoltage { get; set; }
    public float CpuPackageTemp { get; set; }
    public List<float> CpuCoreTemps { get; set; } = new();
    public float CpuPower { get; set; }
    public List<float> CpuCoreClocks { get; set; } = new();
    public float CpuUsageTotal { get; set; }

    // GPU
    public string GpuName { get; set; } = string.Empty;
    public float GpuCoreVoltage { get; set; }
    public float GpuHotspotTemp { get; set; }
    public float GpuCoreTemp { get; set; }
    public float GpuMemoryTemp { get; set; }
    public float GpuPower { get; set; }
    public float GpuCoreClock { get; set; }
    public float GpuMemoryClock { get; set; }
    public float GpuUsage { get; set; }
    public float GpuMemoryUsedMB { get; set; }
    public float GpuFanSpeed { get; set; }

    // Memory (Enhanced)
    public float MemoryUsagePercent { get; set; }
    public float MemoryUsedGB { get; set; }
    public float MemoryTotalGB { get; set; }
    public float MemoryAvailableGB { get; set; }

    // Storage (Enhanced with SMART)
    public List<DiskInfo> Disks { get; set; } = new();

    // Motherboard
    public List<VoltageInfo> MotherboardVoltages { get; set; } = new();
}

public class DiskInfo
{
    public string Name { get; set; } = string.Empty;
    public string DriveLetter { get; set; } = string.Empty;
    public float Temperature { get; set; }
    public float UsedSpacePercent { get; set; }
    public float ReadMBps { get; set; }
    public float WriteMBps { get; set; }
    
    // SMART Data
    public int HealthPercent { get; set; } = -1; // -1 = unavailable
    public int PowerOnHours { get; set; }
    public int PowerCycles { get; set; }
    public float TotalReadsGB { get; set; }
    public float TotalWritesGB { get; set; }
    public bool HasSmartData { get; set; }
}

public record struct VoltageInfo(string Name, float Value);

public class NetworkInterfaceInfo
{
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public float UploadMbps { get; set; }
    public float DownloadMbps { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    public long PacketsSent { get; set; }
    public long PacketsReceived { get; set; }
    public long PacketErrors { get; set; }
    public long PacketDropped { get; set; }
    public bool IsUp { get; set; }
}

