using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RegProbe.App.Services;

/// <summary>
/// Ensures only one instance of the application can run at a time.
/// Uses a named mutex for detection and named pipes for IPC.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexPrefix = "Global\\RegProbe_SingleInstance";
    private const string PipePrefix = "RegProbe_IPC";
    private const int PipeConnectTimeoutMs = 3000;
    private const int MaxRetries = 3;

    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly SingleInstanceIpcClient _ipcClient;

    private Mutex? _mutex;
    private SingleInstanceIpcServer? _ipcServer;
    private bool _isFirstInstance;
    private bool _disposed;

    public SingleInstanceManager()
    {
        var key = SingleInstanceKeyProvider.GetInstanceKey();
        _mutexName = $"{MutexPrefix}_{key}";
        _pipeName = $"{PipePrefix}_{key}";
        _ipcClient = new SingleInstanceIpcClient(_pipeName, PipeConnectTimeoutMs, MaxRetries);
    }

    /// <summary>
    /// Returns true if this is the first (and only) instance of the application.
    /// </summary>
    public bool IsFirstInstance => _isFirstInstance;

    /// <summary>
    /// Raised when another instance sends command-line arguments.
    /// Always raised on the UI thread.
    /// </summary>
    public event EventHandler<string[]>? ArgumentsReceived;

    /// <summary>
    /// Attempts to acquire the single instance lock.
    /// </summary>
    /// <returns>True if this is the first instance, false if another instance is running.</returns>
    public bool TryAcquire()
    {
        try
        {
            // createdNew = true means we created the mutex (first instance)
            // createdNew = false means mutex already exists (another instance)
            _mutex = new Mutex(true, _mutexName, out _isFirstInstance);

            if (_isFirstInstance)
            {
                Debug.WriteLine("[SingleInstance] First instance - starting IPC server");
                StartIpcServer();
                return true;
            }
            else
            {
                Debug.WriteLine("[SingleInstance] Second instance - forwarding args to first");
                _ipcClient.SendArgsToFirstInstance(Environment.GetCommandLineArgs());
                return false;
            }
        }
        catch (AbandonedMutexException)
        {
            // Previous instance crashed without releasing mutex
            // We take ownership and become the first instance
            Debug.WriteLine("[SingleInstance] Abandoned mutex detected - taking ownership");
            _isFirstInstance = true;
            StartIpcServer();
            return true;
        }
        catch (Exception ex)
        {
            // Mutex creation failed (extremely rare)
            Debug.WriteLine($"[SingleInstance] Mutex creation failed: {ex.Message}");
            // Fail open - allow this instance to run
            _isFirstInstance = true;
            return true;
        }
    }

    private void StartIpcServer()
    {
        _ipcServer = new SingleInstanceIpcServer(_pipeName, HandleArgumentsReceivedAsync);
        _ipcServer.Start();
    }

    private async Task HandleArgumentsReceivedAsync(string[] args, CancellationToken ct)
    {
        Debug.WriteLine($"[SingleInstance] Received args: {string.Join(" ", args)}");

        var app = System.Windows.Application.Current;
        if (app != null)
        {
            await app.Dispatcher.InvokeAsync(() =>
            {
                ArgumentsReceived?.Invoke(this, args);
                SingleInstanceWindowActivator.BringToForeground();
            }, DispatcherPriority.Normal, ct);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _ipcServer?.Dispose();

        try
        {
            if (_isFirstInstance && _mutex != null)
            {
                _mutex.ReleaseMutex();
            }
            _mutex?.Dispose();
        }
        catch { /* Ignore dispose errors */ }

        Debug.WriteLine("[SingleInstance] Disposed");
    }
}
