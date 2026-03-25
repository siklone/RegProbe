using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Infrastructure;

namespace OpenTraceProject.App.Services;

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

public static class WinConfigCatalogParser
{
    public static string ExtractLeadParagraph(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var paragraph = new List<string>();
        var inCodeFence = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith(">", StringComparison.Ordinal) ||
                line.StartsWith("|", StringComparison.Ordinal) ||
                line.StartsWith("```", StringComparison.Ordinal))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }

                continue;
            }

            paragraph.Add(line);
        }

        return string.Join(" ", paragraph).Trim();
    }

    public static IReadOnlyList<string> ExtractTopLevelTopics(string markdown, int limit = 12)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return Array.Empty<string>();
        }

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var topics = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            var title = line[2..].Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            topics.Add(title);
            if (topics.Count >= limit)
            {
                break;
            }
        }

        return topics;
    }

    public static WinConfigCatalogFileKind ClassifyFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return WinConfigCatalogFileKind.Data;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".md" => WinConfigCatalogFileKind.Documentation,
            ".ps1" or ".cmd" or ".bat" or ".py" or ".reg" or ".inf" or ".json" or ".xml" => WinConfigCatalogFileKind.Script,
            ".png" or ".jpg" or ".jpeg" or ".svg" or ".bmp" or ".ico" or ".pdf" => WinConfigCatalogFileKind.Asset,
            _ => WinConfigCatalogFileKind.Data
        };
    }
}

public enum WinConfigCatalogFileKind
{
    Documentation = 0,
    Script = 1,
    Asset = 2,
    Data = 3
}

public sealed class WinConfigCatalogService : IDisposable
{
    private const string Owner = "nohuto";
    private const string Repo = "win-config";
    private const string Branch = "main";
    private readonly AppPaths _paths;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public WinConfigCatalogService(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenTraceProject-WinConfigCatalog");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

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

    public async Task<WinConfigCatalogResult> RefreshAsync(CancellationToken ct, TimeSpan? minimumRefreshInterval = null)
    {
        try
        {
            _paths.EnsureDirectories();

            var cachedState = LoadCachedState();
            if (ShouldUseCachedState(cachedState, minimumRefreshInterval))
            {
                return BuildResult(cachedState, usedCachedData: true);
            }

            var latestCommit = await GetLatestCommitAsync(ct);
            if (cachedState.Categories.Count > 0 &&
                !string.IsNullOrWhiteSpace(cachedState.LastCommitSha) &&
                string.Equals(cachedState.LastCommitSha, latestCommit.Sha, StringComparison.OrdinalIgnoreCase))
            {
                cachedState.LastCheckedAtUtc = DateTimeOffset.UtcNow;
                cachedState.LastCommitDateUtc = latestCommit.Date;
                cachedState.LastSummary = BuildSummary(cachedState.Categories);
                SaveState(cachedState);
                SaveMarkdownReport(cachedState);
                return BuildResult(cachedState, usedCachedData: false);
            }

            var categories = await LoadCategoriesAsync(ct);
            var refreshedState = new WinConfigCatalogState
            {
                LastCheckedAtUtc = DateTimeOffset.UtcNow,
                LastCommitSha = latestCommit.Sha,
                LastCommitDateUtc = latestCommit.Date,
                LastSummary = BuildSummary(categories),
                Categories = categories
            };

            SaveState(refreshedState);
            SaveMarkdownReport(refreshedState);
            return BuildResult(refreshedState, usedCachedData: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WinConfigCatalog] Failed: {ex.Message}");
            return new WinConfigCatalogResult
            {
                CheckedSuccessfully = false,
                UsedCachedData = false,
                Summary = $"win-config catalog refresh failed: {ex.Message}",
                MarkdownReportPath = _paths.WinConfigCatalogMarkdownReportPath
            };
        }
    }

    private async Task<List<WinConfigCatalogCategory>> LoadCategoriesAsync(CancellationToken ct)
    {
        var topLevelEntries = await GetDirectoryContentsAsync(string.Empty, ct);
        var categoryDirectories = topLevelEntries
            .Where(static entry => string.Equals(entry.Type, "dir", StringComparison.OrdinalIgnoreCase))
            .Where(static entry => !entry.Name.StartsWith(".", StringComparison.Ordinal))
            .OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var categories = new List<WinConfigCatalogCategory>();

        foreach (var directory in categoryDirectories)
        {
            var files = await GetDirectoryContentsRecursiveAsync(directory.Path, ct);
            var descriptionFile = files.FirstOrDefault(static entry =>
                string.Equals(entry.Name, "desc.md", StringComparison.OrdinalIgnoreCase));
            var markdown = descriptionFile?.DownloadUrl is { Length: > 0 }
                ? await GetRawTextAsync(descriptionFile.DownloadUrl, ct)
                : string.Empty;
            var topics = WinConfigCatalogParser.ExtractTopLevelTopics(markdown);

            var documentationCount = 0;
            var scriptCount = 0;
            var assetCount = 0;

            foreach (var file in files.Where(static entry => string.Equals(entry.Type, "file", StringComparison.OrdinalIgnoreCase)))
            {
                switch (WinConfigCatalogParser.ClassifyFile(file.Path))
                {
                    case WinConfigCatalogFileKind.Documentation:
                        documentationCount++;
                        break;
                    case WinConfigCatalogFileKind.Script:
                        scriptCount++;
                        break;
                    case WinConfigCatalogFileKind.Asset:
                        assetCount++;
                        break;
                }
            }

            categories.Add(new WinConfigCatalogCategory
            {
                Id = directory.Name,
                DisplayName = FormatDisplayName(directory.Name),
                SourceUrl = directory.HtmlUrl,
                DescriptionUrl = descriptionFile?.HtmlUrl ?? directory.HtmlUrl,
                Description = Truncate(WinConfigCatalogParser.ExtractLeadParagraph(markdown), 280),
                TopicCount = topics.Count,
                FileCount = files.Count(static entry => string.Equals(entry.Type, "file", StringComparison.OrdinalIgnoreCase)),
                DocumentationFileCount = documentationCount,
                ScriptFileCount = scriptCount,
                AssetFileCount = assetCount,
                Topics = topics
            });
        }

        return categories;
    }

    private async Task<(string Sha, DateTimeOffset Date)> GetLatestCommitAsync(CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/commits/{Branch}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCommitEnvelope>(stream, JsonOptions, ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Sha))
        {
            throw new InvalidOperationException("Latest win-config commit metadata unavailable.");
        }

        return (payload.Sha, payload.Commit.Committer.Date);
    }

