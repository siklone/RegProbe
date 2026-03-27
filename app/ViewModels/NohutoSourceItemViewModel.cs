using System;
using System.Linq;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class NohutoSourceItemViewModel
{
    public NohutoSourceItemViewModel(NohutoTrackedRepositoryState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        DisplayName = FirstNonEmpty(state.DisplayName, state.RepoId);
        RoleLabel = FirstNonEmpty(state.RoleLabel, "Tracked");
        RoleSummary = FirstNonEmpty(state.RoleSummary, "No repository description available yet.");
        RepositoryUrl = state.RepositoryUrl;
        StatusLabel = state.StateKind switch
        {
            NohutoRepositoryStateKind.Updated => "Updated",
            NohutoRepositoryStateKind.Baseline => "Baseline",
            NohutoRepositoryStateKind.Failed => "Check failed",
            NohutoRepositoryStateKind.Unchanged => "Tracked",
            _ => "Pending"
        };
        Summary = FirstNonEmpty(state.Summary, "No summary available yet.");
        ImpactSummary = BuildImpactSummary(state.LastAnalysis);
        CommitSummary = BuildCommitSummary(state);
    }

    public string DisplayName { get; }
    public string RoleLabel { get; }
    public string RoleSummary { get; }
    public string RepositoryUrl { get; }
    public string StatusLabel { get; }
    public string Summary { get; }
    public string ImpactSummary { get; }
    public string CommitSummary { get; }

    private static string BuildImpactSummary(NohutoChangeAnalysis analysis)
    {
        if (analysis.TopCategories.Count == 0)
        {
            return "Impact map will appear after the first analyzed change set.";
        }

        var categories = analysis.TopCategories
            .Select(static insight => insight.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        return $"Impact: {string.Join(", ", categories)}";
    }

    private static string BuildCommitSummary(NohutoTrackedRepositoryState state)
    {
        if (string.IsNullOrWhiteSpace(state.LastSeenCommitSha) || !state.LastSeenCommitDateUtc.HasValue)
        {
            return "No commit baseline yet.";
        }

        var sha = state.LastSeenCommitSha.Length <= 8
            ? state.LastSeenCommitSha
            : state.LastSeenCommitSha[..8];

        return $"{sha} Â· {state.LastSeenCommitDateUtc.Value:yyyy-MM-dd}";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
