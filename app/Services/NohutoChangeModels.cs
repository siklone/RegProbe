namespace RegProbe.App.Services;

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
