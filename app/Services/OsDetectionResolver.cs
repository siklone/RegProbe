using System;
using RegProbe.App.Diagnostics;

namespace RegProbe.App.Services;

public static class OsDetectionResolver
{
    public static OsDetectionResult Resolve(bool includeWmiCrossCheck)
    {
        var state = new OsDetectionState();
        OsRegistryInfoReader.Populate(state);

        if (includeWmiCrossCheck)
        {
            OsWmiInfoReader.Populate(state);
        }

        ApplyApiFallbacks(state);

        state.ReleaseId = OsDisplayNameNormalizer.NormalizeReleaseId(state.ReleaseId, state.BuildNumber);
        var normalizedName = OsDisplayNameNormalizer.NormalizeName(
            state.BuildNumber,
            state.EditionId,
            state.DisplayVersion,
            state.ReleaseId);
        var iconKey = OsDisplayNameNormalizer.GetIconKey(state.BuildNumber);

        AppDiagnostics.Log($"[OsDetectionResolver] BuildNumber={state.BuildNumber}, ProductName={state.ProductName}, DisplayVersion={state.DisplayVersion}, FinalNormalizedName={normalizedName}, ChosenIcon={iconKey}");

        return new OsDetectionResult(
            ProductName: state.ProductName,
            DisplayVersion: state.DisplayVersion,
            ReleaseId: state.ReleaseId,
            BuildNumber: state.BuildNumber,
            Ubr: state.Ubr,
            EditionId: state.EditionId,
            InstallationType: state.InstallationType,
            Version: state.Version,
            ProductNameSource: state.ProductNameSource,
            DisplayVersionSource: state.DisplayVersionSource,
            BuildNumberSource: state.BuildNumberSource,
            NormalizedName: normalizedName,
            IconKey: iconKey,
            Architecture: state.Architecture,
            InstallDate: state.InstallDate,
            LastBootTime: state.LastBootTime);
    }

    private static void ApplyApiFallbacks(OsDetectionState state)
    {
        if (state.BuildNumber <= 0)
        {
            state.BuildNumber = Environment.OSVersion.Version.Build;
            state.BuildNumberSource = "API";
        }

        if (string.IsNullOrWhiteSpace(state.ProductName))
        {
            state.ProductName = Environment.OSVersion.VersionString;
            state.ProductNameSource = "API";
        }

        if (string.IsNullOrWhiteSpace(state.Architecture))
        {
            state.Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        }
    }
}
