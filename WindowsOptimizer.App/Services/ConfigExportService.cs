using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for exporting and importing application configuration.
/// Supports backup/restore of applied tweaks, startup settings, and preferences.
/// </summary>
public class ConfigExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Export current configuration to a file.
    /// </summary>
    public async Task<bool> ExportAsync(string filePath, ExportOptions options)
    {
        try
        {
            var config = new ExportedConfig
            {
                ExportDate = DateTime.UtcNow,
                AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                MachineName = Environment.MachineName,
                Options = options
            };

            if (options.IncludeTweakStates)
            {
                // TODO: Integrate with TweakService to get applied tweaks
                config.AppliedTweakIds = new List<string>();
            }

            if (options.IncludeStartupItems)
            {
                // TODO: Integrate with StartupService to get disabled items
                config.DisabledStartupItems = new List<string>();
            }

            if (options.IncludeDnsSettings)
            {
                // TODO: Integrate with DnsService to get current DNS
                config.DnsProvider = null;
            }

            if (options.IncludeAppSettings)
            {
                config.Settings = new Dictionary<string, object>
                {
                    ["Theme"] = "Nord",
                    ["CompactMode"] = false,
                    ["EnableCardShadows"] = true
                };
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Import configuration from a file.
    /// </summary>
    public async Task<ImportResult> ImportAsync(string filePath, bool dryRun = false)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<ExportedConfig>(json, JsonOptions);

            if (config == null)
                return new ImportResult(false, "Invalid configuration file");

            var result = new ImportResult(true, "Import successful")
            {
                TweaksToApply = config.AppliedTweakIds?.Count ?? 0,
                StartupItemsToRestore = config.DisabledStartupItems?.Count ?? 0,
                DnsToSet = config.DnsProvider != null,
                SettingsToApply = config.Settings?.Count ?? 0
            };

            if (!dryRun)
            {
                // TODO: Actually apply the configuration
                // await ApplyTweaksAsync(config.AppliedTweakIds);
                // await RestoreStartupItemsAsync(config.DisabledStartupItems);
                // await SetDnsAsync(config.DnsProvider);
                // await ApplySettingsAsync(config.Settings);
            }

            return result;
        }
        catch (Exception ex)
        {
            return new ImportResult(false, $"Import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate a configuration file without importing.
    /// </summary>
    public async Task<ImportResult> ValidateAsync(string filePath)
    {
        return await ImportAsync(filePath, dryRun: true);
    }
}

/// <summary>
/// Options for configuration export.
/// </summary>
public class ExportOptions
{
    public bool IncludeTweakStates { get; init; } = true;
    public bool IncludeStartupItems { get; init; } = true;
    public bool IncludeDnsSettings { get; init; } = true;
    public bool IncludeAppSettings { get; init; } = true;
}

/// <summary>
/// Exported configuration structure.
/// </summary>
public class ExportedConfig
{
    public DateTime ExportDate { get; init; }
    public string AppVersion { get; init; } = "";
    public string MachineName { get; init; } = "";
    public ExportOptions Options { get; init; } = new();
    public List<string>? AppliedTweakIds { get; set; }
    public List<string>? DisabledStartupItems { get; set; }
    public string? DnsProvider { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    public ImportResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }
    public int TweaksToApply { get; init; }
    public int StartupItemsToRestore { get; init; }
    public bool DnsToSet { get; init; }
    public int SettingsToApply { get; init; }
    
    public int TotalChanges => TweaksToApply + StartupItemsToRestore + (DnsToSet ? 1 : 0) + SettingsToApply;
}
