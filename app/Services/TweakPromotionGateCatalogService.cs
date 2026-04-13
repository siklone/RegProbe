using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using RegProbe.App.Utilities;

namespace RegProbe.App.Services;

public sealed class TweakPromotionGateCatalog
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string EvaluatorVersion { get; set; } = string.Empty;
    public string GeneratedUtc { get; set; } = string.Empty;
    public TweakPromotionGateSummary Summary { get; set; } = new();
    public List<TweakPromotionGateEntry> Entries { get; set; } = new();
}

public sealed class BlockedWorklistCatalog
{
    public string GeneratedAt { get; set; } = string.Empty;
    public int BlockedCount { get; set; }
    public Dictionary<string, int> LaneCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, BlockedLaneFocus> LaneFocus { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> TopActionableCandidates { get; set; } = new();
    public List<BlockedWorklistEntry> Items { get; set; } = new();
}

public sealed class BlockedLaneFocus
{
    public string CandidateId { get; set; } = string.Empty;
    public string SuggestedCommand { get; set; } = string.Empty;
    public string NextActionHint { get; set; } = string.Empty;
}

public sealed class BlockedWorklistEntry
{
    public string CandidateId { get; set; } = string.Empty;
    public string FeatureArea { get; set; } = string.Empty;
    public string NextMissingLayer { get; set; } = string.Empty;
    public string Actionability { get; set; } = string.Empty;
    public int PriorityScore { get; set; }
    public int BlockerCount { get; set; }
    public List<string> PromotionBlockers { get; set; } = new();
    public string KeyPath { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public List<string> RecentAuditArtifacts { get; set; } = new();
    public string SuggestedCommand { get; set; } = string.Empty;
    public string NextActionHint { get; set; } = string.Empty;
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
                    ? TweakIngestAllowed
                        ? "Promoted for apply/rollback."
                        : "Promoted for research tracking; app ingest disabled."
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
    private const string BlockedWorklistPath = "registry-research-framework/audit/blocked-worklist.json";
    private const string MutationAuditLogPath = "registry-research-framework/audit/mutation-override-audit-log.jsonl";
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;
    private readonly TweakPromotionGateCatalog _catalog;
    private readonly BlockedWorklistCatalog _blockedWorklist;
    private readonly IReadOnlyDictionary<string, TweakPromotionGateEntry> _index;
    private readonly IReadOnlyDictionary<string, BlockedWorklistEntry> _blockedWorklistIndex;

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
        _blockedWorklist = LoadBlockedWorklist();
        _index = BuildIndex(_catalog.Entries);
        _blockedWorklistIndex = BuildBlockedWorklistIndex(_blockedWorklist.Items);
    }

    public TweakPromotionGateCatalog Catalog => _catalog;
    public BlockedWorklistCatalog BlockedWorklist => _blockedWorklist;
    public string? LastMutationAuditError { get; private set; }

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

    public IEnumerable<BlockedWorklistEntry> ListBlockedWorklist(
        string? reason = null,
        string? lane = null,
        bool actionableOnly = false,
        int? top = null)
    {
        IEnumerable<BlockedWorklistEntry> entries = _blockedWorklist.Items;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            entries = entries.Where(entry => entry.PromotionBlockers.Any(blocker =>
                blocker.Contains(reason, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(lane))
        {
            entries = entries.Where(entry => string.Equals(entry.NextMissingLayer, lane, StringComparison.OrdinalIgnoreCase));
        }

        if (actionableOnly)
        {
            entries = entries.Where(entry => string.Equals(entry.Actionability, "active", StringComparison.OrdinalIgnoreCase));
        }

        entries = entries
            .OrderByDescending(entry => entry.PriorityScore)
            .ThenBy(entry => entry.BlockerCount)
            .ThenBy(entry => entry.CandidateId, StringComparer.OrdinalIgnoreCase);

        if (top is > 0)
        {
            entries = entries.Take(top.Value);
        }

        return entries;
    }

    public bool TryResolveBlockedWorklist(string candidateId, out BlockedWorklistEntry entry)
    {
        entry = new BlockedWorklistEntry();
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            return false;
        }

        if (!_blockedWorklistIndex.TryGetValue(candidateId, out var match))
        {
            return false;
        }

        entry = Clone(match);
        return true;
    }

    public TweakMutationDecision EvaluateApplyRequest(string tweakId, bool overrideRequested = false, string? overrideReason = null, bool? contributorMode = null)
    {
        var entry = ResolveOrFallback(tweakId);
        var contributorModeEnabled = contributorMode ?? ContributorMode.IsEnabled;
        var allowedWithoutOverride =
            string.Equals(entry.TweakOrigin, "legacy-curated", StringComparison.OrdinalIgnoreCase)
            || entry.TweakIngestAllowed;
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

    public void Apply<T>(IEnumerable<T> tweaks) where T : class
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        foreach (var tweak in tweaks)
        {
            if (!TryCreateApplyTarget(tweak, out var tweakId, out var applyResearchPromotionGate))
            {
                continue;
            }

            applyResearchPromotionGate(ResolveOrFallback(tweakId));
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

    private BlockedWorklistCatalog LoadBlockedWorklist()
    {
        var path = ResolvePath(BlockedWorklistPath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new BlockedWorklistCatalog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BlockedWorklistCatalog>(json, JsonOptions)
                   ?? new BlockedWorklistCatalog();
        }
        catch
        {
            return new BlockedWorklistCatalog();
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

    private static IReadOnlyDictionary<string, BlockedWorklistEntry> BuildBlockedWorklistIndex(IEnumerable<BlockedWorklistEntry> entries)
    {
        var index = new Dictionary<string, BlockedWorklistEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.CandidateId) && !index.ContainsKey(entry.CandidateId))
            {
                index[entry.CandidateId] = entry;
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

    private static BlockedWorklistEntry Clone(BlockedWorklistEntry entry)
    {
        return new BlockedWorklistEntry
        {
            CandidateId = entry.CandidateId,
            FeatureArea = entry.FeatureArea,
            NextMissingLayer = entry.NextMissingLayer,
            Actionability = entry.Actionability,
            PriorityScore = entry.PriorityScore,
            BlockerCount = entry.BlockerCount,
            PromotionBlockers = entry.PromotionBlockers.ToList(),
            KeyPath = entry.KeyPath,
            ValueName = entry.ValueName,
            RecentAuditArtifacts = entry.RecentAuditArtifacts.ToList(),
            SuggestedCommand = entry.SuggestedCommand,
            NextActionHint = entry.NextActionHint,
        };
    }

    private static bool TryCreateApplyTarget<T>(
        T tweak,
        out string tweakId,
        out Action<TweakPromotionGateEntry> applyResearchPromotionGate) where T : class
    {
        tweakId = string.Empty;
        applyResearchPromotionGate = static _ => { };

        if (tweak is null)
        {
            return false;
        }

        var tweakType = tweak.GetType();
        var idProperty = tweakType.GetProperty("Id");
        if (idProperty?.GetValue(tweak) is not string id || string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var applyMethod = tweakType.GetMethod("ApplyResearchPromotionGate", [typeof(TweakPromotionGateEntry)]);
        if (applyMethod is null)
        {
            return false;
        }

        tweakId = id;
        applyResearchPromotionGate = entry => applyMethod.Invoke(tweak, [entry]);
        return true;
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
            LastMutationAuditError = null;
        }
        catch (Exception ex)
        {
            LastMutationAuditError = ex.Message;
            Debug.WriteLine($"TweakPromotionGateCatalogService: failed to append mutation audit log: {ex}");
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
