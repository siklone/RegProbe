using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTraceProject.App.HardwareDb;

public sealed class HardwareDbUpdater
{
    private readonly HttpClient _httpClient;

    public HardwareDbUpdater(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<bool> TryUpdateJsonAsync(Uri remoteUri, string localPath, CancellationToken ct)
    {
        try
        {
            var payload = await _httpClient.GetByteArrayAsync(remoteUri, ct).ConfigureAwait(false);
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(localPath, payload, ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
