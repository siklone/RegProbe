using System.Threading;
using System.Threading.Tasks;

namespace RegProbe.Infrastructure.Elevation;

public interface IElevatedHostClient
{
    Task<ElevatedHostResponse> SendAsync(ElevatedHostRequest request, CancellationToken ct);
}
