using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App.Services;

public sealed class TweakProvenanceCatalog
{
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int TotalTweaks { get; set; }
    public int RepoBackedTweaks { get; set; }
    public int InternalsBackedTweaks { get; set; }
    public int ReviewNeededTweaks { get; set; }
    public List<TweakProvenanceSourceState> Sources { get; set; } = new();
    public List<TweakProvenanceEntry> Entries { get; set; } = new();
}

public sealed class TweakProvenanceSourceState
{
    public string Repository { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
}

public sealed class TweakProvenanceEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Risk { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool HasNohutoEvidence { get; set; }
    public bool HasWindowsInternalsContext { get; set; }
    public bool NeedsReview { get; set; }
    public string CoverageState { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> SourceRepositories { get; set; } = new();
    public List<string> MatchedTokens { get; set; } = new();
    public List<TweakProvenanceReference> References { get; set; } = new();
}

public sealed class TweakProvenanceReference
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class TweakProvenanceCatalogService
{
    private const string CatalogPath = "tweaks/tweak-provenance.json";
    private const string MarkdownPath = "tweaks/tweak-provenance.md";
    private readonly string? _docsRoot;
    private readonly string? _repoRoot;
    private readonly TweakProvenanceCatalog _catalog;
    private readonly IReadOnlyDictionary<string, TweakProvenanceEntry> _index;
    private readonly IReadOnlyList<TemplateEntry> _templatedEntries;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TweakProvenanceCatalogService(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
        _catalog = LoadCatalog();
        _index = _catalog.Entries.ToDictionary(
            static entry => entry.Id,
            StringComparer.OrdinalIgnoreCase);
        _templatedEntries = BuildTemplateEntries(_catalog.Entries);
    }

    public TweakProvenanceCatalog Catalog => _catalog;

    public string ResolveMarkdownReportPath() => ResolvePath(MarkdownPath);

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id))
            {
                continue;
            }

            if (!TryResolveEntry(tweak.Id, out var entry))
            {
                tweak.HasNohutoEvidence = false;
                tweak.HasWindowsInternalsContext = false;
                tweak.NeedsSourceReview = true;
                tweak.ProvenanceSummary = "No upstream dump or pseudocode source is linked yet. Keep this tweak in review until the validation proof and app mapping are strong enough.";
                continue;
            }

            tweak.HasNohutoEvidence = entry.HasNohutoEvidence;
            tweak.HasWindowsInternalsContext = entry.HasWindowsInternalsContext;
            tweak.NeedsSourceReview = entry.NeedsReview;
            tweak.ProvenanceSummary = string.IsNullOrWhiteSpace(entry.Summary)
                ? BuildFallbackSummary(entry)
                : entry.Summary.Trim();

            var insertIndex = 0;
            foreach (var reference in entry.References.Take(4))
            {
                var resolvedUrl = ResolvePath(reference.Url);
                if (string.IsNullOrWhiteSpace(resolvedUrl))
                {
                    continue;
                }

                if (TryInsertReferenceLink(
                        tweak,
                        reference.Title,
                        resolvedUrl,
                        insertIndex,
                        reference.Summary,
                        MapReferenceKind(reference.Kind)))
                {
                    insertIndex++;
                }
            }
        }
    }

    private TweakProvenanceCatalog LoadCatalog()
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

    private bool TryResolveEntry(string tweakId, out TweakProvenanceEntry entry)
    {
        if (_index.TryGetValue(tweakId, out var directEntry))
        {
            entry = directEntry;
            return true;
        }

        foreach (var templateEntry in _templatedEntries)
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

    private string ResolvePath(string? path)
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
                ? normalized[(5)..]
                : normalized;
            var docsPath = Path.Combine(_docsRoot, trimmed.TrimStart(Path.DirectorySeparatorChar));
            if (File.Exists(docsPath))
            {
                return docsPath;
            }
        }

        return string.Empty;
    }

    private static string BuildFallbackSummary(TweakProvenanceEntry entry)
    {
        if (entry.HasNohutoEvidence && entry.HasWindowsInternalsContext)
        {
            return "Linked to upstream dump / pseudocode sources and Windows Internals notes. Value semantics are still validated in the research record.";
        }

        if (entry.HasNohutoEvidence)
        {
            return "Linked to upstream dump / pseudocode sources. These links show where the setting came from, not what each value means.";
        }

        if (entry.HasWindowsInternalsContext)
        {
            return "Has Windows Internals notes but still needs stronger repo evidence.";
        }

        return "Source review still needed.";
    }

    private static ReferenceLinkKind MapReferenceKind(string? kind)
    {
        var normalized = kind?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "nohuto" => ReferenceLinkKind.Source,
            "internals" => ReferenceLinkKind.Docs,
            "microsoft" => ReferenceLinkKind.Docs,
            "research" => ReferenceLinkKind.Details,
            _ => ReferenceLinkKind.Other
        };
    }

    private static IReadOnlyList<TemplateEntry> BuildTemplateEntries(IEnumerable<TweakProvenanceEntry> entries)
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

    private static bool TryInsertReferenceLink(
        TweakItemViewModel tweak,
        string title,
        string url,
        int index,
        string? tooltip,
        ReferenceLinkKind kind)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (tweak.ReferenceLinks.Any(link =>
                string.Equals(link.Url, url, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var safeIndex = Math.Clamp(index, 0, tweak.ReferenceLinks.Count);
        tweak.ReferenceLinks.Insert(safeIndex, new ReferenceLink(title, url, tooltip, kind));
        return true;
    }

    private sealed record TemplateEntry(TweakProvenanceEntry Entry, Regex Pattern);
}
