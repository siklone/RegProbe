using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core.Commands;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Core.Registry;
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
            "Software\\RegProbe",
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
            "Software\\RegProbe",
            "TestValue");

        await Assert.ThrowsAsync<ElevatedHostException>(() => accessor.SetValueAsync(
            reference,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 1),
            CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ForCurrentUserPolicy_UsesRegExeFirst()
    {
        var client = new RecordingClient
        {
            CommandResponseFactory = request => new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero))
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            "Software\\Policies\\Microsoft\\Windows\\Explorer",
            "TestValue");

        await accessor.SetValueAsync(
            reference,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 1),
            CancellationToken.None);

        Assert.Single(client.Requests);
        Assert.Equal(ElevatedHostRequestType.Command, client.Requests[0].RequestType);
        Assert.Equal("add", client.Requests[0].CommandRequest!.Command.Arguments.First());
    }

    [Fact]
    public async Task DeleteValueAsync_SendsDeleteRequest()
    {
        var client = new RecordingClient();
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            "Software\\RegProbe",
            "TestValue");

        await accessor.DeleteValueAsync(reference, CancellationToken.None);

        Assert.Equal(ElevatedRegistryOperation.DeleteValue, client.LastRequest!.RegistryRequest!.Operation);
    }

    [Fact]
    public async Task DeleteValueAsync_ForCurrentUserPolicy_UsesRegExeFirst()
    {
        var client = new RecordingClient
        {
            CommandResponseFactory = request => new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero))
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            "Software\\Policies\\Microsoft\\Windows\\Explorer",
            "TestValue");

        await accessor.DeleteValueAsync(reference, CancellationToken.None);

        Assert.Single(client.Requests);
        Assert.Equal(ElevatedHostRequestType.Command, client.Requests[0].RequestType);
        Assert.Equal("delete", client.Requests[0].CommandRequest!.Command.Arguments.First());
    }

    [Fact]
    public async Task SetValueAsync_FallsBackTo_RegExe_OnUnauthorizedAccessException()
    {
        var client = new RecordingClient
        {
            RegistryResponseFactory = _ => throw new UnauthorizedAccessException("Access to the path is denied."),
            CommandResponseFactory = request => new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero))
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            @"Software\RegProbe",
            "TestValue");

        await accessor.SetValueAsync(
            reference,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 8),
            CancellationToken.None);

        Assert.Equal(2, client.Requests.Count);
        Assert.Equal(ElevatedHostRequestType.Command, client.Requests[1].RequestType);
        Assert.Equal("add", client.Requests[1].CommandRequest!.Command.Arguments.First());
    }

    [Fact]
    public async Task DeleteValueAsync_FallsBackTo_RegExe_OnWin32AccessDenied()
    {
        var client = new RecordingClient
        {
            RegistryResponseFactory = _ => throw new Win32Exception(1314, "Not all privileges or groups referenced are assigned to the caller."),
            CommandResponseFactory = request => new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero))
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            @"Software\RegProbe",
            "TestValue");

        await accessor.DeleteValueAsync(reference, CancellationToken.None);

        Assert.Equal(2, client.Requests.Count);
        Assert.Equal(ElevatedHostRequestType.Command, client.Requests[1].RequestType);
        Assert.Equal("delete", client.Requests[1].CommandRequest!.Command.Arguments.First());
    }

    [Fact]
    public async Task SetValueAsync_ForLocalMachine_UsesRegExeFirst()
    {
        var client = new RecordingClient
        {
            CommandResponseFactory = request => new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero))
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.LocalMachine,
            RegistryView.Default,
            @"SOFTWARE\Policies\Microsoft\Windows\Appx",
            "AllowAutomaticAppArchiving");

        await accessor.SetValueAsync(
            reference,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 0),
            CancellationToken.None);

        Assert.Single(client.Requests);
        Assert.Equal(ElevatedHostRequestType.Command, client.Requests[0].RequestType);
        Assert.Equal("add", client.Requests[0].CommandRequest!.Command.Arguments.First());
    }

    [Fact]
    public async Task SetValueAsync_ForNegativeDword_FormatsUnsignedDataForRegExe()
    {
        var client = new RecordingClient
        {
            CommandResponseFactory = request => new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero))
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.LocalMachine,
            RegistryView.Default,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
            "NetworkThrottlingIndex");

        await accessor.SetValueAsync(
            reference,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: -1),
            CancellationToken.None);

        var arguments = client.Requests[0].CommandRequest!.Command.Arguments;
        var dataIndex = arguments
            .Select((argument, index) => (argument, index))
            .FirstOrDefault(entry => string.Equals(entry.argument, "/d", StringComparison.OrdinalIgnoreCase))
            .index;
        Assert.True(dataIndex >= 0);
        Assert.Equal("4294967295", arguments[dataIndex + 1]);
    }

    private sealed class RecordingClient : IElevatedHostClient
    {
        public ElevatedHostRequest? LastRequest { get; private set; }
        public List<ElevatedHostRequest> Requests { get; } = new();
        public Func<ElevatedRegistryRequest, ElevatedRegistryResponse>? RegistryResponseFactory { get; set; }
        public Func<ElevatedCommandRequest, ElevatedCommandResponse>? CommandResponseFactory { get; set; }

        public Task<ElevatedHostResponse> SendAsync(ElevatedHostRequest request, CancellationToken ct)
        {
            LastRequest = request;
            Requests.Add(request);

            if (request.RequestType == ElevatedHostRequestType.Registry)
            {
                var registryRequest = request.RegistryRequest
                    ?? throw new InvalidOperationException("Registry request payload is required.");
                var response = RegistryResponseFactory?.Invoke(registryRequest)
                    ?? new ElevatedRegistryResponse(request.RequestId, true, null, null);
                var hostResponse = new ElevatedHostResponse(request.RequestId, request.RequestType, RegistryResponse: response);
                return Task.FromResult(hostResponse);
            }

            if (request.RequestType == ElevatedHostRequestType.Command)
            {
                var commandRequest = request.CommandRequest
                    ?? throw new InvalidOperationException("Command request payload is required.");
                var response = CommandResponseFactory?.Invoke(commandRequest)
                    ?? new ElevatedCommandResponse(
                        request.RequestId,
                        true,
                        null,
                        new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero));
                var hostResponse = new ElevatedHostResponse(request.RequestId, request.RequestType, CommandResponse: response);
                return Task.FromResult(hostResponse);
            }

            throw new InvalidOperationException($"Unexpected request type: {request.RequestType}.");
        }
    }
}
