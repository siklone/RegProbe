using System;
using System.Collections.Generic;

namespace RegProbe.App.Services;

public enum ConfigurationWorkspaceKind
{
    Settings,
    Maintenance
}

public sealed class ConfigurationWorkspaceClassifier
{
    private static readonly HashSet<string> MaintenanceIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "network.flush-dns-cache",
        "network.reset-winsock"
    };

    private static readonly HashSet<string> PersistentCleanupIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "cleanup.disable-reserved-storage"
    };

    public ConfigurationWorkspaceKind Classify(string? tweakId, string? category)
    {
        if (!string.IsNullOrWhiteSpace(tweakId))
        {
            if (MaintenanceIds.Contains(tweakId))
            {
                return ConfigurationWorkspaceKind.Maintenance;
            }

            if (PersistentCleanupIds.Contains(tweakId))
            {
                return ConfigurationWorkspaceKind.Settings;
            }
        }

        if (string.Equals(category, "Cleanup", StringComparison.OrdinalIgnoreCase))
        {
            return ConfigurationWorkspaceKind.Maintenance;
        }

        return ConfigurationWorkspaceKind.Settings;
    }
}
