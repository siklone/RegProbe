namespace RegProbe.App.Models;

/// <summary>
/// Represents an installed UWP/AppX package.
/// </summary>
public record AppxPackageInfo(
    string PackageFullName,
    string DisplayName,
    string Publisher,
    string Version,
    string InstallLocation,
    long? SizeInBytes,
    bool IsSystemApp,
    bool IsFramework,
    List<string> Dependencies
);

/// <summary>
/// Result of uninstalling a package.
/// </summary>
public record UninstallResult(
    bool Success,
    string PackageName,
    string ErrorMessage,
    List<string> RemovedDependencies
);

/// <summary>
/// Safety level for package removal.
/// </summary>
public enum PackageSafetyLevel
{
    Safe,           // Can be removed without issues
    Caution,        // May affect some features
    Critical        // System critical - DO NOT REMOVE
}
