using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class SettingsWorkspaceCoordinator : ViewModelBase, IDisposable
{
    private static readonly ThemePalette[] _availableThemes =
    {
        ThemeManager.Nord,
        ThemeManager.ElectricPurple,
        ThemeManager.SunsetOrange,
        ThemeManager.CyberGreen,
        ThemeManager.RubyRed
    };

    private readonly ISettingsStore _settingsStore;
    private readonly IAppLogger _appLogger;
    private readonly ThemeManager _themeManager = new();
    private ThemePalette _currentThemePalette = ThemeManager.Nord;
    private bool _isSaving;
    private bool _isLoading;
    private bool _isDisposed;
    private string _statusMessage = "Settings loaded.";

    public SettingsWorkspaceCoordinator()
    {
        var paths = AppPaths.FromEnvironment();
        _settingsStore = new SettingsStore(paths);
        _appLogger = new FileAppLogger(paths);
        _ = LoadSettingsAsync();
    }

    public string Title => "Settings";

    public IEnumerable<ThemePalette> AvailableThemes => _availableThemes;

    public ThemePalette CurrentThemePalette
    {
        get => _currentThemePalette;
        set
        {
            if (SetProperty(ref _currentThemePalette, value))
            {
                _themeManager.ApplyTheme(value);
                QueueSaveIfReady();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    private void QueueSaveIfReady()
    {
        if (_isLoading)
        {
            return;
        }

        _ = SaveSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        _isLoading = true;
        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            _currentThemePalette = AvailableThemes.FirstOrDefault(theme => theme.Name == settings.Theme) ?? ThemeManager.Nord;

            _themeManager.ApplyTheme(_currentThemePalette);

            OnPropertyChanged(nameof(CurrentThemePalette));
            StatusMessage = "Theme preference loads automatically.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load settings: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        IsSaving = true;
        StatusMessage = "Saving changes...";

        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            var previousTheme = settings.Theme;

            settings.Theme = CurrentThemePalette.Name;

            await _settingsStore.SaveAsync(settings, CancellationToken.None);
            LogSettingsChanges(previousTheme, settings);
            StatusMessage = "Changes saved automatically.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void LogSettingsChanges(string previousTheme, AppSettings current)
    {
        var changes = new List<string>();

        if (!string.Equals(previousTheme, current.Theme, StringComparison.OrdinalIgnoreCase))
        {
            changes.Add($"Theme={current.Theme}");
        }

        if (changes.Count == 0)
        {
            return;
        }

        _appLogger.Log(LogLevel.Info, $"Activity: Settings - {string.Join(", ", changes)}");
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
    }
}
