using System;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;
using WindowsOptimizer.Infrastructure.Elevation;

namespace WindowsOptimizer.Infrastructure.Registry;

public sealed class RegistryOwnershipScope : IDisposable
{
    private readonly RegistryKey _key;
    private readonly string _subKeyPath;
    private readonly RegistrySecurity _originalSecurity;
    private bool _disposed;

    public RegistryOwnershipScope(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        _subKeyPath = subKeyPath;
        var baseKey = RegistryKey.OpenBaseKey(hive, view);

        // Enable required privileges for ownership transfer
        using (var pm = new PrivilegeManager())
        {
            pm.EnablePrivilege(PrivilegeNames.TakeOwnership);
            pm.EnablePrivilege(PrivilegeNames.Restore);
        }
        
        // Open with TakeOwnership rights. If it fails even with privileges, let the exception bubble up.
        _key = baseKey.OpenSubKey(subKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership | RegistryRights.ChangePermissions | RegistryRights.ReadPermissions)
               ?? throw new UnauthorizedAccessException($"Could not open key {subKeyPath} for ownership change.");

        _originalSecurity = _key.GetAccessControl();
        
        TakeOwnership();
        GrantFullControl();
    }

    private void TakeOwnership()
    {
        var security = new RegistrySecurity();
        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        security.SetOwner(adminSid);
        _key.SetAccessControl(security);
    }

    private void GrantFullControl()
    {
        var security = _key.GetAccessControl();
        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        
        var rule = new RegistryAccessRule(
            adminSid,
            RegistryRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow);

        security.ResetAccessRule(rule);
        _key.SetAccessControl(security);
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
