using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core.Commands;
using RegProbe.Infrastructure.Commands;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Infrastructure.Registry;
using RegProbe.Core.Registry;
using Xunit;
using CorePluginLoader = RegProbe.Core.Plugins.PluginLoader;
using InfrastructurePluginLoader = RegProbe.Infrastructure.Services.PluginLoader;

public sealed class PipeMessageSerializerTests
{
    [Fact]
    public async Task WriteAsync_RejectsPayloadsOverOneMegabyte()
    {
        var stream = new MemoryStream();
        var payload = new OversizedPipeMessage(new string('a', PipeMessageSerializer.MaxMessageBytes + 1));

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => PipeMessageSerializer.WriteAsync(stream, payload, CancellationToken.None));

        Assert.Contains("maximum size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_RejectsDeclaredPayloadsOverOneMegabyte()
    {
        var stream = new MemoryStream();
        var lengthBytes = BitConverter.GetBytes(PipeMessageSerializer.MaxMessageBytes + 1);
        await stream.WriteAsync(lengthBytes);
        stream.Position = 0;

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => PipeMessageSerializer.ReadAsync<OversizedPipeMessage>(stream, CancellationToken.None));

        Assert.Contains("maximum size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record OversizedPipeMessage(string Data);
}

public sealed class CommandAllowlistSecurityTests
{
    [Fact]
    public void RegQuery_IsAllowedForSupportedHives()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest("query", @"HKLM\SOFTWARE\RegProbe");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void RegMutation_IsRejectedWhenNotExplicitlyAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest("add", @"HKLM\SOFTWARE\RegProbe", "/v", "TestValue", "/t", "REG_DWORD", "/d", "1", "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.False(allowed);
        Assert.Contains("explicit allowlisting", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegMutation_IsAllowedInDeveloperMode()
    {
        var allowlist = CommandAllowlist.CreateDefault(allowUnsafeDeveloperCommands: true);
        var request = CreateRegRequest("add", @"HKLM\SOFTWARE\RegProbe", "/v", "TestValue", "/t", "REG_DWORD", "/d", "1", "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void ExplicitRegAllowlistStillPermitsKnownSafeMutation()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "/v", "DoNotUseWebResults", "/t", "REG_DWORD", "/d", "1", "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    private static CommandRequest CreateRegRequest(params string[] arguments)
    {
        return new CommandRequest(Path.Combine(Environment.SystemDirectory, "reg.exe"), arguments);
    }
}

public sealed class PluginLoaderSecurityTests : IDisposable
{
    private readonly string _pluginDirectory;

    public PluginLoaderSecurityTests()
    {
        _pluginDirectory = Path.Combine(Path.GetTempPath(), $"RegProbePluginSecurity_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_pluginDirectory);
        File.WriteAllText(Path.Combine(_pluginDirectory, "Example.dll"), "not a real dll");
    }

    [Fact]
    public async Task CorePluginLoader_DoesNotDiscoverPluginsWhileDynamicLoadingIsDisabled()
    {
        var loader = new CorePluginLoader(_pluginDirectory);

        var plugins = await loader.DiscoverPluginsAsync(CancellationToken.None);

        Assert.Empty(plugins);
    }

    [Fact]
    public void InfrastructurePluginLoader_DoesNotLoadPluginsWhileDynamicLoadingIsDisabled()
    {
        var loader = new InfrastructurePluginLoader();

        var plugins = loader.LoadPlugins(_pluginDirectory);

        Assert.Empty(plugins);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_pluginDirectory))
            {
                Directory.Delete(_pluginDirectory, true);
            }
        }
        catch
        {
        }
    }
}

public sealed class ElevatedHostSessionSecurityTests
{
    [Fact]
    public void SessionTokenAndPipeNameIncludeNonce()
    {
        var token = ElevatedHostDefaults.CreateSessionToken();
        var pipeName = ElevatedHostDefaults.GetPipeNameForProcess(4242, token);

        Assert.Matches("^[A-F0-9]{32}$", token);
        Assert.Contains(".4242.", pipeName, StringComparison.Ordinal);
        Assert.EndsWith(ElevatedHostSessionSecurity.BuildPipeNonceSuffix(token), pipeName, StringComparison.Ordinal);
    }

    [Fact]
    public void SessionTokenValidationRequiresExactMatch()
    {
        var token = ElevatedHostDefaults.CreateSessionToken();
        var last = token[^1];
        var altered = token[..^1] + (last == '0' ? "1" : "0");

        Assert.True(ElevatedHostSessionSecurity.IsSessionTokenAccepted(token, token));
        Assert.False(ElevatedHostSessionSecurity.IsSessionTokenAccepted(token, altered));
    }

    [Fact]
    public void ClientProcessValidationRejectsUnexpectedPid()
    {
        Assert.True(ElevatedHostSessionSecurity.IsClientProcessAccepted(1234, 1234));
        Assert.False(ElevatedHostSessionSecurity.IsClientProcessAccepted(1234, 9876));
    }
}

public sealed class RegistryOwnershipMutationGuardTests
{
    [Fact]
    public void Execute_RollsBackWhenGrantAccessFails()
    {
        var rollbackCalled = false;

        Assert.Throws<InvalidOperationException>(() =>
            RegistryOwnershipMutationGuard.Execute(
                applyOwnership: () => { },
                grantAccess: () => throw new InvalidOperationException("boom"),
                rollback: () => rollbackCalled = true));

        Assert.True(rollbackCalled);
    }
}

public sealed class ElevatedRegistryAccessorSecurityTests
{
    [Fact]
    public async Task SetValueAsync_FallsBackToRegExe_OnAccessDeniedHResult()
    {
        var client = new RecordingClient
        {
            RegistryResponseFactory = request => new ElevatedRegistryResponse(
                request.RequestId,
                false,
                "Access denied",
                null,
                unchecked((int)0x80070005)),
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
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 1),
            CancellationToken.None);

        Assert.Equal(2, client.Requests.Count);
        Assert.Equal(ElevatedHostRequestType.Command, client.Requests[1].RequestType);
    }

    private sealed class RecordingClient : IElevatedHostClient
    {
        public List<ElevatedHostRequest> Requests { get; } = new();
        public Func<ElevatedRegistryRequest, ElevatedRegistryResponse>? RegistryResponseFactory { get; set; }
        public Func<ElevatedCommandRequest, ElevatedCommandResponse>? CommandResponseFactory { get; set; }

        public Task<ElevatedHostResponse> SendAsync(ElevatedHostRequest request, CancellationToken ct)
        {
            Requests.Add(request);

            if (request.RequestType == ElevatedHostRequestType.Registry)
            {
                var registryRequest = request.RegistryRequest!;
                var response = RegistryResponseFactory!(registryRequest);
                return Task.FromResult(new ElevatedHostResponse(request.RequestId, request.RequestType, RegistryResponse: response));
            }

            if (request.RequestType == ElevatedHostRequestType.Command)
            {
                var commandRequest = request.CommandRequest!;
                var response = CommandResponseFactory!(commandRequest);
                return Task.FromResult(new ElevatedHostResponse(request.RequestId, request.RequestType, CommandResponse: response));
            }

            throw new InvalidOperationException("Unexpected request type.");
        }
    }
}
