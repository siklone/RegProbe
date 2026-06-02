namespace RegProbe.App.Services;

using System;
using System.IO;
using RegProbe.App.Utilities;

internal static class PublicEvidenceLinkPolicy
{
    public const string NoLocalSourceMessage =
        "No RegProbe-controlled source or pseudocode mirror is attached for this card. The Source lane is contributor context only: catalog matches can explain naming/navigation, but they do not prove value behavior. Trust the Docs, Runtime, and Rollback lanes for normal app safety.";

    public const string LocalSourceContextMessage =
        "RegProbe-controlled source or pseudocode context is attached for discovery and naming. It is not value-behavior proof by itself; app safety still comes from Docs, Runtime, and Rollback.";

    public static bool IsSuppressedExternalPseudocodeUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && url.Contains("github.com/nohuto/decompiled-pseudocode", StringComparison.OrdinalIgnoreCase);

    public static bool IsSuppressedExternalSourceUrl(string? url)
        => IsSuppressedExternalPseudocodeUrl(url)
           || (!string.IsNullOrWhiteSpace(url)
               && url.Contains("github.com/nohuto/", StringComparison.OrdinalIgnoreCase));

    public static bool IsMissingLocalSourceMirrorUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri)
                && (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))))
        {
            return false;
        }

        var rawPath = url.Split('#', 2)[0].Trim();
        if (!rawPath.Contains("research/_source-mirrors/", StringComparison.OrdinalIgnoreCase)
            && !rawPath.Contains(@"research\_source-mirrors\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !TryResolveRepoPath(rawPath, out _);
    }

    public static bool ShouldSuppressSourceLink(string? url)
        => IsSuppressedExternalSourceUrl(url) || IsMissingLocalSourceMirrorUrl(url);

    public static bool IsSuppressedExternalSourceText(string? text)
        => !string.IsNullOrWhiteSpace(text)
           && text.Contains("github.com/nohuto/", StringComparison.OrdinalIgnoreCase);

    public static string SanitizePrimarySourceText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || IsSuppressedExternalSourceText(text))
        {
            return string.Empty;
        }

        if (ContainsMissingLocalSourceMirrorReference(text))
        {
            return string.Empty;
        }

        return text.Trim().Replace("nohuto", "local source mirror", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsExternalOnlySourceEvidence(
        string? summary,
        string? primarySourceText,
        int visibleSourceLinkCount)
    {
        if (visibleSourceLinkCount > 0)
        {
            return false;
        }

        if (IsSuppressedExternalSourceText(primarySourceText) || IsNoLocalSourceSummary(summary))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            return false;
        }

        return summary.Contains("Upstream dump / pseudocode links are attached", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("nohuto", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("upstream dump", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("pseudocode", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("upstream documentation", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNoLocalSourceSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return false;
        }

        return summary.Contains("No RegProbe-controlled local source mirror", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("No RegProbe-controlled source or pseudocode mirror", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("Catalog-only source context", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("Source lane is contributor context only", StringComparison.OrdinalIgnoreCase)
               || summary.Contains("No upstream nohuto source link", StringComparison.OrdinalIgnoreCase);
    }

    public static string SanitizeSourceSummary(string? summary, int visibleSourceLinkCount)
    {
        var text = summary?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return visibleSourceLinkCount > 0
                ? LocalSourceContextMessage
                : NoLocalSourceMessage;
        }

        if (text.Contains("No upstream nohuto source link", StringComparison.OrdinalIgnoreCase))
        {
            return NoLocalSourceMessage;
        }

        if (text.Contains("Upstream dump / pseudocode links are attached", StringComparison.OrdinalIgnoreCase))
        {
            return visibleSourceLinkCount > 0
                ? LocalSourceContextMessage
                : NoLocalSourceMessage;
        }

        if (IsExternalOnlySourceEvidence(text, primarySourceText: null, visibleSourceLinkCount))
        {
            return NoLocalSourceMessage;
        }

        return text.Replace("nohuto", "external upstream", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveRepoPath(string path, out string localPath)
    {
        localPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace('/', Path.DirectorySeparatorChar).Trim();
        if (Path.IsPathRooted(normalized) && File.Exists(normalized))
        {
            localPath = normalized;
            return true;
        }

        var docsRoot = DocsLocator.TryFindDocsRoot();
        var repoRoot = string.IsNullOrWhiteSpace(docsRoot)
            ? string.Empty
            : Directory.GetParent(docsRoot)?.FullName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return false;
        }

        var candidate = Path.Combine(repoRoot, normalized.TrimStart(Path.DirectorySeparatorChar));
        if (!File.Exists(candidate))
        {
            return false;
        }

        localPath = candidate;
        return true;
    }

    private static bool ContainsMissingLocalSourceMirrorReference(string text)
    {
        foreach (var token in text.Split(
                     new[] { ' ', '\t', '\r', '\n' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = token.Trim()
                .TrimStart('(', '[', '{', '<', '"', '\'')
                .TrimEnd('.', ',', ';', ':', ')', ']', '}', '>', '"', '\'');
            if (IsMissingLocalSourceMirrorUrl(candidate))
            {
                return true;
            }
        }

        return false;
    }
}
