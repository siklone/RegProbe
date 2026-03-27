using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using RegProbe.App.Utilities;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class TweakDocumentationLinker
{
    private const string DefaultDocPath = "tweaks/tweaks.md";
    private const string DetailsDocPath = "tweaks/tweak-details.html";
    private const string WinConfigDocPath = "tweaks/win-config/batch-01.md";
    private static readonly Regex AnchorRegex = new("id\\s*=\\s*\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly IReadOnlyDictionary<string, string> _categoryDocMap;
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;
    private readonly IReadOnlyDictionary<string, CatalogEntry> _catalogIndex;
    private readonly IReadOnlyList<TemplateCatalogEntry> _templatedCatalogEntries;
    private readonly Dictionary<string, HashSet<string>> _docAnchorCache = new(StringComparer.OrdinalIgnoreCase);

    public TweakDocumentationLinker(string? docsRoot = null)
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
            var hasCatalogEntry = TryResolveCatalogEntry(tweak.Id, out var catalogEntry, out var anchorId);
            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                var catalogHasAnchor = HasDocAnchor(catalogPath, anchorId);
                var catalogUrl = catalogHasAnchor ? $"{catalogPath}#{anchorId}" : catalogPath;
                var catalogTitle = catalogHasAnchor ? "Catalog entry" : "Catalog entry (missing)";
                if (TryInsertReferenceLink(tweak, catalogTitle, catalogUrl, insertIndex,
                        "Full tweak catalog with all entries.", ReferenceLinkKind.Catalog))
                {
                    insertIndex++;
                }
            }

            var detailsPath = ResolveDetailsDocPath();
            if (!string.IsNullOrWhiteSpace(detailsPath))
            {
                var detailsHasAnchor = HasDocAnchor(detailsPath, anchorId);
                var detailsUrl = detailsHasAnchor ? $"{detailsPath}#{anchorId}" : detailsPath;
                var detailsTitle = detailsHasAnchor ? "Docs: Details" : "Docs: Details (missing)";
                if (TryInsertReferenceLink(tweak, detailsTitle, detailsUrl, insertIndex,
                        "Per-tweak summary (Changes, Risk, Source).", ReferenceLinkKind.Details))
                {
                    insertIndex++;
                }
            }

            var winConfigPath = ResolveDocPathFromRelative(WinConfigDocPath);
            if (!string.IsNullOrWhiteSpace(winConfigPath) && HasDocAnchor(winConfigPath, anchorId))
            {
                var winConfigUrl = AppendDocAnchor(winConfigPath, anchorId);
                if (TryInsertReferenceLink(tweak, "Docs: Win-Config", winConfigUrl, insertIndex,
                        "Win-config batch documentation for this tweak.", ReferenceLinkKind.Docs))
                {
                    insertIndex++;
                }
            }

            var prefix = ExtractPrefix(tweak.Id);
            if (hasCatalogEntry)
            {
                if (TryBuildSourceLink(catalogEntry, out var sourceTitle, out var sourcePath)
                    && TryInsertReferenceLink(tweak, sourceTitle, sourcePath, insertIndex,
                        "Open the source definition for this tweak.", ReferenceLinkKind.Source))
                {
                    insertIndex++;
                }

                var entryDocPath = ResolveDocPathFromRelative(catalogEntry.DocsPath);
                if (!string.IsNullOrWhiteSpace(entryDocPath))
                {
                    var entryTitle = BuildDocsTitle(catalogEntry.Category, prefix);
                    var hasAnchor = HasDocAnchor(entryDocPath, anchorId);
                    if (!hasAnchor)
                    {
                        entryTitle += " (section missing)";
                    }

                    var docUrl = hasAnchor ? AppendDocAnchor(entryDocPath, anchorId) : entryDocPath;
                    if (TryInsertReferenceLink(tweak, entryTitle, docUrl, insertIndex,
                            "Category documentation for this tweak.", ReferenceLinkKind.Docs))
                    {
                        insertIndex++;
                    }
                }
                else
                {
                    var fallbackDocPath = ResolveDocPath(prefix);
                    if (!string.IsNullOrWhiteSpace(fallbackDocPath))
                    {
                        var fallbackTitle = BuildDocsTitle(catalogEntry.Category, prefix) + " (file missing)";
                        var hasAnchor = HasDocAnchor(fallbackDocPath, anchorId);
                        var fallbackUrl = hasAnchor ? AppendDocAnchor(fallbackDocPath, anchorId) : fallbackDocPath;
                        if (TryInsertReferenceLink(tweak, fallbackTitle, fallbackUrl, insertIndex,
                                "Category documentation for this tweak.", ReferenceLinkKind.Docs))
                        {
                            insertIndex++;
                        }
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
            var hasFallbackAnchor = HasDocAnchor(docPath, anchorId);
            if (!hasFallbackAnchor)
            {
                title += " (section missing)";
            }

            var fallbackDocUrl = hasFallbackAnchor ? AppendDocAnchor(docPath, anchorId) : docPath;
            TryInsertReferenceLink(tweak, title, fallbackDocUrl, insertIndex,
                "Category documentation for this tweak.", ReferenceLinkKind.Docs);
        }
    }

    private bool TryResolveCatalogEntry(string tweakId, out CatalogEntry entry, out string anchorId)
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

    private string ResolveCatalogPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", "tweak-catalog.html");
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    private string ResolveDetailsDocPath()
    {
        var fullPath = Path.Combine(_docsRoot ?? string.Empty, "tweaks", "tweak-details.html");
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

    private static string AppendDocAnchor(string docPath, string tweakId)
    {
        if (string.IsNullOrWhiteSpace(docPath) || string.IsNullOrWhiteSpace(tweakId))
        {
            return docPath;
        }

        return docPath.Contains('#') ? docPath : $"{docPath}#{tweakId}";
    }

    private bool HasDocAnchor(string docPath, string tweakId)
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

    private bool TryInsertReferenceLink(
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
            if (pattern is null)
            {
                continue;
            }

            entries.Add(new TemplateCatalogEntry(entry.Id, pattern, entry));
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

    private sealed record CatalogEntry(string Id, string? Category, string? SourcePath, string? DocsPath);
    private sealed record TemplateCatalogEntry(string TemplateId, Regex Pattern, CatalogEntry Entry);
}
