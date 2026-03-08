using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for exporting and importing application configuration.
/// Supports backup/restore of applied tweaks, startup settings, and preferences.
/// </summary>
public class ConfigExportService
{
    private readonly ITweakCatalog _tweakCatalog;
    private readonly StartupService _startupService;
    private readonly DnsService _dnsService;
    private readonly ISettingsStore _settingsStore;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigExportService(
        ITweakCatalog? tweakCatalog = null,
        StartupService? startupService = null,
        DnsService? dnsService = null,
        ISettingsStore? settingsStore = null)
    {
        var paths = AppPaths.FromEnvironment();
        _tweakCatalog = tweakCatalog ?? new TweakCatalogService();
        _startupService = startupService ?? new StartupService();
        _dnsService = dnsService ?? new DnsService();
        _settingsStore = settingsStore ?? new SettingsStore(paths);
    }

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
                config.AppliedTweakIds = await GetAppliedTweaksAsync();
            }

            if (options.IncludeStartupItems)
            {
                config.DisabledStartupItems = await GetDisabledStartupItemsAsync();
            }

            if (options.IncludeDnsSettings)
            {
                config.DnsProvider = await GetDnsProviderNameAsync();
            }

            if (options.IncludeAppSettings)
            {
                config.Settings = await GetAppSettingsAsync();
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

            if (dryRun)
            {
                return result;
            }

            var failedTweaks = await ApplyTweaksAsync(config.AppliedTweakIds);
            var failedStartup = await ApplyStartupItemsAsync(config.DisabledStartupItems);
            var dnsApplied = await ApplyDnsAsync(config.DnsProvider);
            var settingsApplied = await ApplySettingsAsync(config.Settings);

            var failures = failedTweaks.Count + failedStartup.Count;
            if (!dnsApplied && config.DnsProvider != null)
            {
                failures += 1;
            }

            if (!settingsApplied && config.Settings != null)
            {
                failures += 1;
            }

            var message = failures == 0
                ? "Import successful"
                : $"Import completed with {failures} failure(s).";

            return new ImportResult(failures == 0, message)
            {
                TweaksToApply = result.TweaksToApply,
                StartupItemsToRestore = result.StartupItemsToRestore,
                DnsToSet = result.DnsToSet,
                SettingsToApply = result.SettingsToApply
            };
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

    private async Task<List<string>> GetAppliedTweaksAsync()
    {
        var applied = new List<string>();

        foreach (var entry in _tweakCatalog.GetAll())
        {
            try
            {
                var step = await _tweakCatalog.ExecuteStepAsync(entry.Tweak, TweakAction.Detect);
                if (step.Result.Status == TweakStatus.Applied)
                {
                    applied.Add(entry.Tweak.Id);
                }
            }
            catch
            {
                // Skip tweaks that fail to detect.
            }
        }

        return applied;
    }

    private async Task<List<string>> GetDisabledStartupItemsAsync()
    {
        var items = await _startupService.GetAllStartupItemsAsync();
        return items.Where(item => !item.IsEnabled).Select(item => item.Id).ToList();
    }

    private async Task<string?> GetDnsProviderNameAsync()
    {
        var config = await _dnsService.GetCurrentDnsAsync();
        if (config == null)
        {
            return null;
        }

        var provider = _dnsService.DetectCurrentProvider(config);
        return provider?.Name;
    }

    private async Task<Dictionary<string, object>> GetAppSettingsAsync()
    {
        var settings = await _settingsStore.LoadAsync(CancellationToken.None);
        return new Dictionary<string, object>
        {
            ["Theme"] = settings.Theme,
            ["EnableCardShadows"] = settings.EnableCardShadows,
            ["RunStartupScanOnLaunch"] = settings.RunStartupScanOnLaunch,
            ["ShowPreviewHint"] = settings.ShowPreviewHint,
            ["DiscordWebhookUrl"] = settings.DiscordWebhookUrl ?? string.Empty,
            ["DiscordNotificationsEnabled"] = settings.DiscordNotificationsEnabled,
            ["DiscordAutoPatchEnabled"] = settings.DiscordAutoPatchEnabled,
            ["IsCompactMode"] = settings.IsCompactMode
        };
    }

    private async Task<List<string>> ApplyTweaksAsync(List<string>? tweakIds)
    {
        var failed = new List<string>();
        if (tweakIds == null || tweakIds.Count == 0)
        {
            return failed;
        }

        var options = new TweakExecutionOptions
        {
            DryRun = false,
            VerifyAfterApply = true,
            RollbackOnFailure = true
        };

        foreach (var tweakId in tweakIds)
        {
            var tweak = _tweakCatalog.FindById(tweakId);
            if (tweak == null)
            {
                failed.Add(tweakId);
                continue;
            }

            try
            {
                var report = await _tweakCatalog.ExecuteAsync(tweak, options);
                if (!report.Succeeded || !report.Applied)
                {
                    failed.Add(tweakId);
                }
            }
            catch
            {
                failed.Add(tweakId);
            }
        }

        return failed;
    }

    private async Task<List<string>> ApplyStartupItemsAsync(List<string>? disabledItems)
    {
        var failed = new List<string>();
        if (disabledItems == null || disabledItems.Count == 0)
        {
            return failed;
        }

        var items = await _startupService.GetAllStartupItemsAsync();
        var byId = items.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var id in disabledItems)
        {
            if (!byId.TryGetValue(id, out var item))
            {
                item = items.FirstOrDefault(i => string.Equals(i.Name, id, StringComparison.OrdinalIgnoreCase));
            }

            if (item == null)
            {
                failed.Add(id);
                continue;
            }

            if (!item.IsEnabled)
            {
                continue;
            }

            var success = await _startupService.DisableStartupItemAsync(item);
            if (!success)
            {
                failed.Add(id);
            }
        }

        return failed;
    }

    private async Task<bool> ApplyDnsAsync(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return true;
        }

        var provider = DnsService.GetProviders()
            .FirstOrDefault(p => string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            return false;
        }

        return await _dnsService.SetDnsAsync(provider);
    }

    private async Task<bool> ApplySettingsAsync(Dictionary<string, object>? settings)
    {
        if (settings == null || settings.Count == 0)
        {
            return true;
        }

        var current = await _settingsStore.LoadAsync(CancellationToken.None);

        if (TryReadString(settings, "Theme", out var theme) && !string.IsNullOrWhiteSpace(theme))
        {
            current.Theme = theme;
        }

        if (TryReadBool(settings, "EnableCardShadows", out var enableShadows))
        {
            current.EnableCardShadows = enableShadows;
        }

        if (TryReadBool(settings, "RunStartupScanOnLaunch", out var runStartupScan))
        {
            current.RunStartupScanOnLaunch = runStartupScan;
        }

        if (TryReadBool(settings, "ShowPreviewHint", out var showPreviewHint))
        {
            current.ShowPreviewHint = showPreviewHint;
        }

        if (TryReadString(settings, "DiscordWebhookUrl", out var webhook))
        {
            current.DiscordWebhookUrl = webhook;
        }

        if (TryReadBool(settings, "DiscordNotificationsEnabled", out var notifyEnabled))
        {
            current.DiscordNotificationsEnabled = notifyEnabled;
        }

        if (TryReadBool(settings, "DiscordAutoPatchEnabled", out var autoPatch))
        {
            current.DiscordAutoPatchEnabled = autoPatch;
        }

        if (TryReadBool(settings, "IsCompactMode", out var compactMode))
        {
            current.IsCompactMode = compactMode;
        }

        await _settingsStore.SaveAsync(current, CancellationToken.None);
        return true;
    }

    private static bool TryReadString(Dictionary<string, object> settings, string key, out string? value)
    {
        value = null;
        if (!settings.TryGetValue(key, out var raw) || raw == null)
        {
            return false;
        }

        if (raw is string str)
        {
            value = str;
            return true;
        }

        if (raw is JsonElement element && element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString();
            return true;
        }

        return false;
    }

    private static bool TryReadBool(Dictionary<string, object> settings, string key, out bool value)
    {
        value = false;
        if (!settings.TryGetValue(key, out var raw) || raw == null)
        {
            return false;
        }

        if (raw is bool flag)
        {
            value = flag;
            return true;
        }

        if (raw is JsonElement element &&
            (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
        {
            value = element.GetBoolean();
            return true;
        }

        return false;
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
