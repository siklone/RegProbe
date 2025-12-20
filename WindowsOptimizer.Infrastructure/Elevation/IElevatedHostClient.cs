using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Elevation;

public interface IElevatedHostClient
{
    Task<ElevatedRegistryResponse> SendAsync(ElevatedRegistryRequest request, CancellationToken ct);
}
