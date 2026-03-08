using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App.Services;

public sealed class UiPreferences : ViewModelBase
{
    private static readonly UiPreferences CurrentInstance = new();
    private bool _enableCardShadows;

    public static UiPreferences Current => CurrentInstance;

    public bool EnableCardShadows
    {
        get => _enableCardShadows;
        set => SetProperty(ref _enableCardShadows, value);
    }

    private bool _isCompactMode;
    public bool IsCompactMode
    {
        get => _isCompactMode;
        set => SetProperty(ref _isCompactMode, value);
    }

    private UiPreferences()
    {
    }
}
