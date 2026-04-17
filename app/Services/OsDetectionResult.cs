namespace RegProbe.App.Services;

public sealed record OsDetectionResult(
    string ProductName,
    string DisplayVersion,
    string ReleaseId,
    int BuildNumber,
    int Ubr,
    string EditionId,
    string InstallationType,
    string Version,
    string ProductNameSource,
    string DisplayVersionSource,
    string BuildNumberSource,
    string NormalizedName,
    string IconKey,
    string Architecture,
    string InstallDate,
    string LastBootTime);
