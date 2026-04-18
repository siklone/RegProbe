namespace RegProbe.App.Services;

public static class WinConfigCatalogParser
{
    public static string ExtractLeadParagraph(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var paragraph = new List<string>();
        var inCodeFence = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith(">", StringComparison.Ordinal) ||
                line.StartsWith("|", StringComparison.Ordinal) ||
                line.StartsWith("```", StringComparison.Ordinal))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }

                continue;
            }

            paragraph.Add(line);
        }

        return string.Join(" ", paragraph).Trim();
    }

    public static IReadOnlyList<string> ExtractTopLevelTopics(string markdown, int limit = 12)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return Array.Empty<string>();
        }

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var topics = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            var title = line[2..].Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            topics.Add(title);
            if (topics.Count >= limit)
            {
                break;
            }
        }

        return topics;
    }

    public static WinConfigCatalogFileKind ClassifyFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return WinConfigCatalogFileKind.Data;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".md" => WinConfigCatalogFileKind.Documentation,
            ".ps1" or ".cmd" or ".bat" or ".py" or ".reg" or ".inf" or ".json" or ".xml" => WinConfigCatalogFileKind.Script,
            ".png" or ".jpg" or ".jpeg" or ".svg" or ".bmp" or ".ico" or ".pdf" => WinConfigCatalogFileKind.Asset,
            _ => WinConfigCatalogFileKind.Data
        };
    }
}
