using RegProbe.Application.Models;

namespace RegProbe.Application.Services;

/// <summary>
/// Service for managing DNS configuration using WMI.
/// </summary>
public class DnsService
{
    private readonly DnsConfigurationStore _configurationStore = new();
    private readonly DnsCacheFlusher _cacheFlusher = new();

    /// <summary>
    /// Gets predefined DNS providers.
    /// </summary>
    public static List<DnsProvider> GetProviders()
        => DnsProviderCatalog.GetProviders();

    /// <summary>
    /// Gets current DNS configuration for active network adapter.
    /// </summary>
    public async Task<DnsConfiguration?> GetCurrentDnsAsync()
        => await Task.Run(_configurationStore.GetCurrentDns);

    /// <summary>
    /// Sets DNS servers for all active network adapters.
    /// </summary>
    public async Task<bool> SetDnsAsync(DnsProvider provider)
        => await Task.Run(() => _configurationStore.SetDns(provider));

    /// <summary>
    /// Flushes DNS resolver cache.
    /// </summary>
    public async Task<bool> FlushDnsCacheAsync()
        => await Task.Run(_cacheFlusher.FlushDnsCache);

    /// <summary>
    /// Detects which provider is currently in use.
    /// </summary>
    public DnsProvider? DetectCurrentProvider(DnsConfiguration config)
    {
        if (config.IsDhcp)
        {
            return GetProviders().FirstOrDefault(p => p.Name == "Automatic");
        }

        return GetProviders().FirstOrDefault(p => p.PrimaryDns == config.PrimaryDns);
    }
}
