namespace RegProbe.Application.Utilities;

internal readonly record struct ElevatedHostBuildInfo(
    string? Configuration,
    string? TargetFramework,
    string? RuntimeIdentifier);
