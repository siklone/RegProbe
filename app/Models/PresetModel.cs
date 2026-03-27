namespace RegProbe.App.Models;

/// <summary>
/// Represents a preset optimization profile.
/// </summary>
public record PresetModel(
    string Id,
    string Name,
    string Description,
    string IconPath,
    PresetCategory Category,
    List<string> TweakIds,
    PresetDifficulty Level
);

/// <summary>
/// Category classification for presets.
/// </summary>
public enum PresetCategory
{
    Performance,
    Privacy,
    Aesthetics,
    Minimal,
    Gaming
}

/// <summary>
/// Difficulty level indicating technical complexity.
/// </summary>
public enum PresetDifficulty
{
    Beginner,
    Advanced,
    Expert
}
