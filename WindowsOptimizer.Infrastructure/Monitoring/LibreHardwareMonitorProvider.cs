using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace WindowsOptimizer.Infrastructure.Monitoring;

public sealed class LibreHardwareMonitorProvider : ISensorProvider, IDisposable
{
    private readonly Computer _computer;
    private bool _opened;

    public LibreHardwareMonitorProvider()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true
        };
    }

    public Task<MonitoringSnapshot> CaptureAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled<MonitoringSnapshot>(ct);
        }

        EnsureOpen();

        var readings = new List<SensorReading>();
        foreach (var hardware in _computer.Hardware)
        {
            CollectHardware(readings, hardware);
        }

        var timestamp = DateTimeOffset.UtcNow;
        return Task.FromResult(new MonitoringSnapshot(readings, timestamp, "LibreHardwareMonitor"));
    }

    private void EnsureOpen()
    {
        if (_opened)
        {
            return;
        }

        _computer.Open();
        _opened = true;
    }

    private static void CollectHardware(List<SensorReading> readings, IHardware hardware)
    {
        hardware.Update();

        foreach (var subHardware in hardware.SubHardware)
        {
            CollectHardware(readings, subHardware);
        }

        foreach (var sensor in hardware.Sensors)
        {
            if (!TryMapSensor(sensor, out var type, out var unit))
            {
                continue;
            }

            if (!sensor.Value.HasValue)
            {
                continue;
            }

            readings.Add(new SensorReading(
                sensor.Name,
                type,
                sensor.Value.Value,
                unit,
                DateTimeOffset.UtcNow,
                hardware.HardwareType.ToString()));
        }
    }

    private static bool TryMapSensor(ISensor sensor, out SensorType type, out string unit)
    {
        switch (sensor.SensorType)
        {
            case LibreHardwareMonitor.Hardware.SensorType.Temperature:
                type = SensorType.Temperature;
                unit = "C";
                return true;
            case LibreHardwareMonitor.Hardware.SensorType.Voltage:
                type = SensorType.Voltage;
                unit = "V";
                return true;
            case LibreHardwareMonitor.Hardware.SensorType.Fan:
                type = SensorType.Fan;
                unit = "RPM";
                return true;
            case LibreHardwareMonitor.Hardware.SensorType.Load:
                type = SensorType.Load;
                unit = "%";
                return true;
            case LibreHardwareMonitor.Hardware.SensorType.Power:
                type = SensorType.Power;
                unit = "W";
                return true;
            case LibreHardwareMonitor.Hardware.SensorType.Clock:
                type = SensorType.Clock;
                unit = "MHz";
                return true;
            case LibreHardwareMonitor.Hardware.SensorType.Data:
                type = SensorType.Data;
                unit = "GB";
                return true;
            case LibreHardwareMonitor.Hardware.SensorType.Throughput:
                type = SensorType.Throughput;
                unit = "MB/s";
                return true;
            default:
                type = SensorType.Other;
                unit = string.Empty;
                return false;
        }
    }

    public void Dispose()
    {
        _computer.Close();
    }
}
