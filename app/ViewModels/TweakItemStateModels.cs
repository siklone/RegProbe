namespace RegProbe.App.ViewModels;

/// <summary>
/// Types of primary actions for a tweak.
/// </summary>
public enum TweakActionType
{
    Toggle,
    Open,
    Import,
    Export,
    Clean,
    Remove,
    Custom
}

/// <summary>
/// Simplified status for first-glance view.
/// </summary>
public enum TweakAppliedStatus
{
    Unknown,
    Applied,
    NotApplied,
    Error
}
