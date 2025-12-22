using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WindowsOptimizer.Infrastructure.Registry;

public sealed class LocalRegistryAccessor : IRegistryAccessor
{
    public Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var baseKey = RegistryKey.OpenBaseKey(reference.Hive, reference.View);
        using var key = baseKey.OpenSubKey(reference.KeyPath, false);
        if (key is null)
        {
            return Task.FromResult(new RegistryValueReadResult(false, null));
        }

        var value = key.GetValue(reference.ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is null)
        {
            return Task.FromResult(new RegistryValueReadResult(false, null));
        }

        var kind = key.GetValueKind(reference.ValueName);
        var data = RegistryValueData.FromObject(kind, value);
        return Task.FromResult(new RegistryValueReadResult(true, data));
    }

    public async Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            using var key = OpenOrCreateKey(reference);
            key.SetValue(reference.ValueName, value.ToObject(), value.Kind);
        }
        catch (UnauthorizedAccessException)
        {
            // If access denied, try to take ownership and retry
            using (new RegistryOwnershipScope(reference.Hive, reference.View, reference.KeyPath))
            {
                using var key = OpenOrCreateKey(reference);
                key.SetValue(reference.ValueName, value.ToObject(), value.Kind);
            }
        }
    }

    public async Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            await ExecuteDeleteAsync(reference);
        }
        catch (UnauthorizedAccessException)
        {
            using (new RegistryOwnershipScope(reference.Hive, reference.View, reference.KeyPath))
            {
                await ExecuteDeleteAsync(reference);
            }
        }
    }

    private static Task ExecuteDeleteAsync(RegistryValueReference reference)
    {
        using var baseKey = RegistryKey.OpenBaseKey(reference.Hive, reference.View);
        using var key = baseKey.OpenSubKey(reference.KeyPath, true);
        if (key is not null && key.GetValue(reference.ValueName) is not null)
        {
            key.DeleteValue(reference.ValueName, false);
        }

        return Task.CompletedTask;
    }

    private static RegistryKey OpenOrCreateKey(RegistryValueReference reference)
    {
        using var baseKey = RegistryKey.OpenBaseKey(reference.Hive, reference.View);
        var key = baseKey.CreateSubKey(reference.KeyPath, true);
        if (key is null)
        {
            throw new InvalidOperationException($"Failed to open registry key {reference.Hive}\\{reference.KeyPath}.");
        }

        return key;
    }
}
