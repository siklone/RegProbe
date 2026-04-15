using System.Text.Json.Serialization;

namespace RegProbe.App.Services;

public sealed class WinConfigCatalogCategory
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string DescriptionUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TopicCount { get; set; }
    public int FileCount { get; set; }
    public int DocumentationFileCount { get; set; }
    public int ScriptFileCount { get; set; }
    public int AssetFileCount { get; set; }
    public IReadOnlyList<string> Topics { get; set; } = Array.Empty<string>();
}

public sealed class WinConfigCatalogState
{
    public DateTimeOffset LastCheckedAtUtc { get; set; }
    public string LastCommitSha { get; set; } = string.Empty;
    public DateTimeOffset? LastCommitDateUtc { get; set; }
    public string LastSummary { get; set; } = string.Empty;
    public List<WinConfigCatalogCategory> Categories { get; set; } = new();
}

public sealed class WinConfigCatalogResult
{
    public bool CheckedSuccessfully { get; init; }
    public bool UsedCachedData { get; init; }
    public string Summary { get; init; } = "win-config catalog unavailable.";
    public DateTimeOffset CheckedAtUtc { get; init; }
    public string MarkdownReportPath { get; init; } = string.Empty;
    public string RepositoryUrl { get; init; } = "https://github.com/nohuto/win-config";
    public IReadOnlyList<WinConfigCatalogCategory> Categories { get; init; } = Array.Empty<WinConfigCatalogCategory>();
}

public enum WinConfigCatalogFileKind
{
    Documentation = 0,
    Script = 1,
    Asset = 2,
    Data = 3
}

internal sealed class GitHubContentEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}
