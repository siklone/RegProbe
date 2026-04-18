namespace RegProbe.Application.Services;

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
    public Dictionary<string, int> ActionabilityCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> LaneCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> OrderedLanes { get; set; } = new();
    public Dictionary<string, BlockedLaneFocus> LaneFocus { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> TopActionableCandidates { get; set; } = new();
    public List<string> TopHoldCandidates { get; set; } = new();
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
