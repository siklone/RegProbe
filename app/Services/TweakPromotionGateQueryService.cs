namespace RegProbe.Application.Services;

internal sealed class TweakPromotionGateQueryService
{
    private readonly BlockedWorklistCatalog _blockedWorklist;
    private readonly IReadOnlyDictionary<string, BlockedWorklistEntry> _blockedWorklistIndex;
    private readonly TweakPromotionGateCatalog _catalog;

    public TweakPromotionGateQueryService(
        TweakPromotionGateCatalog catalog,
        BlockedWorklistCatalog blockedWorklist,
        IReadOnlyDictionary<string, TweakPromotionGateEntry> index,
        IReadOnlyDictionary<string, BlockedWorklistEntry> blockedWorklistIndex)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _blockedWorklist = blockedWorklist ?? throw new ArgumentNullException(nameof(blockedWorklist));
        Index = index ?? throw new ArgumentNullException(nameof(index));
        _blockedWorklistIndex = blockedWorklistIndex ?? throw new ArgumentNullException(nameof(blockedWorklistIndex));
    }

    public IReadOnlyDictionary<string, TweakPromotionGateEntry> Index { get; }

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
}
