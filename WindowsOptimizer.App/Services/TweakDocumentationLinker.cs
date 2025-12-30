using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App.Services;

public sealed class TweakDocumentationLinker
{
    private const string DefaultDocPath = "tweaks/tweaks.md";
    private readonly IReadOnlyDictionary<string, string> _categoryDocMap;
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;
    private readonly IReadOnlyDictionary<string, CatalogEntry> _catalogIndex;

    public TweakDocumentationLinker(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
        _categoryDocMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["privacy"] = Path.Combine("privacy", "privacy.md"),
            ["security"] = Path.Combine("security", "security.md"),
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
    }

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        if (string.IsNullOrWhiteSpace(_docsRoot))
        {
            return;
        }

        var catalogPath = ResolveCatalogPath();
        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id))
            {
                continue;
            }

            var insertIndex = 0;
            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                var catalogUrl = $"{catalogPath}#{tweak.Id}";
                if (TryInsertReferenceLink(tweak, "Catalog entry", catalogUrl, insertIndex))
                {
                    insertIndex++;
                }
            }

            var prefix = ExtractPrefix(tweak.Id);
            if (_catalogIndex.TryGetValue(tweak.Id, out var entry))
            {
                if (TryBuildSourceLink(entry, out var sourceTitle, out var sourcePath)
                    && TryInsertReferenceLink(tweak, sourceTitle, sourcePath, insertIndex))
                {
                    insertIndex++;
                }

                var entryDocPath = ResolveDocPathFromRelative(entry.DocsPath);
                if (!string.IsNullOrWhiteSpace(entryDocPath))
                {
                    var entryTitle = BuildDocsTitle(entry.Category, prefix);
                    if (TryInsertReferenceLink(tweak, entryTitle, entryDocPath, insertIndex))
                    {
                        insertIndex++;
                    }
                }

                continue;
            }

            var docPath = ResolveDocPath(prefix);
            if (string.IsNullOrWhiteSpace(docPath))
            {
                continue;
            }

            var title = BuildDocsTitle(null, prefix);
            TryInsertReferenceLink(tweak, title, docPath, insertIndex);
        }
    }

    private string ResolveCatalogPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", "tweak-catalog.html");
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    private string ResolveDocPath(string prefix)
    {
        var relative = _categoryDocMap.TryGetValue(prefix, out var mapped)
            ? mapped
            : DefaultDocPath;

        return ResolveDocPathFromRelative(relative);
    }

    private string ResolveDocPathFromRelative(string? relativePath)
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

    private static string ExtractPrefix(string tweakId)
    {
        var dotIndex = tweakId.IndexOf('.');
        if (dotIndex <= 0)
        {
            return "other";
        }

        return tweakId.Substring(0, dotIndex).ToLowerInvariant();
    }

    private static string BuildDocsTitle(string? category, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return $"Docs: {category}";
        }

        return $"Docs: {StringPool.GetCategory(prefix)}";
    }

    private bool TryInsertReferenceLink(TweakItemViewModel tweak, string title, string url, int index)
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
        tweak.ReferenceLinks.Insert(safeIndex, new ReferenceLink(title, url));
        return true;
    }

    private bool TryBuildSourceLink(CatalogEntry entry, out string title, out string path)
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

                var entry = new CatalogEntry(
                    id,
                    fields[categoryIndex].Trim(),
                    fields[sourceIndex].Trim(),
                    fields[docsIndex].Trim());

                map[id] = entry;
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
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            map[key] = i;
        }

        return map;
    }

    private static int GetHeaderIndex(IReadOnlyDictionary<string, int> map, string name)
    {
        return map.TryGetValue(name, out var index) ? index : -1;
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar).Trim();
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

    private sealed record CatalogEntry(string Id, string? Category, string? SourcePath, string? DocsPath);
}
