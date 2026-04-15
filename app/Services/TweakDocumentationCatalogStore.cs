using System.Text;
using System.Text.RegularExpressions;
using RegProbe.App.Utilities;

namespace RegProbe.App.Services;

internal sealed class TweakDocumentationCatalogStore
{
    private const string DefaultDocPath = "tweaks/tweaks.md";
    private const string DetailsDocPath = "tweaks/tweak-details.html";
    private const string WinConfigDocPath = "tweaks/win-config/batch-01.md";

    private static readonly Regex AnchorRegex = new(
        "id\\s*=\\s*\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IReadOnlyDictionary<string, string> _categoryDocMap;
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;
    private readonly IReadOnlyDictionary<string, CatalogEntry> _catalogIndex;
    private readonly IReadOnlyList<TemplateCatalogEntry> _templatedCatalogEntries;
    private readonly Dictionary<string, HashSet<string>> _docAnchorCache = new(StringComparer.OrdinalIgnoreCase);

    public TweakDocumentationCatalogStore(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
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
        _catalogIndex = LoadCatalogIndex();
        _templatedCatalogEntries = BuildTemplateCatalogEntries(_catalogIndex);
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_docsRoot);

    public string ResolveCatalogPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", "tweak-catalog.html");
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    public string ResolveDetailsDocPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", "tweak-details.html");
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    public string ResolveWinConfigDocPath()
        => ResolveDocPathFromRelative(WinConfigDocPath);

    public bool TryResolveCatalogEntry(string tweakId, out CatalogEntry entry, out string anchorId)
    {
        anchorId = tweakId;
        if (_catalogIndex.TryGetValue(tweakId, out var found) && found is not null)
        {
            entry = found;
            return true;
        }

        foreach (var templatedEntry in _templatedCatalogEntries)
        {
            if (templatedEntry.Pattern.IsMatch(tweakId))
            {
                entry = templatedEntry.Entry;
                anchorId = templatedEntry.TemplateId;
                return true;
            }
        }

        entry = default!;
        return false;
    }

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

    public bool HasDocAnchor(string docPath, string tweakId)
    {
        if (string.IsNullOrWhiteSpace(docPath) || string.IsNullOrWhiteSpace(tweakId))
        {
            return false;
        }

        if (!_docAnchorCache.TryGetValue(docPath, out var anchors))
        {
            anchors = LoadDocAnchors(docPath);
            _docAnchorCache[docPath] = anchors;
        }

        return anchors.Contains(tweakId);
    }

    public bool TryBuildSourceLink(CatalogEntry entry, out string title, out string path)
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

        title = string.IsNullOrWhiteSpace(line) ? "Source file" : $"Source file (L{line})";
        path = fullPath;
        return true;
    }

    public static string ExtractPrefix(string tweakId)
    {
        var dotIndex = tweakId.IndexOf('.');
        if (dotIndex <= 0)
        {
            return "other";
        }

        return tweakId[..dotIndex].ToLowerInvariant();
    }

    public static string BuildDocsTitle(string? category, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return $"Docs: {category}";
        }

