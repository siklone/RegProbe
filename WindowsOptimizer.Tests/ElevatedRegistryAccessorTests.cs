using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Infrastructure.Elevation;
using WindowsOptimizer.Core.Registry;
using Xunit;

public sealed class ElevatedRegistryAccessorTests
{
    [Fact]
    public async Task ReadValueAsync_ReturnsReadResult()
    {
        var client = new RecordingClient();
        var expected = new RegistryValueReadResult(
            true,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 1));
        client.RegistryResponseFactory = request => new ElevatedRegistryResponse(request.RequestId, true, null, expected);

        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            "Software\\WindowsOptimizer",
            "TestValue");

        var result = await accessor.ReadValueAsync(reference, CancellationToken.None);

        Assert.True(result.Exists);
        Assert.Equal(expected, result);
        Assert.Equal(ElevatedRegistryOperation.ReadValue, client.LastRequest!.RegistryRequest!.Operation);
    }

    [Fact]
    public async Task SetValueAsync_ThrowsOnError()
    {
        var client = new RecordingClient
        {
            RegistryResponseFactory = request => new ElevatedRegistryResponse(request.RequestId, false, "boom", null)
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            "Software\\WindowsOptimizer",
            "TestValue");

        await Assert.ThrowsAsync<ElevatedHostException>(() => accessor.SetValueAsync(
            reference,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 1),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteValueAsync_SendsDeleteRequest()
    {
        var client = new RecordingClient();
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            "Software\\WindowsOptimizer",
            "TestValue");

        await accessor.DeleteValueAsync(reference, CancellationToken.None);

        Assert.Equal(ElevatedRegistryOperation.DeleteValue, client.LastRequest!.RegistryRequest!.Operation);
    }

    private sealed class RecordingClient : IElevatedHostClient
    {
        public ElevatedHostRequest? LastRequest { get; private set; }
        public Func<ElevatedRegistryRequest, ElevatedRegistryResponse>? RegistryResponseFactory { get; set; }

        public Task<ElevatedHostResponse> SendAsync(ElevatedHostRequest request, CancellationToken ct)
        {
            LastRequest = request;
            var registryRequest = request.RegistryRequest
                ?? throw new InvalidOperationException("Registry request payload is required.");
            var response = RegistryResponseFactory?.Invoke(registryRequest)
                ?? new ElevatedRegistryResponse(request.RequestId, true, null, null);
            var hostResponse = new ElevatedHostResponse(request.RequestId, request.RequestType, RegistryResponse: response);
            return Task.FromResult(hostResponse);
        }
    }
}
