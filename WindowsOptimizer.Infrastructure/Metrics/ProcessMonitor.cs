using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class ProcessMonitor
{
    private Dictionary<int, (DateTime Time, TimeSpan TotalProcessorTime)> _previousCpuUsage = new();

    public List<ProcessInfo> GetTopProcessesByCpu(int count = 10)
    {
        var processes = new List<ProcessInfo>();
        var currentTime = DateTime.UtcNow;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var cpuUsage = CalculateCpuUsage(process, currentTime);
                var ramMb = process.WorkingSet64 / (1024.0 * 1024.0);

                processes.Add(new ProcessInfo
                {
                    Name = process.ProcessName,
                    Pid = process.Id,
                    CpuPercent = cpuUsage,
                    RamMb = ramMb
                });
            }
            catch
            {
                // Process may have exited, skip
            }
        }

        return processes.OrderByDescending(p => p.CpuPercent).Take(count).ToList();
    }

    public List<ProcessInfo> GetTopProcessesByRam(int count = 10)
    {
        var processes = new List<ProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var ramMb = process.WorkingSet64 / (1024.0 * 1024.0);

                processes.Add(new ProcessInfo
                {
                    Name = process.ProcessName,
                    Pid = process.Id,
                    RamMb = ramMb,
                    CpuPercent = 0 // Not needed for RAM sort
                });
            }
            catch { }
        }

        return processes.OrderByDescending(p => p.RamMb).Take(count).ToList();
    }

    private double CalculateCpuUsage(Process process, DateTime currentTime)
    {
        var pid = process.Id;
        var currentTotalTime = process.TotalProcessorTime;

        if (_previousCpuUsage.TryGetValue(pid, out var previous))
        {
            var timeDiff = (currentTime - previous.Time).TotalMilliseconds;
            var cpuDiff = (currentTotalTime - previous.TotalProcessorTime).TotalMilliseconds;

            if (timeDiff > 0)
            {
                var cpuUsage = (cpuDiff / (timeDiff * Environment.ProcessorCount)) * 100.0;
                _previousCpuUsage[pid] = (currentTime, currentTotalTime);
                return Math.Min(cpuUsage, 100.0); // Cap at 100%
            }
        }

        _previousCpuUsage[pid] = (currentTime, currentTotalTime);
        return 0;
    }

    public void Cleanup()
    {
        // Remove entries for processes that no longer exist
        var currentPids = Process.GetProcesses().Select(p => p.Id).ToHashSet();
        var deadPids = _previousCpuUsage.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
        foreach (var pid in deadPids)
        {
            _previousCpuUsage.Remove(pid);
        }
    }
}

public sealed class ProcessInfo
{
    public string Name { get; set; } = string.Empty;
    public int Pid { get; set; }
    public double CpuPercent { get; set; }
    public double RamMb { get; set; }
}
