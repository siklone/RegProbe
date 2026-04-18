using RegProbe.Application.Models;

namespace RegProbe.Application.Services;

internal static class DnsProviderCatalog
{
    public static List<DnsProvider> GetProviders()
    {
        return new List<DnsProvider>
        {
            new(
                Name: "Cloudflare",
                Description: "Fast and privacy-focused DNS (1.1.1.1)",
                PrimaryDns: "1.1.1.1",
                SecondaryDns: "1.0.0.1",
                Icon: "CF"
            ),
            new(
                Name: "Google",
                Description: "Reliable and fast DNS (8.8.8.8)",
                PrimaryDns: "8.8.8.8",
                SecondaryDns: "8.8.4.4",
                Icon: "GO"
            ),
            new(
                Name: "Quad9",
                Description: "Security-focused DNS with malware blocking (9.9.9.9)",
                PrimaryDns: "9.9.9.9",
                SecondaryDns: "149.112.112.112",
                Icon: "Q9"
            ),
            new(
                Name: "OpenDNS",
                Description: "Family-safe DNS with content filtering (208.67.222.222)",
                PrimaryDns: "208.67.222.222",
                SecondaryDns: "208.67.220.220",
                Icon: "OD"
            ),
            new(
                Name: "Automatic",
                Description: "Use DNS from DHCP (router default)",
                PrimaryDns: "",
                SecondaryDns: "",
                Icon: "DH"
            )
        };
    }
}
