using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Plugins;
using RegProbe.Core.Registry;
using RegProbe.Engine.Tweaks;

namespace RegProbe.Application.Services.TweakProviders;

/// <summary>
/// Loads tweak definitions from JSON files with hot-reload support.
/// Tweaks are instantiated on demand with proper registry accessor.
/// </summary>
public sealed class JsonTweakLoader : IDisposable
{
    private readonly string _jsonDirectory;
    private readonly ConcurrentDictionary<string, JsonTweakEntry> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _definitionSourceById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HashSet<string>> _definitionIdsByFile = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyList<JsonTweakValidationIssue>> _validationIssuesByFile = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;
    private readonly object _reloadLock = new();
    private bool _hotReloadEnabled;

    public event Action? DefinitionsReloaded;

    public JsonTweakLoader(string jsonDirectory)
    {
        _jsonDirectory = jsonDirectory;
        LoadAllDefinitions();
    }

    /// <summary>
    /// Gets all tweak IDs available from JSON definitions.
    /// </summary>
    public IEnumerable<string> GetTweakIds() => _definitions.Keys;

    /// <summary>
    /// Gets the count of loaded definitions.
    /// </summary>
    public int Count => _definitions.Count;

    public IReadOnlyList<JsonTweakValidationIssue> ValidationIssues =>
        _validationIssuesByFile.Values
            .SelectMany(static issues => issues)
            .OrderBy(static issue => issue.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static issue => issue.EntryId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static issue => issue.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// Creates tweaks using the provided registry accessor.
    /// </summary>
    public IEnumerable<ITweak> CreateTweaks(IRegistryAccessor registryAccessor)
    {
        foreach (var (id, entry) in _definitions)
        {
            var tweak = CreateTweakFromEntry(entry, registryAccessor);
            if (tweak != null)
                yield return tweak;
        }
    }

    /// <summary>
    /// Enables hot-reload watching for JSON file changes.
    /// </summary>
    public void EnableHotReload()
    {
        if (_hotReloadEnabled || !Directory.Exists(_jsonDirectory))
            return;

        _watcher = new FileSystemWatcher(_jsonDirectory, "*.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
        _hotReloadEnabled = true;
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        await Task.Delay(200); // Debounce

        lock (_reloadLock)
        {
            try
            {
                ReloadSingleFile(e.FullPath);
                DefinitionsReloaded?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hot-reload failed: {ex.Message}");
            }
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        lock (_reloadLock)
        {
            RemoveDefinitionsForFile(e.FullPath);
            _validationIssuesByFile.TryRemove(e.FullPath, out _);
            DefinitionsReloaded?.Invoke();
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        lock (_reloadLock)
        {
            RemoveDefinitionsForFile(e.OldFullPath);
            _validationIssuesByFile.TryRemove(e.OldFullPath, out _);
            ReloadSingleFile(e.FullPath);
            DefinitionsReloaded?.Invoke();
        }
    }

    private void LoadAllDefinitions()
    {
        _definitions.Clear();
        _definitionSourceById.Clear();
        _definitionIdsByFile.Clear();
        _validationIssuesByFile.Clear();

        if (!Directory.Exists(_jsonDirectory))
            return;

        foreach (var jsonFile in Directory.GetFiles(_jsonDirectory, "*.json"))
        {
            try
            {
                ReloadSingleFile(jsonFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load {jsonFile}: {ex.Message}");
            }
        }
    }

    private void ReloadSingleFile(string filePath)
    {
        RemoveDefinitionsForFile(filePath);
        if (!File.Exists(filePath))
        {
            _validationIssuesByFile.TryRemove(filePath, out _);
            return;
        }

        var result = ParseDefinitionsFromFile(filePath);
        _validationIssuesByFile[filePath] = result.Issues;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in result.Entries)
        {
            if (entry.Id is null)
            {
                continue;
            }

            if (_definitionSourceById.TryGetValue(entry.Id, out var existingSource)
                && !existingSource.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                AppendValidationIssue(
                    filePath,
                    new JsonTweakValidationIssue(
                        filePath,
                        "duplicate-id",
                        $"Entry '{entry.Id}' already exists in '{existingSource}'.",
                        entry.Id));
                continue;
            }

            _definitions[entry.Id] = entry;
            _definitionSourceById[entry.Id] = filePath;
            ids.Add(entry.Id);
        }

        _definitionIdsByFile[filePath] = ids;
    }

    private void RemoveDefinitionsForFile(string filePath)
    {
        if (!_definitionIdsByFile.TryRemove(filePath, out var ids))
        {
            return;
        }

        foreach (var id in ids)
        {
            if (_definitionSourceById.TryGetValue(id, out var source)
                && source.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                _definitionSourceById.TryRemove(id, out _);
                _definitions.TryRemove(id, out _);
            }
        }
    }

    private JsonTweakLoadResult ParseDefinitionsFromFile(string filePath)
    {
        var issues = new List<JsonTweakValidationIssue>();
        JsonTweakDocument? document;
        try
        {
            var json = File.ReadAllText(filePath);
            using var parsed = JsonDocument.Parse(json);
            ValidateDocumentShape(parsed.RootElement, filePath, issues);
            document = parsed.RootElement.Deserialize<JsonTweakDocument>();
        }
        catch (JsonException ex)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "invalid-json", ex.Message));
            return new JsonTweakLoadResult([], issues);
        }
        catch (Exception ex)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "load-failed", ex.Message));
            return new JsonTweakLoadResult([], issues);
        }

        if (document?.Categories == null)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-categories", "Document did not contain a 'categories' object."));
            return new JsonTweakLoadResult([], issues);
        }

        var entries = new List<JsonTweakEntry>();
        foreach (var (_, category) in document.Categories)
        {
            if (category.Entries == null || category.Entries.Count == 0)
            {
                issues.Add(new JsonTweakValidationIssue(filePath, "empty-category", $"Category '{category.Name ?? "<unnamed>"}' did not contain entries."));
                continue;
            }

            foreach (var entry in category.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    issues.Add(new JsonTweakValidationIssue(filePath, "missing-id", "Entry is missing required field 'id'."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Path))
                {
                    issues.Add(new JsonTweakValidationIssue(filePath, "missing-path", $"Entry '{entry.Id}' is missing required field 'path'.", entry.Id));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.ValueName))
                {
                    issues.Add(new JsonTweakValidationIssue(filePath, "missing-value-name", $"Entry '{entry.Id}' is missing required field 'value_name'.", entry.Id));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Type))
                {
                    issues.Add(new JsonTweakValidationIssue(filePath, "missing-type", $"Entry '{entry.Id}' is missing required field 'type'.", entry.Id));
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Documentation) && entry.Verified != true)
                {
                    issues.Add(new JsonTweakValidationIssue(
                        filePath,
                        "documentation-required",
                        $"Entry '{entry.Id}' is missing documentation and is not marked verified.",
                        entry.Id));
                    continue;
                }

                entry.CategoryRiskLevel = category.RiskLevel;
                entries.Add(entry);
            }
        }

        return new JsonTweakLoadResult(entries, issues);
    }

    private void AppendValidationIssue(string filePath, JsonTweakValidationIssue issue)
    {
        var existing = _validationIssuesByFile.TryGetValue(filePath, out var value)
            ? value.ToList()
            : new List<JsonTweakValidationIssue>();
        existing.Add(issue);
        _validationIssuesByFile[filePath] = existing;
    }

    private static void ValidateDocumentShape(JsonElement root, string filePath, List<JsonTweakValidationIssue> issues)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "root-not-object", "JSON root must be an object."));
            return;
        }

        if (!root.TryGetProperty("categories", out var categories) || categories.ValueKind != JsonValueKind.Object)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-categories", "JSON document must contain an object property named 'categories'."));
            return;
        }

        foreach (var category in categories.EnumerateObject())
        {
            if (!category.Value.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                issues.Add(new JsonTweakValidationIssue(filePath, "missing-entries", $"Category '{category.Name}' must contain an array property named 'entries'."));
            }
        }
    }

    private ITweak? CreateTweakFromEntry(JsonTweakEntry entry, IRegistryAccessor registryAccessor)
    {
        try
        {
            var hive = ParseHive(entry.Path!);
            var subKey = GetSubKey(entry.Path!);
            var valueKind = ParseValueKind(entry.Type);
            var riskLevel = ParseRiskLevel(entry.CategoryRiskLevel);
            var targetValue = entry.RecommendedValue ?? entry.DefaultValue ?? 0;

            return new RegistryValueTweak(
                id: $"json.{entry.Id}",
                name: entry.Name ?? entry.Id!,
                description: entry.Description ?? "",
                risk: riskLevel,
                hive: hive,
                keyPath: subKey,
                valueName: entry.ValueName ?? "",
                valueKind: valueKind,
                targetValue: targetValue,
                registryAccessor: registryAccessor,
                requiresElevation: hive == RegistryHive.LocalMachine
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonTweakLoader] Failed to create {entry.Id}: {ex.Message}");
            return null;
        }
    }

    private static RegistryHive ParseHive(string path) =>
        path.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase) ? RegistryHive.LocalMachine :
        path.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase) ? RegistryHive.CurrentUser :
        RegistryHive.LocalMachine;

    private static string GetSubKey(string path)
    {
        var idx = path.IndexOf('\\');
        return idx >= 0 ? path[(idx + 1)..] : path;
    }

    private static RegistryValueKind ParseValueKind(string? type) =>
        type?.ToUpperInvariant() switch
        {
            "REG_DWORD" => RegistryValueKind.DWord,
            "REG_QWORD" => RegistryValueKind.QWord,
            "REG_SZ" => RegistryValueKind.String,
            "REG_EXPAND_SZ" => RegistryValueKind.ExpandString,
            "REG_MULTI_SZ" => RegistryValueKind.MultiString,
            "REG_BINARY" => RegistryValueKind.Binary,
            _ => RegistryValueKind.DWord
        };

    private static TweakRiskLevel ParseRiskLevel(string? level) =>
        level?.ToLowerInvariant() switch
        {
            "low" => TweakRiskLevel.Safe,
            "medium" => TweakRiskLevel.Advanced,
            "high" => TweakRiskLevel.Risky,
            _ => TweakRiskLevel.Advanced
        };

    public void Dispose() => _watcher?.Dispose();
}

