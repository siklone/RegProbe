using System.Text.Json;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Application.Services;

internal sealed class ConfigImportExecutor
{
    private readonly ITweakCatalog _tweakCatalog;
    private readonly DnsService _dnsService;
    private readonly ISettingsStore _settingsStore;

    public ConfigImportExecutor(
        ITweakCatalog tweakCatalog,
        DnsService dnsService,
        ISettingsStore settingsStore)
    {
        _tweakCatalog = tweakCatalog;
        _dnsService = dnsService;
        _settingsStore = settingsStore;
    }

    public async Task<ImportResult> ExecuteAsync(ExportedConfig config, bool dryRun)
    {
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
            var tweakKey = tweakId ?? string.Empty;
            var normalizedTweakId = tweakId?.Trim();
            var tweak = _tweakCatalog.FindById(normalizedTweakId ?? string.Empty);
            if (tweak == null)
            {
                failed.Add(tweakKey);
                continue;
            }

            try
            {
                var report = await _tweakCatalog.ExecuteAsync(tweak, options);
                if (!report.Succeeded || !report.Applied)
                {
                    failed.Add(tweakKey);
                }
            }
            catch
            {
                failed.Add(tweakKey);
            }
        }

        return failed;
    }

    private async Task<bool> ApplyDnsAsync(string? providerName)
    {
        var normalizedProviderName = providerName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProviderName))
        {
            return true;
        }

        var provider = DnsService.GetProviders()
            .FirstOrDefault(p => string.Equals(p.Name, normalizedProviderName, StringComparison.OrdinalIgnoreCase));

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
