using System;
using System.IO;
using System.Linq;

namespace WindowsOptimizer.Infrastructure;

public sealed class FileAppLogger : IAppLogger
{
    private const long MaxLogBytes = 10 * 1024 * 1024;
    private const int MaxArchivedLogs = 5;
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
            RotateIfNeeded();
            File.AppendAllText(_paths.LogFilePath, line + Environment.NewLine);
        }
    }

    private void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(_paths.LogFilePath))
            {
                return;
            }

            var info = new FileInfo(_paths.LogFilePath);
            if (info.Length < MaxLogBytes)
            {
                return;
            }

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var archivePath = Path.Combine(_paths.LogDirectory, $"app-{timestamp}.log");
            File.Move(_paths.LogFilePath, archivePath);

            var archives = Directory.GetFiles(_paths.LogDirectory, "app-*.log")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            foreach (var old in archives.Skip(MaxArchivedLogs))
            {
                old.Delete();
            }
        }
        catch
        {
            // Ignore rotation errors to avoid blocking log writes.
        }
    }
}
