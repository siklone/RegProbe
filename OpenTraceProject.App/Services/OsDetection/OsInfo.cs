using System;

namespace OpenTraceProject.App.Services.OsDetection;

public sealed class OsInfo
{
    public string? Caption { get; set; }
    public string? Version { get; set; }
    public string? BuildNumber { get; set; }
    public int BuildNumberRaw { get; set; }
    public string? Edition { get; set; }
    public string? EditionId { get; set; }
    public string? DisplayVersion { get; set; }
    public string? ReleaseId { get; set; }
    public string? Architecture { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? LastBootTime { get; set; }
    public string? UptimeFormatted { get; set; }
    public string? SystemDrive { get; set; }
    public string? WindowsDirectory { get; set; }
    public string? RegisteredUser { get; set; }
    public string? Organization { get; set; }
    public string? ProductKey { get; set; }
    public string? ActivationStatus { get; set; }
    public string? InstallationType { get; set; }
    public string? BiosMode { get; set; }
    public bool? SecureBootEnabled { get; set; }
    public int Ubr { get; set; }
    public string? Source { get; set; }

    public string NormalizedName
    {
        get
        {
            var osBase = BuildNumberRaw >= 22000 ? "Windows 11" : "Windows 10";
            var edition = NormalizeEdition(EditionId ?? Edition);
            var normalized = string.IsNullOrWhiteSpace(edition) ? osBase : $"{osBase} {edition}";

            var version = !string.IsNullOrWhiteSpace(DisplayVersion) ? DisplayVersion : ReleaseId;
            if (!string.IsNullOrWhiteSpace(version))
            {
                normalized = $"{normalized} ({version})";
            }

            return normalized;
        }
    }

    public string? IconKey => BuildNumberRaw >= 22000 ? "os_windows11" : "os_windows10";

    private static string NormalizeEdition(string? edition)
    {
        if (string.IsNullOrWhiteSpace(edition))
        {
            return string.Empty;
        }

        return edition.Trim() switch
        {
            "Professional" => "Pro",
            "Core" => "Home",
            "CoreSingleLanguage" => "Home Single Language",
            "EnterpriseS" => "Enterprise LTSC",
            "Enterprise" => "Enterprise",
            "Education" => "Education",
            "ProEducation" => "Pro Education",
            "ProForWorkstations" => "Pro for Workstations",
            _ => edition.Trim()
        };
    }
}
