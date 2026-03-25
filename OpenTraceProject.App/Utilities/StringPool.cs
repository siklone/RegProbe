using System.Collections.Concurrent;

namespace OpenTraceProject.App.Utilities;

/// <summary>
/// Provides string interning/pooling to reduce memory usage for repeated strings.
/// </summary>
public static class StringPool
{
    private static readonly ConcurrentDictionary<string, string> Pool = new();

    // Pre-interned common category names
    private static readonly string[] CommonCategories =
    {
        "System", "Security", "Privacy", "Network", "Visibility",
        "Audio", "Peripheral", "Power", "Performance", "Cleanup",
        "Explorer", "Notifications", "Other", "Misc", "Plugin", "Devtools"
    };

    // Pre-interned common status strings
    private static readonly string[] CommonStatuses =
    {
        "Applied", "Not Applied", "Unknown", "Error", "Idle",
        "Running...", "Detecting...", "Applying...", "Verifying...", "Rolling back..."
    };

    // Pre-interned common impact areas
    private static readonly string[] CommonImpactAreas =
    {
        "Registry", "Service", "Task", "File", "Other"
    };

    static StringPool()
    {
        // Pre-populate pool with common strings
        foreach (var cat in CommonCategories)
        {
            Pool[cat] = string.Intern(cat);
        }

        foreach (var status in CommonStatuses)
        {
            Pool[status] = string.Intern(status);
        }

        foreach (var area in CommonImpactAreas)
        {
            Pool[area] = string.Intern(area);
        }
    }

    /// <summary>
    /// Gets an interned version of the string if it exists in the pool,
    /// or adds it to the pool and returns the interned version.
    /// </summary>
    public static string Intern(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return Pool.GetOrAdd(value, static s => string.Intern(s));
    }

    /// <summary>
    /// Gets a category name from the pool, capitalizing first letter.
    /// </summary>
    public static string GetCategory(string rawCategory)
    {
        if (string.IsNullOrEmpty(rawCategory))
            return Pool["Other"];

        var normalized = char.ToUpper(rawCategory[0]) + rawCategory.Substring(1).ToLowerInvariant();
        return Pool.GetOrAdd(normalized, static s => string.Intern(s));
    }

    /// <summary>
    /// Gets a status string from the pool.
    /// </summary>
    public static string GetStatus(string status)
    {
        return Intern(status);
    }

    /// <summary>
    /// Gets an impact area string from the pool.
    /// </summary>
    public static string GetImpactArea(string area)
    {
        return Intern(area);
    }
}
