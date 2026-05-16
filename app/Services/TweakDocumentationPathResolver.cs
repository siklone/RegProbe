namespace RegProbe.App.Services;

internal sealed class TweakDocumentationPathResolver
{
    private const string DefaultDocPath = "tweaks/tweaks.md";
    private const string DetailsDocPath = "tweaks/tweak-details.html";
    private const string WinConfigDocPath = "tweaks/win-config/batch-01.md";

    private readonly IReadOnlyDictionary<string, string> _categoryDocMap;
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;

    public TweakDocumentationPathResolver(string? docsRoot)
    {
        _docsRoot = docsRoot;
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
        _categoryDocMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["privacy"] = Path.Combine("privacy", "privacy-verified.md"),
            ["security"] = Path.Combine("security", "security-verified.md"),
            ["network"] = Path.Combine("network", "network.md"),
            ["power"] = Path.Combine("power", "power.md"),
            ["system"] = Path.Combine("system", "system.md"),
            ["visibility"] = Path.Combine("visibility", "visibility.md"),
            ["peripheral"] = Path.Combine("peripheral", "peripheral.md"),
            ["audio"] = Path.Combine("peripheral", "peripheral.md"),
            ["misc"] = Path.Combine("misc", "misc.md"),
            ["cleanup"] = Path.Combine("cleanup", "cleanup.md"),
            ["explorer"] = Path.Combine("visibility", "visibility.md"),
            ["notifications"] = Path.Combine("notifications", "notifications.md"),
            ["performance"] = Path.Combine("performance", "performance.md"),
        };
    }

    public string ResolveCatalogPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", "tweak-catalog.html");
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    public string ResolveDetailsDocPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", DetailsDocPath.Split('/')[^1]);
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    public string ResolveWinConfigDocPath()
        => ResolveDocPathFromRelative(WinConfigDocPath);

    public string ResolveDocPath(string prefix)
    {
        var relative = _categoryDocMap.TryGetValue(prefix, out var mapped)
            ? mapped
            : DefaultDocPath;

        return ResolveDocPathFromRelative(relative);
    }

    public string ResolveDocPathFromRelative(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var normalized = NormalizeRelativePath(relativePath);
        if (Path.IsPathRooted(normalized))
        {
            return File.Exists(normalized) ? normalized : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(_repoRoot))
        {
            var rooted = Path.Combine(_repoRoot, normalized);
            if (File.Exists(rooted))
            {
                return rooted;
            }
        }

        if (!string.IsNullOrWhiteSpace(_docsRoot))
        {
            var trimmed = TrimDocsPrefix(normalized);
            var rooted = Path.Combine(_docsRoot, trimmed);
            if (File.Exists(rooted))
            {
                return rooted;
            }
        }

        return string.Empty;
    }

    public bool TryBuildSourceLink(TweakDocumentationCatalogEntry entry, out string title, out string path)
    {
        title = string.Empty;
        path = string.Empty;

        if (string.IsNullOrWhiteSpace(entry.SourcePath))
        {
            return false;
        }

        var (sourcePath, line) = SplitSourcePath(entry.SourcePath);
        var normalized = NormalizeRelativePath(sourcePath);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var fullPath = normalized;
        if (!Path.IsPathRooted(normalized) && !string.IsNullOrWhiteSpace(_repoRoot))
        {
            fullPath = Path.Combine(_repoRoot, normalized);
        }

        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            return false;
        }

        title = BuildSourceTitle(normalized, line);
        path = fullPath;
        return true;
    }

    public string ResolveCatalogCsvPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", "tweak-catalog.csv");
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    private static (string path, string? line) SplitSourcePath(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return (string.Empty, null);
        }

        var markerIndex = sourcePath.LastIndexOf("#L", StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
        {
            return (sourcePath, null);
        }

        var linePart = sourcePath[(markerIndex + 2)..];
        var normalizedLine = int.TryParse(linePart, out var lineNumber)
            ? lineNumber.ToString()
            : linePart.Trim();

        return (sourcePath[..markerIndex], normalizedLine);
    }

    private static string BuildSourceTitle(string normalizedPath, string? line)
    {
        var label = normalizedPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            ? "App implementation source"
            : normalizedPath.Contains("research/records/", StringComparison.OrdinalIgnoreCase)
                || normalizedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    ? "Local research record"
                    : "Local source definition";

        return string.IsNullOrWhiteSpace(line) ? label : $"{label} (L{line})";
    }

    private static string NormalizeRelativePath(string path)
        => path.Replace('/', Path.DirectorySeparatorChar).Trim();

    private static string TrimDocsPrefix(string path)
    {
        var trimmed = path.TrimStart(Path.DirectorySeparatorChar, '/', '\\');
        if (trimmed.StartsWith("Docs", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[4..].TrimStart(Path.DirectorySeparatorChar, '/', '\\');
        }

        return trimmed;
    }
}
