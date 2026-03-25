using System.Threading;
using System.Threading.Tasks;

namespace OpenTraceProject.Core.Registry;

public interface IRegistryAccessor
{
    Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct);
    Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct);
    Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct);
}
