using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace RegProbe.App.Services;

internal sealed class SingleInstanceIpcClient
{
    private readonly string _pipeName;
    private readonly int _connectTimeoutMs;
    private readonly int _maxRetries;

    public SingleInstanceIpcClient(string pipeName, int connectTimeoutMs, int maxRetries)
    {
        _pipeName = pipeName;
        _connectTimeoutMs = connectTimeoutMs;
        _maxRetries = maxRetries;
    }

    public void SendArgsToFirstInstance(string[] args)
    {
        for (var retry = 0; retry < _maxRetries; retry++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
                client.Connect(_connectTimeoutMs);

                using var writer = new StreamWriter(client);
                writer.Write(JsonSerializer.Serialize(args));
                writer.Flush();

                Debug.WriteLine("[SingleInstance] Args sent to first instance");
                return;
            }
            catch (TimeoutException)
            {
                Debug.WriteLine($"[SingleInstance] Pipe connect timeout (attempt {retry + 1}/{_maxRetries})");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[SingleInstance] Pipe IO error: {ex.Message}");
            }
        }

        SingleInstanceUserNotifier.ShowInstanceWarning();
    }
}
