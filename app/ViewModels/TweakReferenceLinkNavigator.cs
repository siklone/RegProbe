using System;
using System.Diagnostics;
using System.IO;
using RegProbe.App.Utilities;

namespace RegProbe.App.ViewModels;

internal static class TweakReferenceLinkNavigator
{
    public static TweakReferenceOpenResult Open(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return TweakReferenceOpenResult.Failure("Could not open link: Link is empty.");
        }

        try
        {
            if (TryOpenFileAnchor(url))
            {
            return TweakReferenceOpenResult.Succeeded("Opening catalog entry...");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
                Verb = "open"
            });

            return TweakReferenceOpenResult.Succeeded("Opening link...");
        }
        catch (Exception ex)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = url,
                    UseShellExecute = true
                });

                return TweakReferenceOpenResult.Succeeded("Opening link...");
            }
            catch
            {
            }

            return TweakReferenceOpenResult.Failure($"Could not open link: {ex.Message}", ex.Message);
        }
    }

    private static bool TryOpenFileAnchor(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri)
            && (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var (rawPath, anchor) = SplitAnchor(url);
        if (TryResolveLocalPath(rawPath, out var localPath))
        {
            return TryOpenLocalPath(localPath, anchor);
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out absoluteUri))
        {
            if (!absoluteUri.IsFile)
            {
                return false;
            }

            var absoluteAnchor = absoluteUri.Fragment;
            var trimmedAnchor = string.IsNullOrWhiteSpace(absoluteAnchor)
                ? anchor
                : absoluteAnchor.TrimStart('#');
            return TryOpenLocalPath(absoluteUri.LocalPath, trimmedAnchor);
        }

        var hashIndex = url.IndexOf('#', StringComparison.Ordinal);
        var path = hashIndex > 0 ? url.Substring(0, hashIndex) : url;
        var fallbackAnchor = hashIndex > 0 ? url[(hashIndex + 1)..] : string.Empty;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        return TryOpenLocalPath(path, fallbackAnchor);
    }

    private static bool TryOpenLocalPath(string localPath, string? anchor)
    {
        if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
        {
            return false;
        }

        var extension = Path.GetExtension(localPath);
        var allowAnchor = string.Equals(extension, ".html", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(extension, ".htm", StringComparison.OrdinalIgnoreCase);

        if (allowAnchor && !string.IsNullOrWhiteSpace(anchor))
        {
            var escapedAnchor = Uri.EscapeDataString(anchor);
            var fileUri = new Uri(localPath, UriKind.Absolute);
            var anchoredUri = new Uri(fileUri.AbsoluteUri + "#" + escapedAnchor, UriKind.Absolute);
            Process.Start(new ProcessStartInfo
            {
                FileName = anchoredUri.AbsoluteUri,
                UseShellExecute = true
            });
            return true;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = localPath,
            UseShellExecute = true
        });
        return true;
    }

    private static (string Path, string? Anchor) SplitAnchor(string url)
    {
        var hashIndex = url.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex <= 0)
        {
            return (url, null);
        }

        return (url[..hashIndex], url[(hashIndex + 1)..]);
    }

    private static bool TryResolveLocalPath(string path, out string localPath)
    {
        localPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Path.IsPathRooted(path) && File.Exists(path))
        {
            localPath = path;
            return true;
        }

        var docsRoot = DocsLocator.TryFindDocsRoot();
        var repoRoot = string.IsNullOrWhiteSpace(docsRoot)
            ? string.Empty
            : Directory.GetParent(docsRoot)?.FullName ?? string.Empty;
        var normalized = path.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            var repoCandidate = Path.Combine(repoRoot, normalized);
            if (File.Exists(repoCandidate))
            {
                localPath = repoCandidate;
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(docsRoot))
        {
            var trimmed = normalized;
            if (trimmed.StartsWith("Docs", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[4..].TrimStart(Path.DirectorySeparatorChar);
            }

            var docsCandidate = Path.Combine(docsRoot, trimmed);
            if (File.Exists(docsCandidate))
            {
                localPath = docsCandidate;
                return true;
            }
        }

        return false;
    }
}

internal readonly record struct TweakReferenceOpenResult(bool Success, string StatusMessage, string? ErrorMessage)
{
    public static TweakReferenceOpenResult Succeeded(string statusMessage) => new(true, statusMessage, null);

    public static TweakReferenceOpenResult Failure(string statusMessage, string? errorMessage = null) => new(false, statusMessage, errorMessage);
}
