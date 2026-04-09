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

public sealed class TweakRollbackGateStatus
{
    public bool RollbackDeclared { get; set; }
    public bool RollbackExecuted { get; set; }
    public bool RollbackVerified { get; set; }
    public string RollbackVerificationMethod { get; set; } = string.Empty;
    public string? RollbackFailureReason { get; set; }
}

public sealed class TweakFreshnessGateStatus
{
    public string Status { get; set; } = string.Empty;
    public bool RevalidationNeeded { get; set; }
    public string? StaleReason { get; set; }
    public string? LastKnownGoodBuild { get; set; }
}

public sealed class TweakMutationDecision
{
    public bool Allowed { get; set; }
    public bool OverrideRequested { get; set; }
    public bool OverrideUsed { get; set; }
    public string OverrideReason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    public TweakPromotionGateEntry Entry { get; set; } = new();
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
    public TweakRollbackGateStatus? RollbackStatus { get; set; }
    public TweakFreshnessGateStatus? FreshnessStatus { get; set; }

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
    private const string MutationAuditLogPath = "registry-research-framework/audit/mutation-override-audit-log.jsonl";
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

    public IEnumerable<TweakPromotionGateEntry> ListBlocked(string? reason = null)
    {
        var entries = _catalog.Entries
            .Where(entry => string.Equals(entry.PromotionState, "blocked", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(reason))
        {
            return entries.OrderBy(entry => entry.TweakId, StringComparer.OrdinalIgnoreCase);
        }

        return entries
            .Where(entry => entry.PromotionBlockers.Any(blocker => blocker.Contains(reason, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(entry => entry.TweakId, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<TweakPromotionGateEntry> ListRevalidationPending()
    {
        return _catalog.Entries
            .Where(entry => string.Equals(entry.PromotionState, "revalidation-pending", StringComparison.OrdinalIgnoreCase)
                            || (entry.FreshnessStatus?.RevalidationNeeded ?? false))
            .OrderBy(entry => entry.TweakId, StringComparer.OrdinalIgnoreCase);
    }

    public TweakMutationDecision EvaluateApplyRequest(string tweakId, bool overrideRequested = false, string? overrideReason = null, bool? contributorMode = null)
    {
        var entry = ResolveOrFallback(tweakId);
        var contributorModeEnabled = contributorMode ?? ContributorMode.IsEnabled;
        var allowedWithoutOverride =
            string.Equals(entry.TweakOrigin, "legacy-curated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.PromotionState, "promoted", StringComparison.OrdinalIgnoreCase);
        var overrideUsed =
            !allowedWithoutOverride
            && overrideRequested
            && contributorModeEnabled
            && entry.DebugOverrideAllowed;

        var decision = new TweakMutationDecision
        {
            Allowed = allowedWithoutOverride || overrideUsed,
            OverrideRequested = overrideRequested,
            OverrideUsed = overrideUsed,
            OverrideReason = overrideReason?.Trim() ?? string.Empty,
            Message = allowedWithoutOverride
                ? "apply-allowed"
                : overrideUsed
                    ? "apply-override-allowed"
                    : $"promotion-state:{entry.PromotionState}",
            Entry = entry,
        };

        if (overrideRequested)
        {
            AppendMutationAuditLog("apply", decision, contributorModeEnabled);
        }

        return decision;
    }

    public TweakMutationDecision EvaluateRollbackRequest(string tweakId, bool overrideRequested = false, string? overrideReason = null, bool? contributorMode = null)
    {
        var decision = EvaluateApplyRequest(tweakId, overrideRequested, overrideReason, contributorMode);
        decision.Message = decision.Allowed ? "rollback-allowed" : decision.Message;

        if (!decision.Allowed)
        {
            return decision;
        }

        var rollback = decision.Entry.RollbackStatus;
        if (rollback is null && !string.Equals(decision.Entry.TweakOrigin, "legacy-curated", StringComparison.OrdinalIgnoreCase))
        {
            decision.Allowed = false;
            decision.Message = "rollback-not-declared";
            return decision;
        }

        if (rollback is not null)
        {
            if (!rollback.RollbackDeclared && !rollback.RollbackExecuted
                && !string.Equals(decision.Entry.TweakOrigin, "legacy-curated", StringComparison.OrdinalIgnoreCase))
            {
                decision.Allowed = false;
                decision.Message = "rollback-not-declared";
            }
            else
            {
                if (rollback.RollbackDeclared && !rollback.RollbackExecuted)
                {
                    decision.Warnings.Add("rollback-declared-but-not-executed");
                }

                if (!rollback.RollbackVerified)
                {
                    decision.Warnings.Add("rollback-unverified");
                }
            }
        }

        if (overrideRequested || decision.Warnings.Count > 0)
        {
            AppendMutationAuditLog("rollback", decision, contributorMode ?? ContributorMode.IsEnabled);
        }

        return decision;
    }

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
            RollbackStatus = entry.RollbackStatus is null
                ? null
                : new TweakRollbackGateStatus
                {
                    RollbackDeclared = entry.RollbackStatus.RollbackDeclared,
                    RollbackExecuted = entry.RollbackStatus.RollbackExecuted,
                    RollbackVerified = entry.RollbackStatus.RollbackVerified,
                    RollbackVerificationMethod = entry.RollbackStatus.RollbackVerificationMethod,
                    RollbackFailureReason = entry.RollbackStatus.RollbackFailureReason,
                },
            FreshnessStatus = entry.FreshnessStatus is null
                ? null
                : new TweakFreshnessGateStatus
                {
                    Status = entry.FreshnessStatus.Status,
                    RevalidationNeeded = entry.FreshnessStatus.RevalidationNeeded,
                    StaleReason = entry.FreshnessStatus.StaleReason,
                    LastKnownGoodBuild = entry.FreshnessStatus.LastKnownGoodBuild,
                },
        };
    }

    private void AppendMutationAuditLog(string action, TweakMutationDecision decision, bool contributorMode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_repoRoot))
            {
                return;
            }

            var path = Path.Combine(_repoRoot, MutationAuditLogPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var payload = new
            {
                timestamp_utc = DateTimeOffset.UtcNow.ToString("O"),
                action,
                candidate_id = decision.Entry.CandidateId,
                tweak_id = decision.Entry.TweakId,
                promotion_state = decision.Entry.PromotionState,
                override_requested = decision.OverrideRequested,
                override_used = decision.OverrideUsed,
                override_reason = string.IsNullOrWhiteSpace(decision.OverrideReason) ? "unspecified" : decision.OverrideReason,
                contributor_mode = contributorMode,
                allowed = decision.Allowed,
                message = decision.Message,
                warnings = decision.Warnings,
            };

            File.AppendAllText(path, JsonSerializer.Serialize(payload, JsonOptions) + Environment.NewLine);
        }
        catch
        {
        }
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
