using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace RegProbe.App.Services;

internal sealed class SingleInstanceIpcServer : IDisposable
{
    private readonly string _pipeName;
    private readonly Func<string[], CancellationToken, Task> _onArgumentsReceived;

    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _pipeCts;
    private bool _disposed;

    public SingleInstanceIpcServer(
        string pipeName,
        Func<string[], CancellationToken, Task> onArgumentsReceived)
    {
        _pipeName = pipeName;
        _onArgumentsReceived = onArgumentsReceived;
    }

    public void Start()
    {
        _pipeCts = new CancellationTokenSource();
        _ = Task.Run(() => IpcServerLoop(_pipeCts.Token));
    }

    private async Task IpcServerLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                Debug.WriteLine("[SingleInstance] IPC server waiting for connection...");
                await _pipeServer.WaitForConnectionAsync(ct);
                Debug.WriteLine("[SingleInstance] IPC client connected");

                await ProcessConnectionAsync(_pipeServer, ct);

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
                try { await Task.Delay(100, ct); }
                catch { break; }
            }
        }
    }

    private async Task ProcessConnectionAsync(Stream pipeServer, CancellationToken ct)
    {
        using var reader = new StreamReader(
            pipeServer,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);
        var json = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        var args = JsonSerializer.Deserialize<string[]>(json);
        if (args is { Length: > 0 })
        {
            await _onArgumentsReceived(args, ct);
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
        catch
        {
        }
    }
}
