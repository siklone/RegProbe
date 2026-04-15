using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class TweakDocumentationLinker
{
    private readonly TweakDocumentationCatalogStore _store;

    public TweakDocumentationLinker(string? docsRoot = null) => _store = new TweakDocumentationCatalogStore(docsRoot);

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        if (!_store.IsAvailable) return;

        var catalogPath = _store.ResolveCatalogPath();
        var detailsPath = _store.ResolveDetailsDocPath();
        var winConfigPath = _store.ResolveWinConfigDocPath();

        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id)) continue;

            var insertIndex = 0;
            var hasCatalogEntry = _store.TryResolveCatalogEntry(tweak.Id, out var catalogEntry, out var anchorId);

            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                var catalogHasAnchor = _store.HasDocAnchor(catalogPath, anchorId);
                var catalogUrl = catalogHasAnchor ? TweakDocumentationCatalogStore.AppendDocAnchor(catalogPath, anchorId) : catalogPath;
                var catalogTitle = catalogHasAnchor ? "Catalog entry" : "Catalog entry (missing)";
                if (TryInsertReferenceLink(
                        tweak,
                        catalogTitle,
                        catalogUrl,
                        insertIndex,
                        "Full tweak catalog with all entries.",
                        ReferenceLinkKind.Catalog))
                {
                    insertIndex++;
                }
            }

            if (!string.IsNullOrWhiteSpace(detailsPath))
            {
                var detailsHasAnchor = _store.HasDocAnchor(detailsPath, anchorId);
                var detailsUrl = detailsHasAnchor ? TweakDocumentationCatalogStore.AppendDocAnchor(detailsPath, anchorId) : detailsPath;
                var detailsTitle = detailsHasAnchor ? "Docs: Details" : "Docs: Details (missing)";
                if (TryInsertReferenceLink(
                        tweak,
                        detailsTitle,
                        detailsUrl,
                        insertIndex,
                        "Per-tweak summary (Changes, Risk, Source).",
                        ReferenceLinkKind.Details))
                {
                    insertIndex++;
                }
            }

            if (!string.IsNullOrWhiteSpace(winConfigPath) && _store.HasDocAnchor(winConfigPath, anchorId))
            {
                var winConfigUrl = TweakDocumentationCatalogStore.AppendDocAnchor(winConfigPath, anchorId);
                if (TryInsertReferenceLink(
                        tweak,
                        "Docs: Win-Config",
                        winConfigUrl,
                        insertIndex,
                        "Win-config batch documentation for this tweak.",
                        ReferenceLinkKind.Docs))
                {
                    insertIndex++;
                }
            }

            var prefix = TweakDocumentationCatalogStore.ExtractPrefix(tweak.Id);
            if (hasCatalogEntry)
            {
                if (_store.TryBuildSourceLink(catalogEntry, out var sourceTitle, out var sourcePath)
                    && TryInsertReferenceLink(
                        tweak,
                        sourceTitle,
                        sourcePath,
                        insertIndex,
                        "Open the source definition for this tweak.",
                        ReferenceLinkKind.Source))
                {
                    insertIndex++;
                }

                var entryDocPath = _store.ResolveDocPathFromRelative(catalogEntry.DocsPath);
                if (!string.IsNullOrWhiteSpace(entryDocPath))
                {
                    var entryTitle = TweakDocumentationCatalogStore.BuildDocsTitle(catalogEntry.Category, prefix);
                    var hasAnchor = _store.HasDocAnchor(entryDocPath, anchorId);
                    if (!hasAnchor) entryTitle += " (section missing)";

                    var docUrl = hasAnchor ? TweakDocumentationCatalogStore.AppendDocAnchor(entryDocPath, anchorId) : entryDocPath;
                    if (TryInsertReferenceLink(
                            tweak,
                            entryTitle,
                            docUrl,
                            insertIndex,
                            "Category documentation for this tweak.",
                            ReferenceLinkKind.Docs))
                    {
                        insertIndex++;
                    }
                }
                else
                {
                    var fallbackDocPath = _store.ResolveDocPath(prefix);
                    if (!string.IsNullOrWhiteSpace(fallbackDocPath))
                    {
                        var fallbackTitle = TweakDocumentationCatalogStore.BuildDocsTitle(catalogEntry.Category, prefix) + " (file missing)";
                        var hasAnchor = _store.HasDocAnchor(fallbackDocPath, anchorId);
                        var fallbackUrl = hasAnchor ? TweakDocumentationCatalogStore.AppendDocAnchor(fallbackDocPath, anchorId) : fallbackDocPath;
                        if (TryInsertReferenceLink(
                                tweak,
                                fallbackTitle,
                                fallbackUrl,
                                insertIndex,
                                "Category documentation for this tweak.",
                                ReferenceLinkKind.Docs))
                        {
                            insertIndex++;
                        }
                    }
                }

                continue;
            }

            var docPath = _store.ResolveDocPath(prefix);
            if (string.IsNullOrWhiteSpace(docPath)) continue;

            var title = TweakDocumentationCatalogStore.BuildDocsTitle(null, prefix);
            var hasFallbackAnchor = _store.HasDocAnchor(docPath, anchorId);
            if (!hasFallbackAnchor) title += " (section missing)";

            var fallbackDocUrl = hasFallbackAnchor ? TweakDocumentationCatalogStore.AppendDocAnchor(docPath, anchorId) : docPath;
            TryInsertReferenceLink(
                tweak,
                title,
                fallbackDocUrl,
                insertIndex,
                "Category documentation for this tweak.",
                ReferenceLinkKind.Docs);
        }
    }

    private static bool TryInsertReferenceLink(
        TweakItemViewModel tweak,
        string title,
        string url,
        int index,
        string? tooltip = null,
        ReferenceLinkKind kind = ReferenceLinkKind.Other)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (tweak.ReferenceLinks.Any(link => string.Equals(link.Url, url, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var safeIndex = Math.Clamp(index, 0, tweak.ReferenceLinks.Count);
        tweak.ReferenceLinks.Insert(safeIndex, new ReferenceLink(title, url, tooltip, kind));
        return true;
    }
}
