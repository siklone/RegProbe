using System;

namespace WindowsOptimizer.Core.Intelligence;

public enum StorageType
{
    Unknown,
    HDD,
    SSD,
    NVMe
}

public sealed record HardwareProfile(
    string ProcessorName,
    int CoreCount,
    bool IsHyperThreadingEnabled,
    long TotalMemoryBytes,
    StorageType PrimaryDiskType,
    string GpuName,
    bool IsAVX512Supported
);

public sealed record TweakRecommendation(
    string TweakId,
    double ConfidenceScore, // 0.0 to 1.0
    string Reason
);
