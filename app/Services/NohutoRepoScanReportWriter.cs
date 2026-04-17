using System.Text;
using System.Text.Json;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

internal sealed class NohutoRepoScanReportWriter
{
    private readonly AppPaths _paths;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public NohutoRepoScanReportWriter(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public void Save(NohutoRepoScanState state, IReadOnlyList<RepositoryScanPayload> repoScans)
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

        foreach (var scan in repoScans.OrderBy(payload => NohutoRepoScanResultBuilder.GetDefinitionOrder(payload.Definition.Id)))
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

    private static string ShortSha(string? sha)
    {
        if (string.IsNullOrWhiteSpace(sha))
        {
            return "unknown";
        }

        return sha.Length <= 8 ? sha : sha[..8];
    }
}
