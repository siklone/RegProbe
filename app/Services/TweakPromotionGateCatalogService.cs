using System.Reflection;
using RegProbe.Application.Utilities;

namespace RegProbe.Application.Services;

public sealed class TweakPromotionGateCatalogService
{
    private readonly TweakPromotionGateCatalogStore _store;
    private readonly TweakPromotionGateCatalog _catalog;
    private readonly BlockedWorklistCatalog _blockedWorklist;
    private readonly IReadOnlyDictionary<string, TweakPromotionGateEntry> _index;
    private readonly IReadOnlyDictionary<string, BlockedWorklistEntry> _blockedWorklistIndex;

    public TweakPromotionGateCatalogService(string? docsRoot = null)
    {
        var bootstrap = TweakPromotionGateCatalogBootstrap.Create(docsRoot);
        _store = bootstrap.Store;
        _catalog = bootstrap.Catalog;
        _blockedWorklist = bootstrap.BlockedWorklist;
        _index = bootstrap.Index;
        _blockedWorklistIndex = bootstrap.BlockedWorklistIndex;
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
        string? actionability = null,
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

        if (!string.IsNullOrWhiteSpace(actionability))
        {
            entries = entries.Where(entry => string.Equals(entry.Actionability, actionability, StringComparison.OrdinalIgnoreCase));
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

        entry = TweakPromotionGateCatalogStore.Clone(match);
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

        entry = TweakPromotionGateCatalogStore.Clone(match);
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

    private void AppendMutationAuditLog(string action, TweakMutationDecision decision, bool contributorMode)
    {
        if (_store.TryAppendMutationAuditLog(action, decision, contributorMode, out var error))
        {
            LastMutationAuditError = null;
            return;
        }

        LastMutationAuditError = error;
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
}
