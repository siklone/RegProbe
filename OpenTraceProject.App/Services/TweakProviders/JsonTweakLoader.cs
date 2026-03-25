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
using OpenTraceProject.Core;
using OpenTraceProject.Core.Plugins;
using OpenTraceProject.Core.Registry;
using OpenTraceProject.Engine.Tweaks;

namespace OpenTraceProject.App.Services.TweakProviders;

/// <summary>
/// Loads tweak definitions from JSON files with hot-reload support.
/// Tweaks are instantiated on demand with proper registry accessor.
/// </summary>
public sealed class JsonTweakLoader : IDisposable
{
    private readonly string _jsonDirectory;
    private readonly ConcurrentDictionary<string, JsonTweakEntry> _definitions = new();
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
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _hotReloadEnabled = true;
    }
    
    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        await Task.Delay(200); // Debounce
        
        lock (_reloadLock)
        {
            try
            {
                _definitions.Clear();
                LoadAllDefinitions();
                DefinitionsReloaded?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hot-reload failed: {ex.Message}");
            }
        }
    }
    
    private void LoadAllDefinitions()
    {
        if (!Directory.Exists(_jsonDirectory))
            return;
            
        foreach (var jsonFile in Directory.GetFiles(_jsonDirectory, "*.json"))
        {
            try
            {
                LoadDefinitionsFromFile(jsonFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load {jsonFile}: {ex.Message}");
            }
        }
    }
    
    private void LoadDefinitionsFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var document = JsonSerializer.Deserialize<JsonTweakDocument>(json);
        
        if (document?.Categories == null)
            return;
            
        foreach (var (categoryKey, category) in document.Categories)
        {
            if (category.Entries == null)
                continue;
                
            foreach (var entry in category.Entries)
            {
                if (string.IsNullOrEmpty(entry.Id) || string.IsNullOrEmpty(entry.Path))
                    continue;
                    
                // Skip undocumented tweaks (documentation-first policy)
                if (string.IsNullOrEmpty(entry.Documentation) && entry.Verified != true)
                {
                    System.Diagnostics.Debug.WriteLine($"[JsonTweakLoader] Skipping undocumented: {entry.Id}");
                    continue;
                }
                
                entry.CategoryRiskLevel = category.RiskLevel;
                _definitions[entry.Id] = entry;
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

#endregion