    private async Task<List<GitHubContentEntry>> GetDirectoryContentsRecursiveAsync(string path, CancellationToken ct)
    {
        var results = new List<GitHubContentEntry>();
        var queue = new Queue<string>();
        queue.Enqueue(path);

        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var current = queue.Dequeue();
            var entries = await GetDirectoryContentsAsync(current, ct);
            foreach (var entry in entries)
            {
                if (string.Equals(entry.Type, "dir", StringComparison.OrdinalIgnoreCase))
                {
                    queue.Enqueue(entry.Path);
                }
                else
                {
                    results.Add(entry);
                }
            }
        }

        return results;
    }

    private async Task<List<GitHubContentEntry>> GetDirectoryContentsAsync(string path, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(path)
            ? $"https://api.github.com/repos/{Owner}/{Repo}/contents"
            : $"https://api.github.com/repos/{Owner}/{Repo}/contents/{path}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<List<GitHubContentEntry>>(stream, JsonOptions, ct);
        return payload ?? new List<GitHubContentEntry>();
    }

    private async Task<string> GetRawTextAsync(string url, CancellationToken ct)
    {
        return await _httpClient.GetStringAsync(url, ct);
    }

    private void SaveState(WinConfigCatalogState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_paths.WinConfigCatalogCacheFilePath, json);
    }

    private void SaveMarkdownReport(WinConfigCatalogState state)
    {
        File.WriteAllText(_paths.WinConfigCatalogMarkdownReportPath, BuildMarkdownReport(state));
    }

    private WinConfigCatalogResult BuildResult(WinConfigCatalogState state, bool usedCachedData)
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

    private static bool ShouldUseCachedState(WinConfigCatalogState state, TimeSpan? minimumRefreshInterval)
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

    private static string BuildSummary(IReadOnlyList<WinConfigCatalogCategory> categories)
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

    private static string FormatDisplayName(string categoryName)
    {
        return string.Join(" ", categoryName
            .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0
                ? string.Empty
                : char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)].TrimEnd() + "...";
    }

    private static string ShortSha(string sha)
        => sha.Length <= 8 ? sha : sha[..8];

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
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
}
