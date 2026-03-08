using WindowsOptimizer.App.Services;

public sealed class HardwarePreloadServiceTests
{
    [Fact]
    public void ChoosePrimaryNetworkAdapter_PrefersPhysicalGatewayBackedAdapter()
    {
        var adapters = new[]
        {
            new NetworkAdapterData
            {
                Name = "WireGuard Tunnel",
                Description = "WireGuard Tunnel",
                AdapterType = "Ethernet",
                Status = "Up",
                Ipv4 = "10.7.0.2",
                LinkSpeed = "10 Gbps"
            },
            new NetworkAdapterData
            {
                Name = "Ethernet",
                Description = "Intel(R) Ethernet Controller I225-V",
                AdapterType = "Ethernet",
                Status = "Connected",
                Ipv4 = "192.168.1.25",
                Gateway = "192.168.1.1",
                LinkSpeed = "2.5 Gbps"
            }
        };

        var primary = HardwarePreloadService.ChoosePrimaryNetworkAdapter(adapters);

        Assert.NotNull(primary);
        Assert.Equal("Ethernet", primary!.Name);
    }
}
