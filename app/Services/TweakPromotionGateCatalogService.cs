using RegProbe.Application.Utilities;

namespace RegProbe.Application.Services;

public sealed class TweakPromotionGateCatalogService
{
    private readonly TweakPromotionGateCatalogStore _store;
    private readonly TweakPromotionGateCatalog _catalog;
    private readonly BlockedWorklistCatalog _blockedWorklist;
    private readonly TweakPromotionGateApplicator _applicator = new();
    private readonly TweakPromotionGateQueryService _queryService;
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
        _queryService = new TweakPromotionGateQueryService(
            _catalog,
            _blockedWorklist,
            _index,
            _blockedWorklistIndex);
    }

    public TweakPromotionGateCatalog Catalog => _catalog;
    public BlockedWorklistCatalog BlockedWorklist => _blockedWorklist;
    public string? LastMutationAuditError { get; private set; }

    public IEnumerable<TweakPromotionGateEntry> ListBlocked(string? reason = null)
        => _queryService.ListBlocked(reason);

    public IEnumerable<TweakPromotionGateEntry> ListRevalidationPending()
        => _queryService.ListRevalidationPending();

    public IEnumerable<BlockedWorklistEntry> ListBlockedWorklist(
        string? reason = null,
        string? lane = null,
        string? actionability = null,
        bool actionableOnly = false,
        int? top = null)
        => _queryService.ListBlockedWorklist(reason, lane, actionability, actionableOnly, top);

    public bool TryResolveBlockedWorklist(string candidateId, out BlockedWorklistEntry entry)
        => _queryService.TryResolveBlockedWorklist(candidateId, out entry);

    public TweakMutationDecision EvaluateApplyRequest(string tweakId, bool overrideRequested = false, string? overrideReason = null, bool? contributorMode = null)
    {
        var entry = ResolveOrFallback(tweakId);
        var contributorModeEnabled = contributorMode ?? ContributorMode.IsEnabled;
        var decision = TweakPromotionGateMutationEvaluator.EvaluateApply(
            entry,
            overrideRequested,
            overrideReason,
            contributorModeEnabled);

        if (overrideRequested)
        {
            AppendMutationAuditLog("apply", decision, contributorModeEnabled);
        }

        return decision;
    }

    public TweakMutationDecision EvaluateRollbackRequest(string tweakId, bool overrideRequested = false, string? overrideReason = null, bool? contributorMode = null)
    {
        var contributorModeEnabled = contributorMode ?? ContributorMode.IsEnabled;
        var decision = TweakPromotionGateMutationEvaluator.EvaluateRollback(
            ResolveOrFallback(tweakId),
            overrideRequested,
            overrideReason,
            contributorModeEnabled);

        if (overrideRequested || decision.Warnings.Count > 0)
        {
            AppendMutationAuditLog("rollback", decision, contributorModeEnabled);
        }

        return decision;
    }

    public void Apply<T>(IEnumerable<T> tweaks) where T : class
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        foreach (var tweak in tweaks)
        {
            if (!_applicator.TryCreateTarget(tweak, out var tweakId, out var applyResearchPromotionGate))
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

}
