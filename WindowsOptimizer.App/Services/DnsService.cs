using System.Diagnostics;
using System.Management;
using WindowsOptimizer.App.Models;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for managing DNS configuration using WMI.
/// </summary>
public class DnsService
{
    private const string BackupFileName = "dns_backup.json";
    
    /// <summary>
    /// Gets predefined DNS providers.
    /// </summary>
    public static List<DnsProvider> GetProviders()
    {
        return new List<DnsProvider>
        {
            new DnsProvider(
                Name: "Cloudflare",
                Description: "Fast and privacy-focused DNS (1.1.1.1)",
                PrimaryDns: "1.1.1.1",
                SecondaryDns: "1.0.0.1",
                Icon: "☁️"
            ),
            new DnsProvider(
                Name: "Google",
                Description: "Reliable and fast DNS (8.8.8.8)",
                PrimaryDns: "8.8.8.8",
                SecondaryDns: "8.8.4.4",
                Icon: "🔍"
            ),
            new DnsProvider(
                Name: "Quad9",
                Description: "Security-focused DNS with malware blocking (9.9.9.9)",
                PrimaryDns: "9.9.9.9",
                SecondaryDns: "149.112.112.112",
                Icon: "🛡️"
            ),
            new DnsProvider(
                Name: "OpenDNS",
                Description: "Family-safe DNS with content filtering (208.67.222.222)",
                PrimaryDns: "208.67.222.222",
                SecondaryDns: "208.67.220.220",
                Icon: "👨‍👩‍👧‍👦"
            ),
            new DnsProvider(
                Name: "Automatic",
                Description: "Use DNS from DHCP (router default)",
                PrimaryDns: "",
                SecondaryDns: "",
                Icon: "🔄"
            )
        };
    }

    /// <summary>
    /// Gets current DNS configuration for active network adapter.
    /// </summary>
    public async Task<DnsConfiguration?> GetCurrentDnsAsync()
    {
        return await Task.Run(() =>
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
            catch (Exception)
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Sets DNS servers for all active network adapters.
    /// </summary>
    public async Task<bool> SetDnsAsync(DnsProvider provider)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");

                foreach (ManagementObject adapter in searcher.Get())
                {
                    // If provider is "Automatic", set to DHCP
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
            catch (Exception)
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Flushes DNS resolver cache.
    /// </summary>
    public async Task<bool> FlushDnsCacheAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit();

                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Detects which provider is currently in use.
    /// </summary>
    public DnsProvider? DetectCurrentProvider(DnsConfiguration config)
    {
        if (config.IsDhcp)
        {
            return GetProviders().FirstOrDefault(p => p.Name == "Automatic");
        }

        return GetProviders().FirstOrDefault(p => 
            p.PrimaryDns == config.PrimaryDns);
    }
}
