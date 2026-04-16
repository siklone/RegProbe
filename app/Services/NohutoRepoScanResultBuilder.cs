namespace RegProbe.App.Services;

internal static class NohutoRepoScanResultBuilder
{
    public static NohutoRepoScanResult BuildResult(
        NohutoRepoScanState state,
        bool usedCachedData,
        string jsonReportPath,
        string markdownReportPath)
    {
        var repositories = state.Repositories
            .OrderBy(repository => GetDefinitionOrder(repository.RepoId))
            .ToArray();

        return new NohutoRepoScanResult
        {
            CheckedSuccessfully = repositories.Any(static repository => repository.CheckedSuccessfully),
            UsedCachedData = usedCachedData,
            UpdatedRepositoryCount = repositories.Count(static repository => repository.StateKind == NohutoRepositoryStateKind.Updated),
            BaselineRepositoryCount = repositories.Count(static repository => repository.StateKind == NohutoRepositoryStateKind.Baseline),
            CheckedAtUtc = state.LastCheckedAtUtc,
            Summary = string.IsNullOrWhiteSpace(state.LastSummary)
                ? BuildAggregateSummary(repositories)
                : state.LastSummary,
            JsonReportPath = jsonReportPath,
            MarkdownReportPath = markdownReportPath,
            Repositories = repositories
        };
    }

    public static RepositoryScanPayload BuildFailureState(
        NohutoRepositoryDefinition definition,
        NohutoTrackedRepositoryState? previousState,
        string error)
    {
        var state = previousState is null
            ? new NohutoTrackedRepositoryState
            {
                RepoId = definition.Id,
                DisplayName = definition.DisplayName,
                RoleLabel = definition.RoleLabel,
                RoleSummary = definition.RoleSummary,
                RepositoryUrl = definition.RepositoryUrl
            }
            : CloneState(definition, previousState);

        state.LastCheckedAtUtc = DateTimeOffset.UtcNow;
        state.CheckedSuccessfully = false;
        state.HasNewCommit = false;
        state.StateKind = NohutoRepositoryStateKind.Failed;
        state.Summary = $"Check failed: {error}";

        return new RepositoryScanPayload
        {
            Definition = definition,
            State = state
        };
    }

    public static NohutoTrackedRepositoryState CloneState(
        NohutoRepositoryDefinition definition,
        NohutoTrackedRepositoryState source)
    {
        return new NohutoTrackedRepositoryState
        {
            RepoId = definition.Id,
            DisplayName = definition.DisplayName,
            RoleLabel = definition.RoleLabel,
            RoleSummary = definition.RoleSummary,
            RepositoryUrl = definition.RepositoryUrl,
            LastSeenCommitSha = source.LastSeenCommitSha,
            LastSeenCommitMessage = source.LastSeenCommitMessage,
            LastSeenCommitDateUtc = source.LastSeenCommitDateUtc,
            LastCheckedAtUtc = source.LastCheckedAtUtc,
            CheckedSuccessfully = source.CheckedSuccessfully,
            HasNewCommit = source.HasNewCommit,
            StateKind = source.StateKind,
            Summary = source.Summary,
            LastAnalysis = source.LastAnalysis ?? new NohutoChangeAnalysis()
        };
    }

    public static string BuildAggregateSummary(IReadOnlyList<NohutoTrackedRepositoryState> repositories)
    {
        if (repositories.Count == 0)
        {
            return "Configuration source feed unavailable.";
        }

        var updated = repositories.Count(static repository => repository.StateKind == NohutoRepositoryStateKind.Updated);
        var baselines = repositories.Count(static repository => repository.StateKind == NohutoRepositoryStateKind.Baseline);
        var successful = repositories.Count(static repository => repository.CheckedSuccessfully);
        var failed = repositories.Count(static repository => repository.StateKind == NohutoRepositoryStateKind.Failed);
        var topImpact = string.Join(", ", AggregateTopCategories(repositories).Take(3));

        if (updated > 0)
        {
            return $"{updated} sources updated{FormatSuffix(topImpact)}.";
        }

        if (baselines > 0)
        {
            return $"{baselines} sources baselined. Future upstream changes will appear here.";
        }

        if (successful > 0 && failed == 0)
        {
            return $"{successful} sources tracked. No new upstream changes.";
        }

        if (successful > 0)
        {
            return $"{successful}/{repositories.Count} sources refreshed. Some checks failed.";
        }

        return "Configuration source feed unavailable.";
    }

    public static string BuildRepositorySummary(
        NohutoRepositoryStateKind stateKind,
        string latestSha,
        NohutoChangeAnalysis analysis)
    {
        var topImpact = analysis.TopCategories.Count > 0
            ? string.Join(", ", analysis.TopCategories.Take(2).Select(static insight => insight.Category))
            : "Misc";

        return stateKind switch
        {
            NohutoRepositoryStateKind.Baseline => $"Baseline from {ShortSha(latestSha)} with {analysis.TotalChangedFiles} files. Top impact {topImpact}.",
            NohutoRepositoryStateKind.Updated => $"Update {ShortSha(latestSha)} touched {analysis.TotalChangedFiles} files. Top impact {topImpact}.",
            _ => $"Tracked at {ShortSha(latestSha)}."
        };
    }

    public static string BuildUnchangedSummary(NohutoChangeAnalysis analysis, string latestSha)
    {
        if (analysis.TopCategories.Count == 0)
        {
            return $"No new changes since {ShortSha(latestSha)}.";
        }

        var topImpact = string.Join(", ", analysis.TopCategories.Take(2).Select(static insight => insight.Category));
        return $"No new changes since {ShortSha(latestSha)}. Last tracked impact {topImpact}.";
    }

    public static int GetDefinitionOrder(string repoId)
    {
        for (var index = 0; index < NohutoConfigurationSourceCatalog.All.Count; index++)
        {
            if (string.Equals(NohutoConfigurationSourceCatalog.All[index].Id, repoId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static IEnumerable<string> AggregateTopCategories(IReadOnlyList<NohutoTrackedRepositoryState> repositories)
    {
        return repositories
            .Where(static repository => repository.CheckedSuccessfully)
            .SelectMany(static repository => repository.LastAnalysis.TopCategories)
            .GroupBy(static insight => insight.Category, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Sum(static insight => insight.Score))
            .ThenByDescending(group => group.Sum(static insight => insight.FileCount))
            .Select(static group => group.Key);
    }

    private static string FormatSuffix(string topImpact)
        => string.IsNullOrWhiteSpace(topImpact) ? string.Empty : $" | top impact {topImpact}";

    private static string ShortSha(string? sha)
    {
        if (string.IsNullOrWhiteSpace(sha))
        {
            return "unknown";
        }

        return sha.Length <= 8 ? sha : sha[..8];
    }
}
