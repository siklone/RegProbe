using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Plugins;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Developer;

namespace RegProbe.Application.Services.TweakProviders;

/// <summary>
/// Loads tweak definitions from JSON files with hot-reload support.
/// Tweaks are instantiated on demand with proper registry accessor.
/// </summary>
public sealed class JsonTweakLoader : IDisposable
{
    private readonly string _jsonDirectory;
    private readonly bool _preserveEntryIds;
    private readonly IServiceManager? _serviceManager;
    private readonly IScheduledTaskManager? _taskManager;
    private readonly ConcurrentDictionary<string, JsonTweakEntry> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _definitionSourceById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HashSet<string>> _definitionIdsByFile = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyList<JsonTweakValidationIssue>> _validationIssuesByFile = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;
    private readonly object _reloadLock = new();
    private bool _hotReloadEnabled;

    public event Action? DefinitionsReloaded;

    public JsonTweakLoader(
        string jsonDirectory,
        bool preserveEntryIds = false,
        IServiceManager? serviceManager = null,
        IScheduledTaskManager? taskManager = null)
    {
        _jsonDirectory = jsonDirectory;
        _preserveEntryIds = preserveEntryIds;
        _serviceManager = serviceManager;
        _taskManager = taskManager;
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
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
        _watcher.EnableRaisingEvents = true;
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
        if (!File.Exists(filePath))
        {
            RemoveDefinitionsForFile(filePath);
            _validationIssuesByFile.TryRemove(filePath, out _);
            return;
        }

        var hasExistingDefinitions = _definitionIdsByFile.TryGetValue(filePath, out var existingIds)
            && existingIds.Count > 0;
        var result = ParseDefinitionsFromFileWithRetry(filePath);
        _validationIssuesByFile[filePath] = result.Issues;
        if (hasExistingDefinitions && HasRetryableReloadFailure(result))
        {
            return;
        }

        RemoveDefinitionsForFile(filePath);
        ApplyDefinitionsForFile(filePath, result);
    }

    private void ApplyDefinitionsForFile(string filePath, JsonTweakLoadResult result)
    {
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

    private JsonTweakLoadResult ParseDefinitionsFromFileWithRetry(string filePath)
    {
        JsonTweakLoadResult result = new([], []);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            result = ParseDefinitionsFromFile(filePath);
            if (!HasRetryableReloadFailure(result))
            {
                break;
            }

            Thread.Sleep(100);
        }

        return result;
    }

    private static bool HasRetryableReloadFailure(JsonTweakLoadResult result) =>
        result.Entries.Count == 0
        && result.Issues.Any(static issue =>
            issue.Code is "invalid-json" or "load-failed");

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

                if (string.IsNullOrEmpty(entry.Documentation) && entry.Verified != true)
                {
                    issues.Add(new JsonTweakValidationIssue(
                        filePath,
                        "documentation-required",
                        $"Entry '{entry.Id}' is missing documentation and is not marked verified.",
                        entry.Id));
                    continue;
                }

                if (!ValidateEntry(filePath, entry, issues))
                {
                    continue;
                }

