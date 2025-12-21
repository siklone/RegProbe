using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed class ElevatedHostClient : IElevatedHostClient
{
    private readonly ElevatedHostClientOptions _options;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _isReady;

    public ElevatedHostClient(ElevatedHostClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(_options.PipeName))
        {
            throw new ArgumentException("Pipe name is required.", nameof(options));
        }
    }

    public async Task<ElevatedHostResponse> SendAsync(ElevatedHostRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            var response = await SendOnceAsync(request, _options.InitialConnectTimeout, ct);
            _isReady = true;
            return response;
        }
        catch (Exception ex) when (IsConnectFailure(ex))
        {
            await EnsureHostStartedAsync(ct);
            return await SendOnceAsync(request, _options.StartupConnectTimeout, ct);
        }
    }

    private async Task EnsureHostStartedAsync(CancellationToken ct)
    {
        if (_isReady)
        {
            return;
        }

        await _startLock.WaitAsync(ct);
        try
        {
            if (_isReady)
            {
                return;
            }

            if (await TryConnectAsync(_options.InitialConnectTimeout, ct))
            {
                _isReady = true;
                return;
            }

            StartHost();

            if (!await TryConnectAsync(_options.StartupConnectTimeout, ct))
            {
                throw new ElevatedHostException("Failed to connect to the elevated host.");
            }

            _isReady = true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    private async Task<ElevatedHostResponse> SendOnceAsync(
        ElevatedHostRequest request,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var pipe = new NamedPipeClientStream(
            ".",
            _options.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        var timeoutMs = (int)Math.Max(1, timeout.TotalMilliseconds);
        await pipe.ConnectAsync(timeoutMs, ct);

        await PipeMessageSerializer.WriteAsync(pipe, request, ct);
        return await PipeMessageSerializer.ReadAsync<ElevatedHostResponse>(pipe, ct);
    }

    private async Task<bool> TryConnectAsync(TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                _options.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            var timeoutMs = (int)Math.Max(1, timeout.TotalMilliseconds);
            await pipe.ConnectAsync(timeoutMs, ct);
            return true;
        }
        catch (Exception ex) when (IsConnectFailure(ex))
        {
            return false;
        }
    }

    private void StartHost()
    {
        if (string.IsNullOrWhiteSpace(_options.HostExecutablePath))
        {
            throw new ElevatedHostLaunchException("Elevated host executable path is not configured.");
        }

        if (!File.Exists(_options.HostExecutablePath))
        {
            throw new ElevatedHostLaunchException($"Elevated host not found at '{_options.HostExecutablePath}'.");
        }

        var arguments = $"--pipe \"{_options.PipeName}\"";
        if (_options.ParentProcessId > 0)
        {
            arguments += $" --parent-pid {_options.ParentProcessId}";
        }
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.HostExecutablePath,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            Process.Start(startInfo);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new ElevatedHostLaunchException("Elevation was cancelled by the user.", ex);
        }
        catch (Exception ex)
        {
            throw new ElevatedHostLaunchException("Failed to start the elevated host.", ex);
        }
    }

    private static bool IsConnectFailure(Exception ex)
    {
        return ex is TimeoutException or IOException;
    }
}
