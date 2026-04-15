using System.Diagnostics;
using System.Text.Json;

namespace RegProbe.Application.Services;

internal sealed class TweakPromotionGateCatalogStore
{
    private const string CatalogPath = "research/promotion-gates.json";
    private const string BlockedWorklistPath = "registry-research-framework/audit/blocked-worklist.json";
    private const string MutationAuditLogPath = "registry-research-framework/audit/mutation-override-audit-log.jsonl";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly string? _docsRoot;
    private readonly string? _repoRoot;

    public TweakPromotionGateCatalogStore(string? docsRoot)
    {
        _docsRoot = docsRoot;
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
    }

    public TweakPromotionGateCatalog LoadCatalog()
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

    public BlockedWorklistCatalog LoadBlockedWorklist()
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

    public bool TryAppendMutationAuditLog(string action, TweakMutationDecision decision, bool contributorMode, out string? error)
    {
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(_repoRoot))
            {
                return true;
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
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Debug.WriteLine($"TweakPromotionGateCatalogService: failed to append mutation audit log: {ex}");
            return false;
        }
    }

    public static IReadOnlyDictionary<string, TweakPromotionGateEntry> BuildIndex(IEnumerable<TweakPromotionGateEntry> entries)
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

    public static IReadOnlyDictionary<string, BlockedWorklistEntry> BuildBlockedWorklistIndex(IEnumerable<BlockedWorklistEntry> entries)
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

    public static TweakPromotionGateEntry Clone(TweakPromotionGateEntry entry)
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

    public static BlockedWorklistEntry Clone(BlockedWorklistEntry entry)
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
