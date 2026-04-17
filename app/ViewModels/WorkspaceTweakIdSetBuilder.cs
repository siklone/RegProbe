namespace RegProbe.App.ViewModels;

internal static class WorkspaceTweakIdSetBuilder
{
    public static HashSet<string> Build(IEnumerable<TweakItemViewModel> tweaks)
    {
        return new HashSet<string>(
            (tweaks ?? Enumerable.Empty<TweakItemViewModel>())
                .Select(t => t.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);
    }
}
