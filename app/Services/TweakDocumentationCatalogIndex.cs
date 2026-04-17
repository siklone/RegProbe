using System.Text;
using System.Text.RegularExpressions;

namespace RegProbe.App.Services;

internal sealed class TweakDocumentationCatalogIndex
{
    private readonly IReadOnlyDictionary<string, TweakDocumentationCatalogEntry> _catalogIndex;
    private readonly IReadOnlyList<TweakDocumentationTemplateCatalogEntry> _templatedCatalogEntries;
    private readonly TweakDocumentationPathResolver _pathResolver;

    public TweakDocumentationCatalogIndex(TweakDocumentationPathResolver pathResolver)
    {
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _catalogIndex = LoadCatalogIndex();
        _templatedCatalogEntries = BuildTemplateCatalogEntries(_catalogIndex);
    }

    public bool TryResolveCatalogEntry(
        string tweakId,
        out TweakDocumentationCatalogEntry entry,
        out string anchorId)
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

    private IReadOnlyDictionary<string, TweakDocumentationCatalogEntry> LoadCatalogIndex()
    {
        var catalogPath = _pathResolver.ResolveCatalogCsvPath();
        if (string.IsNullOrWhiteSpace(catalogPath) || !File.Exists(catalogPath))
        {
            return new Dictionary<string, TweakDocumentationCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var lines = File.ReadAllLines(catalogPath);
            if (lines.Length <= 1)
            {
                return new Dictionary<string, TweakDocumentationCatalogEntry>(StringComparer.OrdinalIgnoreCase);
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
            var map = new Dictionary<string, TweakDocumentationCatalogEntry>(StringComparer.OrdinalIgnoreCase);

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

                map[id] = new TweakDocumentationCatalogEntry(
                    id,
                    fields[categoryIndex].Trim(),
                    fields[sourceIndex].Trim(),
                    fields[docsIndex].Trim());
            }

            return map;
        }
        catch
        {
            return new Dictionary<string, TweakDocumentationCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        }
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

    private static IReadOnlyList<TweakDocumentationTemplateCatalogEntry> BuildTemplateCatalogEntries(
        IReadOnlyDictionary<string, TweakDocumentationCatalogEntry> catalogIndex)
    {
        var entries = new List<TweakDocumentationTemplateCatalogEntry>();
        foreach (var entry in catalogIndex.Values.OrderBy(value => value.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || !entry.Id.Contains('{'))
            {
                continue;
            }

            var pattern = BuildTemplateRegex(entry.Id);
            if (pattern is not null)
            {
                entries.Add(new TweakDocumentationTemplateCatalogEntry(entry.Id, pattern, entry));
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
}
