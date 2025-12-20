using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed class ElevatedRegistryAccessor : IRegistryAccessor
{
    private readonly IElevatedHostClient _client;

    public ElevatedRegistryAccessor(IElevatedHostClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        var request = new ElevatedRegistryRequest(
            Guid.NewGuid(),
            ElevatedRegistryOperation.ReadValue,
            reference,
            null);

        var response = await _client.SendAsync(request, ct);
        EnsureResponse(request, response);

        if (response.ReadResult is null)
        {
            throw new ElevatedHostException("Elevated host did not return a read result.");
        }

        return response.ReadResult;
    }

    public async Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
    {
        var request = new ElevatedRegistryRequest(
            Guid.NewGuid(),
            ElevatedRegistryOperation.SetValue,
            reference,
            value);

        var response = await _client.SendAsync(request, ct);
        EnsureResponse(request, response);
    }

    public async Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        var request = new ElevatedRegistryRequest(
            Guid.NewGuid(),
            ElevatedRegistryOperation.DeleteValue,
            reference,
            null);

        var response = await _client.SendAsync(request, ct);
        EnsureResponse(request, response);
    }

    private static void EnsureResponse(ElevatedRegistryRequest request, ElevatedRegistryResponse response)
    {
        if (response.RequestId != request.RequestId)
        {
            throw new ElevatedHostException("Elevated host response did not match the request.");
        }

        if (!response.Success)
        {
            var message = string.IsNullOrWhiteSpace(response.Error)
                ? "Elevated host reported an error."
                : response.Error;
            throw new ElevatedHostException(message);
        }
    }
}
