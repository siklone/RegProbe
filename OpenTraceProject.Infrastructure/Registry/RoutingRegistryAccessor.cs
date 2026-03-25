using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using OpenTraceProject.Core.Registry;

namespace OpenTraceProject.Infrastructure.Registry;

/// <summary>
/// Routes reads and writes to the most appropriate accessor for the target hive.
/// HKCU writes prefer the local accessor so they stay in the interactive user's hive,
/// but can fall back to the elevated accessor when policy ACLs block the unelevated token.
/// HKLM and other machine hives always use the elevated accessor for mutations.
/// </summary>
public sealed class RoutingRegistryAccessor : IRegistryAccessor
{
    private const string PoliciesPrefix = @"Software\Policies\";
    private const string LegacyPoliciesPrefix = @"Software\Microsoft\Windows\CurrentVersion\Policies\";

    private readonly IRegistryAccessor _localAccessor;
    private readonly IRegistryAccessor _elevatedAccessor;

    public RoutingRegistryAccessor(IRegistryAccessor localAccessor, IRegistryAccessor elevatedAccessor)
    {
        _localAccessor = localAccessor ?? throw new ArgumentNullException(nameof(localAccessor));
        _elevatedAccessor = elevatedAccessor ?? throw new ArgumentNullException(nameof(elevatedAccessor));
    }

    public async Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        try
        {
            return await _localAccessor.ReadValueAsync(reference, ct);
        }
        catch (UnauthorizedAccessException) when (reference.Hive == RegistryHive.CurrentUser)
        {
            return await _elevatedAccessor.ReadValueAsync(reference, ct);
        }
    }

    public async Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
    {
        if (reference.Hive != RegistryHive.CurrentUser)
        {
            await _elevatedAccessor.SetValueAsync(reference, value, ct);
            return;
        }

        try
        {
            await _localAccessor.SetValueAsync(reference, value, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await _elevatedAccessor.SetValueAsync(reference, value, ct);
        }
    }

    public async Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        if (reference.Hive != RegistryHive.CurrentUser)
        {
            await _elevatedAccessor.DeleteValueAsync(reference, ct);
            return;
        }

        try
        {
            await _localAccessor.DeleteValueAsync(reference, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await _elevatedAccessor.DeleteValueAsync(reference, ct);
        }
    }
}
