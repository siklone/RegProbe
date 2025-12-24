using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core.Tasks;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed class ElevatedScheduledTaskManager : IScheduledTaskManager
{
    private readonly IElevatedHostClient _client;

    public ElevatedScheduledTaskManager(IElevatedHostClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<ScheduledTaskInfo> QueryAsync(string taskPath, CancellationToken ct)
    {
        var response = await SendAsync(new ElevatedScheduledTaskRequest(
            Guid.NewGuid(),
            ElevatedScheduledTaskOperation.Query,
            taskPath),
            ct);

        return response.Info ?? new ScheduledTaskInfo(false, false);
    }

    public async Task SetEnabledAsync(string taskPath, bool enabled, CancellationToken ct)
    {
        await SendAsync(new ElevatedScheduledTaskRequest(
            Guid.NewGuid(),
            ElevatedScheduledTaskOperation.SetEnabled,
            taskPath,
            enabled),
            ct);
    }

    private async Task<ElevatedScheduledTaskResponse> SendAsync(ElevatedScheduledTaskRequest request, CancellationToken ct)
    {
        var hostRequest = new ElevatedHostRequest(
            request.RequestId,
            ElevatedHostRequestType.ScheduledTask,
            ScheduledTaskRequest: request);

        var hostResponse = await _client.SendAsync(hostRequest, ct);
        if (hostResponse.ResponseType != ElevatedHostRequestType.ScheduledTask || hostResponse.ScheduledTaskResponse is null)
        {
            throw new ElevatedHostException("Elevated host did not return a scheduled task response.");
        }

        var response = hostResponse.ScheduledTaskResponse;
        if (response.RequestId != request.RequestId)
        {
            throw new ElevatedHostException("Elevated host response did not match the request.");
        }

        if (!response.Success)
        {
            var message = string.IsNullOrWhiteSpace(response.Error)
                ? "Elevated host reported a scheduled task error."
                : response.Error;
            throw new ElevatedHostException(message);
        }

        return response;
    }
}
