using System.Text;
using System.Text.Json;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

internal sealed class WinConfigCatalogStore
{
    private readonly AppPaths _paths;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public WinConfigCatalogStore(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public string MarkdownReportPath => _paths.WinConfigCatalogMarkdownReportPath;

    public void EnsureDirectories()
        => _paths.EnsureDirectories();

    public WinConfigCatalogState LoadCachedState()
    {
        try
        {
            if (!File.Exists(_paths.WinConfigCatalogCacheFilePath))
            {
                return new WinConfigCatalogState();
            }

            var json = File.ReadAllText(_paths.WinConfigCatalogCacheFilePath);
            var state = JsonSerializer.Deserialize<WinConfigCatalogState>(json, JsonOptions);
            return state ?? new WinConfigCatalogState();
        }
        catch
        {
            return new WinConfigCatalogState();
        }
    }

    public void SaveState(WinConfigCatalogState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_paths.WinConfigCatalogCacheFilePath, json);
    }

    public void SaveMarkdownReport(WinConfigCatalogState state)
    {
        File.WriteAllText(_paths.WinConfigCatalogMarkdownReportPath, BuildMarkdownReport(state));
    }

    public WinConfigCatalogResult BuildResult(WinConfigCatalogState state, bool usedCachedData)
    {
        return new WinConfigCatalogResult
        {
            CheckedSuccessfully = state.Categories.Count > 0,
            UsedCachedData = usedCachedData,
            Summary = string.IsNullOrWhiteSpace(state.LastSummary)
                ? BuildSummary(state.Categories)
                : state.LastSummary,
            CheckedAtUtc = state.LastCheckedAtUtc,
            MarkdownReportPath = _paths.WinConfigCatalogMarkdownReportPath,
            Categories = state.Categories
        };
    }

    public static bool ShouldUseCachedState(WinConfigCatalogState state, TimeSpan? minimumRefreshInterval)
    {
        if (!minimumRefreshInterval.HasValue ||
            minimumRefreshInterval.Value <= TimeSpan.Zero ||
            state.Categories.Count == 0 ||
            state.LastCheckedAtUtc == default)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - state.LastCheckedAtUtc < minimumRefreshInterval.Value;
    }

    public static string BuildSummary(IReadOnlyList<WinConfigCatalogCategory> categories)
    {
        if (categories.Count == 0)
        {
            return "win-config catalog unavailable.";
        }

        var topicCount = categories.Sum(static category => category.TopicCount);
        var scriptCount = categories.Sum(static category => category.ScriptFileCount);
        return $"{categories.Count} categories, {topicCount} documented topics, {scriptCount} helper scripts/assets worth curating.";
    }

    private static string BuildMarkdownReport(WinConfigCatalogState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# win-config Catalog Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        if (!string.IsNullOrWhiteSpace(state.LastCommitSha))
        {
            builder.AppendLine($"Commit: {ShortSha(state.LastCommitSha)}");
        }

        builder.AppendLine($"Summary: {state.LastSummary}");
        builder.AppendLine();

        foreach (var category in state.Categories)
        {
            builder.AppendLine($"## {category.DisplayName}");
            builder.AppendLine();
            builder.AppendLine($"- Source: {category.SourceUrl}");
            builder.AppendLine($"- Description doc: {category.DescriptionUrl}");
            builder.AppendLine($"- Description: {category.Description}");
            builder.AppendLine($"- Counts: {category.TopicCount} topics, {category.FileCount} files, {category.DocumentationFileCount} docs, {category.ScriptFileCount} scripts, {category.AssetFileCount} assets");

            if (category.Topics.Count > 0)
            {
                builder.AppendLine("- Topic sample:");
                foreach (var topic in category.Topics.Take(8))
                {
                    builder.AppendLine($"  - {topic}");
                }
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Product Use");
        builder.AppendLine();
        builder.AppendLine("- Treat each category as a future configuration domain, not an auto-import source.");
        builder.AppendLine("- Use descriptions and topics to build read-only catalog cards first.");
        builder.AppendLine("- Promote only curated options into SAFE actions after detect/verify/rollback coverage exists.");
        return builder.ToString();
    }

    private static string ShortSha(string sha)
        => sha.Length <= 8 ? sha : sha[..8];
}
