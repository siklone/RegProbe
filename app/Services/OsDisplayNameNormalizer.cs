using RegProbe.App.Diagnostics;

namespace RegProbe.App.Services;

internal static class OsDisplayNameNormalizer
{
    public static string NormalizeReleaseId(string releaseId, int buildNumber)
    {
        // In-place Windows 11 upgrades can leave old Windows 10 ReleaseId values behind.
        if (buildNumber >= 22000 && int.TryParse(releaseId, out var releaseIdNum) && releaseIdNum <= 2100)
        {
            AppDiagnostics.Log($"[OsDetectionResolver] Cleared stale Win10 ReleaseId '{releaseId}' on Win11 system (build {buildNumber})");
            return string.Empty;
        }

        return releaseId;
    }

    public static string NormalizeName(int buildNumber, string editionId, string displayVersion, string releaseId)
    {
        var osBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var edition = NormalizeEdition(editionId);
        var normalized = string.IsNullOrWhiteSpace(edition) ? osBase : $"{osBase} {edition}";

        var version = !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion : releaseId;
        if (!string.IsNullOrWhiteSpace(version))
        {
            normalized = $"{normalized} ({version})";
        }

        return normalized;
    }

    public static string GetIconKey(int buildNumber)
        => buildNumber >= 22000 ? "os/windows11" : "os/windows10";

    private static string NormalizeEdition(string editionId)
    {
        if (string.IsNullOrWhiteSpace(editionId))
        {
            return string.Empty;
        }

        return editionId.Trim() switch
        {
            "Professional" => "Pro",
            "Core" => "Home",
            "CoreSingleLanguage" => "Home Single Language",
            "EnterpriseS" => "Enterprise LTSC",
            _ => editionId.Trim()
        };
    }
}
