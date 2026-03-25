using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace OpenTraceProject.App.Services;

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
    public int DocumentationChangedFiles { get; init; }
    public int ScriptChangedFiles { get; init; }
    public int SourceChangedFiles { get; init; }
    public int AssetChangedFiles { get; init; }
    public int DataChangedFiles { get; init; }
    public IReadOnlyList<NohutoCategoryInsight> TopCategories { get; init; } = Array.Empty<NohutoCategoryInsight>();
}

public enum NohutoRepositoryStateKind
{
    Unknown = 0,
    Baseline = 1,
    Unchanged = 2,
    Updated = 3,
    Failed = 4
}

public sealed class NohutoTrackedRepositoryState
{
    public string RepoId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string RoleSummary { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
    public string LastSeenCommitSha { get; set; } = string.Empty;
    public string LastSeenCommitMessage { get; set; } = string.Empty;
    public DateTimeOffset? LastSeenCommitDateUtc { get; set; }
    public DateTimeOffset LastCheckedAtUtc { get; set; }
    public bool CheckedSuccessfully { get; set; }
    public bool HasNewCommit { get; set; }
    public NohutoRepositoryStateKind StateKind { get; set; }
    public string Summary { get; set; } = string.Empty;
    public NohutoChangeAnalysis LastAnalysis { get; set; } = new();
}

public sealed class NohutoRepoScanState
{
    public DateTimeOffset LastCheckedAtUtc { get; set; }
    public string LastSummary { get; set; } = string.Empty;
    public List<NohutoTrackedRepositoryState> Repositories { get; set; } = new();
}

public sealed class NohutoRepoScanResult
{
    public bool CheckedSuccessfully { get; init; }
    public bool UsedCachedData { get; init; }
    public int UpdatedRepositoryCount { get; init; }
    public int BaselineRepositoryCount { get; init; }
    public DateTimeOffset CheckedAtUtc { get; init; }
    public string Summary { get; init; } = "Nohuto source scan unavailable.";
    public string JsonReportPath { get; init; } = string.Empty;
    public string MarkdownReportPath { get; init; } = string.Empty;
    public IReadOnlyList<NohutoTrackedRepositoryState> Repositories { get; init; } = Array.Empty<NohutoTrackedRepositoryState>();
}

public static class NohutoChangeAnalyzer
{
    public static NohutoChangeAnalysis Analyze(IEnumerable<NohutoChangedFile> changedFiles)
        => Analyze("win-registry", changedFiles);

    public static NohutoChangeAnalysis Analyze(string repoId, IEnumerable<NohutoChangedFile> changedFiles)
    {
        var definition = NohutoConfigurationSourceCatalog.Get(repoId);
        return Analyze(definition, changedFiles);
    }

