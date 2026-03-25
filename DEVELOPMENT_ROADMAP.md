# Development Roadmap v2.1

> Archived note (2026-03-24): this roadmap contains historical design work for a telemetry-heavy surface that is no longer part of the shipped app. Keep it as background only; the current product direction is tweak workflow, hardware details, and evidence-backed validation.

**Project:** Open Trace Project
**Created:** January 19, 2026
**Status:** APPROVED - Ready for Implementation

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Single Instance & App Lifecycle](#1-single-instance--app-lifecycle)
3. [Threading Architecture](#2-threading-architecture)
4. [Splash Screen Preloading](#3-splash-screen-preloading)
5. [Hardware Database System](#4-hardware-database-system)
6. [Data Fetching with Fallbacks](#5-data-fetching-with-fallbacks)
7. [Monitor View Redesign](#6-monitor-view-redesign)
8. [UI/UX Improvements](#7-uiux-improvements)
9. [Process Management Enhancements](#8-process-management-enhancements)
10. [Implementation Order](#9-implementation-order)
11. [File Structure](#10-file-structure)
12. [Testing & Validation](#11-testing--validation)

---

## Executive Summary

This roadmap outlines major architectural improvements to the Open Trace Project application, focusing on:

- **Single Instance Enforcement**: Only one app instance can run at a time
- **Multi-threaded Architecture**: Dedicated worker threads for responsiveness on multi-core CPUs
- **Splash Screen Preloading**: Heavy operations run during splash to minimize runtime load
- **Hardware Database**: Offline hardware specs database with detailed information
- **Fallback Data Fetching**: Multiple data sources with graceful degradation
- **Monitor View Redesign**: Hardware-specific cards (CPU, GPU, RAM, Storage, Network, Motherboard)
- **Process Management**: Priority, affinity, memory trim, and process tree operations

---

## 1. Single Instance & App Lifecycle

### 1.1 Named Mutex Implementation

```csharp
// app/Services/SingleInstanceManager.cs

public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "Global\\OpenTraceProject_SingleInstance_v2";
    private const string PipeName = "OpenTraceProject_IPC_v2";

    private Mutex? _mutex;
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _pipeCts;
    private bool _isFirstInstance;

    public bool IsFirstInstance => _isFirstInstance;
    public event EventHandler<string[]>? ArgumentsReceived;

    public bool TryAcquire()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);

            if (_isFirstInstance)
            {
                StartIpcServer();
                return true;
            }
            else
            {
                // Send args to existing instance
                SendArgsToFirstInstance(Environment.GetCommandLineArgs());
                return false;
            }
        }
        catch (AbandonedMutexException)
        {
            // Previous instance crashed - we take ownership
            _isFirstInstance = true;
            StartIpcServer();
            return true;
        }
    }

    private void StartIpcServer()
    {
        _pipeCts = new CancellationTokenSource();
        Task.Run(() => IpcServerLoop(_pipeCts.Token));
    }

    private async Task IpcServerLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await _pipeServer.WaitForConnectionAsync(ct);

                using var reader = new StreamReader(_pipeServer);
                var json = await reader.ReadToEndAsync();
                var args = JsonSerializer.Deserialize<string[]>(json);

                if (args != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ArgumentsReceived?.Invoke(this, args);
                        BringToForeground();
                    });
                }

                _pipeServer.Disconnect();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Debug.WriteLine($"IPC Error: {ex.Message}");
                await Task.Delay(100, ct);
            }
        }
    }

    private void SendArgsToFirstInstance(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(3000); // 3 second timeout

            using var writer = new StreamWriter(client);
            writer.Write(JsonSerializer.Serialize(args));
            writer.Flush();
        }
        catch (TimeoutException)
        {
            // First instance not responding - may have crashed
            MessageBox.Show(
                "Another instance appears to be running but not responding.\n" +
                "Please close it manually or restart your computer.",
                "Open Trace Project",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BringToForeground()
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;

            mainWindow.Activate();
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;
            mainWindow.Focus();
        }
    }

    public void Dispose()
    {
        _pipeCts?.Cancel();
        _pipeServer?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
```

### 1.2 App.xaml.cs Integration

```csharp
public partial class App : Application
{
    private SingleInstanceManager? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new SingleInstanceManager();

        if (!_singleInstance.TryAcquire())
        {
            // Another instance is running
            Shutdown(0);
            return;
        }

        _singleInstance.ArgumentsReceived += OnArgumentsReceived;

        base.OnStartup(e);
        // Continue with normal startup...
    }

    private void OnArgumentsReceived(object? sender, string[] args)
    {
        // Handle args from second instance
        // e.g., open specific tab, run specific action
        ProcessCommandLineArgs(args);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
```

### 1.3 Edge Cases

| Scenario | Handling |
|----------|----------|
| First instance crashed (abandoned mutex) | New instance takes ownership |
| IPC pipe timeout | Show warning, suggest manual close |
| Admin vs non-admin instances | Mutex is Global, works across sessions |
| Multiple user sessions | Each session can have one instance |

---

## 2. Threading Architecture

### 2.1 Thread Pool Design

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                         UI Thread (STA)                         â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚   XAML      â”‚  â”‚  Bindings   â”‚  â”‚    Animations           â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                              â–²
                              â”‚ Dispatcher.InvokeAsync
                              â”‚
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                      MetricDataBus (Channel)                    â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”â”‚
â”‚  â”‚ Channel<MetricUpdate> - Lock-free bounded queue (1000)      â”‚â”‚
â”‚  â”‚ 60fps debounced UI dispatch                                 â”‚â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
         â–²                    â–²                    â–²
         â”‚                    â”‚                    â”‚
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”  â”Œâ”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Metric Pool    â”‚  â”‚    I/O Pool     â”‚  â”‚  Dedicated      â”‚
â”‚  (4 workers)    â”‚  â”‚   (2 workers)   â”‚  â”‚  Threads        â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤  â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤  â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ â€¢ CPU %         â”‚  â”‚ â€¢ File I/O      â”‚  â”‚ â€¢ ETW Sampler   â”‚
â”‚ â€¢ RAM %         â”‚  â”‚ â€¢ Registry      â”‚  â”‚ â€¢ Network Stats â”‚
â”‚ â€¢ GPU %         â”‚  â”‚ â€¢ SQLite        â”‚  â”‚ â€¢ Disk Health   â”‚
â”‚ â€¢ Temps         â”‚  â”‚ â€¢ Network API   â”‚  â”‚ â€¢ LHM Polling   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 2.2 MetricDataBus Implementation

```csharp
// infrastructure/Threading/MetricDataBus.cs

public sealed class MetricDataBus : IDisposable
{
    private readonly Channel<MetricUpdate> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _dispatcherTask;
    private readonly Dictionary<string, MetricUpdate> _latestValues;
    private readonly object _lock = new();
    private DateTime _lastDispatch = DateTime.MinValue;
    private const int TargetFps = 60;
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(1000.0 / TargetFps);

    public MetricDataBus()
    {
        _channel = Channel.CreateBounded<MetricUpdate>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        _latestValues = new Dictionary<string, MetricUpdate>();
        _cts = new CancellationTokenSource();
        _dispatcherTask = Task.Run(() => DispatchLoop(_cts.Token));
    }

    public void Publish(MetricUpdate update)
    {
        // Non-blocking write - drops oldest if full
        _channel.Writer.TryWrite(update);
    }

    private async Task DispatchLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Batch reads for efficiency
                while (_channel.Reader.TryRead(out var update))
                {
                    lock (_lock)
                    {
                        _latestValues[update.Key] = update;
                    }
                }

                // Throttle to target FPS
                var elapsed = DateTime.UtcNow - _lastDispatch;
                if (elapsed >= FrameInterval)
                {
                    DispatchToUi();
                    _lastDispatch = DateTime.UtcNow;
                }

                // Wait for next item or timeout
                await Task.WhenAny(
                    _channel.Reader.WaitToReadAsync(ct).AsTask(),
                    Task.Delay(FrameInterval, ct));
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private void DispatchToUi()
    {
        Dictionary<string, MetricUpdate> snapshot;
        lock (_lock)
        {
            if (_latestValues.Count == 0) return;
            snapshot = new Dictionary<string, MetricUpdate>(_latestValues);
            _latestValues.Clear();
        }

        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            MetricsUpdated?.Invoke(this, new MetricBatchEventArgs(snapshot));
        }, DispatcherPriority.DataBind);
    }

    public event EventHandler<MetricBatchEventArgs>? MetricsUpdated;

    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.Complete();
        _dispatcherTask.Wait(1000);
        _cts.Dispose();
    }
}

public record MetricUpdate(string Key, object Value, DateTime Timestamp);
public class MetricBatchEventArgs : EventArgs
{
    public IReadOnlyDictionary<string, MetricUpdate> Updates { get; }
    public MetricBatchEventArgs(Dictionary<string, MetricUpdate> updates) => Updates = updates;
}
```

### 2.3 Worker Thread Management

```csharp
// infrastructure/Threading/MetricWorkerPool.cs

public sealed class MetricWorkerPool : IDisposable
{
    private readonly MetricDataBus _bus;
    private readonly CancellationTokenSource _cts;
    private readonly List<Task> _workers;
    private readonly BlockingCollection<Func<CancellationToken, Task>> _workQueue;

    public MetricWorkerPool(MetricDataBus bus, int workerCount = 4)
    {
        _bus = bus;
        _cts = new CancellationTokenSource();
        _workQueue = new BlockingCollection<Func<CancellationToken, Task>>(
            new ConcurrentQueue<Func<CancellationToken, Task>>());

        _workers = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Factory.StartNew(
                () => WorkerLoop(_cts.Token),
                TaskCreationOptions.LongRunning))
            .ToList();
    }

    public void QueueWork(Func<CancellationToken, Task> work)
    {
        _workQueue.Add(work);
    }

    private async Task WorkerLoop(CancellationToken ct)
    {
        foreach (var work in _workQueue.GetConsumingEnumerable(ct))
        {
            try
            {
                await work(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Debug.WriteLine($"Worker error: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _workQueue.CompleteAdding();
        _cts.Cancel();
        Task.WaitAll(_workers.ToArray(), 2000);
        _cts.Dispose();
        _workQueue.Dispose();
    }
}
```

### 2.4 Thread Assignment Table

| Component | Thread | Priority | Update Rate |
|-----------|--------|----------|-------------|
| CPU Usage/Temps | Metric Pool | Normal | 1 Hz |
| RAM Usage | Metric Pool | Normal | 1 Hz |
| GPU Usage/Temps | Metric Pool | Normal | 1 Hz |
| Process List | Metric Pool | BelowNormal | 2 Hz |
| Network Stats | Dedicated | Normal | 1 Hz |
| Disk I/O | Dedicated | Normal | 1 Hz |
| ETW Sampling | Dedicated | AboveNormal | Continuous |
| Hardware DB | I/O Pool | BelowNormal | On-demand |
| Registry Ops | I/O Pool | Normal | On-demand |
| File I/O | I/O Pool | Normal | On-demand |

### 2.5 Threading Diagnostics

```csharp
public class ThreadingDiagnostics
{
    private readonly ConcurrentDictionary<string, WorkerStats> _stats = new();

    public void RecordWork(string workerName, TimeSpan duration, bool success)
    {
        _stats.AddOrUpdate(
            workerName,
            _ => new WorkerStats { TotalTasks = 1, TotalTime = duration, Failures = success ? 0 : 1 },
            (_, existing) =>
            {
                existing.TotalTasks++;
                existing.TotalTime += duration;
                if (!success) existing.Failures++;
                return existing;
            });
    }

    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Threading Diagnostics ===");
        foreach (var (name, stats) in _stats)
        {
            var avgMs = stats.TotalTime.TotalMilliseconds / stats.TotalTasks;
            var failRate = (double)stats.Failures / stats.TotalTasks * 100;
            sb.AppendLine($"{name}: {stats.TotalTasks} tasks, avg {avgMs:F2}ms, {failRate:F1}% failures");
        }
        return sb.ToString();
    }
}
```

---

## 3. Splash Screen Preloading

### 3.1 PreloadManager

```csharp
// app/Services/PreloadManager.cs

public sealed class PreloadManager
{
    private readonly ConcurrentDictionary<string, PreloadTask> _tasks = new();
    private readonly SemaphoreSlim _throttle;
    private readonly IProgress<PreloadProgress> _progress;

    public PreloadManager(IProgress<PreloadProgress> progress, int maxConcurrency = 4)
    {
        _progress = progress;
        _throttle = new SemaphoreSlim(maxConcurrency);
    }

    public void RegisterTask(string name, Func<CancellationToken, Task<object?>> task,
        bool isCritical = false, int priority = 0)
    {
        _tasks[name] = new PreloadTask
        {
            Name = name,
            Task = task,
            IsCritical = isCritical,
            Priority = priority
        };
    }

    public async Task<PreloadResult> RunAllAsync(CancellationToken ct)
    {
        var result = new PreloadResult();
        var orderedTasks = _tasks.Values
            .OrderByDescending(t => t.IsCritical)
            .ThenByDescending(t => t.Priority)
            .ToList();

        var total = orderedTasks.Count;
        var completed = 0;

        // Run critical tasks first (sequentially)
        var critical = orderedTasks.Where(t => t.IsCritical).ToList();
        foreach (var task in critical)
        {
            try
            {
                _progress.Report(new PreloadProgress(
                    ++completed, total, task.Name, PreloadState.Running));

                var value = await task.Task(ct);
                result.Results[task.Name] = value;

                _progress.Report(new PreloadProgress(
                    completed, total, task.Name, PreloadState.Completed));
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                result.Errors[task.Name] = ex;
                _progress.Report(new PreloadProgress(
                    completed, total, task.Name, PreloadState.Failed, ex.Message));

                if (task.IsCritical)
                    throw new PreloadException($"Critical task '{task.Name}' failed", ex);
            }
        }

        // Run non-critical tasks in parallel
        var nonCritical = orderedTasks.Where(t => !t.IsCritical).ToList();
        var parallelTasks = nonCritical.Select(async task =>
        {
            await _throttle.WaitAsync(ct);
            try
            {
                var current = Interlocked.Increment(ref completed);
                _progress.Report(new PreloadProgress(
                    current, total, task.Name, PreloadState.Running));

                var value = await task.Task(ct);
                result.Results[task.Name] = value;

                _progress.Report(new PreloadProgress(
                    current, total, task.Name, PreloadState.Completed));
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                result.Errors[task.Name] = ex;
                var current = Interlocked.Increment(ref completed);
                _progress.Report(new PreloadProgress(
                    current, total, task.Name, PreloadState.Failed, ex.Message));
            }
            finally
            {
                _throttle.Release();
            }
        });

        await Task.WhenAll(parallelTasks);
        return result;
    }
}

public record PreloadProgress(int Completed, int Total, string CurrentTask,
    PreloadState State, string? Message = null);

public enum PreloadState { Pending, Running, Completed, Failed }

public class PreloadResult
{
    public ConcurrentDictionary<string, object?> Results { get; } = new();
    public ConcurrentDictionary<string, Exception> Errors { get; } = new();
    public bool HasErrors => !Errors.IsEmpty;
}
```

### 3.2 Splash Integration

```csharp
// app/StartupWindow.xaml.cs

public partial class StartupWindow : Window
{
    private readonly PreloadManager _preloadManager;
    private CancellationTokenSource? _cts;

    public StartupWindow()
    {
        InitializeComponent();

        var progress = new Progress<PreloadProgress>(UpdateProgress);
        _preloadManager = new PreloadManager(progress);

        RegisterPreloadTasks();
    }

    private void RegisterPreloadTasks()
    {
        // Critical tasks (must complete before app starts)
        _preloadManager.RegisterTask("Settings", async ct =>
        {
            return await Task.Run(() => AppSettings.Load(), ct);
        }, isCritical: true, priority: 100);

        _preloadManager.RegisterTask("Theme", async ct =>
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var theme = AppSettings.Instance.Theme;
                ThemeManager.ApplyTheme(theme);
            });
            return null;
        }, isCritical: true, priority: 99);

        _preloadManager.RegisterTask("HardwareDB", async ct =>
        {
            return await HardwareDatabase.InitializeAsync(ct);
        }, isCritical: true, priority: 98);

        // Non-critical tasks (can fail gracefully)
        _preloadManager.RegisterTask("HardwareScan", async ct =>
        {
            var service = new HardwareSensorService();
            return await service.GetSnapshotAsync();
        }, isCritical: false, priority: 80);

        _preloadManager.RegisterTask("TweakCatalog", async ct =>
        {
            return await TweakCatalogLoader.LoadAsync(ct);
        }, isCritical: false, priority: 70);

        _preloadManager.RegisterTask("NetworkInterfaces", async ct =>
        {
            return await Task.Run(() => NetworkInterface.GetAllNetworkInterfaces(), ct);
        }, isCritical: false, priority: 60);

        _preloadManager.RegisterTask("DiskInfo", async ct =>
        {
            return await Task.Run(() => DriveInfo.GetDrives()
                .Where(d => d.IsReady).ToList(), ct);
        }, isCritical: false, priority: 60);

        _preloadManager.RegisterTask("HardwareDBUpdate", async ct =>
        {
            return await HardwareDatabase.CheckForUpdatesAsync(ct);
        }, isCritical: false, priority: 20);
    }

    private void UpdateProgress(PreloadProgress progress)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = (double)progress.Completed / progress.Total * 100;
            StatusText.Text = progress.State switch
            {
                PreloadState.Running => $"Loading {progress.CurrentTask}...",
                PreloadState.Completed => $"Loaded {progress.CurrentTask}",
                PreloadState.Failed => $"Failed: {progress.CurrentTask} - {progress.Message}",
                _ => "Initializing..."
            };

            CountText.Text = $"{progress.Completed}/{progress.Total}";
        });
    }

    public async Task<PreloadResult> RunPreloadAsync()
    {
        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Total timeout

        try
        {
            return await _preloadManager.RunAllAsync(_cts.Token);
        }
        catch (PreloadException ex)
        {
            MessageBox.Show(
                $"Failed to start: {ex.Message}\n\nThe application will now close.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Application.Current.Shutdown(1);
            throw;
        }
    }
}
```

### 3.3 Error Recovery

```csharp
public class PreloadErrorRecovery
{
    public static async Task<T?> WithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct,
        int maxRetries = 3,
        TimeSpan? initialDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromMilliseconds(100);

        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                return await operation(ct);
            }
            catch (Exception ex) when (i < maxRetries && !ct.IsCancellationRequested)
            {
                Debug.WriteLine($"Preload attempt {i + 1} failed: {ex.Message}");
                await Task.Delay(delay, ct);
                delay *= 2; // Exponential backoff
            }
        }

        return default;
    }
}
```

---

## 4. Hardware Database System

### 4.1 SQLite Schema

```sql
-- Hardware Specs Database
-- Location: %APPDATA%/OpenTraceProject/hardware.db

CREATE TABLE IF NOT EXISTS cpu_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cpuid TEXT UNIQUE NOT NULL,          -- CPUID string (e.g., "GenuineIntel 0x000906EA")
    brand TEXT NOT NULL,                  -- "Intel Core i7-9750H"
    codename TEXT,                        -- "Coffee Lake"
    cores INTEGER,
    threads INTEGER,
    base_clock_mhz INTEGER,
    boost_clock_mhz INTEGER,
    tdp_watts INTEGER,
    cache_l2_kb INTEGER,
    cache_l3_kb INTEGER,
    lithography_nm INTEGER,
    release_date TEXT,                    -- "Q2 2019"
    socket TEXT,                          -- "FCBGA1440"
    architecture TEXT,                    -- "x86-64"
    features TEXT,                        -- JSON: ["SSE4.2", "AVX2", "AES-NI"]
    max_memory_gb INTEGER,
    memory_channels INTEGER,
    pcie_lanes INTEGER,
    integrated_gpu TEXT,                  -- "Intel UHD Graphics 630"
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS gpu_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    device_id TEXT UNIQUE NOT NULL,       -- PCI Vendor:Device ID "10DE:1F08"
    brand TEXT NOT NULL,                  -- "NVIDIA GeForce RTX 2060"
    codename TEXT,                        -- "Turing TU106"
    cuda_cores INTEGER,                   -- NVIDIA specific
    stream_processors INTEGER,            -- AMD specific
    base_clock_mhz INTEGER,
    boost_clock_mhz INTEGER,
    memory_size_mb INTEGER,
    memory_type TEXT,                     -- "GDDR6"
    memory_bus_bits INTEGER,              -- 192
    memory_bandwidth_gbps REAL,
    tdp_watts INTEGER,
    release_date TEXT,
    architecture TEXT,                    -- "Turing", "Ampere", "RDNA2"
    directx_version TEXT,                 -- "12.1"
    opengl_version TEXT,
    vulkan_version TEXT,
    features TEXT,                        -- JSON: ["Ray Tracing", "DLSS", "NVENC"]
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS motherboard_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    board_id TEXT UNIQUE NOT NULL,        -- "ASUSTeK COMPUTER INC. ROG STRIX Z390-F"
    manufacturer TEXT NOT NULL,
    model TEXT NOT NULL,
    chipset TEXT,                         -- "Intel Z390"
    socket TEXT,                          -- "LGA1151"
    form_factor TEXT,                     -- "ATX"
    memory_slots INTEGER,
    max_memory_gb INTEGER,
    memory_type TEXT,                     -- "DDR4"
    max_memory_speed_mhz INTEGER,
    pcie_slots TEXT,                      -- JSON: {"x16": 2, "x1": 3}
    sata_ports INTEGER,
    m2_slots INTEGER,
    usb_ports TEXT,                       -- JSON: {"USB3.2": 6, "USB2.0": 4}
    audio_codec TEXT,
    network_chip TEXT,
    wifi_chip TEXT,
    bios_type TEXT,                       -- "UEFI"
    release_date TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS storage_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    model_id TEXT UNIQUE NOT NULL,        -- "Samsung SSD 970 EVO Plus 1TB"
    manufacturer TEXT NOT NULL,
    model TEXT NOT NULL,
    type TEXT NOT NULL,                   -- "NVMe SSD", "SATA SSD", "HDD"
    capacity_gb INTEGER,
    interface TEXT,                       -- "PCIe 3.0 x4", "SATA III"
    form_factor TEXT,                     -- "M.2 2280", "2.5\""
    seq_read_mbps INTEGER,
    seq_write_mbps INTEGER,
    random_read_iops INTEGER,
    random_write_iops INTEGER,
    nand_type TEXT,                       -- "V-NAND TLC"
    controller TEXT,                      -- "Samsung Phoenix"
    dram_cache_mb INTEGER,
    tbw_tb INTEGER,                       -- Terabytes Written endurance
    release_date TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ram_specs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    part_number TEXT UNIQUE NOT NULL,     -- "CMK32GX4M2B3200C16"
    manufacturer TEXT NOT NULL,
    model TEXT NOT NULL,                  -- "Corsair Vengeance LPX"
    type TEXT NOT NULL,                   -- "DDR4"
    speed_mhz INTEGER,
    capacity_gb INTEGER,
    modules INTEGER,                      -- 2 for dual-kit
    cas_latency INTEGER,
    timings TEXT,                         -- "16-18-18-36"
    voltage REAL,                         -- 1.35
    ecc INTEGER DEFAULT 0,                -- 0/1
    xmp_profiles TEXT,                    -- JSON
    release_date TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Index for fast lookups
CREATE INDEX IF NOT EXISTS idx_cpu_cpuid ON cpu_specs(cpuid);
CREATE INDEX IF NOT EXISTS idx_gpu_device ON gpu_specs(device_id);
CREATE INDEX IF NOT EXISTS idx_mb_model ON motherboard_specs(board_id);
CREATE INDEX IF NOT EXISTS idx_storage_model ON storage_specs(model_id);
CREATE INDEX IF NOT EXISTS idx_ram_part ON ram_specs(part_number);

-- Metadata table for DB versioning
CREATE TABLE IF NOT EXISTS db_metadata (
    key TEXT PRIMARY KEY,
    value TEXT
);
INSERT OR REPLACE INTO db_metadata (key, value) VALUES ('version', '1.0.0');
INSERT OR REPLACE INTO db_metadata (key, value) VALUES ('last_update', datetime('now'));
```

### 4.2 Hardware Identifier Service

```csharp
// infrastructure/Hardware/HardwareIdentifier.cs

public class HardwareIdentifier
{
    // CPU Identification
    public static CpuIdentity GetCpuId()
    {
        var identity = new CpuIdentity();

        // 1. CPUID instruction (most reliable)
        try
        {
            var cpuInfo = new int[4];
            NativeMethods.Cpuid(cpuInfo, 0);

            // Vendor string from EBX, EDX, ECX
            var vendor = Encoding.ASCII.GetString(new[]
            {
                (byte)cpuInfo[1], (byte)(cpuInfo[1] >> 8), (byte)(cpuInfo[1] >> 16), (byte)(cpuInfo[1] >> 24),
                (byte)cpuInfo[3], (byte)(cpuInfo[3] >> 8), (byte)(cpuInfo[3] >> 16), (byte)(cpuInfo[3] >> 24),
                (byte)cpuInfo[2], (byte)(cpuInfo[2] >> 8), (byte)(cpuInfo[2] >> 16), (byte)(cpuInfo[2] >> 24)
            });
            identity.Vendor = vendor.Trim('\0');

            // Family/Model/Stepping from leaf 1
            NativeMethods.Cpuid(cpuInfo, 1);
            identity.Signature = $"0x{cpuInfo[0]:X8}";
            identity.Family = ((cpuInfo[0] >> 8) & 0xF) + ((cpuInfo[0] >> 20) & 0xFF);
            identity.Model = ((cpuInfo[0] >> 4) & 0xF) | ((cpuInfo[0] >> 12) & 0xF0);
            identity.Stepping = cpuInfo[0] & 0xF;

            // Brand string from leaves 0x80000002-4
            var brand = new StringBuilder();
            for (uint leaf = 0x80000002; leaf <= 0x80000004; leaf++)
            {
                NativeMethods.Cpuid(cpuInfo, (int)leaf);
                brand.Append(Encoding.ASCII.GetString(BitConverter.GetBytes(cpuInfo[0])));
                brand.Append(Encoding.ASCII.GetString(BitConverter.GetBytes(cpuInfo[1])));
                brand.Append(Encoding.ASCII.GetString(BitConverter.GetBytes(cpuInfo[2])));
                brand.Append(Encoding.ASCII.GetString(BitConverter.GetBytes(cpuInfo[3])));
            }
            identity.BrandString = brand.ToString().Trim('\0').Trim();
        }
        catch { /* CPUID not available */ }

        // 2. WMI fallback
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                identity.WmiName = obj["Name"]?.ToString()?.Trim();
                identity.ProcessorId = obj["ProcessorId"]?.ToString();
                identity.Cores = Convert.ToInt32(obj["NumberOfCores"]);
                identity.Threads = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                break;
            }
        }
        catch { /* WMI not available */ }

        // Build lookup key
        identity.LookupKey = $"{identity.Vendor} {identity.Signature}";

        return identity;
    }

    // GPU Identification
    public static GpuIdentity GetGpuId()
    {
        var identity = new GpuIdentity();

        // 1. Registry (most reliable for device ID)
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
            if (key != null)
            {
                var deviceDesc = key.GetValue("DriverDesc")?.ToString();
                var matchingId = key.GetValue("MatchingDeviceId")?.ToString();

                // Parse VEN_XXXX&DEV_XXXX
                var match = Regex.Match(matchingId ?? "", @"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})",
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    identity.VendorId = match.Groups[1].Value;
                    identity.DeviceId = match.Groups[2].Value;
                    identity.PciId = $"{identity.VendorId}:{identity.DeviceId}";
                }

                identity.DriverDesc = deviceDesc;
            }
        }
        catch { /* Registry access failed */ }

        // 2. WMI fallback
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                identity.WmiName = obj["Name"]?.ToString();
                identity.AdapterRam = Convert.ToInt64(obj["AdapterRAM"]);
                identity.DriverVersion = obj["DriverVersion"]?.ToString();
                identity.PnpDeviceId = obj["PNPDeviceID"]?.ToString();
                break;
            }
        }
        catch { /* WMI not available */ }

        identity.LookupKey = identity.PciId ?? identity.WmiName ?? "Unknown";

        return identity;
    }
}

public class CpuIdentity
{
    public string Vendor { get; set; } = "";
    public string Signature { get; set; } = "";
    public int Family { get; set; }
    public int Model { get; set; }
    public int Stepping { get; set; }
    public string BrandString { get; set; } = "";
    public string? WmiName { get; set; }
    public string? ProcessorId { get; set; }
    public int Cores { get; set; }
    public int Threads { get; set; }
    public string LookupKey { get; set; } = "";
}

public class GpuIdentity
{
    public string? VendorId { get; set; }
    public string? DeviceId { get; set; }
    public string? PciId { get; set; }
    public string? DriverDesc { get; set; }
    public string? WmiName { get; set; }
    public long AdapterRam { get; set; }
    public string? DriverVersion { get; set; }
    public string? PnpDeviceId { get; set; }
    public string LookupKey { get; set; } = "";
}
```

### 4.3 Matching Algorithm

```csharp
// infrastructure/Hardware/HardwareDatabase.cs

public class HardwareDatabase
{
    private readonly SqliteConnection _connection;
    private static HardwareDatabase? _instance;

    public static HardwareDatabase Instance => _instance
        ?? throw new InvalidOperationException("Database not initialized");

    public static async Task<HardwareDatabase> InitializeAsync(CancellationToken ct)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenTraceProject", "hardware.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        _instance = new HardwareDatabase(dbPath);
        await _instance.EnsureSchemaAsync(ct);

        return _instance;
    }

    public async Task<CpuSpecs?> LookupCpuAsync(CpuIdentity identity, CancellationToken ct)
    {
        // Strategy 1: Exact CPUID match
        var specs = await QueryCpuByIdAsync(identity.LookupKey, ct);
        if (specs != null) return specs;

        // Strategy 2: Brand string fuzzy match
        if (!string.IsNullOrEmpty(identity.BrandString))
        {
            specs = await FuzzyMatchCpuAsync(identity.BrandString, ct);
            if (specs != null) return specs;
        }

        // Strategy 3: WMI name fuzzy match
        if (!string.IsNullOrEmpty(identity.WmiName))
        {
            specs = await FuzzyMatchCpuAsync(identity.WmiName, ct);
            if (specs != null) return specs;
        }

        // Strategy 4: Return generic based on family
        return CreateGenericCpuSpecs(identity);
    }

    private async Task<CpuSpecs?> FuzzyMatchCpuAsync(string searchTerm, CancellationToken ct)
    {
        // Extract model number (e.g., "i7-9750H", "Ryzen 5 5600X")
        var modelPatterns = new[]
        {
            @"i[3579]-\d{4,5}[A-Z]*",     // Intel Core
            @"Ryzen \d \d{4}[A-Z]*",       // AMD Ryzen
            @"Xeon [A-Z]-\d{4}[A-Z]*",     // Intel Xeon
            @"Pentium [A-Z]\d{4}",         // Intel Pentium
        };

        foreach (var pattern in modelPatterns)
        {
            var match = Regex.Match(searchTerm, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var modelNum = match.Value;
                var sql = "SELECT * FROM cpu_specs WHERE brand LIKE @pattern LIMIT 1";
                var result = await _connection.QueryFirstOrDefaultAsync<CpuSpecs>(
                    sql, new { pattern = $"%{modelNum}%" });
                if (result != null) return result;
            }
        }

        // Fallback: LIKE search on brand
        var likeSql = "SELECT * FROM cpu_specs WHERE brand LIKE @pattern ORDER BY id DESC LIMIT 1";
        var words = searchTerm.Split(' ').Where(w => w.Length > 2).Take(3);
        foreach (var word in words)
        {
            var result = await _connection.QueryFirstOrDefaultAsync<CpuSpecs>(
                likeSql, new { pattern = $"%{word}%" });
            if (result != null) return result;
        }

        return null;
    }

    private CpuSpecs CreateGenericCpuSpecs(CpuIdentity identity)
    {
        var isIntel = identity.Vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase);
        var isAmd = identity.Vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase);

        return new CpuSpecs
        {
            Brand = identity.BrandString.Length > 0 ? identity.BrandString : identity.WmiName ?? "Unknown CPU",
            Cores = identity.Cores > 0 ? identity.Cores : Environment.ProcessorCount / 2,
            Threads = identity.Threads > 0 ? identity.Threads : Environment.ProcessorCount,
            Architecture = Environment.Is64BitOperatingSystem ? "x86-64" : "x86",
            Vendor = isIntel ? "Intel" : isAmd ? "AMD" : "Unknown",
            IsFromDatabase = false
        };
    }
}
```

---

## 5. Data Fetching with Fallbacks

### 5.1 Fallback Chain Architecture

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                      Data Request                                â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                              â”‚
                              â–¼
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Source 1: Legacy hardware telemetry provider (historical)     â”‚
â”‚  â”œâ”€ CPU: Temps, Voltage, Power, Clocks                          â”‚
â”‚  â”œâ”€ GPU: Temps, Voltage, Power, Clocks, Memory                  â”‚
â”‚  â”œâ”€ Storage: Temps, SMART data                                  â”‚
â”‚  â””â”€ Motherboard: Voltages, Temps                                â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                              â”‚ Timeout/Error
                              â–¼
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Source 2: WMI (Secondary)                                      â”‚
â”‚  â”œâ”€ Win32_Processor: Name, Cores, Threads, Speed                â”‚
â”‚  â”œâ”€ Win32_VideoController: GPU Name, Memory, Driver             â”‚
â”‚  â”œâ”€ Win32_DiskDrive: Model, Size, Interface                     â”‚
â”‚  â”œâ”€ MSAcpi_ThermalZoneTemperature: CPU/System temps             â”‚
â”‚  â””â”€ MSFT_PhysicalDisk: NVMe health                              â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                              â”‚ Timeout/Error
                              â–¼
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Source 3: Performance Counters                                 â”‚
â”‚  â”œâ”€ Processor: CPU usage, frequency                             â”‚
â”‚  â”œâ”€ Memory: Available, committed, cache                         â”‚
â”‚  â”œâ”€ LogicalDisk: Read/Write bytes per sec                       â”‚
â”‚  â””â”€ Network Interface: Bytes sent/received                      â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                              â”‚ Timeout/Error
                              â–¼
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Source 4: System APIs (Fallback)                               â”‚
â”‚  â”œâ”€ Environment: ProcessorCount, OSVersion                      â”‚
â”‚  â”œâ”€ DriveInfo: Available space, total size                      â”‚
â”‚  â”œâ”€ NetworkInterface: Basic stats                               â”‚
â”‚  â””â”€ Process: Memory working set                                 â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                              â”‚ All Failed
                              â–¼
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Default/Unknown Values                                         â”‚
â”‚  â””â”€ Return N/A, 0, or reasonable defaults                       â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.2 FallbackDataProvider

```csharp
// infrastructure/Data/FallbackDataProvider.cs

public class FallbackDataProvider<T>
{
    private readonly List<DataSource<T>> _sources = new();
    private readonly ILogger _logger;
    private readonly RetryPolicy _retryPolicy;

    public FallbackDataProvider(ILogger logger, RetryPolicy? policy = null)
    {
        _logger = logger;
        _retryPolicy = policy ?? RetryPolicy.Default;
    }

    public FallbackDataProvider<T> AddSource(string name,
        Func<CancellationToken, Task<T?>> getter,
        int priority = 0,
        TimeSpan? timeout = null)
    {
        _sources.Add(new DataSource<T>
        {
            Name = name,
            Getter = getter,
            Priority = priority,
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        });

        _sources.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return this;
    }

    public async Task<DataResult<T>> GetAsync(CancellationToken ct)
    {
        var errors = new List<DataSourceError>();

        foreach (var source in _sources)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(source.Timeout);

                var result = await ExecuteWithRetryAsync(
                    source.Getter,
                    timeoutCts.Token,
                    _retryPolicy);

                if (result != null)
                {
                    _logger.LogDebug("Data fetched from {Source}", source.Name);
                    return new DataResult<T>
                    {
                        Value = result,
                        Source = source.Name,
                        Success = true
                    };
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                errors.Add(new DataSourceError(source.Name, "Timeout"));
                _logger.LogWarning("Source {Source} timed out", source.Name);
            }
            catch (Exception ex)
            {
                errors.Add(new DataSourceError(source.Name, ex.Message));
                _logger.LogWarning(ex, "Source {Source} failed", source.Name);
            }
        }

        return new DataResult<T>
        {
            Value = default,
            Success = false,
            Errors = errors
        };
    }

    private async Task<T?> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<T?>> operation,
        CancellationToken ct,
        RetryPolicy policy)
    {
        var delay = policy.InitialDelay;

        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                return await operation(ct);
            }
            catch when (attempt < policy.MaxRetries && !ct.IsCancellationRequested)
            {
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(
                    Math.Min(delay.TotalMilliseconds * policy.BackoffMultiplier,
                             policy.MaxDelay.TotalMilliseconds));
            }
        }

        return default;
    }
}

public class RetryPolicy
{
    public int MaxRetries { get; init; } = 2;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    public double BackoffMultiplier { get; init; } = 2.0;
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(2);

    public static RetryPolicy Default => new();
    public static RetryPolicy NoRetry => new() { MaxRetries = 0 };
    public static RetryPolicy Aggressive => new()
    {
        MaxRetries = 3,
        InitialDelay = TimeSpan.FromMilliseconds(50),
        MaxDelay = TimeSpan.FromSeconds(1)
    };
}

public class DataResult<T>
{
    public T? Value { get; init; }
    public string Source { get; init; } = "";
    public bool Success { get; init; }
    public List<DataSourceError> Errors { get; init; } = new();
}

public record DataSourceError(string Source, string Message);
```

### 5.3 Usage Example

```csharp
// infrastructure/Metrics/CpuDataProvider.cs

public class CpuDataProvider
{
    private readonly FallbackDataProvider<CpuMetrics> _provider;

    public CpuDataProvider(ILogger<CpuDataProvider> logger)
    {
        _provider = new FallbackDataProvider<CpuMetrics>(logger)
            .AddSource("LegacyTelemetryProvider", GetFromLhm, priority: 100, timeout: TimeSpan.FromSeconds(3))
            .AddSource("WMI_PerfCounter", GetFromWmiPerf, priority: 80, timeout: TimeSpan.FromSeconds(2))
            .AddSource("WMI_Win32", GetFromWmi, priority: 60, timeout: TimeSpan.FromSeconds(2))
            .AddSource("PerformanceCounter", GetFromPerfCounter, priority: 40, timeout: TimeSpan.FromSeconds(1))
            .AddSource("Environment", GetFromEnvironment, priority: 20, timeout: TimeSpan.FromMilliseconds(100));
    }

    private async Task<CpuMetrics?> GetFromLhm(CancellationToken ct)
    {
        // Historical hardware telemetry implementation
        var computer = new Computer { IsCpuEnabled = true };
        computer.Open();
        // ... extract metrics
        return metrics;
    }

    private async Task<CpuMetrics?> GetFromWmiPerf(CancellationToken ct)
    {
        // WMI Performance counter query
        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_PerfFormattedData_Counters_ProcessorInformation WHERE Name='_Total'");
        // ...
        return metrics;
    }

    // ... other implementations
}
```

---

## 6. Monitor View Redesign

### 6.1 Hardware Card Component

```xml
<!-- app/Views/Components/HardwareCardView.xaml -->

<UserControl x:Class="OpenTraceProject.App.Views.Components.HardwareCardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Border Background="{DynamicResource CardBackgroundBrush}"
            BorderBrush="{DynamicResource GlassBorderBrush}"
            BorderThickness="1"
            CornerRadius="16"
            Padding="20"
            Margin="8"
            x:Name="CardBorder">

        <Border.Effect>
            <DropShadowEffect BlurRadius="20"
                              ShadowDepth="4"
                              Opacity="0.2"
                              Color="{DynamicResource ShadowColor}"/>
        </Border.Effect>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- Header -->
                <RowDefinition Height="Auto"/>  <!-- Primary Metric -->
                <RowDefinition Height="*"/>     <!-- Chart/Details -->
                <RowDefinition Height="Auto"/>  <!-- Secondary Metrics -->
                <RowDefinition Height="Auto"/>  <!-- Footer/Specs Link -->
            </Grid.RowDefinitions>

            <!-- Header: Icon + Name + Status LED -->
            <DockPanel Grid.Row="0" Margin="0,0,0,16">
                <Border DockPanel.Dock="Left"
                        Width="48" Height="48"
                        CornerRadius="12"
                        Background="{Binding IconBackground}">
                    <TextBlock Text="{Binding Icon}"
                               FontSize="24"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center"/>
                </Border>

                <StackPanel Margin="16,0,0,0" VerticalAlignment="Center">
                    <TextBlock Text="{Binding Title}"
                               FontSize="18"
                               FontWeight="SemiBold"
                               Foreground="{DynamicResource ForegroundBrightestBrush}"/>
                    <TextBlock Text="{Binding Subtitle}"
                               FontSize="12"
                               Foreground="{DynamicResource MutedForegroundBrush}"
                               TextTrimming="CharacterEllipsis"/>
                </StackPanel>

                <!-- Status LED -->
                <Ellipse DockPanel.Dock="Right"
                         Width="12" Height="12"
                         HorizontalAlignment="Right"
                         Fill="{Binding StatusColor}">
                    <Ellipse.Effect>
                        <BlurEffect Radius="4"/>
                    </Ellipse.Effect>
                </Ellipse>
            </DockPanel>

            <!-- Primary Metric: Large Number -->
            <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,16">
                <TextBlock Text="{Binding PrimaryValue}"
                           FontSize="48"
                           FontWeight="Bold"
                           Foreground="{Binding PrimaryValueColor}"/>
                <TextBlock Text="{Binding PrimaryUnit}"
                           FontSize="20"
                           Foreground="{DynamicResource MutedForegroundBrush}"
                           VerticalAlignment="Bottom"
                           Margin="4,0,0,10"/>
            </StackPanel>

            <!-- Chart Area -->
            <ContentPresenter Grid.Row="2" Content="{Binding ChartContent}"/>

            <!-- Secondary Metrics Grid -->
            <ItemsControl Grid.Row="3"
                          ItemsSource="{Binding SecondaryMetrics}"
                          Margin="0,16,0,0">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <UniformGrid Columns="3"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Margin="0,0,16,0">
                            <TextBlock Text="{Binding Label}"
                                       FontSize="11"
                                       Foreground="{DynamicResource MutedForegroundBrush}"/>
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding Value}"
                                           FontSize="16"
                                           FontWeight="Medium"
                                           Foreground="{DynamicResource ForegroundBrightestBrush}"/>
                                <TextBlock Text="{Binding Unit}"
                                           FontSize="11"
                                           Foreground="{DynamicResource MutedForegroundBrush}"
                                           VerticalAlignment="Bottom"
                                           Margin="2,0,0,2"/>
                            </StackPanel>
                        </StackPanel>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>

            <!-- Footer: Specs Link -->
            <Button Grid.Row="4"
                    Content="View Detailed Specs"
                    Command="{Binding ShowSpecsCommand}"
                    Style="{StaticResource LinkButtonStyle}"
                    HorizontalAlignment="Left"
                    Margin="0,16,0,0"
                    Visibility="{Binding HasSpecs, Converter={StaticResource BoolToVisibility}}"/>
        </Grid>
    </Border>

    <!-- Hover Animation -->
    <UserControl.Triggers>
        <EventTrigger RoutedEvent="MouseEnter">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Storyboard.TargetName="CardBorder"
                                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                                     To="1.02" Duration="0:0:0.15"/>
                    <DoubleAnimation Storyboard.TargetName="CardBorder"
                                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                                     To="1.02" Duration="0:0:0.15"/>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
        <EventTrigger RoutedEvent="MouseLeave">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Storyboard.TargetName="CardBorder"
                                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                                     To="1.0" Duration="0:0:0.15"/>
                    <DoubleAnimation Storyboard.TargetName="CardBorder"
                                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                                     To="1.0" Duration="0:0:0.15"/>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </UserControl.Triggers>
</UserControl>
```

### 6.2 CPU Card ViewModel

```csharp
// app/ViewModels/Hardware/CpuCardViewModel.cs

public class CpuCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus _bus;
    private readonly HardwareDatabase _db;
    private CpuSpecs? _specs;

    public CpuCardViewModel(MetricDataBus bus, HardwareDatabase db)
    {
        _bus = bus;
        _db = db;

        Icon = "ðŸ”²";
        Title = "CPU";
        IconBackground = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue

        _bus.MetricsUpdated += OnMetricsUpdated;

        Task.Run(LoadSpecsAsync);
    }

    private async Task LoadSpecsAsync()
    {
        var identity = HardwareIdentifier.GetCpuId();
        _specs = await _db.LookupCpuAsync(identity, CancellationToken.None);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Subtitle = _specs?.Brand ?? identity.BrandString;
            HasSpecs = _specs?.IsFromDatabase == true;

            if (_specs != null)
            {
                SecondaryMetrics.Add(new MetricItem("Cores", _specs.Cores.ToString(), ""));
                SecondaryMetrics.Add(new MetricItem("Threads", _specs.Threads.ToString(), ""));
                SecondaryMetrics.Add(new MetricItem("Base", _specs.BaseClockMhz.ToString(), "MHz"));
            }
        });
    }

    private void OnMetricsUpdated(object? sender, MetricBatchEventArgs e)
    {
        if (e.Updates.TryGetValue("cpu.usage", out var usage))
        {
            PrimaryValue = $"{usage.Value:F0}";
            PrimaryUnit = "%";
            PrimaryValueColor = GetUsageColor((float)usage.Value);
        }

        if (e.Updates.TryGetValue("cpu.temp.package", out var temp))
        {
            UpdateSecondaryMetric("Temp", $"{temp.Value:F0}", "Â°C");
            StatusColor = GetTempStatusColor((float)temp.Value);
        }

        if (e.Updates.TryGetValue("cpu.power", out var power))
        {
            UpdateSecondaryMetric("Power", $"{power.Value:F0}", "W");
        }

        if (e.Updates.TryGetValue("cpu.clock.avg", out var clock))
        {
            UpdateSecondaryMetric("Clock", $"{clock.Value:F0}", "MHz");
        }
    }

    private Brush GetUsageColor(float usage) => usage switch
    {
        >= 90 => Brushes.Red,
        >= 70 => Brushes.Orange,
        >= 50 => Brushes.Yellow,
        _ => (Brush)Application.Current.Resources["AccentBrightCyanBrush"]
    };

    private Brush GetTempStatusColor(float temp) => temp switch
    {
        >= 90 => Brushes.Red,
        >= 80 => Brushes.Orange,
        >= 70 => Brushes.Yellow,
        _ => Brushes.LimeGreen
    };

    [RelayCommand]
    private void ShowSpecs()
    {
        if (_specs == null) return;

        var dialog = new HardwareSpecsDialog(_specs);
        dialog.ShowDialog();
    }

    public override void Dispose()
    {
        _bus.MetricsUpdated -= OnMetricsUpdated;
        base.Dispose();
    }
}
```

### 6.3 GPU Card ViewModel

```csharp
// app/ViewModels/Hardware/GpuCardViewModel.cs

public class GpuCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus _bus;
    private readonly HardwareDatabase _db;
    private GpuSpecs? _specs;

    public GpuCardViewModel(MetricDataBus bus, HardwareDatabase db)
    {
        _bus = bus;
        _db = db;

        Icon = "ðŸŽ®";
        Title = "GPU";
        IconBackground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green

        _bus.MetricsUpdated += OnMetricsUpdated;

        Task.Run(LoadSpecsAsync);
    }

    private async Task LoadSpecsAsync()
    {
        var identity = HardwareIdentifier.GetGpuId();
        _specs = await _db.LookupGpuAsync(identity, CancellationToken.None);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Subtitle = _specs?.Brand ?? identity.WmiName ?? "Unknown GPU";
            HasSpecs = _specs?.IsFromDatabase == true;

            SecondaryMetrics.Clear();
            SecondaryMetrics.Add(new MetricItem("VRAM", "â€”", "GB"));
            SecondaryMetrics.Add(new MetricItem("Core", "â€”", "MHz"));
            SecondaryMetrics.Add(new MetricItem("Mem", "â€”", "MHz"));
            SecondaryMetrics.Add(new MetricItem("Fan", "â€”", "RPM"));
            SecondaryMetrics.Add(new MetricItem("Power", "â€”", "W"));
            SecondaryMetrics.Add(new MetricItem("Temp", "â€”", "Â°C"));
        });
    }

    private void OnMetricsUpdated(object? sender, MetricBatchEventArgs e)
    {
        if (e.Updates.TryGetValue("gpu.usage", out var usage))
        {
            PrimaryValue = $"{usage.Value:F0}";
            PrimaryUnit = "%";
            PrimaryValueColor = GetUsageColor((float)usage.Value);
        }

        if (e.Updates.TryGetValue("gpu.temp.core", out var temp))
        {
            UpdateSecondaryMetric("Temp", $"{temp.Value:F0}", "Â°C");
            StatusColor = GetTempStatusColor((float)temp.Value);
        }

        if (e.Updates.TryGetValue("gpu.memory.used", out var vramUsed))
        {
            var usedGb = (float)vramUsed.Value / 1024;
            UpdateSecondaryMetric("VRAM", $"{usedGb:F1}", "GB");
        }

        if (e.Updates.TryGetValue("gpu.clock.core", out var coreClock))
        {
            UpdateSecondaryMetric("Core", $"{coreClock.Value:F0}", "MHz");
        }

        if (e.Updates.TryGetValue("gpu.clock.memory", out var memClock))
        {
            UpdateSecondaryMetric("Mem", $"{memClock.Value:F0}", "MHz");
        }

        if (e.Updates.TryGetValue("gpu.fan.rpm", out var fan))
        {
            UpdateSecondaryMetric("Fan", $"{fan.Value:F0}", "RPM");
        }

        if (e.Updates.TryGetValue("gpu.power", out var power))
        {
            UpdateSecondaryMetric("Power", $"{power.Value:F0}", "W");
        }
    }
}
```

### 6.4 Hardware Details Layout

```xml
<!-- app/Views/HardwareDetailsView.xaml (Redesigned) -->

<UserControl x:Class="OpenTraceProject.App.Views.HardwareDetailsView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Header -->
            <RowDefinition Height="*"/>     <!-- Content -->
        </Grid.RowDefinitions>

        <!-- Header -->
        <DockPanel Grid.Row="0" Margin="24,24,24,16">
            <TextBlock Text="Hardware Monitor"
                       FontSize="32"
                       FontWeight="Bold"
                       Foreground="{DynamicResource ForegroundBrightestBrush}"
                       DockPanel.Dock="Left"/>

            <StackPanel Orientation="Horizontal"
                        DockPanel.Dock="Right"
                        HorizontalAlignment="Right">
                <!-- Live Indicator -->
                <Border Background="{DynamicResource SuccessSurfaceBrush}"
                        CornerRadius="12"
                        Padding="12,6"
                        Margin="0,0,12,0">
                    <StackPanel Orientation="Horizontal">
                        <Ellipse Width="8" Height="8"
                                 Fill="{DynamicResource SuccessBrush}"
                                 Margin="0,0,8,0"/>
                        <TextBlock Text="LIVE"
                                   FontWeight="Bold"
                                   Foreground="{DynamicResource SuccessBrush}"/>
                    </StackPanel>
                </Border>

                <!-- Export Button -->
                <Button Content="ðŸ“Š"
                        Command="{Binding ExportCommand}"
                        ToolTip="Export Sensor Diagnostics"
                        Style="{StaticResource IconButtonStyle}"
                        Width="40" Height="40"/>
            </StackPanel>
        </DockPanel>

        <!-- Scrollable Content -->
        <ScrollViewer Grid.Row="1"
                      VerticalScrollBarVisibility="Auto"
                      HorizontalScrollBarVisibility="Disabled">
            <Grid Margin="16,0,16,24">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>  <!-- Primary Hardware Row -->
                    <RowDefinition Height="Auto"/>  <!-- Secondary Hardware Row -->
                    <RowDefinition Height="Auto"/>  <!-- Storage Row -->
                    <RowDefinition Height="Auto"/>  <!-- Network Row -->
                    <RowDefinition Height="Auto"/>  <!-- Process Lists -->
                </Grid.RowDefinitions>

                <!-- Primary: CPU + GPU (Large Cards) -->
                <UniformGrid Grid.Row="0" Columns="2" Margin="0,0,0,8">
                    <views:HardwareCardView DataContext="{Binding CpuCard}"/>
                    <views:HardwareCardView DataContext="{Binding GpuCard}"/>
                </UniformGrid>

                <!-- Secondary: RAM + Motherboard (Medium Cards) -->
                <UniformGrid Grid.Row="1" Columns="2" Margin="0,0,0,8">
                    <views:HardwareCardView DataContext="{Binding RamCard}"/>
                    <views:HardwareCardView DataContext="{Binding MotherboardCard}"/>
                </UniformGrid>

                <!-- Storage: List of Drives -->
                <views:StorageCardView Grid.Row="2"
                                       DataContext="{Binding StorageCard}"
                                       Margin="8,0,8,8"/>

                <!-- Network: Adapters + Latency -->
                <views:NetworkCardView Grid.Row="3"
                                       DataContext="{Binding NetworkCard}"
                                       Margin="8,0,8,8"/>

                <!-- Process Lists -->
                <Grid Grid.Row="4" Margin="8,8,8,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>

                    <views:ProcessListCard Grid.Column="0"
                                           DataContext="{Binding TopCpuProcesses}"
                                           Header="Top CPU"/>
                    <views:ProcessListCard Grid.Column="1"
                                           DataContext="{Binding TopRamProcesses}"
                                           Header="Top RAM"/>
                    <views:ProcessListCard Grid.Column="2"
                                           DataContext="{Binding TopDiskProcesses}"
                                           Header="Top Disk I/O"/>
                    <views:ProcessListCard Grid.Column="3"
                                           DataContext="{Binding TopNetworkProcesses}"
                                           Header="Top Network"/>
                </Grid>
            </Grid>
        </ScrollViewer>
    </Grid>
</UserControl>
```

---

## 7. UI/UX Improvements

### 7.1 Animation Timing Standards

| Animation Type | Duration | Easing |
|----------------|----------|--------|
| Card Hover Scale | 150ms | CubicEaseOut |
| Card Enter/Reveal | 300ms | CubicEaseOut |
| Chart Line Draw | 500ms | QuadraticEaseOut |
| Value Change | 200ms | LinearEase |
| Tab Switch | 200ms | CubicEaseInOut |
| Modal Open | 250ms | CubicEaseOut |
| Modal Close | 200ms | CubicEaseIn |
| Status LED Pulse | 1000ms | SineEaseInOut (loop) |
| Loading Spinner | 800ms | LinearEase (loop) |

### 7.2 Color System (Status Colors)

```xml
<!-- Colors.xaml additions -->

<!-- Status Colors -->
<Color x:Key="SuccessColor">#22C55E</Color>     <!-- Green - Good/Normal -->
<Color x:Key="WarningColor">#F59E0B</Color>     <!-- Amber - Caution -->
<Color x:Key="DangerColor">#EF4444</Color>      <!-- Red - Critical -->
<Color x:Key="InfoColor">#3B82F6</Color>        <!-- Blue - Informational -->

<!-- Temperature Gradient -->
<Color x:Key="TempCoolColor">#22D3EE</Color>    <!-- Cyan - Cool (<50Â°C) -->
<Color x:Key="TempWarmColor">#FBBF24</Color>    <!-- Yellow - Warm (50-70Â°C) -->
<Color x:Key="TempHotColor">#F97316</Color>     <!-- Orange - Hot (70-85Â°C) -->
<Color x:Key="TempCriticalColor">#DC2626</Color> <!-- Red - Critical (>85Â°C) -->

<!-- Usage Gradient -->
<Color x:Key="UsageLowColor">#10B981</Color>    <!-- Emerald - Low (<30%) -->
<Color x:Key="UsageMedColor">#3B82F6</Color>    <!-- Blue - Medium (30-70%) -->
<Color x:Key="UsageHighColor">#F59E0B</Color>   <!-- Amber - High (70-90%) -->
<Color x:Key="UsageCritColor">#EF4444</Color>   <!-- Red - Critical (>90%) -->

<!-- Hardware Icon Backgrounds -->
<Color x:Key="CpuAccentColor">#3B82F6</Color>   <!-- Blue -->
<Color x:Key="GpuAccentColor">#22C55E</Color>   <!-- Green -->
<Color x:Key="RamAccentColor">#A855F7</Color>   <!-- Purple -->
<Color x:Key="StorageAccentColor">#F59E0B</Color> <!-- Amber -->
<Color x:Key="NetworkAccentColor">#06B6D4</Color> <!-- Cyan -->
<Color x:Key="MoboAccentColor">#EC4899</Color>  <!-- Pink -->
```

---

## 8. Process Management Enhancements

### 8.1 Priority Control

```csharp
// infrastructure/Process/ProcessPriorityManager.cs

public class ProcessPriorityManager
{
    public static readonly Dictionary<string, ProcessPriorityClass> PriorityLevels = new()
    {
        ["Realtime"] = ProcessPriorityClass.RealTime,      // Requires admin
        ["High"] = ProcessPriorityClass.High,
        ["Above Normal"] = ProcessPriorityClass.AboveNormal,
        ["Normal"] = ProcessPriorityClass.Normal,
        ["Below Normal"] = ProcessPriorityClass.BelowNormal,
        ["Idle"] = ProcessPriorityClass.Idle
    };

    public bool SetPriority(int pid, ProcessPriorityClass priority)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            process.PriorityClass = priority;
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access denied
        {
            throw new UnauthorizedAccessException(
                "Administrator privileges required for Realtime priority", ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set priority: {ex.Message}");
            return false;
        }
    }

    public ProcessPriorityClass? GetPriority(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.PriorityClass;
        }
        catch
        {
            return null;
        }
    }
}
```

### 8.2 CPU Affinity Control

```csharp
// infrastructure/Process/ProcessAffinityManager.cs

public class ProcessAffinityManager
{
    private static readonly int ProcessorCount = Environment.ProcessorCount;

    public IntPtr GetAffinity(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessorAffinity;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    public bool SetAffinity(int pid, IntPtr affinityMask)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            process.ProcessorAffinity = affinityMask;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set affinity: {ex.Message}");
            return false;
        }
    }

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

    // Preset patterns
    public static IntPtr PerformanceCores => GetPerformanceCoreMask();
    public static IntPtr EfficiencyCores => GetEfficiencyCoreMask();
    public static IntPtr AllCores => new IntPtr((1L << ProcessorCount) - 1);
    public static IntPtr SingleCore => new IntPtr(1);

    private static IntPtr GetPerformanceCoreMask()
    {
        // For hybrid CPUs (Intel 12th gen+), P-cores are typically first half
        // This is a heuristic - actual detection requires CPUID
        var pCoreCount = ProcessorCount / 2;
        return new IntPtr((1L << pCoreCount) - 1);
    }

    private static IntPtr GetEfficiencyCoreMask()
    {
        var pCoreCount = ProcessorCount / 2;
        var eCoreCount = ProcessorCount - pCoreCount;
        return new IntPtr(((1L << eCoreCount) - 1) << pCoreCount);
    }
}
```

### 8.3 Memory Trim

```csharp
// infrastructure/Process/ProcessMemoryManager.cs

public class ProcessMemoryManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSizeEx(
        IntPtr hProcess,
        IntPtr dwMinimumWorkingSetSize,
        IntPtr dwMaximumWorkingSetSize,
        uint flags);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    private const uint QUOTA_LIMITS_HARDWS_MIN_ENABLE = 0x00000001;
    private const uint QUOTA_LIMITS_HARDWS_MAX_DISABLE = 0x00000008;

    /// <summary>
    /// Trims the working set of a process, moving pages to the pagefile.
    /// </summary>
    public bool TrimWorkingSet(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return EmptyWorkingSet(process.Handle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to trim working set: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Trims working set of all processes except system-critical ones.
    /// </summary>
    public int TrimAllProcesses()
    {
        var trimmed = 0;
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System", "Registry", "smss", "csrss", "wininit",
            "services", "lsass", "svchost", "dwm"
        };

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!excluded.Contains(process.ProcessName) &&
                    EmptyWorkingSet(process.Handle))
                {
                    trimmed++;
                }
            }
            catch { /* Access denied or process exited */ }
            finally
            {
                process.Dispose();
            }
        }

        return trimmed;
    }

    /// <summary>
    /// Gets memory statistics for a process.
    /// </summary>
    public ProcessMemoryInfo GetMemoryInfo(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return new ProcessMemoryInfo
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                PagedMemory = process.PagedMemorySize64,
                PeakWorkingSet = process.PeakWorkingSet64,
                PeakVirtualMemory = process.PeakVirtualMemorySize64
            };
        }
        catch
        {
            return new ProcessMemoryInfo();
        }
    }
}

public record ProcessMemoryInfo
{
    public long WorkingSet { get; init; }
    public long PrivateMemory { get; init; }
    public long VirtualMemory { get; init; }
    public long PagedMemory { get; init; }
    public long PeakWorkingSet { get; init; }
    public long PeakVirtualMemory { get; init; }

    public string WorkingSetFormatted => FormatBytes(WorkingSet);
    public string PrivateMemoryFormatted => FormatBytes(PrivateMemory);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
```

### 8.4 I/O Priority

```csharp
// infrastructure/Process/ProcessIoPriorityManager.cs

public class ProcessIoPriorityManager
{
    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSetInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref int processInformation,
        int processInformationLength);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref int processInformation,
        int processInformationLength,
        out int returnLength);

    private const int ProcessIoPriority = 33;

    public enum IoPriority
    {
        VeryLow = 0,
        Low = 1,
        Normal = 2,
        High = 3,      // Requires admin
        Critical = 4   // Requires admin, not recommended
    }

    public bool SetIoPriority(int pid, IoPriority priority)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            int priorityValue = (int)priority;

            var result = NtSetInformationProcess(
                process.Handle,
                ProcessIoPriority,
                ref priorityValue,
                sizeof(int));

            return result == 0; // STATUS_SUCCESS
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set I/O priority: {ex.Message}");
            return false;
        }
    }

    public IoPriority? GetIoPriority(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            int priorityValue = 0;

            var result = NtQueryInformationProcess(
                process.Handle,
                ProcessIoPriority,
                ref priorityValue,
                sizeof(int),
                out _);

            if (result == 0)
                return (IoPriority)priorityValue;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
```

### 8.5 Process Tree Operations

```csharp
// infrastructure/Process/ProcessTreeManager.cs

public class ProcessTreeManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll")]
    private static extern bool Process32First(IntPtr snapshot, ref PROCESSENTRY32 entry);

    [DllImport("kernel32.dll")]
    private static extern bool Process32Next(IntPtr snapshot, ref PROCESSENTRY32 entry);

    private const uint TH32CS_SNAPPROCESS = 0x00000002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    /// <summary>
    /// Gets all child processes of a given process.
    /// </summary>
    public List<int> GetChildProcessIds(int parentPid)
    {
        var children = new List<int>();
        var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

        if (snapshot == IntPtr.Zero)
            return children;

        try
        {
            var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };

            if (Process32First(snapshot, ref entry))
            {
                do
                {
                    if (entry.th32ParentProcessID == (uint)parentPid)
                    {
                        children.Add((int)entry.th32ProcessID);
                    }
                } while (Process32Next(snapshot, ref entry));
            }
        }
        finally
        {
            NativeMethods.CloseHandle(snapshot);
        }

        return children;
    }

    /// <summary>
    /// Gets all descendant processes (children, grandchildren, etc.).
    /// </summary>
    public List<int> GetDescendantProcessIds(int rootPid)
    {
        var descendants = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(rootPid);

        while (queue.Count > 0)
        {
            var pid = queue.Dequeue();
            var children = GetChildProcessIds(pid);

            foreach (var childPid in children)
            {
                if (!descendants.Contains(childPid))
                {
                    descendants.Add(childPid);
                    queue.Enqueue(childPid);
                }
            }
        }

        return descendants;
    }

    /// <summary>
    /// Kills a process and all its descendants.
    /// </summary>
    public int KillProcessTree(int rootPid)
    {
        var killed = 0;
        var descendants = GetDescendantProcessIds(rootPid);

        // Kill children first (bottom-up)
        descendants.Reverse();

        foreach (var pid in descendants)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                process.Kill();
                killed++;
            }
            catch { /* Process already exited or access denied */ }
        }

        // Kill root process
        try
        {
            using var rootProcess = Process.GetProcessById(rootPid);
            rootProcess.Kill();
            killed++;
        }
        catch { /* Process already exited or access denied */ }

        return killed;
    }

    /// <summary>
    /// Builds a hierarchical tree structure of processes.
    /// </summary>
    public ProcessTreeNode BuildProcessTree(int rootPid)
    {
        var root = new ProcessTreeNode
        {
            ProcessId = rootPid,
            ProcessName = GetProcessName(rootPid)
        };

        BuildTreeRecursive(root);
        return root;
    }

    private void BuildTreeRecursive(ProcessTreeNode node)
    {
        var children = GetChildProcessIds(node.ProcessId);

        foreach (var childPid in children)
        {
            var childNode = new ProcessTreeNode
            {
                ProcessId = childPid,
                ProcessName = GetProcessName(childPid),
                Parent = node
            };

            node.Children.Add(childNode);
            BuildTreeRecursive(childNode);
        }
    }

    private string GetProcessName(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return $"PID:{pid}";
        }
    }
}

public class ProcessTreeNode
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public ProcessTreeNode? Parent { get; set; }
    public List<ProcessTreeNode> Children { get; } = new();

    public int TotalDescendants => Children.Sum(c => 1 + c.TotalDescendants);
}
```

---

## 9. Implementation Order

### Sprint 1: Foundation (1-2 weeks)
- [ ] Single Instance Manager (Mutex + IPC)
- [ ] MetricDataBus (Channel-based)
- [ ] MetricWorkerPool
- [ ] PreloadManager skeleton

### Sprint 2: Data Layer (1-2 weeks)
- [ ] Hardware Database SQLite schema
- [ ] HardwareIdentifier service
- [ ] FallbackDataProvider
- [ ] Retry policies

### Sprint 3: Splash & Preload (1 week)
- [ ] Splash screen UI improvements
- [ ] Progress reporting
- [ ] Critical/non-critical task separation
- [ ] Error recovery

### Sprint 4: Hardware Details Redesign (2-3 weeks)
- [ ] HardwareCardView component
- [ ] CPU/GPU/RAM/Storage/Network cards
- [ ] Hardware details layout
- [ ] Chart improvements

### Sprint 5: Process Management (1-2 weeks)
- [ ] Priority control UI
- [ ] Affinity control UI
- [ ] Memory trim integration
- [ ] Process tree operations

### Sprint 6: Polish (1 week)
- [ ] Animation refinement
- [ ] Color system finalization
- [ ] Performance optimization
- [ ] Testing & bug fixes

---

## 10. File Structure

```
app/
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ SingleInstanceManager.cs         âœ¨ NEW
â”‚   â”œâ”€â”€ PreloadManager.cs                 âœ¨ NEW
â”‚   â””â”€â”€ ThemeManager.cs
â”œâ”€â”€ ViewModels/
â”‚   â”œâ”€â”€ Hardware/                         âœ¨ NEW FOLDER
â”‚   â”‚   â”œâ”€â”€ HardwareCardViewModelBase.cs
â”‚   â”‚   â”œâ”€â”€ CpuCardViewModel.cs
â”‚   â”‚   â”œâ”€â”€ GpuCardViewModel.cs
â”‚   â”‚   â”œâ”€â”€ RamCardViewModel.cs
â”‚   â”‚   â”œâ”€â”€ StorageCardViewModel.cs
â”‚   â”‚   â”œâ”€â”€ NetworkCardViewModel.cs
â”‚   â”‚   â””â”€â”€ MotherboardCardViewModel.cs
â”‚   â””â”€â”€ Hardware detail view models      (major update)
â”œâ”€â”€ Views/
â”‚   â”œâ”€â”€ Components/                       âœ¨ NEW FOLDER
â”‚   â”‚   â”œâ”€â”€ HardwareCardView.xaml
â”‚   â”‚   â”œâ”€â”€ StorageCardView.xaml
â”‚   â”‚   â”œâ”€â”€ NetworkCardView.xaml
â”‚   â”‚   â””â”€â”€ ProcessListCard.xaml
â”‚   â””â”€â”€ Hardware details views           (redesigned)
â””â”€â”€ Resources/
    â””â”€â”€ Colors.xaml                      (extended)

infrastructure/
â”œâ”€â”€ Threading/                            âœ¨ NEW FOLDER
â”‚   â”œâ”€â”€ MetricDataBus.cs
â”‚   â”œâ”€â”€ MetricWorkerPool.cs
â”‚   â””â”€â”€ ThreadingDiagnostics.cs
â”œâ”€â”€ Hardware/                             âœ¨ NEW FOLDER
â”‚   â”œâ”€â”€ HardwareDatabase.cs
â”‚   â”œâ”€â”€ HardwareIdentifier.cs
â”‚   â””â”€â”€ HardwareSpecs.cs
â”œâ”€â”€ Data/                                 âœ¨ NEW FOLDER
â”‚   â”œâ”€â”€ FallbackDataProvider.cs
â”‚   â””â”€â”€ RetryPolicy.cs
â”œâ”€â”€ Process/                              âœ¨ NEW FOLDER
â”‚   â”œâ”€â”€ ProcessPriorityManager.cs
â”‚   â”œâ”€â”€ ProcessAffinityManager.cs
â”‚   â”œâ”€â”€ ProcessMemoryManager.cs
â”‚   â”œâ”€â”€ ProcessIoPriorityManager.cs
â”‚   â””â”€â”€ ProcessTreeManager.cs
â””â”€â”€ Metrics/
    â””â”€â”€ (existing files updated)
```

---

## 11. Testing & Validation

### Test Scenarios

| Scenario | Expected Behavior |
|----------|-------------------|
| Launch second instance | First instance brought to foreground |
| First instance crashes, launch second | Second takes over (abandoned mutex) |
| Splash timeout (30s) | Shows error, allows retry or exit |
| Hardware DB not found | Creates new, populates defaults |
| All data sources fail | Shows "N/A" with fallback message |
| Process tree kill | All descendants terminated |
| Memory trim | Working set reduced, no crash |

### Performance Targets

| Metric | Target |
|--------|--------|
| Splash to main window | < 3 seconds |
| UI thread frame time | < 16ms (60fps) |
| Metric update latency | < 100ms |
| Memory idle | < 150MB |
| Memory active | < 300MB |
| CPU idle | < 2% |
| Hardware DB lookup | < 50ms |

---

## Related Documents

- [DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) - Known issues and recent fixes
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [HANDOFF_REPORT.md](HANDOFF_REPORT.md) - Previous handoff notes
- [CLAUDE.md](CLAUDE.md) - Development instructions

---

**Last Updated:** January 19, 2026
**Status:** APPROVED - Ready for Implementation