#region JSON Models

internal sealed class JsonTweakDocument
{
    [JsonPropertyName("metadata")]
    public JsonTweakMetadata? Metadata { get; set; }

    [JsonPropertyName("categories")]
    public Dictionary<string, JsonTweakCategory>? Categories { get; set; }
}

internal sealed class JsonTweakMetadata
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

internal sealed class JsonTweakCategory
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("risk_level")]
    public string? RiskLevel { get; set; }

    [JsonPropertyName("requires_reboot")]
    public bool RequiresReboot { get; set; }

    [JsonPropertyName("entries")]
    public List<JsonTweakEntry>? Entries { get; set; }
}

internal sealed class JsonTweakEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("value_name")]
    public string? ValueName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("default_value")]
    public object? DefaultValue { get; set; }

    [JsonPropertyName("recommended_value")]
    public object? RecommendedValue { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("documentation")]
    public string? Documentation { get; set; }

    [JsonPropertyName("verified")]
    public bool? Verified { get; set; }

    [JsonPropertyName("safe")]
    public bool? Safe { get; set; }

    // Set by loader from parent category
    [JsonIgnore]
    public string? CategoryRiskLevel { get; set; }
}

public sealed record JsonTweakValidationIssue(
    string FilePath,
    string Code,
    string Message,
    string? EntryId = null);

internal sealed record JsonTweakLoadResult(
    IReadOnlyList<JsonTweakEntry> Entries,
    IReadOnlyList<JsonTweakValidationIssue> Issues);

#endregion
