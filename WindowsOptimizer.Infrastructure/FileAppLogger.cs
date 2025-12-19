using System;
using System.IO;

namespace WindowsOptimizer.Infrastructure;

public sealed class FileAppLogger : IAppLogger
{
    private readonly object _sync = new();
    private readonly AppPaths _paths;

    public FileAppLogger(AppPaths paths)
    {
        _paths = paths;
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _paths.EnsureDirectories();

        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var line = $"{timestamp} [{level.ToString().ToUpperInvariant()}] {message}";
        if (exception is not null)
        {
            line += $" | {exception.GetType().Name}: {exception.Message}";
        }

        lock (_sync)
        {
            File.AppendAllText(_paths.LogFilePath, line + Environment.NewLine);
        }
    }
}
