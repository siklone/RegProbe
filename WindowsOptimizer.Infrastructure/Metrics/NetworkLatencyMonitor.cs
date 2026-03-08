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
    private static readonly IPAddress CloudflareDns = IPAddress.Parse("1.1.1.1");
    private static readonly IPAddress GoogleDns = IPAddress.Parse("8.8.8.8");
    private const int PingTimeoutMs = 1000;
    private const int PingGuardMs = 1200;

    public async Task<NetworkLatencySample?> SampleAsync(CancellationToken cancellationToken)
    {
        var target = GetDefaultGateway();
        var gatewayTarget = target?.ToString() ?? string.Empty;

        try
        {
            var gatewayTask = target == null
                ? Task.FromResult<double?>(null)
                : PingAsync(target, cancellationToken);
            var cloudflareTask = PingAsync(CloudflareDns, cancellationToken);
            var googleTask = PingAsync(GoogleDns, cancellationToken);

            await Task.WhenAll(gatewayTask, cloudflareTask, googleTask);

            return new NetworkLatencySample(
                gatewayTarget,
                gatewayTask.Result,
                cloudflareTask.Result,
                googleTask.Result);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
    }

    private static async Task<double?> PingAsync(IPAddress target, CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var pingTask = ping.SendPingAsync(target, PingTimeoutMs);
            var completed = await Task.WhenAny(pingTask, Task.Delay(PingGuardMs, cancellationToken));

            if (completed != pingTask)
            {
                return null;
            }

            var reply = await pingTask;
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
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

public readonly record struct NetworkLatencySample(
    string GatewayTarget,
    double? GatewayMs,
    double? CloudflareMs,
    double? GoogleMs);
