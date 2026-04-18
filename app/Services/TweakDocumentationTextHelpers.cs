using RegProbe.App.Utilities;

namespace RegProbe.App.Services;

internal static class TweakDocumentationTextHelpers
{
    public static string ExtractPrefix(string tweakId)
    {
        var dotIndex = tweakId.IndexOf('.');
        if (dotIndex <= 0)
        {
            return "other";
        }

        return tweakId[..dotIndex].ToLowerInvariant();
    }

    public static string BuildDocsTitle(string? category, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return $"Docs: {category}";
        }

        return $"Docs: {StringPool.GetCategory(prefix)}";
    }

    public static string AppendDocAnchor(string docPath, string tweakId)
    {
        if (string.IsNullOrWhiteSpace(docPath) || string.IsNullOrWhiteSpace(tweakId))
        {
            return docPath;
        }

        return docPath.Contains('#') ? docPath : $"{docPath}#{tweakId}";
    }
}
