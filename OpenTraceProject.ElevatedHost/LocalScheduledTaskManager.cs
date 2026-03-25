using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core.Tasks;

namespace OpenTraceProject.ElevatedHost;

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
            if (service == null) throw new InvalidOperationException("Task Scheduler instance is null.");
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

        return SetEnabledWithSchtasksAsync(taskPath, enabled, ct);
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

    // The COM Enabled setter is flaky on some inbox tasks; schtasks.exe is more reliable.
    private static async Task SetEnabledWithSchtasksAsync(string taskPath, bool enabled, CancellationToken ct)
    {
        var executable = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Environment.SystemDirectory
        };

        startInfo.ArgumentList.Add("/Change");
        startInfo.ArgumentList.Add("/TN");
        startInfo.ArgumentList.Add(taskPath);
        startInfo.ArgumentList.Add(enabled ? "/ENABLE" : "/DISABLE");

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start schtasks.exe.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            var error = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException($"schtasks failed ({process.ExitCode}): {error.Trim()}");
        }
    }
}
