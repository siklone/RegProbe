using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTraceProject.Infrastructure.Processes;

/// <summary>
/// Provides memory management operations for processes.
/// </summary>
public class ProcessMemoryManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    /// <summary>
    /// Trims the working set of a process, releasing memory back to the system.
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <returns>True if successful.</returns>
    public bool TrimWorkingSet(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            var before = process.WorkingSet64;

            // -1, -1 tells Windows to trim the working set
            var result = SetProcessWorkingSetSize(process.Handle, new IntPtr(-1), new IntPtr(-1));

            if (result)
            {
                process.Refresh();
                var after = process.WorkingSet64;
                var saved = (before - after) / (1024.0 * 1024.0);
                Debug.WriteLine($"[ProcessMemoryManager] PID {pid}: Trimmed {saved:F1} MB");
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessMemoryManager] Failed to trim working set for {pid}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Empties the working set of a process (more aggressive than TrimWorkingSet).
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <returns>True if successful.</returns>
    public bool EmptyProcessWorkingSet(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            var before = process.WorkingSet64;

            var result = EmptyWorkingSet(process.Handle);

            if (result)
            {
                process.Refresh();
                var after = process.WorkingSet64;
                var saved = (before - after) / (1024.0 * 1024.0);
                Debug.WriteLine($"[ProcessMemoryManager] PID {pid}: Emptied working set, freed {saved:F1} MB");
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessMemoryManager] Failed to empty working set for {pid}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets memory information for a process.
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <returns>Memory info, or null if not accessible.</returns>
    public ProcessMemoryInfo? GetMemoryInfo(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            process.Refresh();

            return new ProcessMemoryInfo
            {
                WorkingSetBytes = process.WorkingSet64,
                PrivateMemoryBytes = process.PrivateMemorySize64,
                VirtualMemoryBytes = process.VirtualMemorySize64,
                PagedMemoryBytes = process.PagedMemorySize64,
                PeakWorkingSetBytes = process.PeakWorkingSet64
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Trims working set of all accessible processes.
    /// </summary>
    /// <returns>Number of processes trimmed.</returns>
    public int TrimAllProcesses()
    {
        var trimmed = 0;
        foreach (var process in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                // Skip system processes and current process
                if (process.Id < 4 || process.Id == Environment.ProcessId)
                    continue;

                SetProcessWorkingSetSize(process.Handle, new IntPtr(-1), new IntPtr(-1));
                trimmed++;
            }
            catch
            {
                // Skip inaccessible processes
            }
            finally
            {
                process.Dispose();
            }
        }

        Debug.WriteLine($"[ProcessMemoryManager] Trimmed working set of {trimmed} processes");
        return trimmed;
    }
}

/// <summary>
/// Memory information for a process.
/// </summary>
public class ProcessMemoryInfo
{
    /// <summary>Current working set (physical memory) in bytes.</summary>
    public long WorkingSetBytes { get; init; }

    /// <summary>Private memory in bytes.</summary>
    public long PrivateMemoryBytes { get; init; }

    /// <summary>Virtual memory in bytes.</summary>
    public long VirtualMemoryBytes { get; init; }

    /// <summary>Paged memory in bytes.</summary>
    public long PagedMemoryBytes { get; init; }

    /// <summary>Peak working set in bytes.</summary>
    public long PeakWorkingSetBytes { get; init; }

    /// <summary>Working set in MB.</summary>
    public double WorkingSetMB => WorkingSetBytes / (1024.0 * 1024.0);

    /// <summary>Private memory in MB.</summary>
    public double PrivateMemoryMB => PrivateMemoryBytes / (1024.0 * 1024.0);
}
