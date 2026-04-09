using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RegProbe.App.Utilities;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class TweakPromotionGateCatalog
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string EvaluatorVersion { get; set; } = string.Empty;
    public string GeneratedUtc { get; set; } = string.Empty;
    public TweakPromotionGateSummary Summary { get; set; } = new();
    public List<TweakPromotionGateEntry> Entries { get; set; } = new();
}

public sealed class TweakPromotionGateSummary
{
    public int TotalRecords { get; set; }
    public Dictionary<string, int> PromotionStateCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TweakPromotionScoreBreakdown
{
    public double OverallScore { get; set; }
    public int StaticEvidenceStrength { get; set; }
    public int RuntimeEvidenceStrength { get; set; }
    public int RollbackClarity { get; set; }
    public int BlastRadius { get; set; }
    public int TweakSuitability { get; set; }
    public int PrivilegeComplexity { get; set; }
    public int BuildSpecificity { get; set; }
    public int SiblingExpansionValue { get; set; }
    public int BenchPriority { get; set; }
}

public sealed class TweakPromotionGateEntry
{
    public string CandidateId { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string TweakId { get; set; } = string.Empty;
    public string TweakOrigin { get; set; } = string.Empty;
    public string PromotionState { get; set; } = string.Empty;
    public List<string> PromotionBlockers { get; set; } = new();
    public bool RecordPromotionAllowed { get; set; }
    public bool TweakIngestAllowed { get; set; }
    public bool ApplyAllowed { get; set; }
    public string AppMappingStatus { get; set; } = string.Empty;
    public string NextMissingLayer { get; set; } = string.Empty;
    public bool DebugOverrideAllowed { get; set; }
    public string SchemaCompatibilityMode { get; set; } = string.Empty;
    public string EvaluatorVersion { get; set; } = string.Empty;
    public TweakPromotionScoreBreakdown? ScoreBreakdown { get; set; }

    public string GatingReason =>
        !string.IsNullOrWhiteSpace(NextMissingLayer) && !string.Equals(NextMissingLayer, "none", StringComparison.OrdinalIgnoreCase)
            ? $"Promotion blocked by {NextMissingLayer}."
            : PromotionBlockers.Count > 0
                ? $"Promotion blocked by {string.Join(", ", PromotionBlockers)}."
                : string.Equals(PromotionState, "promoted", StringComparison.OrdinalIgnoreCase)
                    ? "Promoted for apply/rollback."
                    : $"Promotion state: {PromotionState}.";

    public static TweakPromotionGateEntry CreateFallback(string tweakId) => new()
    {
        CandidateId = tweakId,
        RecordId = tweakId,
        TweakId = tweakId,
        TweakOrigin = "legacy-curated",
        PromotionState = "promoted",
        RecordPromotionAllowed = true,
        TweakIngestAllowed = true,
        ApplyAllowed = true,
        AppMappingStatus = "matches-research",
        NextMissingLayer = "none",
        DebugOverrideAllowed = false,
        SchemaCompatibilityMode = "native",
    };
}

public sealed class TweakPromotionGateCatalogService
{
    private const string CatalogPath = "research/promotion-gates.json";
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;
    private readonly TweakPromotionGateCatalog _catalog;
    private readonly IReadOnlyDictionary<string, TweakPromotionGateEntry> _index;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TweakPromotionGateCatalogService(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
        _catalog = LoadCatalog();
        _index = BuildIndex(_catalog.Entries);
    }

    public TweakPromotionGateCatalog Catalog => _catalog;

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id))
            {
                continue;
            }

            tweak.ApplyResearchPromotionGate(ResolveOrFallback(tweak.Id));
        }
    }

    public bool TryResolve(string tweakId, out TweakPromotionGateEntry entry)
    {
        entry = TweakPromotionGateEntry.CreateFallback(tweakId);
        if (string.IsNullOrWhiteSpace(tweakId))
        {
            return false;
        }

        if (!_index.TryGetValue(tweakId, out var match))
        {
            return false;
        }

        entry = Clone(match);
        return true;
    }

    public TweakPromotionGateEntry ResolveOrFallback(string tweakId)
    {
        if (TryResolve(tweakId, out var entry))
        {
            return entry;
        }

        return TweakPromotionGateEntry.CreateFallback(tweakId);
    }

    private TweakPromotionGateCatalog LoadCatalog()
    {
        var path = ResolvePath(CatalogPath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new TweakPromotionGateCatalog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TweakPromotionGateCatalog>(json, JsonOptions)
                   ?? new TweakPromotionGateCatalog();
        }
        catch
        {
            return new TweakPromotionGateCatalog();
        }
    }

    private static IReadOnlyDictionary<string, TweakPromotionGateEntry> BuildIndex(IEnumerable<TweakPromotionGateEntry> entries)
    {
        var index = new Dictionary<string, TweakPromotionGateEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            foreach (var key in new[]
                     {
                         entry.CandidateId,
                         entry.RecordId,
                         entry.TweakId,
                     }.Where(static value => !string.IsNullOrWhiteSpace(value)))
            {
                if (!index.ContainsKey(key))
                {
                    index[key] = entry;
                }
            }
        }

        return index;
    }

    private static TweakPromotionGateEntry Clone(TweakPromotionGateEntry entry)
    {
        return new TweakPromotionGateEntry
        {
            CandidateId = entry.CandidateId,
            RecordId = entry.RecordId,
            TweakId = entry.TweakId,
            TweakOrigin = entry.TweakOrigin,
            PromotionState = entry.PromotionState,
            PromotionBlockers = entry.PromotionBlockers.ToList(),
            RecordPromotionAllowed = entry.RecordPromotionAllowed,
            TweakIngestAllowed = entry.TweakIngestAllowed,
            ApplyAllowed = entry.ApplyAllowed,
            AppMappingStatus = entry.AppMappingStatus,
            NextMissingLayer = entry.NextMissingLayer,
            DebugOverrideAllowed = entry.DebugOverrideAllowed,
            SchemaCompatibilityMode = entry.SchemaCompatibilityMode,
            EvaluatorVersion = entry.EvaluatorVersion,
            ScoreBreakdown = entry.ScoreBreakdown is null
                ? null
                : new TweakPromotionScoreBreakdown
                {
                    OverallScore = entry.ScoreBreakdown.OverallScore,
                    StaticEvidenceStrength = entry.ScoreBreakdown.StaticEvidenceStrength,
                    RuntimeEvidenceStrength = entry.ScoreBreakdown.RuntimeEvidenceStrength,
                    RollbackClarity = entry.ScoreBreakdown.RollbackClarity,
                    BlastRadius = entry.ScoreBreakdown.BlastRadius,
                    TweakSuitability = entry.ScoreBreakdown.TweakSuitability,
                    PrivilegeComplexity = entry.ScoreBreakdown.PrivilegeComplexity,
                    BuildSpecificity = entry.ScoreBreakdown.BuildSpecificity,
                    SiblingExpansionValue = entry.ScoreBreakdown.SiblingExpansionValue,
                    BenchPriority = entry.ScoreBreakdown.BenchPriority,
                },
        };
    }

    private string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized))
        {
            return normalized;
        }

        if (!string.IsNullOrWhiteSpace(_docsRoot))
        {
            var docsRelative = Path.Combine(_docsRoot, normalized);
            if (File.Exists(docsRelative))
            {
                return docsRelative;
            }
        }

        if (!string.IsNullOrWhiteSpace(_repoRoot))
        {
            return Path.Combine(_repoRoot, normalized);
        }

        return normalized;
    }
}
