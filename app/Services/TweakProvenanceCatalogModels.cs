namespace RegProbe.App.Services;

public sealed class TweakProvenanceCatalog
{
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int TotalTweaks { get; set; }
    public int RepoBackedTweaks { get; set; }
    public int InternalsBackedTweaks { get; set; }
    public int ReviewNeededTweaks { get; set; }
    public List<TweakProvenanceSourceState> Sources { get; set; } = new();
    public List<TweakProvenanceEntry> Entries { get; set; } = new();
}

public sealed class TweakProvenanceSourceState
{
    public string Repository { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
}

public sealed class TweakProvenanceEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Risk { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool HasNohutoEvidence { get; set; }
    public bool HasWindowsInternalsContext { get; set; }
    public bool NeedsReview { get; set; }
    public string CoverageState { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> SourceRepositories { get; set; } = new();
    public List<string> MatchedTokens { get; set; } = new();
    public List<TweakProvenanceReference> References { get; set; } = new();
}

public sealed class TweakProvenanceReference
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
