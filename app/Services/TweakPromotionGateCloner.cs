namespace RegProbe.Application.Services;

internal static class TweakPromotionGateCloner
{
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
            ScoreBreakdown = Clone(entry.ScoreBreakdown),
            RollbackStatus = Clone(entry.RollbackStatus),
            FreshnessStatus = Clone(entry.FreshnessStatus),
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

    private static TweakPromotionScoreBreakdown? Clone(TweakPromotionScoreBreakdown? score)
        => score is null
            ? null
            : new TweakPromotionScoreBreakdown
            {
                OverallScore = score.OverallScore,
                StaticEvidenceStrength = score.StaticEvidenceStrength,
                RuntimeEvidenceStrength = score.RuntimeEvidenceStrength,
                RollbackClarity = score.RollbackClarity,
                BlastRadius = score.BlastRadius,
                TweakSuitability = score.TweakSuitability,
                PrivilegeComplexity = score.PrivilegeComplexity,
                BuildSpecificity = score.BuildSpecificity,
                SiblingExpansionValue = score.SiblingExpansionValue,
                BenchPriority = score.BenchPriority,
            };

    private static TweakRollbackGateStatus? Clone(TweakRollbackGateStatus? rollback)
        => rollback is null
            ? null
            : new TweakRollbackGateStatus
            {
                RollbackDeclared = rollback.RollbackDeclared,
                RollbackExecuted = rollback.RollbackExecuted,
                RollbackVerified = rollback.RollbackVerified,
                RollbackVerificationMethod = rollback.RollbackVerificationMethod,
                RollbackFailureReason = rollback.RollbackFailureReason,
            };

    private static TweakFreshnessGateStatus? Clone(TweakFreshnessGateStatus? freshness)
        => freshness is null
            ? null
            : new TweakFreshnessGateStatus
            {
                Status = freshness.Status,
                RevalidationNeeded = freshness.RevalidationNeeded,
                StaleReason = freshness.StaleReason,
                LastKnownGoodBuild = freshness.LastKnownGoodBuild,
            };
}
