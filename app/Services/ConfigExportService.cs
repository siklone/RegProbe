using System.Text.Json;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Application.Services;

/// <summary>
/// Service for exporting and importing application configuration.
/// Supports backup and restore of tweak state, DNS configuration, and minimal app settings.
/// </summary>
public class ConfigExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ConfigExportSnapshotBuilder _snapshotBuilder;
    private readonly ConfigImportExecutor _importExecutor;

    public ConfigExportService(
        ITweakCatalog? tweakCatalog = null,
        DnsService? dnsService = null,
        ISettingsStore? settingsStore = null)
    {
        var paths = AppPaths.FromEnvironment();
        var resolvedTweakCatalog = tweakCatalog ?? new TweakCatalogService();
        var resolvedDnsService = dnsService ?? new DnsService();
        var resolvedSettingsStore = settingsStore ?? new SettingsStore(paths);

        _snapshotBuilder = new ConfigExportSnapshotBuilder(
            resolvedTweakCatalog,
            resolvedDnsService,
            resolvedSettingsStore);
        _importExecutor = new ConfigImportExecutor(
            resolvedTweakCatalog,
            resolvedDnsService,
            resolvedSettingsStore);
    }

    public async Task<bool> ExportAsync(string filePath, ExportOptions options)
    {
        try
        {
            var config = await _snapshotBuilder.BuildAsync(options);
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

            return await _importExecutor.ExecuteAsync(config, dryRun);
        }
        catch (Exception ex)
        {
            return new ImportResult(false, $"Import failed: {ex.Message}");
        }
    }

    public Task<ImportResult> ValidateAsync(string filePath)
    {
        return ImportAsync(filePath, dryRun: true);
    }
}
