using System;
using System.IO;

namespace RegProbe.App.Utilities;

public static class DocsLocator
{
    private const string DocsFolderName = "Docs";
    private const string DocsMarkerPath = "tweaks/tweaks.md";

    public static string? TryFindDocsRoot()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var current = new DirectoryInfo(baseDir);

            for (var depth = 0; depth < 6 && current is not null; depth++)
            {
                var candidate = Path.Combine(current.FullName, DocsFolderName);
                if (Directory.Exists(candidate))
                {
                    var marker = Path.Combine(candidate, DocsMarkerPath);
                    if (File.Exists(marker))
                    {
                        return candidate;
                    }
                }

                current = current.Parent;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
