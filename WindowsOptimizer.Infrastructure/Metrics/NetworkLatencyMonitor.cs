using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Metrics;

[SupportedOSPlatform("windows")]
public sealed class NetworkLatencyMonitor : IDisposable
{
    private readonly Ping _ping = new();

    public async Task<NetworkLatencySample?> SampleAsync(CancellationToken cancellationToken)
    {
        var target = GetDefaultGateway();
        if (target == null)
        {
            return null;
        }

        try
        {
            var pingTask = _ping.SendPingAsync(target, 1000);
            var completed = await Task.WhenAny(pingTask, Task.Delay(1200, cancellationToken));

            if (completed != pingTask)
            {
                return null;
            }

            var reply = await pingTask;
            if (reply.Status != IPStatus.Success)
            {
                return null;
            }

            return new NetworkLatencySample(reply.RoundtripTime, target.ToString());
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _ping.Dispose();
    }

    private static IPAddress? GetDefaultGateway()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                var ipProps = nic.GetIPProperties();
                var gateway = ipProps.GatewayAddresses
                    .Select(address => address.Address)
                    .FirstOrDefault(address =>
                        address != null &&
                        address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(address) &&
                        !address.Equals(IPAddress.Any));

                if (gateway != null)
                {
                    return gateway;
                }
            }
        }
        catch
        {
        }

        return null;
    }
}

public readonly record struct NetworkLatencySample(double LatencyMs, string Target);
