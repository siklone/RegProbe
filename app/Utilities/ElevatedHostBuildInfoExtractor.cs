namespace RegProbe.Application.Utilities;

internal static class ElevatedHostBuildInfoExtractor
{
    public static ElevatedHostBuildInfo Extract(string baseDirectory)
    {
        try
        {
            string? configuration = null;
            string? targetFramework = null;
            string? runtimeIdentifier = null;

            var current = new DirectoryInfo(baseDirectory);
            while (current is not null)
            {
                var name = current.Name;

                if (configuration is null
                    && (string.Equals(name, "Debug", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(name, "Release", StringComparison.OrdinalIgnoreCase)))
                {
                    configuration = name;
                }
                else if (targetFramework is null
                         && name.StartsWith("net", StringComparison.OrdinalIgnoreCase)
                         && name.Contains("windows", StringComparison.OrdinalIgnoreCase))
                {
                    targetFramework = name;
                }
                else if (runtimeIdentifier is null
                         && name.StartsWith("win-", StringComparison.OrdinalIgnoreCase))
                {
                    runtimeIdentifier = name;
                }

                current = current.Parent;
            }

            return new ElevatedHostBuildInfo(configuration, targetFramework, runtimeIdentifier);
        }
        catch
        {
            return new ElevatedHostBuildInfo(null, null, null);
        }
    }
}
