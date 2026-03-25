using System.Threading;
using System.Threading.Tasks;

namespace OpenTraceProject.Infrastructure.Elevation;

public interface IElevatedHostClient
{
    Task<ElevatedHostResponse> SendAsync(ElevatedHostRequest request, CancellationToken ct);
}
