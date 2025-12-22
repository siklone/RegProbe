using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WindowsOptimizer.Infrastructure.Metrics;

[SupportedOSPlatform("windows")]
public sealed class KernelImpactAnalyzer
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);

    private long _lastIdleTime;
    private long _lastKernelTime;
    private long _lastUserTime;

    public KernelImpactAnalyzer()
    {
        Refresh();
    }

    public void Refresh()
    {
        GetSystemTimes(out _lastIdleTime, out _lastKernelTime, out _lastUserTime);
    }

    public double GetKernelEfficiencyScore()
    {
        long currentIdle, currentKernel, currentUser;
        if (!GetSystemTimes(out currentIdle, out currentKernel, out currentUser))
            return 0;

        var idleDiff = currentIdle - _lastIdleTime;
        var kernelDiff = currentKernel - _lastKernelTime;
        var userDiff = currentUser - _lastUserTime;

        _lastIdleTime = currentIdle;
        _lastKernelTime = currentKernel;
        _lastUserTime = currentUser;

        var totalSystemTime = kernelDiff + userDiff;
        if (totalSystemTime == 0) return 0;

        // Efficiency = (Total System Time - Idle Time) / Total System Time
        // This is a proxy for "System Busy-ness" at the kernel level
        double efficiency = (double)(totalSystemTime - idleDiff) / totalSystemTime;
        return Math.Max(0, Math.Min(1, efficiency));
    }
}
