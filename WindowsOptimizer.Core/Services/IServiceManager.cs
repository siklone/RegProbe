using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Services;

public interface IServiceManager
{
    Task<ServiceInfo> QueryAsync(string serviceName, CancellationToken ct);
    Task SetStartModeAsync(string serviceName, ServiceStartMode startMode, CancellationToken ct);
    Task StartAsync(string serviceName, CancellationToken ct);
    Task StopAsync(string serviceName, CancellationToken ct);
    Task<IReadOnlyList<string>> ListServiceNamesAsync(CancellationToken ct);
}
