namespace OpenTraceProject.Core.Intelligence;

/// <summary>
/// Represents hardware profile information for recommendation engine.
/// </summary>
public record HardwareProfile(
    string ProcessorName,
    int CoreCount,
    bool HasHyperThreading,
    long TotalMemoryBytes,
    StorageType PrimaryDiskType,
    string GpuName,
    bool HasAvx512
);

/// <summary>
/// Type of primary storage device.
/// </summary>
public enum StorageType
{
    Unknown,
    HDD,
    SSD,
    NVMe
}

/// <summary>
/// Recommendation for a specific tweak based on hardware profile.
/// </summary>
public record TweakRecommendation(
    string TweakId,
    double ConfidenceScore,
    string Reason
);
