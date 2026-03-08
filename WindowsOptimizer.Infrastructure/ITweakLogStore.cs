using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure;

public interface ITweakLogStore
{
    Task AppendAsync(TweakLogEntry entry, CancellationToken ct);
    Task ExportCsvAsync(string destinationPath, CancellationToken ct);
    Task<IReadOnlyList<TweakLogEntry>> GetRecentHistoryAsync(int count, CancellationToken ct);
}
