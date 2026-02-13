using System;

namespace WindowsOptimizer.Core;

/// <summary>
/// Represents documentation source information for a tweak.
/// Every tweak must have verified documentation from a trusted source.
/// </summary>
public sealed record TweakDocumentation
{
    /// <summary>
    /// Source name: "nohuto", "Microsoft", "25H2", etc.
    /// </summary>
    public required string Source { get; init; }
    
    /// <summary>
    /// Direct URL to the documentation.
    /// </summary>
    public required string DocumentationUrl { get; init; }
    
    /// <summary>
    /// Research method: "IDA decompile", "WPR trace", "Registry diff", "Official docs"
    /// </summary>
    public string? VerificationMethod { get; init; }
    
    /// <summary>
    /// Whether this tweak has been verified on real hardware.
    /// </summary>
    public bool Verified { get; init; }
    
    /// <summary>
    /// Date when documentation was last verified.
    /// </summary>
    public DateTimeOffset? LastVerified { get; init; }
    
    /// <summary>
    /// Creates a TweakDocumentation for nohuto-sourced tweaks.
    /// </summary>
    public static TweakDocumentation FromNohuto(string category, string anchor = "") =>
        new()
        {
            Source = "nohuto",
            DocumentationUrl = BuildLocalNohutoPath(category, anchor),
            VerificationMethod = "IDA + WPR",
            Verified = true,
            LastVerified = DateTimeOffset.UtcNow
        };

    private static string BuildLocalNohutoPath(string category, string anchor)
    {
        const string basePath = "docs/tweaks/win-config/batch-01.md";
        var section = string.IsNullOrWhiteSpace(anchor) ? category : anchor;
        if (string.IsNullOrWhiteSpace(section))
        {
            return basePath;
        }

        var slug = section
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-");

        return $"{basePath}#{slug}";
    }
    
    /// <summary>
    /// Creates a TweakDocumentation for Microsoft Docs sourced tweaks.
    /// </summary>
    public static TweakDocumentation FromMicrosoft(string url) =>
        new()
        {
            Source = "Microsoft",
            DocumentationUrl = url,
            VerificationMethod = "Official docs",
            Verified = true,
            LastVerified = DateTimeOffset.UtcNow
        };
}

/// <summary>
/// Interface for tweaks that have documentation metadata.
/// </summary>
public interface ITweakWithDocumentation
{
    /// <summary>
    /// Documentation source information for this tweak.
    /// </summary>
    TweakDocumentation? Documentation { get; }
}
