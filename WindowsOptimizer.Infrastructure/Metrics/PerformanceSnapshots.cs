using System;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed record CpuPerformanceSnapshot(
    double UsagePercent,
    double? CurrentSpeedMhz,
    double? BaseSpeedMhz,
    int? ProcessCount,
    int? ThreadCount,
    int? HandleCount,
    int? Sockets,
    int? Cores,
    int? LogicalProcessors,
    bool? VirtualizationEnabled,
    int? L2CacheKb,
    int? L3CacheKb);

public sealed record MemoryPerformanceSnapshot(
    double TotalGb,
    double AvailableGb,
    double UsedGb,
    double? CommittedGb,
    double? CommitLimitGb,
    double? CachedGb,
    double? PagedPoolGb,
    double? NonPagedPoolGb,
    double? SpeedMhz,
    int? SlotsUsed,
    int? SlotsTotal,
    string? FormFactor,
    double? HardwareReservedMb);

public sealed record DiskPerformanceSnapshot(
    string DriveLetter,
    int? DiskIndex,
    string? Model,
    string? MediaType,
    string? InterfaceType,
    double TotalSizeGb,
    double FreeSpaceGb,
    double? ActiveTimePercent,
    double? ReadMbps,
    double? WriteMbps,
    double? AvgResponseMs,
    double? QueueLength,
    bool IsSystemDisk,
    bool HasPageFile)
{
    public string? BusType { get; init; }
    public bool? IsExternal { get; init; }
}

public sealed record GpuEngineUsageSnapshot(
    double TotalPercent,
    double Engine3DPercent,
    double CopyPercent,
    double VideoEncodePercent,
    double VideoDecodePercent,
    bool IsAvailable);

public sealed record GpuPerformanceSnapshot(
    string? Name,
    double? DedicatedMemoryMb,
    double? SharedMemoryMb,
    double? TotalMemoryMb,
    string? DriverVersion,
    DateTime? DriverDate,
    string? DirectXVersion,
    string? LocationInfo,
    string? AdapterCompatibility,
    string? VideoProcessor,
    bool IsAvailable);
