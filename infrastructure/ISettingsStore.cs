using System.Threading;
using System.Threading.Tasks;

namespace RegProbe.Infrastructure;

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken ct);
    Task SaveAsync(AppSettings settings, CancellationToken ct);
}
