using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Xml.Linq;

namespace WindowsOptimizer.Infrastructure.Metrics;

/// <summary>
/// Tracks Windows boot time metrics and history.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class BootTimeTracker
{
    private readonly string _historyFilePath;
    private readonly List<BootTimeRecord> _history;
    private readonly object _lock = new();

    public BootTimeTracker(AppPaths paths)
    {
        _historyFilePath = Path.Combine(paths.AppDataRoot, "boot-times.json");
        _history = LoadHistory();
    }

    /// <summary>
    /// Gets the last boot time from WMI.
    /// </summary>
    public DateTime? GetLastBootTime()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                var bootTimeStr = obj["LastBootUpTime"]?.ToString();
                if (!string.IsNullOrEmpty(bootTimeStr))
                {
                    return ManagementDateTimeConverter.ToDateTime(bootTimeStr);
                }
            }
        }
        catch
        {
            // Ignore WMI errors
        }
        return null;
    }

    /// <summary>
    /// Gets the boot duration from Windows Event Log.
    /// Event ID 100 from Microsoft-Windows-Diagnostics-Performance contains boot duration.
    /// </summary>
    public TimeSpan? GetLastBootDuration()
    {
        try
        {
            var query = new EventLogQuery(
                "Microsoft-Windows-Diagnostics-Performance/Operational",
                PathType.LogName,
                "*[System[EventID=100]]");

            using var reader = new EventLogReader(query);
            EventRecord? latestEvent = null;

            // Find the most recent boot event
            for (var evt = reader.ReadEvent(); evt != null; evt = reader.ReadEvent())
            {
                if (latestEvent == null || evt.TimeCreated > latestEvent.TimeCreated)
                {
                    latestEvent?.Dispose();
                    latestEvent = evt;
                }
                else
                {
                    evt.Dispose();
                }
            }

            if (latestEvent != null)
            {
                var bootTimeMs = TryGetBootTimeMs(latestEvent);
                latestEvent.Dispose();
                return bootTimeMs.HasValue && bootTimeMs.Value >= 1000
                    ? TimeSpan.FromMilliseconds(bootTimeMs.Value)
                    : null;
            }
        }
        catch (EventLogNotFoundException)
        {
            // Diagnostics-Performance log may not be available on all systems
        }
        catch (UnauthorizedAccessException)
        {
            // May need admin rights to read this log
        }
        catch
        {
            // Ignore other errors
        }
        return null;
    }

    private static long? TryGetBootTimeMs(EventRecord record)
    {
        try
        {
            if (record.Properties.Count > 0)
            {
                var raw = record.Properties[0].Value;
                if (raw != null && long.TryParse(raw.ToString(), out var parsed) && parsed > 0)
                {
                    return parsed;
                }
            }
        }
        catch
        {
        }

        try
        {
            var xml = record.ToXml();
            var doc = XDocument.Parse(xml);
            var eventData = doc.Root?.Element("EventData");
            if (eventData == null)
            {
                return null;
            }

            long? ReadNamedValue(string name)
            {
                var element = eventData.Elements("Data")
                    .FirstOrDefault(e => string.Equals(e.Attribute("Name")?.Value, name, StringComparison.OrdinalIgnoreCase));
                if (element == null)
                {
                    return null;
                }

                return long.TryParse(element.Value, out var value) ? value : null;
            }

            var bootTime = ReadNamedValue("BootTime") ?? ReadNamedValue("BootDuration");
            if (bootTime.HasValue && bootTime.Value > 0)
            {
                return bootTime.Value;
            }

            var mainPath = ReadNamedValue("MainPathBootTime");
            var postBoot = ReadNamedValue("BootPostBootTime");
            if (mainPath.HasValue || postBoot.HasValue)
            {
                var total = (mainPath ?? 0) + (postBoot ?? 0);
                return total > 0 ? total : null;
            }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>
    /// Records the current boot time to history.
    /// </summary>
    public void RecordCurrentBoot()
    {
        var bootTime = GetLastBootTime();
        var bootDuration = GetLastBootDuration();

        if (bootTime == null) return;

        lock (_lock)
        {
            // Check if we already recorded this boot
            if (_history.Any(h => Math.Abs((h.BootTime - bootTime.Value).TotalSeconds) < 60))
            {
                return; // Already recorded
            }

            var record = new BootTimeRecord
            {
                BootTime = bootTime.Value,
                BootDurationMs = bootDuration.HasValue && bootDuration.Value.TotalMilliseconds >= 1000
                    ? (long?)bootDuration.Value.TotalMilliseconds
                    : null
            };

            _history.Add(record);

            // Keep only last 30 records
            while (_history.Count > 30)
            {
                _history.RemoveAt(0);
            }

            SaveHistory();
        }
    }

    /// <summary>
    /// Gets the boot time history.
    /// </summary>
    public IReadOnlyList<BootTimeRecord> GetHistory()
    {
        lock (_lock)
        {
            return _history.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the average boot duration from history.
    /// </summary>
    public TimeSpan? GetAverageBootDuration()
    {
        lock (_lock)
        {
            var validRecords = _history
                .Where(h => h.BootDurationMs.HasValue && h.BootDurationMs >= 1000)
                .ToList();

            if (validRecords.Count == 0) return null;

            var avgMs = validRecords.Average(h => h.BootDurationMs!.Value);
            return TimeSpan.FromMilliseconds(avgMs);
        }
    }

    /// <summary>
    /// Gets the last N boot durations for trend display.
    /// </summary>
    public IReadOnlyList<double> GetRecentBootDurations(int count = 10)
    {
        lock (_lock)
        {
            return _history
                .Where(h => h.BootDurationMs.HasValue && h.BootDurationMs >= 1000)
                .OrderByDescending(h => h.BootTime)
                .Take(count)
                .OrderBy(h => h.BootTime)
                .Select(h => h.BootDurationMs!.Value / 1000.0) // Convert to seconds
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the boot time improvement percentage comparing latest to earliest.
    /// </summary>
    public double? GetImprovementPercentage()
    {
        lock (_lock)
        {
            var validRecords = _history
                .Where(h => h.BootDurationMs.HasValue && h.BootDurationMs > 0)
                .OrderBy(h => h.BootTime)
                .ToList();

            if (validRecords.Count < 2) return null;

            var earliest = validRecords.First().BootDurationMs!.Value;
            var latest = validRecords.Last().BootDurationMs!.Value;

            if (earliest <= 0) return null;

            return ((earliest - latest) / earliest) * 100;
        }
    }

    private List<BootTimeRecord> LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var json = File.ReadAllText(_historyFilePath);
                var history = JsonSerializer.Deserialize<List<BootTimeRecord>>(json);
                return history ?? new List<BootTimeRecord>();
            }
        }
        catch
        {
            // Ignore load errors
        }
        return new List<BootTimeRecord>();
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_historyFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

/// <summary>
/// A record of a single boot time measurement.
/// </summary>
public sealed class BootTimeRecord
{
    public DateTime BootTime { get; init; }
    public long? BootDurationMs { get; init; }

    public string BootDurationFormatted => BootDurationMs.HasValue
        ? $"{BootDurationMs.Value / 1000.0:F1}s"
        : "Unknown";
}
