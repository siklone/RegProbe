using RegProbe.Application.Utilities;

namespace RegProbe.Application.Services;

internal sealed record TweakPromotionGateCatalogBootstrapResult(
    TweakPromotionGateCatalogStore Store,
    TweakPromotionGateCatalog Catalog,
    BlockedWorklistCatalog BlockedWorklist,
    IReadOnlyDictionary<string, TweakPromotionGateEntry> Index,
    IReadOnlyDictionary<string, BlockedWorklistEntry> BlockedWorklistIndex);

internal static class TweakPromotionGateCatalogBootstrap
{
    public static TweakPromotionGateCatalogBootstrapResult Create(string? docsRoot)
    {
        var store = new TweakPromotionGateCatalogStore(docsRoot ?? DocsLocator.TryFindDocsRoot());
        var catalog = store.LoadCatalog();
        var blockedWorklist = store.LoadBlockedWorklist();
        var index = TweakPromotionGateCatalogStore.BuildIndex(catalog.Entries);
        var blockedWorklistIndex = TweakPromotionGateCatalogStore.BuildBlockedWorklistIndex(blockedWorklist.Items);

        return new TweakPromotionGateCatalogBootstrapResult(
            store,
            catalog,
            blockedWorklist,
            index,
            blockedWorklistIndex);
    }
}
