namespace RegProbe.Application.Utilities;

internal static class ElevatedHostCandidateValidator
{
    public static bool IsCompleteHostCandidate(string candidate)
    {
        if (!File.Exists(candidate))
        {
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(candidate);
            var baseName = Path.GetFileNameWithoutExtension(candidate);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName))
            {
                return false;
            }

            var companionDll = Path.Combine(directory, $"{baseName}.dll");
            var runtimeConfig = Path.Combine(directory, $"{baseName}.runtimeconfig.json");
            return File.Exists(companionDll) && File.Exists(runtimeConfig);
        }
        catch
        {
            return false;
        }
    }
}
