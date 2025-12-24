using System;
using System.Collections.Generic;

namespace WindowsOptimizer.Core.Cloud;

/// <summary>
/// Cloud preset from the community repository
/// </summary>
public sealed class CloudPreset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Author { get; init; }
    public required PresetCategory Category { get; init; }
    public required List<string> TweakIds { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required int DownloadCount { get; init; }
    public required double AverageRating { get; init; }
    public required int RatingCount { get; init; }
    public required string Version { get; init; }
    public required List<string> Tags { get; init; }
    public string? IconUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? DigitalSignature { get; init; }
    public bool IsVerified { get; init; }
    public bool IsOfficial { get; init; }

    /// <summary>
    /// Minimum Windows version required (e.g., "10.0.19041")
    /// </summary>
    public string? MinWindowsVersion { get; init; }

    /// <summary>
    /// Hardware requirements or compatibility notes
    /// </summary>
    public string? HardwareRequirements { get; init; }
}

/// <summary>
/// Preset category for filtering and organization
/// </summary>
public enum PresetCategory
{
    Gaming,
    Work,
    Streaming,
    Privacy,
    Performance,
    Battery,
    Multimedia,
    Development,
    Server,
    Custom
}

/// <summary>
/// Result of downloading a preset
/// </summary>
public sealed class PresetDownloadResult
{
    public required bool Success { get; init; }
    public CloudPreset? Preset { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsSignatureValid { get; init; }
    public DateTime DownloadedAt { get; init; }
}

/// <summary>
/// Result of uploading a preset
/// </summary>
public sealed class PresetUploadResult
{
    public required bool Success { get; init; }
    public string? PresetId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime UploadedAt { get; init; }
}

/// <summary>
/// User rating for a preset
/// </summary>
public sealed class PresetRating
{
    public required string PresetId { get; init; }
    public required int Stars { get; init; } // 1-5
    public required string? Review { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string UserId { get; init; }

    /// <summary>
    /// Performance impact reported by user
    /// </summary>
    public PerformanceImpactReport? PerformanceImpact { get; init; }
}

/// <summary>
/// Performance impact metrics reported by users
/// </summary>
public sealed class PerformanceImpactReport
{
    public double FpsImprovement { get; init; }
    public double LatencyReduction { get; init; }
    public double BootTimeReduction { get; init; }
    public string? HardwareConfig { get; init; }
}

/// <summary>
/// Search filter for finding presets
/// </summary>
public sealed class PresetSearchFilter
{
    public string? Query { get; init; }
    public PresetCategory? Category { get; init; }
    public List<string>? Tags { get; init; }
    public int? MinRating { get; init; }
    public bool? VerifiedOnly { get; init; }
    public bool? OfficialOnly { get; init; }
    public PresetSortOrder SortOrder { get; init; } = PresetSortOrder.MostDownloaded;
    public int PageSize { get; init; } = 20;
    public int PageNumber { get; init; } = 1;
}

/// <summary>
/// Sort order for preset search results
/// </summary>
public enum PresetSortOrder
{
    MostDownloaded,
    HighestRated,
    Newest,
    RecentlyUpdated,
    MostReviewed
}

/// <summary>
/// Search results from the repository
/// </summary>
public sealed class PresetSearchResult
{
    public required List<CloudPreset> Presets { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}

/// <summary>
/// Crowdsourced optimization data for a specific tweak
/// </summary>
public sealed class TweakEffectivenessData
{
    public required string TweakId { get; init; }
    public required int TotalApplications { get; init; }
    public required double SuccessRate { get; init; }
    public required double AverageFpsImprovement { get; init; }
    public required double AverageLatencyReduction { get; init; }
    public required Dictionary<string, int> HardwareBreakdown { get; init; }
    public required DateTime LastUpdated { get; init; }
}
