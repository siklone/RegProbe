using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core.Services;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed class ElevatedServiceManager : IServiceManager
{
    private readonly IElevatedHostClient _client;

    public ElevatedServiceManager(IElevatedHostClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<ServiceInfo> QueryAsync(string serviceName, CancellationToken ct)
    {
        var response = await SendAsync(new ElevatedServiceRequest(
            Guid.NewGuid(),
            ElevatedServiceOperation.Query,
            serviceName),
            ct);

        return response.Info ?? new ServiceInfo(false, ServiceStartMode.Unknown, ServiceStatus.Unknown);
    }

    public async Task SetStartModeAsync(string serviceName, ServiceStartMode startMode, CancellationToken ct)
    {
        await SendAsync(new ElevatedServiceRequest(
            Guid.NewGuid(),
            ElevatedServiceOperation.SetStartMode,
            serviceName,
            startMode),
            ct);
    }

    public async Task StartAsync(string serviceName, CancellationToken ct)
    {
        await SendAsync(new ElevatedServiceRequest(
            Guid.NewGuid(),
            ElevatedServiceOperation.Start,
            serviceName),
            ct);
    }

    public async Task StopAsync(string serviceName, CancellationToken ct)
    {
        await SendAsync(new ElevatedServiceRequest(
            Guid.NewGuid(),
            ElevatedServiceOperation.Stop,
            serviceName),
            ct);
    }

    public async Task<IReadOnlyList<string>> ListServiceNamesAsync(CancellationToken ct)
    {
        var response = await SendAsync(new ElevatedServiceRequest(
            Guid.NewGuid(),
            ElevatedServiceOperation.List,
            string.Empty),
            ct);

        return response.ServiceNames ?? Array.Empty<string>();
    }

    private async Task<ElevatedServiceResponse> SendAsync(ElevatedServiceRequest request, CancellationToken ct)
    {
        var hostRequest = new ElevatedHostRequest(
            request.RequestId,
            ElevatedHostRequestType.Service,
            ServiceRequest: request);

        var hostResponse = await _client.SendAsync(hostRequest, ct);
        if (hostResponse.ResponseType != ElevatedHostRequestType.Service || hostResponse.ServiceResponse is null)
        {
            throw new ElevatedHostException("Elevated host did not return a service response.");
        }

        var response = hostResponse.ServiceResponse;
        if (response.RequestId != request.RequestId)
        {
            throw new ElevatedHostException("Elevated host response did not match the request.");
        }

        if (!response.Success)
        {
            var message = string.IsNullOrWhiteSpace(response.Error)
                ? "Elevated host reported a service error."
                : response.Error;
            throw new ElevatedHostException(message);
        }

        return response;
    }
}
