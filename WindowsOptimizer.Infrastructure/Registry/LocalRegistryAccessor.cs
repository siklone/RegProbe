using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Core.Registry;

namespace WindowsOptimizer.Infrastructure.Registry;

public sealed class LocalRegistryAccessor : IRegistryAccessor
{
    public Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Check if we're on Windows - registry only works on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Windows Registry operations are only supported on Windows. " +
                "The application is currently running on " + RuntimeInformation.OSDescription);
        }

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

    public Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Check if we're on Windows - registry only works on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Windows Registry operations are only supported on Windows. " +
                "The application is currently running on " + RuntimeInformation.OSDescription);
        }

        try
        {
            using var key = OpenOrCreateKey(reference);
            key.SetValue(reference.ValueName, value.ToObject(), value.Kind);
        }
        catch (UnauthorizedAccessException) when (ShouldAttemptOwnership(reference))
        {
            // If access denied, try to take ownership and retry
            using (new RegistryOwnershipScope(reference.Hive, reference.View, reference.KeyPath))
            {
                using var key = OpenOrCreateKey(reference);
                key.SetValue(reference.ValueName, value.ToObject(), value.Kind);
            }
        }

        return Task.CompletedTask;
    }

    public async Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Check if we're on Windows - registry only works on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Windows Registry operations are only supported on Windows. " +
                "The application is currently running on " + RuntimeInformation.OSDescription);
        }

        try
        {
            await ExecuteDeleteAsync(reference);
        }
        catch (UnauthorizedAccessException) when (ShouldAttemptOwnership(reference))
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

    private static bool ShouldAttemptOwnership(RegistryValueReference reference)
    {
        // HKCU failures are commonly policy-ACL or session-specific and should surface
        // as a normal access error rather than trying to escalate token privileges.
        return reference.Hive != RegistryHive.CurrentUser;
    }
}
