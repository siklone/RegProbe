namespace RegProbe.Application.Services;

internal sealed class TweakPromotionGatePathResolver
{
    private readonly string? _docsRoot;

    public TweakPromotionGatePathResolver(string? docsRoot)
    {
        _docsRoot = docsRoot;
        RepoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
    }

    public string? RepoRoot { get; }

    public string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized))
        {
            return normalized;
        }

        if (!string.IsNullOrWhiteSpace(_docsRoot))
        {
            var docsRelative = Path.Combine(_docsRoot, normalized);
            if (File.Exists(docsRelative))
            {
                return docsRelative;
            }
        }

        if (!string.IsNullOrWhiteSpace(RepoRoot))
        {
            return Path.Combine(RepoRoot, normalized);
        }

        return normalized;
    }
}
