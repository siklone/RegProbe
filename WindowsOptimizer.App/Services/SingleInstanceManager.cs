using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Ensures only one instance of the application can run at a time.
/// Uses a named mutex for detection and named pipes for IPC.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "Global\\WindowsOptimizer_SingleInstance_v2";
    private const string PipeName = "WindowsOptimizer_IPC_v2";
    private const int PipeConnectTimeoutMs = 3000;
    private const int MaxRetries = 3;

    private Mutex? _mutex;
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _pipeCts;
    private bool _isFirstInstance;
    private bool _disposed;

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
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);

            if (_isFirstInstance)
            {
                Debug.WriteLine("[SingleInstance] First instance - starting IPC server");
                StartIpcServer();
                return true;
            }
            else
            {
                Debug.WriteLine("[SingleInstance] Second instance - forwarding args to first");
                SendArgsToFirstInstance(Environment.GetCommandLineArgs());
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
        _pipeCts = new CancellationTokenSource();
        // Run on thread pool, not blocking
        _ = Task.Run(() => IpcServerLoop(_pipeCts.Token));
    }

    private async Task IpcServerLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Create new pipe server for each connection
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,  // Max one client at a time
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                Debug.WriteLine("[SingleInstance] IPC server waiting for connection...");
                await _pipeServer.WaitForConnectionAsync(ct);
                Debug.WriteLine("[SingleInstance] IPC client connected");

                using var reader = new StreamReader(_pipeServer);
                var json = await reader.ReadToEndAsync(ct);

                if (!string.IsNullOrEmpty(json))
                {
                    var args = JsonSerializer.Deserialize<string[]>(json);
                    if (args != null && args.Length > 0)
                    {
                        Debug.WriteLine($"[SingleInstance] Received args: {string.Join(" ", args)}");

                        // Dispatch to UI thread
                        var app = Application.Current;
                        if (app != null)
                        {
                            await app.Dispatcher.InvokeAsync(() =>
                            {
                                ArgumentsReceived?.Invoke(this, args);
                                BringToForeground();
                            }, DispatcherPriority.Normal, ct);
                        }
                    }
                }

                _pipeServer.Disconnect();
                _pipeServer.Dispose();
                _pipeServer = null;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[SingleInstance] IPC server cancelled");
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SingleInstance] IPC error: {ex.Message}");
                // Brief delay before retry to prevent tight loop
                try { await Task.Delay(100, ct); }
                catch { break; }
            }
        }
    }

    private void SendArgsToFirstInstance(string[] args)
    {
        for (int retry = 0; retry < MaxRetries; retry++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(PipeConnectTimeoutMs);

                using var writer = new StreamWriter(client);
                writer.Write(JsonSerializer.Serialize(args));
                writer.Flush();

                Debug.WriteLine("[SingleInstance] Args sent to first instance");
                return;
            }
            catch (TimeoutException)
            {
                Debug.WriteLine($"[SingleInstance] Pipe connect timeout (attempt {retry + 1}/{MaxRetries})");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[SingleInstance] Pipe IO error: {ex.Message}");
            }
        }

        // All retries failed - first instance may be hung
        ShowInstanceWarning();
    }

    private static void ShowInstanceWarning()
    {
        MessageBox.Show(
            "Windows Optimizer is already running but not responding.\n\n" +
            "Please close the existing instance using Task Manager,\n" +
            "or restart your computer if the problem persists.",
            "Windows Optimizer",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static void BringToForeground()
    {
        var mainWindow = Application.Current?.MainWindow;
        if (mainWindow == null) return;

        try
        {
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;

            mainWindow.Activate();

            // Workaround: Briefly set Topmost to force focus
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;

            mainWindow.Focus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SingleInstance] BringToForeground failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pipeCts?.Cancel();

        try
        {
            _pipeServer?.Dispose();
        }
        catch { /* Ignore dispose errors */ }

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
