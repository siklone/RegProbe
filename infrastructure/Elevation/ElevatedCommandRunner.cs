using System;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core.Commands;

namespace RegProbe.Infrastructure.Elevation;

public sealed class ElevatedCommandRunner : ICommandRunner
{
    private readonly IElevatedHostClient _client;

    public ElevatedCommandRunner(IElevatedHostClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken ct)
    {
        var requestId = Guid.NewGuid();
        var hostRequest = new ElevatedHostRequest(
            requestId,
            ElevatedHostRequestType.Command,
            CommandRequest: new ElevatedCommandRequest(requestId, ElevatedCommandOperation.Run, request));

        var response = await _client.SendAsync(hostRequest, ct);
        if (response.CommandResponse is null)
        {
            throw new InvalidOperationException("Elevated command response was missing.");
        }

        if (!response.CommandResponse.Success || response.CommandResponse.Result is null)
        {
            var error = response.CommandResponse.ErrorMessage ?? "Elevated command execution failed.";
            throw new InvalidOperationException(error);
        }

        return response.CommandResponse.Result;
    }
}
