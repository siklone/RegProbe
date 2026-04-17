using System.Text.RegularExpressions;

namespace RegProbe.App.Services;

internal sealed class TweakDocumentationAnchorCache
{
    private static readonly Regex AnchorRegex = new(
        "id\\s*=\\s*\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Dictionary<string, HashSet<string>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public bool HasDocAnchor(string docPath, string tweakId)
    {
        if (string.IsNullOrWhiteSpace(docPath) || string.IsNullOrWhiteSpace(tweakId))
        {
            return false;
        }

        if (!_cache.TryGetValue(docPath, out var anchors))
        {
            anchors = LoadDocAnchors(docPath);
            _cache[docPath] = anchors;
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
}
