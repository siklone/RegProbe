namespace RegProbe.App.Services;

internal sealed class OsDetectionState
{
    public string ProductName { get; set; } = string.Empty;

    public string DisplayVersion { get; set; } = string.Empty;

    public string ReleaseId { get; set; } = string.Empty;

    public string EditionId { get; set; } = string.Empty;

    public string InstallationType { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Architecture { get; set; } = string.Empty;

    public string InstallDate { get; set; } = string.Empty;

    public string LastBootTime { get; set; } = string.Empty;

    public int BuildNumber { get; set; }

    public int Ubr { get; set; }

    public string ProductNameSource { get; set; } = "Unknown";

    public string DisplayVersionSource { get; set; } = "Unknown";

    public string BuildNumberSource { get; set; } = "Unknown";
}
