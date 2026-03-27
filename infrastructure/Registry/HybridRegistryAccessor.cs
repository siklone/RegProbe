using System;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core.Registry;

namespace RegProbe.Infrastructure.Registry;

/// <summary>
/// Uses a local accessor for reads and an elevated accessor for writes/deletes.
/// This enables non-interactive detection while keeping Apply/Rollback elevated.
/// </summary>
public sealed class HybridRegistryAccessor : IRegistryAccessor
{
    private readonly IRegistryAccessor _readAccessor;
    private readonly IRegistryAccessor _writeAccessor;

    public HybridRegistryAccessor(IRegistryAccessor readAccessor, IRegistryAccessor writeAccessor)
    {
        _readAccessor = readAccessor ?? throw new ArgumentNullException(nameof(readAccessor));
        _writeAccessor = writeAccessor ?? throw new ArgumentNullException(nameof(writeAccessor));
    }

    public Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        return _readAccessor.ReadValueAsync(reference, ct);
    }

    public Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
    {
        return _writeAccessor.SetValueAsync(reference, value, ct);
    }

    public Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        return _writeAccessor.DeleteValueAsync(reference, ct);
    }
}
