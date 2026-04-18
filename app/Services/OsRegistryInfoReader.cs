using Microsoft.Win32;
using RegProbe.App.Diagnostics;

namespace RegProbe.App.Services;

internal static class OsRegistryInfoReader
{
    public static void Populate(OsDetectionState state)
    {
        try
        {
            using var currentVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (currentVersion == null)
            {
                return;
            }

            state.ProductName = currentVersion.GetValue("ProductName")?.ToString() ?? string.Empty;
            state.DisplayVersion = currentVersion.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
            state.ReleaseId = currentVersion.GetValue("ReleaseId")?.ToString() ?? string.Empty;
            state.EditionId = currentVersion.GetValue("EditionID")?.ToString() ?? string.Empty;
            state.InstallationType = currentVersion.GetValue("InstallationType")?.ToString() ?? string.Empty;
            state.Version = currentVersion.GetValue("CurrentVersion")?.ToString() ?? string.Empty;
            _ = int.TryParse(currentVersion.GetValue("UBR")?.ToString(), out var ubr);
            state.Ubr = ubr;

            var buildText = currentVersion.GetValue("CurrentBuild")?.ToString();
            if (string.IsNullOrWhiteSpace(buildText))
            {
                buildText = currentVersion.GetValue("CurrentBuildNumber")?.ToString();
            }

            if (int.TryParse(buildText, out var parsedBuild))
            {
                state.BuildNumber = parsedBuild;
                state.BuildNumberSource = "Registry";
            }

            if (!string.IsNullOrWhiteSpace(state.ProductName))
            {
                state.ProductNameSource = "Registry";
            }

            if (!string.IsNullOrWhiteSpace(state.DisplayVersion))
            {
                state.DisplayVersionSource = "Registry";
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[OsDetectionResolver] Registry read failed: {ex.Message}");
        }
    }
}
