using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RegProbe.App.Utilities;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class TweakEvidenceClassCatalog
{
    public string GeneratedUtc { get; set; } = string.Empty;
    public TweakEvidenceClassSummary Summary { get; set; } = new();
    public Dictionary<string, TweakEvidenceClassDefinition> Classes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<TweakEvidenceClassEntry> Entries { get; set; } = new();
}

public sealed class TweakEvidenceClassSummary
{
    public int TotalRecords { get; set; }
    public Dictionary<string, int> ClassCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> ActionStateCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TweakEvidenceClassDefinition
{
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class TweakEvidenceClassEntry
{
    public string RecordId { get; set; } = string.Empty;
    public string TweakId { get; set; } = string.Empty;
    public string RecordStatus { get; set; } = string.Empty;
    public string EvidenceClass { get; set; } = string.Empty;
    public string ClassLabel { get; set; } = string.Empty;
    public string ClassTitle { get; set; } = string.Empty;
    public string ClassDescription { get; set; } = string.Empty;
    public bool ShowInApp { get; set; }
    public bool IsActionable { get; set; }
    public bool IsArchived { get; set; }
    public string ActionState { get; set; } = string.Empty;
    public string GatingReason { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
    public string AppMappingStatus { get; set; } = string.Empty;
    public bool RestoreStoryKnown { get; set; }
    public TweakEvidenceProofBlock? ValidatedSemantics { get; set; }
    public TweakEvidenceProofBlock? RuntimeProof { get; set; }
    public TweakEvidenceProofBlock? UpstreamLineage { get; set; }

    public static TweakEvidenceClassEntry CreateFallback(string tweakId) => new()
    {
        RecordId = tweakId,
        TweakId = tweakId,
        RecordStatus = "validated",
        EvidenceClass = "D",
        ClassLabel = "Class D",
        ClassTitle = "Key Known, Value Semantics Unknown",
        ClassDescription = "No derived evidence-class entry was found for this tweak yet.",
        ShowInApp = true,
        IsActionable = false,
        IsArchived = false,
        ActionState = "research-gated",
        GatingReason = "No derived evidence class is loaded for this tweak yet.",
        ValidatedSemantics = new TweakEvidenceProofBlock { Summary = "No derived semantics summary is available yet." },
        RuntimeProof = new TweakEvidenceProofBlock { Summary = "No derived runtime summary is available yet." },
        UpstreamLineage = new TweakEvidenceProofBlock { Summary = "No derived upstream lineage summary is available yet." },
    };
}

public sealed class TweakEvidenceProofBlock
{
    public string Summary { get; set; } = string.Empty;
    public bool HasValidationProof { get; set; }
    public bool HasSemanticsEvidence { get; set; }
    public bool NeedsVmValidation { get; set; }
    public bool HasRuntimeEvidence { get; set; }
    public bool HasNohutoLineage { get; set; }
    public List<TweakEvidenceLink> Links { get; set; } = new();
    public string PrimarySourceText { get; set; } = string.Empty;
}

public sealed class TweakEvidenceLink
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class TweakEvidenceClassCatalogService
{
    private const string CatalogPath = "research/evidence-classes.json";
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;
    private readonly TweakEvidenceClassCatalog _catalog;
    private readonly IReadOnlyDictionary<string, TweakEvidenceClassEntry> _index;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TweakEvidenceClassCatalogService(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
        _catalog = LoadCatalog();
        _index = BuildIndex(_catalog.Entries);
    }

    public TweakEvidenceClassCatalog Catalog => _catalog;

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id))
            {
                continue;
            }

            if (_index.TryGetValue(tweak.Id, out var entry))
            {
                tweak.ApplyEvidenceClassification(CloneWithResolvedLinks(entry));
                continue;
            }

            tweak.ApplyEvidenceClassification(TweakEvidenceClassEntry.CreateFallback(tweak.Id));
        }
    }

    private TweakEvidenceClassCatalog LoadCatalog()
    {
        var path = ResolvePath(CatalogPath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new TweakEvidenceClassCatalog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TweakEvidenceClassCatalog>(json, JsonOptions)
                   ?? new TweakEvidenceClassCatalog();
        }
        catch
        {
            return new TweakEvidenceClassCatalog();
        }
    }

    private static IReadOnlyDictionary<string, TweakEvidenceClassEntry> BuildIndex(IEnumerable<TweakEvidenceClassEntry> entries)
    {
        var index = new Dictionary<string, TweakEvidenceClassEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.RecordId) && !index.ContainsKey(entry.RecordId))
            {
                index[entry.RecordId] = entry;
            }

            if (!string.IsNullOrWhiteSpace(entry.TweakId) && !index.ContainsKey(entry.TweakId))
            {
                index[entry.TweakId] = entry;
            }
        }

        return index;
    }

    private TweakEvidenceClassEntry CloneWithResolvedLinks(TweakEvidenceClassEntry entry)
    {
        return new TweakEvidenceClassEntry
        {
            RecordId = entry.RecordId,
            TweakId = entry.TweakId,
            RecordStatus = entry.RecordStatus,
            EvidenceClass = entry.EvidenceClass,
            ClassLabel = entry.ClassLabel,
            ClassTitle = entry.ClassTitle,
            ClassDescription = entry.ClassDescription,
            ShowInApp = entry.ShowInApp,
            IsActionable = entry.IsActionable,
            IsArchived = entry.IsArchived,
            ActionState = entry.ActionState,
            GatingReason = entry.GatingReason,
            Confidence = entry.Confidence,
            AppMappingStatus = entry.AppMappingStatus,
            RestoreStoryKnown = entry.RestoreStoryKnown,
            ValidatedSemantics = CloneBlock(entry.ValidatedSemantics),
            RuntimeProof = CloneBlock(entry.RuntimeProof),
            UpstreamLineage = CloneBlock(entry.UpstreamLineage),
        };
    }

    private TweakEvidenceProofBlock? CloneBlock(TweakEvidenceProofBlock? block)
    {
        if (block is null)
        {
            return null;
        }

        var links = block.Links
            .Select(link => new TweakEvidenceLink
            {
                Title = link.Title,
                Url = ResolvePath(link.Url),
                Kind = link.Kind,
                Summary = link.Summary,
            })
            .Where(link => !string.IsNullOrWhiteSpace(link.Url))
            .ToList();

        return new TweakEvidenceProofBlock
        {
            Summary = block.Summary,
            HasValidationProof = block.HasValidationProof,
            HasSemanticsEvidence = block.HasSemanticsEvidence,
            NeedsVmValidation = block.NeedsVmValidation,
            HasRuntimeEvidence = block.HasRuntimeEvidence,
            HasNohutoLineage = block.HasNohutoLineage,
            Links = links,
            PrimarySourceText = BuildPrimarySourceText(links.FirstOrDefault()),
        };
    }

    private string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return path;
        }

        var normalized = path.Replace('/', Path.DirectorySeparatorChar).Trim();
        if (Path.IsPathRooted(normalized))
        {
            return File.Exists(normalized) ? normalized : path;
        }

        if (!string.IsNullOrWhiteSpace(_repoRoot))
        {
            var repoPath = Path.Combine(_repoRoot, normalized.TrimStart(Path.DirectorySeparatorChar));
            if (File.Exists(repoPath))
            {
                return repoPath;
            }
        }

        if (!string.IsNullOrWhiteSpace(_docsRoot))
        {
            var trimmed = normalized.StartsWith($"Docs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                ? normalized[(5)..]
                : normalized;
            var docsPath = Path.Combine(_docsRoot, trimmed.TrimStart(Path.DirectorySeparatorChar));
            if (File.Exists(docsPath))
            {
                return docsPath;
            }
        }

        return path;
    }

    private static string BuildPrimarySourceText(TweakEvidenceLink? link)
    {
        if (link is null || string.IsNullOrWhiteSpace(link.Url))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(link.Title)
            ? link.Url
            : $"{link.Title}: {link.Url}";
    }
}
