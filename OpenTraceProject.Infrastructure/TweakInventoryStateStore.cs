using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OpenTraceProject.Infrastructure;

public sealed class TweakInventoryState
{
    public string Id { get; set; } = string.Empty;

    public string AppliedStatus { get; set; } = "Unknown";

    public string CurrentValue { get; set; } = "Unknown";

    public string TargetValue { get; set; } = "Optimized";

    public DateTimeOffset? LastDetectedAtUtc { get; set; }

    public string ImpactArea { get; set; } = string.Empty;
}

public interface ITweakInventoryStateStore
{
    IReadOnlyDictionary<string, TweakInventoryState> Load();

    void Save(IEnumerable<TweakInventoryState> states);
}

public sealed class TweakInventoryStateStore : ITweakInventoryStateStore
{
    private const int CurrentVersion = 1;
    private readonly string _filePath;
    private readonly object _sync = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public TweakInventoryStateStore(AppPaths paths)
    {
        if (paths is null) throw new ArgumentNullException(nameof(paths));
        _filePath = Path.Combine(paths.AppDataRoot, "tweak-inventory-cache.json");
    }

    public IReadOnlyDictionary<string, TweakInventoryState> Load()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new Dictionary<string, TweakInventoryState>(StringComparer.OrdinalIgnoreCase);
                }

                var json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Dictionary<string, TweakInventoryState>(StringComparer.OrdinalIgnoreCase);
                }

                var document = JsonSerializer.Deserialize<TweakInventoryStateDocument>(json, JsonOptions);
                if (document?.Entries is null)
                {
                    return new Dictionary<string, TweakInventoryState>(StringComparer.OrdinalIgnoreCase);
                }

                var result = new Dictionary<string, TweakInventoryState>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in document.Entries)
                {
                    if (entry is null || string.IsNullOrWhiteSpace(entry.Id))
                    {
                        continue;
                    }

                    result[entry.Id] = entry;
                }

                return result;
            }
            catch
            {
                return new Dictionary<string, TweakInventoryState>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public void Save(IEnumerable<TweakInventoryState> states)
    {
        if (states is null) throw new ArgumentNullException(nameof(states));

        lock (_sync)
        {
            try
            {
                var deduplicated = states
                    .Where(state => state is not null && !string.IsNullOrWhiteSpace(state.Id))
                    .GroupBy(state => state.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .ToList();

                var document = new TweakInventoryStateDocument
                {
                    Version = CurrentVersion,
                    SavedAtUtc = DateTimeOffset.UtcNow,
                    Entries = deduplicated
                };

                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = _filePath + ".tmp";
                var json = JsonSerializer.Serialize(document, JsonOptions);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _filePath, overwrite: true);
            }
            catch
            {
                // Cache persistence is best-effort.
            }
        }
    }

    private sealed class TweakInventoryStateDocument
    {
        public int Version { get; set; } = CurrentVersion;

        public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public List<TweakInventoryState> Entries { get; set; } = new();
    }
}
