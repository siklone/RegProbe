using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure;

public sealed class FileTweakLogStore : ITweakLogStore
{
    private readonly object _sync = new();
    private readonly AppPaths _paths;

    public FileTweakLogStore(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public Task AppendAsync(TweakLogEntry entry, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        _paths.EnsureDirectories();

        lock (_sync)
        {
            EnsureCsvHeader();
            File.AppendAllText(_paths.TweakLogFilePath, BuildCsvLine(entry) + Environment.NewLine, Encoding.UTF8);
        }

        return Task.CompletedTask;
    }

    public Task ExportCsvAsync(string destinationPath, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination path is required.", nameof(destinationPath));
        }

        _paths.EnsureDirectories();

        lock (_sync)
        {
            EnsureCsvHeader();
            File.Copy(_paths.TweakLogFilePath, destinationPath, true);
        }

        return Task.CompletedTask;
    }

    private void EnsureCsvHeader()
    {
        if (File.Exists(_paths.TweakLogFilePath))
        {
            return;
        }

        var header = "timestamp,tweak_id,tweak_name,action,status,message,error";
        File.WriteAllText(_paths.TweakLogFilePath, header + Environment.NewLine, Encoding.UTF8);
    }

    private static string BuildCsvLine(TweakLogEntry entry)
    {
        return string.Join(",",
            Escape(entry.Timestamp.ToString("O")),
            Escape(entry.TweakId),
            Escape(entry.TweakName),
            Escape(entry.Action.ToString()),
            Escape(entry.Status.ToString()),
            Escape(entry.Message),
            Escape(entry.Error));
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        if (escaped.IndexOfAny(new[] { ',', '\n', '\r', '\"' }) >= 0)
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }
}
