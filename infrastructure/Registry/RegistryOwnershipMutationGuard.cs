using System;

namespace RegProbe.Infrastructure.Registry;

public static class RegistryOwnershipMutationGuard
{
    public static void Execute(Action applyOwnership, Action grantAccess, Action rollback)
    {
        ArgumentNullException.ThrowIfNull(applyOwnership);
        ArgumentNullException.ThrowIfNull(grantAccess);
        ArgumentNullException.ThrowIfNull(rollback);

        var ownershipApplied = false;
        try
        {
            applyOwnership();
            ownershipApplied = true;
            grantAccess();
        }
        catch
        {
            if (ownershipApplied)
            {
                try
                {
                    rollback();
                }
                catch
                {
                    // Best-effort rollback only.
                }
            }

            throw;
        }
    }
}
