using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTraceProject.Infrastructure.Elevation;

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

        // Check if we're on Windows - elevated host only works on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Elevated operations are only supported on Windows. " +
                "The application is currently running on " + RuntimeInformation.OSDescription);
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
            return await SendWithRetryAsync(request, _options.InitialConnectTimeout, ct);
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

            LogToFile($"ElevatedHostClient: Checking if host is already running");
            if (await TryConnectAsync(_options.InitialConnectTimeout, ct))
            {
                _isReady = true;
                LogToFile($"ElevatedHostClient: Host is already running");
                return;
            }

            LogToFile($"ElevatedHostClient: Host not running, starting it");
            StartHost();

            LogToFile($"ElevatedHostClient: Waiting {_options.StartupConnectTimeout.TotalSeconds}s for host to start");
            if (!await TryConnectAsync(_options.StartupConnectTimeout, ct))
            {
                var exePath = _options.HostExecutablePath ?? "unknown";
                throw new ElevatedHostException(
                    $"Failed to connect to an elevated host after starting it. " +
                    $"EXE path: {exePath}. Check if UAC prompt was shown and accepted.");
            }

            _isReady = true;
            LogToFile($"ElevatedHostClient: Successfully connected to host");
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

    private async Task<ElevatedHostResponse> SendWithRetryAsync(
        ElevatedHostRequest request,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var attempts = Math.Max(1, _options.MaxConnectRetries);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            if (attempt > 1)
            {
                var delay = GetRetryDelay(attempt);
                if (delay > TimeSpan.Zero)
                {
                    LogToFile($"ElevatedHostClient: Retry {attempt}/{attempts} in {delay.TotalMilliseconds:0}ms");
                    await Task.Delay(delay, ct);
                }
            }

            try
            {
                var response = await SendOnceAsync(request, timeout, ct);
                _isReady = true;
                return response;
            }
            catch (Exception ex) when (IsConnectFailure(ex))
            {
                lastException = ex;
                LogToFile($"ElevatedHostClient: Connect attempt {attempt}/{attempts} failed: {ex.Message}");
            }
        }

        var message =
            $"Failed to connect to the elevated host after {attempts} attempt(s). " +
            "Check if UAC was cancelled or if the host executable is missing.";

        if (lastException is null)
        {
            throw new ElevatedHostException(message);
        }

        throw new ElevatedHostException(message, lastException);
    }

    private async Task<bool> TryConnectAsync(TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            var response = await SendOnceAsync(
                new ElevatedHostRequest(Guid.NewGuid(), ElevatedHostRequestType.Ping),
                timeout,
                ct);
            return response.IsElevated;
        }
        catch (Exception ex) when (IsConnectFailure(ex))
        {
            return false;
        }
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        if (attempt <= 1)
        {
            return TimeSpan.Zero;
        }

        var baseDelay = _options.RetryBaseDelay <= TimeSpan.Zero
            ? TimeSpan.FromMilliseconds(200)
            : _options.RetryBaseDelay;
        var maxDelay = _options.MaxRetryDelay <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(2)
            : _options.MaxRetryDelay;
        var backoffMs = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 2);
        var delayMs = Math.Min(backoffMs, maxDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(delayMs);
    }

    private void StartHost()
    {
        if (string.IsNullOrWhiteSpace(_options.HostExecutablePath))
        {
            throw new ElevatedHostLaunchException("Elevated host executable path is not configured.");
        }

        LogToFile($"ElevatedHostClient: Checking for host executable at: {_options.HostExecutablePath}");
        if (!File.Exists(_options.HostExecutablePath))
        {
            throw new ElevatedHostLaunchException(
                $"Elevated host not found at '{_options.HostExecutablePath}'. " +
                $"If you're running from source, ensure '{ElevatedHostDefaults.ExecutableName}' is copied next to the app " +
                $"or set {ElevatedHostDefaults.OverridePathEnvVar} to a valid full path.");
        }

        var arguments = $"--pipe \"{_options.PipeName}\"";
        if (_options.ParentProcessId > 0)
        {
            arguments += $" --parent-pid {_options.ParentProcessId}";
        }

        LogToFile($"ElevatedHostClient: Starting host with arguments: {arguments}");
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.HostExecutablePath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(_options.HostExecutablePath) ?? string.Empty,
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            var process = Process.Start(startInfo);
            if (process is null)
            {
                LogToFile($"ElevatedHostClient: Process.Start returned null - UAC likely cancelled");
                throw new ElevatedHostLaunchException("Failed to start elevated host process (UAC likely cancelled).");
            }

            LogToFile($"ElevatedHostClient: Host process started with PID {process.Id}");

            // If the process exits immediately, surface a clearer error instead of waiting for pipe timeouts.
            if (process.WaitForExit(500))
            {
                LogToFile($"ElevatedHostClient: Host process exited immediately with code {process.ExitCode}");
                throw new ElevatedHostLaunchException(
                    $"Elevated host exited immediately with code {process.ExitCode}. " +
                    "Check that all ElevatedHost dependencies were copied next to the executable.");
            }

            process.Dispose();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            LogToFile($"ElevatedHostClient: Elevation was cancelled by the user (error 1223)");
            throw new ElevatedHostLaunchException("Elevation was cancelled by the user.", ex);
        }
        catch (Exception ex)
        {
            LogToFile($"ElevatedHostClient: Failed to start host: {ex.GetType().Name}: {ex.Message}");
            throw new ElevatedHostLaunchException("Failed to start the elevated host.", ex);
        }
    }

    private static bool IsConnectFailure(Exception ex)
    {
        return ex is TimeoutException or IOException or UnauthorizedAccessException;
    }

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(Path.GetTempPath(), "OpenTraceProject_Diagnostics.log");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
