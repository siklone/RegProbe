using RegProbe.Application.Models;

namespace RegProbe.Application.Services;

public class DnsService
{
    private readonly DnsConfigurationStore _configurationStore = new();
    private readonly DnsCacheFlusher _cacheFlusher = new();

    public static List<DnsProvider> GetProviders()
        => DnsProviderCatalog.GetProviders();

    public async Task<DnsConfiguration?> GetCurrentDnsAsync()
        => await Task.Run(_configurationStore.GetCurrentDns);

    public async Task<bool> SetDnsAsync(DnsProvider provider)
        => await Task.Run(() => _configurationStore.SetDns(provider));

    public async Task<bool> FlushDnsCacheAsync()
        => await Task.Run(_cacheFlusher.FlushDnsCache);

    public DnsProvider? DetectCurrentProvider(DnsConfiguration config)
    {
        if (config.IsDhcp)
        {
            return GetProviders().FirstOrDefault(p => p.Name == "Automatic");
        }

        return GetProviders().FirstOrDefault(p => p.PrimaryDns == config.PrimaryDns);
    }
}
