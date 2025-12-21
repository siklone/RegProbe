using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Monitoring;

public interface ISensorProvider
{
    Task<MonitoringSnapshot> CaptureAsync(CancellationToken ct);
}