                entry.CategoryRiskLevel = category.RiskLevel;
                entries.Add(entry);
            }
        }

        return new JsonTweakLoadResult(entries, issues);
    }

    private static bool ValidateEntry(string filePath, JsonTweakEntry entry, List<JsonTweakValidationIssue> issues)
    {
        var hasPresets = entry.Presets is { Count: > 0 };
        var hasBatchEntries = entry.BatchEntries is { Count: > 0 };
        var hasSubtreeDefinition = IsSubtreeDefinition(entry);
        var hasSingleValueDefinition = HasSingleValueDefinition(entry);

        if ((hasPresets && hasBatchEntries)
            || (hasPresets && hasSingleValueDefinition)
            || (hasBatchEntries && hasSingleValueDefinition)
            || (hasSubtreeDefinition && (hasPresets || hasBatchEntries || HasConcreteSingleValueDefinition(entry))))
        {
            issues.Add(new JsonTweakValidationIssue(
                filePath,
                "conflicting-entry-shape",
                $"Entry '{entry.Id}' must define exactly one of: single value fields, batch_entries, presets, or subtree registry shape.",
                entry.Id));
            return false;
        }

        if (hasSubtreeDefinition)
        {
            return ValidateSubtreeEntry(filePath, entry, issues);
        }

        if (hasPresets)
        {
            return ValidatePresetDefinitions(filePath, entry, issues);
        }

        if (hasBatchEntries)
        {
            return ValidateBatchEntries(filePath, entry.Id!, entry.BatchEntries!, issues);
        }

        return ValidateSingleValueEntry(filePath, entry, issues);
    }

    private static bool ValidateSingleValueEntry(string filePath, JsonTweakEntry entry, List<JsonTweakValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(entry.Path))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-path", $"Entry '{entry.Id}' is missing required field 'path'.", entry.Id));
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.ValueName))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-value-name", $"Entry '{entry.Id}' is missing required field 'value_name'.", entry.Id));
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry.Type))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-type", $"Entry '{entry.Id}' is missing required field 'type'.", entry.Id));
            return false;
        }

        return true;
    }

    private static bool ValidateSubtreeEntry(string filePath, JsonTweakEntry entry, List<JsonTweakValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(entry.Path))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-path", $"Entry '{entry.Id}' is missing required field 'path'.", entry.Id));
            return false;
        }

        if (!string.Equals(entry.Type, "REG_SUBTREE", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "invalid-subtree-type", $"Entry '{entry.Id}' must use type 'REG_SUBTREE' for subtree cards.", entry.Id));
            return false;
        }

        return true;
    }

    private static bool ValidateBatchEntries(
        string filePath,
        string entryId,
        IReadOnlyList<JsonRegistryValueDefinition> definitions,
        List<JsonTweakValidationIssue> issues)
    {
        if (definitions.Count == 0)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "empty-batch-entries", $"Entry '{entryId}' must define at least one batch entry.", entryId));
            return false;
        }

        for (var index = 0; index < definitions.Count; index++)
        {
            if (!ValidateRegistryValueDefinition(filePath, entryId, definitions[index], $"batch_entries[{index}]", issues))
            {
                return false;
            }
        }

        var hasServiceEntries = definitions.Any(IsServiceDefinition);
        var hasTaskEntries = definitions.Any(IsScheduledTaskDefinition);
        if (hasServiceEntries && definitions.Any(definition => !IsServiceDefinition(definition)))
        {
            issues.Add(new JsonTweakValidationIssue(
                filePath,
                "mixed-batch-entry-types",
                $"Entry '{entryId}' mixes service and non-service batch entries.",
                entryId));
            return false;
        }

        if (hasTaskEntries && definitions.Any(definition => !IsScheduledTaskDefinition(definition)))
        {
            issues.Add(new JsonTweakValidationIssue(
                filePath,
                "mixed-batch-entry-types",
                $"Entry '{entryId}' mixes scheduled-task and non-task batch entries.",
                entryId));
            return false;
        }

        return true;
    }

    private static bool ValidatePresetDefinitions(string filePath, JsonTweakEntry entry, List<JsonTweakValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(entry.DefaultPresetKey))
        {
            issues.Add(new JsonTweakValidationIssue(
                filePath,
                "missing-default-preset-key",
                $"Entry '{entry.Id}' must define 'default_preset_key' when presets are present.",
                entry.Id));
            return false;
        }

        var presetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in entry.Presets!)
        {
            if (preset is null)
            {
                issues.Add(new JsonTweakValidationIssue(filePath, "null-preset", $"Entry '{entry.Id}' contains a null preset definition.", entry.Id));
                return false;
            }

            if (string.IsNullOrWhiteSpace(preset.Key))
            {
                issues.Add(new JsonTweakValidationIssue(filePath, "missing-preset-key", $"Entry '{entry.Id}' contains a preset without a key.", entry.Id));
                return false;
            }

            if (string.IsNullOrWhiteSpace(preset.Label))
            {
                issues.Add(new JsonTweakValidationIssue(filePath, "missing-preset-label", $"Entry '{entry.Id}' contains preset '{preset.Key}' without a label.", entry.Id));
                return false;
            }

            if (!presetKeys.Add(preset.Key))
            {
                issues.Add(new JsonTweakValidationIssue(filePath, "duplicate-preset-key", $"Entry '{entry.Id}' contains duplicate preset key '{preset.Key}'.", entry.Id));
                return false;
            }

            if (preset.Entries is not { Count: > 0 })
            {
                issues.Add(new JsonTweakValidationIssue(filePath, "empty-preset-entries", $"Entry '{entry.Id}' preset '{preset.Key}' must define at least one entry.", entry.Id));
                return false;
            }

            for (var index = 0; index < preset.Entries.Count; index++)
            {
                if (!ValidateRegistryValueDefinition(filePath, entry.Id!, preset.Entries[index], $"preset '{preset.Key}' entry[{index}]", issues))
                {
                    return false;
                }
            }
        }

        if (!presetKeys.Contains(entry.DefaultPresetKey))
        {
            issues.Add(new JsonTweakValidationIssue(
                filePath,
                "unknown-default-preset-key",
                $"Entry '{entry.Id}' default preset '{entry.DefaultPresetKey}' was not found in presets.",
                entry.Id));
            return false;
        }

        return true;
    }

    private static bool ValidateRegistryValueDefinition(
        string filePath,
        string entryId,
        JsonRegistryValueDefinition definition,
        string location,
        List<JsonTweakValidationIssue> issues)
    {
        if (definition is null)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "null-registry-definition", $"Entry '{entryId}' contains a null registry definition in {location}.", entryId));
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.Path))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-path", $"Entry '{entryId}' {location} is missing required field 'path'.", entryId));
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.ValueName))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-value-name", $"Entry '{entryId}' {location} is missing required field 'value_name'.", entryId));
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.Type))
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-type", $"Entry '{entryId}' {location} is missing required field 'type'.", entryId));
            return false;
        }

        if (definition.TargetValue is null)
        {
            issues.Add(new JsonTweakValidationIssue(filePath, "missing-target-value", $"Entry '{entryId}' {location} is missing required field 'target_value'.", entryId));
            return false;
        }

        return true;
    }

    private static bool HasSingleValueDefinition(JsonTweakEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.Path)
        || !string.IsNullOrWhiteSpace(entry.ValueName)
        || !string.IsNullOrWhiteSpace(entry.Type)
        || entry.DefaultValue is not null
        || entry.RecommendedValue is not null;

    private static bool HasConcreteSingleValueDefinition(JsonTweakEntry entry) =>
        (!string.IsNullOrWhiteSpace(entry.Path)
        || !string.IsNullOrWhiteSpace(entry.ValueName)
        || !string.IsNullOrWhiteSpace(entry.Type))
        && !IsSubtreeDefinition(entry);

    private static bool IsSubtreeDefinition(JsonTweakEntry entry) =>
        string.Equals(entry.Type, "REG_SUBTREE", StringComparison.OrdinalIgnoreCase);

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
            var riskLevel = ParseRiskLevel(entry.CategoryRiskLevel);
            var tweakId = _preserveEntryIds ? entry.Id! : $"json.{entry.Id}";

            if (IsDockerDesktopWslBackendDefinition(entry))
            {
                return new EnableDockerWsl2BackendTweak(
                    name: entry.Name ?? entry.Id!,
                    description: entry.Description ?? string.Empty);
            }

            if (IsWsl2MemoryLimitDefinition(entry))
            {
                return new SetWsl2MemoryLimitTweak(
                    name: entry.Name ?? entry.Id!,
                    description: entry.Description ?? string.Empty);
            }

            if (entry.Presets is { Count: > 0 })
            {
                var presets = entry.Presets
                    .Select(CreatePresetOption)
                    .ToArray();

                return new RegistryValuePresetBatchTweak(
                    id: tweakId,
                    name: entry.Name ?? entry.Id!,
                    description: entry.Description ?? "",
                    risk: riskLevel,
                    presets: presets,
                    defaultPresetKey: entry.DefaultPresetKey ?? presets[0].Key,
                    registryAccessor: registryAccessor);
            }

            if (entry.BatchEntries is { Count: > 0 })
            {
                if (entry.BatchEntries.All(IsServiceDefinition))
                {
                    if (_serviceManager is null)
                    {
                        throw new InvalidOperationException("Service-backed JSON tweak loading requires a service manager.");
                    }

                    var serviceEntries = entry.BatchEntries
                        .Select(CreateServiceEntry)
                        .ToArray();

                    return new ServiceStartModeBatchTweak(
                        id: tweakId,
                        name: entry.Name ?? entry.Id!,
                        description: entry.Description ?? "",
                        risk: riskLevel,
                        entries: serviceEntries,
                        serviceManager: _serviceManager);
                }

                if (entry.BatchEntries.All(IsScheduledTaskDefinition))
                {
                    if (_taskManager is null)
                    {
                        throw new InvalidOperationException("Scheduled-task-backed JSON tweak loading requires a task manager.");
                    }

                    if (entry.BatchEntries.Any(definition => !IsDisabledTaskState(definition.TargetValue)))
                    {
                        throw new InvalidDataException("Scheduled task JSON surfaces currently support only disabled task targets.");
                    }

                    var taskPaths = entry.BatchEntries
                        .Select(definition => definition.Path!)
                        .ToArray();

                    return new ScheduledTaskBatchTweak(
                        id: tweakId,
                        name: entry.Name ?? entry.Id!,
                        description: entry.Description ?? "",
                        risk: riskLevel,
                        taskPaths: taskPaths,
                        taskManager: _taskManager);
                }

                var batchEntries = entry.BatchEntries
                    .Select(CreateBatchEntry)
                    .ToArray();

                return new RegistryValueBatchTweak(
                    id: tweakId,
                    name: entry.Name ?? entry.Id!,
                    description: entry.Description ?? "",
                    risk: riskLevel,
                    entries: batchEntries,
                    registryAccessor: registryAccessor);
            }

            if (IsSubtreeDefinition(entry))
            {
                var hive = ParseHive(entry.Path!);
                var subKey = GetSubKey(entry.Path!);
                return new RegistrySubtreeTweak(
                    id: tweakId,
                    name: entry.Name ?? entry.Id!,
                    description: entry.Description ?? string.Empty,
                    risk: riskLevel,
                    hive: hive,
                    keyPath: subKey,
                    subtreeLabel: entry.ValueName ?? "(subtree root)",
                    requiresElevation: false);
            }

            var singleValueEntry = new JsonRegistryValueDefinition
            {
                Path = entry.Path,
                ValueName = entry.ValueName,
                Type = entry.Type,
                TargetValue = entry.RecommendedValue ?? entry.DefaultValue ?? 0
            };

            if (IsServiceDefinition(entry.Type))
            {
                if (_serviceManager is null)
                {
                    throw new InvalidOperationException("Service-backed JSON tweak loading requires a service manager.");
                }

                var serviceEntry = CreateServiceEntry(singleValueEntry);

                return new ServiceStartModeBatchTweak(
                    id: tweakId,
                    name: entry.Name ?? entry.Id!,
                    description: entry.Description ?? "",
                    risk: riskLevel,
                    entries: new[] { serviceEntry },
                    serviceManager: _serviceManager);
            }

            if (IsScheduledTaskDefinition(entry.Type))
            {
                if (_taskManager is null)
                {
                    throw new InvalidOperationException("Scheduled-task-backed JSON tweak loading requires a task manager.");
                }

                var targetValue = entry.RecommendedValue ?? entry.DefaultValue ?? "Disabled";
                if (!IsDisabledTaskState(targetValue))
                {
                    throw new InvalidDataException("Scheduled task JSON surfaces currently support only disabled task targets.");
                }

                return new ScheduledTaskBatchTweak(
                    id: tweakId,
                    name: entry.Name ?? entry.Id!,
                    description: entry.Description ?? "",
                    risk: riskLevel,
                    taskPaths: new[] { entry.Path! },
                    taskManager: _taskManager);
            }
            var registryEntry = CreateBatchEntry(singleValueEntry);

            return new RegistryValueTweak(
                id: tweakId,
                name: entry.Name ?? entry.Id!,
                description: entry.Description ?? "",
                risk: riskLevel,
                hive: registryEntry.Hive,
                keyPath: registryEntry.KeyPath,
                valueName: registryEntry.ValueName,
                valueKind: registryEntry.Kind,
                targetValue: registryEntry.TargetValue,
                registryAccessor: registryAccessor,
                view: registryEntry.View,
                requiresElevation: registryEntry.Hive == RegistryHive.LocalMachine
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonTweakLoader] Failed to create {entry.Id}: {ex.Message}");
            return null;
        }
    }

    private static RegistryValuePresetBatchOption CreatePresetOption(JsonTweakPresetDefinition preset)
    {
        var entries = preset.Entries!
            .Select(CreateBatchEntry)
            .ToArray();

        return new RegistryValuePresetBatchOption(
            preset.Key!,
            preset.Label!,
            preset.Description ?? string.Empty,
            entries);
    }

    private static RegistryValueBatchEntry CreateBatchEntry(JsonRegistryValueDefinition entry)
    {
        var hive = ParseHive(entry.Path!);
        var subKey = GetSubKey(entry.Path!);
        var valueKind = ParseValueKind(entry.Type);
        var targetValue = NormalizeValue(valueKind, entry.TargetValue!);

        return new RegistryValueBatchEntry(
            hive,
            subKey,
            entry.ValueName ?? string.Empty,
            valueKind,
            targetValue);
    }

    private static ServiceStartModeEntry CreateServiceEntry(JsonRegistryValueDefinition singleValueEntry)
    {
        return new ServiceStartModeEntry(
            ServiceName: singleValueEntry.Path ?? string.Empty,
            TargetStartMode: ParseServiceStartMode(singleValueEntry.TargetValue));
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

    private static ServiceStartMode ParseServiceStartMode(object? value)
    {
        var raw = value switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            JsonElement element => element.ToString(),
            _ => value?.ToString()
        };

        if (Enum.TryParse<ServiceStartMode>(raw, ignoreCase: true, out var parsed)
            && parsed != ServiceStartMode.Unknown)
        {
            return parsed;
        }

        throw new InvalidDataException($"Unsupported service start mode '{raw}'.");
    }

    private static bool IsDisabledTaskState(object? value)
    {
        return value switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.String
                => string.Equals(element.GetString(), "Disabled", StringComparison.OrdinalIgnoreCase),
            JsonElement element when element.ValueKind == JsonValueKind.False => true,
            JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number)
                => number == 0,
            bool enabled => !enabled,
            string raw => string.Equals(raw, "Disabled", StringComparison.OrdinalIgnoreCase),
            int number => number == 0,
            long number => number == 0,
            _ => false
        };
    }

    private static bool IsServiceDefinition(JsonTweakEntry entry) => IsServiceDefinition(entry.Type);

    private static bool IsDockerDesktopWslBackendDefinition(JsonTweakEntry entry) =>
        string.Equals(entry.Id, "developer.docker-performance", StringComparison.OrdinalIgnoreCase)
        && string.Equals(entry.Type, "FILE_JSON_BOOLEAN", StringComparison.OrdinalIgnoreCase);

    private static bool IsWsl2MemoryLimitDefinition(JsonTweakEntry entry) =>
        string.Equals(entry.Id, "developer.wsl2-memory", StringComparison.OrdinalIgnoreCase)
        && string.Equals(entry.Type, "FILE_WSL2_MEMORY", StringComparison.OrdinalIgnoreCase);

    private static bool IsServiceDefinition(JsonRegistryValueDefinition definition) => IsServiceDefinition(definition.Type);

    private static bool IsServiceDefinition(string? type) =>
        string.Equals(type, "ServiceStartMode", StringComparison.OrdinalIgnoreCase);

    private static bool IsScheduledTaskDefinition(JsonTweakEntry entry) => IsScheduledTaskDefinition(entry.Type);

    private static bool IsScheduledTaskDefinition(JsonRegistryValueDefinition definition) => IsScheduledTaskDefinition(definition.Type);

    private static bool IsScheduledTaskDefinition(string? type) =>
        string.Equals(type, "TaskEnabledState", StringComparison.OrdinalIgnoreCase);

    private static TweakRiskLevel ParseRiskLevel(string? level) =>
        level?.ToLowerInvariant() switch
        {
            "low" => TweakRiskLevel.Safe,
            "medium" => TweakRiskLevel.Advanced,
            "high" => TweakRiskLevel.Risky,
            _ => TweakRiskLevel.Advanced
        };

    private static object NormalizeValue(RegistryValueKind kind, object value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return kind switch
        {
            RegistryValueKind.DWord or RegistryValueKind.QWord => NormalizeNumericValue(element),
            RegistryValueKind.String or RegistryValueKind.ExpandString => NormalizeStringValue(element),
            RegistryValueKind.MultiString => NormalizeMultiStringValue(element),
            RegistryValueKind.Binary => NormalizeBinaryValue(element),
            _ => value
        };
    }

    private static object NormalizeNumericValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String
            && long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        throw new InvalidDataException($"Could not normalize numeric registry value from JSON kind '{element.ValueKind}'.");
    }

    private static object NormalizeStringValue(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : element.ToString();
    }

    private static object NormalizeMultiStringValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(item => item.ToString()).ToArray();
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return new[] { element.GetString() ?? string.Empty };
        }

        throw new InvalidDataException($"Could not normalize multi-string registry value from JSON kind '{element.ValueKind}'.");
    }

    private static object NormalizeBinaryValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var bytes = new List<byte>();
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetByte(out var value))
                {
                    bytes.Add(value);
                    continue;
                }

                throw new InvalidDataException("Binary registry arrays must contain byte-sized numbers.");
            }

            return bytes.ToArray();
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return Convert.FromBase64String(element.GetString() ?? string.Empty);
        }

        throw new InvalidDataException($"Could not normalize binary registry value from JSON kind '{element.ValueKind}'.");
    }

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

    [JsonPropertyName("batch_entries")]
    public List<JsonRegistryValueDefinition>? BatchEntries { get; set; }

    [JsonPropertyName("presets")]
    public List<JsonTweakPresetDefinition>? Presets { get; set; }

    [JsonPropertyName("default_preset_key")]
    public string? DefaultPresetKey { get; set; }

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

internal sealed class JsonRegistryValueDefinition
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("value_name")]
    public string? ValueName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("target_value")]
    public object? TargetValue { get; set; }
}

internal sealed class JsonTweakPresetDefinition
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("entries")]
    public List<JsonRegistryValueDefinition>? Entries { get; set; }
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
