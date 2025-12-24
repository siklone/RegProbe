using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class DiskMonitor
{
    private readonly Dictionary<string, (PerformanceCounter Read, PerformanceCounter Write)> _counters = new();

    public List<DiskInfo> GetDiskActivity()
    {
        var disks = new List<DiskInfo>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed)
                continue;

            var driveName = drive.Name.TrimEnd('\\'); // "C:"
            var instanceName = $"{drive.Name[0]}:"; // Physical disk instance name

            if (!_counters.ContainsKey(driveName))
            {
                try
                {
                    _counters[driveName] = (
                        new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", instanceName),
                        new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", instanceName)
                    );
                }
                catch { continue; }
            }

            // Only add disk if counters were successfully created
            if (!_counters.ContainsKey(driveName))
                continue;

            var (readCounter, writeCounter) = _counters[driveName];

            float readRate = 0;
            float writeRate = 0;

            try
            {
                readRate = readCounter.NextValue();
                writeRate = writeCounter.NextValue();
            }
            catch
            {
                // Skip this disk if performance counters fail
                continue;
            }

            disks.Add(new DiskInfo
            {
                DriveLetter = driveName,
                TotalSizeGb = drive.TotalSize / (1024.0 * 1024 * 1024),
                FreeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                ReadBytesPerSec = readRate,
                WriteBytesPerSec = writeRate
            });
        }

        return disks;
    }

    public void Dispose()
    {
        foreach (var (read, write) in _counters.Values)
        {
            read?.Dispose();
            write?.Dispose();
        }
    }
}

public sealed class DiskInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public double TotalSizeGb { get; set; }
    public double FreeSpaceGb { get; set; }
    public float ReadBytesPerSec { get; set; }
    public float WriteBytesPerSec { get; set; }

    public double UsedSpaceGb => TotalSizeGb - FreeSpaceGb;
    public double UsedPercent => (UsedSpaceGb / TotalSizeGb) * 100.0;
    public double ReadMBps => ReadBytesPerSec / (1024 * 1024);
    public double WriteMBps => WriteBytesPerSec / (1024 * 1024);
}
