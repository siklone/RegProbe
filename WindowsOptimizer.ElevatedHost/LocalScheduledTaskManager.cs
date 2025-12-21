using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Infrastructure.Tasks;

namespace WindowsOptimizer.ElevatedHost;

internal sealed class LocalScheduledTaskManager : IScheduledTaskManager
{
    public Task<ScheduledTaskInfo> QueryAsync(string taskPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(taskPath))
        {
            throw new ArgumentException("Task path is required.", nameof(taskPath));
        }

        var (folderPath, taskName) = SplitTaskPath(taskPath);
        dynamic? service = null;
        dynamic? folder = null;
        dynamic? task = null;

        try
        {
            service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")
                ?? throw new InvalidOperationException("Task Scheduler service is not available."));
            service.Connect();
            folder = service.GetFolder(folderPath);
            task = folder.GetTask(taskName);
            var enabled = (bool)task.Enabled;
            return Task.FromResult(new ScheduledTaskInfo(true, enabled));
        }
        catch (COMException ex) when (IsNotFound(ex))
        {
            return Task.FromResult(new ScheduledTaskInfo(false, false));
        }
        finally
        {
            ReleaseComObject(task);
            ReleaseComObject(folder);
            ReleaseComObject(service);
        }
    }

    public Task SetEnabledAsync(string taskPath, bool enabled, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(taskPath))
        {
            throw new ArgumentException("Task path is required.", nameof(taskPath));
        }

        var (folderPath, taskName) = SplitTaskPath(taskPath);
        dynamic? service = null;
        dynamic? folder = null;
        dynamic? task = null;

        try
        {
            service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")
                ?? throw new InvalidOperationException("Task Scheduler service is not available."));
            service.Connect();
            folder = service.GetFolder(folderPath);
            task = folder.GetTask(taskName);
            task.Enabled = enabled;
            return Task.CompletedTask;
        }
        finally
        {
            ReleaseComObject(task);
            ReleaseComObject(folder);
            ReleaseComObject(service);
        }
    }

    private static (string FolderPath, string TaskName) SplitTaskPath(string taskPath)
    {
        var normalized = taskPath.Trim();
        if (!normalized.StartsWith("\\", StringComparison.Ordinal))
        {
            normalized = "\\" + normalized;
        }

        var lastSlash = normalized.LastIndexOf('\\');
        if (lastSlash <= 0 || lastSlash == normalized.Length - 1)
        {
            throw new ArgumentException("Task path must include a task name.", nameof(taskPath));
        }

        var folderPath = normalized.Substring(0, lastSlash);
        if (string.IsNullOrEmpty(folderPath))
        {
            folderPath = "\\";
        }

        var taskName = normalized[(lastSlash + 1)..];
        return (folderPath, taskName);
    }

    private static bool IsNotFound(COMException ex)
    {
        const uint fileNotFound = 0x80070002;
        const uint pathNotFound = 0x80070003;
        var code = unchecked((uint)ex.ErrorCode);
        return code == fileNotFound || code == pathNotFound;
    }

    private static void ReleaseComObject(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
        {
            Marshal.FinalReleaseComObject(instance);
        }
    }
}
