using System;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;
using RegProbe.Infrastructure.Elevation;

namespace RegProbe.Infrastructure.Registry;

public sealed class RegistryOwnershipScope : IDisposable
{
    private readonly RegistryKey _key;
    private readonly RegistrySecurity _originalSecurity;
    private bool _disposed;

    public RegistryOwnershipScope(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);

        // Enable required privileges for ownership transfer
        using (var pm = new PrivilegeManager())
        {
            pm.EnablePrivilege(PrivilegeNames.TakeOwnership);
            pm.EnablePrivilege(PrivilegeNames.Restore);
        }

        var openedKey = baseKey.OpenSubKey(
                            subKeyPath,
                            RegistryKeyPermissionCheck.ReadWriteSubTree,
                            RegistryRights.TakeOwnership | RegistryRights.ChangePermissions | RegistryRights.ReadPermissions)
                        ?? throw new UnauthorizedAccessException($"Could not open key {subKeyPath} for ownership change.");

        RegistrySecurity? originalSecurity = null;
        try
        {
            originalSecurity = openedKey.GetAccessControl();
            RegistryOwnershipMutationGuard.Execute(
                () => TakeOwnership(openedKey),
                () => GrantFullControl(openedKey),
                () => openedKey.SetAccessControl(originalSecurity));

            _key = openedKey;
            _originalSecurity = originalSecurity;
        }
        catch
        {
            openedKey.Dispose();
            throw;
        }
    }

    private static void TakeOwnership(RegistryKey key)
    {
        var security = new RegistrySecurity();
        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        security.SetOwner(adminSid);
        key.SetAccessControl(security);
    }

    private static void GrantFullControl(RegistryKey key)
    {
        var security = key.GetAccessControl();
        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

        var rule = new RegistryAccessRule(
            adminSid,
            RegistryRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow);

        security.ResetAccessRule(rule);
        key.SetAccessControl(security);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Restore original security (brings back TrustedInstaller ownership if it was the owner)
                _key.SetAccessControl(_originalSecurity);
            }
            catch
            {
                // Best effort
            }
            finally
            {
                _key.Dispose();
                _disposed = true;
            }
        }
    }
}
