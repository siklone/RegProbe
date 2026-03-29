using System.IO;
using System.Text.Json;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

/// <summary>
/// Service for exporting and importing application configuration.
/// Supports backup and restore of tweak state, DNS configuration, and minimal app settings.
/// </summary>
public class ConfigExportService
{
    private readonly ITweakCatalog _tweakCatalog;
    private readonly DnsService _dnsService;
    private readonly ISettingsStore _settingsStore;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigExportService(
        ITweakCatalog? tweakCatalog = null,
        DnsService? dnsService = null,
        ISettingsStore? settingsStore = null)
    {
        var paths = AppPaths.FromEnvironment();
        _tweakCatalog = tweakCatalog ?? new TweakCatalogService();
        _dnsService = dnsService ?? new DnsService();
        _settingsStore = settingsStore ?? new SettingsStore(paths);
    }

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

    public async Task<ImportResult> ImportAsync(string filePath, bool dryRun = false)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<ExportedConfig>(json, JsonOptions);

            if (config == null)
            {
                return new ImportResult(false, "Invalid configuration file");
            }

            var result = new ImportResult(true, "Import successful")
            {
                TweaksToApply = config.AppliedTweakIds?.Count ?? 0,
                DnsToSet = config.DnsProvider != null,
                SettingsToApply = config.Settings?.Count ?? 0
            };

            if (dryRun)
            {
                return result;
            }

            var failedTweaks = await ApplyTweaksAsync(config.AppliedTweakIds);
            var dnsApplied = await ApplyDnsAsync(config.DnsProvider);
            var settingsApplied = await ApplySettingsAsync(config.Settings);

            var failures = failedTweaks.Count;
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
                DnsToSet = result.DnsToSet,
                SettingsToApply = result.SettingsToApply
            };
        }
        catch (Exception ex)
        {
            return new ImportResult(false, $"Import failed: {ex.Message}");
        }
    }

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
            }
        }

        return applied;
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
            ["Theme"] = settings.Theme
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

}

public class ExportOptions
{
    public bool IncludeTweakStates { get; init; } = true;
    public bool IncludeDnsSettings { get; init; } = true;
    public bool IncludeAppSettings { get; init; } = true;
}

public class ExportedConfig
{
    public DateTime ExportDate { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public ExportOptions Options { get; init; } = new();
    public List<string>? AppliedTweakIds { get; set; }
    public string? DnsProvider { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}

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
    public bool DnsToSet { get; init; }
    public int SettingsToApply { get; init; }

    public int TotalChanges => TweaksToApply + (DnsToSet ? 1 : 0) + SettingsToApply;
}
