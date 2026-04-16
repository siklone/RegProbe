using RegProbe.Application.Services.TweakProviders;
using RegProbe.Core;

namespace RegProbe.Application.Services;

internal sealed record TweakCatalogIndex(
    IReadOnlyList<TweakCatalogEntry> Entries,
    Dictionary<string, ITweak> ById);

internal static class TweakCatalogIndexBuilder
{
    public static TweakCatalogIndex Build(
        IReadOnlyList<ITweakProvider> providers,
        TweakExecutionPipeline pipeline,
        TweakContext context,
        bool isElevated)
    {
        var entries = new List<TweakCatalogEntry>();
        foreach (var provider in providers)
        {
            foreach (var tweak in provider.CreateTweaks(pipeline, context, isElevated))
            {
                entries.Add(new TweakCatalogEntry(provider.CategoryName, tweak));
            }
        }

        var byId = entries
            .Select(entry => entry.Tweak)
            .Where(tweak => !string.IsNullOrWhiteSpace(tweak.Id))
            .GroupBy(tweak => tweak.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return new TweakCatalogIndex(entries, byId);
    }
}
