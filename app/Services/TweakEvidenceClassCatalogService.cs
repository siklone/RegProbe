using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class TweakEvidenceClassCatalogService
{
    private readonly TweakEvidenceClassCatalogStore _store;
    private readonly TweakEvidenceClassCatalog _catalog;
    private readonly IReadOnlyDictionary<string, TweakEvidenceClassEntry> _index;

    public TweakEvidenceClassCatalogService(string? docsRoot = null)
    {
        _store = new TweakEvidenceClassCatalogStore(docsRoot);
        _catalog = _store.LoadCatalog();
        _index = TweakEvidenceClassCatalogStore.BuildIndex(_catalog.Entries);
    }

    public TweakEvidenceClassCatalog Catalog => _catalog;

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id))
            {
                continue;
            }

            if (_index.TryGetValue(tweak.Id, out var entry))
            {
                tweak.ApplyEvidenceClassification(_store.CloneWithResolvedLinks(entry));
                continue;
            }

            tweak.ApplyEvidenceClassification(TweakEvidenceClassEntry.CreateFallback(tweak.Id));
        }
    }
}
