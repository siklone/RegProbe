using System;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core.Files;

namespace RegProbe.Infrastructure.Elevation;

public sealed class ElevatedFileSystemAccessor : IFileSystemAccessor
{
    private readonly IElevatedHostClient _client;

    public ElevatedFileSystemAccessor(IElevatedHostClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<bool> FileExistsAsync(string path, CancellationToken ct)
    {
        var response = await SendAsync(new ElevatedFileRequest(
            Guid.NewGuid(),
            ElevatedFileOperation.Exists,
            path),
            ct);

        if (response.Exists is null)
        {
            throw new ElevatedHostException("Elevated host did not return file existence state.");
        }

        return response.Exists.Value;
    }

    public async Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct)
    {
        await SendAsync(new ElevatedFileRequest(
            Guid.NewGuid(),
            ElevatedFileOperation.Move,
            sourcePath,
            destinationPath),
            ct);
    }

    private async Task<ElevatedFileResponse> SendAsync(ElevatedFileRequest request, CancellationToken ct)
    {
        var hostRequest = new ElevatedHostRequest(
            request.RequestId,
            ElevatedHostRequestType.FileSystem,
            FileRequest: request);

        var hostResponse = await _client.SendAsync(hostRequest, ct);
        if (hostResponse.ResponseType != ElevatedHostRequestType.FileSystem || hostResponse.FileResponse is null)
        {
            throw new ElevatedHostException("Elevated host did not return a file system response.");
        }

        var response = hostResponse.FileResponse;
        if (response.RequestId != request.RequestId)
        {
            throw new ElevatedHostException("Elevated host response did not match the request.");
        }

        if (!response.Success)
        {
            var message = string.IsNullOrWhiteSpace(response.Error)
                ? "Elevated host reported a file system error."
                : response.Error;
            throw new ElevatedHostException(message);
        }

        return response;
    }
}
