namespace RegProbe.Application.Services;

internal sealed class TweakPromotionGateCatalogStore
{
    private const string CatalogPath = "research/promotion-gates.json";
    private const string BlockedWorklistPath = "registry-research-framework/audit/blocked-worklist.json";
    private const string MutationAuditLogPath = "registry-research-framework/audit/mutation-override-audit-log.jsonl";

    private readonly TweakPromotionGateAuditLogWriter _auditLogWriter;
    private readonly TweakPromotionGateCatalogLoader _loader;

    public TweakPromotionGateCatalogStore(string? docsRoot)
    {
        var pathResolver = new TweakPromotionGatePathResolver(docsRoot);
        _loader = new TweakPromotionGateCatalogLoader(pathResolver);
        _auditLogWriter = new TweakPromotionGateAuditLogWriter(pathResolver.RepoRoot);
    }

    public TweakPromotionGateCatalog LoadCatalog()
        => _loader.LoadCatalog(CatalogPath);

    public BlockedWorklistCatalog LoadBlockedWorklist()
        => _loader.LoadBlockedWorklist(BlockedWorklistPath);

    public bool TryAppendMutationAuditLog(string action, TweakMutationDecision decision, bool contributorMode, out string? error)
        => _auditLogWriter.TryAppend(MutationAuditLogPath, action, decision, contributorMode, out error);

    public static IReadOnlyDictionary<string, TweakPromotionGateEntry> BuildIndex(IEnumerable<TweakPromotionGateEntry> entries)
        => TweakPromotionGateIndexBuilder.BuildIndex(entries);

    public static IReadOnlyDictionary<string, BlockedWorklistEntry> BuildBlockedWorklistIndex(IEnumerable<BlockedWorklistEntry> entries)
        => TweakPromotionGateIndexBuilder.BuildBlockedWorklistIndex(entries);

    public static TweakPromotionGateEntry Clone(TweakPromotionGateEntry entry)
        => TweakPromotionGateCloner.Clone(entry);

    public static BlockedWorklistEntry Clone(BlockedWorklistEntry entry)
        => TweakPromotionGateCloner.Clone(entry);
}
