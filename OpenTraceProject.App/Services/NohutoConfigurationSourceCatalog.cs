using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenTraceProject.App.Services;

public sealed class NohutoRepositoryDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Owner { get; init; } = "nohuto";
    public string Repository { get; init; } = string.Empty;
    public string Branch { get; init; } = "main";
    public string DisplayName { get; init; } = string.Empty;
    public string RoleLabel { get; init; } = string.Empty;
    public string RoleSummary { get; init; } = string.Empty;
    public string RepositoryUrl => $"https://github.com/{Owner}/{Repository}";
}

public static class NohutoConfigurationSourceCatalog
{
    public static IReadOnlyList<NohutoRepositoryDefinition> All { get; } = new[]
    {
        new NohutoRepositoryDefinition
        {
            Id = "win-config",
            Repository = "win-config",
            DisplayName = "win-config",
            RoleLabel = "Catalog",
            RoleSummary = "Documented Windows option catalog with strict state detection."
        },
        new NohutoRepositoryDefinition
        {
            Id = "win-registry",
            Repository = "win-registry",
            DisplayName = "win-registry",
            RoleLabel = "Research",
            RoleSummary = "Registry traces, defaults, and reverse-engineering notes for Windows behavior."
        },
        new NohutoRepositoryDefinition
        {
            Id = "decompiled-pseudocode",
            Repository = "decompiled-pseudocode",
            DisplayName = "decompiled-pseudocode",
            RoleLabel = "Internals",
            RoleSummary = "Function-level pseudocode from Windows binaries for semantic verification."
        },
        new NohutoRepositoryDefinition
        {
            Id = "regkit",
            Repository = "regkit",
            DisplayName = "regkit",
            RoleLabel = "Inspector",
            RoleSummary = "Advanced registry inspection, traces, defaults, and elevated editing companion."
        }
    };

    public static NohutoRepositoryDefinition Get(string repoId)
    {
        if (string.IsNullOrWhiteSpace(repoId))
        {
            throw new ArgumentException("Repository id is required.", nameof(repoId));
        }

        return All.FirstOrDefault(definition =>
                   string.Equals(definition.Id, repoId, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Unknown nohuto repository id '{repoId}'.");
    }
}
