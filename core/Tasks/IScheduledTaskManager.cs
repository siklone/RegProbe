using System.Threading;
using System.Threading.Tasks;

namespace OpenTraceProject.Core.Tasks;

public interface IScheduledTaskManager
{
    Task<ScheduledTaskInfo> QueryAsync(string taskPath, CancellationToken ct);
    Task SetEnabledAsync(string taskPath, bool enabled, CancellationToken ct);
}
