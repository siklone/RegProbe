using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinTask = Microsoft.Win32.TaskScheduler;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for managing Windows Scheduled Tasks.
/// Allows viewing, disabling, and enabling scheduled tasks.
/// </summary>
public class ScheduledTaskService : IDisposable
{
    private WinTask.TaskService? _taskService;
    private bool _isDisposed;

    /// <summary>
    /// Get all user-level scheduled tasks (excludes system tasks).
    /// </summary>
    public async Task<IEnumerable<ScheduledTaskInfo>> GetAllTasksAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                _taskService ??= new WinTask.TaskService();
                
                return _taskService.AllTasks
                    .Where(t => !IsSystemTask(t))
                    .Select(t => CreateTaskInfo(t))
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get scheduled tasks: {ex.Message}");
                return Enumerable.Empty<ScheduledTaskInfo>();
            }
        });
    }

    /// <summary>
    /// Disable a scheduled task.
    /// </summary>
    public async Task<bool> DisableTaskAsync(string taskPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                _taskService ??= new WinTask.TaskService();
                var task = _taskService.GetTask(taskPath);
                
                if (task == null) return false;
                
                task.Enabled = false;
                Debug.WriteLine($"Task disabled: {taskPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to disable task {taskPath}: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Enable a scheduled task.
    /// </summary>
    public async Task<bool> EnableTaskAsync(string taskPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                _taskService ??= new WinTask.TaskService();
                var task = _taskService.GetTask(taskPath);
                
                if (task == null) return false;
                
                task.Enabled = true;
                Debug.WriteLine($"Task enabled: {taskPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to enable task {taskPath}: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Run a scheduled task immediately.
    /// </summary>
    public async Task<bool> RunTaskAsync(string taskPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                _taskService ??= new WinTask.TaskService();
                var task = _taskService.GetTask(taskPath);
                
                if (task == null) return false;
                
                task.Run();
                Debug.WriteLine($"Task executed: {taskPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to run task {taskPath}: {ex.Message}");
                return false;
            }
        });
    }

    private static ScheduledTaskInfo CreateTaskInfo(WinTask.Task task)
    {
        return new ScheduledTaskInfo
        {
            Name = task.Name,
            Path = task.Path,
            State = task.State.ToString(),
            IsEnabled = task.Enabled,
            LastRunTime = task.LastRunTime,
            NextRunTime = task.NextRunTime,
            Description = task.Definition.RegistrationInfo.Description ?? "",
            Author = task.Definition.RegistrationInfo.Author ?? "",
            Triggers = task.Definition.Triggers.Count,
            Actions = task.Definition.Actions.Count,
            IsHidden = task.Definition.Settings.Hidden
        };
    }

    private static bool IsSystemTask(WinTask.Task task)
    {
        var path = task.Path.ToLowerInvariant();
        var systemPaths = new[]
        {
            @"\microsoft\windows\",
            @"\microsoft\office\",
            @"\microsoft\xblgamesave\",
            @"\microsoft\edgeupdate\"
        };

        return systemPaths.Any(sp => path.Contains(sp, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _taskService?.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Information about a scheduled task.
/// </summary>
public class ScheduledTaskInfo
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string State { get; init; } = "";
    public bool IsEnabled { get; init; }
    public DateTime LastRunTime { get; init; }
    public DateTime NextRunTime { get; init; }
    public string Description { get; init; } = "";
    public string Author { get; init; } = "";
    public int Triggers { get; init; }
    public int Actions { get; init; }
    public bool IsHidden { get; init; }
    
    public string StatusText => IsEnabled ? "Enabled" : "Disabled";
    public string LastRunText => LastRunTime > DateTime.MinValue ? LastRunTime.ToString("g") : "Never";
    public string NextRunText => NextRunTime > DateTime.MinValue ? NextRunTime.ToString("g") : "N/A";
}
