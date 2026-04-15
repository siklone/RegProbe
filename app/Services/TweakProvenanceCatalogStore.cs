using System.Text.Json;
using System.Text.RegularExpressions;
using RegProbe.App.Utilities;

namespace RegProbe.App.Services;

internal sealed class TweakProvenanceCatalogStore
{
    private const string CatalogPath = "tweaks/tweak-provenance.json";
    private const string MarkdownPath = "tweaks/tweak-provenance.md";

    private readonly string? _docsRoot;
    private readonly string? _repoRoot;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TweakProvenanceCatalogStore(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
    }

    public TweakProvenanceCatalog LoadCatalog()
    {
        var path = ResolvePath(CatalogPath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new TweakProvenanceCatalog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TweakProvenanceCatalog>(json, JsonOptions)
                   ?? new TweakProvenanceCatalog();
        }
        catch
        {
            return new TweakProvenanceCatalog();
        }
    }

    public string ResolveMarkdownReportPath()
        => ResolvePath(MarkdownPath);

    public string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return path;
        }

        var normalized = path.Replace('/', Path.DirectorySeparatorChar).Trim();
        if (Path.IsPathRooted(normalized))
        {
            return File.Exists(normalized) ? normalized : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(_repoRoot))
        {
            var repoPath = Path.Combine(_repoRoot, normalized.TrimStart(Path.DirectorySeparatorChar));
            if (File.Exists(repoPath))
            {
                return repoPath;
            }
        }

        if (!string.IsNullOrWhiteSpace(_docsRoot))
        {
            var trimmed = normalized.StartsWith($"Docs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                ? normalized[5..]
                : normalized;
            var docsPath = Path.Combine(_docsRoot, trimmed.TrimStart(Path.DirectorySeparatorChar));
            if (File.Exists(docsPath))
            {
                return docsPath;
            }
        }

        return string.Empty;
    }

    public static IReadOnlyDictionary<string, TweakProvenanceEntry> BuildIndex(IEnumerable<TweakProvenanceEntry> entries)
        => entries.ToDictionary(static entry => entry.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<TemplateEntry> BuildTemplateEntries(IEnumerable<TweakProvenanceEntry> entries)
    {
        var templates = new List<TemplateEntry>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || !entry.Id.Contains('{', StringComparison.Ordinal))
            {
                continue;
            }

            var pattern = BuildTemplateRegex(entry.Id);
            if (pattern is null)
            {
                continue;
            }

            templates.Add(new TemplateEntry(entry, pattern));
        }

        return templates;
    }

    public static bool TryResolveEntry(
        string tweakId,
        IReadOnlyDictionary<string, TweakProvenanceEntry> index,
        IReadOnlyList<TemplateEntry> templatedEntries,
        out TweakProvenanceEntry entry)
    {
        if (index.TryGetValue(tweakId, out var directEntry))
        {
            entry = directEntry;
            return true;
        }

        foreach (var templateEntry in templatedEntries)
        {
            if (templateEntry.Pattern.IsMatch(tweakId))
            {
                entry = templateEntry.Entry;
                return true;
            }
        }

        entry = default!;
        return false;
    }

    private static Regex? BuildTemplateRegex(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId) || !templateId.Contains('{', StringComparison.Ordinal))
        {
            return null;
        }

        var escaped = Regex.Escape(templateId);
        var pattern = Regex.Replace(escaped, @"\\\{[^}]+\\\}", @"[^\s]+");
        return new Regex($"^{pattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    internal sealed record TemplateEntry(TweakProvenanceEntry Entry, Regex Pattern);
}
