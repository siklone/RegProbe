namespace RegProbe.App.Services;

internal static class NohutoChangeKindResolver
{
    public static NohutoChangeKind Resolve(string path)
    {
        if (path.StartsWith("guide/", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Documentation;
        }

        if (path.StartsWith("records/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Data;
        }

        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var byExtension = extension switch
        {
            ".ps1" or ".cmd" or ".bat" or ".vbs" or ".reg" or ".py" or ".iss" => NohutoChangeKind.Script,
            ".c" or ".cc" or ".cpp" or ".h" or ".hpp" or ".rc" or ".vcxproj" or ".filters" or ".props" or ".sln" => NohutoChangeKind.Source,
            ".ico" or ".png" or ".jpg" or ".jpeg" or ".svg" or ".bmp" => NohutoChangeKind.Asset,
            ".txt" or ".json" or ".csv" => NohutoChangeKind.Data,
            _ => NohutoChangeKind.Data
        };

        if (byExtension != NohutoChangeKind.Data)
        {
            return byExtension;
        }

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Asset;
        }

        if (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("include/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/src/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/include/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Source;
        }

        return NohutoChangeKind.Data;
    }

    public static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimStart('/');
}
