using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace WindowsOptimizer.App.Services;

public sealed class NohutoChangedFile
{
    public string Path { get; init; } = string.Empty;
    public int Additions { get; init; }
    public int Deletions { get; init; }
}

public sealed class NohutoCategoryInsight
{
    public string Category { get; init; } = string.Empty;
    public int Score { get; init; }
    public int FileCount { get; init; }
}

public sealed class NohutoChangeAnalysis
{
    public int TotalChangedFiles { get; init; }
    public int RecordsChangedFiles { get; init; }
    public int GuidesChangedFiles { get; init; }
    public int DocsChangedFiles { get; init; }
    public IReadOnlyList<NohutoCategoryInsight> TopCategories { get; init; } = Array.Empty<NohutoCategoryInsight>();
}

public static class NohutoChangeAnalyzer
{
    public static NohutoChangeAnalysis Analyze(IEnumerable<NohutoChangedFile> changedFiles)
    {
        if (changedFiles is null)
        {
            throw new ArgumentNullException(nameof(changedFiles));
        }

        var files = changedFiles
            .Where(file => file is not null && !string.IsNullOrWhiteSpace(file.Path))
            .ToList();

        var scoreByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fileCountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var recordsCount = 0;
        var guidesCount = 0;
        var docsCount = 0;

        foreach (var file in files)
        {
            var normalizedPath = file.Path.Replace('\\', '/');
            if (normalizedPath.StartsWith("records/", StringComparison.OrdinalIgnoreCase))
            {
                recordsCount++;
            }
            else if (normalizedPath.StartsWith("guide/", StringComparison.OrdinalIgnoreCase))
            {
                guidesCount++;
            }
            else if (normalizedPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                docsCount++;
            }

            var category = ResolveCategory(normalizedPath);
            var weight = Math.Max(1, file.Additions + file.Deletions);
            if (!scoreByCategory.ContainsKey(category))
            {
                scoreByCategory[category] = 0;
                fileCountByCategory[category] = 0;
            }

            scoreByCategory[category] += weight;
            fileCountByCategory[category]++;
        }

        var topCategories = scoreByCategory
            .Select(pair => new NohutoCategoryInsight
            {
                Category = pair.Key,
                Score = pair.Value,
                FileCount = fileCountByCategory[pair.Key]
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.FileCount)
            .Take(5)
            .ToList();

        return new NohutoChangeAnalysis
        {
            TotalChangedFiles = files.Count,
            RecordsChangedFiles = recordsCount,
            GuidesChangedFiles = guidesCount,
            DocsChangedFiles = docsCount,
            TopCategories = topCategories
        };
    }

    private static string ResolveCategory(string path)
    {
        var fileName = path;
        var slashIndex = fileName.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < fileName.Length - 1)
        {
            fileName = fileName[(slashIndex + 1)..];
        }

        var key = fileName.ToLowerInvariant();

        if (key.Contains("power") || key.Contains("hiber") || key.Contains("mmcss"))
            return "Power";

        if (key.Contains("tcpip") || key.Contains("dns") || key.Contains("net") || key.Contains("nic"))
            return "Network";

        if (key.Contains("defender") || key.Contains("lsa") || key.Contains("security"))
            return "Security";

        if (key.Contains("privacy") || key.Contains("error-report") || key.Contains("wisp"))
            return "Privacy";

        if (key.Contains("audio"))
            return "Audio";

        if (key.Contains("explorer") || key.Contains("desktop") || key.Contains("mouse") || key.Contains("visibility"))
            return "Visibility";

        if (key.Contains("perf") || key.Contains("stornvme") || key.Contains("storport") || key.Contains("graphics"))
            return "Performance";

        if (key.Contains("task") || key.Contains("service"))
            return "System";

        return "Misc";
    }
}

public sealed class NohutoRepoScanState
{
    public string LastSeenCommitSha { get; set; } = string.Empty;
    public DateTimeOffset? LastSeenCommitDateUtc { get; set; }
    public DateTimeOffset LastCheckedAtUtc { get; set; }
    public NohutoChangeAnalysis LastAnalysis { get; set; } = new();
}

public sealed class NohutoRepoScanResult
{
    public bool CheckedSuccessfully { get; init; }
    public bool HasNewCommit { get; init; }
    public string LatestCommitSha { get; init; } = string.Empty;
    public DateTimeOffset? LatestCommitDateUtc { get; init; }
    public string Summary { get; init; } = "Nohuto repo scan unavailable.";
    public NohutoChangeAnalysis Analysis { get; init; } = new();
}

internal sealed class GitHubCommitEnvelope
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("commit")]
    public GitHubCommitDetails Commit { get; set; } = new();
}

internal sealed class GitHubCommitDetails
{
    [JsonPropertyName("committer")]
    public GitHubCommitter Committer { get; set; } = new();
}

internal sealed class GitHubCommitter
{
    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }
}

internal sealed class GitHubCompareEnvelope
{
    [JsonPropertyName("files")]
    public List<GitHubChangedFileEnvelope> Files { get; set; } = new();
}

internal sealed class GitHubChangedFileEnvelope
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("additions")]
    public int Additions { get; set; }

    [JsonPropertyName("deletions")]
    public int Deletions { get; set; }
}
