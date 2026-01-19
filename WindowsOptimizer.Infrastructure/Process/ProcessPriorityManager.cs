using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace WindowsOptimizer.Infrastructure.Processes;

/// <summary>
/// Manages process priority levels.
/// </summary>
public class ProcessPriorityManager
{
    /// <summary>
    /// Available priority levels with display names.
    /// </summary>
    public static readonly Dictionary<string, ProcessPriorityClass> PriorityLevels = new()
    {
        ["Realtime"] = ProcessPriorityClass.RealTime,      // Requires admin
        ["High"] = ProcessPriorityClass.High,
        ["Above Normal"] = ProcessPriorityClass.AboveNormal,
        ["Normal"] = ProcessPriorityClass.Normal,
        ["Below Normal"] = ProcessPriorityClass.BelowNormal,
        ["Idle"] = ProcessPriorityClass.Idle
    };

    /// <summary>
    /// Gets the display name for a priority class.
    /// </summary>
    public static string GetPriorityName(ProcessPriorityClass priority) => priority switch
    {
        ProcessPriorityClass.RealTime => "Realtime",
        ProcessPriorityClass.High => "High",
        ProcessPriorityClass.AboveNormal => "Above Normal",
        ProcessPriorityClass.Normal => "Normal",
        ProcessPriorityClass.BelowNormal => "Below Normal",
        ProcessPriorityClass.Idle => "Idle",
        _ => "Unknown"
    };

    /// <summary>
    /// Sets the priority of a process.
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <param name="priority">New priority class.</param>
    /// <returns>True if successful.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown for Realtime without admin.</exception>
    public bool SetPriority(int pid, ProcessPriorityClass priority)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            var oldPriority = process.PriorityClass;
            process.PriorityClass = priority;

            Debug.WriteLine($"[ProcessPriorityManager] PID {pid}: {GetPriorityName(oldPriority)} -> {GetPriorityName(priority)}");
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access denied
        {
            throw new UnauthorizedAccessException(
                $"Administrator privileges required for {GetPriorityName(priority)} priority", ex);
        }
        catch (ArgumentException)
        {
            Debug.WriteLine($"[ProcessPriorityManager] Process {pid} not found");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessPriorityManager] Failed to set priority for {pid}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets the priority of a process by name.
    /// </summary>
    public bool SetPriority(string processName, ProcessPriorityClass priority)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessesByName(processName)[0];
            return SetPriority(process.Id, priority);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the priority of a process.
    /// </summary>
    /// <param name="pid">Process ID.</param>
    /// <returns>Priority class, or null if not accessible.</returns>
    public ProcessPriorityClass? GetPriority(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return process.PriorityClass;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Raises priority (moves up one level).
    /// </summary>
    public bool RaisePriority(int pid)
    {
        var current = GetPriority(pid);
        if (current == null) return false;

        var newPriority = current.Value switch
        {
            ProcessPriorityClass.Idle => ProcessPriorityClass.BelowNormal,
            ProcessPriorityClass.BelowNormal => ProcessPriorityClass.Normal,
            ProcessPriorityClass.Normal => ProcessPriorityClass.AboveNormal,
            ProcessPriorityClass.AboveNormal => ProcessPriorityClass.High,
            ProcessPriorityClass.High => ProcessPriorityClass.RealTime,
            _ => current.Value
        };

        if (newPriority == current.Value) return false;
        return SetPriority(pid, newPriority);
    }

    /// <summary>
    /// Lowers priority (moves down one level).
    /// </summary>
    public bool LowerPriority(int pid)
    {
        var current = GetPriority(pid);
        if (current == null) return false;

        var newPriority = current.Value switch
        {
            ProcessPriorityClass.RealTime => ProcessPriorityClass.High,
            ProcessPriorityClass.High => ProcessPriorityClass.AboveNormal,
            ProcessPriorityClass.AboveNormal => ProcessPriorityClass.Normal,
            ProcessPriorityClass.Normal => ProcessPriorityClass.BelowNormal,
            ProcessPriorityClass.BelowNormal => ProcessPriorityClass.Idle,
            _ => current.Value
        };

        if (newPriority == current.Value) return false;
        return SetPriority(pid, newPriority);
    }
}
