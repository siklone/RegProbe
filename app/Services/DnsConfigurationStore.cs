using System.Management;
using RegProbe.Application.Models;

namespace RegProbe.Application.Services;

internal sealed class DnsConfigurationStore
{
    public DnsConfiguration? GetCurrentDns()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");

            foreach (ManagementObject adapter in searcher.Get())
            {
                var adapterName = adapter["Description"]?.ToString() ?? "Unknown";
                var dnsServers = adapter["DNSServerSearchOrder"] as string[];

                if (dnsServers == null || dnsServers.Length == 0)
                {
                    return new DnsConfiguration(adapterName, "DHCP", "DHCP", true);
                }

                return new DnsConfiguration(
                    AdapterName: adapterName,
                    PrimaryDns: dnsServers.Length > 0 ? dnsServers[0] : "",
                    SecondaryDns: dnsServers.Length > 1 ? dnsServers[1] : "",
                    IsDhcp: false
                );
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public bool SetDns(DnsProvider provider)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");

            foreach (ManagementObject adapter in searcher.Get())
            {
                if (string.IsNullOrEmpty(provider.PrimaryDns))
                {
                    adapter.InvokeMethod("SetDNSServerSearchOrder", new object[] { null! });
                }
                else
                {
                    var dnsServers = new[] { provider.PrimaryDns, provider.SecondaryDns };
                    adapter.InvokeMethod("SetDNSServerSearchOrder", new object[] { dnsServers });
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
