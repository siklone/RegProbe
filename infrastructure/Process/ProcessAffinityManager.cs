using System;
using System.Diagnostics;
using System.Linq;

namespace OpenTraceProject.Infrastructure.Processes;

/// <summary>
/// Manages CPU affinity for processes.
/// </summary>
public class ProcessAffinityManager
{
    private static readonly int ProcessorCount = Environment.ProcessorCount;

    /// <summary>
    /// Gets the CPU affinity mask for a process.
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <returns>Affinity mask, or IntPtr.Zero if not accessible.</returns>
    public IntPtr GetAffinity(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return process.ProcessorAffinity;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Sets the CPU affinity mask for a process.
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <param name="affinityMask">Bitmask of allowed CPUs.</param>
    /// <returns>True if successful.</returns>
    public bool SetAffinity(int pid, IntPtr affinityMask)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            var oldAffinity = process.ProcessorAffinity;
            process.ProcessorAffinity = affinityMask;

            Debug.WriteLine($"[ProcessAffinityManager] PID {pid}: 0x{oldAffinity:X} -> 0x{affinityMask:X}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessAffinityManager] Failed to set affinity for {pid}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets the CPU affinity using specific core indices.
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <param name="coreIndices">Array of core indices (0-based).</param>
    /// <returns>True if successful.</returns>
    public bool SetAffinity(int pid, int[] coreIndices)
    {
        long mask = 0;
        foreach (var core in coreIndices)
        {
            if (core >= 0 && core < ProcessorCount)
                mask |= (1L << core);
        }
        return SetAffinity(pid, new IntPtr(mask));
    }

    /// <summary>
    /// Gets the indices of cores enabled in an affinity mask.
    /// </summary>
    public int[] GetCoreIndices(IntPtr affinityMask)
    {
        var mask = affinityMask.ToInt64();
        return Enumerable.Range(0, ProcessorCount)
            .Where(i => (mask & (1L << i)) != 0)
            .ToArray();
    }

    /// <summary>
    /// Gets a human-readable description of an affinity mask.
    /// </summary>
    public string GetAffinityDescription(IntPtr affinityMask)
    {
        var cores = GetCoreIndices(affinityMask);
        if (cores.Length == 0) return "None";
        if (cores.Length == ProcessorCount) return "All Cores";
        if (cores.Length == 1) return $"Core {cores[0]}";
        return $"Cores {string.Join(",", cores)}";
    }

    #region Preset Affinity Masks

    /// <summary>
    /// Gets an affinity mask for all cores.
    /// </summary>
    public static IntPtr AllCores => new IntPtr((1L << ProcessorCount) - 1);

    /// <summary>
    /// Gets an affinity mask for only the first core.
    /// </summary>
    public static IntPtr SingleCore => new IntPtr(1);

    /// <summary>
    /// Gets an affinity mask for even-numbered cores.
    /// </summary>
    public static IntPtr EvenCores
    {
        get
        {
            long mask = 0;
            for (int i = 0; i < ProcessorCount; i += 2)
                mask |= (1L << i);
            return new IntPtr(mask);
        }
    }

    /// <summary>
    /// Gets an affinity mask for odd-numbered cores.
    /// </summary>
    public static IntPtr OddCores
    {
        get
        {
            long mask = 0;
            for (int i = 1; i < ProcessorCount; i += 2)
                mask |= (1L << i);
            return new IntPtr(mask);
        }
    }

    /// <summary>
    /// Gets an affinity mask for first half of cores (P-cores on hybrid CPUs).
    /// </summary>
    public static IntPtr PerformanceCores
    {
        get
        {
            var pCoreCount = ProcessorCount / 2;
            return new IntPtr((1L << pCoreCount) - 1);
        }
    }

    /// <summary>
    /// Gets an affinity mask for second half of cores (E-cores on hybrid CPUs).
    /// </summary>
    public static IntPtr EfficiencyCores
    {
        get
        {
            var pCoreCount = ProcessorCount / 2;
            var eCoreCount = ProcessorCount - pCoreCount;
            return new IntPtr(((1L << eCoreCount) - 1) << pCoreCount);
        }
    }

    /// <summary>
    /// Creates an affinity mask for a specified number of cores.
    /// </summary>
    public static IntPtr FirstNCores(int n)
    {
        n = Math.Clamp(n, 1, ProcessorCount);
        return new IntPtr((1L << n) - 1);
    }

    #endregion

    /// <summary>
    /// Gets the total number of logical processors.
    /// </summary>
    public static int TotalCores => ProcessorCount;
}
