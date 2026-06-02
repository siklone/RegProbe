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

            if (!string.IsNullOrWhiteSpace(catalogPath) && !string.IsNullOrWhiteSpace(anchorId))
            {
                var catalogHasAnchor = _store.HasDocAnchor(catalogPath, anchorId);
                if (catalogHasAnchor && TryInsertReferenceLink(
                        tweak,
                        "Catalog index",
                        TweakDocumentationCatalogStore.AppendDocAnchor(catalogPath, anchorId),
                        insertIndex,
                        "Local catalog index. Naming/navigation context only; not value-behavior proof.",
                        ReferenceLinkKind.Catalog))
                {
                    insertIndex++;
                }
            }

            if (!string.IsNullOrWhiteSpace(detailsPath) && !string.IsNullOrWhiteSpace(anchorId))
            {
                var detailsHasAnchor = _store.HasDocAnchor(detailsPath, anchorId);
                if (detailsHasAnchor && TryInsertReferenceLink(
                        tweak,
                        "Docs: Details",
                        TweakDocumentationCatalogStore.AppendDocAnchor(detailsPath, anchorId),
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
                    if (hasAnchor && TryInsertReferenceLink(
                            tweak,
                            entryTitle,
                            TweakDocumentationCatalogStore.AppendDocAnchor(entryDocPath, anchorId),
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
                        if (hasAnchor && TryInsertReferenceLink(
                                tweak,
                                fallbackTitle,
                                TweakDocumentationCatalogStore.AppendDocAnchor(fallbackDocPath, anchorId),
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
            if (hasFallbackAnchor)
            {
                TryInsertReferenceLink(
                    tweak,
                    title,
                    TweakDocumentationCatalogStore.AppendDocAnchor(docPath, anchorId),
                    insertIndex,
                    "Category documentation for this tweak.",
                    ReferenceLinkKind.Docs);
            }
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
