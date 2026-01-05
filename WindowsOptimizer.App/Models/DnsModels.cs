namespace WindowsOptimizer.App.Models;

/// <summary>
/// Represents a DNS provider configuration.
/// </summary>
public record DnsProvider(
    string Name,
    string Description,
    string PrimaryDns,
    string SecondaryDns,
    string Icon
);

/// <summary>
/// Current DNS configuration for a network adapter.
/// </summary>
public record DnsConfiguration(
    string AdapterName,
    string PrimaryDns,
    string SecondaryDns,
    bool IsDhcp
);
