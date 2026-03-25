namespace OpenTraceProject.App.Models;

/// <summary>
/// Result of applying a preset.
/// </summary>
public record ApplyPresetResult(
    bool Success,
    int Applied,
    int Total,
    List<string> FailedTweaks,
    string Message
);

/// <summary>
/// Result of validating a preset.
/// </summary>
public record PresetValidationResult(
    bool IsValid,
    List<string> IncompatibleTweaks,
    string OsVersion,
    List<string> Warnings
);
