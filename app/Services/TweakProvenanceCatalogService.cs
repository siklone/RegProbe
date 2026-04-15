using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class TweakProvenanceCatalogService
{
    private readonly TweakProvenanceCatalogStore _store;
    private readonly TweakProvenanceCatalog _catalog;
    private readonly IReadOnlyDictionary<string, TweakProvenanceEntry> _index;
    private readonly IReadOnlyList<TweakProvenanceCatalogStore.TemplateEntry> _templatedEntries;

    public TweakProvenanceCatalogService(string? docsRoot = null)
    {
        _store = new TweakProvenanceCatalogStore(docsRoot);
        _catalog = _store.LoadCatalog();
        _index = TweakProvenanceCatalogStore.BuildIndex(_catalog.Entries);
        _templatedEntries = TweakProvenanceCatalogStore.BuildTemplateEntries(_catalog.Entries);
    }

    public TweakProvenanceCatalog Catalog => _catalog;

    public string ResolveMarkdownReportPath() => _store.ResolveMarkdownReportPath();

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id))
            {
                continue;
            }

            if (!TweakProvenanceCatalogStore.TryResolveEntry(tweak.Id, _index, _templatedEntries, out var entry))
            {
                tweak.HasNohutoEvidence = false;
                tweak.HasWindowsInternalsContext = false;
                tweak.NeedsSourceReview = true;
                tweak.ProvenanceSummary = "No upstream dump or pseudocode source is linked yet. Keep this tweak in review until the validation proof and app mapping are strong enough.";
                continue;
            }

            tweak.HasNohutoEvidence = entry.HasNohutoEvidence;
            tweak.HasWindowsInternalsContext = entry.HasWindowsInternalsContext;
            tweak.NeedsSourceReview = entry.NeedsReview;
            tweak.ProvenanceSummary = string.IsNullOrWhiteSpace(entry.Summary)
                ? BuildFallbackSummary(entry)
                : entry.Summary.Trim();

            var insertIndex = 0;
            foreach (var reference in entry.References.Take(4))
            {
                var resolvedUrl = _store.ResolvePath(reference.Url);
                if (string.IsNullOrWhiteSpace(resolvedUrl))
                {
                    continue;
                }

                if (TryInsertReferenceLink(
                        tweak,
                        reference.Title,
                        resolvedUrl,
                        insertIndex,
                        reference.Summary,
                        MapReferenceKind(reference.Kind)))
                {
                    insertIndex++;
                }
            }
        }
    }

    private static string BuildFallbackSummary(TweakProvenanceEntry entry)
    {
        if (entry.HasNohutoEvidence && entry.HasWindowsInternalsContext)
        {
            return "Linked to upstream dump / pseudocode sources and Windows Internals notes. Value semantics are still validated in the research record.";
        }

        if (entry.HasNohutoEvidence)
        {
            return "Linked to upstream dump / pseudocode sources. These links show where the setting came from, not what each value means.";
        }

        if (entry.HasWindowsInternalsContext)
        {
            return "Has Windows Internals notes but still needs stronger repo evidence.";
        }

        return "Source review still needed.";
    }

    private static ReferenceLinkKind MapReferenceKind(string? kind)
    {
        var normalized = kind?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "nohuto" => ReferenceLinkKind.Source,
            "internals" => ReferenceLinkKind.Docs,
            "microsoft" => ReferenceLinkKind.Docs,
            "research" => ReferenceLinkKind.Details,
            _ => ReferenceLinkKind.Other
        };
    }

    private static bool TryInsertReferenceLink(
        TweakItemViewModel tweak,
        string title,
        string url,
        int index,
        string? tooltip,
        ReferenceLinkKind kind)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (tweak.ReferenceLinks.Any(link =>
                string.Equals(link.Url, url, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var safeIndex = Math.Clamp(index, 0, tweak.ReferenceLinks.Count);
        tweak.ReferenceLinks.Insert(safeIndex, new ReferenceLink(title, url, tooltip, kind));
        return true;
    }
}
