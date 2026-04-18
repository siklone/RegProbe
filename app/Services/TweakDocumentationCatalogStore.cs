using RegProbe.App.Utilities;

namespace RegProbe.App.Services;

internal sealed class TweakDocumentationCatalogStore
{
    private readonly TweakDocumentationAnchorCache _anchorCache = new();
    private readonly TweakDocumentationCatalogIndex _catalogIndex;
    private readonly TweakDocumentationPathResolver _pathResolver;
    private readonly string? _docsRoot;

    public TweakDocumentationCatalogStore(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
        _pathResolver = new TweakDocumentationPathResolver(_docsRoot);
        _catalogIndex = new TweakDocumentationCatalogIndex(_pathResolver);
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_docsRoot);

    public string ResolveCatalogPath()
        => _pathResolver.ResolveCatalogPath();

    public string ResolveDetailsDocPath()
        => _pathResolver.ResolveDetailsDocPath();

    public string ResolveWinConfigDocPath()
        => _pathResolver.ResolveWinConfigDocPath();

    public bool TryResolveCatalogEntry(string tweakId, out TweakDocumentationCatalogEntry entry, out string anchorId)
        => _catalogIndex.TryResolveCatalogEntry(tweakId, out entry, out anchorId);

    public string ResolveDocPath(string prefix)
        => _pathResolver.ResolveDocPath(prefix);

    public string ResolveDocPathFromRelative(string? relativePath)
        => _pathResolver.ResolveDocPathFromRelative(relativePath);

    public bool HasDocAnchor(string docPath, string tweakId)
        => _anchorCache.HasDocAnchor(docPath, tweakId);

    public bool TryBuildSourceLink(TweakDocumentationCatalogEntry entry, out string title, out string path)
        => _pathResolver.TryBuildSourceLink(entry, out title, out path);

    public static string ExtractPrefix(string tweakId)
        => TweakDocumentationTextHelpers.ExtractPrefix(tweakId);

    public static string BuildDocsTitle(string? category, string prefix)
        => TweakDocumentationTextHelpers.BuildDocsTitle(category, prefix);

    public static string AppendDocAnchor(string docPath, string tweakId)
        => TweakDocumentationTextHelpers.AppendDocAnchor(docPath, tweakId);
}
