using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Infrastructure;

public sealed class FileTweakLogStore : ITweakLogStore
{
    private readonly object _sync = new();
    private readonly AppPaths _paths;

    public FileTweakLogStore(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public Task<IReadOnlyList<TweakLogEntry>> GetRecentHistoryAsync(int count, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<TweakLogEntry>>(ct);
        }

        var entries = new List<TweakLogEntry>();

        lock (_sync)
        {
            if (!File.Exists(_paths.TweakLogFilePath))
            {
                return Task.FromResult<IReadOnlyList<TweakLogEntry>>(entries.AsReadOnly());
            }

            try
            {
                var lines = File.ReadAllLines(_paths.TweakLogFilePath, Encoding.UTF8);
                // Skip header and parse from end (most recent first)
                foreach (var line in lines.Skip(1).Reverse().Take(count))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parsed = ParseCsvLine(line);
                    if (parsed != null)
                    {
                        entries.Add(parsed);
                    }
                }
            }
            catch
            {
                // Ignore read errors
            }
        }

        return Task.FromResult<IReadOnlyList<TweakLogEntry>>(entries.AsReadOnly());
    }

    private static TweakLogEntry? ParseCsvLine(string line)
    {
        try
        {
            var parts = ParseCsvFields(line);
            if (parts.Count < 6) return null;

            var timestamp = DateTimeOffset.Parse(parts[0]);
            var tweakId = parts[1];
            var tweakName = parts[2];
            var action = Enum.Parse<TweakAction>(parts[3], true);
            var status = Enum.Parse<TweakStatus>(parts[4], true);
            var message = parts[5];
            var error = parts.Count > 6 ? parts[6] : null;

            return new TweakLogEntry(timestamp, tweakId, tweakName, action, status, message, error);
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ParseCsvFields(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
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
