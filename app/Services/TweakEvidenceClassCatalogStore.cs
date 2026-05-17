using System.Text.Json;
using RegProbe.App.Utilities;

namespace RegProbe.App.Services;

internal sealed class TweakEvidenceClassCatalogStore
{
    private const string CatalogPath = "research/evidence-classes.json";

    private readonly string? _docsRoot;
    private readonly string? _repoRoot;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TweakEvidenceClassCatalogStore(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
        _repoRoot = string.IsNullOrWhiteSpace(_docsRoot)
            ? null
            : Directory.GetParent(_docsRoot)?.FullName;
    }

    public TweakEvidenceClassCatalog LoadCatalog()
    {
        var path = ResolvePath(CatalogPath);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new TweakEvidenceClassCatalog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TweakEvidenceClassCatalog>(json, JsonOptions)
                   ?? new TweakEvidenceClassCatalog();
        }
        catch
        {
            return new TweakEvidenceClassCatalog();
        }
    }

    public static IReadOnlyDictionary<string, TweakEvidenceClassEntry> BuildIndex(IEnumerable<TweakEvidenceClassEntry> entries)
    {
        var index = new Dictionary<string, TweakEvidenceClassEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.RecordId) && !index.ContainsKey(entry.RecordId))
            {
                index[entry.RecordId] = entry;
            }

            if (!string.IsNullOrWhiteSpace(entry.TweakId) && !index.ContainsKey(entry.TweakId))
            {
                index[entry.TweakId] = entry;
            }
        }

        return index;
    }

    public TweakEvidenceClassEntry CloneWithResolvedLinks(TweakEvidenceClassEntry entry)
    {
        return new TweakEvidenceClassEntry
        {
            RecordId = entry.RecordId,
            TweakId = entry.TweakId,
            RecordStatus = entry.RecordStatus,
            EvidenceClass = entry.EvidenceClass,
            ClassLabel = entry.ClassLabel,
            ClassTitle = entry.ClassTitle,
            ClassDescription = entry.ClassDescription,
            ShowInApp = entry.ShowInApp,
            IsActionable = entry.IsActionable,
            IsArchived = entry.IsArchived,
            ActionState = entry.ActionState,
            GatingReason = entry.GatingReason,
            Confidence = entry.Confidence,
            AppMappingStatus = entry.AppMappingStatus,
            RestoreStoryKnown = entry.RestoreStoryKnown,
            ValidatedSemantics = CloneBlock(entry.ValidatedSemantics, isSourceBlock: false),
            RuntimeProof = CloneBlock(entry.RuntimeProof, isSourceBlock: false),
            UpstreamLineage = CloneBlock(entry.UpstreamLineage, isSourceBlock: true),
        };
    }

    private TweakEvidenceProofBlock? CloneBlock(TweakEvidenceProofBlock? block, bool isSourceBlock)
    {
        if (block is null)
        {
            return null;
        }

        var links = block.Links
            .Select(link => new TweakEvidenceLink
            {
                Title = link.Title,
                Url = ResolvePath(link.Url),
                Kind = link.Kind,
                Summary = link.Summary,
            })
            .Where(link => !string.IsNullOrWhiteSpace(link.Url)
                           && (!isSourceBlock || !PublicEvidenceLinkPolicy.IsSuppressedExternalSourceUrl(link.Url)))
            .ToList();
        var hasNohutoLineage = !isSourceBlock || links.Count > 0
            ? block.HasNohutoLineage
            : false;

        return new TweakEvidenceProofBlock
        {
            Summary = isSourceBlock
                ? PublicEvidenceLinkPolicy.SanitizeSourceSummary(block.Summary, links.Count)
                : block.Summary,
            HasValidationProof = block.HasValidationProof,
            HasSemanticsEvidence = block.HasSemanticsEvidence,
            NeedsVmValidation = block.NeedsVmValidation,
            HasRuntimeEvidence = block.HasRuntimeEvidence,
            HasNohutoLineage = hasNohutoLineage,
            Links = links,
            PrimarySourceText = BuildPrimarySourceText(links.FirstOrDefault(IsSourceProofLink)),
        };
    }

    private string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return path;
        }

        var normalized = path.Replace('/', Path.DirectorySeparatorChar).Trim();
        if (Path.IsPathRooted(normalized))
        {
            return File.Exists(normalized) ? normalized : path;
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

        return path;
    }

    private static string BuildPrimarySourceText(TweakEvidenceLink? link)
    {
        if (link is null || string.IsNullOrWhiteSpace(link.Url))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(link.Title)
            ? link.Url
            : $"{link.Title}: {link.Url}";
    }

    private static bool IsSourceProofLink(TweakEvidenceLink link)
        => string.Equals(link.Kind, "source", StringComparison.OrdinalIgnoreCase)
           || string.Equals(link.Kind, "nohuto", StringComparison.OrdinalIgnoreCase);
}
