using System.Text;
using System.Text.Json;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

internal sealed class NohutoRepoScanStore
{
    private readonly AppPaths _paths;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public NohutoRepoScanStore(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public string JsonReportPath => _paths.NohutoAnalysisReportPath;

    public string MarkdownReportPath => _paths.NohutoAnalysisMarkdownPath;

    public void EnsureDirectories()
        => _paths.EnsureDirectories();

    public NohutoRepoScanState LoadCachedState()
    {
        try
        {
            if (!File.Exists(_paths.NohutoScanStateFilePath))
            {
                return new NohutoRepoScanState();
            }

            var json = File.ReadAllText(_paths.NohutoScanStateFilePath);
            var state = JsonSerializer.Deserialize<NohutoRepoScanState>(json, JsonOptions);
            return state ?? new NohutoRepoScanState();
        }
        catch
        {
            return new NohutoRepoScanState();
        }
    }

    public void SaveState(NohutoRepoScanState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_paths.NohutoScanStateFilePath, json);
    }

    public void SaveReport(NohutoRepoScanState state, IReadOnlyList<RepositoryScanPayload> repoScans)
    {
        var report = new
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Summary = state.LastSummary,
            Repositories = state.Repositories,
            Details = repoScans.Select(scan => new
            {
                scan.State.RepoId,
                scan.State.DisplayName,
                scan.State.StateKind,
                scan.State.Summary,
                scan.State.LastSeenCommitSha,
                scan.State.LastSeenCommitMessage,
                scan.State.LastSeenCommitDateUtc,
                scan.State.LastAnalysis,
                ChangedFiles = scan.ChangedFiles
            })
        };

        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(_paths.NohutoAnalysisReportPath, json);
        File.WriteAllText(_paths.NohutoAnalysisMarkdownPath, BuildMarkdownReport(state, repoScans));
    }

    public NohutoRepoScanResult BuildResultFromState(NohutoRepoScanState state, bool usedCachedData)
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
            JsonReportPath = _paths.NohutoAnalysisReportPath,
            MarkdownReportPath = _paths.NohutoAnalysisMarkdownPath,
            Repositories = repositories
        };
    }

    public RepositoryScanPayload BuildFailureState(
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

    public static bool ShouldUseCachedState(NohutoRepoScanState state, TimeSpan? minimumRefreshInterval)
    {
        if (!minimumRefreshInterval.HasValue ||
            minimumRefreshInterval.Value <= TimeSpan.Zero ||
            state.Repositories.Count == 0 ||
            state.LastCheckedAtUtc == default)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - state.LastCheckedAtUtc < minimumRefreshInterval.Value;
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

    private static string BuildMarkdownReport(NohutoRepoScanState state, IReadOnlyList<RepositoryScanPayload> repoScans)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Nohuto Configuration Sources Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine($"Summary: {state.LastSummary}");
        builder.AppendLine();
        builder.AppendLine("## Repository Roles");
        builder.AppendLine();

        foreach (var definition in NohutoConfigurationSourceCatalog.All)
        {
            builder.AppendLine($"- `{definition.DisplayName}` ({definition.RoleLabel}): {definition.RoleSummary}");
        }

        builder.AppendLine();
        builder.AppendLine("## Current Status");
        builder.AppendLine();

        foreach (var scan in repoScans.OrderBy(payload => GetDefinitionOrder(payload.Definition.Id)))
        {
            var stateEntry = scan.State;
            builder.AppendLine($"### {stateEntry.DisplayName}");
            builder.AppendLine();
            builder.AppendLine($"- Role: {stateEntry.RoleLabel}");
            builder.AppendLine($"- Repository: {stateEntry.RepositoryUrl}");
            builder.AppendLine($"- Status: {stateEntry.StateKind}");
            builder.AppendLine($"- Checked successfully: {(stateEntry.CheckedSuccessfully ? "Yes" : "No")}");
            builder.AppendLine($"- Last checked: {stateEntry.LastCheckedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
            builder.AppendLine($"- Commit: {FormatCommitLine(stateEntry)}");
            builder.AppendLine($"- Summary: {stateEntry.Summary}");
            builder.AppendLine($"- Change kinds: {FormatChangeKinds(stateEntry.LastAnalysis)}");
            builder.AppendLine($"- Top categories: {FormatTopCategories(stateEntry.LastAnalysis)}");

            if (!string.IsNullOrWhiteSpace(stateEntry.LastSeenCommitMessage))
            {
                builder.AppendLine($"- Commit message: {SingleLine(stateEntry.LastSeenCommitMessage)}");
            }

            if (scan.ChangedFiles.Count > 0)
            {
                builder.AppendLine("- Sample changed paths:");
                foreach (var changedFile in scan.ChangedFiles.Take(8))
                {
                    builder.AppendLine($"  - `{changedFile.Path}` (+{changedFile.Additions}/-{changedFile.Deletions})");
                }
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Product Integration Notes");
        builder.AppendLine();
        builder.AppendLine("- `win-config`: seed user-facing option cards, detection rules, and curated one-click actions.");
        builder.AppendLine("- `win-registry`: back each option with defaults, observed registry activity, and source notes.");
        builder.AppendLine("- `decompiled-pseudocode`: use as internals evidence only; do not expose raw pseudocode as a direct tweak source.");
        builder.AppendLine("- `regkit`: deep-link inspection, trace/default validation, and advanced troubleshooting workflow.");
        builder.AppendLine();
        builder.AppendLine("## Safe Ingestion Rules");
        builder.AppendLine();
        builder.AppendLine("- Only ship options after Detect -> Apply -> Verify -> Rollback is implemented.");
        builder.AppendLine("- Treat reverse-engineered values as research until corroborated by observed state or vendor/Microsoft behavior.");
        builder.AppendLine("- Keep security reductions out of SAFE defaults unless the project explicitly marks them as unsafe/advanced.");

        return builder.ToString();
    }

    private static string FormatCommitLine(NohutoTrackedRepositoryState state)
    {
        if (string.IsNullOrWhiteSpace(state.LastSeenCommitSha) || !state.LastSeenCommitDateUtc.HasValue)
        {
            return "Unavailable";
        }

        return $"{ShortSha(state.LastSeenCommitSha)} ({state.LastSeenCommitDateUtc.Value:yyyy-MM-dd})";
    }

    private static string FormatTopCategories(NohutoChangeAnalysis analysis)
        => analysis.TopCategories.Count == 0
            ? "None"
            : string.Join(", ", analysis.TopCategories.Take(3).Select(static insight => insight.Category));

    private static string FormatChangeKinds(NohutoChangeAnalysis analysis)
    {
        var kinds = new List<string>();

        if (analysis.DocumentationChangedFiles > 0) kinds.Add($"{analysis.DocumentationChangedFiles} docs");
        if (analysis.ScriptChangedFiles > 0) kinds.Add($"{analysis.ScriptChangedFiles} scripts");
        if (analysis.SourceChangedFiles > 0) kinds.Add($"{analysis.SourceChangedFiles} source");
        if (analysis.AssetChangedFiles > 0) kinds.Add($"{analysis.AssetChangedFiles} assets");
        if (analysis.DataChangedFiles > 0) kinds.Add($"{analysis.DataChangedFiles} data");

        return kinds.Count == 0 ? "None" : string.Join(", ", kinds);
    }

    private static string SingleLine(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

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

internal sealed class RepositoryScanPayload
{
    public required NohutoRepositoryDefinition Definition { get; init; }
    public required NohutoTrackedRepositoryState State { get; init; }
    public IReadOnlyList<NohutoChangedFile> ChangedFiles { get; init; } = Array.Empty<NohutoChangedFile>();
}