        return $"Docs: {StringPool.GetCategory(prefix)}";
    }

    public static string AppendDocAnchor(string docPath, string tweakId)
    {
        if (string.IsNullOrWhiteSpace(docPath) || string.IsNullOrWhiteSpace(tweakId))
        {
            return docPath;
        }

        return docPath.Contains('#') ? docPath : $"{docPath}#{tweakId}";
    }

    private static HashSet<string> LoadDocAnchors(string docPath)
    {
        var anchors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!File.Exists(docPath))
            {
                return anchors;
            }

            var text = File.ReadAllText(docPath);
            foreach (Match match in AnchorRegex.Matches(text))
            {
                var id = match.Groups.Count > 1 ? match.Groups[1].Value : string.Empty;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    anchors.Add(id.Trim());
                }
            }
        }
        catch
        {
            // Ignore doc parsing errors; treat as missing anchors.
        }

        return anchors;
    }

    private IReadOnlyDictionary<string, CatalogEntry> LoadCatalogIndex()
    {
        var catalogPath = ResolveCatalogCsvPath();
        if (string.IsNullOrWhiteSpace(catalogPath) || !File.Exists(catalogPath))
        {
            return new Dictionary<string, CatalogEntry>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var lines = File.ReadAllLines(catalogPath);
            if (lines.Length <= 1)
            {
                return new Dictionary<string, CatalogEntry>(StringComparer.OrdinalIgnoreCase);
            }

            var headerFields = SplitCsvLine(lines[0]);
            var indexMap = BuildHeaderIndex(headerFields);
            var idIndex = GetHeaderIndex(indexMap, "id");
            var categoryIndex = GetHeaderIndex(indexMap, "category");
            var sourceIndex = GetHeaderIndex(indexMap, "source");
            var docsIndex = GetHeaderIndex(indexMap, "docs");

            if (idIndex < 0 || categoryIndex < 0 || sourceIndex < 0 || docsIndex < 0)
            {
                idIndex = idIndex < 0 ? 0 : idIndex;
                categoryIndex = categoryIndex < 0 ? 2 : categoryIndex;
                sourceIndex = sourceIndex < 0 ? 4 : sourceIndex;
                docsIndex = docsIndex < 0 ? 5 : docsIndex;
            }

            var maxIndex = new[] { idIndex, categoryIndex, sourceIndex, docsIndex }.Max();
            var map = new Dictionary<string, CatalogEntry>(StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var fields = SplitCsvLine(line);
                if (fields.Count <= maxIndex)
                {
                    continue;
                }

                var id = fields[idIndex].Trim();
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                map[id] = new CatalogEntry(
                    id,
                    fields[categoryIndex].Trim(),
                    fields[sourceIndex].Trim(),
                    fields[docsIndex].Trim());
            }

            return map;
        }
        catch
        {
            return new Dictionary<string, CatalogEntry>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private string ResolveCatalogCsvPath()
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

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var results = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                results.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        results.Add(current.ToString());
        return results;
    }

    private static Dictionary<string, int> BuildHeaderIndex(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var key = headers[i].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                map[key] = i;
            }
        }

        return map;
    }

    private static int GetHeaderIndex(IReadOnlyDictionary<string, int> map, string name)
        => map.TryGetValue(name, out var index) ? index : -1;

    private static string NormalizeRelativePath(string path)
        => path.Replace('/', Path.DirectorySeparatorChar).Trim();

    private static IReadOnlyList<TemplateCatalogEntry> BuildTemplateCatalogEntries(
        IReadOnlyDictionary<string, CatalogEntry> catalogIndex)
    {
        var entries = new List<TemplateCatalogEntry>();
        foreach (var entry in catalogIndex.Values.OrderBy(value => value.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || !entry.Id.Contains('{'))
            {
                continue;
            }

            var pattern = BuildTemplateRegex(entry.Id);
            if (pattern is not null)
            {
                entries.Add(new TemplateCatalogEntry(entry.Id, pattern, entry));
            }
        }

        return entries;
    }

    private static Regex? BuildTemplateRegex(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId) || !templateId.Contains('{'))
        {
            return null;
        }

        var escaped = Regex.Escape(templateId);
        var pattern = Regex.Replace(escaped, @"\\\{[^}]+\\\}", @"[^\s]+");
        return new Regex($"^{pattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string TrimDocsPrefix(string path)
    {
        var trimmed = path.TrimStart(Path.DirectorySeparatorChar, '/', '\\');
        if (trimmed.StartsWith("Docs", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[4..].TrimStart(Path.DirectorySeparatorChar, '/', '\\');
        }

        return trimmed;
    }

    internal sealed record CatalogEntry(string Id, string? Category, string? SourcePath, string? DocsPath);
    internal sealed record TemplateCatalogEntry(string TemplateId, Regex Pattern, CatalogEntry Entry);
}
