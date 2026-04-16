using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Application.Services;

internal sealed class ConfigExportSnapshotBuilder
{
    private readonly ITweakCatalog _tweakCatalog;
    private readonly DnsService _dnsService;
    private readonly ISettingsStore _settingsStore;

    public ConfigExportSnapshotBuilder(
        ITweakCatalog tweakCatalog,
        DnsService dnsService,
        ISettingsStore settingsStore)
    {
        _tweakCatalog = tweakCatalog;
        _dnsService = dnsService;
        _settingsStore = settingsStore;
    }

    public async Task<ExportedConfig> BuildAsync(ExportOptions options)
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

        return config;
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
}
