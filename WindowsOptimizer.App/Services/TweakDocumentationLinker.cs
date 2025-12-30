using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App.Services;

public sealed class TweakDocumentationLinker
{
    private const string DefaultDocPath = "tweaks/tweaks.md";
    private readonly IReadOnlyDictionary<string, string> _categoryDocMap;
    private readonly string? _docsRoot;

    public TweakDocumentationLinker(string? docsRoot = null)
    {
        _docsRoot = docsRoot ?? DocsLocator.TryFindDocsRoot();
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
    }

    public void Apply(IEnumerable<TweakItemViewModel> tweaks)
    {
        if (string.IsNullOrWhiteSpace(_docsRoot))
        {
            return;
        }

        foreach (var tweak in tweaks)
        {
            if (tweak is null || string.IsNullOrWhiteSpace(tweak.Id))
            {
                continue;
            }

            var prefix = ExtractPrefix(tweak.Id);
            var docPath = ResolveDocPath(prefix);
            if (string.IsNullOrWhiteSpace(docPath))
            {
                continue;
            }

            if (tweak.ReferenceLinks.Any(link => string.Equals(link.Url, docPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var title = $"Docs: {StringPool.GetCategory(prefix)}";
            tweak.ReferenceLinks.Insert(0, new ReferenceLink(title, docPath));
        }
    }

    private string ResolveDocPath(string prefix)
    {
        var relative = _categoryDocMap.TryGetValue(prefix, out var mapped)
            ? mapped
            : DefaultDocPath;

        var fullPath = Path.Combine(_docsRoot ?? string.Empty, relative);
        return File.Exists(fullPath) ? fullPath : string.Empty;
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
}