    public static NohutoChangeAnalysis Analyze(NohutoRepositoryDefinition repository, IEnumerable<NohutoChangedFile> changedFiles)
    {
        if (repository is null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (changedFiles is null)
        {
            throw new ArgumentNullException(nameof(changedFiles));
        }

        var files = changedFiles
            .Where(static file => file is not null && !string.IsNullOrWhiteSpace(file.Path))
            .ToList();

        var scoreByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fileCountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var docsCount = 0;
        var scriptCount = 0;
        var sourceCount = 0;
        var assetCount = 0;
        var dataCount = 0;

        foreach (var file in files)
        {
            var normalizedPath = NormalizePath(file.Path);
            switch (ResolveChangeKind(normalizedPath))
            {
                case NohutoChangeKind.Documentation:
                    docsCount++;
                    break;
                case NohutoChangeKind.Script:
                    scriptCount++;
                    break;
                case NohutoChangeKind.Source:
                    sourceCount++;
                    break;
                case NohutoChangeKind.Asset:
                    assetCount++;
                    break;
                default:
                    dataCount++;
                    break;
            }

            var category = ResolveCategory(repository.Id, normalizedPath);
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
            .OrderByDescending(static item => item.Score)
            .ThenByDescending(static item => item.FileCount)
            .Take(5)
            .ToList();

        return new NohutoChangeAnalysis
        {
            TotalChangedFiles = files.Count,
            DocumentationChangedFiles = docsCount,
            ScriptChangedFiles = scriptCount,
            SourceChangedFiles = sourceCount,
            AssetChangedFiles = assetCount,
            DataChangedFiles = dataCount,
            TopCategories = topCategories
        };
    }

    private static string ResolveCategory(string repoId, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var topLevel = segments.Length > 0 ? segments[0].ToLowerInvariant() : string.Empty;
        var fileName = segments.Length > 0 ? segments[^1].ToLowerInvariant() : path.ToLowerInvariant();

        return repoId.ToLowerInvariant() switch
        {
            "win-config" => ResolveWinConfigCategory(topLevel, fileName),
            "win-registry" => ResolveWinRegistryCategory(path, fileName),
            "decompiled-pseudocode" => ResolveDecompiledCategory(topLevel, fileName),
            "regkit" => ResolveRegKitCategory(path, topLevel, fileName),
            _ => ResolveKeywordCategory(fileName)
        };
    }

    private static string ResolveWinConfigCategory(string topLevel, string fileName)
    {
        return topLevel switch
        {
            "affinities" => "Performance",
            "cleanup" => "Maintenance",
            "misc" => "Misc",
            "network" => "Network",
            "nvidia" => "Graphics",
            "peripheral" => "Peripheral",
            "policies" => "Policy",
            "power" => "Power",
            "privacy" => "Privacy",
            "security" => "Security",
            "system" => "System",
            "visibility" => "Display",
            _ => ResolveKeywordCategory(fileName)
        };
    }

    private static string ResolveWinRegistryCategory(string path, string fileName)
    {
        if (path.StartsWith("records/", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveKeywordCategory(fileName);
        }

        if (path.StartsWith("guide/", StringComparison.OrdinalIgnoreCase))
        {
            return "Documentation";
        }

        if (path.Contains("assets/dxg", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("assets/dwm", StringComparison.OrdinalIgnoreCase))
        {
            return "Graphics";
        }

        if (path.Contains("assets/intel-nic", StringComparison.OrdinalIgnoreCase))
        {
            return "Network";
        }

        if (path.Contains("assets/stornvme", StringComparison.OrdinalIgnoreCase))
        {
            return "Storage";
        }

        if (path.Contains("assets/mmcss", StringComparison.OrdinalIgnoreCase))
        {
            return "System";
        }

        return ResolveKeywordCategory(fileName);
    }

    private static string ResolveDecompiledCategory(string topLevel, string fileName)
    {
        return topLevel switch
        {
            "dxgkrnl" => "Graphics",
            "dxgmms2" => "Graphics",
            "dwm" => "Display",
            "dwmcore" => "Display",
            "win32kbase" => "Display",
            "win32kfull" => "Display",
            "usbhub3" => "Peripheral",
            "usbxhci" => "Peripheral",
            "usbhub" => "Peripheral",
            "stornvme" => "Storage",
            "mmcss" => "System",
            "wdf01000" => "System",
            "acpi" => "Power",
            "ntoskrnl" => "Kernel",
            _ => ResolveKeywordCategory(fileName)
        };
    }

    private static string ResolveRegKitCategory(string path, string topLevel, string fileName)
    {
        if (topLevel.Equals("installer", StringComparison.OrdinalIgnoreCase))
        {
            return "Installer";
        }

        if (path.Contains("trace", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("default", StringComparison.OrdinalIgnoreCase))
        {
            return "Research";
        }

        if (path.Contains("theme", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("icon", StringComparison.OrdinalIgnoreCase) ||
            topLevel.Equals("resources", StringComparison.OrdinalIgnoreCase))
        {
            return "UI";
        }

        if (path.Contains("ti", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("system", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("elevat", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("rights", StringComparison.OrdinalIgnoreCase))
        {
            return "Security";
        }

        if (topLevel.Equals("src", StringComparison.OrdinalIgnoreCase) ||
            topLevel.Equals("include", StringComparison.OrdinalIgnoreCase))
        {
            return "Registry";
        }

        return ResolveKeywordCategory(fileName);
    }

    private static string ResolveKeywordCategory(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Misc";
        }

        var key = value.ToLowerInvariant();

        if (key.Contains("power") || key.Contains("hiber") || key.Contains("sleep") || key.Contains("acpi"))
            return "Power";

        if (key.Contains("tcpip") || key.Contains("dnscache") || key.Contains("dns") || key.Contains("net") || key.Contains("nic") || key.Contains("ndis") || key.Contains("nla"))
            return "Network";

        if (key.Contains("defender") || key.Contains("lsa") || key.Contains("security") || key.Contains("tpm") || key.Contains("bitlocker") || key.Contains("crypt"))
            return "Security";

        if (key.Contains("privacy") || key.Contains("error-report") || key.Contains("telemetry"))
            return "Privacy";

        if (key.Contains("audio") || key.Contains("sound"))
            return "Audio";

        if (key.Contains("explorer") || key.Contains("desktop") || key.Contains("mouse") || key.Contains("visibility") || key.Contains("monitor"))
            return "Display";

        if (key.Contains("dxg") || key.Contains("graphics") || key.Contains("dwm") || key.Contains("gpu") || key.Contains("nvidia"))
            return "Graphics";

        if (key.Contains("perf") || key.Contains("stornvme") || key.Contains("storport") || key.Contains("storage") || key.Contains("disk") || key.Contains("nvme"))
            return "Storage";

        if (key.Contains("usb") || key.Contains("xhci") || key.Contains("kbd") || key.Contains("mou") || key.Contains("input") || key.Contains("touch") || key.Contains("pen") || key.Contains("peripheral"))
            return "Peripheral";

        if (key.Contains("policy"))
            return "Policy";

        if (key.Contains("cleanup"))
            return "Maintenance";

        if (key.Contains("trace") || key.Contains("record") || key.Contains("registry"))
            return "Research";

        if (key.Contains("kernel") || key.Contains("system") || key.Contains("mmcss") || key.Contains("service") || key.Contains("session") || key.Contains("pnp") || key.Contains("wdf"))
            return "System";

        return "Misc";
    }

    private static NohutoChangeKind ResolveChangeKind(string path)
    {
        if (path.StartsWith("guide/", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Documentation;
        }

        if (path.StartsWith("records/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Data;
        }

        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var byExtension = extension switch
        {
            ".ps1" or ".cmd" or ".bat" or ".vbs" or ".reg" or ".py" or ".iss" => NohutoChangeKind.Script,
            ".c" or ".cc" or ".cpp" or ".h" or ".hpp" or ".rc" or ".vcxproj" or ".filters" or ".props" or ".sln" => NohutoChangeKind.Source,
            ".ico" or ".png" or ".jpg" or ".jpeg" or ".svg" or ".bmp" => NohutoChangeKind.Asset,
            ".txt" or ".json" or ".csv" => NohutoChangeKind.Data,
            _ => NohutoChangeKind.Data
        };

        if (byExtension != NohutoChangeKind.Data)
        {
            return byExtension;
        }

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Asset;
        }

        if (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("include/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/src/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/include/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Source;
        }

        return NohutoChangeKind.Data;
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimStart('/');

    private enum NohutoChangeKind
    {
        Documentation,
        Script,
        Source,
        Asset,
        Data
    }
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
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

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
